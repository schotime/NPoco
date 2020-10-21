using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class ParameterHelper
    {
        // Helper to handle named parameters from object properties
        public static Regex rxParamsPrefix = new Regex(@"(?<!@)@\w+", RegexOptions.Compiled);

        public static string ProcessParams(string sql, object[] args_src, List<object> args_dest, bool reuseParameters = false)
        {
            var parameters = new Dictionary<string, string>();
            return rxParamsPrefix.Replace(sql, m =>
            {
                string item;
                if (parameters.TryGetValue(m.Value, out item))
                    return item;

                item = parameters[m.Value] = ProcessParam(ref sql, m.Value, args_src, args_dest, reuseParameters);
                return item;
            });
        }
        
        private static string ProcessParam(ref string sql, string rawParam, object[] args_src, List<object> args_dest, bool reuseParameters)
        {
            string param = rawParam.Substring(1);

            object arg_val;

            int paramIndex;
            if (Int32.TryParse(param, out paramIndex))
            {
                // Numbered parameter
                if (paramIndex < 0 || paramIndex >= args_src.Length)
                    throw new ArgumentOutOfRangeException(String.Format("Parameter '@{0}' specified but only {1} parameters supplied (in `{2}`)", paramIndex, args_src.Length, sql));
                arg_val = args_src[paramIndex];
            }
            else
            {
                // Look for a property on one of the arguments with this name
                bool found = false;
                arg_val = null;
                foreach (var o in args_src)
                {
                    var dict = o as IDictionary;
                    if (dict != null)
                    {
                        Type[] arguments = dict.GetType().GetGenericArguments();

                        if (arguments[0] == typeof(string))
                        {
                            var val = dict[param];
                            if (val != null)
                            {
                                found = true;
                                arg_val = val;
                                break;
                            }
                        }
                    }

                    var type = o.GetType();
                    var pi = type.GetProperty(param);
                    if (pi != null)
                    {
                        arg_val = pi.GetValue(o, null);
                        found = true;
                        break;
                    }

                    var fi =  type.GetField(param);
                    if (fi != null)
                    {
                        arg_val = fi.GetValue(o);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new ArgumentException(String.Format("Parameter '@{0}' specified but none of the passed arguments have a property with this name (in '{1}')", param, sql));
            }

            // Expand collections to parameter lists
            if ((arg_val as System.Collections.IEnumerable) != null &&
                (arg_val as string) == null &&
                (arg_val as byte[]) == null)
            {
                var sb = new StringBuilder();
                foreach (var i in arg_val as System.Collections.IEnumerable)
                {
                    var indexOfExistingValue = args_dest.IndexOf(i);
                    if (indexOfExistingValue >= 0)
                    {
                        sb.Append((sb.Length == 0 ? "@" : ",@") + indexOfExistingValue);
                    }
                    else
                    {
                        sb.Append((sb.Length == 0 ? "@" : ",@") + args_dest.Count);
                        args_dest.Add(i);
                    }
                }
                if (sb.Length == 0)
                {
                    Type type = typeof(string);
                    var t = arg_val.GetType();
                    if (t.IsArray)
                        type = t.GetElementType();
                    else if (t.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)))
                        type = t.GetGenericArguments().First();

                    sb.AppendFormat($"select @{args_dest.Count} /*poco_dual*/ where 1 = 0");
                    args_dest.Add(MappingHelper.GetDefault(type));
                }
                return sb.ToString();
            }
            else
            {
                if (reuseParameters)
                {
                    var indexOfExistingValue = args_dest.IndexOf(arg_val);
                    if (indexOfExistingValue >= 0)
                        return "@" + indexOfExistingValue;
                }

                args_dest.Add(arg_val);
                return "@" + (args_dest.Count - 1).ToString();
            }
        }

        public static void SetParameterValue(DatabaseType dbType, DbParameter p, object value)
        {
            if (value == null)
            {
                p.Value = DBNull.Value;
                return;
            }

            // Give the database type first crack at converting to DB required type
            value = dbType.MapParameterValue(value);

            var dbtypeSet = false;
            var t = value.GetType();
            var underlyingT = Nullable.GetUnderlyingType(t);
            if (t.GetTypeInfo().IsEnum || (underlyingT != null && underlyingT.GetTypeInfo().IsEnum))        // PostgreSQL .NET driver wont cast enum to int
            {
                p.Value = (int)value;
            }
            else if (t == typeof(Guid))
            {
                p.Value = value;
                p.DbType = DbType.Guid;
                p.Size = 40;
                dbtypeSet = true;
            }
            else if (t == typeof(string))
            {
                var strValue = value as string;
                if (strValue == null)
                {
                    p.Size = 0;
                    p.Value = DBNull.Value;
                }
                else
                {
                    // out of memory exception occurs if trying to save more than 4000 characters to SQL Server CE NText column. Set before attempting to set Size, or Size will always max out at 4000
                    if (strValue.Length + 1 > 4000 && p.GetType().Name == "SqlCeParameter")
                    {
                        p.GetType().GetProperty("SqlDbType").SetValue(p, SqlDbType.NText, null);
                    }

                    p.Size = Math.Max(strValue.Length + 1, 4000); // Help query plan caching by using common size
                    p.Value = value;
                }
            }
            else if (t == typeof(AnsiString))
            {
                var ansistrValue = value as AnsiString;
                if (ansistrValue?.Value == null)
                {
                    p.Size = 0;
                    p.Value = DBNull.Value;
                    p.DbType = DbType.AnsiString;
                }
                else
                {
                    // Thanks @DataChomp for pointing out the SQL Server indexing performance hit of using wrong string type on varchar
                    p.Size = Math.Max(ansistrValue.Value.Length + 1, 4000);
                    p.Value = ansistrValue.Value;
                    p.DbType = DbType.AnsiString;
                }
                dbtypeSet = true;
            }
            else if (value.GetType().Name == "SqlGeography") //SqlGeography is a CLR Type
            {
                p.GetType().GetProperty("UdtTypeName").SetValue(p, "geography", null); //geography is the equivalent SQL Server Type
                p.Value = value;
            }

            else if (value.GetType().Name == "SqlGeometry") //SqlGeometry is a CLR Type
            {
                p.GetType().GetProperty("UdtTypeName").SetValue(p, "geometry", null); //geography is the equivalent SQL Server Type
                p.Value = value;
            }
            else
            {
                p.Value = value;
            }

            if (!dbtypeSet)
            {
                var dbTypeLookup = dbType.LookupDbType(p.Value.GetTheType(), p.ParameterName);
                if (dbTypeLookup.HasValue)
                {
                    p.DbType = dbTypeLookup.Value;
                }
            }
        }
    }
}

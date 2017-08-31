using System;
using System.Collections;
using System.Collections.Generic;
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

        public static string ProcessParams(string sql, object[] args_src, List<object> args_dest)
        {
            var parameters = new Dictionary<string, string>();
            return rxParamsPrefix.Replace(sql, m =>
            {
                string item;
                if (parameters.TryGetValue(m.Value, out item))
                    return item;

                item = parameters[m.Value] = ProcessParam(ref sql, m.Value, args_src, args_dest);
                return item;
            });
        }
        
        public static string ProcessParam(ref string sql, string rawParam, object[] args_src, List<object> args_dest)
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

                    var pi = o.GetType().GetProperty(param);
                    if (pi != null)
                    {
                        arg_val = pi.GetValue(o, null);
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
                args_dest.Add(arg_val);
                return "@" + (args_dest.Count - 1).ToString();
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class ParameterHelper
    {
        // Helper to handle named parameters from object properties
        public static Regex rxParamsPrefix = new Regex(@"(?<!@)@\w+", RegexOptions.Compiled);

        public static string ProcessParams(string _sql, object[] args_src, List<object> args_dest)
        {
            return rxParamsPrefix.Replace(_sql, m =>
            {
                string param = m.Value.Substring(1);

                object arg_val;

                int paramIndex;
                if (Int32.TryParse(param, out paramIndex))
                {
                    // Numbered parameter
                    if (paramIndex < 0 || paramIndex >= args_src.Length)
                        throw new ArgumentOutOfRangeException(String.Format("Parameter '@{0}' specified but only {1} parameters supplied (in `{2}`)", paramIndex, args_src.Length, _sql));
                    arg_val = args_src[paramIndex];
                }
                else
                {
                    // Look for a property on one of the arguments with this name
                    bool found = false;
                    arg_val = null;
                    foreach (var o in args_src)
                    {
                        var pi = o.GetType().GetProperty(param);
                        if (pi != null)
                        {
                            arg_val = pi.GetValue(o, null);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        throw new ArgumentException(String.Format("Parameter '@{0}' specified but none of the passed arguments have a property with this name (in '{1}')", param, _sql));
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
                        sb.AppendFormat("select 1 /*poco_dual*/ where 1 = 0");
                    }
                    return sb.ToString();
                }
                else
                {
                    var indexOfExistingValue = args_dest.IndexOf(arg_val);
                    if (indexOfExistingValue >= 0)
                        return "@" + indexOfExistingValue;

                    args_dest.Add(arg_val);
                    return "@" + (args_dest.Count - 1).ToString();
                }
            });
        }
    }
}

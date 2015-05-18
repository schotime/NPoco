using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;

namespace NPoco
{
    internal class MultiPocoFactory
    {
        public MultiPocoFactory(IEnumerable<Delegate> dels)
        {
            Delegates = new List<Delegate>(dels);
        }
        private List<Delegate> Delegates { get; set; }
        private Delegate GetItem(int index) { return Delegates[index]; }

        /// <summary>
        /// Calls the delegate at the specified index and returns its values
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private object CallDelegate(int index, IDataReader reader)
        {
            var d = GetItem(index);
            var output = d.DynamicInvoke(reader, null);
            return output;
        }

        /// <summary>
        /// Calls the callback delegate and passes in the output of all delegates as the parameters
        /// </summary>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="callback"></param>
        /// <param name="dr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public TRet CallCallback<TRet>(Delegate callback, IDataReader dr, int count)
        {
            var args = new List<object>();
            for (var i = 0; i < count; i++)
            {
                args.Add(CallDelegate(i, dr));
            }
            return (TRet)callback.DynamicInvoke(args.ToArray());
        }

        // Automagically guess the property relationships between various POCOs and create a delegate that will set them up
        public static Delegate GetAutoMapper(Type[] types)
        {
            // Build a key
            var combiner = new HashCodeCombiner("auto-mapping");
            combiner.Each(types, (x, t) => x.AddType(t));
            var key = combiner.GetCombinedHashCode();

            return AutoMappers.Get(key, () =>
            {
                // Create a method
                var m = new DynamicMethod("poco_automapper", types[0], types, true);
                var il = m.GetILGenerator();

                for (int i = 1; i < types.Length; i++)
                {
                    bool handled = false;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        // Find the property
                        var candidates = types[j].GetProperties().Where(p => p.PropertyType == types[i]).ToList();
                        if (candidates.Count == 0)
                            continue;
                        if (candidates.Count > 1)
                            throw new InvalidOperationException(string.Format("Can't auto join {0} as {1} has more than one property of type {0}", types[i], types[j]));

                        // Generate code
                        var lblIsNull = il.DefineLabel();

                        il.Emit(OpCodes.Ldarg_S, j);       // obj
                        il.Emit(OpCodes.Ldnull);           // obj, null
                        il.Emit(OpCodes.Beq, lblIsNull);   // If obj == null then don't set nested object
                        
                        il.Emit(OpCodes.Ldarg_S, j);       // obj
                        il.Emit(OpCodes.Ldarg_S, i);       // obj, obj2
                        il.Emit(OpCodes.Callvirt, candidates[0].GetSetMethod(true)); // obj = obj2

                        il.MarkLabel(lblIsNull);
                        
                        handled = true;
                    }

                    if (!handled)
                        throw new InvalidOperationException(string.Format("Can't auto join {0}", types[i]));
                }

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ret);

                // Cache it
                var del = m.CreateDelegate(Expression.GetFuncType(types.Concat(types.Take(1)).ToArray()));
                return del;
            });
        }

         // Find the split point in a result set for two different pocos and return the poco factory for the first
        static Delegate FindSplitPoint(Database database, int typeIndex, Type typeThis, Type typeNext, IDataReader r, ref int pos)
        {
            // Last?
            if (typeNext == null)
                return database.PocoDataFactory.ForType(typeThis, true).MappingFactory.GetFactory(pos, r.FieldCount - pos, r, null);

            // Get PocoData for the two types
            PocoData pdThis = database.PocoDataFactory.ForType(typeThis, typeIndex > 0);
            PocoData pdNext = database.PocoDataFactory.ForType(typeNext, true);

            // Find split point
            int firstColumn = pos;
            var usedColumns = new Dictionary<string, bool>();
            for (; pos < r.FieldCount; pos++)
            {
                // Split if field name has already been used, or if the field doesn't exist in current poco but does in the next
                string fieldName = r.GetName(pos);
                if (usedColumns.ContainsKey(fieldName) 
                    || (!pdThis.Columns.ContainsKey(fieldName) && pdNext.Columns.ContainsKey(fieldName))
                    || (!pdThis.Columns.ContainsKey(fieldName.Replace("_", "")) && pdNext.Columns.ContainsKey(fieldName.Replace("_", "")))
                    || (!pdThis.Columns.Any(x => fieldName.Equals(x.Value.ColumnAlias, StringComparison.OrdinalIgnoreCase)) && pdNext.Columns.Any(x => fieldName.Equals(x.Value.ColumnAlias, StringComparison.OrdinalIgnoreCase))))
                {
                    return pdThis.MappingFactory.GetFactory(firstColumn, pos - firstColumn, r, null);
                }
                usedColumns.Add(fieldName, true);
            }

            throw new InvalidOperationException(string.Format("Couldn't find split point between {0} and {1}", typeThis, typeNext));
        }

        // Create a multi-poco factory
        static Func<IDataReader, Delegate, TRet> CreateMultiPocoFactory<TRet>(Database database, Type[] types, IDataReader r)
        {
            // Call each delegate
            var dels = new List<Delegate>();
            int pos = 0;
            for (int i = 0; i < types.Length; i++)
            {
                // Add to list of delegates to call
                var del = FindSplitPoint(database, i, types[i], i + 1 < types.Length ? types[i + 1] : null, r, ref pos);
                dels.Add(del);
            }

            var mpFactory = new MultiPocoFactory(dels);
            return (reader, arg3) => mpFactory.CallCallback<TRet>(arg3, reader, types.Length);
        }

        // Various cached stuff
        static Cache<string, object> MultiPocoFactories = Cache<string, object>.CreateManagedCache();
        static Cache<string, Delegate> AutoMappers = Cache<string, Delegate>.CreateManagedCache();

        // Get (or create) the multi-poco factory for a query
        public static Func<IDataReader, Delegate, TRet> GetMultiPocoFactory<TRet>(Database database, Type[] types, IDataReader r)
        {
            var combiner = new HashCodeCombiner("multi-mapping");
            combiner.AddType(typeof(TRet));
            combiner.Each(types, (x, t) => x.AddType(t));
            for (int col = 0; col < r.FieldCount; col++)
            {
                combiner.AddType(r.GetFieldType(col));
                combiner.AddCaseInsensitiveString(r.GetName(col));
            }
            var key = combiner.GetCombinedHashCode();

            return (Func<IDataReader, Delegate, TRet>)MultiPocoFactories.Get(key, () => CreateMultiPocoFactory<TRet>(database, types, r));
        }
    }

       
}

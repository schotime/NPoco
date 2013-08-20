using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NPoco
{
    public class PocoData
    {
        protected IMapper Mapper;
        static readonly EnumMapper EnumMapper = new EnumMapper();

        public static PocoData ForObject(object o, string primaryKeyName, Func<Type, PocoData> factory)
        {
            var t = o.GetType();
#if !POCO_NO_DYNAMIC
            if (t == typeof(System.Dynamic.ExpandoObject))
            {
                var pd = new PocoData();
                pd.TableInfo = new TableInfo();
                pd.Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
                pd.Columns.Add(primaryKeyName, new ExpandoColumn() { ColumnName = primaryKeyName });
                pd.TableInfo.PrimaryKey = primaryKeyName;
                pd.TableInfo.AutoIncrement = true;
                foreach (var col in ((IDictionary<string, object>)o).Keys)
                {
                    if (col != primaryKeyName)
                        pd.Columns.Add(col, new ExpandoColumn() { ColumnName = col });
                }
                return pd;
            }
            else
#endif
                return ForType(t, factory);
        }

        public static PocoData ForType(Type t, Func<Type, PocoData> factory)
        {
            return ForType(t, false, factory);
        }

        public static PocoData ForType(Type t, bool emptyNestedObjectNull, Func<Type, PocoData> factory)
        {
            
#if !POCO_NO_DYNAMIC
            if (t == typeof(System.Dynamic.ExpandoObject))
                throw new InvalidOperationException("Can't use dynamic types with this method");
#endif

            var pd = _pocoDatas.Get(t, () => factory(t));
            pd._emptyNestedObjectNull = emptyNestedObjectNull;
            return pd;
        }

        protected PocoData()
        {
        }

        public PocoData(Type t, IMapper mapper)
        {
            type = t;
            Mapper = mapper;
            TableInfo = TableInfo.FromPoco(t);

            // Call column mapper
            if (Mapper != null)
                Mapper.GetTableInfo(t, TableInfo);

            // Work out bound properties
            Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(t))
            {
                ColumnInfo ci = ColumnInfo.FromMemberInfo(mi);
                if (ci == null)
                    continue;

                var pc = new PocoColumn();
                pc.MemberInfo = mi;
                pc.ColumnName = ci.ColumnName;
                pc.ResultColumn = ci.ResultColumn;
                pc.ForceToUtc = ci.ForceToUtc;
                pc.ColumnType = ci.ColumnType;

                if (Mapper != null && !Mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;

                // Store it
                Columns.Add(pc.ColumnName, pc);
            }

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn).Select(c => c.Key).ToArray();
        }

        static bool IsIntegralType(Type t)
        {
            var tc = Type.GetTypeCode(t);
            return tc >= TypeCode.SByte && tc <= TypeCode.UInt64;
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        // Create factory function that can convert a IDataReader record into a POCO
        public Delegate GetFactory(string sql, string connString, int firstColumn, int countColumns, IDataReader r, object instance)
        {
            // Check cache
            var key = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", sql, connString, firstColumn, countColumns, instance != GetDefault(type), _emptyNestedObjectNull);
 
            Func<Delegate> createFactory = () =>
            {
                // Create the method
                var m = new DynamicMethod("poco_factory_" + _pocoFactories.Count, type, new Type[] { typeof(IDataReader), type }, true);
                var il = m.GetILGenerator();

#if !POCO_NO_DYNAMIC
                if (type == typeof(object))
                {
                    // var poco=new T()
                    il.Emit(OpCodes.Newobj, typeof(System.Dynamic.ExpandoObject).GetConstructor(Type.EmptyTypes));			// obj

                    MethodInfo fnAdd = typeof(IDictionary<string, object>).GetMethod("Add");

                    // Enumerate all fields generating a set assignment for the column
                    for (int i = firstColumn; i < firstColumn + countColumns; i++)
                    {
                        var srcType = r.GetFieldType(i);

                        il.Emit(OpCodes.Dup);						// obj, obj
                        il.Emit(OpCodes.Ldstr, r.GetName(i));		// obj, obj, fieldname

                        // Get the converter
                        Func<object, object> converter = null;
                        if (Mapper != null)
                            converter = Mapper.GetFromDbConverter((Type)null, srcType);

                        //if (ForceDateTimesToUtc && converter == null && srcType == typeof(DateTime))
                        //    converter = delegate(object src) { return new DateTime(((DateTime)src).Ticks, DateTimeKind.Utc); };

                        // Setup stack for call to converter
                        AddConverterToStack(il, converter);

                        // r[i]
                        il.Emit(OpCodes.Ldarg_0);					// obj, obj, fieldname, converter?,    rdr
                        il.Emit(OpCodes.Ldc_I4, i);					// obj, obj, fieldname, converter?,  rdr,i
                        il.Emit(OpCodes.Callvirt, fnGetValue);		// obj, obj, fieldname, converter?,  value

                        // Convert DBNull to null
                        il.Emit(OpCodes.Dup);						// obj, obj, fieldname, converter?,  value, value
                        il.Emit(OpCodes.Isinst, typeof(DBNull));	// obj, obj, fieldname, converter?,  value, (value or null)
                        var lblNotNull = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, lblNotNull);		// obj, obj, fieldname, converter?,  value
                        il.Emit(OpCodes.Pop);						// obj, obj, fieldname, converter?
                        if (converter != null)
                            il.Emit(OpCodes.Pop);					// obj, obj, fieldname, 
                        il.Emit(OpCodes.Ldnull);					// obj, obj, fieldname, null
                        if (converter != null)
                        {
                            var lblReady = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, lblReady);
                            il.MarkLabel(lblNotNull);
                            il.Emit(OpCodes.Callvirt, fnInvoke);
                            il.MarkLabel(lblReady);
                        }
                        else
                        {
                            il.MarkLabel(lblNotNull);
                        }

                        il.Emit(OpCodes.Callvirt, fnAdd);
                    }
                }
                else
#endif
                    if (type.IsValueType || type == typeof(string) || type == typeof(byte[]))
                    {
                        // Do we need to install a converter?
                        var srcType = r.GetFieldType(0);
                        var converter = GetConverter(Mapper, null, srcType, type);

                        // "if (!rdr.IsDBNull(i))"
                        il.Emit(OpCodes.Ldarg_0);										// rdr
                        il.Emit(OpCodes.Ldc_I4_0);										// rdr,0
                        il.Emit(OpCodes.Callvirt, fnIsDBNull);							// bool
                        var lblCont = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, lblCont);
                        il.Emit(OpCodes.Ldnull);										// null
                        var lblFin = il.DefineLabel();
                        il.Emit(OpCodes.Br_S, lblFin);

                        il.MarkLabel(lblCont);

                        // Setup stack for call to converter
                        AddConverterToStack(il, converter);

                        il.Emit(OpCodes.Ldarg_0);										// rdr
                        il.Emit(OpCodes.Ldc_I4_0);										// rdr,0
                        il.Emit(OpCodes.Callvirt, fnGetValue);							// value

                        // Call the converter
                        if (converter != null)
                            il.Emit(OpCodes.Callvirt, fnInvoke);

                        il.MarkLabel(lblFin);
                        il.Emit(OpCodes.Unbox_Any, type);								// value converted
                    }
                    else if (type == typeof(Dictionary<string, object>))
                    {
                        Func<IDataReader, object, Dictionary<string, object>> func = (reader, inst) =>
                        {
                            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = firstColumn; i < firstColumn + countColumns; i++)
                            {
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                var name = reader.GetName(i);
                                if (!dict.ContainsKey(name))
                                    dict.Add(name, value);
                            }
                            return dict;
                        };

                        var delegateType = typeof(Func<,,>).MakeGenericType(typeof(IDataReader), type, typeof(Dictionary<string, object>));
                        var localDel = Delegate.CreateDelegate(delegateType, func.Target, func.Method);
                        return localDel;
                    }
                    else if (type.IsArray)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, countColumns - firstColumn);
                        il.Emit(OpCodes.Newarr, type.GetElementType());

                        var valueset = typeof(Array).GetMethod("SetValue", new[] { typeof(object), typeof(int) });

                        for (int i = firstColumn; i < firstColumn + countColumns; i++)
                        {
                            // "if (!rdr.IsDBNull(i))"
                            il.Emit(OpCodes.Ldarg_0);									// arr,rdr
                            il.Emit(OpCodes.Ldc_I4, i);									// arr,rdr,i
                            il.Emit(OpCodes.Callvirt, fnIsDBNull);						// arr,bool
                            var lblNext = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, lblNext);							// arr

                            il.Emit(OpCodes.Dup);		                                    // arr,arr
                            il.Emit(OpCodes.Ldarg_0);										// arr,arr,rdr
                            il.Emit(OpCodes.Ldc_I4, i - firstColumn);						// arr,arr,rdr,i
                            il.Emit(OpCodes.Callvirt, fnGetValue);							// arr,arr,value

                            il.Emit(OpCodes.Ldc_I4, i - firstColumn);                       // arr,arr,value,i
                            il.Emit(OpCodes.Callvirt, valueset);                            // arr

                            il.MarkLabel(lblNext);                  
                        }

                        il.Emit(OpCodes.Castclass, type);
                    }
                    else
                    {
                        if (instance != null)
                            il.Emit(OpCodes.Ldarg_1);
                        else
                            // var poco=new T()
                            il.Emit(OpCodes.Newobj, type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null));

                        LocalBuilder a = il.DeclareLocal(typeof(Int32));
                        if (_emptyNestedObjectNull)
                        {
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Stloc, a);
                        }

                        // Enumerate all fields generating a set assignment for the column
                        for (int i = firstColumn; i < firstColumn + countColumns; i++)
                        {
                            // Get the PocoColumn for this db column, ignore if not known
                            PocoColumn pc;
                            if (!Columns.TryGetValue(r.GetName(i), out pc) && !Columns.TryGetValue(r.GetName(i).Replace("_", ""), out pc)
                                || (!pc.MemberInfo.IsField() && ((PropertyInfo)pc.MemberInfo).GetSetMethodOnDeclaringType() == null))
                            {
                                continue;
                            }

                            // Get the source type for this column
                            var srcType = r.GetFieldType(i);
                            var dstType = pc.MemberInfo.GetMemberInfoType();

                            // "if (!rdr.IsDBNull(i))"
                            il.Emit(OpCodes.Ldarg_0);										// poco,rdr
                            il.Emit(OpCodes.Ldc_I4, i);										// poco,rdr,i
                            il.Emit(OpCodes.Callvirt, fnIsDBNull);							// poco,bool
                            var lblNext = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, lblNext);								// poco

                            il.Emit(OpCodes.Dup);											// poco,poco

                            // Do we need to install a converter?
                            var converter = GetConverter(Mapper, pc, srcType, dstType);

                            // Fast
                            bool Handled = false;
                            if (converter == null)
                            {
                                var valuegetter = typeof(IDataRecord).GetMethod("Get" + srcType.Name, new Type[] { typeof(int) });
                                if (valuegetter != null
                                    && valuegetter.ReturnType == srcType
                                    && (valuegetter.ReturnType == dstType || valuegetter.ReturnType == Nullable.GetUnderlyingType(dstType)))
                                {
                                    il.Emit(OpCodes.Ldarg_0);										// *,rdr
                                    il.Emit(OpCodes.Ldc_I4, i);										// *,rdr,i
                                    il.Emit(OpCodes.Callvirt, valuegetter);							// *,value

                                    // Convert to Nullable
                                    if (Nullable.GetUnderlyingType(dstType) != null)
                                    {
                                        il.Emit(OpCodes.Newobj, dstType.GetConstructor(new Type[] { Nullable.GetUnderlyingType(dstType) }));
                                    }

                                    PushMemberOntoStack(il, pc); //poco
                                    Handled = true;
                                }
                            }

                            // Not so fast
                            if (!Handled)
                            {
                                // Setup stack for call to converter
                                AddConverterToStack(il, converter);

                                // "value = rdr.GetValue(i)"
                                il.Emit(OpCodes.Ldarg_0);										// *,rdr
                                il.Emit(OpCodes.Ldc_I4, i);										// *,rdr,i
                                il.Emit(OpCodes.Callvirt, fnGetValue);							// *,value

                                // Call the converter
                                if (converter != null)
                                    il.Emit(OpCodes.Callvirt, fnInvoke);

                                // Assign it
                                il.Emit(OpCodes.Unbox_Any, pc.MemberInfo.GetMemberInfoType());          // poco,poco,value

                                PushMemberOntoStack(il, pc); //poco
                            }

                            if (_emptyNestedObjectNull)
                            {
                                il.Emit(OpCodes.Ldloc, a); // poco, a
                                il.Emit(OpCodes.Ldc_I4, 1); // poco, a, 1
                                il.Emit(OpCodes.Add); // poco, a+1
                                il.Emit(OpCodes.Stloc, a); // poco
                            }
                            il.MarkLabel(lblNext);
                        }

                        var fnOnLoaded = RecurseInheritedTypes<MethodInfo>(type, (x) => x.GetMethod("OnLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null));
                        if (fnOnLoaded != null)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Callvirt, fnOnLoaded);
                        }

                        if (_emptyNestedObjectNull)
                        {
                            var lblNull = il.DefineLabel();
                            var lblElse = il.DefineLabel();

                            il.Emit(OpCodes.Ldc_I4_0); // poco, 0
                            il.Emit(OpCodes.Ldloc, a); // poco, 0, a

                            il.Emit(OpCodes.Beq, lblNull); // poco
                            il.Emit(OpCodes.Br_S, lblElse);

                            il.MarkLabel(lblNull);

                            il.Emit(OpCodes.Pop); // 
                            il.Emit(OpCodes.Ldnull); // null

                            il.MarkLabel(lblElse);
                        }
                    }

                il.Emit(OpCodes.Ret);

                // Cache it, return it
                var del = m.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type, type));
                return del;
            };

            var fac = _pocoFactories.Get(key, createFactory);
            return fac;
        }

        private static void PushMemberOntoStack(ILGenerator il, PocoColumn pc)
        {
            if (pc.MemberInfo.IsField())
                il.Emit(OpCodes.Stfld, (FieldInfo) pc.MemberInfo);
            else
                il.Emit(OpCodes.Callvirt, ((PropertyInfo) pc.MemberInfo).GetSetMethodOnDeclaringType());
        }

        private static void AddConverterToStack(ILGenerator il, Func<object, object> converter)
        {
            if (converter != null)
            {
                // Add the converter
                int converterIndex = m_Converters.Count;
                m_Converters.Add(converter);

                // Generate IL to push the converter onto the stack
                il.Emit(OpCodes.Ldsfld, fldConverters);
                il.Emit(OpCodes.Ldc_I4, converterIndex);
                il.Emit(OpCodes.Callvirt, fnListGetItem);					// Converter
            }
        }

        public static Func<object, object> GetConverter(IMapper mapper, PocoColumn pc, Type srcType, Type dstType)
        {
            Func<object, object> converter = null;

            // Get converter from the mapper
            if (mapper != null)
            {
                converter = pc != null ? mapper.GetFromDbConverter(pc.MemberInfo, srcType) : mapper.GetFromDbConverter(dstType, srcType);
                if (converter != null)
                    return converter;
            }

            // Standard DateTime->Utc mapper
            if (pc != null && pc.ForceToUtc && srcType == typeof(DateTime) && (dstType == typeof(DateTime) || dstType == typeof(DateTime?)))
            {
                converter = delegate(object src) { return new DateTime(((DateTime)src).Ticks, DateTimeKind.Utc); };
                return converter;
            }

            // Forced type conversion including integral types -> enum
            var underlyingType = _underlyingTypes.Get(dstType, () => Nullable.GetUnderlyingType(dstType));
            if (dstType.IsEnum || (underlyingType != null && underlyingType.IsEnum))
            {
                if (srcType == typeof(string))
                {
                    converter = src => EnumMapper.EnumFromString((underlyingType ?? dstType), (string)src);
                    return converter;
                }

                if (IsIntegralType(srcType))
                {
                    converter = src => Enum.ToObject((underlyingType ?? dstType), src);
                    return converter;
                }
            }
            else if (!dstType.IsAssignableFrom(srcType))
            {
                converter = src => Convert.ChangeType(src, (underlyingType ?? dstType), null);
            }
            return converter;
        }

        static T RecurseInheritedTypes<T>(Type t, Func<Type, T> cb)
        {
            while (t != null)
            {
                T info = cb(t);
                if (info != null)
                    return info;
                t = t.BaseType;
            }
            return default(T);
        }

        static Cache<Type, Type> _underlyingTypes = new Cache<Type, Type>();
        static Cache<Type, PocoData> _pocoDatas = new Cache<Type, PocoData>();
        static List<Func<object, object>> m_Converters = new List<Func<object, object>>();
        static MethodInfo fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        static MethodInfo fnIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        static FieldInfo fldConverters = typeof(PocoData).GetField("m_Converters", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        static MethodInfo fnListGetItem = typeof(List<Func<object, object>>).GetProperty("Item").GetGetMethod();
        static MethodInfo fnInvoke = typeof(Func<object, object>).GetMethod("Invoke");
        protected Type type;
        private bool _emptyNestedObjectNull;
        public string[] QueryColumns { get; protected set; }
        public TableInfo TableInfo { get; protected set; }
        public Dictionary<string, PocoColumn> Columns { get; protected set; }
        Cache<string, Delegate> _pocoFactories = new Cache<string, Delegate>();
    }
}

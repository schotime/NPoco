using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco
{
    public class MappingFactory
    {
        private PocoData _pocoData;
        static readonly EnumMapper EnumMapper = new EnumMapper();
        static List<Func<object, object>> m_Converters = new List<Func<object, object>>();
        static MethodInfo fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        static MethodInfo fnIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        static FieldInfo fldConverters = typeof(MappingFactory).GetField("m_Converters", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        static MethodInfo fnListGetItem = typeof(List<Func<object, object>>).GetProperty("Item").GetGetMethod();
        static MethodInfo fnInvoke = typeof(Func<object, object>).GetMethod("Invoke");
        static Cache<Type, Type> _underlyingTypes = Cache<Type, Type>.CreateStaticCache();

        //This will use a managed cache with items that expire
        Cache<string, Delegate> _pocoFactories = Cache<string, Delegate>.CreateManagedCache();

        public MappingFactory(PocoData pocoData)
        {
            _pocoData = pocoData;
        }

        public Delegate GetFactory(int firstColumn, int countColumns, IDataReader r, object instance)
        {
            //Create a hashed key, we don't want to store so much string data in memory
            var combiner = new HashCodeCombiner("mapping");
            combiner.AddType(_pocoData.type);
            combiner.AddInt(firstColumn);
            combiner.AddInt(countColumns);
            for (int col = 0; col < r.FieldCount; col++)
            {
                combiner.AddType(r.GetFieldType(col));
                combiner.AddCaseInsensitiveString(r.GetName(col));
            }
            combiner.AddBool(instance != GetDefault(_pocoData.type));
            combiner.AddBool(_pocoData.EmptyNestedObjectNull);

            var key = combiner.GetCombinedHashCode();
 
            Func<Delegate> createFactory = () =>
            {
                // Create the method
                var m = new DynamicMethod("poco_factory_" + _pocoFactories.Count, typeof(object), new Type[] { typeof(IDataReader), typeof(object) }, true);
                var il = m.GetILGenerator();

#if !POCO_NO_DYNAMIC
                if (_pocoData.type == typeof(object))
                {
                    // var poco=new T()
                    il.Emit(OpCodes.Newobj, typeof(PocoExpando).GetConstructor(Type.EmptyTypes));			// obj

                    MethodInfo fnAdd = typeof(IDictionary<string, object>).GetMethod("Add");

                    // Enumerate all fields generating a set assignment for the column
                    for (int i = firstColumn; i < firstColumn + countColumns; i++)
                    {
                        var srcType = r.GetFieldType(i);

                        il.Emit(OpCodes.Dup);						// obj, obj
                        il.Emit(OpCodes.Ldstr, r.GetName(i));		// obj, obj, fieldname

                        // Get the converter
                        Func<object, object> converter = null;
                        if (_pocoData.Mapper != null)
                            converter = _pocoData.Mapper.GetFromDbConverter((Type)null, srcType);

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
                    if (_pocoData.type.IsValueType || _pocoData.type == typeof(string) || _pocoData.type == typeof(byte[]))
                    {
                        // Do we need to install a converter?
                        var srcType = r.GetFieldType(0);
                        var converter = GetConverter(_pocoData.Mapper, null, srcType, _pocoData.type);

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
                        //il.Emit(OpCodes.Unbox_Any, _pocoData.type);								// value converted
                    }
                    else if (_pocoData.type == typeof(Dictionary<string, object>))
                    {
                        Func<IDataReader, object, object> func = (reader, inst) =>
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

                        var delegateType = typeof(Func<,,>).MakeGenericType(typeof(IDataReader), typeof(object), typeof(object));
                        var localDel = Delegate.CreateDelegate(delegateType, func.Target, func.Method);
                        return localDel;
                    }
                    else if (_pocoData.type.IsArray)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, countColumns - firstColumn);
                        il.Emit(OpCodes.Newarr, _pocoData.type.GetElementType());

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

                        il.Emit(OpCodes.Castclass, _pocoData.type);
                    }
                    else
                    {
                        if (instance != null)
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Castclass, _pocoData.type);
                        }
                        else
                        {
                            var constructorInfo = _pocoData.type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
                            if (constructorInfo == null)
                                throw new Exception(string.Format("Poco '{0}' has no parameterless constructor", _pocoData.type.FullName));

                            // var poco=new T()
                            il.Emit(OpCodes.Newobj, constructorInfo);
                        }

                        LocalBuilder a = il.DeclareLocal(typeof(Int32));
                        if (_pocoData.EmptyNestedObjectNull)
                        {
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Stloc, a);
                        }

                        // Enumerate all fields generating a set assignment for the column
                        for (int i = firstColumn; i < firstColumn + countColumns; i++)
                        {
                            // Get the PocoColumn for this db column, ignore if not known
                            PocoColumn pc;
                            if (!TryGetColumnByName(_pocoData.Columns, r.GetName(i), out pc)
                                || (!pc.MemberInfo.IsField() && ((PropertyInfo)pc.MemberInfo).GetSetMethodOnDeclaringType() == null))
                            {
                                var pcAlias = _pocoData.Columns.Values.SingleOrDefault(x => x.AutoAlias == r.GetName(i))
                                    ?? _pocoData.Columns.Values.SingleOrDefault(x => string.Equals(x.ColumnAlias, r.GetName(i), StringComparison.OrdinalIgnoreCase));
                                
                                if (pcAlias != null)
                                {
                                    pc = pcAlias;
                                }
                                else
                                {
                                    continue;
                                }
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
                            var converter = GetConverter(_pocoData.Mapper, pc, srcType, dstType);

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

                            if (_pocoData.EmptyNestedObjectNull)
                            {
                                il.Emit(OpCodes.Ldloc, a); // poco, a
                                il.Emit(OpCodes.Ldc_I4, 1); // poco, a, 1
                                il.Emit(OpCodes.Add); // poco, a+1
                                il.Emit(OpCodes.Stloc, a); // poco
                            }
                            il.MarkLabel(lblNext);
                        }

                        var fnOnLoaded = RecurseInheritedTypes<MethodInfo>(_pocoData.type, (x) => x.GetMethod("OnLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null));
                        if (fnOnLoaded != null)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Callvirt, (MethodInfo) fnOnLoaded);
                        }

                        if (_pocoData.EmptyNestedObjectNull)
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
                var del = m.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), typeof(object), typeof(object)));
                return del;
            };

            var fac = _pocoFactories.Get(key, createFactory);
            return fac;
        }


        private static void PushMemberOntoStack(ILGenerator il, PocoColumn pc)
        {
            if (pc.MemberInfo.IsField())
                il.Emit(OpCodes.Stfld, (FieldInfo)pc.MemberInfo);
            else
                il.Emit(OpCodes.Callvirt, ((PropertyInfo)pc.MemberInfo).GetSetMethodOnDeclaringType());
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

        public static bool TryGetColumnByName(Dictionary<string, PocoColumn> columns, string name, out PocoColumn pc)
        {
            // Try to get the column by name directly (works when the poco property name matches the DB column name).
            var found = (columns.TryGetValue(name, out pc) || columns.TryGetValue(name.Replace("_", ""), out pc));
            if (!found)
            {
                // Try to get the column by the poco member name (the poco property name is different from the DB column name).
                pc = columns.Values.Where(c => c.MemberInfo.Name == name).FirstOrDefault();
                found = (pc != null);
            }
            return found;
        }
    }
}

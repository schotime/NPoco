using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco.FastJSON
{
	sealed class Reflection
	{
		const BindingFlags __ReflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		// Singleton pattern 4 from : http://csharpindepth.com/articles/general/singleton.aspx
		static readonly Reflection instance = new Reflection();
		static readonly SafeDictionary<Type, JsonDataType> _jsonTypeCache = InitBuiltInTypes ();
		//static readonly SafeDictionary<Type, byte> _pocoCache = InitPocoCache ();
		// TODO: Commented for invisible benefit
		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		//static Reflection () {
		//}
		Reflection () {
		}
		public static Reflection Instance { get { return instance; } }

		readonly SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		#region Built-in Deserializable Types
		static SafeDictionary<Type, JsonDataType> InitBuiltInTypes () {
			var d = new Dictionary<Type, JsonDataType> {
				{ typeof(object), JsonDataType.Object },
				{ typeof(int), JsonDataType.Int },
				{ typeof(long), JsonDataType.Long },
				{ typeof(float), JsonDataType.Single },
				{ typeof(double), JsonDataType.Double },
				{ typeof(bool), JsonDataType.Bool },
				{ typeof(string), JsonDataType.String },
				{ typeof(DateTime), JsonDataType.DateTime },
				{ typeof(Guid), JsonDataType.Guid },
				{ typeof(TimeSpan), JsonDataType.TimeSpan },
				{ typeof(StringDictionary), JsonDataType.StringDictionary },
				{ typeof(NameValueCollection), JsonDataType.NameValue },
#if !SILVERLIGHT
				{ typeof(Hashtable), JsonDataType.Hashtable },
				{ typeof(DataSet), JsonDataType.DataSet },
				{ typeof(DataTable), JsonDataType.DataTable },
#endif
				{ typeof(byte[]), JsonDataType.ByteArray },

				{ typeof(byte), JsonDataType.Primitive },
				{ typeof(sbyte), JsonDataType.Primitive },
				{ typeof(char), JsonDataType.Primitive },
				{ typeof(short), JsonDataType.Primitive },
				{ typeof(ushort), JsonDataType.Primitive },
				{ typeof(uint), JsonDataType.Primitive },
				{ typeof(ulong), JsonDataType.Primitive },
				{ typeof(decimal), JsonDataType.Primitive }
			};
			return new SafeDictionary<Type, JsonDataType> (d);
		}
		//static SafeDictionary<Type, byte> InitPocoCache () {
		//	var d = new Dictionary<Type, byte> {
		//		{ typeof(int), 0 },
		//		{ typeof(long), 0 },
		//		{ typeof(float), 0 },
		//		{ typeof(double), 0 },
		//		{ typeof(bool), 0 },
		//		{ typeof(byte), 0 },
		//		{ typeof(sbyte), 0 },
		//		{ typeof(char), 0 },
		//		{ typeof(short), 0 },
		//		{ typeof(ushort), 0 },
		//		{ typeof(uint), 0 },
		//		{ typeof(ulong), 0 },
		//		{ typeof(decimal), 0 }
		//	};
		//	return new SafeDictionary<Type, byte> (d);
		//}
		internal static JsonDataType GetJsonDataType (Type type) {
			JsonDataType t;
			if (_jsonTypeCache.TryGetValue (type, out t)) {
				return t;
			}
			if (type.IsGenericType) {
				var g = type.GetGenericTypeDefinition ();
				if (typeof(Nullable<>).Equals (g)) {
					var it = type.GetGenericArguments ()[0];
					if (_jsonTypeCache.TryGetValue (it, out t) == false) {
						t = GetJsonDataType (it);
					}
					_jsonTypeCache.Add (type, t);
					return t;
				}
			}
			t = DetermineExtraDataType (type);
			_jsonTypeCache.Add (type, t);
			return t;
		}

		static JsonDataType DetermineExtraDataType (Type type) {
			if (type.IsEnum) {
				return JsonDataType.Enum;
			}
			if (type.IsArray) {
				return type.GetArrayRank () == 1 ? JsonDataType.Array : JsonDataType.MultiDimensionalArray;
			}
			if (type.IsGenericType) {
				foreach (var item in type.GetInterfaces ()) {
					if (item.IsGenericType == false) {
						continue;
					}
					var g = item.GetGenericTypeDefinition ();
					if (typeof(IEnumerable<>).Equals (g)) {
						return JsonDataType.List;
					}
					if (typeof(IDictionary<,>).Equals (g)) {
						var gt = item.GetGenericArguments ();
						if (gt.Length > 0 && typeof (string).Equals (gt[0])) {
							return JsonDataType.StringKeyDictionary;
						}
						return JsonDataType.Dictionary;
					}
				}
			}
			if (typeof (IDictionary).IsAssignableFrom (type)) {
				return JsonDataType.Dictionary;
			}
			if (typeof (DataSet).IsAssignableFrom (type)) {
				return JsonDataType.DataSet;
			}
			if (typeof (DataTable).IsAssignableFrom (type)) {
				return JsonDataType.DataTable;
			}
			if (typeof (NameValueCollection).IsAssignableFrom (type)) {
				return JsonDataType.NameValue;
			}
			if (typeof (IEnumerable).IsAssignableFrom (type)) {
				return JsonDataType.List;
			}
			return JsonDataType.Undefined;
		}
		#endregion
		#region [   MEMBER GET SET   ]
		internal static CreateObject CreateConstructorMethod (Type objtype, bool skipVisibility) {
			CreateObject c;
			var n = objtype.Name + ".ctor";
			if (objtype.IsClass) {
				var dynMethod = skipVisibility
					? new DynamicMethod (n, objtype, null, objtype, true)
					: new DynamicMethod (n, objtype, Type.EmptyTypes);
				var ilGen = dynMethod.GetILGenerator ();
				var ct = objtype.GetConstructor (Type.EmptyTypes)
					?? objtype.GetConstructor (BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
				if (ct == null) {
					return null;
				}
				ilGen.Emit (OpCodes.Newobj, ct);
				ilGen.Emit (OpCodes.Ret);
				c = (CreateObject)dynMethod.CreateDelegate (typeof(CreateObject));
			}
			else {// structs
				var dynMethod = skipVisibility
					? new DynamicMethod (n, typeof(object), null, objtype, true)
					: new DynamicMethod (n, typeof(object), null, objtype);
				var ilGen = dynMethod.GetILGenerator ();
				var lv = ilGen.DeclareLocal (objtype);
				ilGen.Emit (OpCodes.Ldloca_S, lv);
				ilGen.Emit (OpCodes.Initobj, objtype);
				ilGen.Emit (OpCodes.Ldloc_0);
				ilGen.Emit (OpCodes.Box, objtype);
				ilGen.Emit (OpCodes.Ret);
				c = (CreateObject)dynMethod.CreateDelegate (typeof(CreateObject));
			}
			return c;
		}

		internal static MemberCache[] GetMembers (Type type) {
			var pl = type.GetProperties (__ReflectionFlags);
			var fl = type.GetFields (__ReflectionFlags);
			var c = new List<MemberCache> (pl.Length + fl.Length);
			foreach (var m in pl) {
				if (m.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				c.Add (new MemberCache (m));
			}
			foreach (var m in fl) {
				if (m.IsLiteral == false) {
					c.Add (new MemberCache (m));
				}
			}
			c = c.FindAll (m => { return (m.HasPublicGetter || m.HasPublicSetter) || AttributeHelper.HasAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute> (m.MemberInfo, true) == false; });
			var r = new MemberCache[c.Count];
			c.CopyTo (r, 0);
			return r;
		}

		internal static JsonMemberGetter[] GetGetters (Type type, MemberCache[] members, IReflectionController controller) {
			var r = new JsonMemberGetter[members.Length];
			for (int i = r.Length - 1; i >= 0; i--) {
				var m = members[i];
				var g = r[i] = new JsonMemberGetter (m);
				if (controller == null) {
					continue;
				}
				var mi = m.MemberInfo;
				g.Serializable = Constants.ToTriState (controller.IsMemberSerializable (mi, m));
				g.Converter = controller.GetMemberConverter (mi);
				g.ItemConverter = controller.GetMemberItemConverter (mi);
				var dv = controller.GetNonSerializedValues (mi);
				if (dv != null) {
					var v = new List<object> ();
					foreach (var item in dv) {
						v.Add (item);
					}
					g.NonSerializedValues = v.ToArray ();
				}
				g.HasNonSerializedValue = g.NonSerializedValues != null;
				var tn = controller.GetSerializedNames (mi);
				if (tn != null) {
					if (String.IsNullOrEmpty (tn.DefaultName) == false && tn.DefaultName != g.SerializedName) {
						g.SpecificName = true;
					}
					g.SerializedName = tn.DefaultName ?? g.SerializedName;
					if (tn.Count > 0) {
						g.TypedNames = new Dictionary<Type, string> (tn);
						g.SpecificName = true;
					}
				}
			}
			return r;
		}

		internal static GenericGetter CreateGetField (FieldInfo fieldInfo) {
			var type = fieldInfo.DeclaringType;
			var dynamicGet = new DynamicMethod (fieldInfo.Name, typeof(object), new Type[] { typeof(object) }, type, true);

			var il = dynamicGet.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Box, fieldInfo.FieldType);
			}
			else {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Box, fieldInfo.FieldType);
			}

			il.Emit (OpCodes.Ret);

			return (GenericGetter)dynamicGet.CreateDelegate (typeof(GenericGetter));
		}

		internal static GenericGetter CreateGetProperty (PropertyInfo propertyInfo) {
			var getMethod = propertyInfo.GetGetMethod (true);
			if (getMethod == null)
				return null;

			var type = propertyInfo.DeclaringType;
			var pt = propertyInfo.PropertyType;
			var getter = new DynamicMethod (getMethod.Name, typeof(object), new Type[] { typeof(object) }, type, true);

			var il = getter.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.EmitCall (OpCodes.Call, getMethod, null);
				if (pt.IsValueType)
					il.Emit (OpCodes.Box, pt);
			}
			else {
				if (getMethod.IsStatic) {
					il.EmitCall (OpCodes.Call, getMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, type);
					il.EmitCall (OpCodes.Callvirt, getMethod, null);
				}
				if (pt.IsValueType)
					il.Emit (OpCodes.Box, pt);
			}

			il.Emit (OpCodes.Ret);

			return (GenericGetter)getter.CreateDelegate (typeof(GenericGetter));
		}

		internal static GenericSetter CreateSetField (FieldInfo fieldInfo) {
			var type = fieldInfo.DeclaringType;
			var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			var dynamicSet = new DynamicMethod (fieldInfo.Name, typeof(object), arguments, type, true);

			var il = dynamicSet.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsClass)
					il.Emit (OpCodes.Castclass, fieldInfo.FieldType);
				else
					il.Emit (OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit (OpCodes.Stfld, fieldInfo);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Box, type);
				il.Emit (OpCodes.Ret);
			}
			else {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit (OpCodes.Stfld, fieldInfo);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ret);
			}
			return (GenericSetter)dynamicSet.CreateDelegate (typeof(GenericSetter));
		}

		internal static GenericSetter CreateSetProperty (PropertyInfo propertyInfo) {
			var setMethod = propertyInfo.GetSetMethod (true);
			if (setMethod == null)
				return null;

			var type = propertyInfo.DeclaringType;
			var pt = propertyInfo.PropertyType;
			var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			var setter = new DynamicMethod (setMethod.Name, typeof(object), arguments, true);
			var il = setter.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldarg_1);
				if (pt.IsClass)
					il.Emit (OpCodes.Castclass, pt);
				else
					il.Emit (OpCodes.Unbox_Any, pt);
				il.EmitCall (OpCodes.Call, setMethod, null);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Box, type);
			}
			else {
				if (setMethod.IsStatic) {
					il.Emit (OpCodes.Ldarg_1);
					if (pt.IsClass)
						il.Emit (OpCodes.Castclass, pt);
					else
						il.Emit (OpCodes.Unbox_Any, pt);
					il.EmitCall (OpCodes.Call, setMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, type);
					il.Emit (OpCodes.Ldarg_1);
					if (pt.IsClass)
						il.Emit (OpCodes.Castclass, pt);
					else
						il.Emit (OpCodes.Unbox_Any, pt);
					il.EmitCall (OpCodes.Callvirt, setMethod, null);
				}
				il.Emit (OpCodes.Ldarg_0);
			}

			il.Emit (OpCodes.Ret);

			return (GenericSetter)setter.CreateDelegate (typeof(GenericSetter));
		}

		// TODO: Support method that takes more than 1 arguments
		/// <summary>
		/// Creates a wrapper delegate for the given method.
		/// The delegate should have a similar signature as the <paramref name="method"/>, except that an argument in inserted before the method arguments.
		/// </summary>
		/// <typeparam name="T">A delegate definition. The first argument of the delegate will be used to invoke the method.</typeparam>
		/// <param name="method">The method to be converted to the delegate.</param>
		/// <returns>The wrapper delegate to invoke the method.</returns>
		/// <example><code><![CDATA[delegate void MyAddMethod (IEnumerable target, object value);
		/// Reflection.CreateWrapperMethod<MyAddMethod> (typeof(List<string>).GetMethod("Add"));]]></code></example>
		internal static T CreateWrapperMethod<T> (MethodInfo method) where T : class {
			if (method == null)
				return null;

			var type = method.ReflectedType;
			var mp = method.GetParameters ();
			var mr = method.ReturnType;

			var d = typeof (T).GetMethod ("Invoke");
			var dp = d.GetParameters ();
			var dr = d.ReturnType;
			var da = new Type[dp.Length];
			for (int i = da.Length - 1; i >= 0; i--) {
				da[i] = dp[i].ParameterType;
			}

			var m = new DynamicMethod (method.Name, dr, da, true);
			var il = m.GetILGenerator ();

			var lv = il.DeclareLocal (type);
			if (!type.IsClass) // structs
			{
				if (method.IsStatic == false) {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Unbox_Any, type);
					il.Emit (OpCodes.Stloc_0);
					il.Emit (OpCodes.Ldloca_S, lv);
				}
				for (int i = 0; i < mp.Length;) {
					LoadArgument (mp, ++i, il);
				}
				il.EmitCall (method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
			}
			else {
				if (method.IsStatic == false) {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, type); 
				}
				for (int i = 0; i < mp.Length;) {
					LoadArgument (mp, ++i, il);
				}
				il.EmitCall (method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
			}

			var dv = dr.Equals (typeof (void));
			var mv = mr.Equals (typeof (void));
			if (mv == false && dv == false) {
				var rv = il.DeclareLocal (mr);
			}
			var dro = dr.Equals (typeof (object));
			var mro = dr.Equals (typeof (object));
			// TODO: correctly handles the return value
			if (dv) {
				if (mv == false) {
					il.Emit (OpCodes.Pop);
				}
			}
			else if (dro) {
				if (mv) {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Stloc_1);
					il.Emit (OpCodes.Ldloc_1);
				}
				else if (mro) {
					if (mr.IsValueType) {
						il.Emit (OpCodes.Box, mr);
					}
					il.Emit (OpCodes.Stloc_1);
					il.Emit (OpCodes.Ldloc_1);
				}
			}
			else {
				il.Emit (OpCodes.Stloc_1);
				il.Emit (OpCodes.Ldloc_1);
			}
			il.Emit (OpCodes.Ret);

			return m.CreateDelegate (typeof (T)) as T;
		}

		static void LoadArgument (ParameterInfo[] parameters, int index, ILGenerator il) {
			switch (index) {
				case 0: throw new ArgumentException ("index should not be zero");
				case 1: il.Emit (OpCodes.Ldarg_1); break;
				case 2: il.Emit (OpCodes.Ldarg_2); break;
				case 3: il.Emit (OpCodes.Ldarg_3); break;
				default: il.Emit (OpCodes.Ldarg_S); break;
			}
			var pt = parameters[--index].ParameterType;
			if (pt.IsValueType)
				il.Emit (OpCodes.Unbox_Any, pt);
			else if (typeof(object).Equals (pt) == false)
				il.Emit (OpCodes.Castclass, pt);
		}

		/// <summary>
		/// Finds a public instance method with the same name as <paramref name="methodName"/> and having arguments match the <paramref name="argumentTypes"/> in the given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The type which contains the method.</param>
		/// <param name="methodName">The method to match.</param>
		/// <param name="argumentTypes">The types of method arguments. Null value in the array means the corresponding argument can be any type.</param>
		/// <returns>The method matches the name and argument types.</returns>
		internal static MethodInfo FindMethod (Type type, string methodName, Type[] argumentTypes) {
			int ac = argumentTypes != null ? argumentTypes.Length : -1;
			foreach (var method in type.GetMethods (__ReflectionFlags)) {
				if (method.Name != methodName || method.IsPublic == false || method.IsStatic) {
					continue;
				}
				if (ac == -1) {
					return method;
				}
				var p = method.GetParameters ();
				if (p.Length != ac) {
					continue;
				}
				bool m = true;
				for (int i = ac - 1; i >= 0; i--) {
					if (argumentTypes[i] != null && p[i].ParameterType.Equals (argumentTypes[i]) == false) {
						m = false;
						break;
					}
				}
				if (m) {
					return method;
				}
			}
			if (type.IsInterface) {
				foreach (var item in type.GetInterfaces ()) {
					var m = FindMethod (item, methodName, argumentTypes);
					if (m != null) {
						return m;
					}
				}
			}
			return null;
		}

		#endregion

		internal Type GetTypeFromCache(string typename)
		{
			Type val = null;
			if (_typecache.TryGetValue(typename, out val))
				return val;
			else
			{
				Type t = Type.GetType(typename);
				//if (t == null) // RaptorDB : loading runtime assemblies
				//{
				//    t = Type.GetType(typename, (name) => {
				//        return AppDomain.CurrentDomain.GetAssemblies().Where(z => z.FullName == name.FullName).FirstOrDefault();
				//    }, null, true);
				//}
				_typecache.Add(typename, t);
				return t;
			}
		}

	}
}

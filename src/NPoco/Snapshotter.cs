﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco
{
    // Implementation from @SamSaffron
    // http://code.google.com/p/stack-exchange-data-explorer/source/browse/App/StackExchange.DataExplorer/Dapper/Snapshotter.cs
    public static class Snapshotter
    {
        public static Snapshot<T> StartSnapshot<T>(this IDatabase d, T obj)
        {
            return new Snapshot<T>(d, obj);
        }
    }

    public class Snapshot<T>
    {
        static Func<T, T> cloner;
        static Func<T, T, List<Change>> differ;
        T memberWiseClone;
        T trackedObject;
        PocoData pocoData;

        public Snapshot(IDatabase d, T original)
        {
            memberWiseClone = Clone(original);
            trackedObject = original;
            pocoData = d.PocoDataFactory.ForType(typeof(T));
        }

        public class Change
        {
            public string Name { get; set; }
            public string ColumnName { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }

        public void OverrideTrackedObject(T obj)
        {
            trackedObject = obj;
        }

        public List<Change> Changes()
        {
            var changes = Diff(memberWiseClone, trackedObject);
            foreach (var c in changes)
            {
                var typeData = pocoData.Columns.Values.SingleOrDefault(x => x.MemberInfo.Name == c.Name);
                c.ColumnName = typeData != null ? typeData.ColumnName : c.Name;
            }

            return changes;
        }

        public List<string> UpdatedColumns()
        {
            return Changes().Select(x => x.ColumnName).ToList();
        }

        private static T Clone(T myObject)
        {
            cloner = cloner ?? GenerateCloner();
            return cloner(myObject);
        }

        private static List<Change> Diff(T original, T current)
        {
            differ = differ ?? GenerateDiffer();
            return differ(original, current);
        }

        static List<PropertyInfo> RelevantProperties()
        {
            return typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p =>
                    p.GetSetMethod() != null &&
                    p.GetGetMethod() != null &&
                    (p.PropertyType.IsValueType ||
                        p.PropertyType == typeof(string) ||
                        (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    ).ToList();
        }


        private static bool AreEqual<U>(U first, U second)
        {
            if (first == null && second == null) return true;
            if (first == null && second != null) return false;
            return first.Equals(second);
        }

        private static Func<T, T, List<Change>> GenerateDiffer()
        {

            var dm = new DynamicMethod("DoDiff", typeof(List<Change>), new Type[] { typeof(T), typeof(T) }, true);

            var il = dm.GetILGenerator();
            // change list
            il.DeclareLocal(typeof(List<Change>));
            il.DeclareLocal(typeof(Change));
            il.DeclareLocal(typeof(object)); // boxed change
            il.DeclareLocal(typeof(object)); // orig val

            il.Emit(OpCodes.Newobj, typeof(List<Change>).GetConstructor(Type.EmptyTypes));
            // [list]
            il.Emit(OpCodes.Stloc_0);

            foreach (var prop in RelevantProperties())
            {
                // []
                il.Emit(OpCodes.Ldarg_0);
                // [original]
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                // [original prop val]

                il.Emit(OpCodes.Dup);
                // [original prop val, original prop val]

                if (prop.PropertyType != typeof(string))
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                    // [original prop val, original prop val boxed]
                }

                il.Emit(OpCodes.Stloc_3);
                // [original prop val]

                il.Emit(OpCodes.Ldarg_1);
                // [original prop val, current]

                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                // [original prop val, current prop val]

                il.Emit(OpCodes.Dup);
                // [original prop val, current prop val, current prop val]

                if (prop.PropertyType != typeof(string))
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                    // [original prop val, current prop val, current prop val boxed]
                }

                il.Emit(OpCodes.Stloc_2);
                // [original prop val, current prop val]

                il.EmitCall(OpCodes.Call, typeof(Snapshot<T>).GetMethod("AreEqual", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(new Type[] { prop.PropertyType }), null);
                // [result] 

                Label skip = il.DefineLabel();
                il.Emit(OpCodes.Brtrue_S, skip);
                // []

                il.Emit(OpCodes.Newobj, typeof(Change).GetConstructor(Type.EmptyTypes));
                // [change]
                il.Emit(OpCodes.Dup);
                // [change,change]

                il.Emit(OpCodes.Stloc_1);
                // [change]

                il.Emit(OpCodes.Ldstr, prop.Name);
                // [change, name]
                il.Emit(OpCodes.Callvirt, typeof(Change).GetMethod("set_Name"));
                // []

                il.Emit(OpCodes.Ldloc_1);
                // [change]

                il.Emit(OpCodes.Ldloc_3);
                // [change, original prop val boxed]

                il.Emit(OpCodes.Callvirt, typeof(Change).GetMethod("set_OldValue"));
                // []

                il.Emit(OpCodes.Ldloc_1);
                // [change]

                il.Emit(OpCodes.Ldloc_2);
                // [change, boxed]

                il.Emit(OpCodes.Callvirt, typeof(Change).GetMethod("set_NewValue"));
                // []

                il.Emit(OpCodes.Ldloc_0);
                // [change list]
                il.Emit(OpCodes.Ldloc_1);
                // [change list, change]
                il.Emit(OpCodes.Callvirt, typeof(List<Change>).GetMethod("Add"));
                // []

                il.MarkLabel(skip);
            }

            il.Emit(OpCodes.Ldloc_0);
            // [change list]
            il.Emit(OpCodes.Ret);

            return (Func<T, T, List<Change>>)dm.CreateDelegate(typeof(Func<T, T, List<Change>>));
        }


        // adapted from http://stackoverflow.com/a/966466/17174
        private static Func<T, T> GenerateCloner()
        {
            Delegate myExec = null;
            var dm = new DynamicMethod("DoClone", typeof(T), new Type[] { typeof(T) }, true);
            var ctor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);;

            var il = dm.GetILGenerator();

            il.DeclareLocal(typeof(T));

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc_0);

            foreach (var prop in RelevantProperties())
            {
                il.Emit(OpCodes.Ldloc_0);
                // [clone]
                il.Emit(OpCodes.Ldarg_0);
                // [clone, source]
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                // [clone, source val]
                il.Emit(OpCodes.Callvirt, prop.GetSetMethod());
                // []
            }

            // Load new constructed obj on eval stack -> 1 item on stack
            il.Emit(OpCodes.Ldloc_0);
            // Return constructed object.   --> 0 items on stack
            il.Emit(OpCodes.Ret);

            myExec = dm.CreateDelegate(typeof(Func<T, T>));

            return (Func<T, T>)myExec;
        }
    }
}

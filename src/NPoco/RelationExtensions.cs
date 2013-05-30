using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public static class RelationExtensions
    {
        public static List<T> FetchOneToMany<T, T1>(this IDatabase db, Func<T, object> key, Sql Sql)
        {
            var relator = new Relator();
            return db.Fetch<T, T1, T>((a, b) => relator.OneToMany(a, b, key), Sql);
        }

        public static List<T> FetchOneToMany<T, T1>(this IDatabase db, Func<T, object> key, Func<T1, object> manyKey, Sql Sql)
        {
            var relator = new Relator();
            return db.Fetch<T, T1, T>((a, b) => relator.OneToMany(a, b, key, manyKey), Sql);
        }

        public static List<T> FetchOneToMany<T, T1>(this IDatabase db, Func<T, object> key, string sql, params object[] args)
        {
            return db.FetchOneToMany<T, T1>(key, new Sql(sql, args));
        }
        
        public static List<T> FetchOneToMany<T, T1>(this IDatabase db, Func<T, object> key, Func<T1, object> manyKey, string sql, params object[] args)
        {
            return db.FetchOneToMany<T, T1>(key, manyKey, new Sql(sql, args));
        }     
    }

    public class Relator
    {
        private PropertyInfo property1;
        
        public T OneToMany<T, TSub>(T main, TSub sub, Func<T, object> idFunc)
        {
            return OneToMany(main, sub, idFunc, null);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        private object onetomanycurrent;
        public T OneToMany<T, TSub>(T main, TSub sub, Func<T, object> idFunc, Func<TSub, object> subIdFunc)
        {
            if (main == null)
                return (T)onetomanycurrent;

            if (property1 == null)
            {
                property1 = typeof(T).GetProperties().Where(x => typeof(ICollection<TSub>).IsAssignableFrom(x.PropertyType)).FirstOrDefault();
                if (property1 == null)
                    ThrowPropertyNotFoundException<T, ICollection<TSub>>();
            }

            if (onetomanycurrent != null && idFunc((T)onetomanycurrent).Equals(idFunc(main)))
            {
                ((ICollection<TSub>)property1.GetValue((T)onetomanycurrent, null)).Add(sub);
                return default(T);
            }

            var prev = (T)onetomanycurrent;
            onetomanycurrent = main; 

            bool nullMany = sub == null || (subIdFunc != null && subIdFunc(sub).Equals(GetDefault(subIdFunc(sub).GetType())));
            property1.SetValue((T) onetomanycurrent, nullMany ? new List<TSub>() : new List<TSub> {sub}, null);

            return prev;
        }

        private static void ThrowPropertyNotFoundException<T, TSub1>()
        {
            throw new Exception(string.Format("No Property of type {0} found on object of type: {1}", typeof(TSub1).Name, typeof(T).Name));
        }
    }
}

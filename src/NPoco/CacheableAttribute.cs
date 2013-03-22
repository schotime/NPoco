using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CacheableAttribute : Attribute
    {
        public string Key { get; set; }
        public TimeSpan TTL { get; set; }

        public CacheableAttribute()
        {
            Key = Guid.NewGuid().ToString();
            TTL = new TimeSpan(0, 15, 0);
        }

        public CacheableAttribute(string key)
            : this()
        {
            Key = key;
        }

        public CacheableAttribute(string key, TimeSpan ttl)
            : this(key)
        {
            TTL = ttl;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NPoco
{
    /// <summary>
    /// Used to create a hash code from multiple objects.
    /// </summary>
    /// <remarks>
    /// .Net has a class the same as this: System.Web.Util.HashCodeCombiner and of course it works for all sorts of things
    /// which we've not included here as we just need a quick easy class for this in order to create a unique
    /// hash of directories/files to see if they have changed.
    /// </remarks>
    internal class HashCodeCombiner
    {
        public HashCodeCombiner()
        {
            
        }

        public HashCodeCombiner(string seed)
        {
            AddCaseInsensitiveString(seed);
        }

        private long _combinedHash = 5381L;

        internal HashCodeCombiner AddInt(int i)
        {
            _combinedHash = ((_combinedHash << 5) + _combinedHash) ^ i;
            return this;
        }

        internal HashCodeCombiner AddBool(bool b)
        {
            AddInt(b.GetHashCode());
            return this;
        }

        internal HashCodeCombiner AddType(Type t)
        {
            if (t !=  null)
                AddInt((t.AssemblyQualifiedName ?? t.ToString()).GetHashCode());
            return this;
        }

        internal HashCodeCombiner AddCaseInsensitiveString(string s)
        {
            if (s != null)
                AddInt((StringComparer.OrdinalIgnoreCase).GetHashCode(s));
            return this;
        }

        internal HashCodeCombiner Each<T>(IEnumerable<T> list, Action<HashCodeCombiner, T> action)
        {
            foreach (var item in list)
            {
                action(this, item);
            }
            
            return this;
        }

        /// <summary>
        /// Returns the hex code of the combined hash code
        /// </summary>
        /// <returns></returns>
        internal string GetCombinedHashCode()
        {
            return _combinedHash.ToString("x", CultureInfo.InvariantCulture);
        }

    }
}
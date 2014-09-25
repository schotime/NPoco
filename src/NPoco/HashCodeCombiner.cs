using System;
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
        private long _combinedHash = 5381L;

        internal void AddObject(object o)
        {
            AddInt(o.GetHashCode());
        }

        internal void AddInt(int i)
        {
            _combinedHash = ((_combinedHash << 5) + _combinedHash) ^ i;
        }
        
        internal void AddCaseInsensitiveString(string s)
        {
            if (s != null)
                AddInt((StringComparer.InvariantCultureIgnoreCase).GetHashCode(s));
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
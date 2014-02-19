﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    public static class TypeHelpers
    {
        /// <summary>
        /// Gets an object's type even if it is null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="that">The object being extended.</param>
        /// <returns>The objects type.</returns>
        public static Type GetTheType<T>(this T that)
        {
            return typeof(T);
        }

        /// <summary>
        /// Gets an object's type even if it is null.
        /// </summary>
        /// <param name="that">The object being extended.</param>
        /// <returns>The objects type.</returns>
        public static Type GetTheType(this object that)
        {
            if (that != null)
            {
                return that.GetType();
            }

            return null;
        }
    }
}

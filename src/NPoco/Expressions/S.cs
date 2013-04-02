﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco.Expressions
{
    public static class S
    {
        //public static bool In<T>(T value, IList<Object> list)
        //{
        //    foreach (Object obj in list)
        //    {
        //        if (obj == null || value == null) continue;
        //        if (obj.ToString() == value.ToString()) return true;
        //    }
        //    return false;
        //}

        public static bool In<T>(T value, params object[] list)
        {
            foreach (var obj in list)
            {
                if (obj == null || value == null) continue;
                if (obj.ToString() == value.ToString()) return true;
            }
            return false;
        }

        public static string Desc<T>(T value)
        {
            return value == null ? "" : value.ToString() + " DESC";
        }

        public static string As<T>(T value, object asValue)
        {
            return value == null ? "" : string.Format("{0} AS {1}", value.ToString(), asValue);
        }

        public static T Sum<T>(T value)
        {
            return value;
        }

        public static T Count<T>(T value)
        {
            return value;
        }

        public static T Min<T>(T value)
        {
            return value;
        }

        public static T Max<T>(T value)
        {
            return value;
        }

        public static T Avg<T>(T value)
        {
            return value;
        }
    }
}

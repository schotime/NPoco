using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NPoco
{
    public class OneToManyHelper
    {
        public static void SetListValue<T>(Func<T, IList> listFunc, PocoMember pocoMember, object prevPoco, T poco)
        {
            var prevList = listFunc((T)prevPoco);
            var currentList = listFunc(poco);

            if (prevList == null && currentList != null)
            {
                prevList = pocoMember.CreateList();
                pocoMember.SetValue(prevPoco, prevList);
            }

            if (prevList != null && currentList != null)
            {
                foreach (var item in currentList)
                {
                    prevList.Add(item);
                }
            }
        }

        public static void SetForeignList<T>(Func<T, IList> listFunc, PocoMember foreignMember, object prevPoco)
        {
            if (listFunc == null || foreignMember == null)
                return;

            var currentList = listFunc((T)prevPoco);

            if (currentList == null)
                return;

            foreach (var item in currentList)
            {
                foreignMember.SetValue(item, prevPoco);
            }
        }
    }
}

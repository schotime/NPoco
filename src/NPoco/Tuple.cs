using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    #if POCO_NO_DYNAMIC
    public class Tuple<T1, T2>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public Tuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
        public Tuple() { }
    }

    public class Tuple<T1, T2, T3>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public Tuple(T1 item1, T2 item2, T3 item3) { Item1 = item1; Item2 = item2; Item3 = item3; }
        public Tuple() { }
    }

    public class Tuple<T1, T2, T3, T4>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) { Item1 = item1; Item2 = item2; Item3 = item3; Item4 = item4; }
        public Tuple() { }
    }
    #endif
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    public class UpdateBatch<T>
    {
        public T Poco { get; set; }
        public Snapshot<T> Snapshot { get; set; }
    }

    public class UpdateBatch
    {
        public static UpdateBatch<T> For<T>(T poco, Snapshot<T> snapshot = null)
        {
            return new UpdateBatch<T> { Poco = poco, Snapshot = snapshot };
        }
    }
}

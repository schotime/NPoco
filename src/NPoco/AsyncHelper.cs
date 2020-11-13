using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NPoco
{
    internal static class AsyncHelper
    {
        internal static T RunSync<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}

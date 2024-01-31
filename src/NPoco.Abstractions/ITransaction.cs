using System;

namespace NPoco
{
    public interface ITransaction : IDisposable
    {
        void Complete();
    }
}
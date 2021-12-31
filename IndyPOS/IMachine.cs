using System;

namespace IndyPOS
{
    public interface IMachine : IDisposable
    {
        void Launch();
    }
}

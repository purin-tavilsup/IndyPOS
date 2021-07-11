using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS
{
    public interface IMachine : IDisposable
    {
        void Launch();
    }
}

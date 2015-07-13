using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eddn.Sdk
{
    public interface IEddnListener
    {
        Task BeginListener(Action<string> callback, CancellationToken cancellationToken = default(CancellationToken));
    }
}
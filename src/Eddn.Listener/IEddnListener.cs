using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eddn.Listener
{
    /// <summary>
    /// Interface IEddnListener
    /// </summary>
    public interface IEddnListener
    {
        /// <summary>
        /// Begins the listener.
        /// </summary>
        /// <param name="callback">The callback to execute when a message is received.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="cancellationAndTimeOutSeconds">The cancellation and time out seconds.</param>
        /// <returns>The management thread as a task.</returns>
        Task BeginListener(Action<string> callback, CancellationToken cancellationToken = default(CancellationToken), int cancellationAndTimeOutSeconds = 30);
    }
}
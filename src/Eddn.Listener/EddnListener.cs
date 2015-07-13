using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using ZeroMQ;

namespace Eddn.Sdk
{
    public class EddnListener : IEddnListener
    {
        protected readonly string Endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="EddnListener"/> class.
        /// </summary>
        /// <param name="endpoint">The EDDN endpoint.</param>
        protected EddnListener(string endpoint)
        {
            Endpoint = endpoint;
        }

        /// <summary>
        /// Creates a listener at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The EDDN endpoint.</param>
        /// <returns>IEddnListener.</returns>
        public static IEddnListener Create(string endpoint = "tcp://eddn-relay.elite-markets.net:9500")
        {
            return new EddnListener(endpoint);
        }


        /// <summary>
        /// Attempts to receive a message.
        /// </summary>
        /// <param name="subscriber">The subscriber socket.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Awaitable Message</returns>
        protected async Task<string> ReceiveMessage(ZSocket subscriber, CancellationToken cancellationToken)
        {
            var frame = await Task.Run(() =>
            {
                try
                {
                    return subscriber.ReceiveFrame();
                }
                catch (ZException zex)
                {
                    if (zex.ErrNo == 11) //Exceeded Timeout
                        return null;

                    throw;
                }
            }, cancellationToken);

            if (frame != null)
            {
                using (var ms = new MemoryStream())
                {
                    frame.CopyTo(ms);

                    ms.Position = 0;

                    using (var stream = new ZlibStream(ms, CompressionMode.Decompress))
                    using (var sr = new StreamReader(stream))
                    {
                        var json = sr.ReadToEnd();

                        return json;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Begins the listener.
        /// </summary>
        /// <param name="callback">The callback to execute when a message is received.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The management thread as a task.</returns>
        public Task BeginListener(Action<string> callback, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(async () =>
            {
                using (var ztx = new ZContext())
                using (var subscriber = new ZSocket(ztx, ZSocketType.SUB))
                {
                    subscriber.Connect(Endpoint);
                    subscriber.Subscribe("");
                    subscriber.ReceiveTimeout = new TimeSpan(0, 0, 30);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var message = await ReceiveMessage(subscriber, cancellationToken);

                        if (message != null)
                            Task.Run(() => callback(message), cancellationToken).Start();
                    }
                }
            }, cancellationToken);
        }
    }
}

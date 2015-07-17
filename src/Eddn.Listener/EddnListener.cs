using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using ZeroMQ;

namespace Eddn.Listener
{
    public class EddnListener : IEddnListener
    {
        protected readonly string Endpoint;
        protected Action<string> LogMethod = message => { };

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
        public static EddnListener Create(string endpoint = "tcp://eddn-relay.elite-markets.net:9500")
        {
            return new EddnListener(endpoint);
        }

        public IEddnListener AddLogMethod(Action<string> logMethod)
        {
            LogMethod = logMethod ?? (message => { });

            return this;
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
                    LogMethod("Awaiting message");
                    return subscriber.ReceiveFrame();
                }
                catch (ZException zex)
                {
                    if (zex.ErrNo != 11) throw;

                    LogMethod("No message this round");
                    return null;
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
        /// <param name="cancellationAndTimeOutSeconds">The cancellation and time out seconds.</param>
        /// <returns>The management thread as a task.</returns>
        public Task BeginListener(Action<string> callback, CancellationToken cancellationToken = default(CancellationToken), int cancellationAndTimeOutSeconds = 30)
        {
            return Task.Run(async () =>
            {
                using (var ztx = new ZContext())
                using (var subscriber = new ZSocket(ztx, ZSocketType.SUB))
                {
                    subscriber.Subscribe("");                    
                    subscriber.ReceiveTimeout = TimeSpan.FromSeconds(cancellationAndTimeOutSeconds);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        subscriber.Connect(Endpoint);
                        var message = await ReceiveMessage(subscriber, cancellationToken);

                        if (message != null)
                            Task.Run(() =>
                             {
                                 LogMethod("Found message, activating callback.");
                                 callback(message);
                             }, cancellationToken).FireAndForget();
                    }

                    subscriber.Disconnect(Endpoint);
                    subscriber.Close();
                    subscriber.Dispose();
                }
            }, cancellationToken);
        }
    }
}

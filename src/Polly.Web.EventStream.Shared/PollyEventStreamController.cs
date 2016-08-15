using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Http;

namespace Polly.Web.EventStream
{
    
    public class PollyEventStreamController : ApiController
    {
        private static readonly Lazy<Timer> _timer = new Lazy<Timer>(() => new Timer(TimerCallback, null, 0, 1000));
        private static readonly ConcurrentDictionary<StreamWriter, StreamWriter> _outputs = new ConcurrentDictionary<StreamWriter, StreamWriter>();

        /// <summary>
        /// Gets the updates.
        /// </summary>
        /// <returns>HttpResponseMessage.</returns>
        public virtual HttpResponseMessage GetUpdates(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new PushStreamContent((responseStream, httpContent, context) =>
            {
                StreamWriter responseStreamWriter = new StreamWriter(responseStream);

                // Register a callback which gets triggered when a client disconnects
                cancellationToken.Register(CancellationRequested, responseStreamWriter);

                _outputs.TryAdd(responseStreamWriter, responseStreamWriter);

            }, "application/json");

            Timer t = _timer.Value;

            return response;
        }
        private void CancellationRequested(object state)
        {
            StreamWriter responseStreamWriter = state as StreamWriter;

            if (responseStreamWriter != null)
            {
                _outputs.TryRemove(responseStreamWriter, out responseStreamWriter);
            }
        }

        // Runs every second after the first request to this controller and
        // writes to the response streams of all currently active requests
        private static void TimerCallback(object state)
        {
            foreach (var kvp in _outputs.ToArray())
            {
                StreamWriter responseStreamWriter = kvp.Value;

                try
                {
                    
                    responseStreamWriter.Write(DateTime.Now);
                    responseStreamWriter.Flush();
                }
                catch { }
            }
        }

    }
}

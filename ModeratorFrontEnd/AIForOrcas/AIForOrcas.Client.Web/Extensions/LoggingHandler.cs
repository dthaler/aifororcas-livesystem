using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AIForOrcas.Client.Web.Extensions
{
    public sealed class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        private static int _instanceCount = 0;
        private readonly int _instanceId;

        public LoggingHandler(ILogger<LoggingHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _instanceId = Interlocked.Increment(ref _instanceCount);
            _logger.LogDebug("[LoggingHandler #{InstanceId}] Constructed", _instanceId);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _logger.LogError("!!! [LoggingHandler #{InstanceId}] SendAsync CALLED for {Method} {Uri} !!!",
                _instanceId, request.Method, request.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogError("!!! [LoggingHandler #{InstanceId}] Response {StatusCode} !!!",
                _instanceId, response.StatusCode);

            return response;
        }
    }
}

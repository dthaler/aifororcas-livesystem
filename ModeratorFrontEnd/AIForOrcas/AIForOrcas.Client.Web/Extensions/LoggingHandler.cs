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

        public LoggingHandler(ILogger<LoggingHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogError("========== [LoggingHandler] INSTANCE CREATED ==========");
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogError("========== [LoggingHandler] SEND ASYNC STARTED ==========");
                _logger.LogError("[LoggingHandler] {Method} {Uri}", request.Method, request.RequestUri);
                
                var response = await base.SendAsync(request, cancellationToken);
                
                _logger.LogError("[LoggingHandler] Response: {Status}", response.StatusCode);
                _logger.LogError("========== [LoggingHandler] SEND ASYNC COMPLETED ==========");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LoggingHandler] EXCEPTION in SendAsync");
                throw;
            }
        }
    }
}

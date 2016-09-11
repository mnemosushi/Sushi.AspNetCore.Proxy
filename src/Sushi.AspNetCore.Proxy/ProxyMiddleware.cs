using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sushi.AspNetCore.Proxy
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProxyMiddleware> _logger;
        private readonly HttpClient _httpClient;

        public ProxyMiddleware(RequestDelegate next, ILogger<ProxyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();

            _logger.LogInformation($"{httpContext.Request.Method} - {requestFeature.RawTarget}");

            var requestUri = new Uri(requestFeature.RawTarget);
            var requestMethod = new HttpMethod(httpContext.Request.Method);
            var requestMessage = new HttpRequestMessage(requestMethod, requestUri);

            // Filter method which should contains request body data
            if (requestMethod != HttpMethod.Get &&
                requestMethod != HttpMethod.Head &&
                requestMethod != HttpMethod.Delete &&
                requestMethod != HttpMethod.Trace)
            {
                var streamContent = new StreamContent(httpContext.Request.Body);
                requestMessage.Content = streamContent;
            }

            // Set httpcontext request headers into the httpclient request headers
            foreach (var header in httpContext.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                    requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Send request and receive response
            using (var responseMessage = await _httpClient.SendAsync(
                requestMessage, 
                HttpCompletionOption.ResponseHeadersRead, 
                httpContext.RequestAborted))
            {
                httpContext.Response.StatusCode = (int)responseMessage.StatusCode;

                foreach (var header in responseMessage.Headers)
                {
                    httpContext.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    httpContext.Response.Headers[header.Key] = header.Value.ToArray();
                }

                await responseMessage.Content.CopyToAsync(httpContext.Response.Body);
            }
        }
    }
}

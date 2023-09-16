using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Agape.Auctions.Functions.Cars.BidScheduler.Utilities
{
    public class CustomHttpClientHandler : HttpClientHandler
    {
        public static string subKey { get; private set; }

        public CustomHttpClientHandler(string subscriptionKey)
        {
            subKey = subscriptionKey;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", subKey);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Utilities
{
    public class CustomHttpClientHandler : HttpClientHandler
    {

        public static IConfiguration config { get; private set; }
        public CustomHttpClientHandler(IConfiguration configuration)
        {
            config = configuration;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var subscriptionKey = config.GetSection("Configuration").GetValue<string>("SubscriptionKey");
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

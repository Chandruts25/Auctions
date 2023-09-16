using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Agape.Auctions.Functions.Cars.Images.Utilities
{
    public class CustomHttpClientHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", "4b079cac94664961af480012fd59c6df");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

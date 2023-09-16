using Agape.Auctions.Dealers.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Agape.Auctions.Dealers.Repository
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Dealer>> GetItemsAsync();
        Task<Models.Dealer> GetItemAsync(string id);
        Task AddItemAsync(Dealer item);
        Task UpdateItemAsync(string id, Dealer item);
        Task DeleteItemAsync(string id);

        Task<IEnumerable<Dealer>> GetDealerByOwner(string id);
    }
}

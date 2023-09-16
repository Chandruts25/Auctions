using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Auctions.Users.Models;

namespace Agape.Auctions.Users.Repository
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<User>> GetItemsAsync();
        Task<User> GetItemAsync(string id);
        Task AddItemAsync(User item);
        Task UpdateItemAsync(string id, User item);
        Task DeleteItemAsync(string id);
        Task<IEnumerable<Models.User>> GetUserAsyncByIdentity(string id);

    }
}

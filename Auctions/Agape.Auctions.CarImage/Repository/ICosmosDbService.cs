using Agape.Auctions.CarImage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agape.Auctions.CarImage.Repository
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<CarImages>> GetItemsAsync();
        Task<CarImages> GetItemAsync(string id);
        Task AddItemAsync(CarImages item);
        Task UpdateItemAsync(string id, CarImages item);
        Task DeleteItemAsync(string id);
        Task<IEnumerable<CarImages>> GetItemsAsyncByCarId(string id);
    }
}

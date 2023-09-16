using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agape.Auctions.Cars;
using Agape.Auctions.Cars.Models;

namespace Agape.Auctions.Cars.Repository
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Car>> GetItemsAsync();
        Task<Car> GetItemAsync(string id);
        Task AddItemAsync(Car item);
        Task UpdateItemAsync(string id, Car item);
        Task DeleteItemAsync(string id);
        Task<IEnumerable<Car>> GetCarByUser(string id);
        Task<IEnumerable<string>> GetSearchFilter(string id);

        Task<IEnumerable<Car>> GetFilterCars(string make, string model, decimal startPrice, decimal endPrice, int yearFrom, int yearTo);
    }
}

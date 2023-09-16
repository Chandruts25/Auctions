using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agape.Auctions.Cars.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Agape.Auctions.Cars.Repository
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this.container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(Car item)
        {
            // item.Id = Guid.NewGuid().ToString();
            await this.container.CreateItemAsync<Car>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this.container.DeleteItemAsync<Car>(id, new PartitionKey(id));
        }

        public async Task<Car> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Car> response = await this.container.ReadItemAsync<Car>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<Car>> GetItemsAsync()
        {
            var query = this.container.GetItemQueryIterator<Car>();
            var results = new List<Car>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task UpdateItemAsync(string id, Car item)
        {
            await this.container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public async Task<IEnumerable<Car>> GetCarByUser(string id)
        {
            var query = this.container.GetItemLinqQueryable<Car>();
            var iterator = query.Where(i => i.Owner == id).ToFeedIterator();
            return await iterator.ReadNextAsync();
        }

        public async Task<IEnumerable<string>> GetSearchFilter(string id)
        {
            var query = this.container.GetItemLinqQueryable<Car>();
            FeedIterator<string> iterator;
            if (id == "1")
            {
                iterator = query.Select(i => i.Make).Distinct().ToFeedIterator();
            }
            else if(id == "2")
            {
                iterator = query.Select(i => i.Model).Distinct().ToFeedIterator();
            }
            else // id=3
            {
                iterator = query.Select(i => i.Year.ToString()).Distinct().ToFeedIterator();
            }
            return await iterator.ReadNextAsync();
        }

        public async Task<IEnumerable<Car>> GetFilterCars(string make, string model, decimal startPrice, decimal endPrice,int yearFrom,int yearTo)
        {
            var finalResults = new List<Car>();
            var query = this.container.GetItemLinqQueryable<Car>();
            var response = await query.Where(i => ((!string.IsNullOrEmpty(make) && make != "0") ? i.Make == make : 1 == 1) &&
             ((!string.IsNullOrEmpty(model) && model != "0") ? i.Model == model : 1 == 1)
             ).ToFeedIterator().ReadNextAsync();

            var results = new List<Car>();
            results.AddRange(response.ToList());
            foreach (var item in results)
            {
                if (item.SalePrice > startPrice && item.SalePrice < endPrice && item.Year >= yearFrom && item.Year <= yearTo)
                {
                    finalResults.Add(item);
                }
            }
            return finalResults;

            //return await query.Where(i => i.SalePrice > startPrice && i.SalePrice < endPrice)
            // .ToFeedIterator().ReadNextAsync(); //SalePrice between is not working 

        }
    }
}

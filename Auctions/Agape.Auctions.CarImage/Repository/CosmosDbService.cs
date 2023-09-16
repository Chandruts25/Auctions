using Agape.Auctions.CarImage.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;

namespace Agape.Auctions.CarImage.Repository
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this.container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(CarImages item)
        {
            await this.container.CreateItemAsync<CarImages>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this.container.DeleteItemAsync<CarImages>(id, new PartitionKey(id));
        }

        public async Task<CarImages> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<CarImages> response = await this.container.ReadItemAsync<CarImages>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<CarImages>> GetItemsAsync()
        {
            var query = this.container.GetItemQueryIterator<CarImages>();
            var results = new List<CarImages>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateItemAsync(string id, CarImages item)
        {
            await this.container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public async Task<IEnumerable<CarImages>> GetItemsAsyncByCarId(string id)
        {
            var query = this.container.GetItemLinqQueryable<CarImages>();
            var iterator = query.Where(i => i.Owner == id).ToFeedIterator();
            return await iterator.ReadNextAsync();
        }
    }
}

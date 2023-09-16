using Agape.Auctions.Dealers.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;


namespace Agape.Auctions.Dealers.Repository
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this.container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(Dealer item)
        {
            item.Id = Guid.NewGuid().ToString();

            await this.container.CreateItemAsync<Dealer>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this.container.DeleteItemAsync<Dealer>(id, new PartitionKey(id));
        }

        public async Task<Dealer> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Dealer> response = await this.container.ReadItemAsync<Dealer>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<Dealer>> GetItemsAsync()
        {
            var query = this.container.GetItemQueryIterator<Dealer>();
            var results = new List<Dealer>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateItemAsync(string id, Dealer item)
        {
            await this.container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public async Task<IEnumerable<Dealer>> GetDealerByOwner(string id)
        {
            var query = this.container.GetItemLinqQueryable<Dealer>();
            var iterator = query.Where(i => i.Owner == id).ToFeedIterator();
            return await iterator.ReadNextAsync();
        }
    }
}

using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;

namespace Agape.Auctions.Users.Repository
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container container;

        public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this.container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(Models.User item)
        {
            // item.Id = Guid.NewGuid().ToString();
            await this.container.CreateItemAsync<Models.User>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this.container.DeleteItemAsync<Models.User>(id, new PartitionKey(id));
        }

        public async Task<Models.User> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Models.User> response = await this.container.ReadItemAsync<Models.User>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<Models.User>> GetItemsAsync()
        {
            var query = this.container.GetItemQueryIterator<Models.User>();
            var results = new List<Models.User>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task UpdateItemAsync(string id, Models.User item)
        {
            await this.container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public async Task<IEnumerable<Models.User>> GetUserAsyncByIdentity(string id)
        {
            var query = this.container.GetItemLinqQueryable<Models.User>();
            var iterator = query.Where(i => i.Idp == id).ToFeedIterator();
            return await iterator.ReadNextAsync();
        }
    }
}


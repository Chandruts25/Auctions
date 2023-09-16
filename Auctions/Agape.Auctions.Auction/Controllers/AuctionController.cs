using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModelAuction = Agape.Auctions.Models.Auctions;
using System.Linq.Expressions;
using System;

namespace Agape.Auctions.Auction.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class AuctionController 
    {
        private readonly ICosmosRepository<AgapeModelAuction.Auction, AgapeModelAuction.Auction> cosmosRepository;

        public AuctionController(ICosmosRepository<AgapeModelAuction.Auction, AgapeModelAuction.Auction> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }

        // GET: api/<AuctionController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModelAuction.Auction>> Get()
        {
            Expression<Func<AgapeModelAuction.Auction, bool>> funcAuction = c => !(string.IsNullOrEmpty(c.Id) && c.Type=="Auction" && c.Deleted == false);
            var result = await cosmosRepository.GetItemsAsync(funcAuction);
            return result.Resource;
        }

        // GET api/<AuctionController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModelAuction.Auction> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<AuctionController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModelAuction.Auction auction)
        {
            await cosmosRepository.CreateAsync(auction);
        }

        // PUT api/<AuctionController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModelAuction.Auction auction)
        {
            await cosmosRepository.UpdateAsync(id, auction);
        }

        // DELETE api/<AuctionController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }
       
    }
}

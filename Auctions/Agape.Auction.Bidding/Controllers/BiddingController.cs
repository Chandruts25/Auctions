using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModelBidding = Agape.Auctions.Models.Biddings;
using System.Linq.Expressions;
using System;
using System.Linq;

namespace Agape.Auction.Bidding.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class BiddingController 
    {
        private readonly ICosmosRepository<AgapeModelBidding.Bid, AgapeModelBidding.Bid> cosmosRepository;

        public BiddingController(ICosmosRepository<AgapeModelBidding.Bid, AgapeModelBidding.Bid> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }

        // GET: api/<BiddingController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModelBidding.Bid>> Get()
        {
            Expression<Func<AgapeModelBidding.Bid, bool>> funcBid = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Bid" && c.Deleted == false);
            var result = await cosmosRepository.GetItemsAsync(funcBid);
            return result.Resource;
        }

        // GET api/<BiddingController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModelBidding.Bid> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        [HttpGet("GetHighestBid/{id}")]
        public async Task<decimal> GetHighestBid(string id)
        {
            decimal MaxBid = 0;
            var result = await cosmosRepository.FindAsync(li => li.CarId == id);
            if (result != null)
            {
                if (result.Resource.Any())
                    MaxBid = result.Resource.ToList().Max(x => x.BiddingAmount);

            }

            return MaxBid;

        }

        // POST api/<BiddingController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModelBidding.Bid offer)
        {
            await cosmosRepository.CreateAsync(offer);
        }

        // PUT api/<BiddingController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModelBidding.Bid offer)
        {
            await cosmosRepository.UpdateAsync(id, offer);
        }

        // DELETE api/<BiddingController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }
    }
}

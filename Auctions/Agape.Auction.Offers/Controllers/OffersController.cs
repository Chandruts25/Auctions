using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModelOffer = Agape.Auctions.Models.Offers;
using System.Linq.Expressions;
using System;

namespace Agape.Auction.Offers.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class OffersController 
    {
        private readonly ICosmosRepository<AgapeModelOffer.Offer, AgapeModelOffer.Offer> cosmosRepository;

        public OffersController(ICosmosRepository<AgapeModelOffer.Offer, AgapeModelOffer.Offer> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }

        // GET: api/<OffersController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModelOffer.Offer>> Get()
        {
            Expression<Func<AgapeModelOffer.Offer, bool>> funcOffer = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Offer" && c.Deleted == false);
            var result = await cosmosRepository.GetItemsAsync(funcOffer);
            return result.Resource;
        }

        // GET api/<OffersController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModelOffer.Offer> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<OffersController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModelOffer.Offer offer)
        {
            await cosmosRepository.CreateAsync(offer);
        }

        // PUT api/<OffersController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModelOffer.Offer offer)
        {
            await cosmosRepository.UpdateAsync(id, offer);
        }

        // DELETE api/<OffersController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }

    }
}

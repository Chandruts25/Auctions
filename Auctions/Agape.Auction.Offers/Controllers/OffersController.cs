using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Agape.Auction.Offers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController
    {
        private readonly AuctionDbContext _context;

        public OffersController(AuctionDbContext context)
        {
            _context = context;
        }

        // GET: api/<OffersController>
        [HttpGet]
        public async Task<IEnumerable<Offer>> Get()
        {
            Expression<Func<Offer, bool>> funcOffer = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Offer" && c.Deleted == false);
            var result = await _context.Offers
                .Where(funcOffer)
                .ToListAsync();
            return result;
        }

        // GET api/<OffersController>/5
        [HttpGet("{id}")]
        public async Task<Offer> Get(string id)
        {
            var result = await _context.Offers.FindAsync(id);
            return result;
        }

        // POST api/<OffersController>
        [HttpPost]
        public async Task Post([FromBody] Offer offer)
        {
            await _context.Offers.AddAsync(offer);
            await _context.SaveChangesAsync();
        }

        // PUT api/<OffersController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Offer offer)
        {
            _context.Entry(offer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // DELETE api/<OffersController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Offers.RemoveRange(_context.Offers.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }

    }
}

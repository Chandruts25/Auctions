using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModelBidding = Agape.Auctions.Models.Biddings;
using System.Linq.Expressions;
using System;
using System.Linq;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace Agape.Auction.Bidding.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BiddingController
    {
        private readonly AuctionDbContext _context;

        public BiddingController(AuctionDbContext context)
        {
            _context = context;
        }

        // GET: api/<BiddingController>
        [HttpGet]
        public async Task<IEnumerable<Bid>> Get()
        {
            Expression<Func<Bid, bool>> funcBid = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Bid" && c.Deleted == false);
            var result = await _context.Bids
                .Where(funcBid)
                .ToListAsync();
            return result;
        }

        // GET api/<BiddingController>/5
        [HttpGet("{id}")]
        public async Task<Bid> Get(string id)
        {
            var result = await _context.Bids.FindAsync(id);
            return result;
        }

        [HttpGet("GetHighestBid/{id}")]
        public async Task<decimal> GetHighestBid(string id)
        {
            decimal MaxBid = 0;
            var result = await _context.Bids.Where(li => li.CarId == id).ToListAsync();
            if (result != null)
            {
                if (result.Any())
                    MaxBid = result.Max(x => x.BiddingAmount);
            }
            return MaxBid;

        }

        // POST api/<BiddingController>
        [HttpPost]
        public async Task Post([FromBody] Bid offer)
        {
            await _context.Bids.AddAsync(offer);
            await _context.SaveChangesAsync();
        }

        // PUT api/<BiddingController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Bid offer)
        {
            _context.Entry(offer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // DELETE api/<BiddingController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Bids.RemoveRange(_context.Bids.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }
    }
}

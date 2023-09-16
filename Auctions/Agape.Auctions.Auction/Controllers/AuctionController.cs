using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Model = DataAccessLayer.Models;

namespace Agape.Auctions.Auction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionController
    {
        private readonly Model.AuctionDbContext _context;
        public AuctionController(Model.AuctionDbContext context)
        {
            _context = context;
        }

        // GET: api/<AuctionController>
        [HttpGet]
        public async Task<IEnumerable<Model.Auction>> Get()
        {
            Expression<Func<Model.Auction, bool>> funcAuction = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Auction" && c.Deleted == false);
            var result = await _context.Auctions.Where(funcAuction).ToListAsync();
            return result;
        }

        // GET api/<AuctionController>/5
        [HttpGet("{id}")]
        public async Task<Model.Auction> Get(string id)
        {
            var result = await _context.Auctions.FindAsync(id);
            return result;
        }

        // POST api/<AuctionController>
        [HttpPost]
        public async Task Post([FromBody] Model.Auction auction)
        {
            await _context.Auctions.AddAsync(auction);
            await _context.SaveChangesAsync();
        }

        // PUT api/<AuctionController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Model.Auction auction)
        {
            _context.Entry(auction).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // DELETE api/<AuctionController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Auctions.RemoveRange(_context.Auctions.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }

    }
}

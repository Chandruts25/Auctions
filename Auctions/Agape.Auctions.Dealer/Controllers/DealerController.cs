using Agape.Azure.Cosmos;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace Agape.Auctions.Dealers.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class DealerController : ControllerBase
    {
        private readonly AuctionDbContext _context;

        public DealerController(AuctionDbContext context)
        {
            _context = context;
        }
        // GET: api/<DealerController>
        [HttpGet]
        public async Task<IEnumerable<UserBase>> Get()
        {
            Expression<Func<UserBase, bool>> funcDealer = c => (!string.IsNullOrEmpty(c.Id) && c.Type == "user" && c.UserType == "dealer");
            var result = await _context.Users
                .Select(c => new UserBase
                {
                    Type = c.Type,
                    Id = c.Id,
                    Version = c.Version,
                    UserType = c.UserType,
                    DealerId = c.DealerId,
                    Email = c.Email,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Deleted = c.Deleted
                })
                .Where(funcDealer).ToListAsync();
            return result;
        }

        // GET api/<DealerController>/5
        [HttpGet("{id}")]
        public async Task<User> Get(string id)
        {
            var result = await _context.Users.Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);
            result.PaymentMethods = result.PaymentMethodsString.Split("|").ToList();
            if (result.Address != null)
                result.Address.User = null;
            return result;
        }

        // POST api/<DealerController>
        [HttpPost]
        public async Task Post([FromBody] User dealer)
        {
            dealer.PaymentMethodsString = string.Join("|", dealer.PaymentMethods);
            await _context.Users.AddAsync(dealer);
            await _context.SaveChangesAsync();
        }

        // PUT api/<DealerController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] User dealer)
        {
            dealer.PaymentMethodsString = string.Join("|", dealer.PaymentMethods);

            if (dealer.Address != null)
            {
                _context.Users.RemoveRange(_context.Users.Where(c => c.Id == id));
                await _context.SaveChangesAsync();

                await _context.Users.AddAsync(dealer);
            }
            else
            {
                _context.Entry(dealer).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        // DELETE api/<DealerController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Users.RemoveRange(_context.Users.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }
    }
}

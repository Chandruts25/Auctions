using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Linq;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace Agape.Auctions.Users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AuctionDbContext _context;

        public UserController(AuctionDbContext context)
        {
            _context = context;
        }

        [HttpGet("IsUserExist/{email}")]
        public async Task<IActionResult> IsUsrExist(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email);
            if (user != null)
                return Ok();
            else
                return NotFound();
        }

        [HttpGet("Login")]
        public async Task<IActionResult> Login(string userId, string password)
        {
            User result = await _context.Users
                .Where(c => c.Email == userId && c.Password == password)
                .FirstOrDefaultAsync();

            if (result != null)
                return Ok(result);
            else
                return NotFound();
        }

        // GET: api/<UserController>
        [HttpGet]
        public async Task<IEnumerable<UserBase>> Get()
        {
            Expression<Func<UserBase, bool>> funcUser = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "user" && c.UserType.Equals("user"));
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
                .Where(funcUser).ToListAsync();

            return result;
        }

        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public async Task<User> Get(string id)
        {
            var result = await _context.Users.Include(c => c.Address).FirstOrDefaultAsync(c => c.Id == id);
            result.PaymentMethods = result.PaymentMethodsString.Split("|").ToList();
            if (result.Address != null)
                result.Address.User = null;
            return result;
        }
            
        // POST api/<UserController>
        [HttpPost]
        public async Task Post([FromBody] User user)
        {
            user.PaymentMethodsString = string.Join("|", user.PaymentMethods);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] User user)
        {
            user.PaymentMethodsString = string.Join("|", user.PaymentMethods);

            if (user.Address != null)
            {
                _context.Users.RemoveRange(_context.Users.Where(c => c.Id == id));
                await _context.SaveChangesAsync();

                await _context.Users.AddAsync(user);
            }
            else
            {
                _context.Entry(user).State = EntityState.Modified;
            }
            
            await _context.SaveChangesAsync();
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Users.RemoveRange(_context.Users.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }

        // GET api/<UserController>/Users/idp/5
        [HttpGet("idp/{id}")]
        public async Task<IEnumerable<User>> Find(string id)
        {
            Expression<Func<User, bool>> funcUser = c => c.Idp == id;
            var result = await _context.Users
                .Where(funcUser)
                .Include(c => c.Address)
                .ToListAsync();
            result[0].PaymentMethods = result[0].PaymentMethodsString.Split("|").ToList();
            if (result[0].Address != null)
                result[0].Address.User = null;
            return result;
        }
    }
}


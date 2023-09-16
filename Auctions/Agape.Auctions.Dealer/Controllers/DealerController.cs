using Agape.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AgapeModelUser = Agape.Auctions.Models.Users;

namespace Agape.Auctions.Dealers.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class DealerController : ControllerBase
    {
        private readonly ICosmosRepository<AgapeModelUser.User, AgapeModelUser.UserBase> cosmosRepository;

        public DealerController(ICosmosRepository<AgapeModelUser.User, AgapeModelUser.UserBase> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }
        // GET: api/<DealerController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModelUser.UserBase>> Get()
        {
            Expression<Func<AgapeModelUser.UserBase, bool>> funcDealer = c => !(string.IsNullOrEmpty(c.Id)) && c.Type == "user" && c.UserType.Equals("dealer");
            var result = await cosmosRepository.GetItemsAsync(funcDealer);
            return result.Resource;
        }

        // GET api/<DealerController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModelUser.User> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<DealerController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModelUser.User dealer)
        {
            await cosmosRepository.CreateAsync(dealer);
        }

        // PUT api/<DealerController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModelUser.User dealer)
        {
            await cosmosRepository.UpdateAsync(id, dealer);
        }

        // DELETE api/<DealerController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }
    }
}

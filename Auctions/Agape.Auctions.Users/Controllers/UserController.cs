using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModel = Agape.Auctions.Models.Users;
using System;
using System.Linq.Expressions;
using System.Linq;

namespace Agape.Auctions.Users.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ICosmosRepository<AgapeModel.User, AgapeModel.UserBase> cosmosRepository;

        public UserController(ICosmosRepository<AgapeModel.User, AgapeModel.UserBase> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }
        // GET: api/<UserController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModel.UserBase>> Get()
        {
            Expression<Func<AgapeModel.UserBase, bool>> funcUser = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "user" && c.UserType.Equals("user"));
            var result = await cosmosRepository.GetItemsAsync(funcUser);
            return result.Resource;
        }

        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModel.User> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<UserController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModel.User user)
        {
            await cosmosRepository.CreateAsync(user);
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModel.User user)
        {
            await cosmosRepository.UpdateAsync(id, user);
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }

        // GET api/<UserController>/Users/idp/5
        [HttpGet("idp/{id}")]
        public async Task<IEnumerable<AgapeModel.User>> Find(string id)
        {
            Expression<Func<AgapeModel.User, bool>> funcUser = c => c.Idp == id;
            var result = await cosmosRepository.FindAsync(funcUser);
            return result.Resource;
        }
    }
}


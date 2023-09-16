using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModel = Agape.Auctions.Models.Images;
using System.Linq.Expressions;
using System;

namespace Agape.Auctions.CarImage.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class CarImageController
    {
        private readonly ICosmosRepository<AgapeModel.Image, AgapeModel.Image> cosmosRepository;

        public CarImageController(ICosmosRepository<AgapeModel.Image, AgapeModel.Image> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }
        // GET: api/<CarImageController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModel.Image>> Get()
        {
            Expression<Func<AgapeModel.Image, bool>> funcImage = c => !(string.IsNullOrEmpty(c.Id));
            var result = await cosmosRepository.GetItemsAsync(funcImage);
            return result.Resource;
        }

        // GET api/<CarImageController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModel.Image> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<CarImageController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModel.Image carImage)
        {
            await cosmosRepository.CreateAsync(carImage);
        }

        // PUT api/<CarImageController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModel.Image carImage)
        {
            await cosmosRepository.UpdateAsync(id, carImage);
        }

        // DELETE api/<CarImageController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }

        // GET api/<CarImageController>/GetCarImages/5
        [HttpGet("FindImagesByUser/{id}")]
        public async Task<IEnumerable<AgapeModel.Image>> GetCarImages(string id)
        {
            Expression<Func<AgapeModel.Image, bool>> funcImage = c => c.Owner == id;
            var result = await cosmosRepository.FindAsync(funcImage);
            return result.Resource;
        }

    }
}

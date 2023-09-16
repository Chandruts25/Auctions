using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModelCar = Agape.Auctions.Models.Cars;
using System.Linq.Expressions;
using System;

namespace Agape.Auctions.CarReview.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class CarReviewController
    {
        private readonly ICosmosRepository<AgapeModelCar.CarReview, AgapeModelCar.CarReview> cosmosRepository;

        public CarReviewController(ICosmosRepository<AgapeModelCar.CarReview, AgapeModelCar.CarReview> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }

        // GET: api/<OffersController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModelCar.CarReview>> Get()
        {
            Expression<Func<AgapeModelCar.CarReview, bool>> funcOffer = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Review" && c.Deleted == false);
            var result = await cosmosRepository.GetItemsAsync(funcOffer);
            return result.Resource;
        }

        // GET api/<OffersController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModelCar.CarReview> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<OffersController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModelCar.CarReview review)
        {
            await cosmosRepository.CreateAsync(review);
        }

        // PUT api/<OffersController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModelCar.CarReview review)
        {
            await cosmosRepository.UpdateAsync(id, review);
        }

        // DELETE api/<OffersController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await cosmosRepository.DeleteAsync(id);
        }

        // GET api/GetReviewByCar/5
        [HttpGet("GetReviewByCar/{carId}")]
        public async Task<IEnumerable<AgapeModelCar.CarReview>> GetReviewByCar(string carId)
        {
            Expression<Func<AgapeModelCar.CarReview, bool>> funCarReview = c => c.CarId == carId;
            var result = await cosmosRepository.FindAsync(funCarReview);
            return result.Resource;
        }

    }
}
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using AgapeModel = Agape.Auctions.Models.Cars;
using System;
using System.Linq.Expressions;
using System.Linq;

namespace Agape.Auctions.Cars.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class CarController : ControllerBase
    {
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };

        private readonly ICosmosRepository<AgapeModel.Car, AgapeModel.Car> cosmosRepository;

        public CarController(ICosmosRepository<AgapeModel.Car, AgapeModel.Car> cosmosRepositoryServices)
        {
            cosmosRepository = cosmosRepositoryServices;
        }
        // GET: api/<CarController>
        [HttpGet]
        public async Task<IEnumerable<AgapeModel.Car>> Get()
        {
            
            Expression<Func<AgapeModel.Car, bool>> funCar = c => !(string.IsNullOrEmpty(c.Id)) && !c.Deleted;
            var result = await cosmosRepository.GetItemsAsync(funCar);
            return result.Resource;
        }

        // GET api/<CarController>/5
        [HttpGet("{id}")]
        public async Task<AgapeModel.Car> Get(string id)
        {
            var result = await cosmosRepository.GetAsync(id);
            return result.Resource;
        }

        // POST api/<CarController>
        [HttpPost]
        public async Task Post([FromBody] AgapeModel.Car car)
        {
            await cosmosRepository.CreateAsync(car);
        }

        // PUT api/<CarController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] AgapeModel.Car car)
        {
            await cosmosRepository.UpdateAsync(id, car);
        }

        // DELETE api/<CarController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            try
            {

                var getResult = await cosmosRepository.GetAsync(id);
                if (getResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("an error!!");
                }
                getResult.Resource.Deleted = true;
                var updateResult = await cosmosRepository.UpdateAsync(id, getResult.Resource);
                if (updateResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("an error!!");
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        // GET api/<GetCar>/GetCarByUser/5
        [HttpGet("Dealer/{id}")]
        public async Task<IEnumerable<AgapeModel.Car>> GetCarByUser(string id)
        {
            Expression<Func<AgapeModel.Car, bool>> funCar = c => c.Owner == id && !c.Deleted;
            var result = await cosmosRepository.FindAsync(funCar);
            return result.Resource;
        }

        [HttpGet("FindCarsByStatus/{status}")]
        public async Task<IEnumerable<AgapeModel.CarBase>> FindCarsByStatus(string status)
        {
            var result = await cosmosRepository.GetItemsAsync(li => li.Type.Equals("car") && li.Status.Equals(status) && li.Deleted == false);
            return result.Resource;
        }

        // GET api/<GetCar>/GetSearchFilter/1
        [HttpGet("FindMakeModelYear/{id}")]
        public async Task<IEnumerable<string>> GetSearchFilter(string id)
        {
            var result = await Get();
            if (id == "1")
            {
               return result.ToList().Where(i => !invalidStatustoShow.Contains(i.Status)).Select(i => i.Make).Distinct();
            }
            else if (id == "2")
            {
                return result.ToList().Where(i => !invalidStatustoShow.Contains(i.Status)).Select(i => i.Model).Distinct();
            }
            else
            {
                return result.ToList().Where(i => !invalidStatustoShow.Contains(i.Status)).Select(i => i.Year.ToString()).Distinct();
            }
        }

        [HttpGet("FindCarsByFilter/{make}/{model}/{startPrice}/{endPrice}/{yearFrom}/{yearTo}")]
        public async Task<IEnumerable<AgapeModel.Car>> GetFilterCars(string make, string model, double startPrice, double endPrice, int yearFrom, int yearTo)
        {
            Expression<Func<AgapeModel.Car, bool>> funCar = c => ((!string.IsNullOrEmpty(make) && make != "0") ? c.Make == make : 1 == 1) &&
             ((!string.IsNullOrEmpty(model) && model != "0") ? c.Model == model : 1 == 1);

            var response = await cosmosRepository.GetItemsAsync(funCar);

            var finalResults = new List<AgapeModel.Car>();
            var results = new List<AgapeModel.Car>();
            results.AddRange(response.Resource.ToList());
            foreach (var item in results)
            {
                if (item.SalePrice >= startPrice && item.SalePrice <= endPrice && item.Year >= yearFrom && item.Year <= yearTo)
                {
                    finalResults.Add(item);
                }
            }
            return finalResults;
        }
       
    }
}

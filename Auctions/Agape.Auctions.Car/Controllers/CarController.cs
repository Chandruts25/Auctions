using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Linq;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace Agape.Auctions.Cars.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarController : ControllerBase
    {
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };

        private readonly AuctionDbContext _context;

        public CarController(AuctionDbContext context)
        {
            _context = context;
        }
        // GET: api/<CarController>
        [HttpGet]
        public async Task<IEnumerable<Car>> Get()
        {

            Expression<Func<Car, bool>> funCar = c => !(string.IsNullOrEmpty(c.Id)) && !c.Deleted;
            var result = await _context.Cars
                .Include(c => c.Video)
                .Where(funCar)
                .ToListAsync();

            foreach (var item in result)
            {
                if (item.Video != null)
                    item.Video.Car = null;
            }
            return result;
        }

        // GET api/<CarController>/5
        [HttpGet("{id}")]
        public async Task<Car> Get(string id)
        {
            var result = await _context.Cars.Include(c => c.Video).FirstOrDefaultAsync(c => c.Id == id);
            if(result.Video != null)
                result.Video.Car = null;
            return result;
        }

        // POST api/<CarController>
        [HttpPost]
        public async Task Post([FromBody] Car car)
        {
            await _context.Cars.AddAsync(car);
            await _context.SaveChangesAsync();
        }

        // PUT api/<CarController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Car car)
        {
            if (car.Video != null && (!string.IsNullOrEmpty(car.Video.Url) || !string.IsNullOrEmpty(car.Video.Url) || !string.IsNullOrEmpty(car.Video.Url)))
            {
                _context.Cars.RemoveRange(_context.Cars.Where(c => c.Id == id));
                await _context.SaveChangesAsync();

                await _context.Cars.AddAsync(car);
            }
            else
            {
                _context.Entry(car).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        // DELETE api/<CarController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            try
            {

                var getResult = await _context.Cars.FindAsync(id);
                if (getResult == null)
                {
                    throw new Exception("an error!!");
                }
                getResult.Deleted = true;
                _context.Entry(getResult).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        // GET api/<GetCar>/GetCarByUser/5
        [HttpGet("Dealer/{id}")]
        public async Task<IEnumerable<Car>> GetCarByUser(string id)
        {
            Expression<Func<Car, bool>> funCar = c => c.Owner == id && !c.Deleted;
            var cars = await _context.Cars
                .Include(c => c.Video)
                .Where(funCar)
                .ToListAsync();
            foreach (var item in cars)
            {
                if (item.Video != null)
                    item.Video.Car = null;
            }
            return cars;
        }

        [HttpGet("FindCarsByStatus/{status}")]
        public async Task<IEnumerable<CarBase>> FindCarsByStatus(string status)
        {
            var result = await _context.Cars.Select(c => new CarBase
            {
                Type = c.Type,
                Id = c.Id,
                Version = c.Version,
                Owner = c.Owner,
                Mileage = c.Mileage,
                SalePrice = c.SalePrice,
                HasImages = c.HasImages,
                Status = c.Status,
                Make = c.Make,
                Model = c.Model,
                Year = c.Year,
                Color = c.Color,
                Thumbnail = c.Thumbnail,
                PagePartId = c.PagePartId,
                Deleted = c.Deleted
            })
            .Where(li => li.Type.Equals("car") && li.Status.Equals(status) && li.Deleted == false)
            .ToListAsync();
            return result;
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
        public async Task<IEnumerable<Car>> GetFilterCars(string make, string model, double startPrice, double endPrice, int yearFrom, int yearTo)
        {
            Expression<Func<Car, bool>> funCar = c => ((!string.IsNullOrEmpty(make) && make != "0") ? c.Make == make : 1 == 1) &&
             ((!string.IsNullOrEmpty(model) && model != "0") ? c.Model == model : 1 == 1);

            var response = await _context.Cars
                .Include(c => c.Video)
                .Where(funCar)
                .ToListAsync();

            var finalResults = new List<Car>();
            var results = new List<Car>();
            results.AddRange(response.ToList());
            foreach (var item in results)
            {
                if (item.SalePrice >= startPrice && item.SalePrice <= endPrice && item.Year >= yearFrom && item.Year <= yearTo)
                {
                    finalResults.Add(item);
                }
                if (item.Video != null)
                {
                    item.Video.Car = null;
                }
            }
            return finalResults;
        }
    }
}

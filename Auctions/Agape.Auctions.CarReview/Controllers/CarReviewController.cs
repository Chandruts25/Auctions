using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agape.Azure.Cosmos;
using CarReviewModel = DataAccessLayer.Models;
using System.Linq.Expressions;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Agape.Auctions.CarReview.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarReviewController
    {
        private readonly CarReviewModel.AuctionDbContext _context;

        public CarReviewController(CarReviewModel.AuctionDbContext context)
        {
            _context = context;
        }

        // GET: api/<OffersController>
        [HttpGet]
        public async Task<IEnumerable<CarReviewModel.CarReview>> Get()
        {
            Expression<Func<CarReviewModel.CarReview, bool>> funcOffer = c => !(string.IsNullOrEmpty(c.Id) && c.Type == "Review" && c.Deleted == false);
            var result = await _context.CarReviews
                .Where(funcOffer)
                .ToListAsync();
            return result;
        }

        // GET api/<OffersController>/5
        [HttpGet("{id}")]
        public async Task<CarReviewModel.CarReview> Get(string id)
        {
            var result = await _context.CarReviews.FindAsync(id);
            return result;
        }

        // POST api/<OffersController>
        [HttpPost]
        public async Task Post([FromBody] CarReviewModel.CarReview review)
        {
            await _context.CarReviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        // PUT api/<OffersController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] CarReviewModel.CarReview review)
        {
            _context.Entry(review).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // DELETE api/<OffersController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.CarReviews.RemoveRange(_context.CarReviews.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }

        // GET api/GetReviewByCar/5
        [HttpGet("GetReviewByCar/{carId}")]
        public async Task<IEnumerable<CarReviewModel.CarReview>> GetReviewByCar(string carId)
        {
            Expression<Func<CarReviewModel.CarReview, bool>> funCarReview = c => c.CarId == carId;
            var result = await _context.CarReviews
                .Where(funCarReview)
                .ToListAsync();
            return result;
        }
    }
}
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Agape.Auctions.CarImage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarImageController
    {
        private readonly AuctionDbContext _context;

        public CarImageController(AuctionDbContext context)
        {
            _context = context;
        }

        // GET: api/<CarImageController>
        [HttpGet]
        public async Task<IEnumerable<Image>> Get()
        {
            Expression<Func<Image, bool>> funcImage = c => !(string.IsNullOrEmpty(c.Id));
            var result = await _context.Images
                .Where(funcImage)
                .ToListAsync();
            return result;
        }

        // GET api/<CarImageController>/5
        [HttpGet("{id}")]
        public async Task<Image> Get(string id)
        {
            var result = await _context.Images.FindAsync(id);
            return result;
        }

        // POST api/<CarImageController>
        [HttpPost]
        public async Task Post([FromBody] Image carImage)
        {
            await _context.Images.AddAsync(carImage);
            await _context.SaveChangesAsync();
        }

        // PUT api/<CarImageController>/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Image carImage)
        {
            _context.Entry(carImage).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // DELETE api/<CarImageController>/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            _context.Images.RemoveRange(_context.Images.Where(c => c.Id == id));
            await _context.SaveChangesAsync();
        }

        // GET api/<CarImageController>/GetCarImages/5
        [HttpGet("FindImagesByUser/{id}")]
        public async Task<IEnumerable<Image>> GetCarImages(string id)
        {
            Expression<Func<Image, bool>> funcImage = c => c.Owner == id;
            var images = await _context.Images
                .Where(funcImage)
                .ToListAsync();
            return images;
        }

    }
}

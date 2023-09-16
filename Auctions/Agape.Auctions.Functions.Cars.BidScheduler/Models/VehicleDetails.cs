using System;
using System.Collections.Generic;
using System.Text;

namespace Agape.Auctions.Functions.Cars.BidScheduler.Models
{
    public class VehicleDetails
    {
        public int Count { get; set; }
        public string Message { get; set; }
        public string SearchCriteria { get; set; }
        public CarProperties[] Results { get; set; }
    }
}

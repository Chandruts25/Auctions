using Agape.Auctions.Models.Cars;

namespace Agape.Auctions.UI.Cars.Models
{
    public class VehicleDetails
    {
        public int Count { get; set; }
        public string Message { get; set; }
        public string SearchCriteria { get; set; }
        public CarProperties[] Results { get; set; }
    }
}

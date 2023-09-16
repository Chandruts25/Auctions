using System.ComponentModel.DataAnnotations;

namespace Agape.Auctions.Functions.Cars.BidRefresh.Models
{
    public partial class Car : CarBase
    {
        public string Description { get; set; }
        public CarProperties Properties { get; set; }
        public Video Video { get; set; }
        [Required]
        public string Vin { get; set; }
        public bool VinDecoded { get; set; } = false;
        public bool IsAutomatic { get; set; }
        public bool IsNew { get; set; }
        public bool IsPetrol { get; set; }
    }
}
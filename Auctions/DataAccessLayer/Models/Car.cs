using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models
{
    public class Car : CarBase
    {
        //public Car();

        public string Description { get; set; }
        public CarProperties Properties { get; set; }
        public Video Video { get; set; }
        [Required]
        public string Vin { get; set; }
        public bool VinDecoded { get; set; }
        public bool IsAutomatic { get; set; }
        public bool IsNew { get; set; }
        public bool IsPetrol { get; set; }
    }
}

using System.Collections.Generic;

namespace Agape.Auctions.Functions.Cars.WatchEmail.Models
{
    public partial class User : UserBase
    {
        public string Idp { get; set; }
        public string CompanyName { get; set; }
        public List<string> PaymentMethods { get; set; }
        public string Phone { get; set; }
        public bool HasMoreCars { get; set; } = false;
        public List<string> Purchases { get; set; }
        public bool HasMorePurchases { get; set; } = false;
    }
}

using System.Collections.Generic;

namespace Agape.Auctions.UI.Cars.Models
{
    public class DealerInfo
    {
        public string DealerState { get; set; }

        public List<Dealer> Dealers { get; set; }
    }
    public class Dealer
    {
        public string Id { get; set; }
        public string DealerCompanyName { get; set; }

    }
}

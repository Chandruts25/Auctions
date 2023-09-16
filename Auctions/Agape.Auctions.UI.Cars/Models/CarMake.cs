using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Models
{
    public class CarMake
    {
        public int MakeId { get; set; }
        public string MakeName { get; set; }

        public int Model_ID { get; set; }
        public string Model_Name { get; set; }
    }

    public class CarModel
    {
        public int Model_ID { get; set; }
        public string Model_Name { get; set; }
    }

    public class CarMakeDetails
    {
        public string Message { get; set; }

        public List<CarMake> Results { get; set; }

    }
}

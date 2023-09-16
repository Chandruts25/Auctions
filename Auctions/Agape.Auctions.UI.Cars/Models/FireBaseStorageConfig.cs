using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Models
{
    public class FireBaseStorageConfig
    {
        public string apiKey { get; set; }
        public string bucket { get; set; }
        public string authEmail { get; set; }
        public string authPassword { get; set; }
    }
}

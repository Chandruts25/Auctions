using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Agape.Auctions.Functions.Cars.BidScheduler.Models
{
    public class Auction
    {

        public string Type { get; set; } = "Auction";

        [JsonPropertyName("id")]
        public string Id { get; set; }
        public string CarId { get; set; }
        public string DealerId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ApprovedDate { get; set; }
        public string ApprovedBy { get; set; }
        public int AuctionDays { get; set; }
        public decimal Reserve { get; set; }
        public decimal Increment { get; set; }
        public decimal StartAmount { get; set; }
        public bool Deleted { get; set; }
    }
}

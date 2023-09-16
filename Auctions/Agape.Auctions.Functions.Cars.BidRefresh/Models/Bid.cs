using System;
using System.Text.Json.Serialization;


namespace Agape.Auctions.Functions.Cars.BidRefresh.Models
{
    public partial class Bid
    {
        public string Type { get; set; } = "Bid";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CarId { get; set; }
        public decimal BiddingAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public int AuctionDays { get; set; }
        public bool Deleted { get; set; }
    }
}

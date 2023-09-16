using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Agape.Auctions.Functions.Cars.Images.Models
{
    public class CarImage
    {
        public string Type { get; set; } = "image";
        [JsonPropertyName("id")]
        //public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; }
        public string Version { get; set; } = "1";
        public string Owner { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string SmallUrl { get; set; }
        public string Listing { get; set; }
        public string ListingGrid { get; set; }
        public string MediumUrl { get; set; }
        public bool Deleted { get; set; }
        public bool IsProcessed { get; set; }
        public int Order { get; set; }
    }
}

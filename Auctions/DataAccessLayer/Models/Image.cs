using System;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models
{
    public class Image
    {
        //public Image();

        public string Type { get; set; } = "image";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
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

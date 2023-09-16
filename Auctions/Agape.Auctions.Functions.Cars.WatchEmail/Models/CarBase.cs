using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Agape.Auctions.Functions.Cars.WatchEmail.Models
{
    public partial class CarBase
    {
        public string Type { get; set; } = "car";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Version { get; set; } = "1";
        [Required]
        public string Owner { get; set; }
        [Required]
        public int Mileage { get; set; }
        [Required]
        public double SalePrice { get; set; }
        public bool HasImages { get; set; }
        public string Status { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Thumbnail { get; set; }
        public bool Deleted { get; set; }
    }
}
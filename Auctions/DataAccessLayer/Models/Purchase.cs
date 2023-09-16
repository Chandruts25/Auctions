using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models
{
    public class Purchase
    {
        //public Purchase();

        public string Type { get; set; } = "purchase";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Version { get; set; } = "1";
        [Required]
        public string Owner { get; set; }
        [Required]
        public DateTimeOffset Created { get; set; }
        [Required]
        public float Price { get; set; }
        public bool Processed { get; set; }
        public Car Car { get; set; }
        public bool Deleted { get; set; }
    }
}

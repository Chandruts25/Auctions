using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace Agape.Auctions.Functions.Cars.BidRefresh.Models
{
    public partial class UserBase
    {
        public string Type { get; set; } = "user";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Version { get; set; } = "1";
        [Required]
        public string UserType { get; set; } = "user";

        public string DealerId { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string ProfilePhoto { get; set; }
        public bool Deleted { get; set; } = false;
    }
}

using System;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models
{
    public class CarReview
    {
        //public CarReview();

        public string Type { get; set; } = "Review";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Rating { get; set; }
        public string CarId { get; set; }
        public string Title { get; set; }
        public string Comments { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Deleted { get; set; }
    }
}

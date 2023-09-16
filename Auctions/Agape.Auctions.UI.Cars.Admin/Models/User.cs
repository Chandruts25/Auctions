using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Agape.Auctions.UI.Cars.Admin.Models
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "idp")]
        public string Idp { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserType { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string PaymentMethod { get; set; }
        public string Phone { get; set; }
        public string identityId { get; set; }
        public UserAddress Address { get; set; } = new UserAddress();
    }

    public class UserAddress
    {
        public string Country { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
}

using System;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models
{
    public class PaymentMethod
    {
        //public PaymentMethod();

        public string Type { get; set; } = "paymentMethod";
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Version { get; set; } = "1";
        public string Owner { get; set; }
        public DateTimeOffset Created { get; set; }
        public string Status { get; set; }
        public Address BillingDetails { get; set; }
        public string ProviderId { get; set; }
    }
}

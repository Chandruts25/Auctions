using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    public class User : UserBase
    {
        //public User();

        public string Idp { get; set; }
        public string CompanyName { get; set; }
        [NotMapped]
        public List<string> PaymentMethods { get; set; }
        public string PaymentMethodsString { get; set; }
        public string Phone { get; set; }
        public Address Address { get; set; }
        public List<Car> Cars { get; set; }
        public bool HasMoreCars { get; set; }
        [NotMapped]
        public List<string> Purchases { get; set; }
        public string PurchasesString { get; set; }
        public string ProfileUrl { get; set; }
        public bool HasMorePurchases { get; set; }
    }
}

using Newtonsoft.Json;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Shared
{
    public class ContactInfo
    {
        public List<Emails> Emails { get; set; }
        public List<Phone> Phones { get; set; }
        public List<Address> Addresses { get; set; }
    }

    [JsonObject("Email")]
    public class Emails
    {
        public string Email { get; set; }
        public string EmailType { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class Phone
    {
        public string PhoneNumber { get; set; }
        public string PhoneTechType { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class Address
    {
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string CityName { get; set; }
        public string PostalCode { get; set; }
        public string StateProv { get; set; }
        public string State { get; set; }
        public string CountryCode { get; set; }
        public string Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class LoyaltyProgram
    {
        public string ProgramCode { get; set; }
        public string OwnType { get; set; }
        public string MembershipId { get; set; }
    }


}


namespace APITransformationsFunction.Model.Shared
{
    public class ExternalReference
    {
        public string SourceSystem { get; set; }
        public string ReferenceId { get; set; }
    }

    public class PersonName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Title { get; set; }
        public string Suffix { get; set; }
        public string PreferredPronoun { get; set; }
        public string NickName { get; set; }
    }

    public class PostalAddress
    {
        public bool? Status { get; set; }
        public string AddressType { get; set; }
        public string AddressPurpose { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string StateCode { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class PhoneNumber
    {
        public bool? Status { get; set; }
        public string PhoneType { get; set; }
        public string PhonePurpose { get; set; }
        public string Number { get; set; }
        public string Extension { get; set; }
    }

    public class DigitalContact
    {
        public bool? Status { get; set; }
        public string DigitalAddressType { get; set; }
        public string DigitalAddress { get; set; }
    }

    public class MembershipInformation
    {
        public int? PastDue { get; set; }
        public string MembershipLevel { get; set; }
        public string OwnerType { get; set; }
        public TravelersPlus TravelersPlus { get; set; }
        public string AccountNumber { get; set; }
        public int? Points { get; set; }
        public int? PremierGenCode { get; set; }
    }

    public class TravelersPlus
    {
        public string ExpirationDate { get; set; }
    }

    public class OtherAttribute
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }

}

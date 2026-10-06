using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Property
{
    [ExcludeFromCodeCoverage]
    public class Data
    {
        public SystemMeta SystemMeta { get; set; } = null!;
        public ExternalReference ExternalReference { get; set; } = null!;
        public List<Item> ItemsList { get; set; } = [];
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SystemMeta
    {
        public string TenantID { get; set; } = null!;
        public DateTime InsertedDateTime { get; set; }
        public string InsertedBy { get; set; } = null!;
        public DateTime UpdatedDateTime { get; set; }
        public string UpdatedBy { get; set; } = null!;
        public bool Status { get; set; }
        public string StatusReason { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class ExternalReference
    {
        public string SourceSystem { get; set; } = null!;
        public string ReferenceId { get; set; } = null!;
    }
    public class Item
    {
        public string ItemId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string ItemClass { get; set; } = null!;
        public string ItemExternalStatus { get; set; } // Published, Unpublished
        public string ParentId { get; set; }
        public string ParentCode { get; set; } = null!;
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Media> Media { get; set; }
        public Profile Profile { get; set; }
        public GeoLocationItem GeoLocation { get; set; }
        public List<ItemAttribute> ItemAttributes { get; set; }
        public List<OtherAttribute> OtherAttributes { get; set; }
        public SupplementalAttributes SupplementalAttributes { get; set; }
    }

    public class GeoLocationItem
    {
        public string Type { get; set; }
        public List<double> Coordinates { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Media
    {
        public string MediaType { get; set; }
        public string MediaURI { get; set; }
        public string MediaDescription { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Profile
    {
        public List<Address> Addresses { get; set; }
        public List<PhoneNumber> PhoneNumbers { get; set; }
        public List<DigitalAddresses> DigitalAddresses { get; set; }

        public class Address
        {
            public string AddressType { get; set; }
            public string AddressPurpose { get; set; }
            public string Line1 { get; set; } = null!;
            public string City { get; set; } = null!;
            public string State { get; set; }
            public string StateCode { get; set; }
            public string Country { get; set; }
            public string CountryCode { get; set; }
            public string PostalCode { get; set; } = null!;
        }

        public class PhoneNumber
        {
            public string PhoneType { get; set; }
            public string Number { get; set; }
        }

    }

    [ExcludeFromCodeCoverage]
    public class DigitalAddresses
    {
        public string DigitalAddressType { get; set; }
        public string DigitalAddress { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ItemAttribute
    {
        public string Type { get; set; }
        public string Attribute { get; set; }
        public string Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class OtherAttribute
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SupplementalAttributes
    {
        public string PrimaryVendorProfileId { get; set; }
        public double? StandardCost { get; set; }
        public List<ItemSerialNumber> ItemSerialNumbers { get; set; }
        public bool? KitPricedFromComponent { get; set; }
        public List<PriceLevel> PriceLevels { get; set; }
        public List<KitComponent> KitComponents { get; set; }
        public List<ItemSubstitution> ItemSubstitutions { get; set; }
        public List<ItemVendor> ItemVendors { get; set; }

        public class ItemSerialNumber
        {
            public string SerialNumber { get; set; }
            public string SerialSegment1 { get; set; }
            public string SerialSegment2 { get; set; }
            public string SerialSegment3 { get; set; }
            public string SerialSegment4 { get; set; }
            public string SerialSegment5 { get; set; }
            public ExternalReference ExternalReference { get; set; }
        }

        public class PriceLevel
        {
            public bool? Status { get; set; }
            public string PriceLevelName { get; set; }
            public string UnitOfMeasure { get; set; }
            public string Currency { get; set; }
            public double? Price { get; set; }
        }

        public class KitComponent
        {
            public int? Sequence { get; set; }
            public string ComponentItemId { get; set; }
            public double? ItemQuantity { get; set; }
            public double? StandardCost { get; set; }
            public List<KitComponentAttribute> KitComponentAttributes { get; set; }

            public class KitComponentAttribute
            {
                public string Attribute { get; set; }
                public string Value { get; set; }
            }
        }

        public class ItemSubstitution
        {
            public string SubstitutionItemId { get; set; }
        }

        public class ItemVendor
        {
            public string ProfileId { get; set; }
            public string VendorUrl { get; set; }
            public bool? IsManufacturer { get; set; }
        }
    }
}

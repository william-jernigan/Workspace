#nullable enable
using APITransformationsFunction.Model.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Search
{
    public class ONEXInventorySearchResponse
    {
        public string? Id { get; set; }
        public string? Source { get; set; }
        public string? Specversion { get; set; }
        public string? Type { get; set; }
        public string? CorrelationId { get; set; }
        public string? Datacontenttype { get; set; }
        public string? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTime Time { get; set; }
        public bool Ispartial { get; set; }
        public string? Authorization { get; set; }
        public ONEXSearchData? Data { get; set; }
    }

    public class ONEXSearchData
    {
        public SystemMeta? SystemMeta { get; set; }
        public ExternalReference? ExternalReference { get; set; }
        public PagedResult<InventoryItem>? InventoryItems { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ExternalReference
    {
        public string? SourceSystem { get; set; }
        public string? ReferenceId { get; set; }
    }

    public class InventoryItem
    {
        public string? ItemId { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemClass { get; set; }
        public string? ParentId { get; set; }
        public string? ParentCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? MaximumOccupancy { get; set; }
        public List<Media>? Media { get; set; }
        public List<ItemAttribute>? ItemAttributes { get; set; }
        public ItemAvailability? ItemAvailability { get; set; }
        public SupplementalAttributes? SupplementalAttributes { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Media
    {
        public string? MediaType { get; set; }
        public string? MediaURI { get; set; }
        public string? MediaDescription { get; set; }
    }

    public class ItemAttribute
    {
        public string? Type { get; set; } // Direction, Policy, Amenities, Occupancy, Alert, CancelPenalty
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }

    public class ItemAvailability
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? WeekNumber { get; set; }
        public string? Status { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? QuantityOnHand { get; set; }
        public int? QuantityInUse { get; set; }
        public int? QuantityAllocated { get; set; }
        public int? QuantityCommitted { get; set; }
        public List<ItemAttribute>? ItemAvailabilityAttributes { get; set; }
        public List<ItemSchedulePriceLevel> ItemSchedulePriceLevels { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class ItemAvailabilityAttribute
    {
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }

    public class ItemSchedulePriceLevel
    {
        public bool Status { get; set; } = true;
        public string? PriceLevel { get; set; }
        public string? Currency { get; set; }
        public double Price { get; set; }
        public string? PriceDescription { get; set; }
        public List<ItemAttribute>? SchedulePriceAttributes { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SchedulePriceAttribute
    {
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SupplementalAttributes
    {
        public string? PrimaryVendorProfileId { get; set; }
        public double StandardCost { get; set; }
        public List<ItemSerialNumber>? ItemSerialNumbers { get; set; }
        public bool KitPricedFromComponent { get; set; }
        public List<PriceLevel>? PriceLevels { get; set; }
        public List<KitComponent>? KitComponents { get; set; }
        public List<ItemSubstitution>? ItemSubstitutions { get; set; }
        public List<ItemVendor>? ItemVendors { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ItemSerialNumber
    {
        public string? SerialNumber { get; set; }
        public string? SerialSegment1 { get; set; }
        public string? SerialSegment2 { get; set; }
        public string? SerialSegment3 { get; set; }
        public string? SerialSegment4 { get; set; }
        public string? SerialSegment5 { get; set; }
        public ExternalReference? ExternalReference { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PriceLevel
    {
        public bool Status { get; set; }
        public string? PriceLevelName { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? Currency { get; set; }
        public double Price { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class KitComponent
    {
        public int? Sequence { get; set; }
        public string? ComponentItemId { get; set; }
        public double ItemQuantity { get; set; }
        public double StandardCost { get; set; }
        public List<KitComponentAttribute>? KitComponentAttributes { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class KitComponentAttribute
    {
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ItemSubstitution
    {
        public string? SubstitutionItemId { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ItemVendor
    {
        public string? ProfileId { get; set; }
        public string? VendorUrl { get; set; }
        public bool IsManufacturer { get; set; }
    }
}

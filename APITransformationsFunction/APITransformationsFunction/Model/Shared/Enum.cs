namespace APITransformationsFunction.Model.Shared
{
    public enum OrderByField
    {
        propertyUnitRate,
        unitRate,
        distance,
        property,
        rating,
        availability
    }

    public enum OrderByDirection
    {
        asc,
        desc,
        none
    }

    public enum RateTimeUnit
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public enum GeoDistanceUnit
    {
        Miles,
        Kilometers,
        Meters,
        Feet
    }
}

namespace APITransformationsFunction.Common
{
    public static class ApiConstants
    {
        public const string CLAPartnerCode = "CLA";
        public const string MemberEndpoint = "Member"; // To be changed to Member once the member service is deployed
        public const string Version = "v1"; // To be uncommented once the member service and catalog service is deployed
        public const string OrderServiceVersion = "v2";
        public const string APITransformationFunctionPrefix = "APITransformationFunction";
        public const string PropertyEndpoint = "Properties";
        public const string SearchEndpoint = "Search/inventory";
        public const string CancellationEndpoint = "reservations";
        public const string PublishedStatus = "Published";
        public const string ActiveStatus = "Active";
        public const string InactiveStatus = "Inactive";
        public const string ItemClassResort = "Resort";
        public const string ItemClassUnit = "Unit";
        public const string AmenityType = "Amenities";
        public const string WebsiteType = "Website";
        public const string CancelledStatus = "CANCELLED";
        public const string NotCancelledStatus = "NOT_CANCELLED";
        public const string TradingAgreementCode = "HGV-ARV-HGV";
        public const string SuccessMessage = "Reservation cancelled successfully";
        public const string FailureMessage = "Not able to cancel the reservation";
        public const string ErrorMessage = "Not able to cancel this time";
        public const string SpecVersion = "1.0";
        public const string DataContentType = "application/json";
        public const string Api = "api";
        public const string ActivateMember = "activateMember";
        public const string DeActivateMember = "deactivateMember";
        public const string LocationType = "RESERVATION OFFICE";
        public const string EmailType = "INFO";
        public const string PhoneTechType = "VOICE";
        public const string AddressType = "Home";
        public const string ChainCode = "CLA";
        public const string BrandCode = "HGV";
        public const string CheckInTime = "CheckInTime";
        public enum IncludeMedia
        {
            PROPERTIES_ONLY,
            NONE,
            PROPERTIES_UNITS_BOTH,
            UNITS_ONLY
        }
        public enum IncludeAmenities
        {
            PROPERTIES_ONLY,
            UNITS_ONLY,
            NONE,
            PROPERTIES_UNITS_BOTH
        }

        public enum MediaType
        {
            IMAGES,
            VIDEOS,
            IMAGES_VIDEOS_BOTH
        }
        public enum AgeQualifyingCode
        {
            Infant = 7,
            Child = 8,
            Teenager = 9,
            Adult = 10,
            Senior = 11,
            Other = 12
        }

        public enum SourceSystemType
        {
            PARTNER,
            EXCHANGE
        }

        public enum ResStatusType
        {
            RESERVED,
            CANCELLED,
            PENDING
        }
        public const string ReservationEndpoint = "Reservations";
        public const string ReservationUrl = "Order/reservations";
    }
}

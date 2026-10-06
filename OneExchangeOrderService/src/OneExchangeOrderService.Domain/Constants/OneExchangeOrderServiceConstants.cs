
namespace OneExchangeOrderService.Domain.Constants
{
    public static class OneExchangeOrderServiceConstants
    {
        public const string OrderServicePrefix = "ExchangeService";
        public const string OrderServiceReservationPrefix = "OrderServiceReservation";
        public const string MemberIdIdentifierString = "MEMBER_ID";
        public const string PropertyNameAttributeTypeString = "PROPERTY_NAME";
        public const string PropertyPhoneAttributeTypeString = "PROPERTY_PHONE";
        public const string PropertyAddressAttributeTypeString = "PROPERTY_ADDRESS";
        public const string MemberShipLevelAttributeTypeString = "MEMBERSHIP_LEVEL";
        public const string AbsoluteDeadlineAttributeTypeString = "ABSOLUTE_DEADLINE";
        public const string NonRefundableAttributeTypeString = "NON_REFUNDABLE";
        public const string PenaltyDescriptionAttributeTypeString = "PENALTY_DESCRIPTION";
        public const string AbsoluteDeadline = "AbsoluteDeadline";
        public const string NonRefundable = "NonRefundable";
        public const string PenaltyDescription = "PenaltyDescription";
        public const string PropertyUnitTypeString = "UNIT_TYPE";
        public const string PropertyImportantInfoString = "PROPERTY_INFO";
        public const string RefundPointsPaymentPurposeString = "POINTS_REFUND";
        public const string PropertyNameAttribute = "Sunshine Resort Dummy Property";
        public const string PropertyPhoneAttribute = "123456";
        public const string PropertyAddressAttribute = "Street Planato, California USA 34567";
        public const int MaxWaivedBookingsPerYear = 2;
        public const string PropertyUnitTypeAttribute = "1 Bedroom, Non Smomking";
        public const string PropertyImportantInfoAttribute = "Check in will be at 4pm";
        public const string CompleteResortCodeString = "CompleteResortCode";
        public const string BgvMemberIdentifierSource = "BGV";
        public const string ChoMemberIdentifierSource = "CHO";
        public const string BgvTenantCode = "BGV";
        public const string ChoPartnerCode = "CHO";
        public const string GpxPartnerCode = "GPX";
        public const string PostOrderInsertedBy = "Order-Service";
        public const string BgvTenantGuidId = "D4E5F6A7-B8C9-7D8E-2F3A-AB9D8E7F6A5B";
        public const string ChoTradingAgreementGuidId = "0B1DEA3C-D294-4F19-9BC6-4BB3422B9C21";
        public const string GpxTradingAgreementGuidId = "E97F7CF3-2237-42DC-9471-414864C7DC18";
        public const string PatchOrderUpdatedBy = "Durable-function";
        public const string InventoryServiceClientPrefix = "InventoryServiceClient";
        public const string IsoDateFormat = "yyyy-MM-dd";
        public const string ColumnLimit = "Data length exceeds column size.";
        public const string PenaltyDaysExpression = @"\d+";
        public const string StrictPenaltyDays = "{0} days";
        public const int CancelPolicyDays = 10;
        public const string ChoiceCancellationPolicyMessage = "<p> • Please note that reservations cancelled within {0} day(s) of arrival are nonrefundable. A modification or cancellation for this booking will result in a forfeiture of points. Please proceed below if you wish to continue with this booking.</p>";
        public const string OneExchangeCancellationPolicyMessage = "<p> • Please note that reservations cancelled within 10 days of arrival are nonrefundable and will result in a forfeiture of Points. Modifications require a cancel and rebook. A cancellation fee may apply.</p>";

        // V2 Cancellation
        public const string OrderServiceCancellationPrefix = "[OrderService:Cancellation]";
        public const string BoomiCancellationConnector = "BoomiConnector"; // If you want to log connector names
        public const string InventoryCancellation = "InventoryService";
        public const string CancellationConnectorClient = "CancellationConnectorClient";
        public const string CancellationCancelApiKeyName = "Reservation";
        public const string CancellationConnectorCancelEndpointUri = "v1/partner/reservation/cancel";
        public const string InventoryEndpointUri = "Properties/inventory/update";

        // Reservation
        public const string OrderServiceSource = "oneexchange-order-service";
        public const string CloudEventSpecVersion = "1.0";
        public const string ReservationGetResponseType = "Reservation-Get-Response";
        public const string ApplicationJson = "application/json";
        public const string ReservationGetSchema = "https://schemas.onetruth.example/reservations/response/2020-12";
        public const string TargetSystem = "HGV";
        public const string PriceLevelStandard = "STANDARD";

        public const string StgDeploymentEnviornment = "STG";
        /// <summary>
        /// Strings to get code from ItemCode
        /// </summary>
        public const string PartnerCodeString = "PartnerCode";
        public const string ResortCodeString = "ResortCode";
        public const string UnitTypeString = "UnitType";

        /// <summary>
        /// Strings to get code from AgreementTradingCode
        /// </summary>
        public const string TenantCodeString = "TenantCode";
        public const string BuyerPartnerCodeString = "BuyerPartnerCode";
        public const string SupplierPartnerCodeString = "SupplierPartnerCode";

        /// <summary>
        /// AllowedAttributeTypes
        /// </summary>
        public const string CheckInDateOrderItemAttributeTypeString = "CHECK_IN_DATE";
        public const string CheckOutDateOrderItemAttributeTypeString = "CHECK_OUT_DATE";
        public const string NumAdultsOrderItemAttributeTypeString = "NUM_ADULTS";
        public const string NumChildrenOrderItemAttributeTypeString = "NUM_CHILDREN";
        public const string MemberShipLevelOrderItemAttributeTypeString = "MEMBERSHIP_LEVEL";

        /// <summary>
        /// AllowedDigitalAddressType
        /// </summary>
        public const string EmailDigitalAddressTypeString = "EMAIL";


        public const string Currency = "Points";
        public const string Participant = "BXG";
        public const string BgvTenantId = "5AF97CCF-EC52-48B9-A131-03F4605EF0FA";
        public const string GuestEmail = "EMAIL";
        public const string SuccessfulUpdateMessage = "Order updated successfully";
        public const string OrderSameStatusUpdateMessage = "Order is already updated with same status";
        public const string OrderNotFoundUpdateMessage = "Order Not Found";
        public const string OrderStatusPath = "/orderStatus";
        public const string StatusReasonPath = "/statusReason";
        public const string OrderStatusUpdateFailureMessage = "We are currently experiencing issues with our processing. Please try again later.";
        public const string Billing = "BILLING";
        public const string Primary = "PRIMARY";
        public const string CancelledOrderStatus = "CANCELLED";
        public const string ConfirmedOrderStatus = "CONFIRMED";
        public const string OrderItemAttributeCancelPath = "/orderItemAttributes";


        public const string BGVBuyerPartnerCode = "BGV";
        public const string AllSupplierPartnerCode = "ALL";
        public const string MembershipClub = "Premier";
        public const string InRulePrefix = "InRuleService";
        public const string CancellationFee = "CancellationFee";
        public const string InRuleClient = "InRuleClient";
        public const string USD = "USD";
        public const string Active = "Active";
        public const string OneXChange = "OneXChange";
        public const string OrderCancelIdAbbreviation = "RET";

        public const string OrderPendingCancellationStatus = "PENDING_CANCELLATION";


        /// <summary>
        /// Client
        /// </summary>
        public const string InventoryServiceClient = "InventoryServiceClient";
        public const string BoomiConnectorClient = "BoomiConnectorClient";

        public const string SendPointsApiKeyName = "SendPoints";
        public const string ReservationApiKeyName = "Reservation";
        public const string ReservationConnectorCreateEndpointUri = "v1/partner/reservation/create";

        public const string ReservationCreateApiKeyName = "ReservationCreate";

        /// <summary>
        /// InventoryEndPointUri
        /// </summary>

        public const string ChoicePropertyInventoryAvailabilityEndpointUri = "Properties/choice/availability";
        public const string PropertyPriceCheckEndpointUri = "Properties/price/check";


        public const string Checkin = "CHECK_IN_DATE";
        public const string Checkout = "CHECK_OUT_DATE";
        public const string UNITTYPE = "UNIT_TYPE";
        public const string NUMADULTS = "NUM_ADULTS";
        public const string NUMCHILDREN = "NUM_CHILDREN";
        public const string FEEREIMBURSEMENT = "FEE_REIMBURSEMENT";
        public const string FEEREFUND = "FEE_REFUND";
        public const string ORDERFEE = "ORDER_FEE";
        public const string CancellationFeeTransactionType = "CANCELLATION_FEE";
        public const string POINTSREFUND = "POINTS_REFUND";
        public const string QaEnv = "qat";

        // RichTextHelper Constants
        public const string HTTPSPREFIX = "https://";
        public const string HTTPPREFIX = "http://";
        public const string MAILTOPREFIX = "mailto:";
        public const string TELPREFIX = "tel:";
    }
}

using APITransformationsFunction.Model.Reservation.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace APITransformationsFunction.Model.Reservation.Create.Request
{
    [ExcludeFromCodeCoverage]
    public class OTACreateReservationRequest
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public ReservationCandidates ReservationCandidates { get; set; }
    }

    public class ReservationCandidates
    {
        public List<SourceReservationId> SourceReservationIds { get; set; }
        public List<ReservationComment> ReservationComments { get; set; }
        public Properties Properties { get; set; }
        public List<Profile> Profiles { get; set; }
        public LoyaltyRedemption LoyaltyRedemption { get; set; }

    }
    public class Properties
    {
        public BasicPropertyInfoSummaryPostRequest BasicPropertyInfoSummary { get; set; }
    }

    public class BasicPropertyInfoSummaryPostRequest
    {
        public string PropertyCode { get; set; }
        public string PropertyCategory { get; set; }
        public string PropertySegment { get; set; }
        public List<UnitStay> UnitStays { get; set; }
    }

    public class UnitStay
    {
        public string UnitTypeCode { get; set; }
        public string UnitDescription { get; set; }

        public List<RatePlan> RatePlan { get; set; }
        public List<SpecialRequest> SpecialRequests { get; set; } = [];
        public List<RoomRate> RoomRates { get; set; }
        public List<GuestCount> GuestCounts { get; set; }
        public UnitStayDates UnitStayDates { get; set; }
        public Guarantee Guarantee { get; set; }
    }

    public class RatePlan
    {
        public string RatePlanCode { get; set; }
        public string EffectiveDate { get; set; }
        public bool GuaranteeRequired { get; set; }
    }

    public class SpecialRequest
    {

        public string RequestCode { get; set; }

        public string Text { get; set; }
    }

    public class RoomRate
    {
        public int NumberOfUnits { get; set; }
        public List<RateDetail> Rates { get; set; }
    }

    public class GuestCount
    {
        public int Count { get; set; }
        public int AgeQualifyingCode { get; set; }
    }

    public class UnitStayDates : IValidatableObject
    {
        public string Start { get; set; }
        public string End { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {

            // Validate Start date
            if (!DateTime.TryParseExact(Start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                yield return new ValidationResult(
                    "Start date must be a valid calendar date (format: YYYY-MM-DD).",
                    [nameof(Start)]);
            }

            // Validate End date
            if (!DateTime.TryParseExact(End, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                yield return new ValidationResult(
                    "End date must be a valid calendar date (format: YYYY-MM-DD).",
                    [nameof(End)]);
            }

            if (startDate != default && endDate != default && endDate < startDate)
            {
                yield return new ValidationResult(
                    "End date cannot be earlier than Start date.",
                    [nameof(End)]);
            }
        }
    }

    public class Guarantee
    {

        public string GuaranteeType { get; set; }
        public double? Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string Deadline { get; set; }
        public bool GuaranteedAccepted { get; set; }
    }

    public class RateDetail
    {
        public Base Base { get; set; }
        public List<Tax> Taxes { get; set; }
        public List<Fee> Fees { get; set; }
        public Total Total { get; set; }
    }
    public class Total
    {
        public double? AmountAfterTax { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
    }
    public class Base
    {
        public double? AmountBeforeTax { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
    }

    public class Tax
    {
        public double? Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int DecimalPlaces { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class Fee
    {
        public double? Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int DecimalPlaces { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
    }

    public class SourceReservationId
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }

    public class ReservationComment
    {
        public string Comment { get; set; }
        public string Category { get; set; }
    }

    public class LoyaltyRedemption
    {
        public string CertificateNumber { get; set; }
        public string ProgramName { get; set; }
        public string PromotionCode { get; set; }
        public string MembershipId { get; set; }
        public double RedemptionQuantity { get; set; }
        public string CurrencyCode { get; set; }
    }

}





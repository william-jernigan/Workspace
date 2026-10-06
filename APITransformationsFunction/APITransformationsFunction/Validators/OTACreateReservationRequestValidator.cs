using APITransformationsFunction.Model.Reservation.Create.Request;
using APITransformationsFunction.Model.Reservation.Shared;
using FluentValidation;
using System;
using System.Globalization;
using System.Linq;

namespace APITransformationsFunction.Validators
{
    public class OTACreateReservationRequestValidator : AbstractValidator<OTACreateReservationRequest>
    {
        public OTACreateReservationRequestValidator()
        {
            RuleFor(x => x.CorrelationID)
                .NotEmpty()
                .Length(36)
                .Matches(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");

            RuleFor(x => x.TradingAgreementCode)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.ReservationCandidates)
                .NotNull()
                .SetValidator(new ReservationCandidatesValidator());
        }
    }

    public class ReservationCandidatesValidator : AbstractValidator<ReservationCandidates>
    {
        public ReservationCandidatesValidator()
        {

            RuleFor(x => x.Properties)
                .NotNull()
                .SetValidator(new PropertiesValidator());

            RuleForEach(x => x.ReservationComments)
                .SetValidator(new ReservationCommentValidator())
                .When(x => x.ReservationComments != null);

            RuleFor(x => x.Profiles)
              .NotNull().WithMessage("Profiles is required.")
              .Must(list => list != null && list.Count >= 1 && list.Count <= 99)
              .WithMessage("Profiles must contain between 1 and 99 items.");

            RuleForEach(x => x.Profiles)
                .SetValidator(new ProfileValidator());

            RuleForEach(x => x.Profiles)
                .SetValidator(new ProfileValidator())
                .When(x => x.Profiles != null);

            RuleFor(x => x.LoyaltyRedemption)
                .SetValidator(new LoyaltyRedemptionValidator())
                .When(x => x.LoyaltyRedemption != null);

        }
    }

    public class ReservationCommentValidator : AbstractValidator<ReservationComment>
    {
        public ReservationCommentValidator()
        {
            RuleFor(x => x.Comment)
                .MaximumLength(500);

            RuleFor(x => x.Category)
                .NotEmpty()
                .MaximumLength(20)
                .Matches("^(?i)(GENERAL|TIMING|INTERNAL|PAYMENT|ARRIVAL|SPECIALREQUEST)$");
        }
    }

    public class PropertiesValidator : AbstractValidator<Properties>
    {
        public PropertiesValidator() => RuleFor(x => x.BasicPropertyInfoSummary)
                .NotNull()
                .SetValidator(new BasicPropertyInfoSummaryValidator());
    }

    public class BasicPropertyInfoSummaryValidator : AbstractValidator<BasicPropertyInfoSummaryPostRequest>
    {
        public BasicPropertyInfoSummaryValidator()
        {
            // propertyCode (required)
            RuleFor(x => x.PropertyCode)
                .NotEmpty()
                .MaximumLength(32);

            // propertyCategory (optional enum, case-insensitive)
            RuleFor(x => x.PropertyCategory)
                .MaximumLength(20)
                .Must(value => string.IsNullOrEmpty(value) ||
                    new[] { "5-STAR", "4-STAR", "3-STAR", "2-STAR", "1-STAR", "BOUTIQUE", "RESORT" }
                        .Contains(value.ToUpper()))
                .WithMessage("Invalid propertyCategory value.");


            // propertySegment (optional enum, case-insensitive)
            RuleFor(x => x.PropertySegment)
                .MaximumLength(20)
                .Must(value => string.IsNullOrEmpty(value) ||
                    new[] { "LEISURE", "BUSINESS", "URBAN", "RURAL", "COASTAL", "SKI" }
                        .Contains(value.ToUpper()))
                .WithMessage("Invalid propertySegment value.");

            // unitStays (required, 1–5 items)
            RuleFor(x => x.UnitStays)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("unitStays cannot be null.")
                .Must(list => list.Any())
                    .WithMessage("At least one unitStay is required.")
                .Must(list => list.Count <= 5)
                    .WithMessage("A maximum of 5 unitStays can be provided.");

            RuleForEach(x => x.UnitStays)
                .SetValidator(new UnitStayValidator());
        }
    }

    public class UnitStayValidator : AbstractValidator<Model.Reservation.Create.Request.UnitStay>
    {
        public UnitStayValidator()
        {
            RuleFor(x => x.UnitTypeCode)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.UnitDescription)
                .MaximumLength(500);

            // ratePlan (required, min 1)
            RuleFor(x => x.RatePlan)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("RatePlan cannot be null.")
                .Must(list => list.Any())
                    .WithMessage("At least one RatePlan is required.");

            RuleForEach(x => x.RatePlan)
                .SetValidator(new RatePlanValidator());


            // specialRequests (optional)
            RuleForEach(x => x.SpecialRequests)
                .SetValidator(new SpecialRequestValidator())
                .When(x => x.SpecialRequests != null);

            // roomRates (required)
            RuleFor(x => x.RoomRates)
                .NotNull().WithMessage("RoomRates cannot be null.")
                .Must(list => list != null && list.Any())
                .WithMessage("At least one RoomRate is required.");
            RuleForEach(x => x.RoomRates)
                .SetValidator(new RoomRateValidator());


            // guestCounts (required)
            RuleFor(x => x.GuestCounts)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("GuestCounts cannot be null.")
                .Must(list => list.Any()).WithMessage("At least one GuestCount is required.")
                .Must(list => list.Any(g => g.AgeQualifyingCode == 10 && g.Count > 0))
                .WithMessage("At least one adult (AgeQualifyingCode = 10) must be included.");

            RuleForEach(x => x.GuestCounts)
                .SetValidator(new GuestCountValidator());

            // unitStayDates (required)
            RuleFor(x => x.UnitStayDates)
                .NotNull().WithMessage("UnitStayDates are required.")
                .SetValidator(new UnitStayDatesValidator());

            // guarantee (optional)
            RuleFor(x => x.Guarantee)
                .SetValidator(new GuaranteeValidator())
                .When(x => x.Guarantee != null);
        }
    }
    public class GuaranteeValidator : AbstractValidator<Model.Reservation.Create.Request.Guarantee>
    {
        public GuaranteeValidator()
        {
            RuleFor(x => x.GuaranteeType)
                .NotEmpty()
                .MaximumLength(20)
                .Matches("^(?i)(CREDIT_CARD|DEPOSIT|PREPAYMENT|VOUCHER)$");

            RuleFor(x => x.Amount)
                .NotNull()
                .GreaterThanOrEqualTo(0)
                .WithMessage("Amount is required and must be greater than or equal to 0");

            RuleFor(x => x.CurrencyCode)
                .NotEmpty()
                .MaximumLength(3)
                .Matches("^(?i)[A-Z]{3}$");

            RuleFor(x => x.Deadline)
                 .Must(value => string.IsNullOrWhiteSpace(value) || DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out _))
                 .WithMessage("Deadline must be a valid ISO 8601 date-time format (e.g., 2025-10-31T23:59:59Z).");

        }
    }


    public class RatePlanValidator : AbstractValidator<Model.Reservation.Create.Request.RatePlan>
    {
        public RatePlanValidator()
        {
            RuleFor(x => x.RatePlanCode)
                .NotEmpty()
                .WithMessage("RatePlanCode is required.")
                .MaximumLength(10)
                .WithMessage("RatePlanCode cannot exceed 10 characters.");

            RuleFor(x => x.EffectiveDate)
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .When(x => !string.IsNullOrEmpty(x.EffectiveDate))
                .WithMessage("EffectiveDate must be in the format yyyy-MM-dd.");
        }
    }

    public class SpecialRequestValidator : AbstractValidator<Model.Reservation.Create.Request.SpecialRequest>
    {
        public SpecialRequestValidator()
        {
            RuleFor(x => x.RequestCode)
                .MaximumLength(10)
                .Matches("^(?i)(NS|SM|HC|CRIB|ROLLAWAY|LATE_CHECKOUT|EARLY_CHECKIN)$")
                .When(x => !string.IsNullOrEmpty(x.RequestCode));

            RuleFor(x => x.Text)
                .NotEmpty()
                .MaximumLength(200);
        }
    }

    public class GuestCountValidator : AbstractValidator<Model.Reservation.Create.Request.GuestCount>
    {
        private static readonly int[] ValidAgeCodes = [10, 8, 5, 3, 1];

        public GuestCountValidator()
        {
            RuleFor(x => x.Count)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Count must be greater than or equal to 0.");


            RuleFor(x => x.AgeQualifyingCode)
                .Must(code => ValidAgeCodes.Contains(code))
                .WithMessage($"AgeQualifyingCode must be one of: {string.Join(", ", ValidAgeCodes)}.");
        }
    }

    public class UnitStayDatesValidator : AbstractValidator<Model.Reservation.Create.Request.UnitStayDates>
    {
        public UnitStayDatesValidator()
        {
            // Start date validation
            RuleFor(x => x.Start)
                .NotEmpty()
                    .WithMessage("Start date is required.")
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                    .WithMessage("Start date must be in yyyy-MM-dd format.");

            RuleFor(x => x.Start)
                .Must(start =>
                {
                    if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var s))
                    {
                        return true; // skip if format invalid (previous rule handles it)
                    }

                    return s.Date > DateTime.UtcNow.Date; // must be after today
                })
                .When(x => !string.IsNullOrWhiteSpace(x.Start))
                .WithMessage("Start date cannot be today or a past date.");

            RuleFor(x => x.End)
                .NotEmpty()
                    .WithMessage("End date is required.")
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                    .WithMessage("End date must be in yyyy-MM-dd format.");


            // End >= Start
            RuleFor(x => x)
                .Must(x =>
                {
                    if (!DateTime.TryParseExact(x.Start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var s))
                    {
                        return true;
                    }

                    if (!DateTime.TryParseExact(x.End, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var e))
                    {
                        return true;
                    }

                    return e >= s;
                })
                .When(x =>
                    !string.IsNullOrWhiteSpace(x.Start) &&
                    !string.IsNullOrWhiteSpace(x.End))
                .WithMessage("End date cannot be earlier than Start date.");
        }
    }


    public class RoomRateValidator : AbstractValidator<Model.Reservation.Create.Request.RoomRate>
    {
        public RoomRateValidator()
        {
            RuleFor(x => x.NumberOfUnits)
                .InclusiveBetween(1, 10)
                .WithMessage("NumberOfUnits must be between 1 and 10.");

            RuleFor(x => x.Rates)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Rates cannot be null.")
            .Must(list => list.Any()).WithMessage("At least one RateDetail is required.");

            RuleForEach(x => x.Rates)
                .SetValidator(new RateDetailValidator());

        }
    }


    public class RateDetailValidator : AbstractValidator<Model.Reservation.Create.Request.RateDetail>
    {
        public RateDetailValidator()
        {

            RuleFor(x => x.Base)
                .NotNull().WithMessage("Base details under Rate are required.")
                .DependentRules(() => RuleFor(x => x.Base).SetValidator(new BaseValidator()));

            // Taxes validation
            RuleFor(x => x.Taxes)
                .NotNull().WithMessage("Taxes under rate cannot be null.")
                .Must(list => list != null && list.Any()).WithMessage("At least one Tax is required.")
                .DependentRules(() => RuleForEach(x => x.Taxes).SetValidator(new TaxValidator()));

            // Fees validation
            RuleFor(x => x.Fees)
                .NotNull().WithMessage("Fees under rate cannot be null.")
                .Must(list => list != null && list.Any()).WithMessage("At least one Fee is required.")
                .DependentRules(() => RuleForEach(x => x.Fees).SetValidator(new FeeValidator()));

            // Total validation
            RuleFor(x => x.Total)
                .NotNull().WithMessage("Total under rate is required.")
                .DependentRules(() => RuleFor(x => x.Total).SetValidator(new TotalValidator()));


            // Cross-property validation that ties the error to Total.AmountAfterTax           
            RuleFor(x => x.Total.AmountAfterTax)
            .Must((rateDetail, amountAfterTax) =>
                amountAfterTax >= rateDetail.Base.AmountBeforeTax)
            .WithMessage("AmountAfterTax must be greater than AmountBeforeTax")
            .When(x =>
                x.Base != null &&
                x.Total != null &&
                x.Base.AmountBeforeTax != null
            );

            // AmountAfterTax must equal AmountBeforeTax + total Fees + total Taxes, and all currency codes must match

            RuleFor(x => x.Total.AmountAfterTax)
             .Must((rate, amountAfterTax) =>
             {
                 if (!amountAfterTax.HasValue ||
                     rate.Base?.AmountBeforeTax == null ||
                     rate.Taxes == null ||
                     rate.Fees == null)
                 {
                     return true;
                 }

                 // 1. Validate all currency codes match
                 var allCurrencyCodes = new[]
                 {
                    rate.Base.CurrencyCode,
                    rate.Total.CurrencyCode
                 }
                 .Concat(rate.Taxes.Select(t => t.CurrencyCode))
                 .Concat(rate.Fees.Select(f => f.CurrencyCode))
                 .Where(c => c != null)
                 .ToList();

                 var allSameCurrency = allCurrencyCodes
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Count() == 1;

                 if (!allSameCurrency)
                 {
                     return false;
                 }

                 // 2. Calculate expected total
                 var baseAmount = rate.Base.AmountBeforeTax.Value;
                 var taxSum = rate.Taxes.Sum(t => t.Amount ?? 0);
                 var feeSum = rate.Fees.Sum(f => f.Amount ?? 0);

                 var expectedTotal = baseAmount + taxSum + feeSum;

                 // 3. Compare with AmountAfterTax
                 return Math.Abs(amountAfterTax.Value - expectedTotal) < 0.0001;
             })
             .WithMessage("AmountAfterTax must equal AmountBeforeTax + total Fees + total Taxes, and all currency codes must match.")
             .When(x => x.Total != null);

        }
    }

    public class BaseValidator : AbstractValidator<Model.Reservation.Create.Request.Base>
    {
        public BaseValidator()
        {
            RuleFor(x => x.AmountBeforeTax)
            .NotNull().WithMessage("AmountBeforeTax is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Amount Before Tax must be >= 0.");

            RuleFor(x => x.CurrencyCode)
                .NotEmpty().WithMessage("CurrencyCode under base is required.");

            RuleFor(x => x.CurrencyCode)
                .Matches("^(?i)[A-Z]{3}$")
                .WithMessage("CurrencyCode must be a valid 3-letter currency code.")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        }
    }
    public class TotalValidator : AbstractValidator<Model.Reservation.Create.Request.Total>
    {
        public TotalValidator()
        {
            RuleFor(x => x.AmountAfterTax)
            .NotNull().WithMessage("AmountAfterTax is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Amount After Tax must be >= 0.");

            RuleFor(x => x.CurrencyCode)
                .NotEmpty().WithMessage("CurrencyCode under total is required.");

            RuleFor(x => x.CurrencyCode)
                .Matches("^(?i)[A-Z]{3}$")
                .WithMessage("CurrencyCode must be a valid 3-letter currency code.")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        }
    }

    public class TaxValidator : AbstractValidator<Model.Reservation.Create.Request.Tax>
    {
        public TaxValidator()
        {
            RuleFor(x => x.Amount)
            .NotNull().WithMessage("Tax amount is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Tax amount must be >= 0.");

            RuleFor(x => x.CurrencyCode)
                 .NotEmpty().WithMessage("CurrencyCode under tax is required.");

            RuleFor(x => x.CurrencyCode)
                .Matches("^(?i)[A-Z]{3}$")
                .WithMessage("CurrencyCode must be a valid 3-letter currency code.")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        }
    }

    public class FeeValidator : AbstractValidator<Model.Reservation.Create.Request.Fee>
    {
        public FeeValidator()
        {
            RuleFor(x => x.Amount)
            .NotNull().WithMessage("Fee amount is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Fee amount must be >= 0.");

            RuleFor(x => x.CurrencyCode)
                .NotEmpty().WithMessage("CurrencyCode under fees is required.");

            RuleFor(x => x.CurrencyCode)
                .Matches("^(?i)[A-Z]{3}$")
                .WithMessage("CurrencyCode must be a valid 3-letter currency code.")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        }
    }


    public class ProfileValidator : AbstractValidator<Profile>
    {
        public ProfileValidator() => RuleFor(x => x.MemberProfile)
                .NotNull()
                .SetValidator(new MemberProfileValidator());
    }

    public class MemberProfileValidator : AbstractValidator<MemberProfile>
    {
        public MemberProfileValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(50).WithMessage("firstName cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("lastName is required.")
                .MaximumLength(50).WithMessage("lastName cannot exceed 50 characters.");

            RuleFor(x => x.DateOfBirth)
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.DateOfBirth))
                .WithMessage("dateOfBirth must be in YYYY-MM-DD format.")
                .Must(value => DateTime.TryParse(value, CultureInfo.InvariantCulture, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.DateOfBirth))
                .WithMessage("dateOfBirth must be a valid date.");


            RuleFor(x => x.Sex)
                .Matches("^(?i)(M|F|O|U|NB|PREFER_NOT_SAY)$")
                .When(x => !string.IsNullOrEmpty(x.Sex));

            RuleFor(x => x.ProfileType)
                .Matches("^(?i)(Individual|Group|Corporate|Agent)$")
                .When(x => !string.IsNullOrEmpty(x.ProfileType));

            RuleFor(x => x.IsOccupied)
                .NotNull();

            RuleFor(x => x.SourceSystemIds)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("SourceSystemIds cannot be null.")
                .Must(list => list.Count is >= 1 and <= 10).WithMessage("Must contain 1–10 items.")
                .Must(list => list != null && list.Any(s => s.SourceSystem.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase)))
                     .WithMessage("At least one SourceSystemId must have SourceSystem = EXCHANGE.");

            RuleForEach(x => x.SourceSystemIds)
                .SetValidator(new SourceSystemIdValidator());

            RuleFor(x => x.ContactInfo)
                .NotNull()
                .WithMessage("contactInfo is required.");

            RuleFor(x => x.ContactInfo)
                .SetValidator(new ContactInfoValidator())
                .When(x => x.ContactInfo != null);

        }
    }

    public class SourceSystemIdValidator : AbstractValidator<SourceSystemId>
    {
        public SourceSystemIdValidator()
        {
            RuleFor(x => x.SourceSystem)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^(?i)(EXCHANGE|PARTNER|INTERNAL|GDS|OTA|CORPORATE|AFFILIATE|WEBSITE)$")
            .WithMessage("Invalid SourceSystem value.");

            RuleFor(x => x.Id)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Id is required and cannot exceed 20 characters.");
        }
    }

    public class ContactInfoValidator : AbstractValidator<ContactInfo>
    {
        public ContactInfoValidator()
        {

            RuleFor(x => x.Addresses)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("addresses is required.")
            .Must(list => list != null && list.Any()).WithMessage("At least one address is required.")
            .Must(list => list != null && list.Count <= 5).WithMessage("Addresses cannot contain more than 5 items.");

            RuleForEach(x => x.Addresses)
                .SetValidator(new AddressValidator())
                .When(x => x.Addresses != null && x.Addresses.Any());

        }
    }

    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(x => x.AddressType)
                .MaximumLength(20)
             .Matches("^(?i)(Home|Business|Billing|Shipping|Temporary|Mailing|Vacation|Other)$")
             .WithMessage("AddressType must be one of: Home, Business, Billing, Shipping, Temporary, Mailing, Vacation, Other");

            RuleFor(x => x.AddressLine1)
                .MaximumLength(300);

            RuleFor(x => x.AddressLine2)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.AddressLine2));

            RuleFor(x => x.CityName)
                .MaximumLength(50);

            RuleFor(x => x.PostalCode)
                .MaximumLength(10);

            RuleFor(x => x.StateProv)
                .MaximumLength(2);

            RuleFor(x => x.State)
                .MaximumLength(100);

            RuleFor(x => x.CountryCode)
                .MaximumLength(4);

            RuleFor(x => x.Country)
                .MaximumLength(100);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude != null);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude != null);

            RuleFor(x => x.IsPrimary)
                .NotNull();
        }
    }


    public class LoyaltyRedemptionValidator : AbstractValidator<Model.Reservation.Create.Request.LoyaltyRedemption>
    {
        public LoyaltyRedemptionValidator()
        {
            RuleFor(x => x.CertificateNumber)
                .MaximumLength(20)
                .WithMessage("Certificate Number cannot exceed 20 characters.");

            RuleFor(x => x.ProgramName)
                .MaximumLength(20)
                .WithMessage("Program name cannot exceed 20 characters.");

            RuleFor(x => x.MembershipId)
                .MaximumLength(10)
                .WithMessage("Membership Id cannot exceed 10 characters.");

            RuleFor(x => x.PromotionCode)
                .MaximumLength(20)
                .WithMessage("Promotion code cannot exceed 20 characters.");

            RuleFor(x => x.RedemptionQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Redemption quantity must be 0 or higher.");

            RuleFor(x => x.CurrencyCode)
                .MaximumLength(3)
                .WithMessage("Currency code cannot exceed 3 characters.");
        }
    }
}

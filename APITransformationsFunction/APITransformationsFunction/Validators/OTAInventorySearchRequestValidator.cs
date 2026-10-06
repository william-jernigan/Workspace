using APITransformationsFunction.Model.Search;
using FluentValidation;
using System.Linq;

namespace APITransformationsFunction.Validators.Search
{
    public class OTAInventorySearchRequestValidator : AbstractValidator<OTAInventorySearchRequest>
    {
        public OTAInventorySearchRequestValidator()
        {
            RuleFor(x => x.CorrelationID)
                .NotEmpty().WithMessage("correlationID is missing")
                .Length(36).WithMessage("correlationID must be 36 characters long")
                .Matches(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")
                .WithMessage("correlationID must be a valid GUID.");

            RuleFor(x => x.TradingAgreementCode)
                .NotEmpty().WithMessage("tradingAgreementCode is required.")
                .DependentRules(() => RuleFor(x => x.TradingAgreementCode)
                    .MaximumLength(20).WithMessage("tradingAgreementCode must not exceed 20 characters."));

            RuleFor(x => x.PropertySearchCriteria)
                .NotNull().WithMessage("propertySearchCriteria is required.")
                .SetValidator(new OTAPropertySearchCriteriaValidator());
        }
    }

    public class OTAPropertySearchCriteriaValidator : AbstractValidator<OTAPropertySearchCriteria>
    {
        public OTAPropertySearchCriteriaValidator()
        {
            RuleFor(x => x.PropertyCodes).NotNull().WithMessage("propertyCodes is missing");

            When(x => x.PropertyCodes != null, () => RuleFor(x => x.PropertyCodes)
                    .Must(list => list.Count > 0)
                    .WithMessage("propertyCodes must contain at least one value."));

            When(x => x.PropertyCodes != null && x.PropertyCodes.Any(), () =>
            {
                RuleFor(x => x.PropertyCodes.Count)
                    .LessThanOrEqualTo(50).WithMessage("propertyCodes cannot contain more than 50 items.");

                RuleForEach(x => x.PropertyCodes)
                    .MaximumLength(10).WithMessage("Each propertyCode cannot exceed 10 characters.");
            });

            RuleFor(x => x.StayDateRange)
                .NotNull().WithMessage("stayDateRange is required.")
                .SetValidator(new OTAStayDateRangeValidator());

            When(x => x.GeoLocation != null, () => RuleFor(x => x.GeoLocation)
                    .SetValidator(new OTAGeoLocationValidator()));

            When(x => x.RateRange != null, () => RuleFor(x => x.RateRange)
                    .SetValidator(new OTARateRangeValidator()));

            RuleFor(x => x.GuestCounts)
                .NotNull().WithMessage("guestCounts is required.")
                .DependentRules(() => RuleFor(x => x.GuestCounts)
                    .Must(g => g.Count >= 1)
                        .WithMessage("guestCounts must contain at least one guest.")
                    .Must(g => g.Count <= 20)
                        .WithMessage("guestCounts cannot contain more than 20 guests."));

            RuleForEach(x => x.GuestCounts)
                .SetValidator(new OTAGuestCountValidator());

            When(x => x.LoyaltyPrograms != null, () => RuleForEach(x => x.LoyaltyPrograms)
                    .SetValidator(new OTALoyaltyProgramValidator()));
        }
    }

    public class OTAStayDateRangeValidator : AbstractValidator<OTAStayDateRange>
    {
        public OTAStayDateRangeValidator()
        {
            RuleFor(x => x.Start).NotEmpty().WithMessage("stayDateRange.start is required.");
            RuleFor(x => x.End).NotEmpty().WithMessage("stayDateRange.end is required.");

            RuleFor(x => x)
                .Must(x => x.End >= x.Start)
                .WithMessage("stayDateRange.end cannot be less than stayDateRange.start.")
                .When(x => x.End != default && x.Start != default);

            RuleFor(x => x.FlexibleDays)
                .InclusiveBetween(0, 10)
                .When(x => x.FlexibleDays.HasValue);
        }
    }

    public class OTAGeoLocationValidator : AbstractValidator<OTAGeoLocation>
    {
        public OTAGeoLocationValidator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.RadiusDistance).InclusiveBetween(0, 80);
            RuleFor(x => x.DistanceUnit).Equal("MI").WithMessage("distanceUnit must be 'MI'.");
        }
    }
    public class OTARateRangeValidator : AbstractValidator<OTARateRange>
    {
        public OTARateRangeValidator()
        {
            RuleFor(x => x.MinRate).GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxRate).GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxRate)
                .GreaterThanOrEqualTo(x => x.MinRate)
                .WithMessage("rateRange.maxRate must be greater than or equal to minRate.");

            RuleFor(x => x.CurrencyCode)
                .NotEmpty().WithMessage("currencyCode is required for rateRange.")
                .Matches("^USD$").WithMessage("currencyCode must be USD.");

            RuleFor(x => x.RateTimeUnit)
                .IsInEnum().WithMessage("Invalid value for rateTimeUnit.");
        }
    }

    public class OTAGuestCountValidator : AbstractValidator<OTAGuestCount>
    {
        private static readonly int?[] ValidCodes = [7, 8, 9, 10, 11, 12];

        public OTAGuestCountValidator()
        {
            RuleFor(x => x.Count)
                .NotNull().WithMessage("count under guestCounts is required")
                .GreaterThanOrEqualTo(1).WithMessage("guestCounts.count must be at least 1.");

            RuleFor(x => x.AgeQualifyingCode)
                .NotNull().WithMessage("ageQualifyingCode under guestCounts is required")
                .Must(code => ValidCodes.Contains(code))
                .WithMessage("guestCounts.ageQualifyingCode must be one of: 7, 8, 9, 10, 11, 12.");
        }
    }
    public class OTALoyaltyProgramValidator : AbstractValidator<OTALoyaltyProgram>
    {
        public OTALoyaltyProgramValidator() => RuleFor(x => x.ProgramCode)
                .NotEmpty().WithMessage("programCode is required when loyaltyPrograms is provided.")
                .MaximumLength(10);
    }

}

using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Reservation.Search;
using FluentValidation;
using System;
using System.Linq;

namespace ExchangeReservationFunction.Validators
{
    public class OTASearchReservationRequestValidator : AbstractValidator<OTASearchReservationRequest>
    {
        private static readonly string[] AllowedStatuses = ["CONFIRMED", "CANCELLED", "PENDING"];
        public OTASearchReservationRequestValidator()
        {
            // CorrelationID
            RuleFor(x => x.CorrelationID)
                .NotEmpty()
                .Length(36)
                .Matches(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")
                .WithMessage("correlationID must be a valid UUID.");

            // TradingAgreementCode
            RuleFor(x => x.TradingAgreementCode)
                .NotEmpty()
                .MaximumLength(20);

            // Status
            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Status must be one of: CONFIRMED, CANCELLED, PENDING.");


            // SourceReservationIds
            RuleFor(x => x.SourceReservationIds)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("Only up to 10 SourceReservationId's can be provided.");

            RuleForEach(x => x.SourceReservationIds)
                .SetValidator(new OTASourceReservationIdValidator());

            // SourceMemberIds
            RuleFor(x => x.SourceMemberIds)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("Only up to 10 SourceMemberId's can be provided.");

            RuleForEach(x => x.SourceMemberIds)
                .SetValidator(new OTASourceMemberIdValidator());

            // OrderBy
            RuleFor(x => x.OrderBy)
                .SetValidator(new OTAOrderByCriteriaValidator())
                .When(x => x.OrderBy != null);

            // Dates: must be in yyyy-MM-dd format
            RuleFor(x => x.ReservationDate)
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.ReservationDate))
                .WithMessage("ReservationDate must be in format yyyy-MM-dd.");

            RuleFor(x => x.ArrivalDate)
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.ArrivalDate))
                .WithMessage("ArrivalDate must be in format yyyy-MM-dd.");

            // At least one search criterion
            RuleFor(x => x)
                .Must(x =>
                    (x.SourceReservationIds != null && x.SourceReservationIds.Count != 0) ||
                    (x.SourceMemberIds != null && x.SourceMemberIds.Count != 0) ||
                    !string.IsNullOrWhiteSpace(x.Status) ||
                    !string.IsNullOrWhiteSpace(x.ReservationDate) ||
                    !string.IsNullOrWhiteSpace(x.ArrivalDate))
                .WithMessage("At least one search criterion (reservationId, memberId, status, reservationDate, or arrivalDate) must be provided.");
        }
    }

    // Nested validators with unique names
    public class OTASourceReservationIdValidator : AbstractValidator<SourceReservationId>
    {
        public OTASourceReservationIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.SourceSystem).NotEmpty();
        }
    }

    public class OTASourceMemberIdValidator : AbstractValidator<OTASearchReservationRequest.SourceMemberId>
    {
        public OTASourceMemberIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.SourceSystem).NotEmpty();
        }
    }

    public class OTAOrderByCriteriaValidator : AbstractValidator<OTASearchReservationRequest.OrderByCriteria>
    {
        public OTAOrderByCriteriaValidator()
        {
            RuleFor(x => x.Field).MaximumLength(50);
            RuleFor(x => x.Direction).MaximumLength(10);
        }
    }
}

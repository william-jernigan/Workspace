using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail
{
    [ExcludeFromCodeCoverage]
    public class OrderDetailsResponse
    {
        public DateTime? BookingDate { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
        public string UnitType { get; set; }
        public string NumberOfAdults { get; set; }
        public string NumberOfChildren { get; set; }
        public decimal? BookingFees { get; set; }
        public decimal? Taxes { get; set; }
        public string ImportantInformation { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PropertyName { get; set; }
        public string PropertyPhone { get; set; }
        public string PropertyAddress { get; set; }
        public string ItemCode { get; set; }
        public decimal? TotalPoints { get; set; }
        public string PropertyInfo { get; set; }

        public string CancellationPolicy { get; set; } = string.Empty;
        public List<Guid> ProfileIds { get; set; }
    }
}

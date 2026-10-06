using System;

namespace ExchangeReservationFunction.Model.Common
{
    public class ReservationError
    {
        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? ErrorDateTime { get; set; }

        public string? ErrorType { get; set; }
        public string? ErrorTitle { get; set; }

        public string? ErrorInstance { get; set; }
    }
}

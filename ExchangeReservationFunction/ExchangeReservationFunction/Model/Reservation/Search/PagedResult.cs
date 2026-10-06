using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeReservationFunction.Model.Reservation.Search
{
    [ExcludeFromCodeCoverage]
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / PageSize);
    }
}

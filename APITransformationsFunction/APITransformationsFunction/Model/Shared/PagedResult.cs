using System;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Shared
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                {
                    return 0; // or return 1 if you prefer at least 1 page
                }

                return (int)Math.Ceiling((decimal)TotalItems / PageSize);
            }
        }
    }
}

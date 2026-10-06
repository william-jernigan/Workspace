#nullable enable
using System;

namespace APITransformationsFunction.Model.Shared
{
    public class Error
    {
        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? ErrorDateTime { get; set; }

        public string? ErrorType { get; set; }
        public string? ErrorTitle { get; set; }

        public string? ErrorInstance { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace APITransformationsFunction.Model.Shared
{
    public class GuestCount
    {
        [Range(1, int.MaxValue)]
        public required int Count { get; set; }

        [RegularExpression(@"^(7|8|9|10|11|12)$")]
        public required int AgeQualifyingCode { get; set; }
    }
}

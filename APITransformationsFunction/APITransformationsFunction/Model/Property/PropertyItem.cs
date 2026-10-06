using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Property
{
    [ExcludeFromCodeCoverage]
    public class PropertyItem
    {
        public string PropertyCode { get; set; }
        public List<string> UnitTypeCodes { get; set; }
    }
}

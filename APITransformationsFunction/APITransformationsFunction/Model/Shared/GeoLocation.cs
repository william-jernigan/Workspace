using System.ComponentModel.DataAnnotations;

namespace APITransformationsFunction.Model.Shared
{
    public class GeoLocation
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(0, 80)]
        public double RadiusDistance { get; set; }

        [RegularExpression("MI")]
        public required string DistanceUnit { get; set; }
    }
}

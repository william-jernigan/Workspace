//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace APITransformationsFunction.Model.Member
//{
//    public class OTAUpdateMemberRequest
//    {
//        [Required]
//        [JsonPropertyName("operations")]
//        public List<MemberPatchOperation> Operations { get; set; }
//    }

//    public class MemberPatchOperation : IValidatableObject
//    {
//        [Required]
//        [JsonPropertyName("op")]
//        [RegularExpression("^(?i)(add|replace|remove|test|move|copy)$", ErrorMessage = "Invalid operation type in update member request.")]
//        public string Op { get; set; }

//        [Required]
//        [JsonPropertyName("path")]
//        public string Path { get; set; }

//        [JsonPropertyName("value")]
//        public object Value { get; set; }

//        //The value to be used for the operation (required for add, replace, test, copy, move).
//        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
//        {
//            if (Op != null && Op != "remove" && Value == null)
//            {
//                yield return new ValidationResult(
//                    $"Value is required for operation '{Op}' in Update Member Request.",
//                    new[] { nameof(Value) });
//            }
//        }
//    }


//}

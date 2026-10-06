#nullable enable
namespace APITransformationsFunction.Model.Shared
{
    public class MemberResponseMessage
    {
        public required string Message { get; set; }

        public string? MemberStatus { get; set; }

        public MemberInfo? MemberInfo { get; set; }
    }
}

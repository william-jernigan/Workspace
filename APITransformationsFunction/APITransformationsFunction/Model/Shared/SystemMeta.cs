namespace APITransformationsFunction.Model.Shared
{
    public class SystemMeta
    {
        public string TenantID { get; set; }
        public string InsertedDateTime { get; set; }
        public string InsertedBy { get; set; }
        public string UpdatedDateTime { get; set; }
        public string UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string StatusReason { get; set; }
    }
}

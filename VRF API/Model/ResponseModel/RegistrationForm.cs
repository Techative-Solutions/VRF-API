namespace VRF_API.Model.ResponseModel
{
    public class RegistrationForm
    {

    }
    public class ApprovalResponse
    {
        public string? TradeName { get; set; }
        public string? BusinessState { get; set; }
        public string? NatureOfBusiness { get; set; }
        public string? GstNumber { get; set; }
        public string? AppliedDate { get; set; }

        public string? WaitingorApproval { get; set; }
        public string? DepartmentLevel { get; set; }
    }

    public class DraftResponse
    {
        public string? TradeName { get; set; }
        public string? BusinessState { get; set; }
        public string? NatureOfBusiness { get; set; }
        public string? GstNumber { get; set; }
        public string? AppliedDate { get; set; }
    }

    public class PendingResponse
    {
        public string? TradeName { get; set; }
        public string? BusinessState { get; set; }
        public string? NatureOfBusiness { get; set; }
        public string? GstNumber { get; set; }
        public string? AppliedDate { get; set; }
        public string? WaitingorApproval { get; set; }
        public string? DepartmentLevel { get; set; }
    }

    public class CompletedResponse
    {
        public string? TradeName { get; set; }
        public string? BusinessState { get; set; }
        public string? NatureOfBusiness { get; set; }
        public string? GstNumber { get; set; }
        public string? AppliedDate { get; set; }
        public string? ApprovedDate { get; set; }
    }

    public class RejectedResponse1
    {
        public string? TradeName { get; set; }
        public string? BusinessState { get; set; }
        public string? NatureOfBusiness { get; set; }
        public string? GstNumber { get; set; }
        public string? AppliedDate { get; set; }

        public string? RejectedDate { get; set; }
        public string? RejectedReason { get; set; }
    }

    public class SapResponse
    {
        public string? VendorName { get; set; }
        public string? TradeName { get; set; }
        public string? GstNumber { get; set; }
        public string? SaprejReson { get; set; }
    }

    public class CreatedVendorResponse
    {
        public string? CardCode { get; set; }
        public string? GstNumber { get; set; }
    }
}

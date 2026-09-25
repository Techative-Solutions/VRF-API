namespace VRF_API.Model.ResponseModel
{
    public class RejectedResponse
    {
        public string TradeName { get; set; } = string.Empty;
        public string BusinessState { get; set; } = string.Empty;
        public string NatureOfBusiness { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string AppliedDate { get; set; } = string.Empty;
        public string RejectedDate { get; set; } = string.Empty;
        public string RejectedReason { get; set; } = string.Empty;

    }
    public class Reports
    {
        public string ReportName { get; set; } = string.Empty;

    }
        public class ReportRequest
        {
            public string ReportName { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public string UserName { get; set; }
        }

    public class GetListRequest
    {
        public string type { get; set; }
    }

}

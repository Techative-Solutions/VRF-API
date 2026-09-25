namespace VRF_API.Model.RequestModel
{
    public class PushtosapRequest
    {
        public string GroupCode { get; set; }
       
        public string VendorName { get; set; }
        public string vendorType { get; set; }

        public string gstNumber { get; set; }
        public string UserName { get; set; }

      

    }
    public class ApprovalRequest1
    {
        public string Remarks { get; set; }

        public string GstNumber { get; set; }
        public string UserName { get; set; }
       



    }
    public class RejectRequest
    {
        public string Reason { get; set; }

        public string GstNumber { get; set; }
        public string UserName { get; set; }

        public string Status { get; set; }
      



    }
}

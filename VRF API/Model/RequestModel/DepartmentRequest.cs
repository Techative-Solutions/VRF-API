namespace VRF_API.Model.RequestModel
{
    public class DepartmentRequest
    {
        public string DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        //public string IsActive { get; set; }
        public bool Active { get; set; }
    }

    public class DeleteRequest
    {
        public string ID { get; set; }
        public string Type { get; set; }
        
    }
}

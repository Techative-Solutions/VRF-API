namespace VRF_API.Model.RequestModel
{
    public class UserRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string UserMail { get; set; }
        public string Mobileno { get; set; }
        public bool Active { get; set; }
        public string? FileName { get; set; }
        public string? Profileupload { get; set; }
        public string Department { get; set; }
        public string Level { get; set; }
    }
}

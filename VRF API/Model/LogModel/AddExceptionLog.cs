namespace VRF_API.Model.LogModel
{
    public class AddExceptionLog
    {
        public string AppType { get; set; }
        public string MethodName { get; set; }
        public int ModuleID { get; set; }
        public string ExMessage { get; set; }
        public string Parameters { get; set; }
        public string StackTrace { get; set; }
        public string UserID { get; set; }
    }
}

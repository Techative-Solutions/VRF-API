using static VRF_API.Model.ResponseModel.EnumResponse;

namespace VRF_API.Model.ResponseModel
{
    public class ApiResponse
    {
        public ApiStatusEnum Status { get; set; }
        public string Message { get; set; }
        public ErrorCodeEnum ErrorCode { get; set; }
        public object Data { get; set; }
        public List<string> Datas { get; set; }// Can be used to send additional data if needed
    }
}

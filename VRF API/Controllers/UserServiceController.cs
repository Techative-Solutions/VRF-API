using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Model.RequestModel;
using VRF_API.Model.ResponseModel;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserServiceController : ControllerBase
    {

        private readonly IUserService _userService;
        public UserServiceController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _userService.Login(
                request.Username,
                request.Password
            );

            return Ok(response);
        }

        [HttpPost]
        [Route("ResetPassword")]
        public async Task<ActionResult> ResetPassword([FromBody] LoginRequest request)
        {
            var response = await _userService.ResetPassword(
                request.Username,
                request.OldPassword,
                request.NewPassword
            );

            return Ok(response);
        }
        [HttpPost("GetList")]
        public async Task<IActionResult> GetList([FromBody] GetListRequest request)
        {
            var response = await _userService.GetList(request);
            return Ok(response);
        }


        [HttpPost("Department")]
        public async Task<IActionResult> Department()
        {
            var response = await _userService.Department();
            return Ok(response);
        }


        [HttpPost]
        [Route("SaveDepartment")]
        public async Task<ActionResult> SaveDepartment( DepartmentRequest departmentRequest)
        {
            var response = await _userService.SaveDepartment( departmentRequest);

            return Ok(response);
        }

        [HttpPost]
        [Route("CommonDelete")]
        public async Task<ActionResult> CommonDelete(DeleteRequest deleteRequest)
        {
            var response = await _userService.CommonDelete(deleteRequest);

            return Ok(response);
        }

        [HttpPost]
        [Route("CreatUser")]
        public async Task<ActionResult> SaveUserDetails(UserRequest userRequest)
        {
            var response = await _userService.SaveUserDetails(userRequest);

            return Ok(response);
        }

        [HttpGet("GetUserDetails")]
        public async Task<IActionResult> GetUserDetails(string UserID)
        {
            var response = await _userService.GetUserDetails(UserID);
            return Ok(response);
        }

        [HttpPost]
        [Route("UpdateUser")]
        public async Task<ActionResult> Update(UserRequest userRequest)
        {
            var response = await _userService.Update(userRequest);

            return Ok(response);
        }
       

    }
}

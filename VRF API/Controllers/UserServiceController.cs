using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    }
}

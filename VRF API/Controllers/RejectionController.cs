using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Model.ResponseModel;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RejectionController : ControllerBase
    {
        private readonly IRejectionForm _rejectionForm;
        public RejectionController(IRejectionForm rejectionForm)
        {
            _rejectionForm = rejectionForm;
        }


        [HttpGet]
        [Route("RejectedDetails")]
        public async Task<ActionResult> RejectedDetails(string UserName)
        {
            var response = await _rejectionForm.RejectedDetails(UserName);
            return Ok(response);
        }
       
    }
}

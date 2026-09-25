using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Model.RequestModel;
using VRF_API.Model.ResponseModel;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationForm _rejistrationForm;
        public RegistrationController(IRegistrationForm rejistrationForm)
        {
            _rejistrationForm = rejistrationForm;
        }

        [HttpGet]
        [Route("RejistrationDetails")]
        public async Task<ActionResult> RejistrationDetails(string UserNme, string status)
        {
            var response = await _rejistrationForm.RejistrationDetails(UserNme, status);
            return Ok(response);
        }

        [HttpPost]
        [Route("PushToSAP")]
        public async Task<ActionResult> PushToSAP(PushtosapRequest request)
        {
            var response = await _rejistrationForm.PushToSAP(
                request.GroupCode,
                   request.VendorName,
                      request.vendorType,
                         request.gstNumber,
                            request.UserName

                );

            return Ok(response);
        }

        [HttpGet]
        [Route("GroupCode")]
        public async Task<ActionResult> GroupCode(string UserName)
        {
            var response = await _rejistrationForm.GroupCode(UserName);
            return Ok(response);
        }

        [HttpPost]
        [Route("Approved")]
        public async Task<ActionResult> Approved(ApprovalRequest1 request)
        {
            var response = await _rejistrationForm.Approved(
                request.Remarks,
                   request.UserName,
                      request.GstNumber
                         

                );

            return Ok(response);
        }

        [HttpPost]
        [Route("Reject")]
        public async Task<ActionResult> Reject(RejectRequest request)
        {
            var response = await _rejistrationForm.Reject(
                request.Reason,
                   request.GstNumber,
                      request.UserName,
                         request.Status

                );

            return Ok(response);
        }


       


    }
}

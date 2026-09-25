using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Model.RequestModel;
using VRF_API.Model.ResponseModel;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        public ApprovalController(IApprovalService approvalService)
        {
            _approvalService = approvalService;


        }

        [HttpPost]
        [Route("SaveApproval")]
        public async Task<ActionResult> SaveApproval(ApprovalRequest approvalRequest)
        {
            var response = await _approvalService.SaveApproval(approvalRequest);

            return Ok(response);
        }
        
    }
}

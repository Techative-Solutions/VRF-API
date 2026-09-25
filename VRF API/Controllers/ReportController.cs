using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Model.ResponseModel;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReport _report;
        public ReportController(IReport report)
        {
            _report = report;
        }


        [HttpGet]
        [Route("ReportName")]
        public async Task<ActionResult> ReportName()
        {
            var response = await _report.ReportName();
            return Ok(response);
        }

        [HttpPost("GetReport")]
        public async Task<IActionResult> GetReport([FromBody] ReportRequest request)
        {
            var data = await _report.GetReportAsync(request);
            return Ok(data);
        }

    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using VRF_API.Services;
using static VRF_API.Repository.CommonRepo;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorCreationController : ControllerBase
    {
        private readonly IVendorCreationService _vendorCreationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpContext context;
        private readonly ISession session;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SessionManager _sessionManager;
        public VendorCreationController(IVendorCreationService vendorCreationService, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, IConfiguration configuration, SessionManager sessionManager)
        {
            _vendorCreationService = vendorCreationService;
            _httpContextAccessor = httpContextAccessor;
            context = _httpContextAccessor.HttpContext;
            session = context?.Session;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _sessionManager = sessionManager;
        }
        [HttpGet]
        [Route("LoadInitialValues")]
        public async Task<ActionResult> LoadInit()
        {
            var response = await _vendorCreationService.LoadInitialValues();
            return Ok(response);
        }
        [HttpPost]
        [Route("UploadKYCFiles")]
        public async Task<ActionResult> UploadKYCFiles(UploadKYCFileRequest request)
        {
            var response = await _vendorCreationService.UploadKycFile(request.file, request.documentType, request.rowIndex);
            return Ok(response);
        }
        //[HttpPost]
        //[Route("ViewKYCFile")]
        //public async Task<ActionResult> ViewKYCFile(ViewKYCFile request)
        //{
        //    var response = await _vendorCreationService.ViewKYCFile(request.fileName, request.gstNumber, request.documentType);
        //    return Ok(response);
        //}
        [HttpGet]
        [Route("ViewKYCFile")]
        public async Task<IActionResult> ViewKYCFile(
            [FromQuery] string fileName,
            [FromQuery] string? gstNumber,
            [FromQuery] string documentType)
        {
            var response = await _vendorCreationService.ViewKYCFile(
               fileName,
                gstNumber ?? "",
                documentType);

            if (response == null || response.Data == null)
            {
                return NotFound("File not found.");
            }

            var fileResult = response.Data as FileResultModel;

            if (fileResult == null || fileResult.FileBytes == null || fileResult.FileBytes.Length == 0)
            {
                return NotFound("File not found.");
            }

            return File(
                fileResult.FileBytes,
                GetContentType(fileResult.FileName),
                enableRangeProcessing: true);
        }

        [HttpGet]
        [Route("DownloadKYCFile")]
        public async Task<IActionResult> DownloadKYCFile(
    [FromQuery] string fileName,
    [FromQuery] string? gstNumber,
    [FromQuery] string documentType)
        {
            var response = await _vendorCreationService.DownloadKYCFile(
                fileName,
                gstNumber ?? "",
                documentType);

            if (response == null || response.Data == null)
            {
                return NotFound("No file available to download.");
            }

            var fileResult = response.Data as FileResultModel;

            if (fileResult == null ||
                fileResult.FileBytes == null ||
                fileResult.FileBytes.Length == 0)
            {
                return NotFound("No file available to download.");
            }

            return File(
                fileResult.FileBytes,
                GetContentType(fileResult.FileName),
                fileResult.FileName);
        }

        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

       

    }
}

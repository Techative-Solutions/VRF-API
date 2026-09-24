using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using VRF_API.Services;
using static VRF_API.Model.ResponseModel.EnumResponse;
using VRF_API.Utilities;
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

        [HttpPost]
        [Route("NextPageCheck")]
        public async Task<IActionResult> NextPageCheck(NextPageCheckRequest request)
        {
            var response = await _vendorCreationService.NextPageCheck(request.gstNumber, request.page);
            return Ok(response);
        }

        [HttpPost]
        [Route("GstNumberCheck")]
        public async Task<IActionResult> GSTNumberCheck(GstNumberCheckRequest request)
        {
            var response = await _vendorCreationService.GSTNumberCheck(request.gstNumber);
            return Ok(response);
        }
        [HttpPost]
        [Route("SaveDraft")]
        public async Task<IActionResult> SaveDraft(SaveDraftRequest request)
        {
            var response = await _vendorCreationService.SaveDraft(request.Page, request.FormData, request.UploadedFiles);
            return Ok(response);
        }
        [HttpGet("GetStatesByCountry")]
        public IActionResult GetStatesByCountry(string countryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(countryCode))
                {
                    return BadRequest(
                        ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Failure,
                            "Country code is required",
                            null
                        )
                    );
                }

                var states =
                    _vendorCreationService.GetStatesByCountry(countryCode);

                return Ok(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "States fetched successfully",
                        states
                    )
                );
            }
            catch (Exception ex)
            {
                

                return StatusCode(
                    500,
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Error while fetching states",
                        null
                    )
                );
            }
        }
        [HttpPost("SendOTP")]
        public async Task<IActionResult> SendOTP(
     [FromBody] SendOtpRequest request)
        {
            try
            {
                await _vendorCreationService.SendOTP(request);

                return Ok(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "OTP sent successfully.",
                        null));
            }
            catch (Exception ex)
            {
               

                return BadRequest(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        ex.Message,
                        null));
            }
        }


        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP(
            [FromBody] VerifyOtpRequest request)
        {
            try
            {
                await _vendorCreationService.VerifyOTP(request);

                return Ok(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "OTP verified successfully.",
                        null));
            }
            catch (Exception ex)
            {
               
                return BadRequest(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        ex.Message,
                        null));
            }
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


        [HttpPost("SubmitVendor")]
        public async Task<IActionResult> SubmitVendor(
    [FromBody] SubmitVendorRequest request)
        {
            try
            {
               

                var result = await _vendorCreationService.SubmitVendor(request);

                return Ok(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        result.Message,
                        result
                    )
                );
            }
            catch (Exception ex)
            {
                
                return BadRequest(
                    ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        ex.Message,
                        null
                    )
                );
            }
        }
    }
}

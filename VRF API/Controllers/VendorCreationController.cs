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

    }
}

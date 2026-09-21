using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRF_API.Services;

namespace VRF_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomePageController : ControllerBase
    {
        private readonly IHomePageService _homePageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpContext context;
        private readonly ISession session;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SessionManager _sessionManager;
        public HomePageController(IHomePageService homePageService, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, IConfiguration configuration, SessionManager sessionManager)
        {
            _homePageService = homePageService;
            _httpContextAccessor = httpContextAccessor;
            context = _httpContextAccessor.HttpContext;
            session = context?.Session;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _sessionManager = sessionManager;
        }

        [HttpPost]
        [Route("SearchGSTNumber")]
        public async Task<ActionResult> SearchGSTNumber([FromBody] string gstNumber)
        {
            var response = await _homePageService.SearchGSTNumber(gstNumber);
            return Ok(response);
        }
    }
}

using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using VRF_API.Authentication;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;
using DbConnection = VRF_API.Repository.DbConnection;

namespace VRF_API.Services
{
    public interface IHomePageService
    {
        Task<ApiResponse> SearchGSTNumber(string gstNumber);
    }
    public class HomePageService : IHomePageService
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _companyDb;
        private readonly string _sapUser;
        private readonly string _sapPassword;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string connectionString;
        private readonly HttpContext context;
        private readonly ISession session;
        private readonly DbConnection db;
        private readonly IRequestContext _requestContext;
        private readonly string _URL;
        private readonly SessionManager _sessionManager;
        private readonly Log log;
        public HomePageService(IConfiguration configuration, OdbcConnection connection,IHttpContextAccessor httpContextAccessor, DbConnection _db, IRequestContext requestContext, SessionManager sessionManager, Log _log)
        {
            _configuration = configuration;
            _baseUrl = _configuration.GetValue<string>("SAPApiUrl:CusCreationUrl") ?? "";
            _companyDb = _configuration.GetValue<string>("SAPApiUrl:CompanyDB") ?? "";
            _sapUser = _configuration.GetValue<string>("SAPApiUrl:UserName") ?? "";
            _sapPassword = _configuration.GetValue<string>("SAPApiUrl:Password") ?? "";
            _httpContextAccessor = httpContextAccessor;
            context = _httpContextAccessor.HttpContext;
            connectionString = _configuration.GetValue<string>("SqlConnectionStrings:SqlOdbc") ?? string.Empty;
            db = _db;
            session = context.Session;
            _URL = _configuration.GetValue<string>("AppSettings:LoginURL") ?? string.Empty;
            _sessionManager = sessionManager;
            log = _log;
        }

        public async Task<ApiResponse> SearchGSTNumber(string gstNumber)
        {
            string functionName = "Search_GST_Number";
            log.WriteToLogFile_Debug($"{functionName} - Starting the function", functionName);
            log.WriteToLogFile_Debug($"{functionName} - Entered GST Number: {gstNumber}", functionName);
            if (!string.IsNullOrEmpty(gstNumber))
            {
                _sessionManager.Set("GSTNumber", gstNumber);
                _sessionManager.Set("IsDraft", "Y");
                string query = "select 'Y' from TEC_OLED where \"GstNo\" = '" + _sessionManager.Get("GSTNumber").ToString() + "'";
                string isExist = db.GetSingleValue(query);
                string query1 = "select 'Y' from TEC_OLED where \"GstNo\" = '" + _sessionManager.Get("GSTNumber").ToString() + "' and ifnull(\"Draft\",'N')='N'";
                string isExist1 = db.GetSingleValue(query1);
                string ReApplySts = db.GetSingleValue("Select 'N' from \"ApprovalTrace\" where  \"GstNo\"='" + _sessionManager.Get("GSTNumber").ToString() + "' and \"ReApplySts\"='No' ");
                log.WriteToLogFile_Debug($"Checks: isExist=" + isExist + ", isExist1=" + isExist1 + ", ReApplySts=" + ReApplySts, functionName);
                if (ReApplySts == "N")
                {
                    log.WriteToLogFile_Debug("Validation Failed - Reapply not allowed for GST: " + gstNumber, functionName);
                    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Reapply not allowed for GST", null);
                    
                }
                if (isExist1 == "Y")
                {
                    string LiveDb = _configuration.GetValue<string>("HanaSettings:DBName_Live") ?? "";
                    string gstNew = db.GetSingleValue("select \"CardCode\" from " + LiveDb + ".CRD1 where  \"GSTRegnNo\" = '" + gstNumber + "'");
                    log.WriteToLogFile_Debug("[HomePage] [btnHiddenSearch_Click] [FLOW] - Live DB card code: " + gstNew, "btnHiddenSearch_Click");
                    if (gstNew != null && gstNew != "")
                    {
                        _sessionManager.Set("GSTNumber",gstNumber);
                        log.WriteToLogFile_Debug("Redirecting to /Pages/NewProduct.aspx", functionName);
                        return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Redirecting to NewProduct page", "NewProduct");
                       
                    }
                    else
                    {
                        log.WriteToLogFile_Debug("Validation Failed - GST already submitted, card code not found on live", functionName);
                        _sessionManager.Set("GSTNumber", null);
                        return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Entered GST Number is already submitted",null);
                        
                    }
                    
                }
                if (isExist == "Y")
                {
                    log.WriteToLogFile_Debug("Redirecting to /Pages/VendorCreation.aspx", functionName);
                    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Redirecting to the vendor creation page", "VendorCreation");
                    
                }
                else
                {
                    log.WriteToLogFile_Debug("[HomePage] [btnHiddenSearch_Click] [VALIDATION_FAILED] - Entered GSTNo is Invalid: " + gstNumber, functionName);
                    _sessionManager.Set("GSTNumber", null);
                    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Entered GSTNo is invalid",null);                 
                }
            }

            else
            {
                log.WriteToLogFile_Debug("Validation failed - GST number is empty", functionName);
                return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "GST Number is empty", null);

            }
        }
    }
}

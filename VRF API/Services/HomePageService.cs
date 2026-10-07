using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.SqlServer.Server;
using VRF_API.Authentication;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;
using static VRF_API.Repository.CommonRepo;
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
        private readonly string sDBName;
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
            sDBName = _configuration["HanaSettings:DBName"];
            _sessionManager = sessionManager;
            log = _log;
        }
        public async Task<ApiResponse> SearchGSTNumber(string gstNumber)
        {
            string functionName = "SearchGSTNumber";
            string liveDbName = _configuration["HanaSettings:DBName_Live"];

            log.WriteToLogFile_Debug(
                $"{functionName} - Starting the function",
                functionName
            );

            log.WriteToLogFile_Debug(
                $"{functionName} - Entered GST Number: {gstNumber}",
                functionName
            );

            if (string.IsNullOrWhiteSpace(gstNumber))
            {
                log.WriteToLogFile_Debug(
                    $"{functionName} - GST Number is empty",
                    functionName
                );

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "GST Number cannot be empty",
                    null
                );
            }

            try
            {
                // =========================================================
                // Set Session Values
                // =========================================================
                _sessionManager.Set("GSTNumber", gstNumber);
                _sessionManager.Set("IsDraft", "Y");

                log.WriteToLogFile_Debug(
                    $"{functionName} - Session values set successfully",
                    functionName
                );
                //var data = new Dictionary<string, object>
                //{
                //    ["Trade Name"] = "ABC Enterprises",
                //    ["Billing Address"] = "No. 25, Anna Salai, Teynampet, Chennai - 600018",
                //    ["Registered Address"] = "No. 10, GST Road, Guindy, Chennai - 600032",
                //    ["Nature of Business"] = "Wholesale and Distribution of Electrical Products",
                //    ["Mobile Number"] = "9876543210",
                //    ["Office Telephone"] = "044-45678901",
                //    ["Email ID"] = "contact@abcenterprises.com",
                //    ["Agency Email"] = "agency@abcenterprises.com",
                //    ["Contact Person"] = "Rajesh Kumar",
                //    ["Mobile No"] = "9876543210",
                //    ["Bank Name"] = "HDFC Bank",
                //    ["Account Number"] = "50200012345678",
                //    ["IFSC Code"] = "HDFC0001234",
                //    ["Goods Return Address"] = "No. 15, Industrial Estate, Ambattur, Chennai - 600058",
                //    ["Credit Days"] = "30",
                //    ["Discount"] = "5%",
                //    ["GST Number"] = "33ABCDE1234F1Z5",
                //    ["PAN Number"] = "ABCDE1234F",
                //    ["MSME Number"] = "UDYAM-TN-01-0012345",
                //    ["Enterprise Type"] = "Medium",
                //    ["Business Type"] = "Partnership",
                //    ["NHFS Contact Person"] = "Rajesh Kumar",
                //    ["Date"] = DateTime.Now.ToString("dd/MM/yyyy")
                //};

                //var goodsList = new List<MajorGoodsServiceModel>();
                //string htmlContent =
                //   db.GenerateVendorHtmlWithData(
                //       data,
                //       goodsList, true
                //   );

                //byte[] pdfBytes =
                //    db.ConvertHtmlToPdf(htmlContent);

                //string base64 = Convert.ToBase64String(db.ConvertHtmlToPdf(htmlContent));


                // =========================================================
                // Check whether GST exists in TEC_OLED
                // =========================================================
                string isExist = db.GetSingleValue($@"
            SELECT 'Y'
            FROM ""{sDBName}"".""TEC_OLED""
            WHERE ""GstNo"" = '{gstNumber}'
        ");

                log.WriteToLogFile_Debug(
                    $"{functionName} - GST exists check: {isExist}",
                    functionName
                );


                // =========================================================
                // GST DOES NOT EXIST
                // =========================================================
                if (isExist != "Y")
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - GST Number does not exist",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Invalid GST Number",
                        null
                    );
                }


                // =========================================================
                // Get Draft
                // =========================================================
                string draft = db.GetSingleValue($@"
            SELECT IFNULL(""Draft"", '')
            FROM ""{sDBName}"".""TEC_OLED""
            WHERE ""GstNo"" = '{gstNumber}'
        ");

                // Make sure NULL becomes empty string
                //draft = draft ?? "";


                // =========================================================
                // Get DraftApproved
                // =========================================================
                string draftApproved = db.GetSingleValue($@"
            SELECT IFNULL(""DraftApproved"", '')
            FROM ""{sDBName}"".""TEC_OLED""
            WHERE ""GstNo"" = '{gstNumber}'
        ");

                draftApproved = draftApproved ?? "";


                // =========================================================
                // Get ReApply Status
                // =========================================================
                string reApplySts = db.GetSingleValue($@"
            SELECT IFNULL(""ReApplySts"", '')
            FROM ""{sDBName}"".""ApprovalTrace""
            WHERE ""GstNo"" = '{gstNumber}'
            ORDER BY ""CreatedOn"" DESC
            LIMIT 1
        ");

                reApplySts = reApplySts ?? "";


                // =========================================================
                // Logging
                // =========================================================
                log.WriteToLogFile_Debug(
                    $"{functionName} - Draft: '{draft}'",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"{functionName} - DraftApproved: '{draftApproved}'",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"{functionName} - ReApplySts: '{reApplySts}'",
                    functionName
                );


                // =========================================================
                // CASE 1
                // RE-APPLY
                //
                // ReApplySts = Yes
                //
                // Navigate to Vendor Creation Page 1
                // =========================================================
                if (reApplySts.Equals(
                        "Yes",
                        StringComparison.OrdinalIgnoreCase))
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - ReApply allowed. Redirecting to VendorCreation page 1.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "ReApply allowed",
                        new
                        {
                            Redirect = "VendorCreation",
                            Page = 1,
                            ReApply = "ReApply"
                        }
                    );
                }


                // =========================================================
                // CASE 2
                // DRAFT APPROVED
                //
                // DraftApproved = Y
                //
                // Navigate to Page 6
                // =========================================================
                if (draftApproved == "Y" && draft == "Y")
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - DraftApproved=Y. Redirecting to VendorCreation page 6.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "Draft approved",
                        new
                        {
                            Redirect = "VendorCreation",
                            Page = 6,
                            ReApply = "No"
                        }
                    );
                }


                // =========================================================
                // CASE 3
                // DRAFT EXISTS
                //
                // Draft = Y OR Draft = empty
                //
                // Show "GST already in draft"
                // =========================================================
                if (draft == "Y" )
                  
                    {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - GST Number already exists in draft.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Entered GST Number is already in draft",
                        null
                    );
                }
                if(draft == "")
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - GST Number already exists in draft.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "Entered GST Number is already in draft",
                         new
                         {
                             Redirect = "VendorCreation"
                         }
                    );
                }


                // =========================================================
                // CASE 4
                // GST ALREADY SUBMITTED
                //
                // Draft = N
                // =========================================================
                if (draft == "N")
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - GST Number already submitted.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Entered GST Number is already submitted",
                        null
                    );
                }


                // =========================================================
                // OPTIONAL:
                // Existing Vendor / CardCode Check
                //
                // If you still want to support NewProduct redirect,
                // keep this before the final failure.
                // =========================================================
                string cardCode = db.GetSingleValue($@"
            SELECT ""CardCode""
            FROM ""{liveDbName}"".""CRD1""
            WHERE ""GSTRegnNo"" = '{gstNumber}'
        ");

                log.WriteToLogFile_Debug(
                    $"{functionName} - Live DB CardCode: {cardCode}",
                    functionName
                );

                if (!string.IsNullOrEmpty(cardCode))
                {
                    log.WriteToLogFile_Debug(
                        $"{functionName} - CardCode found. Redirecting to NewProduct.",
                        functionName
                    );

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "GST Number is already registered",
                        new
                        {
                            Redirect = "NewProduct",
                            CardCode = cardCode
                        }
                    );
                }


                // =========================================================
                // DEFAULT
                // =========================================================
                log.WriteToLogFile_Debug(
                    $"{functionName} - GST validation completed without a matching condition.",
                    functionName
                );

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "Unable to process the entered GST Number",
                    null
                );
            }
            catch (Exception ex)
            {
                // =========================================================
                // Exception Handling
                // =========================================================
                log.WriteToLogFile_Debug(
                    $"{functionName} - Exception: {ex.Message}",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"{functionName} - StackTrace: {ex.StackTrace}",
                    functionName
                );

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    $"Error while checking GST Number: {ex.Message}",
                    null
                );
            }
        }
        // public async Task<ApiResponse> SearchGSTNumber(string gstNumber)
        // {
        //     string functionName = "Search_GST_Number";
        //     string liveDbName = _configuration["HanaSettings:DBName_Live"];

        //     log.WriteToLogFile_Debug(
        //         $"{functionName} - Starting the function",
        //         functionName
        //     );

        //     log.WriteToLogFile_Debug(
        //         $"{functionName} - Entered GST Number: {gstNumber}",
        //         functionName
        //     );

        //     if (!string.IsNullOrEmpty(gstNumber))
        //     {
        //         try
        //         {
        //             // ---------------------------------------------------------
        //             // Set session values
        //             // ---------------------------------------------------------
        //             _sessionManager.Set("GSTNumber", gstNumber);
        //             _sessionManager.Set("IsDraft", "Y");

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - Session values set successfully",
        //                 functionName
        //             );

        //             // ---------------------------------------------------------
        //             // Check whether GST exists in TEC_OLED
        //             // ---------------------------------------------------------
        //             string isExist = db.GetSingleValue($@"
        //    SELECT 'Y'
        //    FROM ""{sDBName}"".""TEC_OLED""
        //    WHERE ""GstNo"" = '{gstNumber}'
        //");

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - GST exists check: {isExist}",
        //                 functionName
        //             );

        //             // ---------------------------------------------------------
        //             // If GST does not exist
        //             // ---------------------------------------------------------
        //             if (isExist != "Y")
        //             {
        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - GST Number does not exist",
        //                     functionName
        //                 );

        //                 return ApiResponseUtility.GenerateApiResponse(
        //                     ApiStatusEnum.Failure,
        //                     "Invalid GST Number",
        //                     null
        //                 );
        //             }

        //             // ---------------------------------------------------------
        //             // Get Draft value
        //             // ---------------------------------------------------------
        //             string draft = db.GetSingleValue($@"
        //    SELECT ""Draft""
        //    FROM ""{sDBName}"".""TEC_OLED""
        //    WHERE ""GstNo"" = '{gstNumber}'
        //");

        //             // ---------------------------------------------------------
        //             // Get DraftApproved value
        //             // ---------------------------------------------------------
        //             string draftApproved = db.GetSingleValue($@"
        //    SELECT IFNULL(""DraftApproved"", '')
        //    FROM ""{sDBName}"".""TEC_OLED""
        //    WHERE ""GstNo"" = '{gstNumber}'
        //");

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - Draft: '{draft}'",
        //                 functionName
        //             );

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - DraftApproved: '{draftApproved}'",
        //                 functionName
        //             );

        //             // ---------------------------------------------------------
        //             // Check ReApply status
        //             // ---------------------------------------------------------
        //             string reApplySts = db.GetSingleValue($@"
        //    SELECT 'N'
        //    FROM ""{sDBName}"".""ApprovalTrace""
        //    WHERE ""GstNo"" = '{gstNumber}'
        //    AND ""ReApplySts"" = 'No'
        //");

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - ReApplySts: {reApplySts}",
        //                 functionName
        //             );

        //             if (reApplySts == "N")
        //             {
        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - Reapply not allowed for GST",
        //                     functionName
        //                 );

        //                 return ApiResponseUtility.GenerateApiResponse(
        //                     ApiStatusEnum.Failure,
        //                     "Reapply is not allowed for this GST Number",
        //                     null
        //                 );
        //             }


        //             // =========================================================
        //             // CASE 1:
        //             // Draft = Y
        //             // DraftApproved = Y
        //             // Already submitted
        //             // =========================================================
        //             //if (draft == "Y" && draftApproved == "Y")
        //             //{
        //             //    log.WriteToLogFile_Debug(
        //             //        $"{functionName} - Draft=Y and DraftApproved=Y. GST already submitted.",
        //             //        functionName
        //             //    );

        //             //    return ApiResponseUtility.GenerateApiResponse(
        //             //        ApiStatusEnum.Failure,
        //             //        "Entered GST Number is already submitted",
        //             //        null
        //             //    );
        //             //}

        //             // =========================================================
        //             // CASE 2:
        //             // Draft = N
        //             // DraftApproved = Y
        //             // Redirect to VendorCreation
        //             // =========================================================
        //             if (draft == "Y" && draftApproved == "Y")
        //             {
        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - Draft=N and DraftApproved=Y. Redirecting to VendorCreation.",
        //                     functionName
        //                 );

        //                 return ApiResponseUtility.GenerateApiResponse(
        //                     ApiStatusEnum.Success,
        //                     "GST Number is valid",
        //                     new
        //                     {
        //                         Redirect = "VendorCreation"
        //                     }
        //                 );
        //             }
        //             //if(draft =="Y" || draft == "")
        //             //{
        //             //    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "GST Number already exists in the draft", null);
        //             //}
        //             // =========================================================
        //             // CASE 3:
        //             // Draft is empty
        //             // Redirect to VendorCreation
        //             // =========================================================
        //             if (string.IsNullOrEmpty(draft))
        //             {
        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - Draft is empty. Redirecting to VendorCreation.",
        //                     functionName
        //                 );

        //                 return ApiResponseUtility.GenerateApiResponse(
        //                     ApiStatusEnum.Success,
        //                     "GST Number is valid",
        //                     new
        //                     {
        //                         Redirect = "VendorCreation"
        //                     }
        //                 );
        //             }

        //             // =========================================================
        //             // EXISTING LOGIC:
        //             // Draft = N and not approved
        //             // Check whether CardCode already exists in Live DB
        //             // =========================================================
        //             if (draft == "N" && draftApproved != "Y")
        //             {
        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - Draft=N and DraftApproved is not Y. Checking Live DB.",
        //                     functionName
        //                 );

        //                 string cardCode = db.GetSingleValue($@"
        //        SELECT ""CardCode""
        //        FROM ""{liveDbName}"".""CRD1""
        //        WHERE ""GSTRegnNo"" = '{gstNumber}'
        //    ");

        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - Live DB CardCode: {cardCode}",
        //                     functionName
        //                 );

        //                 if (!string.IsNullOrEmpty(cardCode))
        //                 {
        //                     log.WriteToLogFile_Debug(
        //                         $"{functionName} - CardCode found. Redirecting to NewProduct.",
        //                         functionName
        //                     );

        //                     return ApiResponseUtility.GenerateApiResponse(
        //                         ApiStatusEnum.Success,
        //                         "GST Number is already registered",
        //                         new
        //                         {
        //                             Redirect = "NewProduct",
        //                             CardCode = cardCode
        //                         }
        //                     );
        //                 }

        //                 log.WriteToLogFile_Debug(
        //                     $"{functionName} - CardCode not found. GST already submitted.",
        //                     functionName
        //                 );

        //                 return ApiResponseUtility.GenerateApiResponse(
        //                     ApiStatusEnum.Failure,
        //                     "Entered GST Number is already submitted",
        //                     null
        //                 );
        //             }
        //             if (reApplySts == "")
        //             {
        //                 return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "VendorCreation", new
        //                 {
        //                     Redirect = "VendorCreation",
        //                     ReApply = "ReApply"
        //                 });
        //             }
        //             // =========================================================
        //             // Any other case
        //             // =========================================================
        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - GST validation completed without a matching condition.",
        //                 functionName
        //             );

        //             return ApiResponseUtility.GenerateApiResponse(
        //                 ApiStatusEnum.Failure,
        //                 "Unable to process the entered GST Number",
        //                 null
        //             );
        //         }
        //         catch (Exception ex)
        //         {
        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - Exception: {ex.Message}",
        //                 functionName
        //             );

        //             log.WriteToLogFile_Debug(
        //                 $"{functionName} - StackTrace: {ex.StackTrace}",
        //                 functionName
        //             );

        //             return ApiResponseUtility.GenerateApiResponse(
        //                 ApiStatusEnum.Failure,
        //                 $"Error while checking GST Number: {ex.Message}",
        //                 null
        //             );
        //         }
        //     }
        //     else
        //     {
        //         log.WriteToLogFile_Debug(
        //             $"{functionName} - GST Number is empty",
        //             functionName
        //         );

        //         return ApiResponseUtility.GenerateApiResponse(
        //             ApiStatusEnum.Failure,
        //             "GST Number cannot be empty",
        //             null
        //         );
        //     }
        // }
        //public async Task<ApiResponse> SearchGSTNumber(string gstNumber)
        //{
        //    string functionName = "Search_GST_Number";
        //    log.WriteToLogFile_Debug($"{functionName} - Starting the function", functionName);
        //    log.WriteToLogFile_Debug($"{functionName} - Entered GST Number: {gstNumber}", functionName);
        //    if (!string.IsNullOrEmpty(gstNumber))
        //    {
        //        _sessionManager.Set("GSTNumber", gstNumber);
        //        _sessionManager.Set("IsDraft", "Y");
        //        string query = $@"select 'Y' from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{gstNumber}'";
        //        //string query = $@"select 'Y' from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{gstNumber}' and (""Draft"" = 'Y' OR ""Draft"" = '')";
        //        string isExist = db.GetSingleValue(query);
        //        string query1 = $@"select 'Y' from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{gstNumber}' and (""Draft"" !='' OR ""DraftApproved"" !='Y')";
        //        //string query = $@"select 'Y' from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{gstNumber}' and (""Draft"" = 'Y' OR ""Draft"" = '')";
        //        //string isExist = db.GetSingleValue(query);
        //        //string query1 = $@"select 'Y' from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{gstNumber}' and (""DraftApproved"" !='Y')";
        //        string isExist1 = db.GetSingleValue(query1);
        //        string ReApplySts = db.GetSingleValue($@"Select 'N' from ""{sDBName}"".""ApprovalTrace"" where  ""GstNo""='{gstNumber}' and ""ReApplySts""='No' ");
        //        log.WriteToLogFile_Debug($"Checks: isExist=" + isExist + ", isExist1=" + isExist1 + ", ReApplySts=" + ReApplySts, functionName);
        //        if (ReApplySts == "N")
        //        {
        //            log.WriteToLogFile_Debug("Validation Failed - Reapply not allowed for GST: " + gstNumber, functionName);
        //            return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Reapply not allowed for GST", null);

        //        }
        //        if (isExist1 == "Y")
        //        {
        //            string LiveDb = _configuration.GetValue<string>("HanaSettings:DBName_Live") ?? "";
        //            string gstNew = db.GetSingleValue($@"select ""CardCode"" from " + LiveDb + ".CRD1 where  \"GSTRegnNo\" = '" + gstNumber + "'");
        //            log.WriteToLogFile_Debug("[HomePage] [btnHiddenSearch_Click] [FLOW] - Live DB card code: " + gstNew, "btnHiddenSearch_Click");
        //            if (gstNew != null && gstNew != "")
        //            {
        //                _sessionManager.Set("GSTNumber",gstNumber);
        //                log.WriteToLogFile_Debug("Redirecting to /Pages/NewProduct.aspx", functionName);
        //                return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Redirecting to NewProduct page", "NewProduct");

        //            }
        //            else
        //            {
        //                log.WriteToLogFile_Debug("Validation Failed - GST already submitted, card code not found on live", functionName);
        //                _sessionManager.Set("GSTNumber", "");
        //                return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Entered GST Number is already submitted",null);

        //            }

        //        }
        //        if (isExist == "Y")
        //        {
        //            log.WriteToLogFile_Debug("Redirecting to /Pages/VendorCreation.aspx", functionName);
        //            return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Redirecting to the vendor creation page", "VendorCreation");

        //        }
        //        else
        //        {
        //            log.WriteToLogFile_Debug("[HomePage] [btnHiddenSearch_Click] [VALIDATION_FAILED] - Entered GSTNo is Invalid: " + gstNumber, functionName);
        //            _sessionManager.Set("GSTNumber", "");
        //            return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "Entered GSTNo is invalid",null);                 
        //        }
        //    }

        //    else
        //    {
        //        log.WriteToLogFile_Debug("Validation failed - GST number is empty", functionName);
        //        return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "GST Number is empty", null);

        //    }
        //}
    }
}

using Newtonsoft.Json;
using RestSharp;

using Serilog;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VRF_API.Authentication;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;
using static VRF_API.Repository.CommonRepo;
using DbConnection = VRF_API.Repository.DbConnection;
using Log = VRF_API.Repository.Log;

namespace VRF_API.Services
{
    public interface IVendorCreationService
    {
        Task<ApiResponse> LoadInitialValues();
        Task<ApiResponse> UploadKycFile(IFormFile file, string documentType, int rowIndex);
    }
    public class VendorCreationService : IVendorCreationService
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
        private readonly string dbName;
        public VendorCreationService(IConfiguration configuration, OdbcConnection connection, IHttpContextAccessor httpContextAccessor, DbConnection _db, IRequestContext requestContext, SessionManager sessionManager, Log _log)
        {
            _configuration = configuration;
            _baseUrl = _configuration.GetValue<string>("SAPApiUrl:CusCreationUrl") ?? "";
            _companyDb = _configuration.GetValue<string>("SAPApiUrl:CompanyDB") ?? "";
            _sapUser = _configuration.GetValue<string>("SAPApiUrl:UserName") ?? "";
            _sapPassword = _configuration.GetValue<string>("SAPApiUrl:Password") ?? "";
            _httpContextAccessor = httpContextAccessor;
            context = _httpContextAccessor.HttpContext;
            connectionString = _configuration.GetValue<string>("ConnectionStrings:HanaOdbc") ?? string.Empty;
            db = _db;
            session = context.Session;
            _URL = _configuration.GetValue<string>("AppSettings:LoginURL") ?? string.Empty;
            _sessionManager = sessionManager;
            log = _log;
            dbName = _configuration.GetValue<string>("HanaSettings:DBName");
        }
        public async Task<ApiResponse> LoadInitialValues()
        {
            string functionName = "Load_Initial_Values";
            log.WriteToLogFile_Debug($"{functionName} - Starting the function", functionName);
            string gstNumber = _sessionManager.Get("GSTNumber") ?? string.Empty;
            BindPartners();
            BindOperationalContacts();
            BindOtherInformation();
            BindKYCGrid1();
            BindKYCGrid11();
            List<string> contactPersonDropDowns = LoadContactPersonDropdown();
            List<State> states = LoadStates("");
            List<Bank> banks = LoadBanks();
            List<Country> countries = LoadCountries();
            var businessDetails = new List<BusinessDetails>{
                new BusinessDetails { BusinessState = "", GSTNumber = "", AddressOfPlace = "", GSTVendorClassification = "" }
            };
            var partnerDetails = new List<PartnerDetails>{
                new PartnerDetails { Name = "", Contact_No = "", Email_ID = "" }
            };
            var majorGoodsService = new List<MajorGoodsService>{
                new MajorGoodsService { Product =  "",Brand = "",Size = "" }
            };
            var MajorCustomers = new List<MajorCustomers>{
                new MajorCustomers {  CustomerName= "" }
            };
            _sessionManager.Set("BusinessDetails", JsonConvert.SerializeObject(businessDetails));
            _sessionManager.Set("PartnerDetails",JsonConvert.SerializeObject(partnerDetails));
            _sessionManager.Set("MajorGoodsService",JsonConvert.SerializeObject(majorGoodsService));
            _sessionManager.Set("MajorCustomer",JsonConvert.SerializeObject(MajorCustomers));
            RefreshKYC();
            RefreshKYC1();
            //if (GSTNumber.Text != "") GSTNumber_TextChanged(sender, e);
            if (_sessionManager.Get("DocumentDetails") == null)
            {
                InitializeGrid();
            }
            var response = new
            {
                contactPerson = contactPersonDropDowns,
                states = states,
                banks = banks,
                countires = countries
            };
            return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Loaded the intial values", response);

        }
        public async Task<ApiResponse> UploadKycFile(IFormFile file,string documentType,int rowIndex)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure,
                        "File is required",
                        null
                    );
                }

                if (string.IsNullOrWhiteSpace(documentType))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Document type is required",
                        null
                    );
                }

                string[] allowedDocumentTypes =
                {
            "PAN Card",
            "GST Certificate",
            "Bank Account",
            "MSME Certificate",
            "Performa Invoice"
        };

                if (!allowedDocumentTypes.Any(x =>
                    x.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Invalid document type",
                        null
                    );
                }

                if (documentType.Equals(
                        "Performa Invoice",
                        StringComparison.OrdinalIgnoreCase)
                    && rowIndex < 0)
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Invalid row index",
                        null
                    );
                }

                string folderPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "TempFiles"
                );

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string originalFileName = Path.GetFileName(file.FileName);
                string fileNameWithoutExtension =
                    Path.GetFileNameWithoutExtension(originalFileName);
                string extension = Path.GetExtension(originalFileName);

                const string characters =
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                string uniqueNumber = new string(
                    Enumerable.Range(0, 4)
                        .Select(x => characters[Random.Shared.Next(characters.Length)])
                        .ToArray()
                );

                string fileName =
                    $"{fileNameWithoutExtension}_{uniqueNumber}{extension}";

                string filePath = Path.Combine(
                    folderPath,
                    fileName
                );

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string rowKeySuffix = "";

                if (documentType.Equals(
                        "Performa Invoice",
                        StringComparison.OrdinalIgnoreCase))
                {
                    rowKeySuffix = "_" + rowIndex;
                }

                string sessionPathKey =
                    $"Path_{documentType}{rowKeySuffix}";

                string sessionFileNameKey =
                    $"FileName_{documentType}{rowKeySuffix}";

                _sessionManager.Set(
                    sessionPathKey,
                    filePath
                );

                _sessionManager.Set(
                    sessionFileNameKey,
                    fileName
                );

                var documentResult = await APIPosting(
                    documentType,
                    filePath
                );

                if (documentResult == null)
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Unable to process the uploaded document",
                        null
                    );
                }

                documentResult.DocumentType = documentType;
                documentResult.RowIndex = rowIndex;
                documentResult.FileName = fileName;

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Success,
                    "File uploaded and processed successfully",
                    documentResult
                );
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[UploadKycFile] [ERROR] {ex.Message}",
                    "UploadKycFile"
                );

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "Error while uploading file",
                    null
                );
            }
        }

        private async Task<KycUploadResponse?> APIPosting(
            string docType,
            string filePath)
        {
            try
            {
                string accountNumber = "";
                string accountNameHolder = "";
                string ifscCode = "";

                string legalName = "";
                string tradeName = "";
                string gstNumber = "";

                string building = "";
                string street = "";
                string locality = "";
                string city = "";
                string district = "";
                string state = "";
                string pincode = "";

                string sDocUrl = _configuration["DocUrl"] ?? "";
                string sDocKey = _configuration["DocKey"] ?? "";

                string docNo = "1";

                if (docType == "PAN Card")
                    docNo = "4";

                if (docType == "GST Certificate")
                    docNo = "2";

                if (docType == "Bank Account")
                    docNo = "1";

                if (docType == "MSME Certificate")
                    docNo = "3";

                string apiUrl =
                    sDocUrl +
                    docNo +
                    "&key=" +
                    sDocKey;

                var client = new RestClient(apiUrl);

                var request = new RestRequest("", Method.Post);

                request.AlwaysMultipartFormData = true;
                request.AddHeader("accept", "application/json");
                request.AddFile("file", filePath);

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    return null;
                }

                string json = response.Content ?? "";

                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var result = new KycUploadResponse();

                if (docNo == "1")
                {
                    dynamic data =
                        JsonConvert.DeserializeObject(json);

                    string returnedIfsc =
                        data?.ifsc_code?.ToString();

                    if (!string.IsNullOrEmpty(returnedIfsc))
                    {
                        accountNumber =
                            data?.account_number?.ToString() ?? "";

                        accountNameHolder =
                            data?.account_nameholder?.ToString() ?? "";

                        ifscCode = returnedIfsc;

                        string bankCodeFromIfsc =
                            new string(
                                ifscCode
                                    .TakeWhile(c => !char.IsDigit(c))
                                    .ToArray()
                            );

                        result.BankDetails =
                            new BankDocumentResponse
                            {
                                AccountNumber = accountNumber,
                                AccountNameHolder = accountNameHolder,
                                IfscCode = ifscCode,
                                BankCode = bankCodeFromIfsc
                            };
                    }
                }

                if (docNo == "2")
                {
                    GstDetails data =
                        JsonConvert.DeserializeObject<GstDetails>(json);

                    if (data != null &&
                        !string.IsNullOrEmpty(data.legal_name))
                    {
                        legalName = data.legal_name;
                        tradeName = data.trade_name;
                        gstNumber = data.gst_number;

                        if (data.address_in_7_separate_feilds != null)
                        {
                            building =
                                data.address_in_7_separate_feilds.building;

                            street =
                                data.address_in_7_separate_feilds.street;

                            locality =
                                data.address_in_7_separate_feilds.locality;

                            city =
                                data.address_in_7_separate_feilds.city;

                            district =
                                data.address_in_7_separate_feilds.district;

                            state =
                                data.address_in_7_separate_feilds.state;

                            pincode =
                                data.address_in_7_separate_feilds.pincode;
                        }

                        result.GstDetails =
                            new GstDocumentResponse
                            {
                                LegalName = legalName,
                                TradeName = tradeName,
                                GstNumber = gstNumber,
                                Building = building,
                                Street = street,
                                Locality = locality,
                                City = city,
                                District = district,
                                State = state,
                                Pincode = pincode
                            };
                    }
                }

                if (docNo == "3")
                {
                    UdyamDetails data =
                        JsonConvert.DeserializeObject<UdyamDetails>(json);

                    if (data != null)
                    {
                        string registerNumber =
                            data.register_number;

                        string enterpriseType =
                            data.enterprise_type;

                        string majorActivity =
                            data.major_activity;

                        bool valid =
                            IsValidMsme(registerNumber);

                        result.MsmeDetails =
                            new MsmeDocumentResponse
                            {
                                RegisterNumber = registerNumber,
                                EnterpriseType = enterpriseType,
                                MajorActivity = majorActivity,
                                IsValid = valid
                            };
                    }
                }

                if (docNo == "4")
                {
                    PanDetails data =
                        JsonConvert.DeserializeObject<PanDetails>(json);

                    if (data != null)
                    {
                        string panNo = data.pan_no;
                        string dateOfIncorporation =
                            data.date_of_incorporation;

                        if (!string.IsNullOrEmpty(panNo))
                        {
                            _sessionManager.Set("PAN", panNo);
                        }

                        string formattedDate = "";

                        if (!string.IsNullOrEmpty(dateOfIncorporation))
                        {
                            if (DateTime.TryParse(
                                dateOfIncorporation,
                                out DateTime dateValue))
                            {
                                formattedDate =
                                    dateValue.ToString("yyyy-MM-dd");
                            }
                        }

                        result.PanDetails =
                            new PanDocumentResponse
                            {
                                PanNumber = panNo,
                                DateOfIncorporation = formattedDate
                            };
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[APIPosting] [ERROR] {ex.Message}",
                    "APIPosting"
                );

                return null;
            }
        }
        private void BindOtherInformation()
        {
            var otherInformation = new List<OtherInformation>{
                new OtherInformation{ Description = "Total Count of Employees / Labours",TextMode = "Number"},
                new OtherInformation{ Description = "Area of Office / Factory",TextMode = "Text"},
                new OtherInformation{ Description = "Max. Production Capacity",TextMode = "Number"},
                new OtherInformation{ Description = "Yearly Turnover",TextMode = "Number"}
            };
            _sessionManager.Set("OtherInformation", JsonConvert.SerializeObject(otherInformation));
        }
        private void BindOperationalContacts()
        {
            List<OperationalContact> contacts = new List<OperationalContact>
            {
                new OperationalContact { Department = "Business head", ContactNo = "", Email = "" },
                new OperationalContact { Department = "sale manager", ContactNo = "", Email = "" },
                new OperationalContact { Department = "Account Head", ContactNo = "", Email = "" }
            };
            _sessionManager.Set("OperationalContact", JsonConvert.SerializeObject(contacts));
        }
        protected void BindPartners()
        {
            var partnerDetailsJson = _sessionManager.Get("PartnerDetails");
            List<PartnerDetails> partnerDetails = string.IsNullOrEmpty(partnerDetailsJson) ? new List<PartnerDetails>() : JsonConvert.DeserializeObject<List<PartnerDetails>>(partnerDetailsJson);
            for (int i = 0; i < partnerDetails.Count; i++)
            {
                partnerDetails[i].RowID = i;
            }
            _sessionManager.Set("PartnerDetails", JsonConvert.SerializeObject(partnerDetails));

        }
        private void BindKYCGrid1()
        {
            var kycDocuments = new List<KYCDocument>
            {
                new KYCDocument{DocumentType = "PAN Card",FileData = ""},
                new KYCDocument{DocumentType = "GST Certificate",FileData = ""},
                new KYCDocument{DocumentType = "Bank Account",FileData = ""},
                new KYCDocument{DocumentType = "MSME Certificate",FileData = ""}
            };
            _sessionManager.Set("KYCDocuments", JsonConvert.SerializeObject(kycDocuments));
        }
        private void BindKYCGrid11()
        {
            var kycDocuments = new List<KYCDocument>
            {
                new KYCDocument{DocumentType = "Performa Invoice",FileData = ""}
            };
            _sessionManager.Set("PerformaInvoice", JsonConvert.SerializeObject(kycDocuments));
        }
        private List<string> LoadContactPersonDropdown()
        {
            string functionName = "LoadContactPersonDropdown";
            log.WriteToLogFile_Debug($"{functionName} - Starting the function", functionName);
            try
            {
                string sp = "CALL \"GetContactTypes\"";
                log.WriteToLogFile_Debug($"{sp} - Calling the sp to fetch the contact persons", functionName);
                DataTable dt = db.ExecuteQueryForDataTable(sp);

                var contactTypes = new List<string>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        contactTypes.Add(row["Name"].ToString() ?? string.Empty);
                    }
                }
                log.WriteToLogFile_Debug($"{functionName} - SP Result - {contactTypes}", functionName);
                return contactTypes;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[LoadContactPersonDropdown] Error: " + ex.Message,
                    "LoadContactPersonDropdown"
                );

                return new List<string>();
            }
        }
        private List<State> LoadStates(string country)
        {
            var states = new List<State>();

            string query =
                $"CALL \"{dbName}\".\"GetStates\"('{country}')";

            using (var connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                using (var command = new OdbcCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        states.Add(new State
                        {
                            StateName = reader["StateName"]?.ToString(),
                            StateCode = reader["StateCode"]?.ToString()
                        });
                    }
                }
            }

            return states;
        }
        private List<Bank> LoadBanks()
        {
            var banks = new List<Bank>();

            string query = $"CALL \"{dbName}\".\"GetBanks\"()";

            using (var connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            banks.Add(new Bank
                            {
                                BankName = reader["BankName"]?.ToString(),
                                BankCode = reader["BankCode"]?.ToString()
                            });
                        }
                    }
                }
            }

            return banks;
        }
        private List<Country> LoadCountries()
        {
            var countries = new List<Country>();

            string query = $"CALL \"{dbName}\".\"GetCountries\"()";

            using (var connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countries.Add(new Country
                            {
                                CountryName = reader["CountryName"]?.ToString(),
                                CountryCode = reader["CountryCode"]?.ToString()
                            });
                        }
                    }
                }
            }
            return countries;
        }
        public void RefreshKYC()
        {
            string[] documentTypes ={"PAN Card","GST Certificate","Bank Account","MSME Certificate"};
            foreach (var documentType in documentTypes)
            {
                _sessionManager.Set($"Path_{documentType}",null);
                _sessionManager.Set($"base64_{documentType}",null);
                _sessionManager.Set($"FileName_{documentType}",null);
            }
        }
        public void RefreshKYC1()
        {
            string documentType = "Performa Invoice";
            _sessionManager.Set($"Path_{documentType}",null);
            _sessionManager.Set($"base64_{documentType}",null);
            _sessionManager.Set($"FileName_{documentType}",null );
        }
        private void InitializeGrid()
        {
            List<DocumentDetail> documentList = new List<DocumentDetail>{
                new DocumentDetail { DocumentType = "Performa Invoice", DocumentName = "" }
            };
            _sessionManager.Set("DocumentDetails", JsonConvert.SerializeObject(documentList));
        }
        private bool IsValidMsme(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            return Regex.IsMatch(value, @"^UDYAM-[A-Z]{2}-\d{2}-\d{7}$",
                RegexOptions.IgnoreCase);
        }

    }
}

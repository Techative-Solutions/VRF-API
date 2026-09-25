using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Hosting.Server;
using Newtonsoft.Json;
using RestSharp;
using Sap.Data.Hana;
using Serilog;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Net.Mail;
using System.Reflection;
using System.Text;
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
        Task<ApiResponse> ViewKYCFile(string fileName, string gstNumber, string documentType);
        Task<ApiResponse> DownloadKYCFile(string fileName, string gstNumber, string documentType);
        Task<ApiResponse> GSTNumberCheck(string gstNumber);
        Task<ApiResponse> NextPageCheck(string gstNumber, int page);
        List<State> GetStatesByCountry(string country);
        Task<ApiResponse> SaveDraft(int Page, FormDataModel formData, UploadedFilesModel files);
        Task<bool> VerifyOTP(VerifyOtpRequest request);
        Task<bool> SendOTP(SendOtpRequest request);
        Task<SubmitVendorResult> SubmitVendor(SubmitVendorRequest request);
        Task<(bool Success, string Message)> SaveNewProductAsync(
           SaveRequest request);
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
        private readonly string sDBName;
        private readonly IConverter _converter;
        public VendorCreationService(IConverter converter,IConfiguration configuration, OdbcConnection connection, IHttpContextAccessor httpContextAccessor, DbConnection _db, IRequestContext requestContext, SessionManager sessionManager, Log _log)
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
            sDBName = _configuration["HanaSettings:DBName"];
            _converter = converter;
            dbName = _configuration.GetValue<string>("HanaSettings:DBName");
        }
        public async Task<bool> SendOTP(SendOtpRequest request)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [SendOTP] [START] - Send OTP clicked",
                "SendOTP");

            try
            {
                if (request == null)
                {
                    throw new Exception("Invalid OTP request.");
                }

                string mobileNumber =
                    request.MobileNumber?.Trim() ?? "";

                string gstNumber =
                    request.GstNumber?.Trim() ?? "";

                // ----------------------------------------------------
                // Mobile validation
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(mobileNumber))
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [SendOTP] [VALIDATION_FAILED] - Mobile number is empty",
                        "SendOTP");

                    throw new Exception(
                        "Please Enter Mobile Number before proceeding.");
                }

                if (mobileNumber.Length != 10 ||
                    !mobileNumber.All(char.IsDigit))
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [SendOTP] [VALIDATION_FAILED] - Invalid mobile number",
                        "SendOTP");

                    throw new Exception(
                        "Please Enter valid Mobile Number before proceeding.");
                }

                // ----------------------------------------------------
                // GST validation
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(gstNumber))
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [SendOTP] [VALIDATION_FAILED] - GST number is empty",
                        "SendOTP");

                    throw new Exception(
                        "GST Number is required.");
                }

                // ----------------------------------------------------
                // Generate OTP
                // ----------------------------------------------------

                string otp = GenerateOTP();

                if (string.IsNullOrWhiteSpace(otp))
                {
                    throw new Exception(
                        "Unable to generate OTP.");
                }

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendOTP] [OTP_GENERATED] - OTP generated successfully",
                    "SendOTP");

                // ----------------------------------------------------
                // Creation / ValidUntil
                // ----------------------------------------------------

                DateTime creation = DateTime.Now;

                DateTime validUntil =
                    creation.AddMinutes(15);

                // ----------------------------------------------------
                // Insert OTP record
                // ----------------------------------------------------

                string query = $@"
                INSERT INTO ""{sDBName}"".""TEC_BPRegistrationOTP""
                (
                    ""gstNumber"",
                    ""Mobileno"",
                    ""OTPMobileno"",
                    ""OTP"",
                    ""Creation"",
                    ""ValidUntil"",
                    ""Verified"",
                    ""ValidateOTP""
                )
                VALUES
                (
                    ?,
                    ?,
                    ?,
                    ?,
                    ?,
                    ?,
                    ?,
                    ?
                )";

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendOTP] [DB_START] - Inserting OTP record for mobile: " +
                    mobileNumber,
                    "SendOTP");

                using (var connection =
                    new OdbcConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command =
                        new OdbcCommand(query, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@gstNumber",
                            gstNumber);

                        command.Parameters.AddWithValue(
                            "@Mobileno",
                            mobileNumber);

                        command.Parameters.AddWithValue(
                            "@OTPMobileno",
                            mobileNumber);

                        command.Parameters.AddWithValue(
                            "@OTP",
                            otp);

                        command.Parameters.AddWithValue(
                            "@Creation",
                            creation);

                        command.Parameters.AddWithValue(
                            "@ValidUntil",
                            validUntil);

                        command.Parameters.AddWithValue(
                            "@Verified",
                            0);

                        command.Parameters.AddWithValue(
                            "@ValidateOTP",
                            otp);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendOTP] [DB_END] - OTP record inserted successfully",
                    "SendOTP");

                // ----------------------------------------------------
                // Send SMS
                // ----------------------------------------------------

                await SendSMS(
                    mobileNumber,
                    otp);

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendOTP] [END] - Send OTP completed",
                    "SendOTP");

                return true;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendOTP] [ERROR] - " +
                    ex.Message,
                    "SendOTP");

                throw;
            }
        }
        public async Task<SubmitVendorResult> SubmitVendor(SubmitVendorRequest request)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [SubmitVendor] [START] - Submit vendor started",
                "SubmitVendor");

            if (request == null)
                throw new Exception("Invalid submit request.");

            if (request.FormData == null)
                throw new Exception("Vendor form data is required.");

            if (request.IsExistingVendor && !request.OtpValid)
                throw new Exception("Kindly Verify Mobile Number.");

            var model = request.FormData;

            string gstNumber = model.GstNumber?.Trim().ToUpper() ?? "";
            string email = model.Email?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(gstNumber))
                throw new Exception("GST Number is required.");

            if (gstNumber.Length != 15)
                throw new Exception("GSTNO must be 15 character.");

            if (string.IsNullOrWhiteSpace(model.TradeName))
                throw new Exception("Trade Name is required.");

            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(model.MobileNumber))
                throw new Exception("Mobile Number is required.");

            if (model.RegisteredOffice == null ||
                string.IsNullOrWhiteSpace(model.RegisteredOffice.Address1) ||
                string.IsNullOrWhiteSpace(model.RegisteredOffice.Country))
                throw new Exception("Registered Office Address and Country are required.");

            if (model.BillingAddress == null ||
                string.IsNullOrWhiteSpace(model.BillingAddress.Address1) ||
                string.IsNullOrWhiteSpace(model.BillingAddress.Country))
                throw new Exception("Billing Address and Country are required.");

            if (model.BankDetails == null ||
                string.IsNullOrWhiteSpace(model.BankDetails.BankName) ||
                string.IsNullOrWhiteSpace(model.BankDetails.AccountNameHolder) ||
                string.IsNullOrWhiteSpace(model.BankDetails.AccountNumber) ||
                string.IsNullOrWhiteSpace(model.BankDetails.IfscCode))
                throw new Exception("Bank Name, Account Name, Account Number and IFSC Code are required.");

            using var connection = new OdbcConnection(connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                int existingId = 0;

                // A submitted vendor is a duplicate only when a NON-DRAFT
                // record already exists. A draft record may be converted
                // into the final submission.
                string existingFinalQuery = $@"
                    SELECT ""Id""
                    FROM ""{sDBName}"".""TEC_OLED""
                    WHERE ""GstNo"" = ?
                      AND IFNULL(""Draft"", 'N') = 'N'";

                using (var command = new OdbcCommand(existingFinalQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@GstNo", gstNumber);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                        throw new Exception("GSTNumber is already registered.");
                }

                // Check whether this GST belongs to an existing draft.
                string existingDraftQuery = $@"
                    SELECT ""Id""
                    FROM ""{sDBName}"".""TEC_OLED""
                    WHERE ""GstNo"" = ?
                      AND IFNULL(""Draft"", 'N') = 'Y'
                    ORDER BY ""Id"" DESC";

                using (var command = new OdbcCommand(existingDraftQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@GstNo", gstNumber);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                        existingId = Convert.ToInt32(result);
                }

                int id = existingId;

                if (id == 0)
                {
                    string nextIdQuery = $@"
                        SELECT IFNULL(MAX(""Id""), 0) + 1
                        FROM ""{sDBName}"".""TEC_OLED""";

                    using var idCommand = new OdbcCommand(
                        nextIdQuery,
                        connection,
                        transaction);

                    id = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
                }

                // IMPORTANT:
                // These are FINAL-SUBMISSION methods.
                // They do not contain draft/update branching.
                // If a draft already exists, all draft child rows are
                // removed first and then the complete final data is inserted.
                if (existingId > 0)
                {
                    await DeleteSubmissionChildData(
                        connection,
                        transaction,
                        existingId);

                    await SubmitUpdateHeaderDetails(
                        connection,
                        transaction,
                        existingId,
                        model);
                }
                else
                {
                    await SubmitInsertHeaderDetails(
                        connection,
                        transaction,
                        id,
                        model);
                }

                await SubmitInsertPaymentDetails(
                    connection,
                    transaction,
                    id,
                    model.PaymentDetails);

                await SubmitInsertBusinessDetails(
                    connection,
                    transaction,
                    id,
                    model.OtherBusinessLocations);

                // React: formData.businessPartners -> TEC_LED2
                await SubmitInsertPartnerDetails(
                    connection,
                    transaction,
                    id,
                    model.BusinessPartners);

                await SubmitInsertOperationalContacts(
                    connection,
                    transaction,
                    id,
                    model.OperationalContacts);

                await SubmitInsertMajorGoodsServices(
                    connection,
                    transaction,
                    id,
                    model.MajorGoodsServices);

                await SubmitInsertMajorCustomers(
                    connection,
                    transaction,
                    id,
                    model.MajorCustomers);

                await SubmitInsertOtherInformation(
                    connection,
                    transaction,
                    id,
                    model.OtherInformation);

                await SubmitInsertDocuments(
                    connection,
                    transaction,
                    id,
                    model,
                    request.UploadedFiles);

                transaction.Commit();

                log.WriteToLogFile_Debug(
                    $"[VendorCreation] [SubmitVendor] [DB_END] - Final submission committed. Id: {id}",
                    "SubmitVendor");

                try
                {
                    await SentMail(request.FormData,email, request.FormData.PaymentDetails.AgencyEmail);
                }
                catch (Exception mailEx)
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [SubmitVendor] [MAIL_ERROR] - " + mailEx.Message,
                        "SubmitVendor");
                }

                return new SubmitVendorResult
                {
                    Success = true,
                    Id = id,
                    GstNumber = gstNumber,
                    Message = "You have successfully submitted the form. Our representative will reach out to you soon. Please use your GSTIN as the reference number."
                };
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // Ignore rollback errors.
                }

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SubmitVendor] [ERROR] - " + ex.Message,
                    "SubmitVendor");

                throw;
            }
        }

        private async Task SentMail(
    FormDataModel formData,
    string toMail,
    string agentMail)
        {
            try
            {
                // ============================================================
                // 1. VALIDATE FORM DATA
                // ============================================================

                if (formData == null)
                {
                    throw new Exception("Form data is missing.");
                }

                log.WriteToLogFile_Debug(
                    "Build Preview started",
                    "Mail"
                );

                // ============================================================
                // 2. BUILD PREVIEW DATA FROM FORM DATA
                // ============================================================

                var data = new Dictionary<string, object>();

                data["Trade Name"] =
                    formData.TradeName ?? "";

                data["Billing Address"] =
                    formData.BillingAddress?.Address1 ?? "";

                data["Registered Address"] =
                    formData.RegisteredOffice?.Address1 ?? "";

                data["Nature of Business"] =
                    formData.NatureOfBusiness ?? "";

                data["Mobile Number"] =
                    formData.MobileNumber ?? "";

                data["Office Telephone"] =
                    formData.OfficeTelephoneNo ?? "";

                data["Email ID"] =
                    formData.Email ?? "";

                data["Agency Email"] =
                    formData.PaymentDetails.AgencyEmail ?? "";

                data["Contact Person"] =
                    formData.ContactPerson ?? "";

                data["Mobile No"] =
                    formData.MobileNumber ?? "";

                data["Bank Name"] =
                    formData.BankDetails?.BankName ?? "";

                data["Account Number"] =
                    formData.BankDetails?.AccountNumber ?? "";

                data["IFSC Code"] =
                    formData.BankDetails?.IfscCode ?? "";

                data["Goods Return Address"] =
                    formData.GoodsReturnAddress?.Address1 ?? "";

                data["Credit Days"] =
                    formData.PaymentDetails?.CreditDays ?? "";

                data["Discount"] =
                    formData.PaymentDetails?.BillLevelDiscount ?? "";

                data["GST Number"] =
                    formData.GstNumber ?? "";

                data["PAN Number"] =
                    formData.PanNumber ?? "";

                data["MSME Number"] =
                    formData.MsmeDetails?.MsmeNo ?? "";

                data["Enterprise Type"] =
                    formData.MsmeDetails?.EnterpriseType ?? "";

                //data["Major Activity"] =
                   // formData.MsmeDetails?.MajorActivity ?? "";

                //data["Legal Name"] =
                    //formData.LegalName ?? "";

                data["Business Type"] =
                    formData.PartnerType ?? "";

                data["NHFS Contact Person"] =
                    formData.ContactPerson ?? "";

                data["Date"] =
                    DateTime.Now.ToString("dd/MM/yyyy");

                log.WriteToLogFile_Debug(
                    "Build Preview Ended",
                    "Mail"
                );

                // ============================================================
                // 3. GET MAJOR GOODS FROM FORMDATA
                // ============================================================

                var goodsList =
                    formData.MajorGoodsServices
                    ?? new List<MajorGoodsServiceModel>();

                log.WriteToLogFile_Debug(
                    "Major goods collected: " +
                    goodsList.Count,
                    "Mail"
                );

                // ============================================================
                // 4. GENERATE PREVIEW HTML
                // ============================================================

                string htmlContent =
                    GenerateVendorHtmlWithData(
                        data,
                        goodsList
                    );

                log.WriteToLogFile_Debug(
                    "Html content completed",
                    "Mail"
                );

                // ============================================================
                // 5. CONVERT HTML TO PDF
                // ============================================================

                byte[] pdfBytes =
                    ConvertHtmlToPdf(htmlContent);

                if (pdfBytes == null ||
                    pdfBytes.Length == 0)
                {
                    throw new Exception(
                        "Failed to generate Vendor Preview PDF."
                    );
                }

                log.WriteToLogFile_Debug(
                    "ConvertHtmlToPdf completed",
                    "Mail"
                );

                // ============================================================
                // 6. GET MAIL BODY / SUBJECT
                // ============================================================

                string body = "";
                string subject = "";
                string ccMails = "";

                using (var connection = new OdbcConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query =
                        $"CALL \"{sDBName}\".\"Mail_BOSY&SUBJECT\"('DRAFT')";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int bodyIndex =
                                    reader.GetOrdinal("Body");

                                int subjectIndex =
                                    reader.GetOrdinal("Subject");

                                body =
                                    reader.IsDBNull(bodyIndex)
                                        ? ""
                                        : reader.GetString(bodyIndex);

                                subject =
                                    reader.IsDBNull(subjectIndex)
                                        ? ""
                                        : reader.GetString(subjectIndex);

                                // CCMail may or may not be returned
                                try
                                {
                                    int ccIndex =
                                        reader.GetOrdinal("CCMail");

                                    ccMails =
                                        reader.IsDBNull(ccIndex)
                                            ? ""
                                            : reader.GetString(ccIndex);
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    ccMails = "";
                                }
                            }
                        }
                    }
                }

                // ============================================================
                // 7. REPLACE VENDOR NAME IN MAIL BODY
                // ============================================================

                string vendorName =
                    formData.TradeName ?? "";

                body =
                    body.Replace(
                        "{Vendor Name}",
                        vendorName
                    );

                // ============================================================
                // 8. GET SMTP SETTINGS
                // ============================================================

                string frommail =
                   _configuration["Mail:fromMail"];

                string username =
                    _configuration["Mail:SMTPUSER"];

                string password =
                     _configuration["Mail:SMTPPWD"];

                string server =
                     _configuration["Mail:SMTPSERVER"];

                int port =
                    Convert.ToInt32(
                        _configuration["Mail:SMTPPORT"]
                    );

                log.WriteToLogFile_Debug(
                    "Mail settings loaded",
                    "Mail"
                );

                // ============================================================
                // 9. CREATE MAIL
                // ============================================================

                using (MailMessage mail = new MailMessage())
                {
                    mail.From =
                        new MailAddress(frommail);

                    // ========================================================
                    // TO MAIL
                    // ========================================================

                    if (!string.IsNullOrWhiteSpace(toMail))
                    {
                        mail.To.Add(
                            toMail.Trim()
                        );
                    }

                    // ========================================================
                    // AGENT MAIL
                    // ========================================================

                    if (!string.IsNullOrWhiteSpace(agentMail))
                    {
                        mail.To.Add(
                            agentMail.Trim()
                        );
                    }

                    // ========================================================
                    // CC MAILS
                    // ========================================================

                    if (!string.IsNullOrWhiteSpace(ccMails))
                    {
                        foreach (
                            string cc in ccMails.Split(
                                new[] { ',', ';' },
                                StringSplitOptions.RemoveEmptyEntries
                            )
                        )
                        {
                            if (!string.IsNullOrWhiteSpace(cc))
                            {
                                mail.CC.Add(
                                    cc.Trim()
                                );
                            }
                        }
                    }

                    // ========================================================
                    // SUBJECT
                    // ========================================================

                    mail.Subject = subject;

                    // ========================================================
                    // BODY
                    // ========================================================

                    mail.Body = body;

                    mail.IsBodyHtml = true;

                    // ========================================================
                    // 10. ATTACH GENERATED PREVIEW PDF
                    // ========================================================

                    using (MemoryStream ms =
                        new MemoryStream(pdfBytes))
                    {
                        Attachment attachment =
                            new Attachment(
                                ms,
                                "VendorRegistrationForm.pdf",
                                "application/pdf"
                            );

                        mail.Attachments.Add(
                            attachment
                        );

                        log.WriteToLogFile_Debug(
                            "Vendor Preview PDF attached",
                            "Mail"
                        );

                        // ====================================================
                        // 11. SEND EMAIL
                        // ====================================================

                        using (SmtpClient smtp =
                            new SmtpClient(
                                server,
                                port
                            ))
                        {
                            smtp.Credentials =
                                new System.Net.NetworkCredential(
                                    username,
                                    password
                                );

                            smtp.EnableSsl = true;

                            log.WriteToLogFile_Debug(
                                "Mail sending started",
                                "Mail"
                            );

                            await smtp.SendMailAsync(mail);

                            log.WriteToLogFile_Debug(
                                "Mail Sent completed",
                                "Mail"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "Error while sending mail : " +
                    ex.Message +
                    " | StackTrace: " +
                    ex.StackTrace,
                    "Mail"
                );

                throw;
            }
        }
        private string GenerateVendorHtmlWithData(
    Dictionary<string, object> data,
    IEnumerable<object> goodsList)
        {
            string GetValue(string key)
            {
                if (data == null || !data.ContainsKey(key))
                    return "";

                return data[key]?.ToString() ?? "";
            }

            string HtmlEncode(string value)
            {
                return System.Net.WebUtility.HtmlEncode(
                    value ?? ""
                );
            }

            string vendorName =
                HtmlEncode(GetValue("Trade Name"));

            string billingAddress =
                HtmlEncode(GetValue("Billing Address"));

            string registeredAddress =
                HtmlEncode(GetValue("Registered Address"));

            string natureOfBusiness =
                HtmlEncode(GetValue("Nature of Business"));

            string mobileNumber =
                HtmlEncode(GetValue("Mobile Number"));

            string officeTelephone =
                HtmlEncode(GetValue("Office Telephone"));

            string email =
                HtmlEncode(GetValue("Email ID"));

            string agencyEmail =
                HtmlEncode(GetValue("Agency Email"));

            string contactPerson =
                HtmlEncode(GetValue("Contact Person"));

            string bankName =
                HtmlEncode(GetValue("Bank Name"));

            string accountNumber =
                HtmlEncode(GetValue("Account Number"));

            string ifscCode =
                HtmlEncode(GetValue("IFSC Code"));

            string goodsReturnAddress =
                HtmlEncode(GetValue("Goods Return Address"));

            string creditDays =
                HtmlEncode(GetValue("Credit Days"));

            string discount =
                HtmlEncode(GetValue("Discount"));

            string gstNumber =
                HtmlEncode(GetValue("GST Number"));

            string panNumber =
                HtmlEncode(GetValue("PAN Number"));

            string msmeNumber =
                HtmlEncode(GetValue("MSME Number"));

            string enterpriseType =
                HtmlEncode(GetValue("Enterprise Type"));

            string majorActivity =
                HtmlEncode(GetValue("Major Activity"));

            string legalName =
                HtmlEncode(GetValue("Legal Name"));

            string businessType =
                HtmlEncode(GetValue("Business Type"));

            string nhfsContactPerson =
                HtmlEncode(GetValue("NHFS Contact Person"));

            string formDate =
                HtmlEncode(GetValue("Date"));

            // ------------------------------------------------------------
            // GOODS TABLE
            // ------------------------------------------------------------

            StringBuilder goodsHtml =
                new StringBuilder();

            int goodsIndex = 1;

            if (goodsList != null)
            {
                foreach (var item in goodsList)
                {
                    if (item == null)
                        continue;

                    string description = "";
                    string category = "";
                    string code = "";

                    // Supports your existing model without forcing
                    // one exact property structure here.
                    var type = item.GetType();

                    var property =
                        type.GetProperty("Description");

                    if (property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Goods");

                    if (string.IsNullOrWhiteSpace(description) &&
                        property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Name");

                    if (string.IsNullOrWhiteSpace(description) &&
                        property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Category");

                    if (property != null)
                    {
                        category =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("ItemCode");

                    if (property != null)
                    {
                        code =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    goodsHtml.Append($@"
                <tr>
                    <td style=""text-align:center;"">
                        {goodsIndex}
                    </td>

                    <td>
                        {HtmlEncode(description)}
                    </td>

                    <td>
                        {HtmlEncode(category)}
                    </td>

                    <td>
                        {HtmlEncode(code)}
                    </td>
                </tr>");

                    goodsIndex++;
                }
            }

            if (goodsIndex == 1)
            {
                goodsHtml.Append(@"
            <tr>
                <td colspan=""4"" style=""height:30px;"">
                    &nbsp;
                </td>
            </tr>");
            }

            // ------------------------------------------------------------
            // LOGO
            // ------------------------------------------------------------

            string logoPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Images",
                    "Logo.png"
                );

            string logoHtml = "";

            if (File.Exists(logoPath))
            {
                byte[] logoBytes =
                    File.ReadAllBytes(logoPath);

                string base64Logo =
                    Convert.ToBase64String(logoBytes);

                logoHtml =
                    $"data:image/png;base64,{base64Logo}";
            }

            // ------------------------------------------------------------
            // WATERMARK
            // ------------------------------------------------------------

            string watermark = "DRAFT";

            // ------------------------------------------------------------
            // FINAL HTML
            // ------------------------------------------------------------

            string html = $@"
<!DOCTYPE html>

<html>

<head>

<meta charset=""UTF-8"" />

<title>Vendor Registration Form</title>

<style>

    @page {{
        size: A4;
        margin: 0;
    }}

    html,
    body {{
        margin: 0;
        padding: 0;
        background: white;
    }}

    body {{
        font-family: ""Times New Roman"", serif;
        font-size: 12px;
    }}

    .page {{
        position: relative;

        width: 1094px;
        min-height: 1123px;

        margin: 0 auto;

        padding:
            25px
            35px
            25px
            35px;

        box-sizing: border-box;

        background: white;

        overflow: hidden;
    }}

    .page * {{
        position: relative;
        z-index: 1;
    }}

    .watermark {{
        position: absolute !important;

        top: 50% !important;
        left: 50% !important;

        transform:
            translate(-50%, -50%)
            rotate(-35deg) !important;

        width: 90%;

        text-align: center;

        font-size: 260px;

        font-weight: 900;

        font-family: ""Arial Black"", sans-serif;

        text-transform: uppercase;

        letter-spacing: 10px;

        color: rgba(0, 0, 0, 0.18);

        opacity: 0.25;

        pointer-events: none;

        user-select: none;

        z-index: 0 !important;

        white-space: nowrap;
    }}

    .logo-box {{
        width: 145px;
        height: 55px;

        border: 1px solid #000;

        display: flex;

        align-items: center;

        justify-content: center;

        margin-bottom: 5px;
    }}

    .logo {{
        width: 125px;
        height: auto;
    }}

    .header {{
        text-align: center;

        font-size: 13px;

        font-weight: bold;

        margin-bottom: 10px;
    }}

    h2 {{
        text-align: center;

        font-size: 18px;

        margin:
            8px
            0
            12px
            0;

        text-decoration: underline;
    }}

    .ref-table {{
        width: 100%;

        border-collapse: collapse;

        margin-bottom: 5px;
    }}

    .ref-table td {{
        width: 50%;

        vertical-align: top;

        padding: 0;
    }}

    .right-align {{
        text-align: right;
    }}

    .field-line {{
        display: flex;

        align-items: center;

        min-height: 22px;
    }}

    .field-line label {{
        width: 230px;

        text-align: left;

        font-weight: bold;

        flex-shrink: 0;
    }}

    .readonly-field {{
        display: inline-block;

        text-align: left;

        flex: 1;

        border-bottom: 1px solid #000;

        min-height: 16px;

        padding-left: 3px;
    }}

    .section-title {{
        font-weight: bold;

        margin-top: 8px;

        margin-bottom: 3px;
    }}

    table.data-table {{
        width: 100%;

        border-collapse: collapse;

        margin-top: 5px;

        margin-bottom: 8px;
    }}

    table.data-table th,
    table.data-table td {{
        border: 1px solid #000;

        padding: 4px;

        vertical-align: top;
    }}

    table.data-table th {{
        text-align: center;

        font-weight: bold;
    }}

    .signature {{
        margin-top: 30px;

        width: 100%;
    }}

    .signature-table {{
        width: 100%;

        border-collapse: collapse;
    }}

    .signature-table td {{
        width: 50%;

        height: 80px;

        vertical-align: bottom;

        padding: 5px;
    }}

    .signature-line {{
        border-top: 1px solid #000;

        width: 80%;

        margin-top: 35px;
    }}

    .small {{
        font-size: 11px;
    }}

</style>

</head>

<body>

<div class=""page"">

    <div class=""watermark"">
        {HtmlEncode(watermark)}
    </div>

    {(string.IsNullOrWhiteSpace(logoHtml)
                ? ""
                : $@"<div class=""logo-box"">
                <img
                    src=""{logoHtml}""
                    alt=""Logo""
                    class=""logo""
                />
            </div>")}

    <div class=""header"">
        No. 7, Basudev Street, Pondy Bazaar,
        T. Nagar, Chennai – 600 017
        Contact: 044 24340714
    </div>

    <h2>
        VENDOR REGISTRATION FORM
    </h2>

    <!-- REF / CODE / DATE / LOCATION -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>Ref. No.:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

                <div class=""field-line"">
                    <label>CODE NO.:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

            </td>

            <td class=""right-align"">

                <div class=""field-line"">
                    <label>Date:</label>
                    <span class=""readonly-field"">
                        {formDate}
                    </span>
                </div>

                <div class=""field-line"">
                    <label>LOCATION:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- BASIC DETAILS -->

    <div class=""field-line"">
        <label>1. Name of Vendor:</label>
        <span class=""readonly-field"">
            {vendorName}
        </span>
    </div>

    <div class=""field-line"">
        <label>2. Address:</label>
        <span class=""readonly-field"">
            {billingAddress}
        </span>
    </div>

    <div class=""field-line"">
        <label>Registered Office:</label>
        <span class=""readonly-field"">
            {registeredAddress}
        </span>
    </div>

    <div class=""field-line"">
        <label>3. Nature of Business:</label>
        <span class=""readonly-field"">
            {natureOfBusiness}
        </span>
    </div>


    <!-- CONTACT -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>4. Contact No. 1:</label>
                    <span class=""readonly-field"">
                        {mobileNumber}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>Contact No. 2:</label>
                    <span class=""readonly-field"">
                        {officeTelephone}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <div class=""field-line"">
        <label>Email ID:</label>
        <span class=""readonly-field"">
            {email}
        </span>
    </div>

    <div class=""field-line"">
        <label>Contact Person:</label>
        <span class=""readonly-field"">
            {contactPerson}
        </span>
    </div>


    <!-- BANK -->

    <div class=""section-title"">
        RTGS / BANK DETAILS
    </div>

    <table class=""data-table"">

        <tr>
            <th>Bank Name</th>
            <th>Account Number</th>
            <th>IFSC Code</th>
        </tr>

        <tr>
            <td>{bankName}</td>
            <td>{accountNumber}</td>
            <td>{ifscCode}</td>
        </tr>

    </table>


    <!-- GOODS RETURN -->

    <div class=""field-line"">
        <label>Goods Return Address:</label>
        <span class=""readonly-field"">
            {goodsReturnAddress}
        </span>
    </div>


    <!-- PAYMENT -->

    <div class=""section-title"">
        PAYMENT DETAILS
    </div>

    <table class=""data-table"">

        <tr>
            <th>Credit Days</th>
            <th>Discount</th>
        </tr>

        <tr>
            <td>{creditDays}</td>
            <td>{discount}</td>
        </tr>

    </table>


    <!-- GST / PAN -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>GST Number:</label>
                    <span class=""readonly-field"">
                        {gstNumber}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>PAN Number:</label>
                    <span class=""readonly-field"">
                        {panNumber}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- MSME -->

    <div class=""section-title"">
        MSME DETAILS
    </div>

    <table class=""data-table"">

        <tr>

            <th>MSME Number</th>

            <th>Enterprise Type</th>

            <th>Major Activity</th>

        </tr>

        <tr>

            <td>
                {msmeNumber}
            </td>

            <td>
                {enterpriseType}
            </td>

            <td>
                {majorActivity}
            </td>

        </tr>

    </table>


    <!-- LEGAL -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>Legal Name:</label>
                    <span class=""readonly-field"">
                        {legalName}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>Business Type:</label>
                    <span class=""readonly-field"">
                        {businessType}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- AGENCY -->

    <div class=""field-line"">
        <label>Agency Email:</label>
        <span class=""readonly-field"">
            {agencyEmail}
        </span>
    </div>


    <!-- MAJOR GOODS -->

    <div class=""section-title"">
        MAJOR GOODS AND SERVICES
    </div>

    <table class=""data-table"">

        <tr>

            <th style=""width:8%;"">
                S.No
            </th>

            <th>
                Description
            </th>

            <th>
                Category
            </th>

            <th>
                Item Code
            </th>

        </tr>

        {goodsHtml}

    </table>


    <!-- NHFS CONTACT -->

    <div class=""field-line"">
        <label>NHFS Contact Person:</label>
        <span class=""readonly-field"">
            {nhfsContactPerson}
        </span>
    </div>


    <!-- SIGNATURE -->

    <div class=""signature"">

        <table class=""signature-table"">

            <tr>

                <td>

                    <div class=""signature-line""></div>

                    <div class=""small"">
                        Vendor Signature
                    </div>

                </td>

                <td>

                    <div class=""signature-line""></div>

                    <div class=""small"">
                        NHFS Authorized Signatory
                    </div>

                </td>

            </tr>

        </table>

    </div>

</div>

</body>

</html>";

            return html;
        }
        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                throw new ArgumentException(
                    "HTML content is empty.",
                    nameof(htmlContent)
                );
            }

            try
            {
                // =========================================================
                // CHECK libwkhtmltox.dll
                // =========================================================

                string baseDirectory = AppContext.BaseDirectory;

                string dllPath = Path.Combine(
                    baseDirectory,
                    "libwkhtmltox.dll"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] Base Directory: {baseDirectory}",
                    "ConvertHtmlToPdf"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] DLL Path: {dllPath}",
                    "ConvertHtmlToPdf"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] DLL Exists: {File.Exists(dllPath)}",
                    "ConvertHtmlToPdf"
                );

                if (!File.Exists(dllPath))
                {
                    throw new FileNotFoundException(
                        $"libwkhtmltox.dll was not found at: {dllPath}"
                    );
                }

                // =========================================================
                // CREATE PDF DOCUMENT
                // =========================================================

                var document = new HtmlToPdfDocument
                {
                    GlobalSettings =
            {
                ColorMode = ColorMode.Color,

                Orientation =
                    Orientation.Portrait,

                PaperSize =
                    PaperKind.A4,

                Margins =
                {
                    Top = 0,
                    Bottom = 0,
                    Left = 0,
                    Right = 0
                },

                DocumentTitle =
                    "Vendor Registration Form"
            },

                    Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = htmlContent,

                    WebSettings =
                    {
                        DefaultEncoding = "utf-8",

                        LoadImages = true,

                        EnableJavascript = false
                    },

                    UseLocalLinks = true
                }
            }
                };

                // =========================================================
                // CONVERT
                // =========================================================

                log.WriteToLogFile_Debug(
                    "[ConvertHtmlToPdf] Starting PDF conversion",
                    "ConvertHtmlToPdf"
                );

                byte[] pdfBytes =
                    _converter.Convert(document);

                // =========================================================
                // VALIDATE RESULT
                // =========================================================

                if (pdfBytes == null ||
                    pdfBytes.Length == 0)
                {
                    throw new Exception(
                        "PDF conversion returned an empty file."
                    );
                }

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] PDF generated successfully. Size: {pdfBytes.Length} bytes",
                    "ConvertHtmlToPdf"
                );

                return pdfBytes;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[ConvertHtmlToPdf] ERROR - " +
                    ex.ToString(),
                    "ConvertHtmlToPdf"
                );

                throw;
            }
        }

        private static string GenerateOTP()
        {
            try
            {
                string numbers = "123456789";

                string otp = string.Empty;

                int length = 6;

                var random = new Random();

                for (int i = 0; i < length; i++)
                {
                    string character;

                    do
                    {
                        int index =
                            random.Next(0, numbers.Length);

                        character =
                            numbers[index].ToString();

                    }
                    while (otp.Contains(character));

                    otp += character;
                }

                return otp;
            }
            catch
            {
                return "";
            }
        }


        // ============================================================
        // SEND SMS
        // ============================================================

        private async Task SendSMS(
            string mobileNumber,
            string otp)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [SendSMS] [API_START] - Sending SMS to mobile: " +
                mobileNumber,
                "SendSMS");

            try
            {
                string otpTemplateId =
                    _configuration["OTP:RedeemTemplateId"] ?? "";

                string otpApiKey =
                    _configuration["OTP:ApiKey"] ?? "";

                string otpClientId =
                    _configuration["OTP:ClientId"] ?? "";

                if (string.IsNullOrWhiteSpace(otpTemplateId))
                {
                    throw new Exception(
                        "OTP Template ID is not configured.");
                }

                if (string.IsNullOrWhiteSpace(otpApiKey))
                {
                    throw new Exception(
                        "OTP API Key is not configured.");
                }

                if (string.IsNullOrWhiteSpace(otpClientId))
                {
                    throw new Exception(
                        "OTP Client ID is not configured.");
                }

                string response =
                    await SendRedeemOTP(
                        mobileNumber,
                        otp,
                        otpTemplateId,
                        otpApiKey,
                        otpClientId);

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendSMS] [API_END] - SMS API response received",
                    "SendSMS");
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendSMS] [ERROR] - Exception occurred while sending SMS: " +
                    ex.Message,
                    "SendSMS");

                throw;
            }
        }


        // ============================================================
        // SEND REDEEM OTP
        // ============================================================

        private async Task<string> SendRedeemOTP(
            string mobileNumber,
            string otp,
            string templateId,
            string apiKey,
            string clientId)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [SendRedeemOTP] [API_START] - Sending OTP to mobile: " +
                mobileNumber,
                "SendRedeemOTP");

            string responseContent = string.Empty;

            try
            {
                string baseUrl =
                    _configuration["OTP:RedeemOTPUrl"] ?? "";

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new Exception(
                        "Redeem OTP URL is not configured.");
                }

                var client =
                    new RestClient(baseUrl);

                var request =
                    new RestRequest("", Method.Get);

                // Same parameters as your old Web Forms code

                request.AddParameter(
                    "SenderId",
                    "NAIDUH");

                request.AddParameter(
                    "Message",
                    $"Your OTP for completing VNH NAIDUHALL'S vendor registration form is {otp}. It is valid for 15 minutes. Thank you.");

                request.AddParameter(
                    "MobileNumbers",
                    mobileNumber);

                request.AddParameter(
                    "TemplateId",
                    templateId);

                request.AddParameter(
                    "ApiKey",
                    apiKey);

                request.AddParameter(
                    "ClientId",
                    clientId);

                var response =
                    await client.ExecuteAsync(request);

                responseContent =
                    response.Content ?? "";

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendRedeemOTP] [API_RESPONSE] - " +
                    responseContent,
                    "SendRedeemOTP");

                if (!response.IsSuccessful)
                {
                    throw new Exception(
                        $"SMS API failed. Status: {response.StatusCode}, Response: {responseContent}");
                }

                // Keep the same JSON handling as old code
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        dynamic value =
                            JsonConvert.DeserializeObject(
                                responseContent);

                        responseContent =
                            value == null
                                ? responseContent
                                : value.ToString();
                    }
                    catch
                    {
                        // If response is not JSON,
                        // keep original response.
                    }
                }
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [SendRedeemOTP] [ERROR] - Exception occurred during SMS API call: " +
                    ex.Message,
                    "SendRedeemOTP");

                throw;
            }

            log.WriteToLogFile_Debug(
                "[VendorCreation] [SendRedeemOTP] [API_END] - SMS API response content length: " +
                (responseContent?.Length ?? 0),
                "SendRedeemOTP");

            return responseContent;
        }


        // ============================================================
        // VERIFY OTP
        // ============================================================

        public async Task<bool> VerifyOTP(
            VerifyOtpRequest request)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [VerifyOTP] [START] - Validating OTP",
                "VerifyOTP");

            try
            {
                if (request == null)
                {
                    throw new Exception(
                        "Invalid OTP verification request.");
                }

                string gstNumber =
                    request.GstNumber?.Trim() ?? "";

                string mobileNumber =
                    request.MobileNumber?.Trim() ?? "";

                string otp =
                    request.Otp?.Trim() ?? "";

                // ----------------------------------------------------
                // Validation
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(gstNumber))
                {
                    throw new Exception(
                        "GST Number is required.");
                }

                if (!Regex.IsMatch(
                        mobileNumber,
                        @"^\d{10}$"))
                {
                    throw new Exception(
                        "Please enter a valid 10-digit Mobile Number.");
                }

                if (!Regex.IsMatch(
                        otp,
                        @"^\d{6}$"))
                {
                    throw new Exception(
                        "Please enter a valid 6-digit OTP.");
                }

                // ----------------------------------------------------
                // Same logic as Tech_otps()
                // Calls:
                //
                // TECH_FETCH_OTP_LoginOTPVerify
                //
                // GST
                // Mobile
                // OTP
                // ----------------------------------------------------

                List<TechOTP> otpValid =
                    await Tech_otps(
                        gstNumber,
                        mobileNumber,
                        mobileNumber,
                        otp);

                if (otpValid == null ||
                    otpValid.Count == 0)
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [VerifyOTP] [VALIDATION_FAILED] - OTP verification returned no records",
                        "VerifyOTP");

                    throw new Exception(
                        "Invalid OTP.");
                }

                var otpResult =
                    otpValid[0];

                if (otpResult.MESSAGE?.ToString()
                        == "OTP Verified Successfully")
                {
                    log.WriteToLogFile_Debug(
                        "[VendorCreation] [VerifyOTP] [VALIDATION_SUCCESS] - OTP verified successfully",
                        "VerifyOTP");

                    return true;
                }

                string message =
                    otpResult.MESSAGE?.ToString()
                    ?? "Invalid OTP.";

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [VerifyOTP] [VALIDATION_FAILED] - " +
                    message,
                    "VerifyOTP");

                throw new Exception(message);
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [VerifyOTP] [ERROR] - Exception occurred during verification: " +
                    ex.Message,
                    "VerifyOTP");

                throw;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [VerifyOTP] [END] - Validate OTP completed",
                    "VerifyOTP");
            }
        }


        // ============================================================
        // TECH_OTP
        // ============================================================

        private async Task<List<TechOTP>> Tech_otps(
            string gstNumber,
            string mobileNumber,
            string otpMobileNumber,
            string otp)
        {
            log.WriteToLogFile_Debug(
                "[VendorCreation] [Tech_otps] [START] - Calling Tech_otps",
                "Tech_otps");

            try
            {
                string query = $@"
                CALL ""{sDBName}"".""TECH_FETCH_OTP_LoginOTPVerify""
                (
                    ?,
                    ?,
                    ?
                )";

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [Tech_otps] [DB_START] - Executing OTP verification SP call",
                    "Tech_otps");

                var result =
                    new List<TechOTP>();

                using (var connection =
                    new OdbcConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command =
                        new OdbcCommand(
                            query,
                            connection))
                    {
                        // IMPORTANT:
                        // ODBC ? parameters are positional.

                        command.Parameters.AddWithValue(
                            "@gstNumber",
                            gstNumber);

                        command.Parameters.AddWithValue(
                            "@otpMobileNumber",
                            otpMobileNumber);

                        command.Parameters.AddWithValue(
                            "@otp",
                            otp);

                        using (var reader =
                            await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item =
                                    new TechOTP();

                                if (reader["MESSAGE"] != DBNull.Value)
                                {
                                    item.MESSAGE =
                                        reader["MESSAGE"]
                                            ?.ToString();
                                }

                                result.Add(item);
                            }
                        }
                    }
                }

                log.WriteToLogFile_Debug(
                    "[VendorCreation] [Tech_otps] [DB_END] - Verification SP execution completed",
                    "Tech_otps");

                return result;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[VendorCreation] [Tech_otps] [ERROR] - " +
                    ex.Message,
                    "Tech_otps");

                throw;
            }
        }

        public List<State> GetStatesByCountry(string country)
        {
            return LoadStates(country);
        }
        public async Task<(bool Success, string Message)> SaveNewProductAsync(
            SaveRequest request)
        {
            if (request == null)
            {
                return (false, "Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.GstNumber))
            {
                return (false, "GST Number is required.");
            }

            try
            {
                string server =
                    _configuration["ConnectionSettings:Server"] ?? "";

                string dbUser =
                    _configuration["ConnectionSettings:DBUser"] ?? "";

                string dbPassword =
                    _configuration["ConnectionSettings:DBPwd"] ?? "";

                string dbName =
                    _configuration["ConnectionSettings:DBName"] ?? "";

                /*
                 * If DBUser / DBPwd are encrypted in your configuration,
                 * decrypt them here using your existing decrypt method.
                 *
                 * Example:
                 *
                 * dbUser = DBConnection.DecryptFun(dbUser);
                 * dbPassword = DBConnection.DecryptFun(dbPassword);
                 */


                /*
                 * =====================================================
                 * GET VENDOR ID USING GST NUMBER
                 * =====================================================
                 */

                int vendorId;

                await using (var connection =
                    new OdbcConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string vendorQuery = $@"
    SELECT ""Id""
    FROM ""{sDBName}"".""TEC_OLED""
    WHERE ""GstNo"" = ?
";


                    await using (var command =
                        new OdbcCommand(vendorQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "GstNo",
                            request.GstNumber.Trim().ToUpper());

                        object? result =
                            await command.ExecuteScalarAsync();

                        if (result == null ||
                            result == DBNull.Value)
                        {
                            //_logger.LogWarning(
                            //    "[NewProduct] Vendor not found for GST: {GST}",
                            //    request.GstNumber);

                            return (
                                false,
                                "Vendor not found for the provided GST Number."
                            );
                        }

                        vendorId =
                            Convert.ToInt32(result);
                    }

                    //_logger.LogInformation(
                    //    "[NewProduct] Vendor ID resolved: {VendorId}",
                    //    vendorId);

                    /*
                     * =================================================
                     * START TRANSACTION
                     * =================================================
                     */

                     using OdbcTransaction transaction =
                         connection.BeginTransaction();

                    try
                    {
                        

                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            vendorId,
                            request.PaymentDetails);

                        /*
                         * =================================================
                         * INSERT MAJOR GOODS / SERVICES
                         * =================================================
                         */

                        await InsertMajorGoodsServices(
                            connection,
                            transaction,
                            vendorId,
                            request.MajorGoodsServices);

                        /*
                         * =================================================
                         * INSERT DOCUMENTS
                         * =================================================
                         */

                        await InsertDocuments1(
                            connection,
                            transaction,
                            vendorId,
                            request.UploadedFiles);

                        /*
                         * =================================================
                         * COMMIT
                         * =================================================
                         */

                        await transaction.CommitAsync();

                        //_logger.LogInformation(
                        //    "[NewProduct] Transaction committed successfully. VendorId: {VendorId}",
                        //    vendorId);

                        return (
                            true,
                            "Product details submitted successfully."
                        );
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(
                        //    ex,
                        //    "[NewProduct] Error occurred. Rolling back transaction.");

                        await transaction.RollbackAsync();

                        return (
                            false,
                            "Unable to save product details."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                //log.WriteToLogFile_Debug(
                  
                //    "[NewProduct] Unexpected error while saving product details.",);

                return (
                    false,
                    "Unable to save product details."
                );
            }
        }
        private async Task InsertDocuments1(
        OdbcConnection conn,
        OdbcTransaction transaction,
        int vendorId,
        List<string> uploadedFiles)
        {
            log.WriteToLogFile_Debug(
                "[NewProduct] [InsertDocuments1] [START] - Inserting vendor documents",
                "InsertDocuments1");

            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                log.WriteToLogFile_Debug(
                    "[NewProduct] [InsertDocuments1] [INFO] - No uploaded files found",
                    "InsertDocuments1");

                return;
            }


            string imagePath =
               _configuration["Folder:ImagePath"];

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new Exception(
                    "ImagePath is not configured in Web.config.");
            }

            string sql = $@"
        INSERT INTO ""{sDBName}"".""TEC_LED7""
        (
            ""Id"",
            ""LineId"",
            ""TradeName"",
            ""DocumentName"",
            ""DocumentType"",
            ""FileData""
        )
        VALUES
        (
            ?,
            ?,
            ?,
            ?,
            ?,
            ?
        )";

            int lineId = 0;

            foreach (string fileName in uploadedFiles)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;

                /*
                 * Only take the file name.
                 *
                 * This prevents a value such as:
                 * C:\Uploads\abc.pdf
                 *
                 * from being treated as the file name.
                 */
                string cleanFileName =
                    Path.GetFileName(fileName);

                /*
                 * Build the complete physical path
                 *
                 * Example:
                 *
                 * ImagePath = D:\VendorDocuments
                 * FileName  = invoice.pdf
                 *
                 * Result:
                 * D:\VendorDocuments\invoice.pdf
                 */
                string documentPath =
                    Path.Combine(
                        imagePath,
                        cleanFileName);

                /*
                 * Convert to the format expected by the DB.
                 *
                 * If your application stores Windows paths,
                 * you can remove this replacement.
                 */
                documentPath =
                    documentPath.Replace("\\", "/");

                using (OdbcCommand cmd =
                       new OdbcCommand(sql, conn, transaction))
                {
                    /*
                     * IMPORTANT:
                     *
                     * ODBC uses positional parameters (?).
                     *
                     * Therefore parameters MUST be added
                     * in exactly the same order as the SQL.
                     */

                    cmd.Parameters.Add(
                        "Id",
                        OdbcType.Int).Value =
                        vendorId;

                    cmd.Parameters.Add(
                        "LineId",
                        OdbcType.Int).Value =
                        lineId;

                    /*
                     * TradeName
                     *
                     * If TEC_LED7 requires the vendor trade name,
                     * replace this with the actual value.
                     */
                    cmd.Parameters.Add(
                        "TradeName",
                        OdbcType.VarChar).Value =
                        "";

                    cmd.Parameters.Add(
                        "DocumentName",
                        OdbcType.VarChar).Value =
                        cleanFileName;

                    cmd.Parameters.Add(
                        "DocumentType",
                        OdbcType.VarChar).Value =
                        "Performa Document";

                    cmd.Parameters.Add(
                        "FileData",
                        OdbcType.VarChar).Value =
                        documentPath;

                    cmd.ExecuteNonQuery();
                }

                log.WriteToLogFile_Debug(
                    "[NewProduct] [InsertDocuments1] [INSERTED] - " +
                    $"VendorId: {vendorId}, " +
                    $"LineId: {lineId}, " +
                    $"DocumentName: {cleanFileName}, " +
                    $"DocumentPath: {documentPath}",
                    "InsertDocuments1");

                lineId++;
            }

            log.WriteToLogFile_Debug(
                "[NewProduct] [InsertDocuments1] [END] - Documents inserted successfully",
                "InsertDocuments1");
        }

        public async Task<ApiResponse> SaveDraft(
           int page,
           FormDataModel model,
           UploadedFilesModel uploadedFiles)
        {
            OdbcConnection connection = null;

            OdbcTransaction transaction = null;

            try
            {
                log.WriteToLogFile_Debug(
                    $"[VendorCreation] [SaveDraft] START - Page: {page}",
                    "SaveDraft"
                );


                // -------------------------------------------------
                // VALIDATION
                // -------------------------------------------------

                if (model == null)
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Form data is required",
                        null
                    );
                }


                if (string.IsNullOrWhiteSpace(model.GstNumber))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "GST Number is required",
                        null
                    );
                }


                string gstNumber =
                    model.GstNumber.Trim().ToUpper();


                // -------------------------------------------------
                // CHECK EXISTING DRAFT
                // -------------------------------------------------

                string existingDraftId =
                    await GetExistingDraftId(gstNumber);


                bool isExistingDraft =
                    !string.IsNullOrWhiteSpace(existingDraftId);


                int id;


                // -------------------------------------------------
                // EXISTING DRAFT
                // -------------------------------------------------

                if (isExistingDraft)
                {
                    id = Convert.ToInt32(existingDraftId);


                    log.WriteToLogFile_Debug(
                        $"[SaveDraft] Existing draft found. Id: {id}",
                        "SaveDraft"
                    );


                    await DeleteDraftData(id);
                }


                // -------------------------------------------------
                // NEW DRAFT
                // -------------------------------------------------

                else
                {
                    bool gstExists =
                        await CheckNonDraftGstExists(gstNumber);


                    if (gstExists)
                    {
                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Failure,
                            "GST Number already exists",
                            null
                        );
                    }


                    id =
                        await GetNextId();
                }


                // -------------------------------------------------
                // OPEN HANA CONNECTION
                // -------------------------------------------------

                connection =
                    new OdbcConnection(connectionString);

                await connection.OpenAsync();


                transaction =
                    connection.BeginTransaction();


                // -------------------------------------------------
                // HEADER
                // -------------------------------------------------

                await InsertHeaderDetails(
                    connection,
                    transaction,
                    id,
                    model
                );


                // -------------------------------------------------
                // PAGE BASED SAVE
                // -------------------------------------------------

                switch (page)
                {
                    case 1:

                        break;


                    case 2:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );

                        break;


                    case 3:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );


                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            id,
                            model.PaymentDetails
                        );

                        break;


                    case 4:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );


                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            id,
                            model.PaymentDetails
                        );


                        await InsertBusinessDetails(
                            connection,
                            transaction,
                            id,
                            model.OtherBusinessLocations
                        );


                        await InsertPartnerDetails(
                            connection,
                            transaction,
                            id,
                            model.BusinessPartners
                        );

                        break;


                    case 5:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );


                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            id,
                            model.PaymentDetails
                        );


                        await InsertBusinessDetails(
                            connection,
                            transaction,
                            id,
                            model.OtherBusinessLocations
                        );


                        await InsertPartnerDetails(
                            connection,
                            transaction,
                            id,
                            model.BusinessPartners
                        );


                        await InsertOperationalContacts(
                            connection,
                            transaction,
                            id,
                            model.OperationalContacts
                        );


                        await InsertMajorGoodsServices(
                            connection,
                            transaction,
                            id,
                            model.MajorGoodsServices
                        );

                        break;


                    case 6:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );


                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            id,
                            model.PaymentDetails
                        );


                        await InsertBusinessDetails(
                            connection,
                            transaction,
                            id,
                            model.OtherBusinessLocations
                        );


                        await InsertPartnerDetails(
                            connection,
                            transaction,
                            id,
                            model.BusinessPartners
                        );


                        await InsertOperationalContacts(
                            connection,
                            transaction,
                            id,
                            model.OperationalContacts
                        );


                        await InsertMajorGoodsServices(
                            connection,
                            transaction,
                            id,
                            model.MajorGoodsServices
                        );

                        break;


                    case 7:

                        await InsertDocuments(
                            connection,
                            transaction,
                            id,
                            model,
                            uploadedFiles
                        );


                        await InsertPaymentDetails(
                            connection,
                            transaction,
                            id,
                            model.PaymentDetails
                        );


                        await InsertBusinessDetails(
                            connection,
                            transaction,
                            id,
                            model.OtherBusinessLocations
                        );


                        await InsertPartnerDetails(
                            connection,
                            transaction,
                            id,
                            model.BusinessPartners
                        );


                        await InsertOperationalContacts(
                            connection,
                            transaction,
                            id,
                            model.OperationalContacts
                        );


                        await InsertMajorGoodsServices(
                            connection,
                            transaction,
                            id,
                            model.MajorGoodsServices
                        );

                        break;


                    default:

                        throw new Exception(
                            $"Invalid page number: {page}"
                        );
                }


                // -------------------------------------------------
                // COMMIT
                // -------------------------------------------------

                transaction.Commit();


                log.WriteToLogFile_Debug(
                    $"[SaveDraft] SUCCESS - Id: {id}, Page: {page}",
                    "SaveDraft"
                );


                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Success,
                    "Draft saved successfully",
                    new
                    {
                        Id = id,
                        Page = page,
                        GstNumber = gstNumber
                    }
                );
            }
            catch (Exception ex)
            {
                try
                {
                    transaction?.Rollback();
                }
                catch
                {
                }


                log.WriteToLogFile_Debug(
                    $"[SaveDraft] ERROR: {ex}",
                    "SaveDraft"
                );


                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "Error while saving draft",
                    null
                );
            }
            finally
            {
                transaction?.Dispose();


                if (connection != null)
                {
                    await connection.CloseAsync();

                    connection.Dispose();
                }
            }
        }


        // =====================================================
        // GET EXISTING DRAFT ID
        // =====================================================

        private async Task<string> GetExistingDraftId(
            string gstNumber)
        {
            string query = @$"
                SELECT TOP 1 ""Id""
                FROM ""{sDBName}"".""TEC_OLED""
                WHERE ""GstNo"" = ?
                AND IFNULL(""Draft"", 'N') = 'Y'
                ORDER BY ""Id"" DESC";


            using var connection =
                new OdbcConnection(connectionString);


            await connection.OpenAsync();


            using var command =
                new OdbcCommand(
                    query,
                    connection
                );


            command.Parameters.AddWithValue(
                "@GstNo",
                gstNumber
            );


            object result =
                await command.ExecuteScalarAsync();


            if (result == null ||
                result == DBNull.Value)
            {
                return null;
            }


            return result.ToString();
        }


        // =====================================================
        // CHECK NON-DRAFT GST
        // =====================================================

        private async Task<bool> CheckNonDraftGstExists(
            string gstNumber)
        {
            string query = @$"
                SELECT COUNT(*)
                FROM ""{sDBName}"".""TEC_OLED""
                WHERE ""GstNo"" = ?
                AND IFNULL(""Draft"", 'N') <> 'Y'";


            using var connection =
                new OdbcConnection(connectionString);


            await connection.OpenAsync();


            using var command =
                new OdbcCommand(
                    query,
                    connection
                );


            command.Parameters.AddWithValue(
                "@GstNo",
                gstNumber
            );


            object result =
                await command.ExecuteScalarAsync();


            return Convert.ToInt32(result) > 0;
        }


        // =====================================================
        // GET NEXT ID
        // =====================================================

        private async Task<int> GetNextId()
        {
            string query = @$"
                SELECT COALESCE(MAX(""Id""), 0) + 1
                FROM ""{sDBName}"".""TEC_OLED""";


            using var connection =
                new OdbcConnection(connectionString);


            await connection.OpenAsync();


            using var command =
                new OdbcCommand(
                    query,
                    connection
                );


            object result =
                await command.ExecuteScalarAsync();


            return Convert.ToInt32(result);
        }


        // =====================================================
        // DELETE EXISTING DRAFT
        // =====================================================

        private async Task DeleteDraftData(int id)
        {
            using var connection =
                new OdbcConnection(connectionString);


            await connection.OpenAsync();


            using var transaction =
                connection.BeginTransaction();


            try
            {
                string[] queries =
                {
                    @$"DELETE FROM ""{sDBName}"".""TEC_LED7"" WHERE ""Id"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED6"" WHERE ""ID"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED5"" WHERE ""ID"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED4"" WHERE ""Id"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED3"" WHERE ""ID"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED2"" WHERE ""Id"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_LED1"" WHERE ""Id"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""PaymentDetails"" WHERE ""Id"" = ?",

                    @$"DELETE FROM ""{sDBName}"".""TEC_OLED"" WHERE ""Id"" = ?"
                };


                foreach (string query in queries)
                {
                    using var command =
                        new OdbcCommand(
                            query,
                            connection,
                            transaction
                        );


                    command.Parameters.AddWithValue(
                        "@Id",
                        id
                    );


                    await command.ExecuteNonQueryAsync();
                }


                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();

                throw;
            }
        }


        // =====================================================
        // INSERT DOCUMENTS
        //
        // IMPORTANT:
        // We DO NOT read the file.
        // We DO NOT convert to Base64.
        //
        // We take:
        //
        // appsettings path
        //       +
        // uploaded filename
        //
        // and save the complete path.
        // =====================================================

        private async Task InsertDocuments(
     OdbcConnection connection,
     OdbcTransaction transaction,
     int id,
     FormDataModel model,
     UploadedFilesModel uploadedFiles)
        {
            log.WriteToLogFile_Debug(
                $"[InsertDocuments] START - Id: {id}",
                "InsertDocuments"
            );

            if (uploadedFiles == null)
            {
                log.WriteToLogFile_Debug(
                    "[InsertDocuments] uploadedFiles is null",
                    "InsertDocuments"
                );

                return;
            }

            string tradeName =
                model.TradeName?.Trim() ?? "";

            string uploadFolder =
                _configuration["Folder:Path"];

            if (string.IsNullOrWhiteSpace(uploadFolder))
            {
                throw new Exception(
                    "FileUpload:TempFolder is missing in appsettings.json"
                );
            }

            // -------------------------------------------------
            // SINGLE DOCUMENTS
            // -------------------------------------------------

            var documents =
                new List<(int LineId, string DocumentType, string FileName)>
                {
            (
                1,
                "PAN Card",
                uploadedFiles.PanCard ?? ""
            ),

            (
                2,
                "GST Certificate",
                uploadedFiles.GstCertificate ?? ""
            ),

            (
                3,
                "Bank Account",
                uploadedFiles.BankProof ?? ""
            ),

            (
                4,
                "MSME Certificate",
                uploadedFiles.MsmeCertificate ?? ""
            )
                };

            // -------------------------------------------------
            // INSERT SINGLE DOCUMENTS
            // -------------------------------------------------

            foreach (var document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.FileName))
                {
                    continue;
                }

                string fullFilePath =
                    Path.Combine(
                        uploadFolder,
                        document.FileName
                    );

                log.WriteToLogFile_Debug(
                    $"[InsertDocuments] " +
                    $"DocumentType: {document.DocumentType}, " +
                    $"FileName: {document.FileName}, " +
                    $"Path: {fullFilePath}",
                    "InsertDocuments"
                );

                string query = @$"
            INSERT INTO ""{sDBName}"".""TEC_LED7""
            (
                ""Id"",
                ""LineId"",
                ""TradeName"",
                ""DocumentName"",
                ""DocumentType"",
                ""FileData""
            )
            VALUES
            (
                ?,
                ?,
                ?,
                ?,
                ?,
                ?
            )";

                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );

                command.Parameters.AddWithValue(
                    "@Id",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    document.LineId
                );

                command.Parameters.AddWithValue(
                    "@TradeName",
                    tradeName
                );

                command.Parameters.AddWithValue(
                    "@DocumentName",
                    document.FileName
                );

                command.Parameters.AddWithValue(
                    "@DocumentType",
                    document.DocumentType
                );

                // FileData stores physical file path
                command.Parameters.AddWithValue(
                    "@FileData",
                    fullFilePath
                );

                await command.ExecuteNonQueryAsync();

                log.WriteToLogFile_Debug(
                    $"[InsertDocuments] Inserted - " +
                    $"DocumentType: {document.DocumentType}, " +
                    $"FileName: {document.FileName}",
                    "InsertDocuments"
                );
            }

            // -------------------------------------------------
            // MULTIPLE PROFORMA INVOICES
            // -------------------------------------------------

            if (uploadedFiles.PerformaInvoice != null &&
                uploadedFiles.PerformaInvoice.Count > 0)
            {
                log.WriteToLogFile_Debug(
                    $"[InsertDocuments] Proforma Invoice Count: " +
                    $"{uploadedFiles.PerformaInvoice.Count}",
                    "InsertDocuments"
                );

                foreach (string fileName in uploadedFiles.PerformaInvoice)
                {
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        continue;
                    }

                    string fullFilePath =
                        Path.Combine(
                            uploadFolder,
                            fileName
                        );

                    log.WriteToLogFile_Debug(
                        $"[InsertDocuments] " +
                        $"DocumentType: Performa Invoice, " +
                        $"FileName: {fileName}, " +
                        $"Path: {fullFilePath}",
                        "InsertDocuments"
                    );

                    string query = @$"
                INSERT INTO ""{sDBName}"".""TEC_LED7""
                (
                    ""Id"",
                    ""LineId"",
                    ""TradeName"",
                    ""DocumentName"",
                    ""DocumentType"",
                    ""FileData""
                )
                VALUES
                (
                    ?,
                    ?,
                    ?,
                    ?,
                    ?,
                    ?
                )";

                    using var command =
                        new OdbcCommand(
                            query,
                            connection,
                            transaction
                        );

                    command.Parameters.AddWithValue(
                        "@Id",
                        id
                    );

                    command.Parameters.AddWithValue(
                        "@LineId",
                        5
                    );

                    command.Parameters.AddWithValue(
                        "@TradeName",
                        tradeName
                    );

                    command.Parameters.AddWithValue(
                        "@DocumentName",
                        fileName
                    );

                    command.Parameters.AddWithValue(
                        "@DocumentType",
                        "Performa Invoice"
                    );

                    // FileData stores physical file path
                    command.Parameters.AddWithValue(
                        "@FileData",
                        fullFilePath
                    );

                    await command.ExecuteNonQueryAsync();

                    log.WriteToLogFile_Debug(
                        $"[InsertDocuments] " +
                        $"Inserted Proforma Invoice - " +
                        $"FileName: {fileName}",
                        "InsertDocuments"
                    );
                }
            }

            log.WriteToLogFile_Debug(
                "[InsertDocuments] END",
                "InsertDocuments"
            );
        }


        // =====================================================
        // FINAL SUBMISSION METHODS
        //
        // IMPORTANT:
        // These methods are ONLY for SubmitVendor.
        // They do NOT contain draft/update branching.
        // SaveDraft can continue using the old draft methods.
        // =====================================================

        private string GetStringProperty(object source, string propertyName)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
                return "";

            var property = source.GetType().GetProperty(propertyName);

            if (property == null)
                return "";

            return property.GetValue(source)?.ToString()?.Trim() ?? "";
        }

        private async Task DeleteSubmissionChildData(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id)
        {
            string[] queries =
            {
                $@"DELETE FROM ""{sDBName}"".""TEC_LED7"" WHERE ""Id"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED6"" WHERE ""ID"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED5"" WHERE ""ID"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED4"" WHERE ""Id"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED3"" WHERE ""ID"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED2"" WHERE ""Id"" = ?",
                $@"DELETE FROM ""{sDBName}"".""TEC_LED1"" WHERE ""Id"" = ?",
                $@"DELETE FROM ""{sDBName}"".""PaymentDetails"" WHERE ""Id"" = ?"
            };

            foreach (string query in queries)
            {
                using var command = new OdbcCommand(
                    query,
                    connection,
                    transaction);

                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task SubmitInsertHeaderDetails(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            FormDataModel model)
        {
            string query = $@"
                INSERT INTO ""{sDBName}"".""TEC_OLED""
                (
                    ""Id"",
                    ""TName"",
                    ""Raddress1"",
                    ""Raddress2"",
                    ""Raddress3"",
                    ""Rcountry"",
                    ""Rstate"",
                    ""registeredOfficeCity"",
                    ""Rzipcode"",
                    ""Baddress1"",
                    ""Baddress2"",
                    ""Baddress3"",
                    ""Bcountry"",
                    ""Bstate"",
                    ""businessBillingCity"",
                    ""Bzipcode"",
                    ""NatureOfBusinessActivity"",
                    ""DateOfEstablishment"",
                    ""ContactPersonName"",
                    ""Designation"",
                    ""EmailId"",
                    ""MobileNo"",
                    ""OfficeTelephoneNo"",
                    ""TANNo"",
                    ""MsmeRegistrationStatus"",
                    ""BankName"",
                    ""AccountName"",
                    ""AccountNumber"",
                    ""IfscCode"",
                    ""BranchCode"",
                    ""BankAddress"",
                    ""GstNo"",
                    ""DeclarationName"",
                    ""DeclarationDesignation"",
                    ""Draft"",
                    ""AppliedDate"",
                    ""PartnerType"",
                    ""PanNo"",
                    ""MSMENo"",
                    ""EnterpriseType"",
                    ""BusinessType"",
                    ""AgencyEmail"",
                    ""AgencyName"",
                    ""VerificationNo"",
                    ""Gaddress1"",
                    ""Gaddress2"",
                    ""Gaddress3"",
                    ""Gcountry"",
                    ""Gstate"",
                    ""Gcity"",
                    ""Gzipcode"",
                    ""Saddress1"",
                    ""Saddress2"",
                    ""Saddress3"",
                    ""Scountry"",
                    ""Sstate"",
                    ""Scity"",
                    ""Szipcode"",
                    ""ContactPerson""
                )
                VALUES
                (
                    ?, ?, ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?, ?
                )";

            using var command = new OdbcCommand(
                query,
                connection,
                transaction);

            AddHeaderParameters(command, id, model, "N");

            await command.ExecuteNonQueryAsync();
        }

        private async Task SubmitUpdateHeaderDetails(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            FormDataModel model)
        {
            string query = $@"
                UPDATE ""{sDBName}"".""TEC_OLED""
                SET
                    ""TName"" = ?,
                    ""Raddress1"" = ?,
                    ""Raddress2"" = ?,
                    ""Raddress3"" = ?,
                    ""Rcountry"" = ?,
                    ""Rstate"" = ?,
                    ""registeredOfficeCity"" = ?,
                    ""Rzipcode"" = ?,
                    ""Baddress1"" = ?,
                    ""Baddress2"" = ?,
                    ""Baddress3"" = ?,
                    ""Bcountry"" = ?,
                    ""Bstate"" = ?,
                    ""businessBillingCity"" = ?,
                    ""Bzipcode"" = ?,
                    ""NatureOfBusinessActivity"" = ?,
                    ""DateOfEstablishment"" = ?,
                    ""ContactPersonName"" = ?,
                    ""Designation"" = ?,
                    ""EmailId"" = ?,
                    ""MobileNo"" = ?,
                    ""OfficeTelephoneNo"" = ?,
                    ""TANNo"" = ?,
                    ""MsmeRegistrationStatus"" = ?,
                    ""BankName"" = ?,
                    ""AccountName"" = ?,
                    ""AccountNumber"" = ?,
                    ""IfscCode"" = ?,
                    ""BranchCode"" = ?,
                    ""BankAddress"" = ?,
                    ""GstNo"" = ?,
                    ""DeclarationName"" = ?,
                    ""DeclarationDesignation"" = ?,
                    ""Draft"" = ?,
                    ""AppliedDate"" = ?,
                    ""PartnerType"" = ?,
                    ""PanNo"" = ?,
                    ""MSMENo"" = ?,
                    ""EnterpriseType"" = ?,
                    ""BusinessType"" = ?,
                    ""AgencyEmail"" = ?,
                    ""AgencyName"" = ?,
                    ""VerificationNo"" = ?,
                    ""Gaddress1"" = ?,
                    ""Gaddress2"" = ?,
                    ""Gaddress3"" = ?,
                    ""Gcountry"" = ?,
                    ""Gstate"" = ?,
                    ""Gcity"" = ?,
                    ""Gzipcode"" = ?,
                    ""Saddress1"" = ?,
                    ""Saddress2"" = ?,
                    ""Saddress3"" = ?,
                    ""Scountry"" = ?,
                    ""Sstate"" = ?,
                    ""Scity"" = ?,
                    ""Szipcode"" = ?,
                    ""ContactPerson"" = ?
                WHERE ""Id"" = ?";

            using var command = new OdbcCommand(
                query,
                connection,
                transaction);

            AddHeaderParametersWithoutId(command, model, "N");
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        private void AddHeaderParameters(
            OdbcCommand command,
            int id,
            FormDataModel model,
            string draft)
        {
            command.Parameters.AddWithValue("@Id", id);
            AddHeaderParametersWithoutId(command, model, draft);
        }

        private void AddHeaderParametersWithoutId(
            OdbcCommand command,
            FormDataModel model,
            string draft)
        {
            var registered = model.RegisteredOffice;
            var billing = model.BillingAddress;
            var goodsReturn = model.GoodsReturnAddress;
            var shipping = model.ShippingAddress;
            var bank = model.BankDetails;
            var msme = model.MsmeDetails;
            var payment = model.PaymentDetails;

            string businessType =
                GetStringProperty(payment, "TypeOfVendor");

            if (string.IsNullOrWhiteSpace(businessType))
                businessType = GetStringProperty(payment, "BusinessType");

            string agencyEmail =
                GetStringProperty(payment, "AgencyEmail");

            string agencyName =
                GetStringProperty(payment, "AgencyName");

            command.Parameters.AddWithValue("@TName", model.TradeName ?? "");
            command.Parameters.AddWithValue("@Raddress1", registered?.Address1 ?? "");
            command.Parameters.AddWithValue("@Raddress2", registered?.Address2 ?? "");
            command.Parameters.AddWithValue("@Raddress3", registered?.Address3 ?? "");
            command.Parameters.AddWithValue("@Rcountry", registered?.Country ?? "");
            command.Parameters.AddWithValue("@Rstate", registered?.State ?? "");
            command.Parameters.AddWithValue("@registeredOfficeCity", registered?.City ?? "");
            command.Parameters.AddWithValue("@Rzipcode", registered?.Pincode ?? "");

            command.Parameters.AddWithValue("@Baddress1", billing?.Address1 ?? "");
            command.Parameters.AddWithValue("@Baddress2", billing?.Address2 ?? "");
            command.Parameters.AddWithValue("@Baddress3", billing?.Address3 ?? "");
            command.Parameters.AddWithValue("@Bcountry", billing?.Country ?? "");
            command.Parameters.AddWithValue("@Bstate", billing?.State ?? "");
            command.Parameters.AddWithValue("@businessBillingCity", billing?.City ?? "");
            command.Parameters.AddWithValue("@Bzipcode", billing?.Pincode ?? "");

            command.Parameters.AddWithValue("@NatureOfBusinessActivity", model.NatureOfBusiness ?? "");
            command.Parameters.AddWithValue("@DateOfEstablishment", model.DateOfEstablishment ?? "");
            command.Parameters.AddWithValue("@ContactPersonName", model.ContactPersonName ?? "");
            command.Parameters.AddWithValue("@Designation", model.Designation ?? "");
            command.Parameters.AddWithValue("@EmailId", model.Email ?? "");
            command.Parameters.AddWithValue("@MobileNo", model.MobileNumber ?? "");
            command.Parameters.AddWithValue("@OfficeTelephoneNo", model.OfficeTelephoneNo ?? "");
            command.Parameters.AddWithValue("@TANNo", model.TanNumber ?? "");

            command.Parameters.AddWithValue("@MsmeRegistrationStatus", msme?.MsmeRegistrationStatus ?? "");

            command.Parameters.AddWithValue("@BankName", bank?.BankName ?? "");
            command.Parameters.AddWithValue("@AccountName", bank?.AccountNameHolder ?? "");
            command.Parameters.AddWithValue("@AccountNumber", bank?.AccountNumber ?? "");
            command.Parameters.AddWithValue("@IfscCode", bank?.IfscCode ?? "");
            command.Parameters.AddWithValue("@BranchCode", bank?.BranchCode ?? "");
            command.Parameters.AddWithValue("@BankAddress", bank?.BankAddress ?? "");

            command.Parameters.AddWithValue("@GstNo", model.GstNumber ?? "");
            command.Parameters.AddWithValue("@DeclarationName", model.DeclarationName ?? "");
            command.Parameters.AddWithValue("@DeclarationDesignation", model.DeclarationDesignation ?? "");
            command.Parameters.AddWithValue("@Draft", draft);
            command.Parameters.AddWithValue("@AppliedDate", DateTime.Now.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@PartnerType", model.PartnerType ?? "");
            command.Parameters.AddWithValue("@PanNo", model.PanNumber ?? "");
            command.Parameters.AddWithValue("@MSMENo", msme?.MsmeNo ?? "");
            command.Parameters.AddWithValue("@EnterpriseType", msme?.EnterpriseType ?? "");
            command.Parameters.AddWithValue("@BusinessType", businessType);
            command.Parameters.AddWithValue("@AgencyEmail", agencyEmail);
            command.Parameters.AddWithValue("@AgencyName", agencyName);
            command.Parameters.AddWithValue("@VerificationNo", model.MobileNumber ?? "");

            command.Parameters.AddWithValue("@Gaddress1", goodsReturn?.Address1 ?? "");
            command.Parameters.AddWithValue("@Gaddress2", goodsReturn?.Address2 ?? "");
            command.Parameters.AddWithValue("@Gaddress3", goodsReturn?.Address3 ?? "");
            command.Parameters.AddWithValue("@Gcountry", goodsReturn?.Country ?? "");
            command.Parameters.AddWithValue("@Gstate", goodsReturn?.State ?? "");
            command.Parameters.AddWithValue("@Gcity", goodsReturn?.City ?? "");
            command.Parameters.AddWithValue("@Gzipcode", goodsReturn?.Pincode ?? "");

            command.Parameters.AddWithValue("@Saddress1", shipping?.Address1 ?? "");
            command.Parameters.AddWithValue("@Saddress2", shipping?.Address2 ?? "");
            command.Parameters.AddWithValue("@Saddress3", shipping?.Address3 ?? "");
            command.Parameters.AddWithValue("@Scountry", shipping?.Country ?? "");
            command.Parameters.AddWithValue("@Sstate", shipping?.State ?? "");
            command.Parameters.AddWithValue("@Scity", shipping?.City ?? "");
            command.Parameters.AddWithValue("@Szipcode", shipping?.Pincode ?? "");
            command.Parameters.AddWithValue("@ContactPerson", model.ContactPerson ?? "");
        }

        private async Task SubmitInsertPaymentDetails(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            PaymentDetailsModel payment)
        {
            string query = $@"
                INSERT INTO ""{sDBName}"".""PaymentDetails""
                (
                    ""Id"",
                    ""CreditDays"",
                    ""DisCount"",
                    ""MarkDownTax0"",
                    ""MarkDownWithoutTax0"",
                    ""MarkDownTax3"",
                    ""MarkDownWithoutTax3"",
                    ""MarkDownTax5"",
                    ""MarkDownWithoutTax5"",
                    ""MarkDownTax18"",
                    ""MarkDownWithoutTax18"",
                    ""BusinessType"",
                    ""AgencyEmail"",
                    ""AgencyName"",
                    ""PriceType""
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using var command = new OdbcCommand(query, connection, transaction);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@CreditDays", GetStringProperty(payment, "CreditDays"));
            command.Parameters.AddWithValue("@DisCount", GetStringProperty(payment, "DisCount"));
            command.Parameters.AddWithValue("@MarkDownTax0", GetStringProperty(payment, "MarkDownWithTax0"));
            command.Parameters.AddWithValue("@MarkDownWithoutTax0", GetStringProperty(payment, "MarkDownWithoutTax0"));
            command.Parameters.AddWithValue("@MarkDownTax3", GetStringProperty(payment, "MarkDownWithTax3"));
            command.Parameters.AddWithValue("@MarkDownWithoutTax3", GetStringProperty(payment, "MarkDownWithoutTax3"));
            command.Parameters.AddWithValue("@MarkDownTax5", GetStringProperty(payment, "MarkDownWithTax5"));
            command.Parameters.AddWithValue("@MarkDownWithoutTax5", GetStringProperty(payment, "MarkDownWithoutTax5"));
            command.Parameters.AddWithValue("@MarkDownTax18", GetStringProperty(payment, "MarkDownWithTax18"));
            command.Parameters.AddWithValue("@MarkDownWithoutTax18", GetStringProperty(payment, "MarkDownWithoutTax18"));

            string businessType = GetStringProperty(payment, "TypeOfVendor");
            if (string.IsNullOrWhiteSpace(businessType))
                businessType = GetStringProperty(payment, "BusinessType");

            command.Parameters.AddWithValue("@BusinessType", businessType);
            command.Parameters.AddWithValue("@AgencyEmail", GetStringProperty(payment, "AgencyEmail"));
            command.Parameters.AddWithValue("@AgencyName", GetStringProperty(payment, "AgencyName"));
            command.Parameters.AddWithValue("@PriceType", GetStringProperty(payment, "TypeOfMargin"));

            await command.ExecuteNonQueryAsync();
        }

        private async Task SubmitInsertBusinessDetails<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> locations)
        {
            if (locations == null)
                return;

            int lineId = 1;

            foreach (var location in locations)
            {
                if (location == null)
                    continue;

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED1""
                    (
                        ""Id"",
                        ""LineId"",
                        ""BusinessState"",
                        ""GSTNumber"",
                        ""AddressOfPlace"",
                        ""GSTVendorClassification""
                    )
                    VALUES (?, ?, ?, ?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@BusinessState", GetStringProperty(location, "State"));
                command.Parameters.AddWithValue("@GSTNumber", GetStringProperty(location, "GstNumber"));
                command.Parameters.AddWithValue("@AddressOfPlace", GetStringProperty(location, "Address"));
                command.Parameters.AddWithValue("@GSTVendorClassification", GetStringProperty(location, "GstClassification"));

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertPartnerDetails<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> partners)
        {
            if (partners == null)
                return;

            int lineId = 1;

            foreach (var partner in partners)
            {
                if (partner == null)
                    continue;

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED2""
                    (
                        ""Id"",
                        ""LineId"",
                        ""Name"",
                        ""Designation"",
                        ""Contact_No"",
                        ""Email_ID""
                    )
                    VALUES (?, ?, ?, ?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@Name", GetStringProperty(partner, "Name"));
                command.Parameters.AddWithValue("@Designation", GetStringProperty(partner, "Designation"));
                command.Parameters.AddWithValue("@ContactNo", GetStringProperty(partner, "ContactNo"));
                command.Parameters.AddWithValue("@Email", GetStringProperty(partner, "Email"));

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertOperationalContacts<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> contacts)
        {
            if (contacts == null)
                return;

            int lineId = 1;

            foreach (var contact in contacts)
            {
                if (contact == null)
                    continue;

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED3""
                    (
                        ""ID"",
                        ""LineId"",
                        ""Department"",
                        ""Name"",
                        ""Designation"",
                        ""ContactNo"",
                        ""Email""
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@Department", GetStringProperty(contact, "Department"));
                command.Parameters.AddWithValue("@Name", GetStringProperty(contact, "Name"));
                command.Parameters.AddWithValue("@Designation", GetStringProperty(contact, "Designation"));
                command.Parameters.AddWithValue("@ContactNo", GetStringProperty(contact, "ContactNo"));
                command.Parameters.AddWithValue("@Email", GetStringProperty(contact, "Email"));

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertMajorGoodsServices<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> goodsServices)
        {
            if (goodsServices == null)
                return;

            int lineId = 1;

            foreach (var item in goodsServices)
            {
                if (item == null)
                    continue;

                string product = GetStringProperty(item, "Product");
                string imageUpload = GetStringProperty(item, "ImageUpload");

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED4""
                    (
                        ""Id"",
                        ""LineId"",
                        ""MaterialDescription"",
                        ""HSNCode"",
                        ""Brand"",
                        ""Size"",
                        ""Product"",
                        ""TaxPercentage""
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@MaterialDescription", GetStringProperty(item, "MaterialDescription"));
                command.Parameters.AddWithValue("@HSNCode", GetStringProperty(item, "HsnCode"));
                command.Parameters.AddWithValue("@Brand", GetStringProperty(item, "Brand"));
                command.Parameters.AddWithValue("@Size", GetStringProperty(item, "Size"));
                command.Parameters.AddWithValue("@Product", product);
                command.Parameters.AddWithValue("@TaxPercentage", GetStringProperty(item, "TaxPercentage"));

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertMajorCustomers<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> customers)
        {
            if (customers == null)
                return;

            int lineId = 1;

            foreach (var customer in customers)
            {
                if (customer == null)
                    continue;

                string customerName = GetStringProperty(customer, "CustomerName");

                if (string.IsNullOrWhiteSpace(customerName))
                    continue;

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED5""
                    (
                        ""ID"",
                        ""LineId"",
                        ""CustomerName""
                    )
                    VALUES (?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@CustomerName", customerName);

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertOtherInformation<T>(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            IEnumerable<T> information)
        {
            if (information == null)
                return;

            int lineId = 1;

            foreach (var item in information)
            {
                if (item == null)
                    continue;

                string description = GetStringProperty(item, "Description");
                string textMode = GetStringProperty(item, "TextMode");

                if (string.IsNullOrWhiteSpace(textMode))
                    continue;

                string query = $@"
                    INSERT INTO ""{sDBName}"".""TEC_LED6""
                    (
                        ""ID"",
                        ""LineId"",
                        ""Description"",
                        ""TextMode""
                    )
                    VALUES (?, ?, ?, ?)";

                using var command = new OdbcCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@LineId", lineId);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@TextMode", textMode);

                await command.ExecuteNonQueryAsync();
                lineId++;
            }
        }

        private async Task SubmitInsertDocuments(
        OdbcConnection connection,
        OdbcTransaction transaction,
        int id,
        FormDataModel model,
        UploadedFilesModel uploadedFiles)
        {
            if (uploadedFiles == null)
                return;

            string tradeName = model.TradeName?.Trim() ?? "";

            // -------------------------------------------------
            // SINGLE DOCUMENTS
            // -------------------------------------------------

            var documents = new List<(int LineId, string DocumentType, string FileName)>
    {
        (1, "PAN Card", uploadedFiles.PanCard ?? ""),
        (2, "GST Certificate", uploadedFiles.GstCertificate ?? ""),
        (3, "Bank Account", uploadedFiles.BankProof ?? ""),
        (4, "MSME Certificate", uploadedFiles.MsmeCertificate ?? "")
    };

            // -------------------------------------------------
            // INSERT SINGLE DOCUMENTS
            // -------------------------------------------------

            foreach (var document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.FileName))
                    continue;

                string fullFilePath =
                    GetUploadedFilePath(document.FileName);

                string query = $@"
            INSERT INTO ""{sDBName}"".""TEC_LED7""
            (
                ""Id"",
                ""LineId"",
                ""TradeName"",
                ""DocumentName"",
                ""DocumentType"",
                ""FileData""
            )
            VALUES (?, ?, ?, ?, ?, ?)";

                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );

                command.Parameters.AddWithValue(
                    "@Id",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    document.LineId
                );

                command.Parameters.AddWithValue(
                    "@TradeName",
                    tradeName
                );

                command.Parameters.AddWithValue(
                    "@DocumentName",
                    document.FileName
                );

                command.Parameters.AddWithValue(
                    "@DocumentType",
                    document.DocumentType
                );

                command.Parameters.AddWithValue(
                    "@FileData",
                    fullFilePath
                );

                await command.ExecuteNonQueryAsync();
            }

            // -------------------------------------------------
            // MULTIPLE PROFORMA INVOICES
            // -------------------------------------------------

            if (uploadedFiles.PerformaInvoice != null &&
                uploadedFiles.PerformaInvoice.Count > 0)
            {
                foreach (string fileName in uploadedFiles.PerformaInvoice)
                {
                    if (string.IsNullOrWhiteSpace(fileName))
                        continue;

                    string fullFilePath =
                        GetUploadedFilePath(fileName);

                    string query = $@"
                INSERT INTO ""{sDBName}"".""TEC_LED7""
                (
                    ""Id"",
                    ""LineId"",
                    ""TradeName"",
                    ""DocumentName"",
                    ""DocumentType"",
                    ""FileData""
                )
                VALUES (?, ?, ?, ?, ?, ?)";

                    using var command =
                        new OdbcCommand(
                            query,
                            connection,
                            transaction
                        );

                    command.Parameters.AddWithValue(
                        "@Id",
                        id
                    );

                    // 5 = Proforma Invoice
                    command.Parameters.AddWithValue(
                        "@LineId",
                        5
                    );

                    command.Parameters.AddWithValue(
                        "@TradeName",
                        tradeName
                    );

                    command.Parameters.AddWithValue(
                        "@DocumentName",
                        fileName
                    );

                    command.Parameters.AddWithValue(
                        "@DocumentType",
                        "Performa Invoice"
                    );

                    command.Parameters.AddWithValue(
                        "@FileData",
                        fullFilePath
                    );

                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        // =====================================================
        // INSERT PAYMENT DETAILS
        // =====================================================

        private async Task InsertPaymentDetails(
     OdbcConnection connection,
     OdbcTransaction transaction,
     int id,
     PaymentDetailsModel payment)
        {
            log.WriteToLogFile_Debug(
                $"[VendorCreation] [InsertPaymentDetails] [START] - Id: {id}",
                "InsertPaymentDetails");

            string query = @$"
        INSERT INTO ""{sDBName}"".""PaymentDetails""
        (
            ""Id"",
            ""CreditDays"",
            ""DisCount"",
            ""MarkDownTax0"",
            ""MarkDownWithoutTax0"",
            ""MarkDownTax3"",
            ""MarkDownWithoutTax3"",
            ""MarkDownTax5"",
            ""MarkDownWithoutTax5"",
            ""MarkDownTax18"",
            ""MarkDownWithoutTax18"",
            ""BusinessType"",
            ""AgencyEmail"",
            ""AgencyName"",
            ""PriceType""
        )
        VALUES
        (
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?,
            ?
        )";

            using var command = new OdbcCommand(
                query,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "@Id",
                id);

            command.Parameters.AddWithValue(
                "@CreditDays",
                payment?.CreditDays ?? "");

            command.Parameters.AddWithValue(
                "@DisCount",
                payment?.BillLevelDiscount ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownTax0",
                payment?.MarkDownWithTax0 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownWithoutTax0",
                payment?.MarkDownWithoutTax0 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownTax3",
                payment?.MarkDownWithTax3 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownWithoutTax3",
                payment?.MarkDownWithoutTax3 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownTax5",
                payment?.MarkDownWithTax5 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownWithoutTax5",
                payment?.MarkDownWithoutTax5 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownTax18",
                payment?.MarkDownWithTax18 ?? "");

            command.Parameters.AddWithValue(
                "@MarkDownWithoutTax18",
                payment?.MarkDownWithoutTax18 ?? "");

            command.Parameters.AddWithValue(
                "@BusinessType",
                payment?.TypeOfVendor ?? "");

            command.Parameters.AddWithValue(
                "@AgencyEmail",
                payment?.AgencyEmail ?? "");

            command.Parameters.AddWithValue(
                "@AgencyName",
                payment?.AgencyName ?? "");

            command.Parameters.AddWithValue(
                "@PriceType",
                payment?.TypeOfMargin ?? "");

            await command.ExecuteNonQueryAsync();

            log.WriteToLogFile_Debug(
                $"[VendorCreation] [InsertPaymentDetails] [END] - Id: {id}",
                "InsertPaymentDetails");
        }

        // =====================================================
        // INSERT OTHER BUSINESS LOCATIONS
        // =====================================================

        private async Task InsertBusinessDetails(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            List<BusinessLocationModel> locations)
        {
            if (locations == null ||
                locations.Count == 0)
            {
                return;
            }


            int lineId = 1;


            foreach (var location in locations)
            {
                string query = @$"
                    INSERT INTO ""{sDBName}"".""TEC_LED1""
                    (
                        ""Id"",
                        ""LineId"",
                        ""BusinessState"",
                        ""GSTNumber"",
                        ""AddressOfPlace"",
                        ""GSTVendorClassification""
                    )
                    VALUES
                    (
                        ?,
                        ?,
                        ?,
                        ?,
                        ?,
                        ?
                    )";


                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );


                command.Parameters.AddWithValue(
                    "@Id",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    lineId
                );

                command.Parameters.AddWithValue(
                    "@BusinessState",
                    location.State ?? ""
                );

                command.Parameters.AddWithValue(
                    "@GSTNumber",
                    location.GstNumber ?? ""
                );

                command.Parameters.AddWithValue(
                    "@AddressOfPlace",
                    location.Address ?? ""
                );

                command.Parameters.AddWithValue(
                    "@GSTVendorClassification",
                    location.GstClassification ?? ""
                );


                await command.ExecuteNonQueryAsync();


                lineId++;
            }
        }


        // =====================================================
        // INSERT BUSINESS PARTNERS
        // =====================================================

        private async Task InsertPartnerDetails(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            List<BusinessPartnerModel> partners)
        {
            if (partners == null ||
                partners.Count == 0)
            {
                return;
            }


            int lineId = 1;


            foreach (var partner in partners)
            {
                string query = @$"
                    INSERT INTO ""{sDBName}"".""TEC_LED2""
                    (
                        ""Id"",
                        ""LineId"",
                        ""Name"",
                        ""Designation"",
                        ""Contact_No"",
                        ""Email_ID""
                    )
                    VALUES
                    (
                        ?,
                        ?,
                        ?,
                        ?,
                        ?,
                        ?
                    )";


                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );


                command.Parameters.AddWithValue(
                    "@Id",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    lineId
                );

                command.Parameters.AddWithValue(
                    "@Name",
                    partner.Name ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Designation",
                    partner.Designation ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Contact_No",
                    partner.ContactNo ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Email_ID",
                    partner.Email ?? ""
                );


                await command.ExecuteNonQueryAsync();


                lineId++;
            }
        }


        // =====================================================
        // INSERT OPERATIONAL CONTACTS
        // =====================================================

        private async Task InsertOperationalContacts(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            List<OperationalContactModel> contacts)
        {
            if (contacts == null ||
                contacts.Count == 0)
            {
                return;
            }


            int lineId = 1;


            foreach (var contact in contacts)
            {
                string query = @$"
                    INSERT INTO ""{sDBName}"".""TEC_LED3""
                    (
                        ""ID"",
                        ""LineId"",
                        ""Name"",
                        ""ContactNo"",
                        ""Email""
                    )
                    VALUES
                    (
                        ?,
                        ?,
                        ?,
                        ?,
                        ?
                    )";


                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );


                command.Parameters.AddWithValue(
                    "@ID",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    lineId
                );

                command.Parameters.AddWithValue(
                    "@Name",
                    contact.Name ?? ""
                );

                command.Parameters.AddWithValue(
                    "@ContactNo",
                    contact.ContactNo ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Email",
                    contact.Email ?? ""
                );


                await command.ExecuteNonQueryAsync();


                lineId++;
            }
        }


        // =====================================================
        // INSERT MAJOR GOODS / SERVICES
        // =====================================================

        private async Task InsertMajorGoodsServices(
            OdbcConnection connection,
            OdbcTransaction transaction,
            int id,
            List<MajorGoodsServiceModel> goodsServices)
        {
            if (goodsServices == null ||
                goodsServices.Count == 0)
            {
                return;
            }


            int lineId = 1;


            foreach (var item in goodsServices)
            {
                string imagePath = null;


                // -------------------------------------------------
                // imageUpload already contains uploaded filename/path
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                    item.ImageUpload))
                {
                    imagePath =
                        GetUploadedFilePath(
                            item.ImageUpload
                        );
                }


                string query = @$"
                    INSERT INTO ""{sDBName}"".""TEC_LED4""
                    (
                        ""Id"",
                        ""LineId"",
                        ""MaterialDescription"",
                        ""HSNCode"",
                        ""Brand"",
                        ""Size"",
                        ""Product""
                    )
                    VALUES
                    (
                        ?,
                        ?,
                        ?,
                        ?,
                        ?,
                        ?,
                        ?
                    )";


                using var command =
                    new OdbcCommand(
                        query,
                        connection,
                        transaction
                    );


                command.Parameters.AddWithValue(
                    "@Id",
                    id
                );

                command.Parameters.AddWithValue(
                    "@LineId",
                    lineId
                );

                command.Parameters.AddWithValue(
                    "@MaterialDescription",
                    item.MaterialDescription ?? ""
                );

                command.Parameters.AddWithValue(
                    "@HSNCode",
                    item.HsnCode ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Brand",
                    item.Brand ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Size",
                    item.Size ?? ""
                );

                command.Parameters.AddWithValue(
                    "@Product",
                    imagePath ?? ""
                );


                await command.ExecuteNonQueryAsync();


                lineId++;
            }
        }


        // =====================================================
        // GET FILE PATH
        //
        // appsettings:
        //
        // "FileUpload": {
        //     "TempFolder": "D:\\NHFS\\VendorDocuments"
        // }
        // =====================================================

        private string GetUploadedFilePath(
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }


            string uploadFolder =
                _configuration[
                    "Folder:ImagePath"
                ];


            if (string.IsNullOrWhiteSpace(
                uploadFolder))
            {
                throw new Exception(
                    "FileUpload:TempFolder is missing in appsettings.json"
                );
            }


            // If only filename is coming from React,
            // combine folder + filename.
            //
            // If the API happens to return a full path,
            // don't combine it again.

            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }


            return Path.Combine(
                uploadFolder,
                fileName
            );
        }



        private async Task InsertHeaderDetails(
    OdbcConnection connection,
    OdbcTransaction transaction,
    int id,
    FormDataModel model)
        {
            string query = @$"
        INSERT INTO ""{sDBName}"".""TEC_OLED""
        (
            ""Id"",
            ""TName"",
            ""Raddress1"",
            ""Raddress2"",
            ""Raddress3"",
            ""Rcountry"",
            ""Rstate"",
            ""registeredOfficeCity"",
            ""Rzipcode"",

            ""Baddress1"",
            ""Baddress2"",
            ""Baddress3"",
            ""Bcountry"",
            ""Bstate"",
            ""businessBillingCity"",
            ""Bzipcode"",

            ""NatureOfBusinessActivity"",
            ""DateOfEstablishment"",
            ""ContactPersonName"",
            ""Designation"",
            ""EmailId"",
            ""MobileNo"",
            ""OfficeTelephoneNo"",
            ""TANNo"",

            ""MsmeRegistrationStatus"",
            ""BankName"",
            ""AccountName"",
            ""AccountNumber"",
            ""IfscCode"",
            ""BranchCode"",
            ""BankAddress"",

            ""GstNo"",
            ""DeclarationName"",
            ""DeclarationDesignation"",
            ""Draft"",
            ""AppliedDate"",
            ""PartnerType"",
            ""PanNo"",
            ""MSMENo"",
            ""EnterpriseType"",
            ""BusinessType"",
            ""AgencyEmail"",
            ""AgencyName"",
            ""VerificationNo"",

            ""Gaddress1"",
            ""Gaddress2"",
            ""Gaddress3"",
            ""Gcountry"",
            ""Gstate"",
            ""Gcity"",
            ""Gzipcode"",

            ""Saddress1"",
            ""Saddress2"",
            ""Saddress3"",
            ""Scountry"",
            ""Sstate"",
            ""Scity"",
            ""Szipcode"",
            ""ContactPerson""
        )
        VALUES
        (
            ?,
            ?, ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?,
            ?, ?, ?, ?, ?, ?, ?,
?
        )";

            using var command =
                new OdbcCommand(
                    query,
                    connection,
                    transaction
                );

            var registered =
                model.RegisteredOffice;

            var billing =
                model.BillingAddress;

            var goodsReturn =
                model.GoodsReturnAddress;

            var shipping =
                model.ShippingAddress;

            var bank =
                model.BankDetails;

            var msme =
                model.MsmeDetails;

            var payment =
                model.PaymentDetails;

            command.Parameters.AddWithValue(
                "@Id",
                id
            );

            command.Parameters.AddWithValue(
                "@TName",
                model.TradeName ?? ""
            );

            command.Parameters.AddWithValue(
                "@Raddress1",
                registered?.Address1 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Raddress2",
                registered?.Address2 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Raddress3",
                registered?.Address3 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Rcountry",
                registered?.Country ?? ""
            );

            command.Parameters.AddWithValue(
                "@Rstate",
                registered?.State ?? ""
            );

            command.Parameters.AddWithValue(
                "@registeredOfficeCity",
                registered?.City ?? ""
            );

            command.Parameters.AddWithValue(
                "@Rzipcode",
                registered?.Pincode ?? ""
            );

            // =========================================================
            // BILLING
            // =========================================================

            command.Parameters.AddWithValue(
                "@Baddress1",
                billing?.Address1 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Baddress2",
                billing?.Address2 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Baddress3",
                billing?.Address3 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Bcountry",
                billing?.Country ?? ""
            );

            command.Parameters.AddWithValue(
                "@Bstate",
                billing?.State ?? ""
            );

            command.Parameters.AddWithValue(
                "@businessBillingCity",
                billing?.City ?? ""
            );

            command.Parameters.AddWithValue(
                "@Bzipcode",
                billing?.Pincode ?? ""
            );

            // =========================================================
            // COMMON
            // =========================================================

            command.Parameters.AddWithValue(
                "@NatureOfBusinessActivity",
                model.NatureOfBusiness ?? ""
            );

            command.Parameters.AddWithValue(
                "@DateOfEstablishment",
                model.DateOfEstablishment ?? ""
            );

            command.Parameters.AddWithValue(
                "@ContactPersonName",
                model.ContactPersonName ?? ""
            );

            command.Parameters.AddWithValue(
                "@Designation",
                model.Designation ?? ""
            );

            command.Parameters.AddWithValue(
                "@EmailId",
                model.Email ?? ""
            );

            command.Parameters.AddWithValue(
                "@MobileNo",
                model.MobileNumber ?? ""
            );

            command.Parameters.AddWithValue(
                "@OfficeTelephoneNo",
                model.OfficeTelephoneNo ?? ""
            );

            command.Parameters.AddWithValue(
                "@TANNo",
                model.TanNumber ?? ""
            );

            // =========================================================
            // MSME
            // =========================================================

            command.Parameters.AddWithValue(
                "@MsmeRegistrationStatus",
                msme?.MsmeRegistrationStatus ?? ""
            );

            // =========================================================
            // BANK
            // =========================================================

            command.Parameters.AddWithValue(
                "@BankName",
                bank?.BankName ?? ""
            );

            command.Parameters.AddWithValue(
                "@AccountName",
                bank?.AccountNameHolder ?? ""
            );

            command.Parameters.AddWithValue(
                "@AccountNumber",
                bank?.AccountNumber ?? ""
            );

            command.Parameters.AddWithValue(
                "@IfscCode",
                bank?.IfscCode ?? ""
            );

            command.Parameters.AddWithValue(
                "@BranchCode",
                bank?.BranchCode ?? ""
            );

            command.Parameters.AddWithValue(
                "@BankAddress",
                bank?.BankAddress ?? ""
            );

            // =========================================================
            // GST / DECLARATION
            // =========================================================

            command.Parameters.AddWithValue(
                "@GstNo",
                model.GstNumber ?? ""
            );

            command.Parameters.AddWithValue(
                "@DeclarationName",
                 ""
            );

            command.Parameters.AddWithValue(
                "@DeclarationDesignation",
                 ""
            );

            command.Parameters.AddWithValue(
                "@Draft",
                "Y"
            );

            command.Parameters.AddWithValue(
                "@AppliedDate",
                DateTime.Now.ToString("yyyy-MM-dd")
            );

            command.Parameters.AddWithValue(
                "@PartnerType",
                model.PartnerType ?? ""
            );

            command.Parameters.AddWithValue(
                "@PanNo",
                model.PanNumber ?? ""
            );

            command.Parameters.AddWithValue(
                "@MSMENo",
                msme?.MsmeNo ?? ""
            );

            command.Parameters.AddWithValue(
                "@EnterpriseType",
                msme?.EnterpriseType ?? ""
            );

            // =========================================================
            // PAYMENT
            // =========================================================

            command.Parameters.AddWithValue(
                "@BusinessType",
                payment?.TypeOfVendor ?? ""
            );

            command.Parameters.AddWithValue(
                "@AgencyEmail",
                payment?.AgencyEmail ?? ""
            );

            command.Parameters.AddWithValue(
                "@AgencyName",
                payment?.AgencyName ?? ""
            );

            command.Parameters.AddWithValue(
                "@VerificationNo",
                model.MobileNumber ?? ""
            );

            // =========================================================
            // GOODS RETURN
            // =========================================================

            command.Parameters.AddWithValue(
                "@Gaddress1",
                goodsReturn?.Address1 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gaddress2",
                goodsReturn?.Address2 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gaddress3",
                goodsReturn?.Address3 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gcountry",
                goodsReturn?.Country ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gstate",
                goodsReturn?.State ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gcity",
                goodsReturn?.City ?? ""
            );

            command.Parameters.AddWithValue(
                "@Gzipcode",
                goodsReturn?.Pincode ?? ""
            );

            // =========================================================
            // SHIPPING
            // =========================================================

            command.Parameters.AddWithValue(
                "@Saddress1",
                shipping?.Address1 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Saddress2",
                shipping?.Address2 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Saddress3",
                shipping?.Address3 ?? ""
            );

            command.Parameters.AddWithValue(
                "@Scountry",
                shipping?.Country ?? ""
            );

            command.Parameters.AddWithValue(
                "@Sstate",
                shipping?.State ?? ""
            );

            command.Parameters.AddWithValue(
                "@Scity",
                shipping?.City ?? ""
            );

            command.Parameters.AddWithValue(
                "@Szipcode",
                shipping?.Pincode ?? ""
            );

            // =========================================================
            // CONTACT PERSON
            // =========================================================

            command.Parameters.AddWithValue(
                "@ContactPerson",
                model.ContactPerson ?? ""
            );

            await command.ExecuteNonQueryAsync();
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
                new MajorGoodsService { ImageFile =  "",Brand = "",Size = "" }
            };
            var MajorCustomers = new List<MajorCustomers>{
                new MajorCustomers {  CustomerName= "" }
            };
            _sessionManager.Set("BusinessDetails", JsonConvert.SerializeObject(businessDetails));
            _sessionManager.Set("PartnerDetails", JsonConvert.SerializeObject(partnerDetails));
            _sessionManager.Set("MajorGoodsService", JsonConvert.SerializeObject(majorGoodsService));
            _sessionManager.Set("MajorCustomer", JsonConvert.SerializeObject(MajorCustomers));
            RefreshKYC();
            RefreshKYC1();
            //if (GSTNumber.Text != "") GSTNumber_TextChanged(sender, e);
            if (_sessionManager.Get("DocumentDetails") == "")
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
        public async Task<ApiResponse> UploadKycFile(IFormFile file, string documentType, int rowIndex)
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

                string folderPath = _configuration["Folder:Path"]; if (string.IsNullOrWhiteSpace(folderPath)) { throw new Exception("Folder path is not configured in appsettings.json."); }
                if (!Directory.Exists(folderPath)) { Directory.CreateDirectory(folderPath); }

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

                //string rowKeySuffix = "";

                //if (documentType.Equals(
                //        "Performa Invoice",
                //        StringComparison.OrdinalIgnoreCase))
                //{
                //    rowKeySuffix = "_" + rowIndex;
                //}

                string sessionPathKey =
                    $"Path_{documentType}";

                string sessionFileNameKey =
                    $"FileName_{documentType}";

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
        public async Task<ApiResponse> DownloadKYCFile(
    string file,
    string gstNo,
    string documentType)
        {
            string functionName = "Download_KYC_File";

            log.WriteToLogFile_Debug(
                $"Starting the function - {functionName}",
                functionName);

            log.WriteToLogFile_Debug(
                $"Request - Gst NO:{gstNo}, File:{file}, DocumentType:{documentType}",
                functionName);

            var fileResultModel = new FileResultModel();

            try
            {
                // =========================================================
                // PERFORMA INVOICE
                // =========================================================
                // For Performa Invoice:
                // Folder:Path + file name
                // No DB FileData
                // No Base64 conversion
                // =========================================================

                if (documentType?.Trim().Equals(
                        "Performa Invoice",
                        StringComparison.OrdinalIgnoreCase) == true
                    ||
                    documentType?.Trim().Equals(
                        "Proforma Invoice",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    string folderPath = _configuration["Folder:Path"];

                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Failure,
                            "KYC document folder path is not configured.",
                            null);
                    }

                    string fileName = Path.GetFileName(file?.Trim() ?? "");

                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Failure,
                            "File name is required.",
                            null);
                    }

                    string filePath = Path.Combine(
                        folderPath,
                        fileName);

                    log.WriteToLogFile_Debug(
                        $"Performa Invoice file path: {filePath}",
                        functionName);

                    if (!File.Exists(filePath))
                    {
                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Failure,
                            "Performa Invoice file not found.",
                            null);
                    }

                    byte[] fileBytes =
                        await File.ReadAllBytesAsync(filePath);

                    fileResultModel.FileBytes = fileBytes;
                    fileResultModel.FileName = fileName;
                    fileResultModel.ContentType =
                        GetContentType(fileName);

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "Successfully retrieved the file",
                        fileResultModel);
                }

                // =========================================================
                // OTHER DOCUMENT TYPES
                // =========================================================

                string sessionTempPathKey =
                    "TempFilePath_" + documentType;

                string sessionViewPathKey =
                    "Path_" + documentType;

                string sessionFileNameKey =
                    "FileName_" + documentType;

                var sessionViewPath =
                    _sessionManager.Get(sessionViewPathKey);

                if (sessionViewPath != null &&
                    !string.IsNullOrWhiteSpace(sessionViewPath.ToString()) &&
                    File.Exists(sessionViewPath.ToString()))
                {
                    string viewPath =
                        sessionViewPath.ToString();

                    byte[] fileBytes =
                        await File.ReadAllBytesAsync(viewPath);

                    string fileName =
                        _sessionManager.Get(sessionFileNameKey) != null
                            ? _sessionManager
                                .Get(sessionFileNameKey)
                                .ToString()
                            : Path.GetFileName(viewPath);

                    fileResultModel.FileBytes = fileBytes;
                    fileResultModel.FileName = fileName;
                    fileResultModel.ContentType =
                        GetContentType(fileName);

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "Successfully retrieved the file",
                        fileResultModel);
                }

                var sessionTempPath =
                    _sessionManager.Get(sessionTempPathKey);

                if (sessionTempPath != null &&
                    !string.IsNullOrWhiteSpace(sessionTempPath.ToString()) &&
                    File.Exists(sessionTempPath.ToString()))
                {
                    string tempPath =
                        sessionTempPath.ToString();

                    byte[] fileBytes =
                        await File.ReadAllBytesAsync(tempPath);

                    string fileName =
                        _sessionManager.Get(sessionFileNameKey) != null
                            ? _sessionManager
                                .Get(sessionFileNameKey)
                                .ToString()
                            : Path.GetFileName(tempPath);

                    fileResultModel.FileBytes = fileBytes;
                    fileResultModel.FileName = fileName;
                    fileResultModel.ContentType =
                        GetContentType(fileName);

                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Success,
                        "Successfully retrieved the file",
                        fileResultModel);
                }

                // =========================================================
                // GET VENDOR ID
                // =========================================================

                string getIdQuery = $@"
            select ifnull(""Id"",0)
            from ""{sDBName}"".""TEC_OLED""
            where ""GstNo""='{gstNo?.Trim()}'";

                string getId =
                    db.GetSingleValue(getIdQuery);

                if (!string.IsNullOrEmpty(getId) &&
                    getId != "0")
                {
                    // =====================================================
                    // GET FILE DATA FROM DATABASE
                    // =====================================================

                    string fileDataQuery = $@"
                select ""FileData""
                from ""{sDBName}"".""TEC_LED7""
                where ""Id""='{getId}'
                and ""DocumentType""='{documentType}'";

                    string fileDataBase64 =
                        db.GetSingleValue(fileDataQuery);

                    if (!string.IsNullOrEmpty(fileDataBase64))
                    {
                        byte[] fileBytes;

                        try
                        {
                            fileBytes =
                                Convert.FromBase64String(fileDataBase64);
                        }
                        catch
                        {
                            return ApiResponseUtility.GenerateApiResponse(
                                ApiStatusEnum.Failure,
                                "Invalid file data.",
                                null);
                        }

                        string fileType =
                            GetFileType(fileDataBase64);

                        string fileName;

                        if (fileType == "pdf")
                        {
                            fileName = documentType + ".pdf";
                        }
                        else if (fileType == "jpg" ||
                                 fileType == "jpeg")
                        {
                            fileName = documentType + ".jpg";
                        }
                        else if (fileType == "png")
                        {
                            fileName = documentType + ".png";
                        }
                        else
                        {
                            fileName = documentType;
                        }

                        string folderPath =
                            _configuration["Folder:Path"];

                        if (string.IsNullOrWhiteSpace(folderPath))
                        {
                            return ApiResponseUtility.GenerateApiResponse(
                                ApiStatusEnum.Failure,
                                "KYC document folder path is not configured.",
                                null);
                        }

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        await File.WriteAllBytesAsync(
                            filePath,
                            fileBytes);

                        _sessionManager.Set(
                            sessionViewPathKey,
                            filePath);

                        _sessionManager.Set(
                            sessionTempPathKey,
                            filePath);

                        _sessionManager.Set(
                            sessionFileNameKey,
                            fileName);

                        fileResultModel.FileBytes =
                            fileBytes;

                        fileResultModel.FileName =
                            fileName;

                        fileResultModel.ContentType =
                            GetContentType(fileName);

                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Success,
                            "Successfully retrieved the file",
                            fileResultModel);
                    }
                }

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "No file available to download.",
                    null);
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"Error - {ex.Message}",
                    functionName);

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "Unable to download the document.",
                    null);
            }
        }
        public async Task<ApiResponse> NextPageCheck(string gstNumber, int page)
        {
            string functionName = "Next_Page_Check";

            if (page == 2)
            {
                string GstValid = db.GetSingleValue(
                    $@"Select ""GstNo"" 
               from ""{sDBName}"".""TEC_OLED"" 
               where ""GstNo""='" + gstNumber + "'"
                );

                if (!string.IsNullOrEmpty(GstValid))
                {
                    string draftCheck = db.GetSingleValue(
                        $@"Select ifnull(""Draft"",'N') 
                   from ""{sDBName}"".""TEC_OLED"" 
                   where ""GstNo""='" + gstNumber + "'"
                    );

                    string IsGstSaved = db.GetSingleValue(
                        $@"Select ""GstNo"" 
                   from ""{sDBName}"".""TEC_OLED"" 
                   where ""GstNo""='" + gstNumber + "'"
                    );

                    if (!string.IsNullOrEmpty(draftCheck))
                    {
                        string ISFromDraft = string.Empty;

                        var isDraftValue = _sessionManager.Get("IsDraft");

                        if (isDraftValue != null &&
                            !string.IsNullOrEmpty(isDraftValue.ToString().Trim()))
                        {
                            ISFromDraft = isDraftValue.ToString().Trim();
                        }

                        if (draftCheck == "Y" && ISFromDraft != "Y")
                        {
                            log.WriteToLogFile_Debug(
                                "[VendorCreation] [btnNext_Click] [VALIDATION_FAILED] - Page 2: GST: "
                                + gstNumber + " is already in draft",
                                "btnNext_Click"
                            );

                            return ApiResponseUtility.GenerateApiResponse(
                                ApiStatusEnum.Failure,
                                "Your Gst Number is Already in draft",
                                null
                            );
                        }
                        else if (draftCheck == "N" &&
                                 !string.IsNullOrEmpty(IsGstSaved))
                        {
                            return ApiResponseUtility.GenerateApiResponse(
                                ApiStatusEnum.Failure,
                                "Your GST Number is Already Submitted",
                                null
                            );
                        }
                    }
                }
            }

            // No validation issue, allow the user to continue
            return ApiResponseUtility.GenerateApiResponse(
                ApiStatusEnum.Success,
                "Validation successful",
                null
            );
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

                string sDocUrl = _configuration["Posting:DocUrl"] ?? "";
                string sDocKey = _configuration["Posting:DocKey"] ?? "";
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
        public async Task<ApiResponse> ViewKYCFile(
      string file,
      string gstNo,
      string documentType)
        {
            string functionName = "View_KYC_File";

            log.WriteToLogFile_Debug(
                $"Starting the function - {functionName}",
                functionName);

            log.WriteToLogFile_Debug(
                $"Request - Gst NO:{gstNo}, File - {file}, DocumentType - {documentType}",
                functionName);

            var fileResultModel = new FileResultModel();

            try
            {
                // ============================================================
                // VALIDATION
                // ============================================================

                if (string.IsNullOrWhiteSpace(documentType))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "Document type is required.",
                        null);
                }

                //if (string.IsNullOrWhiteSpace(gstNo))
                //{
                //    return ApiResponseUtility.GenerateApiResponse(
                //        ApiStatusEnum.Failure,
                //        "GST number is required.",
                //        null);
                //}

                // ============================================================
                // SESSION KEYS
                // ============================================================

                string sessionPathKey =
                    "Path_" + documentType;

                string sessionBase64Key =
                    "base64_" + documentType;

                string sessionFileNameKey =
                    "FileName_" + documentType;


                // ============================================================
                // 1. CHECK DATABASE DRAFT FILE
                // ============================================================

                string getIdQuery = $@"
            SELECT IFNULL(""Id"", 0)
            FROM ""{sDBName}"".""TEC_OLED""
            WHERE ""GstNo"" = '{gstNo.Trim()}'";

                string getId = db.GetSingleValue(getIdQuery);

                if (!string.IsNullOrEmpty(getId) &&
                    getId != "0")
                {
                    string draftQuery = $@"
                SELECT ""Draft""
                FROM ""{sDBName}"".""TEC_OLED""
                WHERE ""Id"" = '{getId}'";

                    string draft = db.GetSingleValue(draftQuery);

                    // ========================================================
                    // DRAFT = Y
                    // ========================================================

                    if (draft == "Y")
                    {
                        string filePathQuery = $@"
                    SELECT ""FileData""
                    FROM ""{sDBName}"".""TEC_LED7""
                    WHERE ""Id"" = '{getId}'
                    AND ""DocumentType"" = '{documentType}'";

                        string filePathFromDb =
                            db.GetSingleValue(filePathQuery);

                        log.WriteToLogFile_Debug(
                            $"File path from DB: {filePathFromDb}",
                            functionName);

                        if (!string.IsNullOrWhiteSpace(filePathFromDb) &&
                            File.Exists(filePathFromDb))
                        {
                            byte[] fileBytes =
                                await File.ReadAllBytesAsync(filePathFromDb);

                            string fileName =
                                Path.GetFileName(filePathFromDb);

                            fileResultModel.FileBytes =
                                fileBytes;

                            fileResultModel.FileName =
                                fileName;

                            fileResultModel.ContentType =
                                GetContentType(fileName);

                            // Save into session
                            _sessionManager.Set(
                                sessionPathKey,
                                filePathFromDb);

                            _sessionManager.Set(
                                sessionBase64Key,
                                Convert.ToBase64String(fileBytes));

                            _sessionManager.Set(
                                sessionFileNameKey,
                                fileName);

                            return ApiResponseUtility.GenerateApiResponse(
                                ApiStatusEnum.Success,
                                "Successfully retrieved the details",
                                fileResultModel);
                        }
                    }
                }


                // ============================================================
                // 2. CHECK SESSION PATH
                // ============================================================

                var sessionPath =
                    _sessionManager.Get(sessionPathKey);

                log.WriteToLogFile_Debug(
                    $"Session Path Key: {sessionPathKey}",
                    functionName);

                log.WriteToLogFile_Debug(
                    $"Session Path Value: {sessionPath}",
                    functionName);

                if (sessionPath != null &&
                    !string.IsNullOrWhiteSpace(sessionPath.ToString()))
                {
                    string tempPath =
                        sessionPath.ToString();

                    log.WriteToLogFile_Debug(
                        $"Checking session file path: {tempPath}",
                        functionName);

                    if (File.Exists(tempPath))
                    {
                        byte[] fileBytes =
                            await File.ReadAllBytesAsync(tempPath);

                        string fileName = "";

                        var sessionFileName =
                            _sessionManager.Get(sessionFileNameKey);

                        if (sessionFileName != null &&
                            !string.IsNullOrWhiteSpace(
                                sessionFileName.ToString()))
                        {
                            fileName =
                                sessionFileName.ToString();
                        }
                        else
                        {
                            fileName =
                                Path.GetFileName(tempPath);
                        }

                        string base64File =
                            Convert.ToBase64String(fileBytes);

                        _sessionManager.Set(
                            sessionBase64Key,
                            base64File);

                        fileResultModel.FileBytes =
                            fileBytes;

                        fileResultModel.FileName =
                            fileName;

                        fileResultModel.ContentType =
                            GetContentType(fileName);

                        return ApiResponseUtility.GenerateApiResponse(
                            ApiStatusEnum.Success,
                            "Successfully retrieved the details",
                            fileResultModel);
                    }
                }


                // ============================================================
                // 3. FALLBACK:
                //    USE Folder:Path + FILE NAME
                //
                //    This is important for Performa Invoice.
                // ============================================================

                log.WriteToLogFile_Debug(
                    "Session path not found. Trying configured folder path.",
                    functionName);

                string folderPath =
                    _configuration["Folder:Path"];

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "KYC document folder path is not configured.",
                        null);
                }


                // ============================================================
                // GET FILE NAME
                // ============================================================

                string fileNameFromRequest = "";

                if (!string.IsNullOrWhiteSpace(file))
                {
                    fileNameFromRequest =
                        Path.GetFileName(file.Trim());
                }


                // If file parameter is empty, try session filename
                if (string.IsNullOrWhiteSpace(fileNameFromRequest))
                {
                    var sessionFileName =
                        _sessionManager.Get(sessionFileNameKey);

                    if (sessionFileName != null &&
                        !string.IsNullOrWhiteSpace(
                            sessionFileName.ToString()))
                    {
                        fileNameFromRequest =
                            Path.GetFileName(
                                sessionFileName.ToString().Trim());
                    }
                }


                if (string.IsNullOrWhiteSpace(fileNameFromRequest))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        "File name is required.",
                        null);
                }


                // ============================================================
                // BUILD PHYSICAL FILE PATH
                // ============================================================

                string folderFilePath =
                    Path.Combine(
                        folderPath,
                        fileNameFromRequest);

                log.WriteToLogFile_Debug(
                    $"Folder Path: {folderPath}",
                    functionName);

                log.WriteToLogFile_Debug(
                    $"File Name: {fileNameFromRequest}",
                    functionName);

                log.WriteToLogFile_Debug(
                    $"Final File Path: {folderFilePath}",
                    functionName);


                // ============================================================
                // CHECK FILE EXISTS
                // ============================================================

                if (!File.Exists(folderFilePath))
                {
                    return ApiResponseUtility.GenerateApiResponse(
                        ApiStatusEnum.Failure,
                        $"File not found: {folderFilePath}",
                        null);
                }


                // ============================================================
                // READ FILE
                // ============================================================

                byte[] finalFileBytes =
                    await File.ReadAllBytesAsync(folderFilePath);

                string finalFileName =
                    Path.GetFileName(folderFilePath);

                string finalBase64 =
                    Convert.ToBase64String(finalFileBytes);


                // ============================================================
                // SAVE INTO SESSION
                // ============================================================

                _sessionManager.Set(
                    sessionPathKey,
                    folderFilePath);

                _sessionManager.Set(
                    sessionBase64Key,
                    finalBase64);

                _sessionManager.Set(
                    sessionFileNameKey,
                    finalFileName);


                // ============================================================
                // PREPARE RESPONSE
                // ============================================================

                fileResultModel.FileBytes =
                    finalFileBytes;

                fileResultModel.FileName =
                    finalFileName;

                fileResultModel.ContentType =
                    GetContentType(finalFileName);


                // ============================================================
                // SUCCESS
                // ============================================================

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Success,
                    "Successfully retrieved the details",
                    fileResultModel);
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"Error - {ex.Message}",
                    functionName);

                log.WriteToLogFile_Debug(
                    $"Stack Trace - {ex.StackTrace}",
                    functionName);

                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "Unable to retrieve the document.",
                    null);
            }
        }
        public async Task<ApiResponse> GSTNumberCheck(string gstNumber)
        {
            string functionName = "GSTNumber";

            GstResponse response = new GstResponse();

            log.WriteToLogFile_Debug(
                $"Starting the function with GstNumber - {gstNumber}",
                functionName
            );

            if (string.IsNullOrWhiteSpace(gstNumber))
            {
                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "GST Number is required.",
                    null
                );
            }

            gstNumber = gstNumber.Trim().ToUpper();

            // ---------------------------------------------------------
            // PAN NUMBER FROM GST
            // ---------------------------------------------------------

            if (gstNumber.Length == 15)
            {
                response.PANNumber = gstNumber.Substring(2, 10);
            }

            // ---------------------------------------------------------
            // CHECK DRAFT GST
            // ---------------------------------------------------------

            string gst = db.GetSingleValue(
                $@"SELECT ""GstNo""
   FROM {sDBName}.""TEC_OLED""
   WHERE ""GstNo"" = '{gstNumber}'
   AND ""Draft"" = 'Y'"
            );

            // ---------------------------------------------------------
            // GET ID
            // ---------------------------------------------------------

            string getid1 = db.GetSingleValue(
                $@"SELECT ""Id""
   FROM {sDBName}.""TEC_OLED""
   WHERE ""GstNo"" = '{gstNumber}'"
            );

            // ---------------------------------------------------------
            // CHECK RE-APPLY
            // ---------------------------------------------------------

            string IsReApply = db.GetSingleValue(
                $@"SELECT T1.""GstNo""
   FROM {sDBName}.""TEC_OLED"" T1
   INNER JOIN {sDBName}.""ApprovalTrace"" T2
   ON T2.""GstNo"" = T1.""GstNo""
   WHERE T2.""ReApplySts"" = 'No'
   AND T2.""GstNo"" = '{gstNumber}'"
            );

            if (!string.IsNullOrEmpty(IsReApply))
            {
                return ApiResponseUtility.GenerateApiResponse(
                    ApiStatusEnum.Failure,
                    "",
                    null
                );
            }

            // ---------------------------------------------------------
            // IF GST EXISTS, GET ALL DETAILS
            // ---------------------------------------------------------

            if (gst != null && gst != "")
            {
                if (!string.IsNullOrEmpty(gstNumber))
                {
                    string getid = db.GetSingleValue($@"select ""Id"" from ""{sDBName}"".""TEC_OLED"" where ""GstNo""='" + gstNumber.Trim() + "' ");
                    if (!string.IsNullOrEmpty(getid))
                    {
                        int findId = Convert.ToInt32(getid);

                        using (OdbcConnection connection = new OdbcConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            // =====================================================
                            // 1. TEC_OLED
                            // =====================================================

                            string oledQuery = $@"SELECT
        ""TName"",
        ""PartnerType"",
        ""Raddress1"",
        ""Raddress2"",
        ""Raddress3"",
        ""Rcountry"",
        ""Rstate"",
        ""businessBillingCity"",
        ""registeredOfficeCity"",
        ""Gcity"",
        ""Scity"",
        ""Rzipcode"",
        ""Gaddress1"",
        ""Gaddress2"",
        ""Gaddress3"",
        ""Gcountry"",
        ""Gstate"",
        ""Gzipcode"",
        ""Saddress1"",
        ""Saddress2"",
        ""Saddress3"",
        ""Scountry"",
        ""Sstate"",
        ""Szipcode"",
        ""Baddress1"",
        ""Baddress2"",
        ""Baddress3"",
        ""Bcountry"",
        ""Bstate"",
        ""Bzipcode"",
        ""NatureOfBusinessActivity"",
        ""DateOfEstablishment"",
        ""ContactPersonName"",
        ""Designation"",
        ""EmailId"",
        ""MobileNo"",
        ""OfficeTelephoneNo"",
        ""TANNo"",
        ""MsmeRegistrationStatus"",
        ""MSMENo"",
        ""BankName"",
        ""AccountName"",
        ""AccountNumber"",
        ""IfscCode"",
        ""BranchCode"",
        ""BankAddress"",
        ""DeclarationName"",
        ""DeclarationDesignation"",
        ""EnterpriseType"",
        ""BusinessType"",
        ""AgencyEmail"",
        ""AgencyName"",
        ""VerificationNo"",
        ""ContactPerson""
    FROM {sDBName}.""TEC_OLED""
    WHERE ""Id"" = ?";

                            using (OdbcCommand command = new OdbcCommand(oledQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        response.VendorDetails = new OLEDDetailsDto
                                        {
                                            TName = reader["TName"]?.ToString(),
                                            PartnerType = reader["PartnerType"]?.ToString(),

                                            Raddress1 = reader["Raddress1"]?.ToString(),
                                            Raddress2 = reader["Raddress2"]?.ToString(),
                                            Raddress3 = reader["Raddress3"]?.ToString(),
                                            Rcountry = reader["Rcountry"]?.ToString(),
                                            Rstate = reader["Rstate"]?.ToString(),
                                            Rzipcode = reader["Rzipcode"]?.ToString(),
                                            RegisteredOfficeCity = reader["registeredOfficeCity"]?.ToString(),

                                            Gaddress1 = reader["Gaddress1"]?.ToString(),
                                            Gaddress2 = reader["Gaddress2"]?.ToString(),
                                            Gaddress3 = reader["Gaddress3"]?.ToString(),
                                            Gcountry = reader["Gcountry"]?.ToString(),
                                            Gstate = reader["Gstate"]?.ToString(),
                                            Gzipcode = reader["Gzipcode"]?.ToString(),
                                            Gcity = reader["Gcity"]?.ToString(),

                                            Saddress1 = reader["Saddress1"]?.ToString(),
                                            Saddress2 = reader["Saddress2"]?.ToString(),
                                            Saddress3 = reader["Saddress3"]?.ToString(),
                                            Scountry = reader["Scountry"]?.ToString(),
                                            Sstate = reader["Sstate"]?.ToString(),
                                            Szipcode = reader["Szipcode"]?.ToString(),
                                            Scity = reader["Scity"]?.ToString(),

                                            Baddress1 = reader["Baddress1"]?.ToString(),
                                            Baddress2 = reader["Baddress2"]?.ToString(),
                                            Baddress3 = reader["Baddress3"]?.ToString(),
                                            Bcountry = reader["Bcountry"]?.ToString(),
                                            Bstate = reader["Bstate"]?.ToString(),
                                            Bzipcode = reader["Bzipcode"]?.ToString(),
                                            BusinessBillingCity = reader["businessBillingCity"]?.ToString(),

                                            NatureOfBusinessActivity =
                                                reader["NatureOfBusinessActivity"]?.ToString(),

                                            DateOfEstablishment =
                                                reader["DateOfEstablishment"]?.ToString(),

                                            ContactPersonName =
                                                reader["ContactPersonName"]?.ToString(),

                                            Designation =
                                                reader["Designation"]?.ToString(),

                                            EmailId =
                                                reader["EmailId"]?.ToString(),

                                            MobileNo =
                                                reader["MobileNo"]?.ToString(),

                                            OfficeTelephoneNo =
                                                reader["OfficeTelephoneNo"]?.ToString(),

                                            TANNo =
                                                reader["TANNo"]?.ToString(),

                                            MSMERegistrationStatus =
                                                reader["MsmeRegistrationStatus"]?.ToString(),

                                            MSMENo =
                                                reader["MSMENo"]?.ToString(),

                                            BankName =
                                                reader["BankName"]?.ToString(),

                                            AccountName =
                                                reader["AccountName"]?.ToString(),

                                            AccountNumber =
                                                reader["AccountNumber"]?.ToString(),

                                            IfscCode =
                                                reader["IfscCode"]?.ToString(),

                                            BranchCode =
                                                reader["BranchCode"]?.ToString(),

                                            BankAddress =
                                                reader["BankAddress"]?.ToString(),

                                            DeclarationName =
                                                reader["DeclarationName"]?.ToString(),

                                            DeclarationDesignation =
                                                reader["DeclarationDesignation"]?.ToString(),

                                            EnterpriseType =
                                                reader["EnterpriseType"]?.ToString(),

                                            BusinessType =
                                                reader["BusinessType"]?.ToString(),

                                            AgencyEmail =
                                                reader["AgencyEmail"]?.ToString(),

                                            AgencyName =
                                                reader["AgencyName"]?.ToString(),

                                            VerificationNo =
                                                reader["VerificationNo"]?.ToString(),

                                            ContactPerson =
                                                reader["ContactPerson"]?.ToString()
                                        };
                                    }
                                }
                            }

                            // =====================================================
                            // 2. PaymentDetails
                            // =====================================================

                            string paymentQuery = $@"SELECT
        ""CreditDays"",
        ""DisCount"",
        ""PriceType"",
        ""MarkDownTax0"",
        ""MarkDownWithoutTax0"",
        ""MarkDownTax3"",
        ""MarkDownWithoutTax3"",
        ""MarkDownTax5"",
        ""MarkDownWithoutTax5"",
        ""MarkDownTax18"",
        ""MarkDownWithoutTax18"",
        ""BusinessType"",
        ""AgencyEmail"",
        ""AgencyName""
    FROM {sDBName}.""PaymentDetails""
    WHERE ""Id"" = ?";

                            using (OdbcCommand command = new OdbcCommand(paymentQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        response.PaymentDetails = new PaymentDetailsDto
                                        {
                                            CreditDays = reader["CreditDays"]?.ToString(),
                                            DisCount = reader["DisCount"]?.ToString(),
                                            PriceType = reader["PriceType"]?.ToString(),

                                            MarkDownTax0 =
                                                reader["MarkDownTax0"]?.ToString(),

                                            MarkDownWithoutTax0 =
                                                reader["MarkDownWithoutTax0"]?.ToString(),

                                            MarkDownTax3 =
                                                reader["MarkDownTax3"]?.ToString(),

                                            MarkDownWithoutTax3 =
                                                reader["MarkDownWithoutTax3"]?.ToString(),

                                            MarkDownTax5 =
                                                reader["MarkDownTax5"]?.ToString(),

                                            MarkDownWithoutTax5 =
                                                reader["MarkDownWithoutTax5"]?.ToString(),

                                            MarkDownTax18 =
                                                reader["MarkDownTax18"]?.ToString(),

                                            MarkDownWithoutTax18 =
                                                reader["MarkDownWithoutTax18"]?.ToString(),

                                            BusinessType =
                                                reader["BusinessType"]?.ToString(),

                                            AgencyEMail =
                                                reader["AgencyEmail"]?.ToString(),

                                            AgencyName =
                                                reader["AgencyName"]?.ToString()
                                        };
                                    }
                                }
                            }

                            // =====================================================
                            // 3. TEC_LED1 - BUSINESS DETAILS
                            // =====================================================

                            string businessQuery = $@"SELECT
        ""BusinessState"",
        ""GSTNumber"",
        ""AddressOfPlace"",
        ""GSTVendorClassification""
    FROM {sDBName}.""TEC_LED1""
    WHERE ""Id"" = ?";

                            using (OdbcCommand command = new OdbcCommand(businessQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.BusinessDetails.Add(new BusinessDetails
                                        {
                                            BusinessState = reader["BusinessState"]?.ToString(),
                                            GSTNumber = reader["GSTNumber"]?.ToString(),
                                            AddressOfPlace = reader["AddressOfPlace"]?.ToString(),
                                            GSTVendorClassification =
                                                reader["GSTVendorClassification"]?.ToString()
                                        });
                                    }
                                }
                            }

                            // =====================================================
                            // 4. TEC_LED2 - PARTNER DETAILS
                            // =====================================================

                            string partnerQuery = $@"SELECT
        ""Name"",
        ""Designation"",
        ""Contact_No"",
        ""Email_ID""
    FROM {sDBName}.""TEC_LED2""
    WHERE ""Id"" = ?";

                            using (OdbcCommand command = new OdbcCommand(partnerQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.PartnerDetails.Add(new PartnerDetails
                                        {
                                            Name = reader["Name"]?.ToString(),
                                            Designation = reader["Designation"]?.ToString(),
                                            Contact_No = reader["Contact_No"]?.ToString(),
                                            Email_ID = reader["Email_ID"]?.ToString()
                                        });
                                    }
                                }
                            }

                            // =====================================================
                            // 5. TEC_LED3 - OPERATIONAL CONTACTS
                            // =====================================================

                            string operationalQuery = $@"SELECT
        ""Department"",
        ""Name"",
        ""Designation"",
        ""ContactNo"",
        ""Email""
    FROM {sDBName}.""TEC_LED3""
    WHERE ""ID"" = ?";

                            using (OdbcCommand command = new OdbcCommand(operationalQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.OperationalContacts.Add(new OperationalContact
                                        {
                                            Department = reader["Department"]?.ToString(),
                                            Name = reader["Name"]?.ToString(),
                                            Designation = reader["Designation"]?.ToString(),
                                            ContactNo = reader["ContactNo"]?.ToString(),
                                            Email = reader["Email"]?.ToString()
                                        });
                                    }
                                }
                            }

                            // =====================================================
                            // 6. TEC_LED4 - MAJOR GOODS / SERVICES
                            // =====================================================

                            string goodsQuery = $@"SELECT
        ""MaterialDescription"",
        ""HSNCode"",
        ""Brand"",
        ""Size"",
        ""Product"",
        ""TaxPercentage""
    FROM {sDBName}.""TEC_LED4""
    WHERE ""Id"" = ?";

                            using (OdbcCommand command = new OdbcCommand(goodsQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.MajorGoodsServices.Add(new MajorGoodsService
                                        {
                                            MaterialDescription =
                                                reader["MaterialDescription"]?.ToString(),

                                            HSNCode =
                                                reader["HSNCode"]?.ToString(),

                                            Brand =
                                                reader["Brand"]?.ToString(),

                                            Size =
                                                reader["Size"]?.ToString(),

                                            ImageFile =
                                                reader["Product"]?.ToString(),

                                            TaxPercentage =
                                                reader["TaxPercentage"]?.ToString()
                                        });
                                    }
                                }
                            }

                            // =====================================================
                            // 7. TEC_LED5 - MAJOR CUSTOMERS
                            // =====================================================

                            string customerQuery = $@"SELECT
        ""CustomerName""
    FROM {sDBName}.""TEC_LED5""
    WHERE ""ID"" = ?";

                            using (OdbcCommand command = new OdbcCommand(customerQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.MajorCustomers.Add(new MajorCustomerDto
                                        {
                                            CustomerName =
                                                reader["CustomerName"]?.ToString()
                                        });
                                    }
                                }
                            }

                            // =====================================================
                            // 8. TEC_LED6 - OTHER INFORMATION
                            // =====================================================

                            string otherQuery = $@"SELECT
        ""Description"",
        ""TextMode""
    FROM {sDBName}.""TEC_LED6""
    WHERE ""ID"" = ?";

                            using (OdbcCommand command = new OdbcCommand(otherQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.OtherInformation.Add(new OtherInformation
                                        {
                                            Description =
                                                reader["Description"]?.ToString(),

                                            TextMode =
                                                reader["TextMode"]?.ToString()
                                        });
                                    }
                                }
                            }


                            string kycQuery = $@"
            SELECT
                ""LineId"",
                ""DocumentType"",
                ""DocumentName"",
                ""FileData""
            FROM {sDBName}.""TEC_LED7""
            WHERE ""Id"" = ?
            AND ""LineId"" BETWEEN 1 AND 4
            ORDER BY ""LineId""";

                            using (OdbcCommand command = new OdbcCommand(
                                kycQuery,
                                connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.KYCDocuments.Add(
                                            new KYCDocumentDto
                                            {
                                                LineId =
                                                    reader["LineId"] != DBNull.Value
                                                        ? Convert.ToInt32(reader["LineId"])
                                                        : 0,

                                                DocumentType =
                                                    reader["DocumentType"]?.ToString(),

                                                DocumentName =
                                                    reader["DocumentName"]?.ToString(),

                                                FileData =
                                                    reader["FileData"]?.ToString()
                                            });
                                    }
                                }
                            }


                            string additionalDocumentsQuery = $@"
            SELECT
                ""LineId"",
                ""DocumentType"",
                ""DocumentName"",
                ""FileData""
            FROM {sDBName}.""TEC_LED7""
            WHERE ""Id"" = ?
            AND ""LineId"" >= 5
            ORDER BY ""LineId""";

                            using (OdbcCommand command = new OdbcCommand(
                                additionalDocumentsQuery,
                                connection))
                            {
                                command.Parameters.AddWithValue("@Id", findId);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.Documents.Add(
                                            new DocumentDetails
                                            {
                                                LineId =
                                                    reader["LineId"] != DBNull.Value
                                                        ? Convert.ToInt32(reader["LineId"])
                                                        : 0,

                                                DocumentType =
                                                    reader["DocumentType"]?.ToString(),

                                                DocumentName =
                                                    reader["DocumentName"]?.ToString(),

                                                FileData =
                                                    reader["FileData"]?.ToString()
                                            });
                                    }
                                }
                            }


                            string draftApprovedQuery = $@"
            SELECT ""DraftApproved""
            FROM {sDBName}.""TEC_OLED""
            WHERE ""GstNo"" = ?
            AND ""Draft"" = 'Y'";

                            using (OdbcCommand command = new OdbcCommand(
                                draftApprovedQuery,
                                connection))
                            {
                                command.Parameters.AddWithValue("@GstNo", gstNumber);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        response.draftDetails = new DraftDetails
                                        {
                                            DraftApproved = reader["DraftApproved"]?.ToString() == "Y",
                                            page = 6
                                        };
                                    }
                                }
                            }
                        }
                    }


                }
                else
                {
                    return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Failure, "No records found for the entered GST Number", null);
                }

            }
            return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Successfully retrieved the GST Details", response);

        }

        private string GetFileTypeFromExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".pdf":
                    return "pdf";
                case ".jpg":
                case ".jpeg":
                    return "jpg";
                case ".png":
                    return "png";
                default:
                    return "unknown";
            }
        }
        private string GetContentType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".pdf": return "application/pdf";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                default: return "application/octet-stream";
            }
        }
        private string GetFileType(string documentType)
        {
            if (documentType.Length >= 4 && documentType.Substring(0, 4).Equals("JVBE", StringComparison.OrdinalIgnoreCase))
            {
                return "pdf"; // Return pdf if the first four characters are "JVBE"
            }

            return "image";
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
                string sp = $@"CALL ""{sDBName}"".""GetContactTypes""";

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
            string[] documentTypes = { "PAN Card", "GST Certificate", "Bank Account", "MSME Certificate" };
            foreach (var documentType in documentTypes)
            {
                _sessionManager.Set($"Path_{documentType}", "");
                _sessionManager.Set($"base64_{documentType}", "");
                _sessionManager.Set($"FileName_{documentType}", "");
            }
        }
        public void RefreshKYC1()
        {
            string documentType = "Performa Invoice";
            _sessionManager.Set($"Path_{documentType}", "");
            _sessionManager.Set($"base64_{documentType}", "");
            _sessionManager.Set($"FileName_{documentType}", "");
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

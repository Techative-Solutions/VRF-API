using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Text;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using static VRF_API.Model.ResponseModel.EnumResponse;

using System.Data.Common;
using Company = SAPbobsCOM.Company;
using VRF_API.Repository;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Security.Cryptography;
using SAPbobsCOM;
using Attachments2_Lines = VRF_API.Model.ResponseModel.Attachments2_Lines;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting.Server;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net.Http;
using Microsoft.Extensions.Configuration;


namespace VRF_API.Services
{
    public interface IRegistrationForm
    {
        Task<object> RejistrationDetails(
           string UserNme,
           string status);
        Task<ApiResponse> PushToSAP(string GroupCode,
          string VendorName,
          string vendorType,
          string gstNumber,
          string UserName
          );
        Task<List<GroupCode>> GroupCode(string UserName);

        Task<ApiResponse> Approved(string Remarks,
            string UserName,
            string GstNumber
            );
        Task<ApiResponse> Reject(
           string Reason,
           string GstNumber,
           string UserName,
           string Status
           );
    }


    public class RegistrationForm : IRegistrationForm
    {
        private readonly Repository.Log log;
        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly string sConstr;
        string CN1, CN2, CV1, CV2;
        public RegistrationForm(IConfiguration configuration, OdbcConnection connection, Repository.Log _log)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            log = _log;

        }



        public async Task<List<GroupCode>> GroupCode(string UserName)
        {
            const string functionName = "GroupCode";
            //   Log.Information("Starting function {FunctionName}", functionName);
            const string spName = "TEC_VRF_GETBPGROUPLIST";
            string query = @$"CALL ""{sDBName}"".""{spName}"" (?)";

            // Log.Debug("SQL Query for {FunctionName}: {Query}", functionName, query);

            List<GroupCode> RejDetailsList = new();

            try
            {
                using var connection = new OdbcConnection(sConstr);
                // Log.Debug("Opening ODBC connection...");
                connection.Open();

                using var cmd = new OdbcCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserName", UserName);
                // Log.Debug("Executing SQL query...");
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var detail = new GroupCode
                    {
                        Code = reader["Code"] == DBNull.Value ? null : reader["Code"].ToString(),
                        Name = reader["Name"] == DBNull.Value ? null : reader["Name"].ToString(),



                    };

                    RejDetailsList.Add(detail);
                }

                //Log.Information(
                //    "{FunctionName} executed successfully. Total records loaded: {Count}",
                //    functionName, cusDetailsList.Count
                //);

                return RejDetailsList;
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Error in {FunctionName}. Message: {Message}", functionName, ex.Message);
                return new List<GroupCode>();
            }
            finally
            {
                // Log.Information("Ending function {FunctionName}", functionName);
            }
        }
        public async Task<object> RejistrationDetails(
           string UserNme,
           string status)
        {
            try
            {
                using var connection = new OdbcConnection(sConstr);

                await connection.OpenAsync();

                switch (status?.ToLower())
                {
                    case "approval":
                        return await GetApprovalDetails(connection, UserNme);

                    case "draft":
                        return await GetDraftDetails(connection, UserNme);

                    case "pending":
                        return await GetPendingDetails(connection, UserNme);

                    case "completed":
                        return await GetCompletedDetails(connection, UserNme);

                    case "rejected":
                        return await GetRejectedDetails(connection, UserNme);

                    case "sap":
                        return await GetSapDetails(connection, UserNme);

                    case "created":
                        return await GetCreatedVendorDetails(connection, UserNme);

                    default:
                        return new List<object>();
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }
        private static string? GetString(
     DbDataReader reader,
     string columnName)
        {
            try
            {
                if (reader[columnName] == DBNull.Value)
                    return null;

                return reader[columnName]?.ToString();
            }
            catch
            {
                return null;
            }
        }
        private static string? GetDate(
    DbDataReader reader,
    string columnName)
        {
            try
            {
                if (reader[columnName] == DBNull.Value)
                    return null;

                return Convert
                    .ToDateTime(reader[columnName])
                    .ToString("dd-MM-yyyy");
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<PendingResponse>> GetPendingDetails(
OdbcConnection connection,
string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetApprovalDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<PendingResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new PendingResponse
                {
                    TradeName = GetString(reader, "TName"),
                    BusinessState = GetString(reader, "Bstate"),
                    NatureOfBusiness = GetString(
                        reader,
                        "NatureOfBusinessActivity"),
                    GstNumber = GetString(reader, "GstNo"),

                    AppliedDate = GetDate(
                        reader,
                        "DateOfEstablishment"),


                });
            }

            return list;
        }
        private async Task<List<ApprovalResponse>> GetApprovalDetails(
  OdbcConnection connection,
  string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetApprovalWaitingDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<ApprovalResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new ApprovalResponse
                {
                    TradeName = GetString(reader, "TName"),
                    BusinessState = GetString(reader, "Bstate"),
                    NatureOfBusiness = GetString(
                        reader,
                        "NatureOfBusinessActivity"),
                    GstNumber = GetString(reader, "GstNo"),

                    AppliedDate = GetDate(
                        reader,
                        "DateOfEstablishment"),

                    WaitingorApproval = GetString(
                        reader,
                        "ApprovalWaiting"),

                    DepartmentLevel = GetString(
                        reader,
                        "Level")
                });
            }

            return list;
        }

        private async Task<List<CompletedResponse>> GetCompletedDetails(
    OdbcConnection connection,
    string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetAprrovedDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<CompletedResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new CompletedResponse
                {
                    TradeName = GetString(reader, "TName"),
                    BusinessState = GetString(reader, "Bstate"),
                    NatureOfBusiness = GetString(
                        reader,
                        "NatureOfBusinessActivity"),
                    GstNumber = GetString(reader, "GstNo"),

                    AppliedDate = GetDate(
                        reader,
                        "DateOfEstablishment"),

                    ApprovedDate = GetDate(
                        reader,
                        "ApprovedDate"),


                });
            }

            return list;
        }

        private async Task<List<DraftResponse>> GetDraftDetails(
    OdbcConnection connection,
    string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetDraftDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<DraftResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new DraftResponse
                {
                    TradeName = GetString(reader, "TName"),
                    BusinessState = GetString(reader, "Bstate"),

                    NatureOfBusiness = GetString(
                        reader,
                        "NatureOfBusinessActivity"),

                    GstNumber = GetString(reader, "GstNo"),

                    AppliedDate = GetDate(
                        reader,
                        "DateOfEstablishment")
                });
            }

            return list;
        }

        private async Task<List<RejectedResponse>> GetRejectedDetails(
    OdbcConnection connection,
    string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetRejectedDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<RejectedResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new RejectedResponse
                {
                    TradeName = GetString(reader, "TName"),

                    BusinessState = GetString(
                        reader,
                        "Bstate"),

                    NatureOfBusiness = GetString(
                        reader,
                        "NatureOfBusinessActivity"),

                    GstNumber = GetString(
                        reader,
                        "GstNo"),

                    AppliedDate = GetDate(
                        reader,
                        "DateOfEstablishment"),

                    RejectedDate = GetDate(
                        reader,
                        "RejectedDate"),

                    RejectedReason = GetString(
                        reader,
                        "RejectedReason")
                });
            }

            return list;
        }

        private async Task<List<SapResponse>> GetSapDetails(
    OdbcConnection connection,
    string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetSapPostDetails"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<SapResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new SapResponse
                {
                    VendorName = GetString(
                        reader,
                        "VendorName"),

                    TradeName = GetString(
                        reader,
                        "TName"),

                    GstNumber = GetString(
                        reader,
                        "GstNo"),

                    SaprejReson = GetString(
                        reader,
                        "SAPRejReason")
                });
            }

            return list;
        }


        private async Task<List<CreatedVendorResponse>>
    GetCreatedVendorDetails(
        OdbcConnection connection,
        string userName)
        {
            string query =
                $@"CALL ""{sDBName}"".""TEC_GetPostedVendors"" (?)";

            using var cmd = new OdbcCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserName", userName);

            using var reader = await cmd.ExecuteReaderAsync();

            List<CreatedVendorResponse> list = new();

            while (await reader.ReadAsync())
            {
                list.Add(new CreatedVendorResponse
                {
                    CardCode = GetString(
                        reader,
                        "CardCode"),

                    GstNumber = GetString(
                        reader,
                        "GstNo")
                });
            }

            return list;
        }



        public DataTable ExecuteQueryForDataTable(string sQuery)
        {
            String sFuncName = "HanaExecuteQueryReturnDataTable";
            log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [START] - Executing query for DataTable", sFuncName);
            OdbcConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            try
            {
                // log.WriteToLogFile_Debug("Starting the function", sFuncName);
                string SAP_Constr = sConstr;
                SAP_Con = new OdbcConnection(SAP_Constr);
                SAP_Con.Open();
                OdbcCommand SAP_Cmd = new OdbcCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                OdbcDataAdapter SAP_da = new OdbcDataAdapter();
                SAP_da.SelectCommand = SAP_Cmd;
                SAP_da.Fill(dt);
                //log.WriteToLogFile_Debug("Completed the function successfully", sFuncName);
                log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [DB_END] - Query executed successfully, filled " + dt.Rows.Count + " rows", sFuncName);
                return dt;
            }
            catch (Exception ex)
            {
                // log.WriteToLogFile_Debug(ex.Message, sFuncName);
                log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [ERROR] - Exception occurred: " + ex.Message, sFuncName);
                throw new Exception(ex.Message);
            }
            finally
            {
                if ((SAP_Con != null))
                {
                    SAP_Con.Close();
                    SAP_Con.Dispose();
                }
            }
        }


        public string GetSingleValue(string sQuery)
        {
            log.WriteToLogFile_Debug("[DBConnection] [GetSingleValue] [START] - Fetching single value", "GetSingleValue");
            OdbcConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            string sSingleValue = string.Empty;

            try
            {
                string SAP_Constr = sConstr;
                SAP_Con = new OdbcConnection(SAP_Constr);
                SAP_Con.Open();
                OdbcCommand SAP_Cmd = new OdbcCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                OdbcDataAdapter SAP_da = new OdbcDataAdapter();
                SAP_da.SelectCommand = SAP_Cmd;
                SAP_da.Fill(dt);

                if (dt.Rows.Count > 0)
                    sSingleValue = dt.Rows[0][0].ToString().Trim();

                else
                {
                }

                log.WriteToLogFile_Debug("[DBConnection] [GetSingleValue] [DB_END] - Value retrieved successfully: " + sSingleValue, "GetSingleValue");
                return sSingleValue;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug("[DBConnection] [GetSingleValue] [ERROR] - Exception occurred: " + ex.Message, "GetSingleValue");
                throw ex;
            }
            finally
            {
                if ((SAP_Con != null))
                {
                    SAP_Con.Close();
                    SAP_Con.Dispose();
                }
            }
        }
        public static string DecryptFun(string password)
        {
            string key = "TechativeSolutions04December2023";
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(password);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        public void ExecuteNonQuery(string sQuery)
        {
            string SAP_Constr = sConstr;
            OdbcConnection oCon = new OdbcConnection(SAP_Constr);
            OdbcCommand oCmd = new OdbcCommand();
            OdbcDataAdapter oSQLAdapter = new OdbcDataAdapter();

            try
            {
                oCmd.CommandType = CommandType.Text;
                oCmd.CommandText = sQuery;
                oCmd.Connection = oCon;
                oCmd.CommandTimeout = 0;
                if (oCon.State == ConnectionState.Closed)
                    oCon.Open();
                oCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (oCon != null)
                {
                    oCon.Close();
                    oCon.Dispose();
                }
            }
        }
        public void LogVendorJson(string cardCode, string json)
        {
            try
            {
                string folderPath = _configuration["ApprovalPosting:VendorJsonLogPath"];

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Create file path
                string filePath = Path.Combine(folderPath, "VendorLog.txt");

                // Create file if not exists
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }

                string logMessage = $"Date: {DateTime.Now}\r\n" +
                                    $"CardCode: {cardCode}\r\n" +
                                    $"JSON: {json}\r\n" +
                                    $"-----------------------------------------\r\n";

                File.AppendAllText(filePath, logMessage);
            }
            catch (Exception ex)
            {
                // optional error handling
            }
        }
        public async Task<ApiResponse> PushToSAP(string GroupCode,
            string VendorName,
            string vendorType,
            string gstNumber,
            string UserName
            )
        {

            Company oCompany;
            string Id = string.Empty;
            string CardCode = string.Empty;

            try
            {


                DataTable dataTable = null;
                DataTable dataTable1 = null;
                DataTable dataTable2 = null;
                string VendorCode = string.Empty;

                if (string.IsNullOrEmpty(GroupCode))
                {

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Please select Group Code.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                VendorCode += $"{vendorType}-{VendorName},";
                dataTable = ExecuteQueryForDataTable($@"call ""{sDBName}"".""BPDetails"" ('CARDDETAILS')");
                dataTable1 = ExecuteQueryForDataTable($@"call ""{sDBName}"".""VendorCreation""('" + gstNumber + "','" + VendorCode + "')");
                dataTable2 = ExecuteQueryForDataTable($@"call ""{sDBName}"".""BPDetails"" ('SERIES')");

                string DBName = _configuration["ApprovalPosting:DBName"];
                string DB = _configuration["ApprovalPosting:DBName1"];
                string user = DecryptFun(_configuration["ApprovalPosting:SAPUserName"]);
                string Pass = DecryptFun(_configuration["ApprovalPosting:SAPPassword"]);
                string TransURL = _configuration["ApprovalPosting:TransURL"];
                string loginURL = _configuration["ApprovalPosting:loginURL"];
                string StrRouteVal = "";


                oCompany = new Company
                {
                    Server = _configuration["ApprovalPosting:ServerIP"],
                    LicenseServer = _configuration["ApprovalPosting:LicenseServer"],
                    DbServerType = BoDataServerTypes.dst_HANADB,
                    CompanyDB = _configuration["ApprovalPosting:DBName1"],
                    UserName = DecryptFun(_configuration["ApprovalPosting:SAPUserName"]),
                    Password = DecryptFun(_configuration["ApprovalPosting:SAPPassword"]),
                    language = BoSuppLangs.ln_English,
                    UseTrusted = false
                };

                if (oCompany.Connect() != 0)
                {
                    string err = oCompany.GetLastErrorDescription();
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = err,
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }


                BusinessPartners oBusinessPartner = (BusinessPartners)oCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);
                for (int i = 0; i < dataTable1.Rows.Count; i++)
                {

                    Id = dataTable1.Rows[i]["Id"].ToString();
                    CardCode = dataTable1.Rows[i]["CardCode"].ToString();
                    //string tempFolderPath = ConfigurationManager.AppSettings["TempAttachmentPath"];
                    string tempFolderPath = GetSingleValue($@"select ""AttachPath"" from ""{DB}"".""OADP""");
                    if (!Directory.Exists(tempFolderPath))
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }

                    List<Attachments2_Lines> attachmentLines = new List<Attachments2_Lines>();

                    string count;
                    string Type = _configuration["ServerType"];
                    count = GetSingleValue($@"select Count(*) from ""{DBName}"".""TEC_LED7"" where ""Id""='" + dataTable1.Rows[i]["Id"].ToString() + "'");


                    int count1 = Convert.ToInt32(count);
                    for (int j = 1; j <= count1; j++)
                    {
                        string base64String;
                        string fileNameFromDB;
                        string fileTypeFromDB;

                        base64String = GetSingleValue($@"select ""FileData"" from ""{DBName}"".""TEC_LED7"" where ""Id""='{dataTable1.Rows[i]["Id"].ToString()} ' and ""LineId""='{j}'");

                        fileNameFromDB = GetSingleValue($@"select ""DocumentType"" from ""{DBName}"".""TEC_LED7"" where ""Id""='{dataTable1.Rows[i]["Id"].ToString()} ' and ""LineId""='{j} '");

                        fileTypeFromDB = GetSingleValue($@"select ""DocumentName"" from ""{DBName}"".""TEC_LED7"" where ""Id""='{dataTable1.Rows[i]["Id"].ToString()}' and ""LineId""='{j}'");
                        if (!string.IsNullOrWhiteSpace(base64String) && !string.IsNullOrWhiteSpace(fileTypeFromDB))

                        {
                            // Ensure unique filename
                            string fileExtension = Path.GetExtension(fileTypeFromDB); // e.g. ".pdf"
                            string uniqueFileName = $"BPMaster_Doc-{dataTable1.Rows[i]["Id"].ToString()}-Line{j}-{fileNameFromDB}{fileExtension}";
                            string fullFilePath = Path.Combine(tempFolderPath, uniqueFileName);

                            // Write to diskif (File.Exists(filepath))
                            string filepath = base64String;
                            if (File.Exists(filepath))
                            {
                                base64String = Convert.ToBase64String(File.ReadAllBytes(filepath));
                                Console.WriteLine(base64String);
                            }
                            else
                            {
                                Console.WriteLine("File not found: " + filepath);
                            }

                            byte[] fileBytes = Convert.FromBase64String(base64String);
                            File.WriteAllBytes(fullFilePath, fileBytes);

                            // Prepare attachment line
                            Attachments2_Lines line = new Attachments2_Lines()
                            {
                                FileName = Path.GetFileNameWithoutExtension(uniqueFileName),
                                FileExtension = fileExtension.TrimStart('.'),
                                SourcePath = tempFolderPath
                            };

                            attachmentLines.Add(line);
                        }
                    }
                    // After filling your attachmentLines list
                    AttachmentsWrapper wrapper = new AttachmentsWrapper
                    {
                        Attachments2_Lines = attachmentLines
                    };

                    string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                    // writeLog("Attachment Json : " + json, "VendorCreation");
                    string Discount = GetSingleValue($@"Select ifnull(""DisCount"",'0') from ""{DBName}"".""PaymentDetails"" where ""Id"" = '{dataTable1.Rows[i]["Id"].ToString()}'");
                    // writeLog("Discount : " + Discount, "VendorCreation");
                    if (string.IsNullOrEmpty(Discount)) Discount = "0";
                    string CreditDays = GetSingleValue($@"Call ""{DBName}"".""TEC_GetCreditDaysDetails""('" + dataTable1.Rows[i]["Id"].ToString() + "')") != "" ? GetSingleValue($@"Call ""{DBName}"".""TEC_GetCreditDaysDetails""('" + dataTable1.Rows[i]["Id"].ToString() + "')") : "0";
                    //writeLog("CreditDays : " + CreditDays, "VendorCreation");
                    string creditcode = GetSingleValue($@"Select Trim(""CreditDays"") from ""{DBName}"".""PaymentDetails"" where ""Id""='" + dataTable1.Rows[i]["Id"].ToString() + "'");
                    //writeLog("CreditCode : " + creditcode, "VendorCreation");
                    string creditDaysNumber = new string(CreditDays.Where(char.IsDigit).ToArray());

                    // creditCode is already numeric, but still clean it just in case
                    string creditCodeNumber = new string(creditcode.Where(char.IsDigit).ToArray());

                    // Compare
                    string result = (creditDaysNumber == creditCodeNumber) ? "nodefault" : "default";

                    string GroupNum = GetSingleValue($@"call ""{DBName}"".""TEC_GetGroupNum"" ('" + CreditDays + "')") != "" ? GetSingleValue($@"call ""{DBName}"".""TEC_GetGroupNum"" ('" + CreditDays + "')") : "0";
                    //writeLog("GroupNum : " + GroupNum, "VendorCreation");
                    //writeLog("GroupCode : " + groupCode, "VendorCreation");
                    string Remarks = GetSingleValue($@"Call ""{DBName}"".""GetRemarks""('" + dataTable1.Rows[i]["GstNo"].ToString() + "')");
                    //writeLog("Remarks : " + Remarks, "VendorCreation");
                    string rDoc = "0";
                    string sessionId = Login(TransURL, DB, user, Pass, out StrRouteVal);
                    string strRoutevalue = "";
                    string Result = TransactionPosting(TransURL + "Attachments2", json, sessionId, "Attachment", strRoutevalue, DB);
                    // writeLog("Attachment Result : " + Result, "VendorCreation");
                    int AbsEntry = Convert.ToInt32(Result);
                    // writeLog("Attachment Entry : " + AbsEntry, "VendorCreation");
                    Id = dataTable1.Rows[i]["Id"].ToString();
                    Vendor1 vendor = new Vendor1
                    {
                        CardCode = CardCode,
                        CardName = dataTable1.Rows[i]["TName"].ToString(),
                        CardType = "cSupplier",
                        GroupCode = Convert.ToInt32(GroupCode.ToString()),
                        Phone1 = dataTable1.Rows[i]["MobileNo"].ToString(),
                        Phone2 = dataTable1.Rows[i]["VerificationNo"].ToString(),
                        EmailAddress = dataTable1.Rows[i]["EmailId"].ToString(),
                        FederalTaxID = dataTable1.Rows[i]["PanNo"].ToString(),
                        DiscountPercent = Convert.ToDouble(
    Discount.Replace("%", "").Trim()
),
                        PayTermsGrpCode = Convert.ToInt32(GroupNum),
                        FreeText = Remarks,
                        AttachmentEntry = AbsEntry,
                        U_MSMENo = dataTable1.Rows[i]["MSMENo"].ToString(),
                        U_VRFAppover = UserName,
                        ContactEmployees = new List<ContactEmployee>
    {
        new ContactEmployee
        {
            Name = dataTable1.Rows[i]["ContactPersonName"].ToString()
        }
                        },

                        BPAddresses = new List<Address>
    {
        new Address
        {
            AddressName = "Billing",
            AddressType = "bo_BillTo",
            Street = dataTable1.Rows[i]["Raddress1"].ToString(),
            City = dataTable1.Rows[i]["registeredOfficeCity"].ToString(),
            ZipCode = dataTable1.Rows[i]["Rzipcode"].ToString(),
            Country = dataTable1.Rows[i]["Rcountry"].ToString(),
            State = dataTable1.Rows[i]["Rstate"].ToString(),
            GSTIN = dataTable1.Rows[i]["GstNo"].ToString(),

        }
    },

                        BPBankAccounts = new List<BankAccount>
    {
        new BankAccount
        {
            BankCode = dataTable1.Rows[i]["BankName"].ToString(),
            AccountNo = dataTable1.Rows[i]["AccountNumber"].ToString(),
            AccountName = dataTable1.Rows[i]["AccountName"].ToString(),
            BICSwiftCode = dataTable1.Rows[i]["IfscCode"].ToString()
        }
    }
                    }
                ;
                    string json1 = JsonConvert.SerializeObject(vendor);
                    LogVendorJson(vendor.CardCode, json1);



                    {
                        CardCode = dataTable1.Rows[i]["CardCode"].ToString();
                        // Set header-level data
                        oBusinessPartner.CardCode = CardCode;//dataTable1.Rows[i]["CardCode"].ToString();
                        oBusinessPartner.CardName = dataTable1.Rows[i]["TName"].ToString();
                        oBusinessPartner.CardType = SAPbobsCOM.BoCardTypes.cSupplier;
                        oBusinessPartner.GroupCode = Convert.ToInt32(GroupCode);
                        string PanNumber = dataTable1.Rows[i]["PanNo"].ToString();
                        oBusinessPartner.UserFields.Fields.Item("U_MSMENo").Value = dataTable1.Rows[i]["MSMENo"].ToString();

                        //oBusinessPartner.FederalTaxID = PanNumber;
                        oBusinessPartner.Phone1 = dataTable1.Rows[i]["MobileNo"].ToString();
                        oBusinessPartner.Phone2 = dataTable1.Rows[i]["VerificationNo"].ToString();
                        oBusinessPartner.EmailAddress = dataTable1.Rows[i]["EmailId"].ToString();
                        oBusinessPartner.DiscountPercent = Convert.ToDouble(Discount);
                        oBusinessPartner.PayTermsGrpCode = Convert.ToInt32(GroupNum);
                        oBusinessPartner.FiscalTaxID.TaxId0 = dataTable1.Rows[i]["PanNo"].ToString();

                        //oBusinessPartner.FiscalTaxID = dataTable1.Rows[i]["PanNo"].ToString();
                        oBusinessPartner.FreeText = Remarks;//conn.GetSingleValue("call \"GetRemarks\"('" + dataTable1.Rows[i]["GstNo"].ToString() + "')");
                        oBusinessPartner.ContactPerson = dataTable1.Rows[i]["ContactPersonName"].ToString();

                        oBusinessPartner.ContactEmployees.Name = dataTable1.Rows[i]["ContactPersonName"].ToString();

                        oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                        oBusinessPartner.Addresses.AddressName = dataTable1.Rows[i]["TName"].ToString().Trim().Length > 50
                                                                ? dataTable1.Rows[i]["TName"].ToString().Trim().Substring(0, 50)
                                                                : dataTable1.Rows[i]["TName"].ToString().Trim();
                        oBusinessPartner.Addresses.Street = dataTable1.Rows[i]["Baddress1"].ToString();
                        oBusinessPartner.Addresses.Street = dataTable1.Rows[i]["Baddress2"].ToString();
                        oBusinessPartner.Addresses.StreetNo = dataTable1.Rows[i]["Raddress3"].ToString();
                        oBusinessPartner.Addresses.City = dataTable1.Rows[i]["businessBillingCity"].ToString();
                        oBusinessPartner.Addresses.ZipCode = dataTable1.Rows[i]["Bzipcode"].ToString();
                        oBusinessPartner.Addresses.Country = dataTable1.Rows[i]["Bcountry"].ToString();
                        oBusinessPartner.Addresses.State = dataTable1.Rows[i]["Bstate"].ToString();
                        oBusinessPartner.Addresses.GSTIN = dataTable1.Rows[i]["GstNo"].ToString();
                        oBusinessPartner.Addresses.GstType = SAPbobsCOM.BoGSTRegnTypeEnum.gstRegularTDSISD;
                        oBusinessPartner.Addresses.UserFields.Fields.Item("U_IsVgst").Value = "Y";
                        oBusinessPartner.Addresses.Add();


                        oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                        oBusinessPartner.Addresses.AddressName = dataTable1.Rows[i]["TName"].ToString().Trim().Length > 50
                                                                ? dataTable1.Rows[i]["TName"].ToString().Trim().Substring(0, 50)
                                                                : dataTable1.Rows[i]["TName"].ToString().Trim();
                        oBusinessPartner.Addresses.Street = dataTable1.Rows[i]["Raddress1"].ToString();
                        oBusinessPartner.Addresses.Block = dataTable1.Rows[i]["Raddress2"].ToString();
                        oBusinessPartner.Addresses.StreetNo = dataTable1.Rows[i]["Raddress3"].ToString();
                        oBusinessPartner.Addresses.City = dataTable1.Rows[i]["registeredOfficeCity"].ToString();
                        oBusinessPartner.Addresses.ZipCode = dataTable1.Rows[i]["Rzipcode"].ToString();
                        oBusinessPartner.Addresses.Country = dataTable1.Rows[i]["Rcountry"].ToString();
                        oBusinessPartner.Addresses.State = dataTable1.Rows[i]["Rstate"].ToString();
                        oBusinessPartner.Addresses.GSTIN = dataTable1.Rows[i]["GstNo"].ToString();
                        oBusinessPartner.Addresses.GstType = SAPbobsCOM.BoGSTRegnTypeEnum.gstRegularTDSISD;
                        oBusinessPartner.Addresses.UserFields.Fields.Item("U_IsVgst").Value = "Y";
                        oBusinessPartner.Addresses.Add();


                        oBusinessPartner.BPBankAccounts.AccountNo = dataTable1.Rows[i]["AccountNumber"].ToString();
                        //oBusinessPartner.BPBankAccounts.bankname = dataTable1.Rows[i]["BankName"].ToString();
                        oBusinessPartner.BPBankAccounts.BankCode = dataTable1.Rows[i]["BankName"].ToString();//"SBI";//dataTable1.Rows[i]["IfscCode"].ToString();
                        oBusinessPartner.BPBankAccounts.BICSwiftCode = dataTable1.Rows[i]["IfscCode"].ToString();
                        oBusinessPartner.BPBankAccounts.AccountName = dataTable1.Rows[i]["AccountName"].ToString();
                        ////oBusinessPartner.BPBankAccounts.Country = "India";//dataTable1.Rows[i]["BankCountry"].ToString();
                        oBusinessPartner.BPBankAccounts.Add();
                        oBusinessPartner.UserFields.Fields.Item("U_VRFAppover").Value = UserName;


                        oBusinessPartner.AttachmentEntry = AbsEntry;

                    }
                    if (oBusinessPartner.Add() != 0)
                    {
                        string err = oCompany.GetLastErrorDescription();
                        //Console.WriteLine($"Error: {oCompany.GetLastErrorDescription()}");
                        //Log.WriteToLogFile_Debug(err, "SAP Posting");
                        string error = err.Replace("'", "") + "'||'Vendor-'||'" + dataTable1.Rows[i]["CardCode"].ToString();
                        string update = $@"update ""{sDBName}"".""TEC_OLED"" set ""SAPRejReason""='{error}' where ""Id""='{dataTable1.Rows[i]["Id"].ToString()}'";

                        ExecuteNonQuery(update);

                        return new ApiResponse
                        {
                            Status = ApiStatusEnum.Failure,
                            Message = err,
                            ErrorCode = ErrorCodeEnum.Failure,
                            Data = null
                        };

                    }
                    else
                    {

                        //  Log.WriteToLogFile_Debug("Posting Completed for the Traders" + dataTable1.Rows[i]["TName"].ToString() + "    Completed Successfully.", "SAP Posting");
                        string update = "";
                        if (result == "nodefault")
                        {
                            update = $@"update ""{sDBName}"".""TEC_OLED"" set ""SAPRejReason""=''||'Vendor-'||'{dataTable1.Rows[i]["CardCode"].ToString()}'||'->Created' where ""Id""='{dataTable1.Rows[i]["Id"].ToString()}'";
                        }
                        else
                        {
                            update = $@"update ""{sDBName}"".""TEC_OLED"" set ""SAPRejReason""=''||'Vendor-'||'{dataTable1.Rows[i]["CardCode"].ToString()}'||'->Created' where ""Id""='{dataTable1.Rows[i]["Id"].ToString()}'";
                        }
                        string toMail = GetSingleValue($@"Select ""EmailId"" from ""{sDBName}"".""TEC_OLED"" where ""Id"" = '{dataTable1.Rows[i]["Id"].ToString()} '");

                        ExecuteNonQuery(update);

                        //SentMail(toMail, gstNumber, dataTable1.Rows[i]["CardCode"].ToString());

                        ExecuteNonQuery($@"Insert into ""{sDBName}"".""Mail_Log"" (""GstNo"",""Type"",""ActionDate"") values('{dataTable1.Rows[i]["GstNo"].ToString()}','Created',Current_Date)");
                    }

                    if (!(string.IsNullOrEmpty(CardCode)))
                    {
                        string message = "Business Partner added successfully!";




                        return new ApiResponse
                        {
                            Status = ApiStatusEnum.Success,
                            Message = message,
                            ErrorCode = ErrorCodeEnum.Success,
                            Data = null
                        };

                    }




                }



                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Business Partner added successfully!",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                string update = $@"update ""{sDBName}"".""TEC_OLED"" set ""SAPRejReason""='{ex.Message.Replace("'", "")}'||'-->Vendor-->'||'{CardCode}' where ""Id""='{Id}'";

                ExecuteNonQuery(update);

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = "An error occurred while processing the login.",
                    Data = null
                };

            }
            finally
            {
                // Disconnect from the company

                Console.WriteLine("Disconnected from SAP Business One.");
            }
        }

        private DataTable GetVendorDetails(string gstNo)
        {
            string query = $@"select * from ""{sDBName}"".""TEC_OLED"" where ""GstNo""='{gstNo}'";
            //writeLog("GetVendorDetails Query : " + query, "Mail");
            DataTable dt = ExecuteQueryForDataTable(query);
            return dt;
        }

        private List<GoodItem> LoadGoodsByGST(string gstNo)
        {
            List<GoodItem> goods = new List<GoodItem>();
            string Id = GetSingleValue($@"select * from ""{sDBName}"".""TEC_OLED"" where ""GstNo""='{gstNo}'");
            string query = $@"select * from ""{sDBName}"".""TEC_LED4"" where ""Id""='{Id}'";
            DataTable dt = ExecuteQueryForDataTable(query);
            if (dt.Rows.Count > 0)
            {
                int i = 1;
                foreach (DataRow row in dt.Rows)
                {
                    goods.Add(new GoodItem
                    {
                        SerialNo = i++,
                        Product = row["Product"]?.ToString(),
                        Brand = row["Brand"]?.ToString(),
                        Size = row["Size"]?.ToString(),
                        MaterialDescription = row["MaterialDescription"]?.ToString(),
                        HSNCode = row["HSNCode"]?.ToString(),
                        TaxPercentage = row["TaxPercentage"]?.ToString()
                    });
                }
            }
            return goods;
        }
        //private string GenerateVendorHtmlWithData(Dictionary<string, object> data, List<GoodItem> goods)
        //{
        //    string htmlTemplate = System.IO.File.ReadAllText(Server.MapPath("~/Design/VendorForm1.html"));
        //    htmlTemplate = Regex.Replace(htmlTemplate, @"<div class=""watermark""[^>]*>.*?</div>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        //    htmlTemplate = PopulateFields(htmlTemplate, data);
        //    htmlTemplate = htmlTemplate.Replace(
        //        "<div class=\"items-section\" id=\"items_supplied\"></div>",
        //        GenerateItemsHtml(goods)
        //    );
        //    return htmlTemplate;
        //}
        private string GenerateItemsHtml(List<GoodItem> goods)
        {
            StringBuilder items = new StringBuilder();
            string[] letters = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
            for (int i = 0; i < goods.Count && i < letters.Length; i++)
            {
                var item = goods[i];
                items.Append($@"
            <div style=""margin-bottom: 12px; line-height: 1.4;"">
                <div style=""margin-bottom: 6px;""><strong>{letters[i]}) Product:</strong> {HttpUtility.HtmlEncode(item.Product ?? "")}</div>
                <div style=""margin-bottom: 6px;""><strong>Brand:</strong> {HttpUtility.HtmlEncode(item.Brand ?? "")}</div>
                <div><strong>Size:</strong> {HttpUtility.HtmlEncode(item.Size ?? "")}</div>
            </div>");
            }
            return $"<div class=\"items-section\" id=\"items_supplied\">{items}</div>";
        }

        //protected void SentMail(string toMail, string selectedGST, string CardCode)
        //{
        //    string gstNo = selectedGST;
        //    string agentMail = string.Empty;
        //    var data1 = new Dictionary<string, object>();
        //    List<GoodItem> goods = new List<GoodItem>();
        //   // writeLog("SentMail started", "Mail");
        //    DataTable ds = GetVendorDetails(gstNo);
        //    //writeLog("Getting data", "Mail");
        //    if (ds.Rows.Count > 0)
        //    {
        //        // writeLog("Getting Row Data", "Mail");
        //        DataRow dr = ds.Rows[0];

        //        Dictionary<string, object> data = new Dictionary<string, object>();

        //        // ===== Basic Info =====
        //        data["GST Number"] = dr["GstNo"]?.ToString();
        //        data["PAN Number"] = dr["PanNo"]?.ToString();
        //        data["Trade Name"] = dr["TName"]?.ToString();
        //        data["Nature of Business"] = dr["NatureOfBusinessActivity"]?.ToString();
        //        data["Date of Establishment"] = dr["DateOfEstablishment"]?.ToString();
        //        data["NHFS Contact Person"] = dr["ContactPerson"]?.ToString();
        //        data["Designation"] = dr["DeclarationDesignation"]?.ToString();
        //        data["Email ID"] = dr["EmailId"]?.ToString();
        //        data["Mobile Number"] = dr["MobileNo"]?.ToString();
        //        data["Office Telephone"] = dr["VerificationNo"]?.ToString();
        //        data["TAN Number"] = dr["TANNo"]?.ToString();
        //        data["Contact Person"] = dr["ContactPersonName"]?.ToString();
        //        // ===== Address Details =====
        //        data["Registered Address"] = dr["Raddress1"]?.ToString() + "," + dr["Raddress2"]?.ToString() + "," + dr["Raddress3"]?.ToString() + "," + dr["registeredOfficeCity"]?.ToString() + "," + dr["Rstate"]?.ToString() + "," + dr["Rcountry"]?.ToString() + "-" + dr["Rzipcode"]?.ToString();
        //        data["Billing Address"] = dr["Baddress1"]?.ToString() + "," + dr["Baddress2"]?.ToString() + "," + dr["Baddress3"]?.ToString() + "," + dr["businessBillingCity"]?.ToString() + "," + dr["Bstate"]?.ToString() + "," + dr["Bcountry"]?.ToString() + "-" + dr["Bzipcode"]?.ToString();
        //        data["Shipping Address"] = dr["Saddress1"]?.ToString() + "," + dr["Saddress2"]?.ToString() + "," + dr["Saddress3"]?.ToString() + "," + dr["Scity"]?.ToString() + "," + dr["Sstate"]?.ToString() + "," + dr["Scountry"]?.ToString() + "-" + dr["Szipcode"]?.ToString();
        //        data["Goods Return Address"] = dr["Gaddress1"]?.ToString() + "," + dr["Gaddress2"]?.ToString() + "," + dr["Gaddress3"]?.ToString() + "," + dr["Gcity"]?.ToString() + "," + dr["Gstate"]?.ToString() + "," + dr["Gcountry"]?.ToString() + "-" + dr["Gzipcode"]?.ToString();

        //        // ===== Financial / Bank =====
        //        data["Bank Name"] = dr["BankName"]?.ToString();
        //        data["Account Name"] = dr["AccountName"]?.ToString();
        //        data["Account Number"] = dr["AccountNumber"]?.ToString();
        //        data["IFSC Code"] = dr["IfscCode"]?.ToString();
        //        data["Branch Code"] = dr["BranchCode"]?.ToString();
        //        data["Bank Address"] = dr["BankAddress"]?.ToString();

        //        // ===== MSME / Other Info =====
        //        data["MSME Status"] = dr["MsmeRegistrationStatus"]?.ToString();
        //        data["MSME Number"] = dr["MSMENo"]?.ToString();
        //        data["Enterprise Type"] = dr["EnterpriseType"]?.ToString();
        //        string Remarks = conn.GetSingleValue("Call \"GetRemarks\"('" + gstNo + "')");
        //        data["Remarks"] = Remarks;

        //        data["date"] = DateTime.Now.ToString("yyyy-MM-dd");
        //        data["location"] = "TamilNadu";
        //        // ===== Commercial Details =====
        //        string Id = GetSingleValue($@"select * from ""{sDBName}"".""TEC_OLED"" where ""GstNo""='{gstNo}'");
        //        DataTable dt = ExecuteQueryForDataTable($@"Select * from ""{sDBName}"".""PaymentDetails"" where ""Id""='{Id} '");

        //       // writeLog("Getting Payment details", "Mail");
        //        if (dt.Rows.Count > 0)
        //        {
        //            DataRow dr1 = dt.Rows[0];
        //            data["Credit Days"] = dr1["CreditDays"]?.ToString();
        //            data["Discount"] = dr1["DisCount"]?.ToString();
        //            data["md0_with"] = dr1["MarkDownTax0"]?.ToString();
        //            data["md0_without"] = dr1["MarkDownWithoutTax0"]?.ToString();
        //            data["md3_with"] = dr1["MarkDownTax3"]?.ToString();
        //            data["md3_without"] = dr1["MarkDownWithoutTax3"]?.ToString();
        //            data["md5_with"] = dr1["MarkDownTax5"]?.ToString();
        //            data["md5_without"] = dr1["MarkDownWithoutTax5"]?.ToString();
        //            data["md18_with"] = dr1["MarkDownTax18"]?.ToString();
        //            data["md18_without"] = dr1["MarkDownWithoutTax18"]?.ToString();
        //        }
        //        // ===== Agency / Business Type =====
        //        data["Business Type"] = dr["BusinessType"]?.ToString();
        //        data["Agency Email"] = dr["AgencyEmail"]?.ToString();
        //        data["Agency Name"] = dr["AgencyName"]?.ToString();
        //        agentMail = dr["AgencyEmail"]?.ToString();
        //        // ===== Declaration =====
        //        data["Name"] = dr["DeclarationName"]?.ToString();
        //        data["Designation"] = dr["DeclarationDesignation"]?.ToString();
        //        data["Mobile No"] = dr["VerificationNo"]?.ToString();
        //        data["Code"] = CardCode;
        //        // ===== Major Goods / Related Tables =====
        //        goods = LoadGoodsByGST(gstNo);
        //        Session["MajorGoods"] = goods;

        //        // ===== Other related grids (optional) =====
        //        //AddGridToData(data, gvProjectDetails, "Business Location");
        //        //AddGridToData(data, gvPartners, "Partners/Proprietor/Director's / Business Head Detail (Provide at Least One Person Details)");
        //        //AddGridToData(data, gvOperationalContacts, "Primary Operational Contacts");
        //        //AddGridToData(data, gvMajorGoods, "Major goods and services Details With");

        //        //AddGridToData(data, gvMajorCustomers, "List of Major Customers");
        //        //AddGridToData(data, gvOtherInformation, "Other Information");

        //        // ===== Preview Redirect =====
        //        data1 = data as Dictionary<string, object>;
        //        //Session["PreviewData"] = JsonConvert.SerializeObject(data);
        //    }
        //    //var data1 = Session["PreviewData"] as Dictionary<string, object>;
        //    string htmlContent = GenerateVendorHtmlWithData(data1, goods);
        //    writeLog("Getting HtmlContent", "Mail");
        //    byte[] pdfBytes = ConvertHtmlToPdf(htmlContent);
        //    writeLog("Getting pdf", "Mail");
        //    if (!string.IsNullOrEmpty(sRejectType) && sRejectType == "REJECT")
        //    {
        //        try
        //        {
        //            DataTable dt = dBConnection.ExecuteQueryForDataTable("Call \"Mail_BOSY&SUBJECT\"('REJECT')");
        //            string body = "", subject = "", ccMails = "";

        //            foreach (DataRow row in dt.Rows)
        //            {
        //                body = row["Body"].ToString();
        //                subject = row["Subject"].ToString();
        //                if (dt.Columns.Contains("CCMail")) ccMails = row["CCMail"].ToString();
        //            }
        //            writeLog("REJECT Mail Started", "Mail");
        //            using (MailMessage mail = new MailMessage())
        //            {
        //                string frommail = ConfigurationManager.AppSettings["MAILID"];
        //                string username = ConfigurationManager.AppSettings["SMTPUSER"];
        //                string password = ConfigurationManager.AppSettings["SMTPPWD"];
        //                string server = ConfigurationManager.AppSettings["SMTPSERVER"];
        //                int port = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPORT"]);

        //                mail.From = new MailAddress(frommail);
        //                writeLog("To Mail :" + toMail, "Mail");
        //                writeLog("Agent Mail :" + agentMail, "Mail");
        //                writeLog("CC Mail :" + ccMails, "Mail");
        //                if (!string.IsNullOrWhiteSpace(toMail))
        //                    mail.To.Add(toMail.Trim());

        //                // AGENT MAIL
        //                if (!string.IsNullOrWhiteSpace(agentMail))
        //                    mail.To.Add(agentMail.Trim());

        //                if (!string.IsNullOrWhiteSpace(ccMails))
        //                {
        //                    foreach (var cc in ccMails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        //                        mail.CC.Add(cc.Trim());
        //                }

        //                mail.Subject = subject;
        //                body = body.Replace("{Vendor Name}", data1["Trade Name"].ToString());
        //                body = body.Replace("{Remarks}", Session["RejectRemarks"].ToString());
        //                mail.Body = body;
        //                mail.IsBodyHtml = true;

        //                using (MemoryStream ms = new MemoryStream(pdfBytes))
        //                {
        //                    mail.Attachments.Add(new System.Net.Mail.Attachment(ms, "VendorRegistrationForm.pdf", "application/pdf"));

        //                    using (SmtpClient smtp = new SmtpClient(server, port))
        //                    {
        //                        smtp.Credentials = new System.Net.NetworkCredential(username, password);
        //                        smtp.EnableSsl = true;
        //                        writeLog("Mail sending", "Mail");
        //                        smtp.Send(mail);
        //                        writeLog("Mail Ended", "Mail");
        //                    }
        //                }
        //            }

        //            ScriptManager.RegisterStartupScript(this, GetType(), "mailSuccess", "alert('Mail sent successfully!');", true);
        //        }
        //        catch (Exception ex)
        //        {
        //            writeLog("Error while sending mail : " + ex.Message, "Mail");
        //            ScriptManager.RegisterStartupScript(this, GetType(), "mailError", $"alert('Error sending mail: {ex.Message}');", true);
        //        }
        //    }
        //    else
        //    {
        //        try
        //        {
        //            DataTable dt = dBConnection.ExecuteQueryForDataTable("Call \"Mail_BOSY&SUBJECT\"('SAP')");
        //            string body = "", subject = "", ccMails = "";

        //            foreach (DataRow row in dt.Rows)
        //            {
        //                body = row["Body"].ToString();
        //                subject = row["Subject"].ToString();
        //                if (dt.Columns.Contains("CCMail")) ccMails = row["CCMail"].ToString();
        //            }
        //            writeLog("SAP Mail Started", "Mail");
        //            using (MailMessage mail = new MailMessage())
        //            {
        //                string frommail = ConfigurationManager.AppSettings["MAILID"];
        //                string username = ConfigurationManager.AppSettings["SMTPUSER"];
        //                string password = ConfigurationManager.AppSettings["SMTPPWD"];
        //                string server = ConfigurationManager.AppSettings["SMTPSERVER"];
        //                int port = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPORT"]);

        //                mail.From = new MailAddress(frommail);
        //                writeLog("To Mail :" + toMail, "Mail");
        //                writeLog("Agent Mail :" + agentMail, "Mail");
        //                writeLog("CC Mail :" + ccMails, "Mail");
        //                mail.To.Add(toMail);
        //                mail.To.Add(agentMail);
        //                if (!string.IsNullOrWhiteSpace(ccMails))
        //                {
        //                    foreach (var cc in ccMails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        //                        mail.CC.Add(cc.Trim());
        //                }

        //                mail.Subject = subject;
        //                body = body.Replace("{Vendor Name}", data1["Trade Name"].ToString());
        //                body = body.Replace("{Vendor Code}", data1["Code"].ToString());
        //                mail.Body = body;
        //                mail.IsBodyHtml = true;

        //                using (MemoryStream ms = new MemoryStream(pdfBytes))
        //                {
        //                    mail.Attachments.Add(new System.Net.Mail.Attachment(ms, "VendorRegistrationForm.pdf", "application/pdf"));

        //                    using (SmtpClient smtp = new SmtpClient(server, port))
        //                    {
        //                        smtp.Credentials = new System.Net.NetworkCredential(username, password);
        //                        smtp.EnableSsl = true;
        //                        writeLog("Mail sending", "Mail");
        //                        smtp.Send(mail);
        //                        writeLog("Mail Ended", "Mail");
        //                    }
        //                }
        //            }

        //            ScriptManager.RegisterStartupScript(this, GetType(), "mailSuccess", "alert('Mail sent successfully!');", true);
        //        }
        //        catch (Exception ex)
        //        {
        //            writeLog("Error while sending mail : " + ex.Message, "Mail");
        //            ScriptManager.RegisterStartupScript(this, GetType(), "mailError", $"alert('Error sending mail: {ex.Message}');", true);
        //        }
        //    }
        //}


        public string JsonStringToDataTable(string jsonString, string strFun)
        {
            try
            {
                DataTable dt = new DataTable();
                var trgArray = new JArray();
                var cleanRow1 = new JObject();
                var cleanRow = new JObject();
                var Rows = new JObject();
                var js = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonString);
                var jsonLinq = JObject.Parse(jsonString);
                string Cnd = string.Empty;
                string errval = string.Empty;
                string DocEntry = string.Empty;
                string status = string.Empty;
                string message = string.Empty;
                string DocNum = string.Empty;
                string CardCode = string.Empty;
                string CardName = string.Empty;
                string DocumentEntry = string.Empty;
                string DocumentNumber = string.Empty;
                string ServiceCallId = string.Empty;
                var ssdf = Newtonsoft.Json.JsonConvert.SerializeObject(jsonLinq);

                foreach (var lin in jsonLinq)
                {
                    if (lin.Key == "error")
                    {
                        Rows.Add(lin.Key, lin.Key);
                        var srcArray = jsonLinq.Descendants().Where(d => d is JObject).First();
                        //errval = "100000027";
                        var errorCode = srcArray.ToList().First().ToList()[0].ToString();
                        if (errorCode == "100000027")
                        {
                            errval = errorCode;
                            break;
                        }


                        foreach (JObject row in srcArray.Last())
                        {
                            cleanRow = new JObject();
                            foreach (JProperty column in row.Properties())
                            {
                                // Only include JValue types
                                if (column.Value is JValue)
                                {
                                    if (column.Name == "value")
                                    {
                                        if (column.Value.ToString() == "Fail to get DB Credentials from SLD")
                                        {
                                            errval = "100000027";
                                        }
                                        else
                                        {
                                            cleanRow.Add(column.Name, column.Value);
                                            errval = Convert.ToString(column.Value);
                                        }

                                    }

                                }
                            }
                        }
                    }
                    else
                    {





                        if (lin.Key == "DocEntry")
                        {
                            DocEntry = lin.Value.ToString();
                        }
                        else if (lin.Key == "DocNum")
                        {
                            DocNum = lin.Value.ToString();
                        }
                        else if (lin.Key == "Code")
                        {
                            DocEntry = lin.Value.ToString();
                        }
                        else if (lin.Key == "CardCode")
                        {
                            CardCode = lin.Value.ToString();
                        }
                        else if (lin.Key == "CardName")
                        {
                            CardName = lin.Value.ToString();
                        }
                        else if (lin.Key == "AbsEntry")
                        {
                            DocEntry = lin.Value.ToString();
                        }
                        else if (lin.Key == "DepositNumber")
                        {
                            DocNum = lin.Value.ToString();
                        }
                        else if (lin.Key == "ReconNum")
                        {
                            DocNum = lin.Value.ToString();
                        }
                        else if (lin.Key == "DocumentEntry")
                        {
                            DocEntry = lin.Value.ToString();
                        }
                        else if (lin.Key == "DocumentNumber")
                        {
                            DocNum = lin.Value.ToString();
                        }
                        else if (lin.Key == "ServiceCallID")
                        {
                            ServiceCallId = lin.Value.ToString();
                        }
                        else if (lin.Key == "AbsoluteEntry")
                        {
                            DocEntry = lin.Value.ToString();
                        }
                        else if (lin.Key == "Status")
                        {
                            status = lin.Value.ToString();
                        }
                        else if (lin.Key == "Message")
                        {
                            message = lin.Value.ToString();
                        }
                        if (strFun == "Login")
                        {
                            errval = "Company Connected";
                            break;
                        }
                        else if (strFun == "BPMasterCreation")
                        {
                            if (CardCode != "" && CardName != "")
                            {
                                errval = CardCode + "#" + CardName + "#" + "Customer created successfully";
                                break;
                            }

                        }
                        else if (strFun == "ServiceCalls")
                        {
                            if (ServiceCallId != "" && DocNum != "")
                            {
                                errval = ServiceCallId + "#" + DocNum + "#" + "Service Call created Sucessfully";
                                break;
                            }

                        }
                        else if (strFun == "Attachments2")
                        {
                            if (DocEntry != "")
                            {
                                errval = DocEntry + "#" + "Added attachments successfully";
                                break;
                            }

                        }
                        else if (strFun == "DownPayments")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "DownPayments created successfully";
                                break;
                            }
                        }
                        else if (strFun == "SalesInvoice")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "ARInvoice created successfully";
                                break;
                            }
                        }
                        else if (strFun == "SalesReturn")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "SalesReturn created successfully";
                                break;
                            }
                        }
                        else if (strFun == "Incoming")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Payment created successfully";
                                break;
                            }
                            //if (DocNum != "")
                            //{
                            //    errval = DocNum + "#" + "Payment created successfully";
                            //    break;
                            //}
                        }
                        else if (strFun == "SalesOrder")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "SalesOrder Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "SalesReturn")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "SalesReturn Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "GRPO")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "GRPO created successfully";
                                break;
                            }
                        }
                        else if (strFun == "StockTransferRequest")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Stocktransferrequest created successfully";
                                break;
                            }
                        }
                        else if (strFun == "Stocktransfer")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Stocktransfer created successfully";
                                break;
                            }
                        }
                        else if (strFun == "OutgoingPayment")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "OutgoingPayment created Sucessfully";
                                break;
                            }
                        }
                        else if (strFun == "Deposit")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Deposit created Sucessfully";
                                break;
                            }
                        }
                        else if (strFun == "InternalReconciliations")
                        {
                            errval = "Reconciliation(s) done successfully";
                            break;

                        }
                        else if (strFun == "InventoryCountings")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "InventoryCountings created Sucessfully";
                                break;
                            }

                        }
                        else if (strFun == "ODEF")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Defective Document Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "GIS_OSPN")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Shipping Note Document Created successfully";
                                break;
                            }
                        }
                        //DENOMINATION
                        else if (strFun == "DENOMINATION")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "Denomination Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "PurchaseReturns")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "PurchaseReturns Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "GIS_IDFC")
                        {
                            if (DocEntry != "" && DocNum != "")
                            {
                                errval = DocEntry + "#" + DocNum + "#" + "IDFC Indagration Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "U_GIS_ADVPAY")
                        {
                            if (DocEntry != "")
                            {
                                errval = DocEntry + "#" + "Advance Payment Created successfully";
                                break;
                            }
                        }
                        else if (strFun == "GSTN")
                        {
                            if (status == "False")
                            {
                                if (message != "")
                                {
                                    errval = message;
                                    break;
                                }

                            }
                            else
                            {
                                errval = "GSTN Is Valid";
                                break;
                            }


                        }


                    }
                }
                return errval;
            }
            catch
            {
                throw;
            }
        }


        //public string Login(string URL, string CompanyDB, string UserName, string Password, out string strRouteVal)
        //{
        //    string str_Response = string.Empty;
        //    string ResponseMessage = string.Empty;

        //    try
        //    {
        //        string strFun = "Login";
        //        string sURL = URL + strFun;
        //        string json = "{\"CompanyDB\": \"" + CompanyDB + "\", \"UserName\": \"" + UserName + "\", \"Password\": \"" + Password + "\"}";
        //        var client = new RestClient(sURL);
        //        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        //        var request = new RestRequest("", Method.Post);
        //        //request.AddHeader("cache-control", "no-cache");
        //        request.AddHeader("content-type", "application/json");
        //        request.AddParameter("application/json", json, ParameterType.RequestBody);
        //        RestResponse response = client.Execute(request);
        //        dynamic value = JsonConvert.DeserializeObject(response.Content);
        //        value = (value == null) ? null : value.ToString();
        //        if (value != null)
        //        {
        //            str_Response = JsonStringToDataTable(value, strFun);
        //        }
        //        CookieContainer cookie = new CookieContainer();
        //        var cookie_1 = response.Cookies.FirstOrDefault();
        //        var cookie_2 = response.Cookies.LastOrDefault();
        //        CN1 = cookie_1.Name;
        //        CN2 = cookie_2.Name;
        //        CV1 = cookie_1.Value;
        //        CV2 = cookie_2.Value;
        //        strRouteVal = CV2;
        //        if (str_Response == "Company Connected")
        //        {
        //            ResponseMessage = CV1;
        //        }
        //        else
        //        {
        //            ResponseMessage = str_Response;
        //        }

        //        //string sLogout = Logout(URL, CompanyDB, UserName, Password);
        //        return ResponseMessage;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public string Login(
     string URL,
     string CompanyDB,
     string UserName,
     string Password,
     out string strRouteVal)
        {
            string str_Response = string.Empty;
            string ResponseMessage = string.Empty;

            strRouteVal = string.Empty;

            try
            {
                // =====================================================
                // 1. Clean input
                // =====================================================

                CompanyDB = CompanyDB?.Trim();
                UserName = UserName?.Trim();

                // Do NOT Trim() password unless you are 100% sure
                // spaces are not part of the password.

                if (string.IsNullOrWhiteSpace(URL))
                    throw new Exception("SAP Service Layer URL is empty.");

                if (string.IsNullOrWhiteSpace(CompanyDB))
                    throw new Exception("CompanyDB is empty.");

                if (string.IsNullOrWhiteSpace(UserName))
                    throw new Exception("UserName is empty.");

                if (string.IsNullOrEmpty(Password))
                    throw new Exception("Password is empty.");


                // =====================================================
                // 2. Build SAP Login URL
                // =====================================================

                string sURL = $"{URL.TrimEnd('/')}/Login";


                Console.WriteLine("========================================");
                Console.WriteLine("SAP LOGIN REQUEST");
                Console.WriteLine("URL       : " + sURL);
                Console.WriteLine("CompanyDB : [" + CompanyDB + "]");
                Console.WriteLine("UserName  : [" + UserName + "]");
                Console.WriteLine("Password Length : " + Password.Length);
                Console.WriteLine("========================================");


                // =====================================================
                // 3. RestSharp Client
                // =====================================================

                var options = new RestClientOptions(sURL)
                {
                    // DEVELOPMENT ONLY
                    // Do NOT use this in production.
                    RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true
                };

                var client = new RestClient(options);


                // =====================================================
                // 4. Create POST request
                // =====================================================

                var request = new RestRequest("", Method.Post);


                // =====================================================
                // 5. Create SAP Login JSON
                // =====================================================

                var loginRequest = new
                {
                    CompanyDB = CompanyDB,
                    UserName = UserName,
                    Password = Password
                };


                // =====================================================
                // 6. Serialize JSON
                // =====================================================

                string json = JsonConvert.SerializeObject(loginRequest);


                // Safe logging
                Console.WriteLine("========== LOGIN BODY ==========");

                Console.WriteLine(
                    JsonConvert.SerializeObject(
                        new
                        {
                            CompanyDB = CompanyDB,
                            UserName = UserName,
                            Password = "***"
                        }
                    )
                );

                Console.WriteLine("================================");


                // =====================================================
                // 7. Add headers
                // =====================================================

                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");


                // =====================================================
                // 8. Add JSON body
                // =====================================================

                request.AddStringBody(json, ContentType.Json);


                // =====================================================
                // 9. Execute SAP Login
                // =====================================================

                RestResponse response = client.Execute(request);


                // =====================================================
                // 10. Log SAP Response
                // =====================================================

                Console.WriteLine("========== SAP RESPONSE ==========");
                Console.WriteLine("Status Code    : " + response.StatusCode);
                Console.WriteLine("Is Successful  : " + response.IsSuccessful);
                Console.WriteLine("Error Message  : " + response.ErrorMessage);
                Console.WriteLine("Response       : " + response.Content);
                Console.WriteLine("==================================");


                // =====================================================
                // 11. Check HTTP response
                // =====================================================

                if (!response.IsSuccessful)
                {
                    throw new Exception(
                        $"SAP Login failed. " +
                        $"StatusCode: {response.StatusCode}, " +
                        $"Error: {response.ErrorMessage}, " +
                        $"Response: {response.Content}"
                    );
                }


                // =====================================================
                // 12. Check response content
                // =====================================================

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    throw new Exception(
                        "SAP Login returned an empty response."
                    );
                }


                // =====================================================
                // 13. Deserialize SAP response
                // =====================================================

                dynamic value;

                try
                {
                    value = JsonConvert.DeserializeObject(
                        response.Content
                    );
                }
                catch (JsonException jsonEx)
                {
                    throw new Exception(
                        "SAP returned invalid JSON. " +
                        "Response: " + response.Content,
                        jsonEx
                    );
                }


                // =====================================================
                // 14. Convert SAP response
                // =====================================================

                if (value != null)
                {
                    string jsonValue = value.ToString();

                    str_Response = JsonStringToDataTable(
                        jsonValue,
                        "Login"
                    );
                }


                // =====================================================
                // 15. Get B1SESSION cookie
                // =====================================================

                var sessionCookie = response.Cookies?
                    .FirstOrDefault(x =>
                        x.Name.Equals(
                            "B1SESSION",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );


                if (sessionCookie == null)
                {
                    throw new Exception(
                        "SAP Login succeeded but B1SESSION cookie was not returned."
                    );
                }


                // =====================================================
                // 16. Store B1SESSION
                // =====================================================

                CN1 = sessionCookie.Name;
                CV1 = sessionCookie.Value;


                // =====================================================
                // 17. Get ROUTEID cookie
                // =====================================================

                var routeCookie = response.Cookies?
                    .FirstOrDefault(x =>
                        x.Name.Equals(
                            "ROUTEID",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );


                // =====================================================
                // 18. Store ROUTEID
                // =====================================================

                if (routeCookie != null)
                {
                    CN2 = routeCookie.Name;
                    CV2 = routeCookie.Value;

                    strRouteVal = routeCookie.Value;
                }
                else
                {
                    CN2 = string.Empty;
                    CV2 = string.Empty;
                    strRouteVal = string.Empty;
                }


                // =====================================================
                // 19. Final response
                // =====================================================

                if (str_Response == "Company Connected")
                {
                    ResponseMessage = CV1;
                }
                else
                {
                    ResponseMessage = str_Response;
                }


                // =====================================================
                // 20. Success log
                // =====================================================

                Console.WriteLine("========== SAP LOGIN SUCCESS ==========");

                // Don't log session IDs in production.
                Console.WriteLine("B1SESSION received : " +
                    (!string.IsNullOrEmpty(CV1)));

                Console.WriteLine("ROUTEID received   : " +
                    (!string.IsNullOrEmpty(strRouteVal)));

                Console.WriteLine("Response           : " + ResponseMessage);

                Console.WriteLine("========================================");


                return ResponseMessage;
            }
            catch (Exception ex)
            {
                strRouteVal = string.Empty;

                Console.WriteLine("========== SAP LOGIN ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=====================================");

                throw;
            }
        }
        public string TransactionPosting(
    string URL,
    string MasterData,
    string str_SessionID,
    string TransactionType,
    string strRoutevalue,
    string strCompDB)
        {
            try
            {
                // =====================================================
                // 1. Validate input
                // =====================================================

                if (string.IsNullOrWhiteSpace(URL))
                    throw new Exception("SAP Service Layer URL is empty.");

                if (string.IsNullOrWhiteSpace(MasterData))
                    throw new Exception("MasterData is empty.");

                if (string.IsNullOrWhiteSpace(str_SessionID))
                    throw new Exception("SAP Session ID is empty.");

                // Keep session ID in your existing variable
                CV1 = str_SessionID;

                string strFun = TransactionType;

                // Make sure URL does not end with /
                string sURL = URL.TrimEnd('/');

                Console.WriteLine("========================================");
                Console.WriteLine("SAP TRANSACTION POSTING");
                Console.WriteLine("URL          : " + sURL);
                Console.WriteLine("Transaction  : " + TransactionType);
                Console.WriteLine("CompanyDB    : " + strCompDB);
                Console.WriteLine("Session      : " +
                    (!string.IsNullOrWhiteSpace(str_SessionID)));
                Console.WriteLine("ROUTEID      : " +
                    (!string.IsNullOrWhiteSpace(strRoutevalue)));
                Console.WriteLine("========================================");


                // =====================================================
                // 2. RestSharp client
                // =====================================================

                var options = new RestClientOptions(sURL)
                {
                    // DEVELOPMENT / INTERNAL TESTING ONLY
                    // Production should use a trusted SAP certificate.
                    RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true
                };

                var client = new RestClient(options);


                // =====================================================
                // 3. Create POST request
                // =====================================================

                var request = new RestRequest("", Method.Post);


                // =====================================================
                // 4. Headers
                // =====================================================

                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");


                // =====================================================
                // 5. SAP Service Layer cookies
                // =====================================================

                request.AddCookie(
                    "B1SESSION",
                    str_SessionID
                );

                if (!string.IsNullOrWhiteSpace(strRoutevalue))
                {
                    request.AddCookie(
                        "ROUTEID",
                        strRoutevalue
                    );
                }


                // =====================================================
                // 6. Add request body
                // =====================================================

                request.AddStringBody(
                    MasterData,
                    ContentType.Json
                );


                // =====================================================
                // 7. Execute request
                // =====================================================

                RestResponse response = client.Execute(request);


                // =====================================================
                // 8. Log response
                // =====================================================

                Console.WriteLine("========== SAP RESPONSE ==========");
                Console.WriteLine("Status Code   : " + response.StatusCode);
                Console.WriteLine("Is Successful : " + response.IsSuccessful);
                Console.WriteLine("Error Message : " + response.ErrorMessage);
                Console.WriteLine("Response      : " + response.Content);
                Console.WriteLine("==================================");


                // =====================================================
                // 9. Check HTTP error
                // =====================================================

                if (!response.IsSuccessful)
                {
                    throw new Exception(
                        $"SAP Transaction failed. " +
                        $"StatusCode: {response.StatusCode}, " +
                        $"Error: {response.ErrorMessage}, " +
                        $"Response: {response.Content}"
                    );
                }


                // =====================================================
                // 10. Check response content
                // =====================================================

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    throw new Exception(
                        "SAP returned an empty response."
                    );
                }


                // =====================================================
                // 11. Parse JSON
                // =====================================================

                JObject jsonResponse;

                try
                {
                    jsonResponse = JObject.Parse(response.Content);
                }
                catch (JsonException jsonEx)
                {
                    throw new Exception(
                        "SAP returned invalid JSON. " +
                        "Response: " + response.Content,
                        jsonEx
                    );
                }


                // =====================================================
                // 12. Check SAP error response
                // =====================================================

                if (jsonResponse["error"] != null)
                {
                    string sapError =
                        jsonResponse["error"]?["message"]?["value"]?.ToString();

                    if (string.IsNullOrWhiteSpace(sapError))
                    {
                        sapError = jsonResponse["error"]?.ToString();
                    }

                    throw new Exception(
                        "SAP Transaction Error: " + sapError
                    );
                }


                // =====================================================
                // 13. Get AbsoluteEntry
                // =====================================================

                JToken absoluteEntryToken =
                    jsonResponse["AbsoluteEntry"];


                if (absoluteEntryToken == null)
                {
                    throw new Exception(
                        "SAP transaction succeeded but AbsoluteEntry " +
                        "was not found in the response. " +
                        "Response: " + response.Content
                    );
                }


                // =====================================================
                // 14. Convert AbsoluteEntry
                // =====================================================

                if (!int.TryParse(
                        absoluteEntryToken.ToString(),
                        out int absoluteEntry))
                {
                    throw new Exception(
                        "Invalid AbsoluteEntry returned by SAP: " +
                        absoluteEntryToken
                    );
                }


                // =====================================================
                // 15. Success
                // =====================================================

                Console.WriteLine("========== SAP TRANSACTION SUCCESS ==========");
                Console.WriteLine("Transaction Type : " + TransactionType);
                Console.WriteLine("AbsoluteEntry    : " + absoluteEntry);
                Console.WriteLine("=============================================");


                return absoluteEntry.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== SAP TRANSACTION ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("============================================");

                // You can either throw the exception
                // so the controller receives the actual error,
                // or return the error string.

                throw;
            }
        }






        public async Task<ApiResponse> Approved(string Remarks,
            string UserName,
            string GstNumber
            )
        {

            try
            {
                string Department = string.Empty;


                string level = GetSingleValue($@"Select ""Level"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                Department = GetSingleValue($@"Select ""Department"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                string IsDepartment = GetSingleValue($@"Select ""ApprovedDepartment"" from  ""{sDBName}"".""ApprovalCheck"" where ""ApprovedDepartment""='{Department}'");
                if (IsDepartment != "" && IsDepartment != null)
                {
                    ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalCheck""  (""UserName"",""ApprovedDepartment"",""DepartmentApprovedCount"",""GSTNO"",""Level"",""Reason"") values('{UserName}','{Department} ','1','{GstNumber}','{level} ','{Remarks}')");
                }
                else
                {
                    ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalCheck""  (""UserName"",""ApprovedDepartment"",""DepartmentApprovedCount"",""GSTNO"",""Level"",""Reason"") values('{UserName}','{Department} ','1','{GstNumber}','{level} ','{Remarks}')");
                }

                ExecuteNonQuery($@"call ""{sDBName}"".""IsApproved""('{UserName}','{Department}','{GstNumber}')");



                ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalTrace""  (""User"",""GstNo"",""ApproveStatus"",""Level"") values('{UserName}','{GstNumber}','Y','{level}')");

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Approval Successfull",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {


                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = ex.Message,
                    Data = null
                };

            }
            finally
            {



            }

        }


        public async Task<ApiResponse> Reject(
            string Reason,
            string GstNumber,
            string UserName,
            string Status
            )
        {
            try
            {


                string level = GetSingleValue($@"Select ""Level"" from  ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                string Department = string.Empty;

                Department = GetSingleValue($@"Select ""Department"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");

                string query = $@"UPDATE ""{sDBName}"".""TEC_OLED"" SET ""Approval""='N', ""Draft""='Y',""RejectionStatus""='Y',""DraftApproved""='N',""RejectionReason""='{Reason}', ""RejectedUser""='{UserName}',""ApprovedDepartment"" = '{Department}' WHERE ""GstNo""='{GstNumber}'";

                ExecuteNonQuery(query);

                ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalTrace""  (""User"",""GstNo"",""ApproveStatus"",""RjectedReason"",""Level"",""ReApplySts"") values('{UserName}','{GstNumber}','N','{Reason}','{level}','{Status}')");

                string toMail = GetSingleValue($@"Select ""EmailId"" from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{GstNumber}'");

                if (!string.IsNullOrEmpty(toMail))
                {
                
               
                    //SentMail(toMail, gstNo, "");
                  
                }


                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Rejected Successfull",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {


                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = ex.Message,
                    Data = null
                };

            }
            finally
            {

            }

        }
    }

}


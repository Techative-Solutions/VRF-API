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
using DbConnection = VRF_API.Repository.DbConnection;
using Attachment = System.Net.Mail.Attachment;


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
        Task<bool> DraftApproved(
     string Draft,
     string GstNumber,
     string UserName);
        Task<ApiResponse> DraftApproved1(
          string GstNumber,
           string UserName

          );
    }


    public class RegistrationForm : IRegistrationForm
    {
        private readonly Repository.Log log;
        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly DbConnection db;
        private readonly string sConstr;
        private readonly SessionManager _sessionManager;
        public string mailTemplate = string.Empty;
        string CN1, CN2, CV1, CV2;
        public RegistrationForm(SessionManager sessionManager,IConfiguration configuration, OdbcConnection connection, Repository.Log _log, DbConnection _db)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            log = _log;
            db = _db;
            _sessionManager = sessionManager;
        }



        public async Task<List<GroupCode>> GroupCode(string UserName)
        {
            const string functionName = "GroupCode";
            log.WriteToLogFile_Debug(
              $"[{functionName}] [START] - GroupCode process started.",
              functionName
          );

            const string spName = "TEC_VRF_GETBPGROUPLIST";
            log.WriteToLogFile_Debug(
            $"[{functionName}] [REQUEST] - UserName received: {UserName}",
            functionName
        );

            string query = @$"CALL ""{sDBName}"".""{spName}"" (?)";

            log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
           functionName
       );


            List<GroupCode> RejDetailsList = new();
            log.WriteToLogFile_Debug(
         $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
         functionName
     );
            try
            {
                using var connection = new OdbcConnection(sConstr);
       
                connection.Open();
                log.WriteToLogFile_Debug(
           $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
           functionName
       );


                using var cmd = new OdbcCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserName", UserName);
                log.WriteToLogFile_Debug(
             $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
             functionName
         );

                using var reader = await cmd.ExecuteReaderAsync();
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
          functionName
      );
                int recordCount = 0;
                while (await reader.ReadAsync())
                {
                    var detail = new GroupCode
                    {
                        Code = reader["Code"] == DBNull.Value ? null : reader["Code"].ToString(),
                        Name = reader["Name"] == DBNull.Value ? null : reader["Name"].ToString(),


                      
                    };

                    RejDetailsList.Add(detail);
                    recordCount++;
                }

                log.WriteToLogFile_Debug(
            $"[{functionName}] [SUCCESS] - GroupCode data retrieved successfully. " +
            $"Total records loaded: {recordCount}",
            functionName
        );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - GroupCode process completed successfully.",
                    functionName
                );


                return RejDetailsList;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
               $"[{functionName}] [EXCEPTION] - Error while retrieving GroupCode data. " +
               $"UserName: {UserName} | " +
               $"Message: {ex.Message} | " +
               $"StackTrace: {ex.StackTrace}",
               functionName
           );

                return new List<GroupCode>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
          $"[{functionName}] [END] - GroupCode process execution ended.",
          functionName
      );
            }
        }
        public async Task<object> RejistrationDetails(
    string UserNme,
    string status)
        {
            const string functionName = "RejistrationDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Registration details process started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {UserNme}, Status: {status}",
                    functionName
                );

                using var connection = new OdbcConnection(sConstr);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
                    functionName
                );

                await connection.OpenAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
                    functionName
                );

                string requestStatus = status?.ToLower()?.Trim() ?? "";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [STATUS] - Processing status: {requestStatus}",
                    functionName
                );

                switch (requestStatus)
                {
                    case "approval":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Approval status selected. Calling GetApprovalDetails.",
                            functionName
                        );

                        var approvalResult = await GetApprovalDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Approval details retrieved successfully.",
                            functionName
                        );

                        return approvalResult;


                    case "draft":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Draft status selected. Calling GetDraftDetails.",
                            functionName
                        );

                        var draftResult = await GetDraftDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Draft details retrieved successfully.",
                            functionName
                        );

                        return draftResult;


                    case "pending":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Pending status selected. Calling GetPendingDetails.",
                            functionName
                        );

                        var pendingResult = await GetPendingDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Pending details retrieved successfully.",
                            functionName
                        );

                        return pendingResult;


                    case "completed":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Completed status selected. Calling GetCompletedDetails.",
                            functionName
                        );

                        var completedResult = await GetCompletedDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Completed details retrieved successfully.",
                            functionName
                        );

                        return completedResult;


                    case "rejected":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Rejected status selected. Calling GetRejectedDetails.",
                            functionName
                        );

                        var rejectedResult = await GetRejectedDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Rejected details retrieved successfully.",
                            functionName
                        );

                        return rejectedResult;


                    case "sap":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - SAP status selected. Calling GetSapDetails.",
                            functionName
                        );

                        var sapResult = await GetSapDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - SAP details retrieved successfully.",
                            functionName
                        );

                        return sapResult;


                    case "created":

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Created status selected. Calling GetCreatedVendorDetails.",
                            functionName
                        );

                        var createdResult = await GetCreatedVendorDetails(connection, UserNme);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SUCCESS] - Created vendor details retrieved successfully.",
                            functionName
                        );

                        return createdResult;


                    default:

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [VALIDATION_FAILED] - Invalid or unsupported status received: {requestStatus}",
                            functionName
                        );

                        return new List<object>();
                }
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving registration details. " +
                    $"UserName: {UserNme} | " +
                    $"Status: {status} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Registration details process ended.",
                    functionName
                );
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
            const string functionName = "GetPendingDetails";
            const string spName = "TEC_GetApprovalDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Pending details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<PendingResponse> list = new();

                int recordCount = 0;

                while (await reader.ReadAsync())
                {
                    list.Add(new PendingResponse
                    {
                        TradeName = GetString(reader, "TName"),

                        BusinessState = GetString(reader, "Bstate"),

                        NatureOfBusiness = GetString(
                            reader,
                            "NatureOfBusinessActivity"),

                        GstNumber = GetString(
                            reader,
                            "GstNo"),

                        AppliedDate = GetDate(
                            reader,
                            "DateOfEstablishment")
                    });

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Pending details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving pending details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<PendingResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Pending details retrieval ended.",
                    functionName
                );
            }
        }
        private async Task<List<ApprovalResponse>> GetApprovalDetails(
         OdbcConnection connection,
         string userName)
        {
            const string functionName = "GetApprovalDetails";
            const string spName = "TEC_GetApprovalWaitingDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Approval details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<ApprovalResponse> list = new();

                int recordCount = 0;

                while (await reader.ReadAsync())
                {
                    list.Add(new ApprovalResponse
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

                        WaitingorApproval = GetString(
                            reader,
                            "ApprovalWaiting"),

                        DepartmentLevel = GetString(
                            reader,
                            "Level")
                    });

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Approval details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving approval details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<ApprovalResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Approval details retrieval ended.",
                    functionName
                );
            }
        }

        private async Task<List<CompletedResponse>> GetCompletedDetails(
      OdbcConnection connection,
      string userName)
        {
            const string functionName = "GetCompletedDetails";
            const string spName = "TEC_GetAprrovedDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Completed details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<CompletedResponse> list = new();

                int recordCount = 0;

                while (await reader.ReadAsync())
                {
                    list.Add(new CompletedResponse
                    {
                        TradeName = GetString(
                            reader,
                            "TName"),

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

                        ApprovedDate = GetDate(
                            reader,
                            "ApprovedDate")
                    });

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Completed details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving completed details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<CompletedResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Completed details retrieval ended.",
                    functionName
                );
            }
        }

        private async Task<List<DraftResponse>> GetDraftDetails(
    OdbcConnection connection,
    string userName)
        {
            const string functionName = "GetDraftDetails";
            const string spName = "TEC_GetDraftDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Draft details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<DraftResponse> list = new();

                int recordCount = 0;

                while (await reader.ReadAsync())
                {
                    list.Add(new DraftResponse
                    {
                        TradeName = GetString(
                            reader,
                            "TName"),

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
                            "DateOfEstablishment")
                    });

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Draft details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving draft details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<DraftResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Draft details retrieval ended.",
                    functionName
                );
            }
        }

        private async Task<List<RejectedResponse>> GetRejectedDetails(
      OdbcConnection connection,
      string userName)
        {
            const string functionName = "GetRejectedDetails";
            const string spName = "TEC_GetRejectedDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Rejected details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<RejectedResponse> list = new();

                int recordCount = 0;

                while (await reader.ReadAsync())
                {
                    list.Add(new RejectedResponse
                    {
                        TradeName = GetString(
                            reader,
                            "TName"),

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

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Rejected details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving rejected details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<RejectedResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Rejected details retrieval ended.",
                    functionName
                );
            }
        }

        private async Task<List<SapResponse>> GetSapDetails(
      OdbcConnection connection,
      string userName)
        {
            const string functionName = "GetSapDetails";
            const string spName = "TEC_GetSapPostDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - SAP details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<SapResponse> list = new();

                int recordCount = 0;

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

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - SAP details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving SAP details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<SapResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - SAP details retrieval ended.",
                    functionName
                );
            }
        }
        private async Task<List<CreatedVendorResponse>>
            GetCreatedVendorDetails(
                OdbcConnection connection,
                string userName)
        {
            const string functionName = "GetCreatedVendorDetails";
            const string spName = "TEC_GetPostedVendors";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Created vendor details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - UserName: {userName}",
                    functionName
                );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" (?)";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing to execute stored procedure: {spName}",
                    functionName
                );

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue("@UserName", userName);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing stored procedure: {spName}",
                    functionName
                );

                using var reader = await cmd.ExecuteReaderAsync();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
                    functionName
                );

                List<CreatedVendorResponse> list = new();

                int recordCount = 0;

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

                    recordCount++;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Created vendor details retrieved successfully. " +
                    $"Total records loaded: {recordCount}",
                    functionName
                );

                return list;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving created vendor details. " +
                    $"UserName: {userName} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<CreatedVendorResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Created vendor details retrieval ended.",
                    functionName
                );
            }
        }

        public DataTable ExecuteQueryForDataTable(string sQuery)
        {
            String sFuncName = "HanaExecuteQueryReturnDataTable";
            log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [START] - Executing query for DataTable", sFuncName);
            OdbcConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            try
            {
                 log.WriteToLogFile_Debug("Starting the function", sFuncName);
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
                log.WriteToLogFile_Debug("Completed the function successfully", sFuncName);
                log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [DB_END] - Query executed successfully, filled " + dt.Rows.Count + " rows", sFuncName);
                return dt;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(ex.Message, sFuncName);
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
            const string functionName = "PushToSAP";
            Company oCompany = null;

            string Id = string.Empty;
            string CardCode = string.Empty;
            log.WriteToLogFile_Debug(
      $"[{functionName}] [START] - SAP vendor posting process started.",
      functionName
  );
            try
            {


                DataTable dataTable = null;
                DataTable dataTable1 = null;
                DataTable dataTable2 = null;
                string VendorCode = string.Empty;
                log.WriteToLogFile_Debug(
          $"[{functionName}] [REQUEST] - VendorName: {VendorName} | " +
          $"VendorType: {vendorType} | UserName: {UserName} | " +
          $"GroupCode: {GroupCode}",
          functionName
      );
                if (string.IsNullOrEmpty(GroupCode))
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [VALIDATION_FAILED] - Group Code is empty.",
                functionName
            );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Please select Group Code.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
           $"[{functionName}] [VALIDATION] - Group Code validation successful.",
           functionName
       );

                VendorCode += $"{vendorType}-{VendorName},";
                log.WriteToLogFile_Debug(
        $"[{functionName}] [DATABASE] - Fetching BP card details.",
        functionName
    );

                dataTable = ExecuteQueryForDataTable($@"call ""{sDBName}"".""BPDetails"" ('CARDDETAILS')");
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - BP card details retrieved successfully.",
         functionName
     );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching vendor creation details.",
                    functionName
                );

                dataTable1 = ExecuteQueryForDataTable($@"call ""{sDBName}"".""VendorCreation""('" + gstNumber + "','" + VendorCode + "')");
                log.WriteToLogFile_Debug(
            $"[{functionName}] [DATABASE] - Vendor creation details retrieved successfully. " +
            $"Total records: {dataTable1?.Rows.Count ?? 0}",
            functionName
        );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching SAP series details.",
                    functionName
                );


                dataTable2 = ExecuteQueryForDataTable($@"call ""{sDBName}"".""BPDetails"" ('SERIES')");
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - SAP series details retrieved successfully.",
          functionName
      );

                string DBName = _configuration["ApprovalPosting:DBName"];
                string DB = _configuration["ApprovalPosting:DBName1"];
                string user = DecryptFun(_configuration["ApprovalPosting:SAPUserName"]);
                string Pass = DecryptFun(_configuration["ApprovalPosting:SAPPassword"]);
                string TransURL = _configuration["ApprovalPosting:TransURL"];
                string loginURL = _configuration["ApprovalPosting:loginURL"];
                string StrRouteVal = "";

                log.WriteToLogFile_Debug(
      $"[{functionName}] [CONFIGURATION] - SAP configuration loaded successfully.",
      functionName
  );

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
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SAP_CONNECTION] - Connecting to SAP Business One.",
           functionName
       );
                if (oCompany.Connect() != 0)
                {
                    string err = oCompany.GetLastErrorDescription();
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [SAP_CONNECTION_FAILED] - SAP connection failed. " +
               $"Error: {err}",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = err,
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                log.WriteToLogFile_Debug(
            $"[{functionName}] [SAP_CONNECTION] - Connected to SAP Business One successfully.",
            functionName
        );
                BusinessPartners oBusinessPartner = (BusinessPartners)oCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SAP_OBJECT] - Business Partner object created successfully.",
           functionName
       );

                for (int i = 0; i < dataTable1.Rows.Count; i++)
                {

                    Id = dataTable1.Rows[i]["Id"].ToString();
                    CardCode = dataTable1.Rows[i]["CardCode"].ToString();
                    log.WriteToLogFile_Debug(
                 $"[{functionName}] [VENDOR_START] - Processing vendor. " +
                 $"Id: {Id} | CardCode: {CardCode}",
                 functionName
             );

                    string tempFolderPath = GetSingleValue($@"select ""AttachPath"" from ""{DB}"".""OADP""");
                    if (!Directory.Exists(tempFolderPath))
                    {
                        Directory.CreateDirectory(tempFolderPath);
                        log.WriteToLogFile_Debug(
                  $"[{functionName}] [ATTACHMENT] - Attachment directory created: {tempFolderPath}",
                  functionName
              );
                    }

                    List<Attachments2_Lines> attachmentLines = new List<Attachments2_Lines>();

                    string count;
                    string Type = _configuration["ServerType"];
                    count = GetSingleValue($@"select Count(*) from ""{DBName}"".""TEC_LED7"" where ""Id""='" + dataTable1.Rows[i]["Id"].ToString() + "'");


                    int count1 = Convert.ToInt32(count);
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [ATTACHMENT] - Attachment records found: {count1} | Id: {Id}",
              functionName
          );

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
                                log.WriteToLogFile_Debug(
                          $"[{functionName}] [ATTACHMENT] - Source file not found. " +
                          $"Id: {Id} | LineId: {j}",
                          functionName
                      );
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
                            log.WriteToLogFile_Debug(
                      $"[{functionName}] [ATTACHMENT] - Attachment prepared. " +
                      $"Id: {Id} | LineId: {j}",
                      functionName
                  );
                        }
                    }
                    // After filling your attachmentLines list
                    AttachmentsWrapper wrapper = new AttachmentsWrapper
                    {
                        Attachments2_Lines = attachmentLines
                    };

                    string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [ATTACHMENT] - Total attachments prepared: " +
                        $"{attachmentLines.Count} | Id: {Id}",
                        functionName
                    );
                    string Discount = GetSingleValue($@"Select ifnull(""DisCount"",'0') from ""{DBName}"".""PaymentDetails"" where ""Id"" = '{dataTable1.Rows[i]["Id"].ToString()}'");
                     log.WriteToLogFile_Debug("Discount : " + Discount, "VendorCreation");
                    if (string.IsNullOrEmpty(Discount)) Discount = "0";
                    string CreditDays = GetSingleValue($@"Call ""{DBName}"".""TEC_GetCreditDaysDetails""('" + dataTable1.Rows[i]["Id"].ToString() + "')") != "" ? GetSingleValue($@"Call ""{DBName}"".""TEC_GetCreditDaysDetails""('" + dataTable1.Rows[i]["Id"].ToString() + "')") : "0";
                   log.WriteToLogFile_Debug("CreditDays : " + CreditDays, "VendorCreation");
                    string creditcode = GetSingleValue($@"Select Trim(""CreditDays"") from ""{DBName}"".""PaymentDetails"" where ""Id""='" + dataTable1.Rows[i]["Id"].ToString() + "'");
                    log.WriteToLogFile_Debug("CreditCode : " + creditcode, "VendorCreation");
                    string creditDaysNumber = new string(CreditDays.Where(char.IsDigit).ToArray());

                    // creditCode is already numeric, but still clean it just in case
                    string creditCodeNumber = new string(creditcode.Where(char.IsDigit).ToArray());

                    // Compare
                    string result = (creditDaysNumber == creditCodeNumber) ? "nodefault" : "default";

                    string GroupNum = GetSingleValue($@"call ""{DBName}"".""TEC_GetGroupNum"" ('" + CreditDays + "')") != "" ? GetSingleValue($@"call ""{DBName}"".""TEC_GetGroupNum"" ('" + CreditDays + "')") : "0";
                     log.WriteToLogFile_Debug("GroupNum : " + GroupNum, "VendorCreation");
                     log.WriteToLogFile_Debug("GroupCode : " + GroupCode, "VendorCreation");
                    string Remarks = GetSingleValue($@"Call ""{DBName}"".""GetRemarks""('" + dataTable1.Rows[i]["GstNo"].ToString() + "')");
                     log.WriteToLogFile_Debug("Remarks : " + Remarks, "VendorCreation");
                    string rDoc = "0";
                    log.WriteToLogFile_Debug(
           $"[{functionName}] [SAP_LOGIN] - Logging into SAP Service Layer.",
           functionName
       );

                    string sessionId = Login(TransURL, DB, user, Pass, out StrRouteVal);
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [SAP_LOGIN] - SAP Service Layer login completed.",
                functionName
            );
                    string strRoutevalue = "";
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [ATTACHMENT_POST] - Posting attachments to SAP. " +
              $"Id: {Id}",
              functionName
          );
                    string Result = TransactionPosting(TransURL + "Attachments2", json, sessionId, "Attachment", strRoutevalue, DB);
                      log.WriteToLogFile_Debug("Attachment Result : " + Result, "VendorCreation");
                    int AbsEntry = Convert.ToInt32(Result);
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [ATTACHMENT_POST] - Attachment posted successfully. " +
              $"AttachmentEntry: {AbsEntry} | Id: {Id}",
              functionName
          );

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

                    log.WriteToLogFile_Debug(
           $"[{functionName}] [VENDOR_OBJECT] - SAP Business Partner object populated. " +
           $"CardCode: {CardCode} | Id: {Id}",
           functionName
       );


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
                        log.WriteToLogFile_Debug(
               $"[{functionName}] [SAP_OBJECT] - Business Partner data prepared for SAP Add. " +
               $"CardCode: {CardCode} | Id: {Id}",
               functionName
           );
                        log.WriteToLogFile_Debug(
             $"[{functionName}] [SAP_POST] - Adding Business Partner to SAP. " +
             $"CardCode: {CardCode}",
             functionName
         );
                    }
                    if (oBusinessPartner.Add() != 0)
                    {
                        string err = oCompany.GetLastErrorDescription();
                        log.WriteToLogFile_Debug(
                   $"[{functionName}] [SAP_POST_FAILED] - Business Partner creation failed. " +
                   $"Id: {Id} | CardCode: {CardCode} | Error: {err}",
                   functionName
               );
                        string error = err.Replace("'", "") + "'||'Vendor-'||'" + dataTable1.Rows[i]["CardCode"].ToString();
                        string update = $@"update ""{sDBName}"".""TEC_OLED"" set ""SAPRejReason""='{error}' where ""Id""='{dataTable1.Rows[i]["Id"].ToString()}'";
                        log.WriteToLogFile_Debug(
                $"[{functionName}] [DATABASE] - SAP rejection reason updated. Id: {Id}",
                functionName
            );

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

                        log.WriteToLogFile_Debug("Posting Completed for the Traders" + dataTable1.Rows[i]["TName"].ToString() + "    Completed Successfully.", "SAP Posting");
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
                        log.WriteToLogFile_Debug(
              $"[{functionName}] [DATABASE] - Vendor SAP status updated successfully. " +
              $"Id: {Id}",
              functionName
          );
                        SentMail(toMail, gstNumber, dataTable1.Rows[i]["CardCode"].ToString());
                        log.WriteToLogFile_Debug(
              $"[{functionName}] [EMAIL] - Vendor creation email sent successfully. " +
              $"Id: {Id}",
              functionName
          );


                        ExecuteNonQuery($@"Insert into ""{sDBName}"".""Mail_Log"" (""GstNo"",""Type"",""ActionDate"") values('{dataTable1.Rows[i]["GstNo"].ToString()}','Created',Current_Date)");


                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [MAIL_LOG] - Mail log inserted successfully. " +
                            $"Id: {Id}",
                            functionName
                        );
                    }

                    if (!(string.IsNullOrEmpty(CardCode)))
                    {
                        log.WriteToLogFile_Debug(
                   $"[{functionName}] [SUCCESS] - Business Partner added successfully. " +
                   $"CardCode: {CardCode} | Id: {Id}",
                   functionName
               );
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

                log.WriteToLogFile_Debug(
   $"[{functionName}] [SUCCESS] - SAP vendor posting completed successfully.",
   functionName
);


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
              log.WriteToLogFile_Debug(
            $"[{functionName}] [EXCEPTION] - Error while posting vendor to SAP. " +
            $"Id: {Id} | CardCode: {CardCode} | " +
            $"Message: {ex.Message} | StackTrace: {ex.StackTrace}",
            functionName
        );

                try
                {
                    string update =
                        $@"update ""{sDBName}"".""TEC_OLED""
                   set ""SAPRejReason""='{ex.Message.Replace("'", "")}'
                   ||'-->Vendor-->'||'{CardCode}'
                   where ""Id""='{Id}'";

                    ExecuteNonQuery(update);

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE] - Exception details updated in TEC_OLED. " +
                        $"Id: {Id}",
                        functionName
                    );
                }
                catch (Exception updateEx)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE_UPDATE_EXCEPTION] - " +
                        $"Failed to update SAP rejection reason. " +
                        $"Id: {Id} | Message: {updateEx.Message}",
                        functionName
                    );
                }

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = ex.Message,
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };

            }
            finally
            {
             oCompany.Disconnect();
                try
                {
                    if (oCompany != null)
                    {
                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SAP_DISCONNECT] - Disconnecting from SAP Business One.",
                            functionName
                        );

                        if (oCompany.Connected)
                        {
                            oCompany.Disconnect();
                        }

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [SAP_DISCONNECT] - SAP Business One disconnected successfully.",
                            functionName
                        );
                    }
                }
                catch (Exception disconnectEx)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [SAP_DISCONNECT_EXCEPTION] - " +
                        $"Error while disconnecting from SAP. " +
                        $"Message: {disconnectEx.Message}",
                        functionName
                    );
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - SAP vendor posting process ended. " +
                    $"Id: {Id} | CardCode: {CardCode}",
                    functionName
                );
            }
        }
        private async Task<(bool Success, string Message)> SentMail(
      string toMail,
      string selectedGST,
      string cardCode)
        {
            const string functionName = "SentMail";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor mail process started.",
                functionName
            );
            try
            {
                string gstNo = selectedGST?.Trim() ?? "";
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Mail request received. " +
           $"CardCode: {cardCode} | ToMail: {toMail}",
           functionName
       );

                if (string.IsNullOrWhiteSpace(gstNo))
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - GST Number is required.",
               functionName
           );

                    return (false, "GST Number is required.");
                }
                log.WriteToLogFile_Debug(
           $"[{functionName}] [VALIDATION] - GST validation successful.",
           functionName
       );
                log.WriteToLogFile_Debug(
                    "SendVendorMail started",
                    "Mail"
                );

 
                DataTable ds = GetVendorDetails(gstNo);

                if (ds == null || ds.Rows.Count == 0)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - Vendor details not found.",
               functionName
           );
                    return (false, "Vendor details not found.");
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Vendor details retrieved successfully. " +
          $"Records: {ds.Rows.Count}",
          functionName
      );
                DataRow dr = ds.Rows[0];

                var data = new Dictionary<string, object>();

                data["GST Number"] =
                    dr["GstNo"]?.ToString() ?? "";

                data["PAN Number"] =
                    dr["PanNo"]?.ToString() ?? "";

                data["Trade Name"] =
                    dr["TName"]?.ToString() ?? "";

                data["Nature of Business"] =
                    dr["NatureOfBusinessActivity"]?.ToString() ?? "";

                data["Date of Establishment"] =
                    dr["DateOfEstablishment"]?.ToString() ?? "";

                data["NHFS Contact Person"] =
                    dr["ContactPerson"]?.ToString() ?? "";

                data["Designation"] =
                    dr["DeclarationDesignation"]?.ToString() ?? "";

                data["Email ID"] =
                    dr["EmailId"]?.ToString() ?? "";

                data["Mobile Number"] =
                    dr["MobileNo"]?.ToString() ?? "";

                data["Office Telephone"] =
                    dr["VerificationNo"]?.ToString() ?? "";

                data["TAN Number"] =
                    dr["TANNo"]?.ToString() ?? "";

                data["Contact Person"] =
                    dr["ContactPersonName"]?.ToString() ?? "";


                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATA] - Vendor basic details prepared.",
       functionName
   );

                // =====================================================
                // ADDRESS DETAILS
                // =====================================================

                data["Registered Address"] =
                    $"{dr["Raddress1"]}," +
                    $"{dr["Raddress2"]}," +
                    $"{dr["Raddress3"]}," +
                    $"{dr["registeredOfficeCity"]}," +
                    $"{dr["Rstate"]}," +
                    $"{dr["Rcountry"]}-" +
                    $"{dr["Rzipcode"]}";

                data["Billing Address"] =
                    $"{dr["Baddress1"]}," +
                    $"{dr["Baddress2"]}," +
                    $"{dr["Baddress3"]}," +
                    $"{dr["businessBillingCity"]}," +
                    $"{dr["Bstate"]}," +
                    $"{dr["Bcountry"]}-" +
                    $"{dr["Bzipcode"]}";

                data["Shipping Address"] =
                    $"{dr["Saddress1"]}," +
                    $"{dr["Saddress2"]}," +
                    $"{dr["Saddress3"]}," +
                    $"{dr["Scity"]}," +
                    $"{dr["Sstate"]}," +
                    $"{dr["Scountry"]}-" +
                    $"{dr["Szipcode"]}";

                data["Goods Return Address"] =
                    $"{dr["Gaddress1"]}," +
                    $"{dr["Gaddress2"]}," +
                    $"{dr["Gaddress3"]}," +
                    $"{dr["Gcity"]}," +
                    $"{dr["Gstate"]}," +
                    $"{dr["Gcountry"]}-" +
                    $"{dr["Gzipcode"]}";


                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATA] - Vendor address details prepared.",
       functionName
   );

                // =====================================================
                // BANK DETAILS
                // =====================================================

                data["Bank Name"] =
                    dr["BankName"]?.ToString() ?? "";

                data["Account Name"] =
                    dr["AccountName"]?.ToString() ?? "";

                data["Account Number"] =
                    dr["AccountNumber"]?.ToString() ?? "";

                data["IFSC Code"] =
                    dr["IfscCode"]?.ToString() ?? "";

                data["Branch Code"] =
                    dr["BranchCode"]?.ToString() ?? "";

                data["Bank Address"] =
                    dr["BankAddress"]?.ToString() ?? "";
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATA] - Bank details prepared.",
          functionName
      );
                // =====================================================
                // MSME
                // =====================================================

                data["MSME Status"] =
                    dr["MsmeRegistrationStatus"]?.ToString() ?? "";

                data["MSME Number"] =
                    dr["MSMENo"]?.ToString() ?? "";

                data["Enterprise Type"] =
                    dr["EnterpriseType"]?.ToString() ?? "";

                // =====================================================
                // REMARKS
                // =====================================================
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Fetching vendor remarks.",
         functionName
     );

                string remarks = db.GetSingleValue(
                    $@"Call ""{sDBName}"".""GetRemarks""('{gstNo}')"
                );

                data["Remarks"] = remarks ?? "";
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Vendor remarks retrieved successfully.",
           functionName
       );

                // =====================================================
                // DATE / LOCATION
                // =====================================================

                data["date"] =
                    DateTime.Now.ToString("yyyy-MM-dd");

                data["location"] = "TamilNadu";

                // =====================================================
                // PAYMENT DETAILS
                // =====================================================

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching payment details.",
                    functionName
                );

                string id = db.GetSingleValue(
                    $@"select * 
               from ""{sDBName}"".""TEC_OLED"" 
               where ""GstNo""='{gstNo}'"
                );

                DataTable paymentTable =
                    db.ExecuteQueryForDataTable(
                        $@"Select * 
                   from ""{sDBName}"".""PaymentDetails"" 
                   where ""Id""='{id}'"
                    );

                if (paymentTable != null &&
                    paymentTable.Rows.Count > 0)
                {
                    DataRow paymentRow =
                        paymentTable.Rows[0];

                    data["Credit Days"] =
                        paymentRow["CreditDays"]?.ToString() ?? "";

                    data["Discount"] =
                        paymentRow["DisCount"]?.ToString() ?? "";

                    data["md0_with"] =
                        paymentRow["MarkDownTax0"]?.ToString() ?? "";

                    data["md0_without"] =
                        paymentRow["MarkDownWithoutTax0"]?.ToString() ?? "";

                    data["md3_with"] =
                        paymentRow["MarkDownTax3"]?.ToString() ?? "";

                    data["md3_without"] =
                        paymentRow["MarkDownWithoutTax3"]?.ToString() ?? "";

                    data["md5_with"] =
                        paymentRow["MarkDownTax5"]?.ToString() ?? "";

                    data["md5_without"] =
                        paymentRow["MarkDownWithoutTax5"]?.ToString() ?? "";

                    data["md18_with"] =
                        paymentRow["MarkDownTax18"]?.ToString() ?? "";

                    data["md18_without"] =
                        paymentRow["MarkDownWithoutTax18"]?.ToString() ?? "";
                }
                log.WriteToLogFile_Debug(
                $"[{functionName}] [DATABASE] - Payment details retrieved successfully.",
                functionName
            );
                // =====================================================
                // BUSINESS / AGENCY
                // =====================================================

                data["Business Type"] =
                    dr["BusinessType"]?.ToString() ?? "";

                data["Agency Email"] =
                    dr["AgencyEmail"]?.ToString() ?? "";

                data["Agency Name"] =
                    dr["AgencyName"]?.ToString() ?? "";

                string agentMail =
                    dr["AgencyEmail"]?.ToString() ?? "";

                // =====================================================
                // DECLARATION
                // =====================================================

                data["Name"] =
                    dr["DeclarationName"]?.ToString() ?? "";

                data["Designation"] =
                    dr["DeclarationDesignation"]?.ToString() ?? "";

                data["Mobile No"] =
                    dr["VerificationNo"]?.ToString() ?? "";

                data["Code"] =
                    cardCode ?? "";
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Loading vendor goods details.",
         functionName
     );

                // =====================================================
                // MAJOR GOODS
                // =====================================================

                List<GoodItem> goods =
                    LoadGoodsByGST(gstNo);
                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATABASE] - Vendor goods details loaded successfully. " +
       $"Total items: {goods?.Count ?? 0}",
       functionName
   );

                // =====================================================
                // GENERATE HTML
                // =====================================================
                log.WriteToLogFile_Debug(
         $"[{functionName}] [HTML] - Generating vendor HTML.",
         functionName
     );
                string htmlContent =
                    db.GenerateVendorHtmlWithData(
                        data,
                        goods
                    );

                log.WriteToLogFile_Debug(
                    "Vendor HTML generated",
                    "Mail"
                );

                // =====================================================
                // CONVERT HTML TO PDF
                // =====================================================

                byte[] pdfBytes =
                    db.ConvertHtmlToPdf(htmlContent);

                log.WriteToLogFile_Debug(
                    "PDF generated",
                    "Mail"
                );

                // =====================================================
                // DETERMINE MAIL TYPE
                // =====================================================

                string templateType =
                    !string.IsNullOrWhiteSpace(mailTemplate) &&
                    mailTemplate.Trim().ToUpper() == "REJECT"
                        ? "REJECT"
                        : "SAP";
                log.WriteToLogFile_Debug(
            $"[{functionName}] [TEMPLATE] - Mail template type determined: {templateType}",
            functionName
        );
                // =====================================================
                // GET MAIL TEMPLATE
                // =====================================================
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Fetching mail template. " +
          $"TemplateType: {templateType}",
          functionName
      );
                DataTable mailTemplateTable =
                    db.ExecuteQueryForDataTable(
                        $@"Call ""{sDBName}"".""Mail_BOSY&SUBJECT_1""('{templateType}')"
                    );

                string body = "";
                string subject = "";
                string ccMails = "";

                if (mailTemplateTable != null &&
                    mailTemplateTable.Rows.Count > 0)
                {
                    DataRow row =
                        mailTemplateTable.Rows[0];

                    body =
                        row["Body"]?.ToString() ?? "";

                    subject =
                        row["Subject"]?.ToString() ?? "";

                    if (mailTemplateTable.Columns.Contains("CCMail"))
                    {
                        ccMails =
                            row["CCMail"]?.ToString() ?? "";
                    }
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [TEMPLATE] - Mail template retrieved successfully. " +
              $"TemplateType: {templateType}",
              functionName
          );
                }
                else
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [TEMPLATE_NOT_FOUND] - Mail template not found. " +
                $"TemplateType: {templateType}",
                functionName
            );

                    return (
                        false,
                        $"Mail template not found for {templateType}."
                    );
                }

                // =====================================================
                // REJECT / SAP BODY
                // =====================================================

                body = body.Replace(
                    "{Vendor Name}",
                    data["Trade Name"]?.ToString() ?? ""
                );

                if (templateType == "REJECT")
                {
                    // Old Web Forms:
                    // body = body.Replace(
                    //     "{Remarks}",
                    //     Session["RejectRemarks"].ToString()
                    // );

                    body = body.Replace(
                        "{Remarks}",
                        _sessionManager.Get("RejectRemarks") ?? ""
                    );
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [TEMPLATE] - Reject remarks replaced in mail body.",
              functionName
          );
                }
                else
                {
                    // Old Web Forms:
                    // body = body.Replace(
                    //     "{Vendor Code}",
                    //     data1["Code"].ToString()
                    // );

                    body = body.Replace(
                        "{Vendor Code}",
                        data["Code"]?.ToString() ?? ""
                    );
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [TEMPLATE] - Vendor code replaced in mail body.",
                functionName
            );
                }

                // =====================================================
                // SMTP SETTINGS
                // =====================================================

                string fromMail =
                    _configuration["MailSettings:MAILID"] ?? "";

                string username =
                    _configuration["MailSettings:SMTPUSER"] ?? "";

                string password =
                    _configuration["MailSettings:SMTPPWD"] ?? "";

                string server =
                    _configuration["MailSettings:SMTPSERVER"] ?? "";

                int port =
                    int.TryParse(
                        _configuration["MailSettings:SMTPPORT"],
                        out int smtpPort
                    )
                        ? smtpPort
                        : 25;
                log.WriteToLogFile_Debug(
        $"[{functionName}] [SMTP] - SMTP configuration loaded. " +
        $"Server: {server} | Port: {port}",
        functionName
    );
                // =====================================================
                // SEND MAIL
                // =====================================================

                using (MailMessage mail =
                    new MailMessage())
                {
                    mail.From =
                        new MailAddress(fromMail);
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [MAIL] - Preparing mail recipients. " +
                $"ToMail: {toMail} | AgentMail: {agentMail}",
                functionName
            );
                    // =================================================
                    // LOG MAIL DETAILS
                    // =================================================

                    log.WriteToLogFile_Debug(
                        "To Mail : " + toMail,
                        "Mail"
                    );

                    log.WriteToLogFile_Debug(
                        "Agent Mail : " + agentMail,
                        "Mail"
                    );

                    log.WriteToLogFile_Debug(
                        "CC Mail : " + ccMails,
                        "Mail"
                    );
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [MAIL] - Mail recipients configured successfully. " +
              $"ToCount: {mail.To.Count} | CCCount: {mail.CC.Count}",
              functionName
          );
                    // =================================================
                    // TO MAIL
                    // =================================================

                    if (!string.IsNullOrWhiteSpace(toMail))
                    {
                        mail.To.Add(
                            toMail.Trim()
                        );
                    }

                    // =================================================
                    // AGENT MAIL
                    // =================================================

                    if (!string.IsNullOrWhiteSpace(agentMail))
                    {
                        mail.To.Add(
                            agentMail.Trim()
                        );
                    }

                    // =================================================
                    // CC MAIL
                    // =================================================

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

                    // =================================================
                    // SUBJECT
                    // =================================================

                    mail.Subject = subject;

                    // =================================================
                    // BODY
                    // =================================================

                    mail.Body = body;

                    mail.IsBodyHtml = true;

                    // =================================================
                    // PDF ATTACHMENT
                    // =================================================

                    using (MemoryStream ms =
                        new MemoryStream(pdfBytes))
                    {
                        mail.Attachments.Add(
                            new Attachment(
                                ms,
                                "VendorRegistrationForm.pdf",
                                "application/pdf"
                            )
                        );
                        log.WriteToLogFile_Debug(
                 $"[{functionName}] [MAIL] - PDF attachment added successfully.",
                 functionName
             );

                        // =============================================
                        // SMTP
                        // =============================================

                        using (SmtpClient smtp =
                            new SmtpClient(
                                server,
                                port
                            ))
                        {
                            smtp.Credentials =
                                new NetworkCredential(
                                    username,
                                    password
                                );

                            smtp.EnableSsl = true;

                            log.WriteToLogFile_Debug(
                                $"{templateType} Mail sending",
                                "Mail"
                            );

                            await smtp.SendMailAsync(mail);

                            log.WriteToLogFile_Debug(
                                $"{templateType} Mail Ended",
                                "Mail"
                            );
                        }
                    }
                }

                // =====================================================
                // SUCCESS
                // =====================================================

                string successMessage =
             templateType == "REJECT"
                 ? "Reject mail sent successfully."
                 : "Mail sent successfully.";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - {successMessage} " +
                    $"CardCode: {cardCode}",
                    functionName
                );

                return (
                    true,
                    successMessage
                );
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [EXCEPTION] - Error while sending vendor mail. " +
            $"CardCode: {cardCode} | " +
            $"ToMail: {toMail} | " +
            $"Message: {ex.Message} | " +
            $"StackTrace: {ex.StackTrace}",
            functionName
        );

                return (
                    false,
                    "Error while sending mail: " +
                    ex.Message
                );
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Vendor mail process ended. " +
                    $"CardCode: {cardCode}",
                    functionName
                );
            }
        }
        private DataTable GetVendorDetails(string gstNo)
        {
            const string functionName = "GetVendorDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor details retrieval started.",
                functionName
            );

            try
            {
                string query =
                    $@"select * 
               from ""{sDBName}"".""TEC_OLED"" 
               where ""GstNo""='{gstNo}'";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - Fetching vendor details for GST Number.",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing vendor details query.",
                    functionName
                );

                DataTable dt = ExecuteQueryForDataTable(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE] - No vendor details found.",
                        functionName
                    );

                    return dt;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Vendor details retrieved successfully. " +
                    $"Records: {dt.Rows.Count}",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Vendor details retrieval completed successfully.",
                    functionName
                );

                return dt;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while retrieving vendor details. " +
                    $"Message: {ex.Message} | StackTrace: {ex.StackTrace}",
                    functionName
                );

                return null;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Vendor details retrieval ended.",
                    functionName
                );
            }
        }
        private List<GoodItem> LoadGoodsByGST(string gstNo)
        {
            const string functionName = "LoadGoodsByGST";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Loading goods details started.",
                functionName
            );

            try
            {
                List<GoodItem> goods = new List<GoodItem>();

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - GST Number received for goods retrieval.",
                    functionName
                );

                // Get Vendor ID
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching vendor ID from TEC_OLED.",
                    functionName
                );

                string Id = GetSingleValue(
                    $@"select ""Id""
               from ""{sDBName}"".""TEC_OLED""
               where ""GstNo""='{gstNo}'"
                );

                if (string.IsNullOrWhiteSpace(Id))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - Vendor ID not found for the provided GST Number.",
                        functionName
                    );

                    return goods;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Vendor ID retrieved successfully. " +
                    $"Vendor ID: {Id}",
                    functionName
                );

                // Get Goods Details
                string query =
                    $@"select * 
               from ""{sDBName}"".""TEC_LED4"" 
               where ""Id""='{Id}'";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching goods details from TEC_LED4.",
                    functionName
                );

                DataTable dt = ExecuteQueryForDataTable(query);

                if (dt == null || dt.Rows.Count == 0)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE] - No goods details found. " +
                        $"Vendor ID: {Id}",
                        functionName
                    );

                    return goods;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Goods details retrieved successfully. " +
                    $"Total records: {dt.Rows.Count}",
                    functionName
                );

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

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Goods details loaded successfully. " +
                    $"Total items: {goods.Count}",
                    functionName
                );

                return goods;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while loading goods details. " +
                    $"Message: {ex.Message} | StackTrace: {ex.StackTrace}",
                    functionName
                );

                return new List<GoodItem>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Loading goods details ended.",
                    functionName
                );
            }
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

        public string Login(
     string URL,
     string CompanyDB,
     string UserName,
     string Password,
     out string strRouteVal)
        {
            const string functionName = "Login";
            string str_Response = string.Empty;
            string ResponseMessage = string.Empty;

            strRouteVal = string.Empty;
            log.WriteToLogFile_Debug(
      $"[{functionName}] [START] - SAP Service Layer login process started.",
      functionName
  );
            try
            {
             
                CompanyDB = CompanyDB?.Trim();
                UserName = UserName?.Trim();

                log.WriteToLogFile_Debug(
            $"[{functionName}] [REQUEST] - SAP login request received. " +
            $"CompanyDB: {CompanyDB} | UserName: {UserName}",
            functionName
        );


                if (string.IsNullOrWhiteSpace(URL))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - SAP Service Layer URL is empty.",
                        functionName
                    );

                    throw new Exception("SAP Service Layer URL is empty.");
                }

                if (string.IsNullOrWhiteSpace(CompanyDB))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - CompanyDB is empty.",
                        functionName
                    );

                    throw new Exception("CompanyDB is empty.");
                }

                if (string.IsNullOrWhiteSpace(UserName))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - UserName is empty.",
                        functionName
                    );

                    throw new Exception("UserName is empty.");
                }

                if (string.IsNullOrEmpty(Password))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - Password is empty.",
                        functionName
                    );

                    throw new Exception("Password is empty.");
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [VALIDATION] - SAP login input validation successful.",
                    functionName
                );



                string sURL = $"{URL.TrimEnd('/')}/Login";
                log.WriteToLogFile_Debug(
         $"[{functionName}] [SAP_URL] - SAP login endpoint prepared. " +
         $"URL: {sURL}",
         functionName
     );


                Console.WriteLine("========================================");
                Console.WriteLine("SAP LOGIN REQUEST");
                Console.WriteLine("URL       : " + sURL);
                Console.WriteLine("CompanyDB : [" + CompanyDB + "]");
                Console.WriteLine("UserName  : [" + UserName + "]");
                Console.WriteLine("Password Length : " + Password.Length);
                Console.WriteLine("========================================");


                log.WriteToLogFile_Debug(
        $"[{functionName}] [HTTP_CLIENT] - Initializing SAP Service Layer client.",
        functionName
    );

                var options = new RestClientOptions(sURL)
                {
                    // DEVELOPMENT ONLY
                    // Do NOT use this in production.
                    RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true
                };

                var client = new RestClient(options);
                log.WriteToLogFile_Debug(
          $"[{functionName}] [HTTP_CLIENT] - SAP Service Layer client initialized successfully.",
          functionName
      );
               
                var request = new RestRequest("", Method.Post);

                log.WriteToLogFile_Debug(
         $"[{functionName}] [HTTP_REQUEST] - SAP login POST request created with JSON headers.",
         functionName
     );
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
                log.WriteToLogFile_Debug(
          $"[{functionName}] [REQUEST_BODY] - SAP login request body prepared. " +
          $"CompanyDB: {CompanyDB} | UserName: {UserName} | PasswordProvided: {!string.IsNullOrEmpty(Password)}",
          functionName
      );


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

                log.WriteToLogFile_Debug(
           $"[{functionName}] [HTTP_REQUEST] - SAP login POST request created with JSON headers.",
           functionName
       );

                // =====================================================
                // 8. Add JSON body
                // =====================================================

                request.AddStringBody(json, ContentType.Json);

               
                log.WriteToLogFile_Debug(
          $"[{functionName}] [SAP_LOGIN] - Sending login request to SAP Service Layer.",
          functionName
      );

                RestResponse response = client.Execute(request);

                log.WriteToLogFile_Debug(
       $"[{functionName}] [SAP_RESPONSE] - SAP login response received. " +
       $"StatusCode: {response.StatusCode} | " +
       $"IsSuccessful: {response.IsSuccessful} | " +
       $"HasError: {!string.IsNullOrWhiteSpace(response.ErrorMessage)} | " +
       $"ResponseLength: {response.Content?.Length ?? 0}",
       functionName
   );
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
                    log.WriteToLogFile_Debug(
           $"[{functionName}] [SAP_RESPONSE_ERROR] - SAP Service Layer returned an error. " +
           $"StatusCode: {response.StatusCode} | " +
           $"Error: {response.ErrorMessage}",
           functionName
       );
                    throw new Exception(
                        $"SAP Login failed. " +
                        $"StatusCode: {response.StatusCode}, " +
                        $"Error: {response.ErrorMessage}, " +
                        $"Response: {response.Content}"
                    );
                  
                }

                log.WriteToLogFile_Debug(
         $"[{functionName}] [SAP_LOGIN] - SAP login HTTP request completed successfully.",
         functionName
     );
                // =====================================================
                // 12. Check response content
                // =====================================================

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [SAP_RESPONSE] - SAP Login returned an empty response.",
               functionName
           );

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
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [JSON] - Deserializing SAP login response.",
               functionName
           );

                    value = JsonConvert.DeserializeObject(
                        response.Content
                    );
                }
                catch (JsonException jsonEx)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [JSON_ERROR] - SAP returned invalid JSON. " +
               $"Message: {jsonEx.Message}",
               functionName
           );

                    throw new Exception(
                        "SAP returned invalid JSON. " +
                        "Response: " + response.Content,
                        jsonEx
                    );
                }
                log.WriteToLogFile_Debug(
            $"[{functionName}] [JSON] - SAP login response deserialized successfully.",
            functionName
        );

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
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [RESPONSE_PROCESSING] - SAP login response processed successfully. " +
               $"ResponseStatus: {str_Response}",
               functionName
           );
                }

                log.WriteToLogFile_Debug(
          $"[{functionName}] [SESSION] - Checking SAP B1SESSION cookie.",
          functionName
      );

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
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [SESSION_FAILED] - SAP Login succeeded but B1SESSION cookie was not returned.",
              functionName
          );

                    throw new Exception(
                        "SAP Login succeeded but B1SESSION cookie was not returned."
                    );
                }


                // =====================================================
                // 16. Store B1SESSION
                // =====================================================

                CN1 = sessionCookie.Name;
                CV1 = sessionCookie.Value;

                log.WriteToLogFile_Debug(
        $"[{functionName}] [SESSION] - B1SESSION cookie received successfully.",
        functionName
    );

                // =====================================================
                // 17. Get ROUTEID cookie
                // =====================================================
                log.WriteToLogFile_Debug(
          $"[{functionName}] [ROUTE] - Checking SAP ROUTEID cookie.",
          functionName
      );

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
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [ROUTE] - ROUTEID cookie received successfully.",
                functionName
            );
                }
                else
                {
                    CN2 = string.Empty;
                    CV2 = string.Empty;
                    strRouteVal = string.Empty;
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [ROUTE] - ROUTEID cookie was not returned by SAP.",
                functionName
            );
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
                log.WriteToLogFile_Debug(
          $"[{functionName}] [SUCCESS] - SAP login completed successfully. " +
          $"B1SESSIONReceived: {!string.IsNullOrEmpty(CV1)} | " +
          $"ROUTEIDReceived: {!string.IsNullOrEmpty(strRouteVal)} | " +
          $"ResponseStatus: {str_Response}",
          functionName
      );

                return ResponseMessage;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [EXCEPTION] - Error during SAP Service Layer login. " +
           $"CompanyDB: {CompanyDB} | " +
           $"UserName: {UserName} | " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           functionName
       );
                strRouteVal = string.Empty;

                Console.WriteLine("========== SAP LOGIN ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=====================================");

                throw;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - SAP Service Layer login process ended.",
                    functionName
                );
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
            const string functionName = "TransactionPosting";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - SAP transaction posting process started.",
                functionName
            );
            try
            {
                log.WriteToLogFile_Debug(
              $"[{functionName}] [REQUEST] - Transaction request received. " +
              $"TransactionType: {TransactionType} | " +
              $"CompanyDB: {strCompDB} | " +
              $"SessionProvided: {!string.IsNullOrWhiteSpace(str_SessionID)} | " +
              $"RouteProvided: {!string.IsNullOrWhiteSpace(strRoutevalue)}",
              functionName
          );
                if (string.IsNullOrWhiteSpace(URL))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - SAP Service Layer URL is empty.",
                        functionName
                    );

                    throw new Exception("SAP Service Layer URL is empty.");
                }

                if (string.IsNullOrWhiteSpace(MasterData))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - MasterData is empty.",
                        functionName
                    );

                    throw new Exception("MasterData is empty.");
                }

                if (string.IsNullOrWhiteSpace(str_SessionID))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - SAP Session ID is empty.",
                        functionName
                    );

                    throw new Exception("SAP Session ID is empty.");
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [VALIDATION] - Transaction input validation successful.",
                    functionName
                );

                // Keep session ID in your existing variable
                CV1 = str_SessionID;

                string strFun = TransactionType;

                // Make sure URL does not end with /
                string sURL = URL.TrimEnd('/');
                log.WriteToLogFile_Debug(
         $"[{functionName}] [SAP_URL] - SAP transaction endpoint prepared. " +
         $"URL: {sURL}",
         functionName
     );

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

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [HTTP_CLIENT] - Initializing SAP Service Layer client.",
                    functionName
                );

                var options = new RestClientOptions(sURL)
                {
                    // DEVELOPMENT / INTERNAL TESTING ONLY
                    // Production should use a trusted SAP certificate.
                    RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true
                };

                var client = new RestClient(options);
                log.WriteToLogFile_Debug(
           $"[{functionName}] [HTTP_CLIENT] - SAP Service Layer client initialized successfully.",
           functionName
       );

                // =====================================================
                // 3. Create POST request
                // =====================================================

                var request = new RestRequest("", Method.Post);


                // =====================================================
                // 4. Headers
                // =====================================================

                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");
                log.WriteToLogFile_Debug(
          $"[{functionName}] [HTTP_REQUEST] - SAP POST request created with JSON headers.",
          functionName
      );


                // =====================================================
                // 5. SAP Service Layer cookies
                // =====================================================

                request.AddCookie(
                    "B1SESSION",
                    str_SessionID
                );
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SESSION] - B1SESSION cookie added to SAP request.",
           functionName
       );

                if (!string.IsNullOrWhiteSpace(strRoutevalue))
                {
                    request.AddCookie(
                        "ROUTEID",
                        strRoutevalue
                    );
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [ROUTE] - ROUTEID cookie added to SAP request.",
                functionName
            );
                }


                // =====================================================
                // 6. Add request body
                // =====================================================

                request.AddStringBody(
                    MasterData,
                    ContentType.Json
                );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST_BODY] - SAP transaction JSON body added successfully. " +
                    $"PayloadLength: {MasterData?.Length ?? 0}",
                    functionName
                );


                // =====================================================
                // 7. Execute request
                // =====================================================
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SAP_POST] - Sending SAP transaction request. " +
           $"TransactionType: {TransactionType}",
           functionName
       );

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
                log.WriteToLogFile_Debug(
        $"[{functionName}] [SAP_RESPONSE] - SAP transaction response received. " +
        $"StatusCode: {response.StatusCode} | " +
        $"IsSuccessful: {response.IsSuccessful} | " +
        $"HasError: {!string.IsNullOrWhiteSpace(response.ErrorMessage)} | " +
        $"ResponseLength: {response.Content?.Length ?? 0}",
        functionName
    );


                if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [SAP_RESPONSE_ERROR] - SAP Service Layer returned an HTTP/client error. " +
                        $"StatusCode: {response.StatusCode} | " +
                        $"Error: {response.ErrorMessage}",
                        functionName
                    );
                }

                if (!response.IsSuccessful)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [SAP_POST_FAILED] - SAP transaction request failed. " +
                        $"TransactionType: {TransactionType} | " +
                        $"StatusCode: {response.StatusCode} | " +
                        $"Error: {response.ErrorMessage}",
                        functionName
                    );

                    throw new Exception(
                        $"SAP Transaction failed. " +
                        $"StatusCode: {response.StatusCode}, " +
                        $"Error: {response.ErrorMessage}, " +
                        $"Response: {response.Content}"
                    );
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SAP_POST] - SAP transaction HTTP request completed successfully.",
                    functionName
                );

                // =====================================================
                // 10. Check response content
                // =====================================================

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [SAP_RESPONSE] - SAP returned an empty response.",
               functionName
           );
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
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [JSON] - Parsing SAP transaction response.",
              functionName
          );

                    jsonResponse = JObject.Parse(response.Content);
                }
                catch (JsonException jsonEx)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [JSON_ERROR] - SAP returned invalid JSON. " +
               $"Message: {jsonEx.Message}",
               functionName
           );

                    throw new Exception(
                        "SAP returned invalid JSON. " +
                        "Response: " + response.Content,
                        jsonEx
                    );
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [JSON] - SAP transaction response parsed successfully.",
          functionName
      );


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
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [SAP_ERROR] - SAP returned a transaction error. " +
              $"TransactionType: {TransactionType} | " +
              $"Error: {sapError}",
              functionName
          );

                    throw new Exception(
                        "SAP Transaction Error: " + sapError
                    );
                }


                // =====================================================
                // 13. Get AbsoluteEntry
                // =====================================================
                log.WriteToLogFile_Debug(
      $"[{functionName}] [RESPONSE_PROCESSING] - Checking SAP response for AbsoluteEntry.",
      functionName
  );
                JToken absoluteEntryToken =
                    jsonResponse["AbsoluteEntry"];


                if (absoluteEntryToken == null)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [RESPONSE_VALIDATION_FAILED] - " +
               $"SAP transaction succeeded but AbsoluteEntry was not found.",
               functionName
           );
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
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [RESPONSE_VALIDATION_FAILED] - " +
               $"Invalid AbsoluteEntry returned by SAP. " +
               $"Value: {absoluteEntryToken}",
               functionName
           );
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

                log.WriteToLogFile_Debug(
           $"[{functionName}] [SUCCESS] - SAP transaction posted successfully. " +
           $"TransactionType: {TransactionType} | " +
           $"AbsoluteEntry: {absoluteEntry}",
           functionName
       );
                return absoluteEntry.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== SAP TRANSACTION ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("============================================");

              log.WriteToLogFile_Debug(

           $"[{functionName}] [EXCEPTION] - Error during SAP transaction posting. " +
           $"TransactionType: {TransactionType} | " +
           $"CompanyDB: {strCompDB} | " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           functionName
       );

                throw;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - SAP transaction posting process ended. " +
                    $"TransactionType: {TransactionType}",
                    functionName
                );
            }
        }






        public async Task<ApiResponse> Approved(string Remarks,
            string UserName,
            string GstNumber
            )
        {
            const string functionName = "Approved";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor approval process started.",
                functionName
            );

            try
            {
                string Department = string.Empty;
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Approval request received. " +
           $"UserName: {UserName} | GST Number: {GstNumber}",
           functionName
       );

                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Fetching user approval level.",
          functionName
      );

                string level = GetSingleValue($@"Select ""Level"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - User approval level retrieved. " +
          $"Level: {level}",
          functionName
      );


                log.WriteToLogFile_Debug(
            $"[{functionName}] [DATABASE] - Fetching user department.",
            functionName
        );

                Department = GetSingleValue($@"Select ""Department"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - User department retrieved. " +
           $"Department: {Department}",
           functionName
       );


                string IsDepartment = GetSingleValue($@"Select ""ApprovedDepartment"" from  ""{sDBName}"".""ApprovalCheck"" where ""ApprovedDepartment""='{Department}'");

                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Checking ApprovalCheck for department.",
           functionName
       );
                bool departmentExists =
          !string.IsNullOrWhiteSpace(IsDepartment);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Approval department check completed. " +
                    $"DepartmentExists: {departmentExists}",
                    functionName
                );


                if (IsDepartment != "" && IsDepartment != null)
                {

                    ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalCheck""  (""UserName"",""ApprovedDepartment"",""DepartmentApprovedCount"",""GSTNO"",""Level"",""Reason"") values('{UserName}', '{Department.Trim()}','1','{GstNumber}','{level} ','{Remarks}')");
                }
                else
                {
                    ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalCheck""  (""UserName"",""ApprovedDepartment"",""DepartmentApprovedCount"",""GSTNO"",""Level"",""Reason"") values('{UserName}', '{Department.Trim()}','1','{GstNumber}','{level} ','{Remarks}')");
                }



                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing IsApproved procedure.",
                    functionName
                );

                ExecuteNonQuery($@"call ""{sDBName}"".""IsApproved""('{UserName}','{Department}','{GstNumber}')");
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - IsApproved procedure executed successfully.",
           functionName
       );

                log.WriteToLogFile_Debug(
            $"[{functionName}] [DATABASE] - Inserting approval trace.",
            functionName
        );
                ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalTrace""  (""User"",""GstNo"",""ApproveStatus"",""Level"") values('{UserName}','{GstNumber}','Y','{level}')");
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Approval trace inserted successfully.",
           functionName
       );

                log.WriteToLogFile_Debug(
           $"[{functionName}] [SUCCESS] - Vendor approval completed successfully. " +
           $"UserName: {UserName} | Level: {level}",
           functionName
       );

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
                log.WriteToLogFile_Debug(
           $"[{functionName}] [EXCEPTION] - Error during vendor approval. " +
           $"UserName: {UserName} | " +
           $"GST Number: {GstNumber} | " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           functionName
       );

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = ex.Message,
                    Data = null
                };

            }
            finally
            {

                log.WriteToLogFile_Debug(
            $"[{functionName}] [END] - Vendor approval process ended.",
            functionName
        );

            }

        }


        public async Task<ApiResponse> Reject(
            string Reason,
            string GstNumber,
            string UserName,
            string Status
            )
        {
            const string functionName = "Reject";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor rejection process started.",
                functionName
            );
            try
            {
                string Department = string.Empty;
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Rejection request received. " +
           $"UserName: {UserName} | GST Number: {GstNumber} | Status: {Status}",
           functionName
       );

                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Fetching user approval level.",
         functionName
     );
                string level = GetSingleValue($@"Select ""Level"" from  ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                log.WriteToLogFile_Debug(
              $"[{functionName}] [DATABASE] - User approval level retrieved. " +
              $"Level: {level}",
              functionName
          );
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Fetching user department.",
           functionName
       );

                Department = GetSingleValue($@"Select ""Department"" from ""{sDBName}"".""TEC_OUSR"" where ""User_Name""='{UserName}' or  ""User_Mail_Id"" = '{UserName}'");
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - User department retrieved. " +
         $"Department: {Department}",
         functionName
     );

                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Updating vendor rejection details in TEC_OLED.",
          functionName
      );


                string query = $@"UPDATE ""{sDBName}"".""TEC_OLED"" SET ""Approval""='N', ""Draft""='Y',""RejectionStatus""='Y',""DraftApproved""='N',""RejectionReason""='{Reason}', ""RejectedUser""='{UserName}',""ApprovedDepartment"" = '{Department}' WHERE ""GstNo""='{GstNumber}'";

                ExecuteNonQuery(query);
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Vendor rejection details updated successfully.",
          functionName
      );

                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Inserting rejection trace.",
         functionName
     );

                ExecuteNonQuery($@"insert into ""{sDBName}"".""ApprovalTrace""  (""User"",""GstNo"",""ApproveStatus"",""RjectedReason"",""Level"",""ReApplySts"") values('{UserName}','{GstNumber}','N','{Reason}','{level}','{Status}')");
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Rejection trace inserted successfully.",
          functionName
      );


                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Fetching vendor recipient email.",
           functionName
       );

                string toMail = GetSingleValue($@"Select ""EmailId"" from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{GstNumber}'");

                if (!string.IsNullOrEmpty(toMail))
                {
                    mailTemplate = "REJECT";
                    _sessionManager.Set("RejectRemarks", Reason);
                    SentMail(toMail, GstNumber, "");
                    mailTemplate = string.Empty;

                }
                bool mailAvailable = !string.IsNullOrWhiteSpace(toMail);

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [MAIL] - Recipient email lookup completed. " +
                    $"EmailAvailable: {mailAvailable}",
                    functionName
                );

                log.WriteToLogFile_Debug(
         $"[{functionName}] [SUCCESS] - Vendor rejection completed successfully. " +
         $"UserName: {UserName} | Level: {level}",
         functionName
     );

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

                log.WriteToLogFile_Debug(
         $"[{functionName}] [EXCEPTION] - Error during vendor rejection. " +
         $"UserName: {UserName} | " +
         $"GST Number: {GstNumber} | " +
         $"Status: {Status} | " +
         $"Message: {ex.Message} | " +
         $"StackTrace: {ex.StackTrace}",
         functionName
     );

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = ex.Message,
                    Data = null
                };

            }
            finally
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [END] - Vendor rejection process ended.",
            functionName
        );
            }

        }


        public async Task<bool> DraftApproved(
     string Draft,
     string GstNumber,
     string UserName)
        {
            const string functionName = "DraftApproved";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Draft approval validation process started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - Draft approval request received. " +
                    $"Draft: {Draft} | GST Provided: {!string.IsNullOrWhiteSpace(GstNumber)} | " +
                    $"UserName: {UserName}",
                    functionName
                );

                if (GstNumber != null)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION] - GST number is null. Proceeding with draft approval validation.",
                        functionName
                    );

                    if (Draft == "draft")
                    {
                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Draft status confirmed.",
                            functionName
                        );

                        string departmentQuery =
                            $@"Select ""Department"" 
                       from ""{sDBName}"".""TEC_OUSR"" 
                       where ""User_Name""='{UserName}' 
                       or ""User_Mail_Id""='{UserName}'";

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [DATABASE] - Fetching department for user.",
                            functionName
                        );

                        string Department = GetSingleValue(departmentQuery);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [DATABASE] - Department retrieved successfully. " +
                            $"Department: {Department}",
                            functionName
                        );

                        string approvalQuery =
                            $@"Call ""{sDBName}"".""IsApprovalReq""('{UserName}','{Department}')";

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [DATABASE] - Checking approval requirement.",
                            functionName
                        );

                        string IsApprovalReq = GetSingleValue(approvalQuery);

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [DATABASE] - Approval requirement check completed. " +
                            $"IsApprovalReq: {IsApprovalReq}",
                            functionName
                        );

                        if (IsApprovalReq == "Y")
                        {
                            log.WriteToLogFile_Debug(
                                $"[{functionName}] [SUCCESS] - Draft requires approval. Approval validation passed.",
                                functionName
                            );

                            return true;
                        }

                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [FLOW] - Draft does not require approval.",
                            functionName
                        );
                    }
                    else
                    {
                        log.WriteToLogFile_Debug(
                            $"[{functionName}] [VALIDATION_FAILED] - Draft value is not 'Draft'.",
                            functionName
                        );
                    }
                }
                else
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - GST number is already provided. " +
                        $"Draft approval validation skipped.",
                        functionName
                    );
                }

                return false;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while validating draft approval. " +
                    $"Draft: {Draft} | UserName: {UserName} | " +
                    $"Message: {ex.Message} | StackTrace: {ex.StackTrace}",
                    functionName
                );

                return false;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Draft approval validation process ended.",
                    functionName
                );
            }
        }


        public async Task<ApiResponse> DraftApproved1(
          string GstNumber,
          string UserName
       
          )
        {
            const string functionName = "DraftApproved1";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor rejection process started.",
                functionName
            );
            try
            {
               
      ExecuteNonQuery($@"Update ""{sDBName}"".""TEC_OLED"" set ""MerApproved""='Y',""DraftApprovedUser""='{UserName}',""DraftApproved""='Y' where ""GstNo""='{GstNumber}'");

                   string toMail = GetSingleValue($@"Select ""EmailId"" from ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{GstNumber}'");
                string agentMail =GetSingleValue($@"Select ""AgencyEmail"" from  ""{sDBName}"".""TEC_OLED"" where ""GstNo"" = '{GstNumber}'");
                SentMail1(toMail, agentMail, GstNumber, "");
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Approved Successfull",
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
                log.WriteToLogFile_Debug(
            $"[{functionName}] [END] - Vendor rejection process ended.",
            functionName
        );
            }

        }

        private async Task<(bool Success, string Message)> SentMail1(
  string toMail,
  string agentMail,
  string selectedGST,
  string cardCode)
        {
            const string functionName = "SentMail1";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Vendor mail process started.",
                functionName
            );
            try
            {
                string gstNo = selectedGST?.Trim() ?? "";
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Mail request received. " +
           $"CardCode: {cardCode} | ToMail: {toMail}",
           functionName
       );

                if (string.IsNullOrWhiteSpace(gstNo))
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - GST Number is required.",
               functionName
           );

                    return (false, "GST Number is required.");
                }
                log.WriteToLogFile_Debug(
           $"[{functionName}] [VALIDATION] - GST validation successful.",
           functionName
       );
                log.WriteToLogFile_Debug(
                    "SendVendorMail started",
                    "Mail"
                );


                DataTable ds = GetVendorDetails(gstNo);

                if (ds == null || ds.Rows.Count == 0)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - Vendor details not found.",
               functionName
           );
                    return (false, "Vendor details not found.");
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Vendor details retrieved successfully. " +
          $"Records: {ds.Rows.Count}",
          functionName
      );
                DataRow dr = ds.Rows[0];

                var data = new Dictionary<string, object>();

                data["GST Number"] =
                    dr["GstNo"]?.ToString() ?? "";

                data["PAN Number"] =
                    dr["PanNo"]?.ToString() ?? "";

                data["Trade Name"] =
                    dr["TName"]?.ToString() ?? "";

                data["Nature of Business"] =
                    dr["NatureOfBusinessActivity"]?.ToString() ?? "";

                data["Date of Establishment"] =
                    dr["DateOfEstablishment"]?.ToString() ?? "";

                data["NHFS Contact Person"] =
                    dr["ContactPerson"]?.ToString() ?? "";

                data["Designation"] =
                    dr["DeclarationDesignation"]?.ToString() ?? "";

                data["Email ID"] =
                    dr["EmailId"]?.ToString() ?? "";

                data["Mobile Number"] =
                    dr["MobileNo"]?.ToString() ?? "";

                data["Office Telephone"] =
                    dr["VerificationNo"]?.ToString() ?? "";

                data["TAN Number"] =
                    dr["TANNo"]?.ToString() ?? "";

                data["Contact Person"] =
                    dr["ContactPersonName"]?.ToString() ?? "";


                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATA] - Vendor basic details prepared.",
       functionName
   );

                // =====================================================
                // ADDRESS DETAILS
                // =====================================================

                data["Registered Address"] =
                    $"{dr["Raddress1"]}," +
                    $"{dr["Raddress2"]}," +
                    $"{dr["Raddress3"]}," +
                    $"{dr["registeredOfficeCity"]}," +
                    $"{dr["Rstate"]}," +
                    $"{dr["Rcountry"]}-" +
                    $"{dr["Rzipcode"]}";

                data["Billing Address"] =
                    $"{dr["Baddress1"]}," +
                    $"{dr["Baddress2"]}," +
                    $"{dr["Baddress3"]}," +
                    $"{dr["businessBillingCity"]}," +
                    $"{dr["Bstate"]}," +
                    $"{dr["Bcountry"]}-" +
                    $"{dr["Bzipcode"]}";

                data["Shipping Address"] =
                    $"{dr["Saddress1"]}," +
                    $"{dr["Saddress2"]}," +
                    $"{dr["Saddress3"]}," +
                    $"{dr["Scity"]}," +
                    $"{dr["Sstate"]}," +
                    $"{dr["Scountry"]}-" +
                    $"{dr["Szipcode"]}";

                data["Goods Return Address"] =
                    $"{dr["Gaddress1"]}," +
                    $"{dr["Gaddress2"]}," +
                    $"{dr["Gaddress3"]}," +
                    $"{dr["Gcity"]}," +
                    $"{dr["Gstate"]}," +
                    $"{dr["Gcountry"]}-" +
                    $"{dr["Gzipcode"]}";


                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATA] - Vendor address details prepared.",
       functionName
   );

                // =====================================================
                // BANK DETAILS
                // =====================================================

                data["Bank Name"] =
                    dr["BankName"]?.ToString() ?? "";

                data["Account Name"] =
                    dr["AccountName"]?.ToString() ?? "";

                data["Account Number"] =
                    dr["AccountNumber"]?.ToString() ?? "";

                data["IFSC Code"] =
                    dr["IfscCode"]?.ToString() ?? "";

                data["Branch Code"] =
                    dr["BranchCode"]?.ToString() ?? "";

                data["Bank Address"] =
                    dr["BankAddress"]?.ToString() ?? "";
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATA] - Bank details prepared.",
          functionName
      );
                // =====================================================
                // MSME
                // =====================================================

                data["MSME Status"] =
                    dr["MsmeRegistrationStatus"]?.ToString() ?? "";

                data["MSME Number"] =
                    dr["MSMENo"]?.ToString() ?? "";

                data["Enterprise Type"] =
                    dr["EnterpriseType"]?.ToString() ?? "";

                // =====================================================
                // REMARKS
                // =====================================================
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Fetching vendor remarks.",
         functionName
     );

                string remarks = db.GetSingleValue(
                    $@"Call ""{sDBName}"".""GetRemarks""('{gstNo}')"
                );

                data["Remarks"] = remarks ?? "";
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Vendor remarks retrieved successfully.",
           functionName
       );

                // =====================================================
                // DATE / LOCATION
                // =====================================================

                data["date"] =
                    DateTime.Now.ToString("yyyy-MM-dd");

                data["location"] = "TamilNadu";

                // =====================================================
                // PAYMENT DETAILS
                // =====================================================

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching payment details.",
                    functionName
                );

                string id = db.GetSingleValue(
                    $@"select * 
               from ""{sDBName}"".""TEC_OLED"" 
               where ""GstNo""='{gstNo}'"
                );

                DataTable paymentTable =
                    db.ExecuteQueryForDataTable(
                        $@"Select * 
                   from ""{sDBName}"".""PaymentDetails"" 
                   where ""Id""='{id}'"
                    );

                if (paymentTable != null &&
                    paymentTable.Rows.Count > 0)
                {
                    DataRow paymentRow =
                        paymentTable.Rows[0];

                    data["Credit Days"] =
                        paymentRow["CreditDays"]?.ToString() ?? "";

                    data["Discount"] =
                        paymentRow["DisCount"]?.ToString() ?? "";

                    data["md0_with"] =
                        paymentRow["MarkDownTax0"]?.ToString() ?? "";

                    data["md0_without"] =
                        paymentRow["MarkDownWithoutTax0"]?.ToString() ?? "";

                    data["md3_with"] =
                        paymentRow["MarkDownTax3"]?.ToString() ?? "";

                    data["md3_without"] =
                        paymentRow["MarkDownWithoutTax3"]?.ToString() ?? "";

                    data["md5_with"] =
                        paymentRow["MarkDownTax5"]?.ToString() ?? "";

                    data["md5_without"] =
                        paymentRow["MarkDownWithoutTax5"]?.ToString() ?? "";

                    data["md18_with"] =
                        paymentRow["MarkDownTax18"]?.ToString() ?? "";

                    data["md18_without"] =
                        paymentRow["MarkDownWithoutTax18"]?.ToString() ?? "";
                }
                log.WriteToLogFile_Debug(
                $"[{functionName}] [DATABASE] - Payment details retrieved successfully.",
                functionName
            );
                // =====================================================
                // BUSINESS / AGENCY
                // =====================================================

                data["Business Type"] =
                    dr["BusinessType"]?.ToString() ?? "";

                data["Agency Email"] =
                    dr["AgencyEmail"]?.ToString() ?? "";

                data["Agency Name"] =
                    dr["AgencyName"]?.ToString() ?? "";

              

                // =====================================================
                // DECLARATION
                // =====================================================

                data["Name"] =
                    dr["DeclarationName"]?.ToString() ?? "";

                data["Designation"] =
                    dr["DeclarationDesignation"]?.ToString() ?? "";

                data["Mobile No"] =
                    dr["VerificationNo"]?.ToString() ?? "";

                data["Code"] =
                    cardCode ?? "";
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Loading vendor goods details.",
         functionName
     );

                // =====================================================
                // MAJOR GOODS
                // =====================================================

                List<GoodItem> goods =
                    LoadGoodsByGST(gstNo);
                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATABASE] - Vendor goods details loaded successfully. " +
       $"Total items: {goods?.Count ?? 0}",
       functionName
   );

                // =====================================================
                // GENERATE HTML
                // =====================================================
                log.WriteToLogFile_Debug(
         $"[{functionName}] [HTML] - Generating vendor HTML.",
         functionName
     );
                string htmlContent =
                    db.GenerateVendorHtmlWithData(
                        data,
                        goods
                    );

                log.WriteToLogFile_Debug(
                    "Vendor HTML generated",
                    "Mail"
                );

                // =====================================================
                // CONVERT HTML TO PDF
                // =====================================================

                byte[] pdfBytes =
                    db.ConvertHtmlToPdf(htmlContent);

                log.WriteToLogFile_Debug(
                    "PDF generated",
                    "Mail"
                );

                // =====================================================
                // DETERMINE MAIL TYPE
                // =====================================================

                string templateType = "OTP-DRAFT";
                log.WriteToLogFile_Debug(
            $"[{functionName}] [TEMPLATE] - Mail template type determined: {templateType}",
            functionName
        );
                // =====================================================
                // GET MAIL TEMPLATE
                // =====================================================
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Fetching mail template. " +
          $"TemplateType: {templateType}",
          functionName
      );
                DataTable mailTemplateTable =
                    db.ExecuteQueryForDataTable(
                        $@"Call ""{sDBName}"".""Mail_BOSY&SUBJECT""('{templateType}')"
                    );

                string body = "";
                string subject = "";
                string ccMails = "";

                if (mailTemplateTable != null &&
                    mailTemplateTable.Rows.Count > 0)
                {
                    DataRow row =
                        mailTemplateTable.Rows[0];

                    body =
                        row["Body"]?.ToString() ?? "";

                    subject =
                        row["Subject"]?.ToString() ?? "";

                    if (mailTemplateTable.Columns.Contains("CCMail"))
                    {
                        ccMails =
                            row["CCMail"]?.ToString() ?? "";
                    }
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [TEMPLATE] - Mail template retrieved successfully. " +
              $"TemplateType: {templateType}",
              functionName
          );
                }
                else
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [TEMPLATE_NOT_FOUND] - Mail template not found. " +
                $"TemplateType: {templateType}",
                functionName
            );

                    return (
                        false,
                        $"Mail template not found for {templateType}."
                    );
                }

                // =====================================================
                // REJECT / SAP BODY
                // =====================================================

                body = body.Replace(
                    "{Vendor Name}",
                    data["Trade Name"]?.ToString() ?? ""
                );

                if (templateType == "REJECT")
                {
                    // Old Web Forms:
                    // body = body.Replace(
                    //     "{Remarks}",
                    //     Session["RejectRemarks"].ToString()
                    // );

                    body = body.Replace(
                        "{Remarks}",
                        _sessionManager.Get("RejectRemarks") ?? ""
                    );
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [TEMPLATE] - Reject remarks replaced in mail body.",
              functionName
          );
                }
                else
                {
                    // Old Web Forms:
                    // body = body.Replace(
                    //     "{Vendor Code}",
                    //     data1["Code"].ToString()
                    // );

                    body = body.Replace(
                        "{Vendor Code}",
                        data["Code"]?.ToString() ?? ""
                    );
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [TEMPLATE] - Vendor code replaced in mail body.",
                functionName
            );
                }

                // =====================================================
                // SMTP SETTINGS
                // =====================================================

                string fromMail =
                    _configuration["Mail:fromMail"] ?? "";

                string username =
                    _configuration["Mail:SMTPUSER"] ?? "";

                string password =
                    _configuration["Mail:SMTPPWD"] ?? "";

                string server =
                    _configuration["Mail:SMTPSERVER"] ?? "";

                int port =
                    int.TryParse(
                        _configuration["Mail:SMTPPORT"],
                        out int smtpPort
                    )
                        ? smtpPort
                        : 25;
                log.WriteToLogFile_Debug(
        $"[{functionName}] [SMTP] - SMTP configuration loaded. " +
        $"Server: {server} | Port: {port}",
        functionName
    );
                // =====================================================
                // SEND MAIL
                // =====================================================

                using (MailMessage mail =
                    new MailMessage())
                {
                    mail.From =
                        new MailAddress(fromMail);
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [MAIL] - Preparing mail recipients. " +
                $"ToMail: {toMail} | AgentMail: {agentMail}",
                functionName
            );
                    // =================================================
                    // LOG MAIL DETAILS
                    // =================================================

                    log.WriteToLogFile_Debug(
                        "To Mail : " + toMail,
                        "Mail"
                    );

                    log.WriteToLogFile_Debug(
                        "Agent Mail : " + agentMail,
                        "Mail"
                    );

                    log.WriteToLogFile_Debug(
                        "CC Mail : " + ccMails,
                        "Mail"
                    );
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [MAIL] - Mail recipients configured successfully. " +
              $"ToCount: {mail.To.Count} | CCCount: {mail.CC.Count}",
              functionName
          );
                    // =================================================
                    // TO MAIL
                    // =================================================

                    if (!string.IsNullOrWhiteSpace(toMail))
                    {
                        mail.To.Add(
                            toMail.Trim()
                        );
                    }

                    // =================================================
                    // AGENT MAIL
                    // =================================================

                    if (!string.IsNullOrWhiteSpace(agentMail))
                    {
                        mail.To.Add(
                            agentMail.Trim()
                        );
                    }

                    // =================================================
                    // CC MAIL
                    // =================================================

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

                    // =================================================
                    // SUBJECT
                    // =================================================

                    mail.Subject = subject;

                    // =================================================
                    // BODY
                    // =================================================

                    mail.Body = body;

                    mail.IsBodyHtml = true;

                    // =================================================
                    // PDF ATTACHMENT
                    // =================================================

                    using (MemoryStream ms =
                        new MemoryStream(pdfBytes))
                    {
                        mail.Attachments.Add(
                            new Attachment(
                                ms,
                                "VendorRegistrationForm.pdf",
                                "application/pdf"
                            )
                        );
                        log.WriteToLogFile_Debug(
                 $"[{functionName}] [MAIL] - PDF attachment added successfully.",
                 functionName
             );

                        // =============================================
                        // SMTP
                        // =============================================

                        using (SmtpClient smtp =
                            new SmtpClient(
                                server,
                                port
                            ))
                        {
                            smtp.Credentials =
                                new NetworkCredential(
                                    username,
                                    password
                                );

                            smtp.EnableSsl = true;

                            log.WriteToLogFile_Debug(
                                $"{templateType} Mail sending",
                                "Mail"
                            );

                            await smtp.SendMailAsync(mail);

                            log.WriteToLogFile_Debug(
                                $"{templateType} Mail Ended",
                                "Mail"
                            );
                        }
                    }
                }

                // =====================================================
                // SUCCESS
                // =====================================================

                string successMessage =
             templateType == "REJECT"
                 ? "Reject mail sent successfully."
                 : "Mail sent successfully.";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - {successMessage} " +
                    $"CardCode: {cardCode}",
                    functionName
                );

                return (
                    true,
                    successMessage
                );
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [EXCEPTION] - Error while sending vendor mail. " +
            $"CardCode: {cardCode} | " +
            $"ToMail: {toMail} | " +
            $"Message: {ex.Message} | " +
            $"StackTrace: {ex.StackTrace}",
            functionName
        );

                return (
                    false,
                    "Error while sending mail: " +
                    ex.Message
                );
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Vendor mail process ended. " +
                    $"CardCode: {cardCode}",
                    functionName
                );
            }
        }
    }

}


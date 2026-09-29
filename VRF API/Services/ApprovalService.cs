using Serilog;
using System;
using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using VRF_API.Model.RequestModel;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;
using Log = VRF_API.Repository.Log;

namespace VRF_API.Services
{

    public interface IApprovalService
    {
        Task<ApiResponse> SaveApproval(ApprovalRequest approvalRequest);

    }
    public class ApprovalService : IApprovalService
    {
        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly DbConnection db;
        private readonly string sConstr;
        private readonly Log log;
        public ApprovalService(IConfiguration configuration, OdbcConnection connection, DbConnection _db, Log _log)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
            log = _log;
        }


        public async Task<ApiResponse> SaveApproval(ApprovalRequest approvalRequest)
        {
            log.WriteToLogFile_Debug(
    "[ApprovalService] [SaveApproval] [START] - SaveApproval process started.",
    "SaveApproval"
);
            try
            {
                if (approvalRequest == null)
                {
                    log.WriteToLogFile_Debug(
              "[ApprovalService] [SaveApproval] [VALIDATION] - Approval request is null.",
              "SaveApproval"
          );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid approval request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
          $"[ApprovalService] [SaveApproval] [REQUEST] - " +
          $"UserID: {approvalRequest.userID}, " +
          $"UserName: {approvalRequest.UserName}, " +
          $"Department: {approvalRequest.Department}, " +
          $"Count: {approvalRequest.Count}, " +
          $"Level: {approvalRequest.Level}",
          "SaveApproval"
      );

                string checkQuery = "";
                if (approvalRequest.userID > 0)
                {
                    checkQuery = @$"SELECT COUNT(*) FROM ""{sDBName}"".""ApproverMaster"" WHERE ""ApproverDepartment"" = '{approvalRequest.Department}'and ""ID""='{approvalRequest.userID}' and ""Level""='{approvalRequest.Level}'";

                    log.WriteToLogFile_Debug(
                   $"[ApprovalService] [SaveApproval] [DUPLICATE-CHECK] - " +
                   $"Checking existing approval record for Update. UserID: {approvalRequest.userID}, " +
                   $"Department: {approvalRequest.Department}, Level: {approvalRequest.Level}",
                   "SaveApproval"
               );
                }
                else
                {
                    checkQuery = @$"SELECT COUNT(*) FROM ""{sDBName}"".""ApproverMaster"" WHERE ""ApproverDepartment"" = '{approvalRequest.Department}' and ""Level""='{approvalRequest.Level}'";
                    log.WriteToLogFile_Debug(
                     $"[ApprovalService] [SaveApproval] [DUPLICATE-CHECK] - " +
                     $"Checking existing approval record for Insert. " +
                     $"Department: {approvalRequest.Department}, Level: {approvalRequest.Level}",
                     "SaveApproval"
                 );

                }
                
                int count = Convert.ToInt32(db.GetSingleValue(checkQuery));
                log.WriteToLogFile_Debug(
           $"[ApprovalService] [SaveApproval] [DUPLICATE-CHECK] - " +
           $"Existing record count: {count}",
           "SaveApproval"
       );

                if (count > 0)
                {
                    log.WriteToLogFile_Debug(
               $"[ApprovalService] [SaveApproval] [DUPLICATE] - " +
               $"Duplicate approval record found. " +
               $"Department: {approvalRequest.Department}, " +
               $"Level: {approvalRequest.Level}, " +
               $"UserID: {approvalRequest.userID}",
               "SaveApproval"
           );
                    if (approvalRequest.userID > 0)
                    {
                        return new ApiResponse
                        {
                            Status = ApiStatusEnum.Failure,
                            Message = "This department already exists in another record.",
                            ErrorCode = ErrorCodeEnum.Failure,
                            Data = null
                        };
                       
                    }
                    else
                    {
                        return new ApiResponse
                        {
                            Status = ApiStatusEnum.Failure,
                            Message = "This department already exists in the Approver list.",
                            ErrorCode = ErrorCodeEnum.Failure,
                            Data = null
                        };
                    }

                }
                log.WriteToLogFile_Debug(
           $"[ApprovalService] [SaveApproval] [WAITING-APPROVAL-CHECK] - " +
           $"Checking pending approvals for UserName: {approvalRequest.UserName}",
           "SaveApproval"
       );

                string query = $@"Call ""{sDBName}"".""TEC_GetApprovalWaitingDetails"" ('{approvalRequest.UserName}')";

               

                DataTable dt = db.ExecuteQueryForDataTable(query);
                log.WriteToLogFile_Debug(
                   $"[ApprovalService] [SaveApproval] [WAITING-APPROVAL-CHECK] - " +
                   $"Pending approval record count: {dt.Rows.Count}",
                   "SaveApproval"
               );
            //    if (dt.Rows.Count > 0)
            //    {
            //        log.WriteToLogFile_Debug(
            //    $"[ApprovalService] [SaveApproval] [VALIDATION] - " +
            //    $"User has pending approvals. Approver master change is not allowed. " +
            //    $"UserName: {approvalRequest.UserName}",
            //    "SaveApproval"
            //);

            //        return new ApiResponse
            //        {
            //            Status = ApiStatusEnum.Failure,
            //            Message = "Kindly approve all waiting approvals before change the approver master.",
            //            ErrorCode = ErrorCodeEnum.Failure,
            //            Data = null
            //        };

            //    }
                string count1 = db.GetSingleValue($@"Select ifnull(max(""ID""),0)+1 from ""{sDBName}"".""ApproverMaster""");
                Int64 ID = Convert.ToInt64(count1);
                log.WriteToLogFile_Debug(
           $"[ApprovalService] [SaveApproval] [ID-GENERATION] - " +
           $"Generated new ApproverMaster ID: {ID}",
           "SaveApproval"
       );

                string query2 = "";
                if (approvalRequest.userID > 0)
                {
                    log.WriteToLogFile_Debug(
               $"[ApprovalService] [SaveApproval] [UPDATE] - " +
               $"Preparing to update ApproverMaster. " +
               $"ID: {approvalRequest.userID}, " +
               $"Department: {approvalRequest.Department}, " +
               $"Count: {approvalRequest.Count}, " +
               $"Level: {approvalRequest.Level}",
               "SaveApproval"
           );
                    query2 = $@"UPDATE ""{sDBName}"".""ApproverMaster"" SET ""ApproverDepartment"" = '{approvalRequest.Department}', 
                        ""DepartmentApproverCount"" = {approvalRequest.Count},""Level""={approvalRequest.Level} WHERE ""ID"" = {approvalRequest.userID}";
                  
                }
                else
                {
                    log.WriteToLogFile_Debug(
                $"[ApprovalService] [SaveApproval] [INSERT] - " +
                $"Preparing to insert new ApproverMaster record. " +
                $"ID: {ID}, " +
                $"Department: {approvalRequest.Department}, " +
                $"Count: {approvalRequest.Count}, " +
                $"Level: {approvalRequest.Level}",
                "SaveApproval"
            );

                    query2 = $@"
            INSERT INTO ""{sDBName}"".""ApproverMaster""
            (
                ""ID"",
                ""ApproverDepartment"",
                ""DepartmentApproverCount"",
                ""Level""
            )
            VALUES
            (
                '{ID}',
                '{approvalRequest.Department}',
                '{approvalRequest.Count}',
                '{approvalRequest.Level}'
            )";
                }

                    db.ExecuteNonQuery(query2);
                log.WriteToLogFile_Debug(
         $"[ApprovalService] [SaveApproval] [DATABASE] - " +
         $"ApproverMaster {(approvalRequest.userID > 0 ? "update" : "insert")} completed successfully. " +
         $"ID: {(approvalRequest.userID > 0 ? approvalRequest.userID : ID)}",
         "SaveApproval"
     );

                if (approvalRequest.userID > 0)
                {
                    log.WriteToLogFile_Debug(
               $"[ApprovalService] [SaveApproval] [SUCCESS] - " +
               $"Approval updated successfully. ID: {approvalRequest.userID}, " +
               $"Department: {approvalRequest.Department}",
               "SaveApproval"
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Success,
                        Message = "Approval Updated Successfully.",
                        ErrorCode = ErrorCodeEnum.Success,
                        Data = null
                    };
                }
                else{
                    log.WriteToLogFile_Debug(
              $"[ApprovalService] [SaveApproval] [SUCCESS] - " +
              $"Approval saved successfully. ID: {ID}, " +
              $"Department: {approvalRequest.Department}",
              "SaveApproval"
          );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Success,
                        Message = "Approval Saved Successfully.",
                        ErrorCode = ErrorCodeEnum.Success,
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
           $"[ApprovalService] [SaveApproval] [EXCEPTION] - " +
           $"Error while saving approval. " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           "SaveApproval"
       );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving department: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }

    }
}

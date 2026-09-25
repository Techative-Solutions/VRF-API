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

        public ApprovalService(IConfiguration configuration, OdbcConnection connection, DbConnection _db)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
        }


        public async Task<ApiResponse> SaveApproval(ApprovalRequest approvalRequest)
        {
            try
            {
                if (approvalRequest == null)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid approval request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                string checkQuery = "";
                if (approvalRequest.userID > 0)
                {
                    checkQuery = @$"SELECT COUNT(*) FROM ""{sDBName}"".""ApproverMaster"" WHERE ""ApproverDepartment"" = '{approvalRequest.Department}'and ""ID""='{approvalRequest.userID}' and ""Level""='{approvalRequest.Level}'";
                }
                else
                {
                    checkQuery = @$"SELECT COUNT(*) FROM ""{sDBName}"".""ApproverMaster"" WHERE ""ApproverDepartment"" = '{approvalRequest.Department}' and ""Level""='{approvalRequest.Level}'";
                }
                
                int count = Convert.ToInt32(db.GetSingleValue(checkQuery));

                if (count > 0)
                {
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

                //string query = $@"Call ""{sDBName}"".""TEC_GetApprovalWaitingDetails"" ('{approvalRequest.UserName}')";
                //DataTable dt = db.ExecuteQueryForDataTable(query);

                //if (dt.Rows.Count > 0)
                //{
                //    return new ApiResponse
                //    {
                //        Status = ApiStatusEnum.Failure,
                //        Message = "Kindly approve all waiting approvals before change the approver master.",
                //        ErrorCode = ErrorCodeEnum.Failure,
                //        Data = null
                //    };

                //}
                string count1 = db.GetSingleValue($@"Select ifnull(max(""ID""),0)+1 from ""{sDBName}"".""ApproverMaster""");
                Int64 ID = Convert.ToInt64(count1);

                string query2 = "";
                if (approvalRequest.userID > 0)
                {

                    query2 = $@"UPDATE ""{sDBName}"".""ApproverMaster"" SET ""ApproverDepartment"" = '{approvalRequest.Department}', 
                        ""DepartmentApproverCount"" = {approvalRequest.Count},""Level""={approvalRequest.Level} WHERE ""ID"" = {approvalRequest.userID}";
                  
                }
                else
                {
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

                if (approvalRequest.userID > 0)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Success,
                        Message = "Approval Updated Successfully.",
                        ErrorCode = ErrorCodeEnum.Success,
                        Data = null
                    };
                }
                else{
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

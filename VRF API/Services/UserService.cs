using Serilog;
using System;
using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Text;
using VRF_API.Model.RequestModel;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;
using Log = VRF_API.Repository.Log;

namespace VRF_API.Services
{

    public interface IUserService
    {
        Task<ApiResponse> Login(string Username, string Password);
        Task<ApiResponse> ResetPassword(
       string username,
       string oldPassword,
       string newPassword);
        Task<ApiResponse> GetList(GetListRequest request);
        Task<List<DeportmentResponse>> Department();
        Task<ApiResponse> SaveDepartment(DepartmentRequest departmentRequest);
        Task<ApiResponse> CommonDelete(DeleteRequest deleteRequest);
        Task<ApiResponse> SaveUserDetails(UserRequest userRequest);
        Task<List<UserResponse>> GetUserDetails(string UserID);

        Task<ApiResponse> Update(UserRequest userRequest);
    }

    public class UserService : IUserService
    {

        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly DbConnection db;
        private readonly string sConstr;
        private readonly Repository.Log log;
        public UserService(IConfiguration configuration, OdbcConnection connection, DbConnection _db, Log _log)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
            log = _log;
        }

        public async Task<ApiResponse> Login(string username, string password)
        {
            const string functionName = "Login";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - User login process started.",
                functionName
            );
            try
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Login request received. " +
           $"Username: {username}",
           functionName
       );

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [VALIDATION_FAILED] - " +
                $"Username or password is empty.",
                functionName
            );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Username and password are required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                log.WriteToLogFile_Debug(
         $"[{functionName}] [VALIDATION] - Login request validation completed successfully.",
         functionName
     );

            
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Checking user account status.",
                    functionName
                );

                string query = $@"
            SELECT ""Active""
            FROM ""{sDBName}"".""TEC_OUSR""
            WHERE ""User_Name"" ='{username}'
               OR ""User_Mail_Id"" ='{username}'";



                string active = db.GetSingleValue(query);

                log.WriteToLogFile_Debug(
            $"[{functionName}] [DATABASE] - User account status retrieved successfully.",
            functionName
        );

                if (string.IsNullOrEmpty(active))
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [LOGIN_FAILED] - User not found for the provided username/email.",
                functionName
            );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid username or email.",
                        ErrorCode = ErrorCodeEnum.Failure,

                        Data = null
                    };
                }

                // User inactive
                if (active.Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    log.WriteToLogFile_Debug(
            $"[{functionName}] [LOGIN_FAILED] - User account is inactive.",
            functionName
        );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,

                        Message = "User account is inactive.",
                        ErrorCode = ErrorCodeEnum.Failure,

                        Data = null
                    };
                }

                log.WriteToLogFile_Debug(
             $"[{functionName}] [DATABASE] - User account is active. " +
             $"Proceeding with credential validation.",
             functionName
         );

                // Validate username + password
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [AUTHENTICATION] - Validating user credentials.",
                    functionName
                );

                int isValid = IsValidUser(username, password);

                if (isValid == 1)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [SUCCESS] - User login successful.",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Success,
                        Message = "Login successful.",
                        ErrorCode = ErrorCodeEnum.Success,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
           $"[{functionName}] [LOGIN_FAILED] - Invalid username or password.",
           functionName
       );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = "Invalid username or password.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [EXCEPTION] - Error occurred during login process. " +
             $"Username: {username} | " +
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
                    $"[{functionName}] [END] - User login process ended.",
                    functionName
                );
            }
        }

        private int IsValidUser(string username, string password)
        {
            const string functionName = "IsValidUser";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - User credential validation started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [REQUEST] - Credential validation request received. " +
                    $"Username: {username}",
                    functionName
                );

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - Username or password is empty.",
                        functionName
                    );

                    return 0;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Fetching stored password for user.",
                    functionName
                );

                string query = $@"
            SELECT ""Password""
            FROM ""{sDBName}"".""TEC_OUSR""
            WHERE ""Active"" = true
              AND (
                    ""User_Name"" = '{username}'
                    OR ""User_Mail_Id"" = '{username}'
                  )";

                string pass = db.GetSingleValue(query);

                if (string.IsNullOrWhiteSpace(pass))
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [AUTHENTICATION_FAILED] - " +
                        $"No password record found for the provided username/email.",
                        functionName
                    );

                    return 0;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Stored password retrieved successfully.",
                    functionName
                );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [AUTHENTICATION] - Decrypting stored password for validation.",
                    functionName
                );

                string pass1 = Decryptpass(pass);

                if (pass1 == password)
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [SUCCESS] - User credentials validated successfully.",
                        functionName
                    );

                    return 1;
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [AUTHENTICATION_FAILED] - Invalid user credentials.",
                    functionName
                );

                return 0;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [EXCEPTION] - Error while validating user credentials. " +
                    $"Username: {username} | " +
                    $"Message: {ex.Message} | " +
                    $"StackTrace: {ex.StackTrace}",
                    functionName
                );

                return 0;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - User credential validation ended.",
                    functionName
                );
            }
        }
        static string Decryptpass(string encodedPassword)
        {
            byte[] decodedBytes = Convert.FromBase64String(encodedPassword);
            string decodedPassword = Encoding.UTF8.GetString(decodedBytes);
            return decodedPassword;
        }

        public async Task<ApiResponse> ResetPassword(
       string username,
       string oldPassword,
       string newPassword)
        {
            const string functionName = "ResetPassword";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Password reset process started.",
                functionName
            );
            try
            {
                log.WriteToLogFile_Debug(
         $"[{functionName}] [REQUEST] - Password reset request received. " +
         $"Username: {username}",
         functionName
     );

                if (string.IsNullOrWhiteSpace(username))
                {
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [VALIDATION_FAILED] - Username is empty.",
              functionName
          );

                    return new ApiResponse
                    {

                        Status = ApiStatusEnum.Failure,
                        Message = "Username is required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [VALIDATION_FAILED] - Old password is empty.",
                functionName
            );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Old password is required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - New password is empty.",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "New password is required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                log.WriteToLogFile_Debug(
             $"[{functionName}] [VALIDATION] - Password reset request validation completed successfully.",
             functionName
         );

                // 1. Validate old password
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [AUTHENTICATION] - Validating old password.",
                    functionName
                );

                int isValid = IsValidUser(username, oldPassword);

                if (isValid != 1)
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [AUTHENTICATION_FAILED] - Old password validation failed.",
                functionName
            );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Old password is incorrect.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
                           $"[{functionName}] [AUTHENTICATION] - Old password validated successfully.",
                           functionName
                       );

                // 2. Encrypt new password
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SECURITY] - Encrypting new password.",
                    functionName
                );
                string encryptedPassword = Encryptpass(newPassword);
                log.WriteToLogFile_Debug(
          $"[{functionName}] [SECURITY] - New password encrypted successfully.",
          functionName
      );

                // Escape username/password before SQL
                string safeUsername = username.Replace("'", "''");
                string safePassword = encryptedPassword.Replace("'", "''");

                log.WriteToLogFile_Debug(
              $"[{functionName}] [DATABASE] - Updating user password.",
              functionName
          );

                string query = $@"
            UPDATE ""{sDBName}"".""TEC_OUSR""
            SET
                ""Password"" = '{safePassword}',
                ""Confirm_Password"" = '{safePassword}'
            WHERE
                ""User_Name"" = '{safeUsername}'
                OR ""User_Mail_Id"" = '{safeUsername}'";

                db.ExecuteNonQuery(query);
                log.WriteToLogFile_Debug(
      $"[{functionName}] [DATABASE] - User password updated successfully.",
      functionName
  );

                // 4. Success
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Password reset completed successfully. " +
                    $"Username: {username}",
                    functionName
                );

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Password updated successfully.",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [EXCEPTION] - Error while resetting password. " +
             $"Username: {username} | " +
             $"Message: {ex.Message} | " +
             $"StackTrace: {ex.StackTrace}",
             functionName
         );


                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = "An error occurred while updating the password.",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }

        private static string Encryptpass(string password)
        {
            string msg = "";
            byte[] encode = new byte[password.Length];
            encode = Encoding.UTF8.GetBytes(password);
            msg = Convert.ToBase64String(encode);
            return msg;
        }


        public async Task<ApiResponse> GetList(GetListRequest request)
        {
            const string functionName = "Get_List";

            var result = new List<Dictionary<string, object>>();

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Get list process started.",
                functionName
            );

          

            try
            {
                string spName = "";

                if (request.type == "User")
                {
                    spName = "TEC_UserDetails";
                }
                else if (request.type == "Department")
                {
                    spName = "TEC_DepartmentDetails";
                }
                else if (request.type == "Approval")
                {
                    spName = "TEC_ApprovalDetails";
                }
                log.WriteToLogFile_Debug(
      $"[{functionName}] [FLOW] - Request type validated successfully. " +
      $"Type: {request.type} | SPName: {spName}",
      functionName
  );

                string query = @$"CALL ""{sDBName}"".""{spName}"" ()";
                log.WriteToLogFile_Debug(
            $"[{functionName}] [DATABASE] - Preparing stored procedure. " +
            $"SPName: {spName}",
            functionName
        );
                using var connection = new OdbcConnection(_connection.ConnectionString);
                {
                    log.WriteToLogFile_Debug(
          $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
          functionName
      );

                    await connection.OpenAsync();

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
                        functionName
                    );

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Executing stored procedure. " +
         $"SPName: {spName}",
         functionName
     );


                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
           functionName
       );

                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    row[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }

                                result.Add(row);
                            }
                        }
                    }
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [SUCCESS] - List retrieved successfully. " +
          $"Type: {request.type} | " +
          $"SPName: {spName} | " +
          $"Total records: {result.Count}",
          functionName
      );

                return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Retrieved successfully", result);
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
              $"[{functionName}] [EXCEPTION] - Error while retrieving list. " +
              $"Type: {request?.type} | " +
              $"Message: {ex.Message} | " +
              $"StackTrace: {ex.StackTrace}",
              functionName
          );

                throw;
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Get list process ended. " +
                    $"Total records: {result.Count}",
                    functionName
                );
            }
        }



        public async Task<List<DeportmentResponse>> Department()
        {
            const string functionName = "Department";
            const string spName = "Get_Department";

            List<DeportmentResponse> RejDetailsList = new();

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Department details retrieval started.",
                functionName
            );


         

            try
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [REQUEST] - Department details request received.",
            functionName
        );

                string query =
                    $@"CALL ""{sDBName}"".""{spName}"" ()";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing stored procedure. " +
                    $"SPName: {spName}",
                    functionName
                );
                using var connection = new OdbcConnection(sConstr);
                log.WriteToLogFile_Debug(
             $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
             functionName
         );

                connection.Open();
                log.WriteToLogFile_Debug(
         $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
         functionName
     );

                using var cmd = new OdbcCommand(query, connection);

                log.WriteToLogFile_Debug(
             $"[{functionName}] [DATABASE] - Executing stored procedure. " +
             $"SPName: {spName}",
             functionName
         );

                using var reader = await cmd.ExecuteReaderAsync();
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
           functionName
       );

                while (await reader.ReadAsync())
                {
                    var detail = new DeportmentResponse
                    {
                        DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : reader["DepartmentID"].ToString(),
                        DepartmentName = reader["DepartmentName"] == DBNull.Value ? null : reader["DepartmentName"].ToString(),
                        IsActive = reader["IsActive"] == DBNull.Value ? null : reader["IsActive"].ToString(),


                    };

                    RejDetailsList.Add(detail);
                }

                log.WriteToLogFile_Debug(
           $"[{functionName}] [SUCCESS] - Department details retrieved successfully. " +
           $"Total records: {RejDetailsList.Count}",
           functionName
       );

                return RejDetailsList;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [EXCEPTION] - Error while retrieving department details. " +
             $"SPName: {spName} | " +
             $"Message: {ex.Message} | " +
             $"StackTrace: {ex.StackTrace}",
             functionName
         );
                return new List<DeportmentResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [END] - Department details retrieval ended. " +
             $"Total records: {RejDetailsList.Count}",
             functionName
         );
            }
        }


        public async Task<ApiResponse> SaveDepartment(DepartmentRequest departmentRequest)
        {
            const string functionName = "SaveDepartment";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Department save process started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Department save request received.",
           functionName
       );
                if (departmentRequest == null)
                {
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [VALIDATION_FAILED] - Department request is null.",
              functionName
          );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid department request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
        $"[{functionName}] [REQUEST] - Department details received. " +
        $"DepartmentID: {departmentRequest.DepartmentID} | " +
        $"DepartmentName: {departmentRequest.DepartmentName} | " +
        $"Active: {departmentRequest.Active}",
        functionName
    );

                bool active = departmentRequest.Active == true;
                log.WriteToLogFile_Debug(
    $"[{functionName}] [VALIDATION] - Department request validation completed successfully. " +
    $"Active status: {active}",
    functionName
);

                // Prepare insert query
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing department insert query.",
                    functionName
                );

                string query = $@"
            INSERT INTO ""{sDBName}"".""Department""
            (
                ""DepartmentID"",
                ""DepartmentName"",
                ""IsActive""
            )
            VALUES
            (
                '{departmentRequest.DepartmentID}',
                '{departmentRequest.DepartmentName}',
                {(active ? "TRUE" : "FALSE")}
            )";
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Executing department insert.",
          functionName
      );

                db.ExecuteNonQuery(query);
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Department inserted successfully. " +
          $"DepartmentID: {departmentRequest.DepartmentID}",
          functionName
      );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Department saved successfully. " +
                    $"DepartmentID: {departmentRequest.DepartmentID}",
                    functionName
                );

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Department Saved Successfully.",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [EXCEPTION] - Error while saving department. " +
           $"DepartmentID: {departmentRequest?.DepartmentID} | " +
           $"DepartmentName: {departmentRequest?.DepartmentName} | " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           functionName
       );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving department: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Department save process ended.",
                    functionName
                );
            }
        }

        public async Task<ApiResponse> CommonDelete(DeleteRequest deleteRequest)
        {
            const string functionName = "CommonDelete";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Common delete process started.",
                functionName
            );
            try
            {
                log.WriteToLogFile_Debug(
        $"[{functionName}] [REQUEST] - Delete request received. " +
        $"Type: {deleteRequest?.Type} | ID: {deleteRequest?.ID}",
        functionName
    );
                if (deleteRequest == null || string.IsNullOrWhiteSpace(deleteRequest.ID))
                {
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [VALIDATION_FAILED] - " +
              $"Delete request or ID is invalid.",
              functionName
          );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid Department ID.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                string query = "";
                if (deleteRequest.Type == "User")
                {
                    query = $@"
            DELETE FROM ""{sDBName}"".""TEC_OUSR""
            WHERE ""User_Mail_Id"" = ?";
                }
                else if (deleteRequest.Type == "Department")
                {
                    query = $@"
            DELETE FROM ""{sDBName}"".""Department""
            WHERE ""DepartmentID"" = ?";
                }
                else if (deleteRequest.Type == "Approval")
                {
                    query = $@"
            DELETE FROM ""{sDBName}"".""ApproverMaster""
            WHERE ""ID"" = ?";
                }
                else
                {
                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [VALIDATION_FAILED] - " +
                        $"Invalid delete type. Type: {deleteRequest.Type}",
                        functionName
                    );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid delete type.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                log.WriteToLogFile_Debug(
         $"[{functionName}] [FLOW] - Delete type validated successfully. " +
         $"Type: {deleteRequest.Type} | " ,
         functionName
     );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Preparing delete operation. ",
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
                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue(
                    "@DepartmentID",
                    deleteRequest.ID
                );
                log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Executing delete operation. " +
           $"Type: {deleteRequest.Type} | ",
           functionName
       );

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Delete operation executed. " +
         $"Rows affected: {rowsAffected}",
         functionName
     );
                if (rowsAffected == 0)
                {
                    log.WriteToLogFile_Debug(
            $"[{functionName}] [DELETE_FAILED] - No record found to delete. " +
            $"Type: {deleteRequest.Type} | " +
            $"ID: {deleteRequest.ID}",
            functionName
        );
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Department not found.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SUCCESS] - Data deleted successfully. " +
           $"Type: {deleteRequest.Type} | " +
           $"Rows affected: {rowsAffected}",
           functionName
       );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "Data Deleted Successfully.",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [EXCEPTION] - Error while deleting data. " +
             $"Type: {deleteRequest?.Type} | " +
             $"ID: {deleteRequest?.ID} | " +
             $"Message: {ex.Message} | " +
             $"StackTrace: {ex.StackTrace}",
             functionName
         );

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while deleting department: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Common delete process ended.",
                    functionName
                );
            }

        }



        public async Task<ApiResponse> SaveUserDetails(UserRequest userRequest)
        {
            const string functionName = "SaveUserDetails";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - User details save process started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - User creation request received. " +
           $"UserName: {userRequest?.UserName} | " +
           $"UserMail: {userRequest?.UserMail} | " +
           $"MobileNo: {userRequest?.Mobileno} | " +
           $"Department: {userRequest?.Department} | " +
           $"Level: {userRequest?.Level} | " +
           $"Active: {userRequest?.Active}",
           functionName
       );

                if (userRequest == null)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - User request is null.",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid userRequest request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
            $"[{functionName}] [VALIDATION] - User request validation completed successfully.",
            functionName
        );

                bool active = userRequest.Active == true;
                log.WriteToLogFile_Debug(
          $"[{functionName}] [SECURITY] - Encrypting user password.",
          functionName
      );

                string pass = Encryptpass(userRequest.Password);
                string conpass = Encryptpass(userRequest.ConfirmPassword);
                log.WriteToLogFile_Debug(
           $"[{functionName}] [SECURITY] - Password encryption completed successfully.",
           functionName
       );
                log.WriteToLogFile_Debug(
                         $"[{functionName}] [DATABASE] - Checking username availability.",
                         functionName
                     );
                string query = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Name"" = '{userRequest.UserName}'";
                string query1 = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Mail_Id"" = '{userRequest.UserMail}'";
                

                string userCount = db.GetSingleValue(query);
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Username availability check completed. " +
          $"Existing records: {userCount}",
          functionName
      );
                string userCount1 = db.GetSingleValue(query1);
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Email availability check completed. " +
         $"Existing records: {userCount1}",
         functionName
     );

                if (Convert.ToInt32(userCount) > 0)
                {
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [VALIDATION_FAILED] - Username already exists. " +
              $"UserName: {userRequest.UserName}",
              functionName
          );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Username already Exist!.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                if (Convert.ToInt32(userCount1) > 0)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - User email already exists. " +
               $"UserMail: {userRequest.UserMail}",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "UserMail already Exist!.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Preparing user insert operation.",
          functionName
      );

                string query3 = $@"
            INSERT INTO ""{sDBName}"".""TEC_OUSR""
            (
                ""User_Name"",
                ""Password"",
                ""Confirm_Password"",
                ""User_Mail_Id"",
                ""Mobile_No"",
                ""Active"",
                 ""FileName"",
                ""ProfileUpload"",
                ""Department"",
                 ""Level""
            )
            VALUES
            (
                '{userRequest.UserName}',
                '{pass}',
                '{conpass}',
                 '{userRequest.UserMail}',
                '{userRequest.Mobileno}',
                {(active ? "TRUE" : "FALSE")},
                '{userRequest.FileName}',
                '{userRequest.Profileupload}',
                '{userRequest.Department}',
                '{userRequest.Level}'
            )";

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Executing user insert operation.",
                    functionName
                );
                db.ExecuteNonQuery(query3);
                log.WriteToLogFile_Debug(
       $"[{functionName}] [DATABASE] - User record inserted successfully. " +
       $"UserName: {userRequest.UserName}",
       functionName
   );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - User created successfully. " +
                    $"UserName: {userRequest.UserName}",
                    functionName
                );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "User Created Successfully.",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
         $"[{functionName}] [EXCEPTION] - Error while saving user details. " +
         $"UserName: {userRequest?.UserName} | " +
         $"UserMail: {userRequest?.UserMail} | " +
         $"Message: {ex.Message} | " +
         $"StackTrace: {ex.StackTrace}",
         functionName
     );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving User: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - User details save process ended.",
                    functionName
                );
            }
        }


        public async Task<List<UserResponse>> GetUserDetails(string UserID)
        {
            const string functionName = "GetUserDetails";
            const string spName = "TEC_Editing";

            string query = $@"CALL ""{sDBName}"".""{spName}"" (?, ?)";

            List<UserResponse> userDetailsList = new();

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - User details retrieval started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [REQUEST] - User details request received. " +
            $"UserID: {UserID}",
            functionName
        );

                log.WriteToLogFile_Debug(
            $"[{functionName}] [REQUEST] - User details request received. " +
            $"UserID: {UserID}",
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

                using var cmd = new OdbcCommand(query, connection);

                // ODBC parameters are positional
                cmd.Parameters.AddWithValue("@Type", "EditUser");
                cmd.Parameters.AddWithValue("@UserID", UserID);
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Executing stored procedure. " +
         $"SPName: {spName} | Type: EditUser | UserID: {UserID}",
         functionName
     );

                using var reader = await cmd.ExecuteReaderAsync();
                log.WriteToLogFile_Debug(
        $"[{functionName}] [DATABASE] - Stored procedure executed successfully. " +
        $"SPName: {spName}",
        functionName
    );

                while (await reader.ReadAsync())
                {
                    var detail = new UserResponse
                    {
                        UserID = reader["User_Id"] == DBNull.Value
                            ? null
                            : reader["User_Id"].ToString(),

                        UserName = reader["User_Name"] == DBNull.Value
                            ? null
                            : reader["User_Name"].ToString(),

                        Password = Decryptpass(reader["Password"] == DBNull.Value
                            ? null
                            : reader["Password"].ToString()),

                        ConfirmPassword = Decryptpass(reader["Confirm_Password"] == DBNull.Value
                            ? null
                            : reader["Confirm_Password"].ToString()),

                        UserMail = reader["User_Mail_Id"] == DBNull.Value
                            ? null
                            : reader["User_Mail_Id"].ToString(),

                        Mobileno = reader["Mobile_No"] == DBNull.Value
                            ? null
                            : reader["Mobile_No"].ToString(),

                        Department = reader["Department"] == DBNull.Value
                            ? null
                            : reader["Department"].ToString(),

                        Active = reader["Active"] == DBNull.Value
                            ? false
                            : Convert.ToBoolean(reader["Active"]),

                        FileName = reader["FileName"] == DBNull.Value
                            ? null
                            : reader["FileName"].ToString(),

                        Profileupload = reader["ProfileUpload"] == DBNull.Value
                            ? null
                            : reader["ProfileUpload"].ToString(),

                        Level = reader["Level"] == DBNull.Value
                            ? null
                            : reader["Level"].ToString()
                    };
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [SUCCESS] - User details retrieved successfully. " +
               $"UserID: {UserID} | " +
               $"Total records: {userDetailsList.Count}",
               functionName
           );

                    userDetailsList.Add(detail);
                }

                return userDetailsList;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [EXCEPTION] - Error while retrieving user details. " +
            $"UserID: {UserID} | " +
            $"SPName: {spName} | " +
            $"Message: {ex.Message} | " +
            $"StackTrace: {ex.StackTrace}",
            functionName
        );


                return new List<UserResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - User details retrieval ended. " +
                    $"Total records: {userDetailsList.Count}",
                    functionName
                );
            }
        }



        public async Task<ApiResponse> Update(UserRequest userRequest)
        {
            const string functionName = "Update";
            const string spName = "TEC_UPDATEUSER";

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - User update process started.",
                functionName
            );

            try
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - User update request received. " +
           $"UserName: {userRequest?.UserName} | " +
           $"UserMail: {userRequest?.UserMail} | " +
           $"MobileNo: {userRequest?.Mobileno} | " +
           $"Department: {userRequest?.Department} | " +
           $"Level: {userRequest?.Level} | " +
           $"Active: {userRequest?.Active}",
           functionName
       );

                if (userRequest == null)
                {
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION_FAILED] - User request is null.",
               functionName
           );

                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid userRequest request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                bool active = userRequest.Active == true;

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SECURITY] - Encrypting updated user password.",
                    functionName
                );
                string pass = Encryptpass(userRequest.Password);
                string conpass = Encryptpass(userRequest.ConfirmPassword);
                log.WriteToLogFile_Debug(
       $"[{functionName}] [SECURITY] - Password encryption completed successfully.",
       functionName
   );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [DATABASE] - Checking existing username using user email.",
                    functionName
                );
                string query = $@"SELECT ""User_Name"" FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Mail_Id"" = '{userRequest.UserMail}'";


                string existingUsername = db.GetSingleValue(query);
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - Existing username lookup completed. " +
         $"Username found: {!string.IsNullOrWhiteSpace(existingUsername)}",
         functionName
     );

                string query3 = "";
                if (userRequest.UserName == existingUsername)
                {
                    log.WriteToLogFile_Debug(
              $"[{functionName}] [FLOW] - Existing username belongs to the provided email. " +
              $"Proceeding with user update.",
              functionName
          );

                    query3 = $@"CALL ""{sDBName}"".""TEC_UPDATEUSER""
            (
                '{userRequest.UserName}',
                '{pass}',
                '{conpass}',
  '{userRequest.Mobileno}',
                
              
                {(active ? "TRUE" : "FALSE")},
                '{userRequest.FileName}',
                '{userRequest.Profileupload}',
                '{userRequest.Department}',
                '{userRequest.Level}',
 '{userRequest.UserMail}'
            )";
                }
                else
                {
                    log.WriteToLogFile_Debug(
                $"[{functionName}] [FLOW] - Username differs from the username associated with the email. " +
                $"Checking username availability.",
                functionName
            );

                    string query4 = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Name"" = '{userRequest.UserName}'";
                    log.WriteToLogFile_Debug(
               $"[{functionName}] [DATABASE] - Checking username availability.",
               functionName
           );


                    string userCount = db.GetSingleValue(query4);

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE] - Username availability check completed. " +
                        $"Existing records: {userCount}",
                        functionName
                    );

                    if (Convert.ToInt32(userCount) > 0)
                    {
                        log.WriteToLogFile_Debug(
                    $"[{functionName}] [VALIDATION_FAILED] - Username already exists. " +
                    $"UserName: {userRequest.UserName}",
                    functionName
                );

                        return new ApiResponse
                        {
                            Status = ApiStatusEnum.Failure,
                            Message = "Username already Exist!.",
                            ErrorCode = ErrorCodeEnum.Failure,
                            Data = null
                        };
                    }
                    else
                    {
                        log.WriteToLogFile_Debug(
               $"[{functionName}] [VALIDATION] - Username is available. Proceeding with user update.",
               functionName
           );
                        query3 = $@"CALL ""{sDBName}"".""TEC_UPDATEUSER""
            (
                '{userRequest.UserName}',
                '{pass}',
                '{conpass}',
  '{userRequest.Mobileno}',
                
              
                {(active ? "TRUE" : "FALSE")},
                '{userRequest.FileName}',
                '{userRequest.Profileupload}',
                '{userRequest.Department}',
                '{userRequest.Level}',
 '{userRequest.UserMail}'
            )";
                    }
                }
                log.WriteToLogFile_Debug(
          $"[{functionName}] [DATABASE] - Executing user update stored procedure. " +
          $"SPName: {spName}",
          functionName
      );

                db.ExecuteNonQuery(query3);
                log.WriteToLogFile_Debug(
         $"[{functionName}] [DATABASE] - User update stored procedure executed successfully. " +
         $"SPName: {spName}",
         functionName
     );

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - User updated successfully. " +
                    $"UserName: {userRequest.UserName}",
                    functionName
                );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Success,
                    Message = "User Updated Successfully.",
                    ErrorCode = ErrorCodeEnum.Success,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [EXCEPTION] - Error while updating user details. " +
           $"UserName: {userRequest?.UserName} | " +
           $"UserMail: {userRequest?.UserMail} | " +
           $"SPName: {spName} | " +
           $"Message: {ex.Message} | " +
           $"StackTrace: {ex.StackTrace}",
           functionName
       );
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving User: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - User update process ended.",
                    functionName
                );
            }
        }


    }



}

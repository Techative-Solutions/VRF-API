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

        public UserService(IConfiguration configuration, OdbcConnection connection, DbConnection _db)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
        }

        public async Task<ApiResponse> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Username and password are required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                // IMPORTANT:
                // Use parameterized SQL instead of concatenating username.
                string query = $@"
            SELECT ""Active""
            FROM ""{sDBName}"".""TEC_OUSR""
            WHERE ""User_Name"" ='{username}'
               OR ""User_Mail_Id"" ='{username}'";



                string active = db.GetSingleValue(query);

                // User not found
                if (string.IsNullOrEmpty(active))
                {
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
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,

                        Message = "User account is inactive.",
                        ErrorCode = ErrorCodeEnum.Failure,

                        Data = null
                    };
                }

                // Validate username + password
                int isValid = IsValidUser(username, password);

                if (isValid == 1)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Success,
                        Message = "Login successful.",
                        ErrorCode = ErrorCodeEnum.Success,
                        Data = null
                    };
                }

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = "Invalid username or password.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                // Log the exception here
                // logger.LogError(ex, "Login failed for {Username}", username);

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = "An error occurred while processing the login.",
                    Data = null
                };
            }
        }

        private int IsValidUser(string username, string password)
        {
            string query = $@"
            SELECT ""Password""
            FROM ""{sDBName}"".""TEC_OUSR""
            WHERE ""Active"" =true
               and ""User_Name"" ='{username}' or ""User_Mail_Id"" ='{username}'";
            string pass = "";

            pass = db.GetSingleValue(query);

            string pass1 = Decryptpass(pass);
            if (pass1 == password)
            {
                return 1;
            }
            else
            {
                return 0;
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
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
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
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "New password is required.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                // 1. Check old password
                int isValid = IsValidUser(username, oldPassword);

                if (isValid != 1)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Old password is incorrect.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                // 2. Encrypt new password
                string encryptedPassword = Encryptpass(newPassword);

                // Escape username/password before SQL
                string safeUsername = username.Replace("'", "''");
                string safePassword = encryptedPassword.Replace("'", "''");

                // 3. Update password
                string query = $@"
            UPDATE ""{sDBName}"".""TEC_OUSR""
            SET
                ""Password"" = '{safePassword}',
                ""Confirm_Password"" = '{safePassword}'
            WHERE
                ""User_Name"" = '{safeUsername}'
                OR ""User_Mail_Id"" = '{safeUsername}'";

                db.ExecuteNonQuery(query);

                // 4. Success
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
                // logger.LogError(ex, "Password reset failed.");

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
            string functionName = "Get_List";
            // Log.Information($"Starting the function", functionName);

            var result = new List<Dictionary<string, object>>();

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

                string query = @$"CALL ""{sDBName}"".""{spName}"" ()";
                using var connection = new OdbcConnection(_connection.ConnectionString);
                {
                    await connection.OpenAsync();

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;


                        using (var reader = await command.ExecuteReaderAsync())
                        {
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

                return ApiResponseUtility.GenerateApiResponse(ApiStatusEnum.Success, "Retrieved successfully", result);
            }
            catch (Exception ex)
            {
                //Log.Error($"Exception: {ex.Message}", functionName);
                throw;
            }
        }



        public async Task<List<DeportmentResponse>> Department()
        {
            const string functionName = "Department";
            //   Log.Information("Starting function {FunctionName}", functionName);
            const string spName = "Get_Department";
            string query = @$"CALL ""{sDBName}"".""{spName}"" ()";

            // Log.Debug("SQL Query for {FunctionName}: {Query}", functionName, query);

            List<DeportmentResponse> RejDetailsList = new();

            try
            {
                using var connection = new OdbcConnection(sConstr);
                // Log.Debug("Opening ODBC connection...");
                connection.Open();

                using var cmd = new OdbcCommand(query, connection);

                // Log.Debug("Executing SQL query...");
                using var reader = await cmd.ExecuteReaderAsync();

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

                //Log.Information(
                //    "{FunctionName} executed successfully. Total records loaded: {Count}",
                //    functionName, cusDetailsList.Count
                //);

                return RejDetailsList;
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Error in {FunctionName}. Message: {Message}", functionName, ex.Message);
                return new List<DeportmentResponse>();
            }
            finally
            {
                // Log.Information("Ending function {FunctionName}", functionName);
            }
        }



        //public async Task<ApiResponse> SaveDepartment(DepartmentRequest departmentRequest)
        //{
        //    try
        //    {
        //        if (departmentRequest == null)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = ApiStatusEnum.Failure,
        //                Message = "Invalid department request.",
        //                Data = null
        //            };
        //        }
        //        int active = departmentRequest.Active == true ? 1 : 0;
        //        string query = $@"
        //    INSERT INTO ""{sDBName}"".""Department""
        //    (
        //        ""DepartmentID"",
        //        ""DepartmentName"",
        //        ""IsActive""
        //    )
        //    VALUES
        //    (
        //        '{departmentRequest.DepartmentID}',
        //        '{departmentRequest.DepartmentName}',
        //        '{active}'
        //    )";

        //        db.ExecuteNonQuery(query);

        //        return new ApiResponse
        //        {
        //            Status = ApiStatusEnum.Success,
        //            Message = "Department Saved Successfully.",
        //            Data = null
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Status = ApiStatusEnum.Failure,
        //            Message = $"Error while saving department: {ex.Message}",
        //            Data = null
        //        };
        //    }
        //}

        public async Task<ApiResponse> SaveDepartment(DepartmentRequest departmentRequest)
        {
            try
            {
                if (departmentRequest == null)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid department request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                bool active = departmentRequest.Active == true;

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

                db.ExecuteNonQuery(query);

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
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving department: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse> CommonDelete(DeleteRequest deleteRequest)
        {
            try
            {
                if (deleteRequest == null || string.IsNullOrWhiteSpace(deleteRequest.ID))
                {
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

                using var connection = new OdbcConnection(sConstr);
                await connection.OpenAsync();

                using var cmd = new OdbcCommand(query, connection);

                cmd.Parameters.AddWithValue(
                    "@DepartmentID",
                    deleteRequest.ID
                );

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Department not found.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

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
                // logger.LogError(ex, "Error while deleting department");

                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while deleting department: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }



        public async Task<ApiResponse> SaveUserDetails(UserRequest userRequest)
        {
            try
            {
                if (userRequest == null)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid userRequest request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                bool active = userRequest.Active == true;
                string pass = Encryptpass(userRequest.Password);
                string conpass = Encryptpass(userRequest.ConfirmPassword);
                string query = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Name"" = '{userRequest.UserName}'";
                string query1 = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Mail_Id"" = '{userRequest.UserMail}'";

                string userCount = db.GetSingleValue(query);
                string userCount1 = db.GetSingleValue(query1);

                if (Convert.ToInt32(userCount) > 0)
                {
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
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "UserMail already Exist!.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }
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

                db.ExecuteNonQuery(query3);

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
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving User: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }


        public async Task<List<UserResponse>> GetUserDetails(string UserID)
        {
            const string functionName = "GetUserDetails";
            const string spName = "TEC_Editing";

            string query = $@"CALL ""{sDBName}"".""{spName}"" (?, ?)";

            List<UserResponse> userDetailsList = new();

            try
            {
                using var connection = new OdbcConnection(sConstr);
                await connection.OpenAsync();

                using var cmd = new OdbcCommand(query, connection);

                // ODBC parameters are positional
                cmd.Parameters.AddWithValue("@Type", "EditUser");
                cmd.Parameters.AddWithValue("@UserID", UserID);

                using var reader = await cmd.ExecuteReaderAsync();

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

                    userDetailsList.Add(detail);
                }

                return userDetailsList;
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Error in {FunctionName}: {Message}",
                //     functionName, ex.Message);

                return new List<UserResponse>();
            }
        }



        public async Task<ApiResponse> Update(UserRequest userRequest)
        {
            try
            {
                if (userRequest == null)
                {
                    return new ApiResponse
                    {
                        Status = ApiStatusEnum.Failure,
                        Message = "Invalid userRequest request.",
                        ErrorCode = ErrorCodeEnum.Failure,
                        Data = null
                    };
                }

                bool active = userRequest.Active == true;
                string pass = Encryptpass(userRequest.Password);
                string conpass = Encryptpass(userRequest.ConfirmPassword);

                string query = $@"SELECT ""User_Name"" FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Mail_Id"" = '{userRequest.UserMail}'";


                string existingUsername = db.GetSingleValue(query);
                string query3 = "";
                if (userRequest.UserName == existingUsername)
                {
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
                    string query4 = $@"SELECT COUNT(*) FROM ""{sDBName}"".""TEC_OUSR"" WHERE ""User_Name"" = '{userRequest.UserName}'";
                    string userCount = db.GetSingleValue(query4);
                    if (Convert.ToInt32(userCount) > 0)
                    {
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

                db.ExecuteNonQuery(query3);

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
                return new ApiResponse
                {
                    Status = ApiStatusEnum.Failure,
                    Message = $"Error while saving User: {ex.Message}",
                    ErrorCode = ErrorCodeEnum.Failure,
                    Data = null
                };
            }
        }

    }



}

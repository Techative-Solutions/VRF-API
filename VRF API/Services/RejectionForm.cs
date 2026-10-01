using Serilog;
using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Text;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using static VRF_API.Model.ResponseModel.EnumResponse;
using static VRF_API.Repository.CommonRepo;


namespace VRF_API.Services
{

    public interface IRejectionForm
    {
        Task<List<RejectedResponse>> RejectedDetails(string UserNme);
    }
    public class RejectionForm : IRejectionForm
    {
        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly DbConnection db;
        private readonly string sConstr;
        private readonly Repository.Log log;
        private readonly string connectionString;
        public RejectionForm(IConfiguration configuration, OdbcConnection connection, DbConnection _db, Repository.Log _log)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
            log = _log;
            connectionString = _configuration.GetValue<string>("ConnectionStrings:HanaOdbc") ?? string.Empty;
        }

        public async Task<List<RejectedResponse>> RejectedDetails(string UserNme)
        {
            const string functionName = "RejectedDetails";
            const string spName = "TEC_GetEdit_RejectedDetails";
            log.WriteToLogFile_Debug(
        $"[{functionName}] [START] - Rejected details retrieval started.",
        functionName
    );
            log.WriteToLogFile_Debug(
           $"[{functionName}] [REQUEST] - Rejected details request received. " +
           $"UserName: {UserNme}",
           functionName
       );

            string query = @$"CALL ""{sDBName}"".""{spName}"" (?)";



            List<RejectedResponse> RejDetailsList = new();

            try
            {
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
                cmd.Parameters.AddWithValue("@UserName", UserNme);

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
                    var detail = new RejectedResponse
                    {
                        TradeName = reader["TName"] == DBNull.Value ? null : reader["TName"].ToString(),
                        BusinessState = reader["Bstate"] == DBNull.Value ? null : reader["Bstate"].ToString(),
                        NatureOfBusiness = reader["NatureOfBusinessActivity"] == DBNull.Value ? null : reader["NatureOfBusinessActivity"].ToString(),
                        GstNumber = reader["GstNo"] == DBNull.Value ? null : reader["GstNo"].ToString(),
                        AppliedDate = reader["DateOfEstablishment"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["DateOfEstablishment"]).ToString("dd-MM-yyyy"),

                        RejectedDate = reader["RejectedDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["RejectedDate"]).ToString("dd-MM-yyyy"),
                        RejectedReason = reader["RejectedReason"] == DBNull.Value ? null : reader["RejectedReason"].ToString(),

                    };

                    RejDetailsList.Add(detail);
                }

                log.WriteToLogFile_Debug(
          $"[{functionName}] [SUCCESS] - Rejected details retrieved successfully. " +
          $"Total records: {RejDetailsList.Count}",
          functionName
      );
                return RejDetailsList;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
            $"[{functionName}] [EXCEPTION] - Error while retrieving rejected details. " +
            $"UserName: {UserNme} | " +
            $"SPName: {spName} | " +
            $"Message: {ex.Message} | " +
            $"StackTrace: {ex.StackTrace}",
            functionName
        );

                return new List<RejectedResponse>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
           $"[{functionName}] [END] - Rejected details retrieval ended. " +
           $"Total records: {RejDetailsList.Count}",
           functionName
       );
            }
        }

//        public async Task<SubmitVendorResult> SubmitVendor(SubmitVendorRequest request)
//        {
//            const string functionName = "SubmitVendor";
//            log.WriteToLogFile_Debug(
//                "[VendorCreation] [SubmitVendor] [START] - Submit vendor started",
//                "SubmitVendor");

//            if (request == null)
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Submit request is null.",
//                    functionName
//                );

//                throw new Exception("Invalid submit request.");
//            }

//            if (request.FormData == null)
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Vendor form data is missing.",
//                    functionName
//                );

//                throw new Exception("Vendor form data is required.");
//            }

//            log.WriteToLogFile_Debug(
//                $"[{functionName}] [REQUEST] - Request type: " +
//                $"ExistingVendor: {request.IsDraftApproved} | " +
//                $"OtpValid: {request.OtpValid}",
//                functionName
//            );




//            var model = request.FormData;

//            string gstNumber = model.GstNumber?.Trim().ToUpper() ?? "";
//            string email = model.Email?.Trim() ?? "";

//            log.WriteToLogFile_Debug(
//                $"[{functionName}] [REQUEST] - Vendor submission details received. " +
//                $"GST: {gstNumber} | " +
//                $"TradeName: {model.TradeName} | " +
//                $"Email: {email} | " +
//                $"Mobile: {model.MobileNumber}",
//                functionName
//            );

//            if (string.IsNullOrWhiteSpace(gstNumber))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - GST Number is required.",
//                    functionName
//                );

//                throw new Exception("GST Number is required.");
//            }

//            if (gstNumber.Length != 15)
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - GST Number length is invalid.",
//                    functionName
//                );

//                throw new Exception("GSTNO must be 15 character.");
//            }

//            if (string.IsNullOrWhiteSpace(model.TradeName))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Trade Name is required.",
//                    functionName
//                );

//                throw new Exception("Trade Name is required.");
//            }

//            if (string.IsNullOrWhiteSpace(email))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Email is required.",
//                    functionName
//                );

//                throw new Exception("Email is required.");
//            }

//            if (string.IsNullOrWhiteSpace(model.MobileNumber))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Mobile Number is required.",
//                    functionName
//                );

//                throw new Exception("Mobile Number is required.");
//            }


//            if (model.RegisteredOffice == null ||
//            string.IsNullOrWhiteSpace(model.RegisteredOffice.Address1) ||
//            string.IsNullOrWhiteSpace(model.RegisteredOffice.Country))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Registered Office Address or Country is missing.",
//                    functionName
//                );

//                throw new Exception("Registered Office Address and Country are required.");
//            }

//            if (model.BillingAddress == null ||
//                string.IsNullOrWhiteSpace(model.BillingAddress.Address1) ||
//                string.IsNullOrWhiteSpace(model.BillingAddress.Country))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Billing Address or Country is missing.",
//                    functionName
//                );

//                throw new Exception("Billing Address and Country are required.");
//            }

//            if (model.BankDetails == null ||

//                string.IsNullOrWhiteSpace(model.BankDetails.AccountNameHolder) ||
//                string.IsNullOrWhiteSpace(model.BankDetails.AccountNumber) ||
//                string.IsNullOrWhiteSpace(model.BankDetails.IfscCode))
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [VALIDATION_FAILED] - Required bank details are missing.",
//                    functionName
//                );

//                throw new Exception(
//                    "Bank Name, Account Name, Account Number and IFSC Code are required."
//                );
//            }

//            log.WriteToLogFile_Debug(
//                $"[{functionName}] [VALIDATION] - Vendor request validation completed successfully.",
//                functionName
//            );
         

//            log.WriteToLogFile_Debug(
//         $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
//         functionName
//     );
//            using var connection = new OdbcConnection(connectionString);
//            await connection.OpenAsync();

//            log.WriteToLogFile_Debug(
//                $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
//                functionName
//            );
//            using var transaction = connection.BeginTransaction();
//            log.WriteToLogFile_Debug(
//           $"[{functionName}] [TRANSACTION] - Database transaction started.",
//           functionName
//       );

//            try
//            {
//                log.WriteToLogFile_Debug(
//         $"[{functionName}] [DATABASE] - Checking whether GST is already registered in a non-draft record.",
//         functionName
//     );

//                int existingId = 0;

                

//                string existingDraftQuery = $@"
//    SELECT ""Id""
//    FROM ""{sDBName}"".""TEC_OLED""
//    WHERE ""GstNo"" = ?";

//                using (var command = new OdbcCommand(existingDraftQuery, connection, transaction))
//                {
//                    command.Parameters.AddWithValue("@GstNo", gstNumber);
//                    var result = await command.ExecuteScalarAsync();

//                    if (result != null && result != DBNull.Value)
//                    {
//                        existingId = Convert.ToInt32(result);

//                        log.WriteToLogFile_Debug(
//                            $"[{functionName}] [DATABASE] - Existing draft found. " +
//                            $"DraftId: {existingId}",
//                            functionName
//                        );
//                    }
//                    else
//                    {
//                        log.WriteToLogFile_Debug(
//                            $"[{functionName}] [DATABASE] - No existing draft found.",
//                            functionName
//                        );
//                    }
//                }

//                int id = existingId;

//                if (id == 0)
//                {
//                    log.WriteToLogFile_Debug(
//                $"[{functionName}] [DATABASE] - No existing draft found. Generating new vendor ID.",
//                functionName
//            );

//                    string nextIdQuery = $@"
//                        SELECT IFNULL(MAX(""Id""), 0) + 1
//                        FROM ""{sDBName}"".""TEC_OLED""";

//                    using var idCommand = new OdbcCommand(
//                        nextIdQuery,
//                        connection,
//                        transaction);

//                    id = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
//                    log.WriteToLogFile_Debug(
//              $"[{functionName}] [DATABASE] - New vendor ID generated. " +
//              $"Id: {id}",
//              functionName
//          );
//                }

//                // IMPORTANT:
//                // These are FINAL-SUBMISSION methods.
//                // They do not contain draft/update branching.
//                // If a draft already exists, all draft child rows are
//                // removed first and then the complete final data is inserted.
//                if (existingId > 0)
//                {
//                    log.WriteToLogFile_Debug(
//              $"[{functionName}] [DATABASE] - Removing existing draft child data. " +
//              $"Id: {existingId}",
//              functionName
//          );
//                    await DeleteSubmissionChildData(
//                        connection,
//                        transaction,
//                        existingId);
//                    log.WriteToLogFile_Debug(
//    $"[{functionName}] [DATABASE] - Existing draft child data removed successfully. " +
//    $"Id: {existingId}",
//    functionName
//);

//                    log.WriteToLogFile_Debug(
//                        $"[{functionName}] [DATABASE] - Updating existing vendor header details. " +
//                        $"Id: {existingId}",
//                        functionName
//                    );


//                    await SubmitUpdateHeaderDetails(
//                        connection,
//                        transaction,
//                        existingId,
//                        model);
//                    log.WriteToLogFile_Debug(
//              $"[{functionName}] [DATABASE] - Existing vendor header updated successfully. " +
//              $"Id: {existingId}",
//              functionName
//          );
//                }
//                else
//                {
//                    log.WriteToLogFile_Debug(
//               $"[{functionName}] [DATABASE] - Inserting new vendor header details. " +
//               $"Id: {id}",
//               functionName
//           );
//                    await SubmitInsertHeaderDetails(
//                        connection,
//                        transaction,
//                        id,
//                        model);

//                    log.WriteToLogFile_Debug(
//                        $"[{functionName}] [DATABASE] - Vendor header inserted successfully. " +
//                        $"Id: {id}",
//                        functionName
//                    );
//                }

//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [DATABASE] - Saving payment details. " +
//                    $"Id: {id}",
//                    functionName
//                );
//                await SubmitInsertPaymentDetails(
//                    connection,
//                    transaction,
//                    id,
//                    model.PaymentDetails);
//                log.WriteToLogFile_Debug(
//          $"[{functionName}] [DATABASE] - Payment details saved successfully.",
//          functionName
//      );

//                log.WriteToLogFile_Debug(
//         $"[{functionName}] [DATABASE] - Saving other business locations. " +
//         $"Id: {id} | Count: {model.OtherBusinessLocations?.Count ?? 0}",
//         functionName
//     );

//                await SubmitInsertBusinessDetails(
//                    connection,
//                    transaction,
//                    id,
//                    model.OtherBusinessLocations);

//                // React: formData.businessPartners -> TEC_LED2
//                log.WriteToLogFile_Debug(
//         $"[{functionName}] [DATABASE] - Saving business partner details. " +
//         $"Id: {id} | Count: {model.BusinessPartners?.Count ?? 0}",
//         functionName
//     );
//                await SubmitInsertPartnerDetails(
//                    connection,
//                    transaction,
//                    id,
//                    model.BusinessPartners);
//                log.WriteToLogFile_Debug(
//           $"[{functionName}] [DATABASE] - Saving operational contacts. " +
//           $"Id: {id} | Count: {model.OperationalContacts?.Count ?? 0}",
//           functionName
//       );
//                await SubmitInsertOperationalContacts(
//                    connection,
//                    transaction,
//                    id,
//                    model.OperationalContacts);
//                log.WriteToLogFile_Debug(
//        $"[{functionName}] [DATABASE] - Saving major goods/services. " +
//        $"Id: {id} | Count: {model.MajorGoodsServices?.Count ?? 0}",
//        functionName
//    );
//                await SubmitInsertMajorGoodsServices(
//                    connection,
//                    transaction,
//                    id,
//                    model.MajorGoodsServices);
//                log.WriteToLogFile_Debug(
//       $"[{functionName}] [DATABASE] - Saving major customers. " +
//       $"Id: {id} | Count: {model.MajorCustomers?.Count ?? 0}",
//       functionName
//   );

//                await SubmitInsertMajorCustomers(
//                    connection,
//                    transaction,
//                    id,
//                    model.MajorCustomers);
//                log.WriteToLogFile_Debug(
//           $"[{functionName}] [DATABASE] - Saving other information. " +
//           $"Id: {id}",
//           functionName
//       );

//                await SubmitInsertOtherInformation(
//                    connection,
//                    transaction,
//                    id,
//                    model.OtherInformation);


//                await SubmitInsertDocuments(
//                    connection,
//                    transaction,
//                    id,
//                    model,
//                    request.UploadedFiles);
//                log.WriteToLogFile_Debug(
//          $"[{functionName}] [DATABASE] - Vendor documents saved successfully.",
//          functionName
//      );

//                transaction.Commit();

//                log.WriteToLogFile_Debug(
//                    $"[VendorCreation] [SubmitVendor] [DB_END] - Final submission committed. Id: {id}",
//                    "SubmitVendor");

//                try
//                {
//                    log.WriteToLogFile_Debug(
//               $"[{functionName}] [MAIL] - Preparing vendor submission email. " +
//               $"Id: {id} | Recipient: {email}",
//               functionName
//           );

//                    if (!request.OtpValid)
//                    {
//                        await SentMail(request.FormData, email, request.FormData.PaymentDetails.AgencyEmail);
//                    }

//                    log.WriteToLogFile_Debug(
//                        $"[{functionName}] [MAIL] - Vendor submission email sent successfully. " +
//                        $"Id: {id}",
//                        functionName
//                    );
//                }
//                catch (Exception mailEx)
//                {
//                    log.WriteToLogFile_Debug(
//                $"[{functionName}] [MAIL_ERROR] - Vendor submission committed, " +
//                $"but email sending failed. " +
//                $"Id: {id} | " +
//                $"Message: {mailEx.Message} | " +
//                $"StackTrace: {mailEx.StackTrace}",
//                functionName
//            );
//                }

//                return new SubmitVendorResult
//                {
//                    Success = true,
//                    Id = id,
//                    GstNumber = gstNumber,
//                    Message = "You have successfully submitted the form. Our representative will reach out to you soon. Please use your GSTIN as the reference number."
//                };
//            }
//            catch (Exception ex)
//            {
//                // ---------------------------
//                // ROLLBACK
//                // ---------------------------

//                if (transaction != null)
//                {
//                    try
//                    {
//                        log.WriteToLogFile_Debug(
//                            $"[{functionName}] [TRANSACTION] - Rolling back vendor submission transaction.",
//                            functionName
//                        );

//                        transaction.Rollback();

//                        log.WriteToLogFile_Debug(
//                            $"[{functionName}] [TRANSACTION] - Transaction rolled back successfully.",
//                            functionName
//                        );
//                    }
//                    catch (Exception rollbackEx)
//                    {
//                        log.WriteToLogFile_Debug(
//                            $"[{functionName}] [ROLLBACK_ERROR] - Error while rolling back transaction. " +
//                            $"Message: {rollbackEx.Message}",
//                            functionName
//                        );
//                    }
//                }

//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [EXCEPTION] - Error while submitting vendor. " +
//                    $"GST: {request?.FormData?.GstNumber} | " +
//                    $"Email: {request?.FormData?.Email} | " +
//                    $"Message: {ex.Message} | " +
//                    $"StackTrace: {ex.StackTrace}",
//                    functionName
//                );

//                throw;
//            }
//            finally
//            {
//                log.WriteToLogFile_Debug(
//                    $"[{functionName}] [END] - Submit vendor process ended.",
//                    functionName
//                );
//            }
//        }


    }
}

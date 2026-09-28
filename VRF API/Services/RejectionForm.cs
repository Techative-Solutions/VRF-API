using Serilog;
using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Text;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using static VRF_API.Model.ResponseModel.EnumResponse;


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
        public RejectionForm(IConfiguration configuration, OdbcConnection connection, DbConnection _db)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
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



    }
}

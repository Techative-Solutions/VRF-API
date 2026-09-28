 using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Text;
using VRF_API.Model.ResponseModel;
using VRF_API.Repository;
using VRF_API.Utilities;
using static VRF_API.Model.ResponseModel.EnumResponse;


namespace VRF_API.Services
{

    public interface IReport
    {
        Task<List<Reports>> ReportName();
        Task<List<Dictionary<string, object>>> GetReportAsync(ReportRequest request);
    }
    public class Report: IReport
    {
        private readonly Repository.Log log;
        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string sDBName;
        private readonly string _anotherDbName;
        private readonly DbConnection db;
        private readonly string sConstr;

        public Report(IConfiguration configuration, OdbcConnection connection, DbConnection _db)
        {
            _configuration = configuration;
            _connection = connection;
            sDBName = _configuration["HanaSettings:DBName"];
            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            db = _db;
        }

        public async Task<List<Reports>> ReportName()
        {
            const string functionName = "ReportName";
              log.WriteToLogFile_Debug("Starting function {FunctionName}", functionName);
            const string spName = "SP_GetReportTypes";
            string query = @$"CALL ""{sDBName}"".""{spName}"" ()";


            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Report name retrieval started.",
                functionName
            );


            List<Reports> RejDetailsList = new();

            try
            {
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
                    var detail = new Reports
                    {
                        ReportName = reader["ReportName"] == DBNull.Value ? null : reader["ReportName"].ToString(),
                       

                    };

                    RejDetailsList.Add(detail);
                }

                log.WriteToLogFile_Debug(
           $"[{functionName}] [SUCCESS] - Report names retrieved successfully. " +
           $"Total records: {RejDetailsList.Count}",
           functionName
       );


                return RejDetailsList;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
               $"[{functionName}] [EXCEPTION] - Error while retrieving report names. " +
               $"SPName: {spName} | " +
               $"Message: {ex.Message} | " +
               $"StackTrace: {ex.StackTrace}",
               functionName
           );
                return new List<Reports>();
            }
            finally
            {
                log.WriteToLogFile_Debug(
             $"[{functionName}] [END] - Report name retrieval ended. " +
             $"Total records: {RejDetailsList.Count}",
             functionName
         );
            }
        }

        public async Task<List<Dictionary<string, object>>> GetReportAsync(ReportRequest request)
        {
            const string functionName = "GetReportAsync";
            const string spName = "SP_GetReportData";

            var result = new List<Dictionary<string, object>>();

            log.WriteToLogFile_Debug(
                $"[{functionName}] [START] - Report data retrieval started.",
                functionName
            );



            try
            {
                log.WriteToLogFile_Debug(
          $"[{functionName}] [REQUEST] - Report request received. " +
          $"UserName: {request?.UserName} | " +
          $"ReportName: {request?.ReportName} | " +
          $"FromDate: {request?.FromDate} | " +
          $"ToDate: {request?.ToDate}",
          functionName
      );

                using (OdbcConnection conn = new OdbcConnection(sConstr))
                {

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [CONNECTION] - Opening ODBC database connection.",
                        functionName
                    );
                    await conn.OpenAsync();

                    log.WriteToLogFile_Debug(
              $"[{functionName}] [CONNECTION] - ODBC database connection opened successfully.",
              functionName
          );

                    string procedureCall =
             $@"CALL ""{sDBName}"".""{spName}"" (?, ?, ?, ?)";

                    log.WriteToLogFile_Debug(
                        $"[{functionName}] [DATABASE] - Preparing stored procedure. " +
                        $"SPName: {spName}",
                        functionName
                    );


                    using (OdbcCommand cmd = new OdbcCommand(procedureCall, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("UserName", OdbcType.VarChar).Value = request.UserName ?? "";
                        cmd.Parameters.Add("ReportType", OdbcType.VarChar).Value = request.ReportName;
                        cmd.Parameters.Add("FromDate", OdbcType.VarChar).Value = request.FromDate;
                        cmd.Parameters.Add("ToDate", OdbcType.VarChar).Value = request.ToDate;



                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            log.WriteToLogFile_Debug(
           $"[{functionName}] [DATABASE] - Stored procedure executed successfully.",
           functionName
       );
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(
                                        reader.GetName(i),
                                        reader.IsDBNull(i) ? null : reader.GetValue(i)
                                    );
                                }

                                result.Add(row);
                            }
                        }
                    }
                }

                log.WriteToLogFile_Debug(
                    $"[{functionName}] [SUCCESS] - Report data retrieved successfully. " +
                    $"Total records: {result.Count}",
                    functionName
                );

                return result;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
         $"[{functionName}] [EXCEPTION] - Error while retrieving report data. " +
         $"SPName: {spName} | " +
         $"Message: {ex.Message} | " +
         $"StackTrace: {ex.StackTrace}",
         functionName
     );

                throw new Exception("Error while fetching report data from HANA.", ex);
            }

            finally
            {
                log.WriteToLogFile_Debug(
                    $"[{functionName}] [END] - Report data retrieval ended. " +
                    $"Total records: {result.Count}",
                    functionName
                );
            }
        }



        }
}

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
            //   Log.Information("Starting function {FunctionName}", functionName);
            const string spName = "SP_GetReportTypes";
            string query = @$"CALL ""{sDBName}"".""{spName}"" ()";

            // Log.Debug("SQL Query for {FunctionName}: {Query}", functionName, query);

            List<Reports> RejDetailsList = new();

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
                    var detail = new Reports
                    {
                        ReportName = reader["ReportName"] == DBNull.Value ? null : reader["ReportName"].ToString(),
                       

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
                return new List<Reports>();
            }
            finally
            {
                // Log.Information("Ending function {FunctionName}", functionName);
            }
        }

        public async Task<List<Dictionary<string, object>>> GetReportAsync(ReportRequest request)
        {
            var result = new List<Dictionary<string, object>>();

            try
            {
                using (OdbcConnection conn = new OdbcConnection(sConstr))
                {
                    await conn.OpenAsync();

                    // Build fully qualified procedure name
                    string procedureCall =
                        $"CALL \"{sDBName}\".\"SP_GetReportData\"(?, ?, ?, ?)";

                    using (OdbcCommand cmd = new OdbcCommand(procedureCall, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("UserName", OdbcType.VarChar).Value = request.UserName ?? "";
                        cmd.Parameters.Add("ReportType", OdbcType.VarChar).Value = request.ReportName;
                        cmd.Parameters.Add("FromDate", OdbcType.VarChar).Value = request.FromDate;
                        cmd.Parameters.Add("ToDate", OdbcType.VarChar).Value = request.ToDate;
                       

          
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching report data from HANA.", ex);
            }

            return result;
        }


      
    }
}

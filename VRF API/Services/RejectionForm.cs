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
            //   Log.Information("Starting function {FunctionName}", functionName);
            const string spName = "TEC_GetEdit_RejectedDetails";
            string query = @$"CALL ""{sDBName}"".""{spName}"" (?)";

            // Log.Debug("SQL Query for {FunctionName}: {Query}", functionName, query);

            List<RejectedResponse> RejDetailsList = new();

            try
            {
                using var connection = new OdbcConnection(sConstr);
                // Log.Debug("Opening ODBC connection...");
                connection.Open();

                using var cmd = new OdbcCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserName", UserNme);


                // Log.Debug("Executing SQL query...");
                using var reader = await cmd.ExecuteReaderAsync();

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

                //Log.Information(
                //    "{FunctionName} executed successfully. Total records loaded: {Count}",
                //    functionName, cusDetailsList.Count
                //);

                return RejDetailsList;
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Error in {FunctionName}. Message: {Message}", functionName, ex.Message);
                return new List<RejectedResponse>();
            }
            finally
            {
                // Log.Information("Ending function {FunctionName}", functionName);
            }
        }



    }
}

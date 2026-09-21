using Microsoft.Data.SqlClient;
using Sap.Data.Hana;
using Serilog;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
namespace VRF_API.Repository
{
    public class DbConnection
    {
        private readonly Log log;
        private readonly IConfiguration _configuration;

        private readonly string sServer;
        private readonly string sDBUser;
        private readonly string sDBPwd;

        public string sDBName { get; }

        public int fSize = 5;

        public string sConstr { get; }

        public static long lretcode;

        SqlConnection sqlCon;
        SqlConnection sqlSAPCon;
        SqlCommand cmd;
        SqlDataAdapter sa;
        DataTable dtlog;
        DataSet dslog;

        public DbConnection(
            Log _log,
            IConfiguration configuration)
        {
            log = _log;
            _configuration = configuration;

            sServer = _configuration["HanaSettings:Server"];

            sDBUser = _configuration["HanaSettings:DBUser"];

            sDBPwd = _configuration["HanaSettings:DBPwd"];

            sDBName = _configuration["HanaSettings:DBName"];

            sConstr =
                $"Driver={{HDBODBC}};" +
                $"UID={sDBUser};" +
                $"PWD={sDBPwd};" +
                $"DATABASENAME={sDBName};" +
                $"SERVERNODE={sServer};";
        }
        public string GetSingleValue(string sQuery)
        {
            log.WriteToLogFile_Debug("[DBConnection] [GetSingleValue] [START] - Fetching single value", "GetSingleValue");
            HanaConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            string sSingleValue = string.Empty;

            try
            {
                string SAP_Constr = sConstr;
                SAP_Con = new HanaConnection(SAP_Constr);
                SAP_Con.Open();
                HanaCommand SAP_Cmd = new HanaCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                HanaDataAdapter SAP_da = new HanaDataAdapter();
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
        public DataTable ExecuteQueryForDataTable(string sQuery)
        {
            String sFuncName = "HanaExecuteQueryReturnDataTable";
            log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [START] - Executing query for DataTable", sFuncName);
            HanaConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            try
            {
                // log.WriteToLogFile_Debug("Starting the function", sFuncName);
                string SAP_Constr = sConstr;
                SAP_Con = new HanaConnection(SAP_Constr);
                SAP_Con.Open();
                HanaCommand SAP_Cmd = new HanaCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                HanaDataAdapter SAP_da = new HanaDataAdapter();
                SAP_da.SelectCommand = SAP_Cmd;
                SAP_da.Fill(dt);
                //log.WriteToLogFile_Debug("Completed the function successfully", sFuncName);
                log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [DB_END] - Query executed successfully, filled " + dt.Rows.Count + " rows", sFuncName);
                return dt;
            }
            catch (Exception ex)
            {
                // log.WriteToLogFile_Debug(ex.Message, sFuncName);
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
    }
}

using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Data;
using System.Data.Odbc;
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
        private readonly IConverter _converter;

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
            IConfiguration configuration, IConverter converter)
        {
            log = _log;
            _configuration = configuration;

            sServer = _configuration["HanaSettings:Server"];

            sDBUser = _configuration["HanaSettings:DBUser"];

            sDBPwd = _configuration["HanaSettings:DBPwd"];

            sDBName = _configuration["HanaSettings:DBName"];

            sConstr = _configuration["ConnectionStrings:HanaOdbc"];
            _converter = converter;
            
        }
        public string GetSingleValue(string sQuery)
        {
            log.WriteToLogFile_Debug("[DBConnection] [GetSingleValue] [START] - Fetching single value", "GetSingleValue");
            OdbcConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            string sSingleValue = string.Empty;

            try
            {
                string SAP_Constr = sConstr;
                SAP_Con = new OdbcConnection(SAP_Constr);
                SAP_Con.Open();
                OdbcCommand SAP_Cmd = new OdbcCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                OdbcDataAdapter SAP_da = new OdbcDataAdapter();
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


        public string ExecuteNonQuery(string sQuery)
        {
            string SAP_Constr = sConstr;

            using (OdbcConnection oCon = new OdbcConnection(SAP_Constr))
            using (OdbcCommand oCmd = new OdbcCommand())
            {
                try
                {
                    oCmd.CommandType = CommandType.Text;
                    oCmd.CommandText = sQuery;
                    oCmd.Connection = oCon;
                    oCmd.CommandTimeout = 0;

                    oCon.Open();

                    oCmd.ExecuteNonQuery();

                    return "Success";
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public DataTable ExecuteQueryForDataTable(string sQuery)
        {
            String sFuncName = "HanaExecuteQueryReturnDataTable";
            log.WriteToLogFile_Debug("[DBConnection] [ExecuteQueryForDataTable] [START] - Executing query for DataTable", sFuncName);
            OdbcConnection SAP_Con = null/* TODO Change to default(_) if this is not a reference type */;
            DataTable dt = new DataTable();
            try
            {
                // log.WriteToLogFile_Debug("Starting the function", sFuncName);
                string SAP_Constr = sConstr;
                SAP_Con = new OdbcConnection(SAP_Constr);
                SAP_Con.Open();
                OdbcCommand SAP_Cmd = new OdbcCommand();
                SAP_Cmd.CommandType = CommandType.Text;
                SAP_Cmd.CommandText = sQuery;
                SAP_Cmd.Connection = SAP_Con;
                SAP_Cmd.CommandTimeout = 0;
                if (SAP_Con.State == ConnectionState.Closed)
                    SAP_Con.Open();
                OdbcDataAdapter SAP_da = new OdbcDataAdapter();
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
        public string GenerateVendorHtmlWithData(
   Dictionary<string, object> data,
   IEnumerable<object> goodsList)
        {
            string GetValue(string key)
            {
                if (data == null || !data.ContainsKey(key))
                    return "";

                return data[key]?.ToString() ?? "";
            }

            string HtmlEncode(string value)
            {
                return System.Net.WebUtility.HtmlEncode(
                    value ?? ""
                );
            }

            string vendorName =
                HtmlEncode(GetValue("Trade Name"));

            string billingAddress =
                HtmlEncode(GetValue("Billing Address"));

            string registeredAddress =
                HtmlEncode(GetValue("Registered Address"));

            string natureOfBusiness =
                HtmlEncode(GetValue("Nature of Business"));

            string mobileNumber =
                HtmlEncode(GetValue("Mobile Number"));

            string officeTelephone =
                HtmlEncode(GetValue("Office Telephone"));

            string email =
                HtmlEncode(GetValue("Email ID"));

            string agencyEmail =
                HtmlEncode(GetValue("Agency Email"));

            string contactPerson =
                HtmlEncode(GetValue("Contact Person"));

            string bankName =
                HtmlEncode(GetValue("Bank Name"));

            string accountNumber =
                HtmlEncode(GetValue("Account Number"));

            string ifscCode =
                HtmlEncode(GetValue("IFSC Code"));

            string goodsReturnAddress =
                HtmlEncode(GetValue("Goods Return Address"));

            string creditDays =
                HtmlEncode(GetValue("Credit Days"));

            string discount =
                HtmlEncode(GetValue("Discount"));

            string gstNumber =
                HtmlEncode(GetValue("GST Number"));

            string panNumber =
                HtmlEncode(GetValue("PAN Number"));

            string msmeNumber =
                HtmlEncode(GetValue("MSME Number"));

            string enterpriseType =
                HtmlEncode(GetValue("Enterprise Type"));

            string majorActivity =
                HtmlEncode(GetValue("Major Activity"));

            string legalName =
                HtmlEncode(GetValue("Legal Name"));

            string businessType =
                HtmlEncode(GetValue("Business Type"));

            string nhfsContactPerson =
                HtmlEncode(GetValue("NHFS Contact Person"));

            string formDate =
                HtmlEncode(GetValue("Date"));

            // ------------------------------------------------------------
            // GOODS TABLE
            // ------------------------------------------------------------

            StringBuilder goodsHtml =
                new StringBuilder();

            int goodsIndex = 1;

            if (goodsList != null)
            {
                foreach (var item in goodsList)
                {
                    if (item == null)
                        continue;

                    string description = "";
                    string category = "";
                    string code = "";

                    // Supports your existing model without forcing
                    // one exact property structure here.
                    var type = item.GetType();

                    var property =
                        type.GetProperty("Description");

                    if (property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Goods");

                    if (string.IsNullOrWhiteSpace(description) &&
                        property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Name");

                    if (string.IsNullOrWhiteSpace(description) &&
                        property != null)
                    {
                        description =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("Category");

                    if (property != null)
                    {
                        category =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    property =
                        type.GetProperty("ItemCode");

                    if (property != null)
                    {
                        code =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    goodsHtml.Append($@"
                <tr>
                    <td style=""text-align:center;"">
                        {goodsIndex}
                    </td>

                    <td>
                        {HtmlEncode(description)}
                    </td>

                    <td>
                        {HtmlEncode(category)}
                    </td>

                    <td>
                        {HtmlEncode(code)}
                    </td>
                </tr>");

                    goodsIndex++;
                }
            }

            if (goodsIndex == 1)
            {
                goodsHtml.Append(@"
            <tr>
                <td colspan=""4"" style=""height:30px;"">
                    &nbsp;
                </td>
            </tr>");
            }

            // ------------------------------------------------------------
            // LOGO
            // ------------------------------------------------------------

            string logoPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Images",
                    "Logo.png"
                );

            string logoHtml = "";

            if (File.Exists(logoPath))
            {
                byte[] logoBytes =
                    File.ReadAllBytes(logoPath);

                string base64Logo =
                    Convert.ToBase64String(logoBytes);

                logoHtml =
                    $"data:image/png;base64,{base64Logo}";
            }

            // ------------------------------------------------------------
            // WATERMARK
            // ------------------------------------------------------------

            string watermark = "DRAFT";

            // ------------------------------------------------------------
            // FINAL HTML
            // ------------------------------------------------------------

            string html = $@"
<!DOCTYPE html>

<html>

<head>

<meta charset=""UTF-8"" />

<title>Vendor Registration Form</title>

<style>

    @page {{
        size: A4;
        margin: 0;
    }}

    html,
    body {{
        margin: 0;
        padding: 0;
        background: white;
    }}

    body {{
        font-family: ""Times New Roman"", serif;
        font-size: 12px;
    }}

    .page {{
        position: relative;

        width: 1094px;
        min-height: 1123px;

        margin: 0 auto;

        padding:
            25px
            35px
            25px
            35px;

        box-sizing: border-box;

        background: white;

        overflow: hidden;
    }}

    .page * {{
        position: relative;
        z-index: 1;
    }}

    .watermark {{
        position: absolute !important;

        top: 50% !important;
        left: 50% !important;

        transform:
            translate(-50%, -50%)
            rotate(-35deg) !important;

        width: 90%;

        text-align: center;

        font-size: 260px;

        font-weight: 900;

        font-family: ""Arial Black"", sans-serif;

        text-transform: uppercase;

        letter-spacing: 10px;

        color: rgba(0, 0, 0, 0.18);

        opacity: 0.25;

        pointer-events: none;

        user-select: none;

        z-index: 0 !important;

        white-space: nowrap;
    }}

    .logo-box {{
        width: 145px;
        height: 55px;

        border: 1px solid #000;

        display: flex;

        align-items: center;

        justify-content: center;

        margin-bottom: 5px;
    }}

    .logo {{
        width: 125px;
        height: auto;
    }}

    .header {{
        text-align: center;

        font-size: 13px;

        font-weight: bold;

        margin-bottom: 10px;
    }}

    h2 {{
        text-align: center;

        font-size: 18px;

        margin:
            8px
            0
            12px
            0;

        text-decoration: underline;
    }}

    .ref-table {{
        width: 100%;

        border-collapse: collapse;

        margin-bottom: 5px;
    }}

    .ref-table td {{
        width: 50%;

        vertical-align: top;

        padding: 0;
    }}

    .right-align {{
        text-align: right;
    }}

    .field-line {{
        display: flex;

        align-items: center;

        min-height: 22px;
    }}

    .field-line label {{
        width: 230px;

        text-align: left;

        font-weight: bold;

        flex-shrink: 0;
    }}

    .readonly-field {{
        display: inline-block;

        text-align: left;

        flex: 1;

        border-bottom: 1px solid #000;

        min-height: 16px;

        padding-left: 3px;
    }}

    .section-title {{
        font-weight: bold;

        margin-top: 8px;

        margin-bottom: 3px;
    }}

    table.data-table {{
        width: 100%;

        border-collapse: collapse;

        margin-top: 5px;

        margin-bottom: 8px;
    }}

    table.data-table th,
    table.data-table td {{
        border: 1px solid #000;

        padding: 4px;

        vertical-align: top;
    }}

    table.data-table th {{
        text-align: center;

        font-weight: bold;
    }}

    .signature {{
        margin-top: 30px;

        width: 100%;
    }}

    .signature-table {{
        width: 100%;

        border-collapse: collapse;
    }}

    .signature-table td {{
        width: 50%;

        height: 80px;

        vertical-align: bottom;

        padding: 5px;
    }}

    .signature-line {{
        border-top: 1px solid #000;

        width: 80%;

        margin-top: 35px;
    }}

    .small {{
        font-size: 11px;
    }}

</style>

</head>

<body>

<div class=""page"">

    <div class=""watermark"">
        {HtmlEncode(watermark)}
    </div>

    {(string.IsNullOrWhiteSpace(logoHtml)
                ? ""
                : $@"<div class=""logo-box"">
                <img
                    src=""{logoHtml}""
                    alt=""Logo""
                    class=""logo""
                />
            </div>")}

    <div class=""header"">
        No. 7, Basudev Street, Pondy Bazaar,
        T. Nagar, Chennai – 600 017
        Contact: 044 24340714
    </div>

    <h2>
        VENDOR REGISTRATION FORM
    </h2>

    <!-- REF / CODE / DATE / LOCATION -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>Ref. No.:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

                <div class=""field-line"">
                    <label>CODE NO.:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

            </td>

            <td class=""right-align"">

                <div class=""field-line"">
                    <label>Date:</label>
                    <span class=""readonly-field"">
                        {formDate}
                    </span>
                </div>

                <div class=""field-line"">
                    <label>LOCATION:</label>
                    <span class=""readonly-field"">
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- BASIC DETAILS -->

    <div class=""field-line"">
        <label>1. Name of Vendor:</label>
        <span class=""readonly-field"">
            {vendorName}
        </span>
    </div>

    <div class=""field-line"">
        <label>2. Address:</label>
        <span class=""readonly-field"">
            {billingAddress}
        </span>
    </div>

    <div class=""field-line"">
        <label>Registered Office:</label>
        <span class=""readonly-field"">
            {registeredAddress}
        </span>
    </div>

    <div class=""field-line"">
        <label>3. Nature of Business:</label>
        <span class=""readonly-field"">
            {natureOfBusiness}
        </span>
    </div>


    <!-- CONTACT -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>4. Contact No. 1:</label>
                    <span class=""readonly-field"">
                        {mobileNumber}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>Contact No. 2:</label>
                    <span class=""readonly-field"">
                        {officeTelephone}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <div class=""field-line"">
        <label>Email ID:</label>
        <span class=""readonly-field"">
            {email}
        </span>
    </div>

    <div class=""field-line"">
        <label>Contact Person:</label>
        <span class=""readonly-field"">
            {contactPerson}
        </span>
    </div>


    <!-- BANK -->

    <div class=""section-title"">
        RTGS / BANK DETAILS
    </div>

    <table class=""data-table"">

        <tr>
            <th>Bank Name</th>
            <th>Account Number</th>
            <th>IFSC Code</th>
        </tr>

        <tr>
            <td>{bankName}</td>
            <td>{accountNumber}</td>
            <td>{ifscCode}</td>
        </tr>

    </table>


    <!-- GOODS RETURN -->

    <div class=""field-line"">
        <label>Goods Return Address:</label>
        <span class=""readonly-field"">
            {goodsReturnAddress}
        </span>
    </div>


    <!-- PAYMENT -->

    <div class=""section-title"">
        PAYMENT DETAILS
    </div>

    <table class=""data-table"">

        <tr>
            <th>Credit Days</th>
            <th>Discount</th>
        </tr>

        <tr>
            <td>{creditDays}</td>
            <td>{discount}</td>
        </tr>

    </table>


    <!-- GST / PAN -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>GST Number:</label>
                    <span class=""readonly-field"">
                        {gstNumber}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>PAN Number:</label>
                    <span class=""readonly-field"">
                        {panNumber}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- MSME -->

    <div class=""section-title"">
        MSME DETAILS
    </div>

    <table class=""data-table"">

        <tr>

            <th>MSME Number</th>

            <th>Enterprise Type</th>

            <th>Major Activity</th>

        </tr>

        <tr>

            <td>
                {msmeNumber}
            </td>

            <td>
                {enterpriseType}
            </td>

            <td>
                {majorActivity}
            </td>

        </tr>

    </table>


    <!-- LEGAL -->

    <table class=""ref-table"">

        <tr>

            <td>

                <div class=""field-line"">
                    <label>Legal Name:</label>
                    <span class=""readonly-field"">
                        {legalName}
                    </span>
                </div>

            </td>

            <td>

                <div class=""field-line"">
                    <label>Business Type:</label>
                    <span class=""readonly-field"">
                        {businessType}
                    </span>
                </div>

            </td>

        </tr>

    </table>


    <!-- AGENCY -->

    <div class=""field-line"">
        <label>Agency Email:</label>
        <span class=""readonly-field"">
            {agencyEmail}
        </span>
    </div>


    <!-- MAJOR GOODS -->

    <div class=""section-title"">
        MAJOR GOODS AND SERVICES
    </div>

    <table class=""data-table"">

        <tr>

            <th style=""width:8%;"">
                S.No
            </th>

            <th>
                Description
            </th>

            <th>
                Category
            </th>

            <th>
                Item Code
            </th>

        </tr>

        {goodsHtml}

    </table>


    <!-- NHFS CONTACT -->

    <div class=""field-line"">
        <label>NHFS Contact Person:</label>
        <span class=""readonly-field"">
            {nhfsContactPerson}
        </span>
    </div>


    <!-- SIGNATURE -->

    <div class=""signature"">

        <table class=""signature-table"">

            <tr>

                <td>

                    <div class=""signature-line""></div>

                    <div class=""small"">
                        Vendor Signature
                    </div>

                </td>

                <td>

                    <div class=""signature-line""></div>

                    <div class=""small"">
                        NHFS Authorized Signatory
                    </div>

                </td>

            </tr>

        </table>

    </div>

</div>

</body>

</html>";

            return html;
        }
        public byte[] ConvertHtmlToPdf(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                throw new ArgumentException(
                    "HTML content is empty.",
                    nameof(htmlContent)
                );
            }

            try
            {
                // =========================================================
                // CHECK libwkhtmltox.dll
                // =========================================================

                string baseDirectory = AppContext.BaseDirectory;

                string dllPath = Path.Combine(
                    baseDirectory,
                    "libwkhtmltox.dll"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] Base Directory: {baseDirectory}",
                    "ConvertHtmlToPdf"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] DLL Path: {dllPath}",
                    "ConvertHtmlToPdf"
                );

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] DLL Exists: {File.Exists(dllPath)}",
                    "ConvertHtmlToPdf"
                );

                if (!File.Exists(dllPath))
                {
                    throw new FileNotFoundException(
                        $"libwkhtmltox.dll was not found at: {dllPath}"
                    );
                }

                // =========================================================
                // CREATE PDF DOCUMENT
                // =========================================================

                var document = new HtmlToPdfDocument
                {
                    GlobalSettings =
            {
                ColorMode = ColorMode.Color,

                Orientation =
                    Orientation.Portrait,

                PaperSize =
                    PaperKind.A4,

                Margins =
                {
                    Top = 0,
                    Bottom = 0,
                    Left = 0,
                    Right = 0
                },

                DocumentTitle =
                    "Vendor Registration Form"
            },

                    Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = htmlContent,

                    WebSettings =
                    {
                        DefaultEncoding = "utf-8",

                        LoadImages = true,

                        EnableJavascript = false
                    },

                    UseLocalLinks = true
                }
            }
                };

                // =========================================================
                // CONVERT
                // =========================================================

                log.WriteToLogFile_Debug(
                    "[ConvertHtmlToPdf] Starting PDF conversion",
                    "ConvertHtmlToPdf"
                );

                byte[] pdfBytes =
                    _converter.Convert(document);

                // =========================================================
                // VALIDATE RESULT
                // =========================================================

                if (pdfBytes == null ||
                    pdfBytes.Length == 0)
                {
                    throw new Exception(
                        "PDF conversion returned an empty file."
                    );
                }

                log.WriteToLogFile_Debug(
                    $"[ConvertHtmlToPdf] PDF generated successfully. Size: {pdfBytes.Length} bytes",
                    "ConvertHtmlToPdf"
                );

                return pdfBytes;
            }
            catch (Exception ex)
            {
                log.WriteToLogFile_Debug(
                    "[ConvertHtmlToPdf] ERROR - " +
                    ex.ToString(),
                    "ConvertHtmlToPdf"
                );

                throw;
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

using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Data.SqlClient;
using SelectPdf;
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
        //        public string GenerateVendorHtmlWithData(
        //      Dictionary<string, object> data,
        //      IEnumerable<object> goodsList)
        //        {
        //            // ============================================================
        //            // HELPERS
        //            // ============================================================

        //            string GetValue(string key)
        //            {
        //                if (data == null)
        //                    return "";

        //                if (!data.ContainsKey(key))
        //                    return "";

        //                return data[key]?.ToString() ?? "";
        //            }

        //            string HtmlEncode(string value)
        //            {
        //                return System.Net.WebUtility.HtmlEncode(value ?? "");
        //            }


        //            // ============================================================
        //            // BASIC DETAILS
        //            // ============================================================

        //            string vendorName =
        //                HtmlEncode(GetValue("Trade Name"));

        //            string billingAddress =
        //                HtmlEncode(GetValue("Billing Address"));

        //            string registeredAddress =
        //                HtmlEncode(GetValue("Registered Address"));

        //            string natureOfBusiness =
        //                HtmlEncode(GetValue("Nature of Business"));

        //            string mobileNumber =
        //                HtmlEncode(GetValue("Mobile Number"));

        //            string officeTelephone =
        //                HtmlEncode(GetValue("Office Telephone"));

        //            string email =
        //                HtmlEncode(GetValue("Email ID"));

        //            string agencyEmail =
        //                HtmlEncode(GetValue("Agency Email"));

        //            string contactPerson =
        //                HtmlEncode(GetValue("Contact Person"));


        //            // ============================================================
        //            // BANK
        //            // ============================================================

        //            string bankName =
        //                HtmlEncode(GetValue("Bank Name"));

        //            string accountNumber =
        //                HtmlEncode(GetValue("Account Number"));

        //            string ifscCode =
        //                HtmlEncode(GetValue("IFSC Code"));


        //            // ============================================================
        //            // PAYMENT
        //            // ============================================================

        //            string goodsReturnAddress =
        //                HtmlEncode(GetValue("Goods Return Address"));

        //            string creditDays =
        //                HtmlEncode(GetValue("Credit Days"));

        //            string discount =
        //                HtmlEncode(GetValue("Discount"));


        //            // ============================================================
        //            // GST / PAN
        //            // ============================================================

        //            string gstNumber =
        //                HtmlEncode(GetValue("GST Number"));

        //            string panNumber =
        //                HtmlEncode(GetValue("PAN Number"));


        //            // ============================================================
        //            // MSME
        //            // ============================================================

        //            string msmeNumber =
        //                HtmlEncode(GetValue("MSME Number"));

        //            string msmeDate =
        //                HtmlEncode(GetValue("MSME Date"));

        //            string enterpriseType =
        //                HtmlEncode(GetValue("Enterprise Type"));

        //            string majorActivity =
        //                HtmlEncode(GetValue("Major Activity"));


        //            // ============================================================
        //            // LEGAL
        //            // ============================================================

        //            string legalName =
        //                HtmlEncode(GetValue("Legal Name"));

        //            string tradeName =
        //                HtmlEncode(GetValue("Trade Name"));

        //            string businessType =
        //                HtmlEncode(GetValue("Business Type"));

        //            string nhfsContactPerson =
        //                HtmlEncode(GetValue("NHFS Contact Person"));

        //            string formDate =
        //                HtmlEncode(GetValue("Date"));


        //            // ============================================================
        //            // REFERENCE
        //            // ============================================================

        //            string refNo =
        //                HtmlEncode(GetValue("Ref. No."));

        //            string codeNo =
        //                HtmlEncode(GetValue("CODE NO."));

        //            string location =
        //                HtmlEncode(GetValue("LOCATION"));


        //            // ============================================================
        //            // MARK DOWN
        //            // ============================================================

        //            string md0With =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (with Tax @0%)")
        //                );

        //            string md0Without =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (without Tax @0%)")
        //                );

        //            string md3With =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (with Tax @3%)")
        //                );

        //            string md3Without =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (without Tax @3%)")
        //                );

        //            string md5With =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (with Tax @5%)")
        //                );

        //            string md5Without =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (without Tax @5%)")
        //                );

        //            string md18With =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (with Tax @18%)")
        //                );

        //            string md18Without =
        //                HtmlEncode(
        //                    GetValue("Mark Down % on MRP (without Tax @18%)")
        //                );


        //            // ============================================================
        //            // GOODS
        //            // ============================================================

        //            StringBuilder goodsHtml =
        //                new StringBuilder();

        //            int goodsIndex = 0;

        //            if (goodsList != null)
        //            {
        //                foreach (var item in goodsList)
        //                {
        //                    if (item == null)
        //                        continue;

        //                    string product = "";
        //                    string brand = "";
        //                    string size = "";

        //                    Type type =
        //                        item.GetType();


        //                    // ----------------------------------------------------
        //                    // PRODUCT
        //                    // ----------------------------------------------------

        //                    var property =
        //                        type.GetProperty("Product");

        //                    if (property != null)
        //                    {
        //                        product =
        //                            property.GetValue(item)?.ToString() ?? "";
        //                    }

        //                    if (string.IsNullOrWhiteSpace(product))
        //                    {
        //                        property =
        //                            type.GetProperty("product");

        //                        if (property != null)
        //                        {
        //                            product =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }

        //                    if (string.IsNullOrWhiteSpace(product))
        //                    {
        //                        property =
        //                            type.GetProperty("Description");

        //                        if (property != null)
        //                        {
        //                            product =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }

        //                    if (string.IsNullOrWhiteSpace(product))
        //                    {
        //                        property =
        //                            type.GetProperty("Goods");

        //                        if (property != null)
        //                        {
        //                            product =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }

        //                    if (string.IsNullOrWhiteSpace(product))
        //                    {
        //                        property =
        //                            type.GetProperty("materialDescription");

        //                        if (property != null)
        //                        {
        //                            product =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }


        //                    // ----------------------------------------------------
        //                    // BRAND
        //                    // ----------------------------------------------------

        //                    property =
        //                        type.GetProperty("Brand");

        //                    if (property != null)
        //                    {
        //                        brand =
        //                            property.GetValue(item)?.ToString() ?? "";
        //                    }

        //                    if (string.IsNullOrWhiteSpace(brand))
        //                    {
        //                        property =
        //                            type.GetProperty("brand");

        //                        if (property != null)
        //                        {
        //                            brand =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }


        //                    // ----------------------------------------------------
        //                    // SIZE
        //                    // ----------------------------------------------------

        //                    property =
        //                        type.GetProperty("Size");

        //                    if (property != null)
        //                    {
        //                        size =
        //                            property.GetValue(item)?.ToString() ?? "";
        //                    }

        //                    if (string.IsNullOrWhiteSpace(size))
        //                    {
        //                        property =
        //                            type.GetProperty("size");

        //                        if (property != null)
        //                        {
        //                            size =
        //                                property.GetValue(item)?.ToString() ?? "";
        //                        }
        //                    }


        //                    // ----------------------------------------------------
        //                    // LETTER
        //                    // ----------------------------------------------------

        //                    char letter =
        //                        (char)('a' + goodsIndex);


        //                    // ----------------------------------------------------
        //                    // ITEM
        //                    // ----------------------------------------------------

        //                    goodsHtml.Append($@"

        //                <div class=""item-entry"">

        //                    <div>
        //                        <strong>{letter}) Product:</strong>
        //                        {HtmlEncode(product)}
        //                    </div>

        //                    <div>
        //                        <strong>Brand:</strong>
        //                        {HtmlEncode(brand)}
        //                    </div>

        //                    <div>
        //                        <strong>Size:</strong>
        //                        {HtmlEncode(size)}
        //                    </div>

        //                </div>

        //            ");

        //                    goodsIndex++;
        //                }
        //            }


        //            // ============================================================
        //            // NO GOODS
        //            // ============================================================

        //            if (goodsIndex == 0)
        //            {
        //                goodsHtml.Append(@"

        //            <div class=""item-empty"">
        //                -
        //            </div>

        //        ");
        //            }


        //            // ============================================================
        //            // LOGO
        //            // ============================================================

        //            string logoPath =
        //                Path.Combine(
        //                    Directory.GetCurrentDirectory(),
        //                    "wwwroot",
        //                    "Images",
        //                    "Logo.png"
        //                );

        //            string logoHtml = "";

        //            if (File.Exists(logoPath))
        //            {
        //                byte[] logoBytes =
        //                    File.ReadAllBytes(logoPath);

        //                string base64Logo =
        //                    Convert.ToBase64String(logoBytes);

        //                logoHtml =
        //                    $"data:image/png;base64,{base64Logo}";
        //            }


        //            string logoSection = "";

        //            if (!string.IsNullOrWhiteSpace(logoHtml))
        //            {
        //                logoSection = $@"

        //            <div class=""logo-box"">

        //                <img
        //                    src=""{logoHtml}""
        //                    alt=""Logo""
        //                    class=""logo""
        //                />

        //            </div>

        //        ";
        //            }


        //            // ============================================================
        //            // WATERMARK
        //            // ============================================================

        //            string watermark = "DRAFT";


        //            // ============================================================
        //            // HTML
        //            // ============================================================

        //            string html = $@"

        //<!DOCTYPE html>

        //<html>

        //<head>

        //<meta charset=""UTF-8"" />

        //<title>
        //    Vendor Registration Form
        //</title>


        //<style>

        ///* ============================================================
        //   RESET
        //============================================================ */

        //* {{
        //    box-sizing: border-box;
        //}}

        //html,
        //body {{
        //    margin: 0;
        //    padding: 0;
        //}}

        //body {{

        //    background: #eeeeee;

        //    font-family:
        //        ""Times New Roman"",
        //        Times,
        //        serif;

        //    font-size: 9px;

        //    color: #000;

        //    line-height: 1.1;
        //}}


        ///* ============================================================
        //   A4
        //============================================================ */

        //.page {{

        //    position: relative;

        //    width: 210mm;

        //    height: 297mm;

        //    min-height: 297mm;

        //    margin: 12px auto;

        //    padding:
        //        4mm
        //        7mm
        //        5mm
        //        7mm;

        //    background: #fff;

        //    border: 1px solid #000;

        //    overflow: hidden;

        //    isolation: isolate;
        //}}


        ///* ============================================================
        //   WATERMARK
        //============================================================ */

        //.watermark {{

        //    position: absolute;

        //    top: 50%;

        //    left: 50%;

        //    width: 100%;

        //    transform:
        //        translate(-50%, -50%)
        //        rotate(-35deg);

        //    z-index: 0;

        //    text-align: center;

        //    font-family:
        //        Arial,
        //        Helvetica,
        //        sans-serif;

        //    font-size: 95px;

        //    font-weight: 900;

        //    letter-spacing: 4px;

        //    color: #d6d6d6;

        //    opacity: 0.50;

        //    white-space: nowrap;

        //    pointer-events: none;

        //    user-select: none;
        //}}


        ///* ============================================================
        //   CONTENT OVER WATERMARK
        //============================================================ */

        //.page > *:not(.watermark) {{

        //    position: relative;

        //    z-index: 1;
        //}}


        ///* ============================================================
        //   LOGO BOX
        //============================================================ */

        //.logo-box {{

        //    width: 100%;

        //    height: 50px;

        //    border:
        //        1px
        //        solid
        //        #000;

        //    margin:
        //        0
        //        0
        //        3px
        //        0;

        //    padding: 3px;

        //    display: flex;

        //    align-items: center;

        //    justify-content: center;

        //    background: #fff;
        //}}


        ///* ============================================================
        //   LOGO IMAGE
        //============================================================ */

        //.logo {{

        //    display: block;

        //    width: 175px;

        //    height: auto;

        //    max-width: 100%;

        //    max-height: 42px;

        //    object-fit: contain;
        //}}


        ///* ============================================================
        //   COMPANY HEADER
        //============================================================ */

        //.header {{

        //    width: 100%;

        //    margin:
        //        0
        //        0
        //        4px
        //        0;

        //    text-align: center;

        //    font-size: 7px;

        //    font-weight: bold;

        //    line-height: 1.05;
        //}}


        ///* ============================================================
        //   TITLE
        //============================================================ */

        //h2 {{

        //    margin:
        //        0
        //        0
        //        7px
        //        0;

        //    padding: 0;

        //    text-align: center;

        //    font-size: 11px;

        //    font-weight: bold;

        //    line-height: 1;

        //    text-decoration: underline;
        //}}


        ///* ============================================================
        //   NORMAL FIELD
        //============================================================ */

        //.field-line {{

        //    display: flex;

        //    align-items: baseline;

        //    width: 100%;

        //    min-height: 14px;

        //    margin:
        //        1.5px
        //        0;

        //    padding: 0;

        //    font-size: 8.5px;

        //    line-height: 1.1;

        //    page-break-inside: avoid;

        //    break-inside: avoid;
        //}}


        ///*
        //   IMPORTANT:
        //   No fixed 90px / 128px width here.
        //   The value starts immediately after the label.
        //*/

        //.field-line label {{

        //    flex:
        //        0 0 auto;

        //    width: auto;

        //    margin:
        //        0
        //        5px
        //        0
        //        0;

        //    padding: 0;

        //    font-weight: bold;

        //    white-space: nowrap;

        //    text-align: left;
        //}}


        ///* ============================================================
        //   UNDERLINE VALUE
        //============================================================ */

        //.readonly-field {{

        //    display: block;

        //    flex: 1;

        //    min-width: 0;

        //    min-height: 13px;

        //    margin: 0;

        //    padding:
        //        0
        //        2px
        //        1px
        //        2px;

        //    border-bottom:
        //        1px
        //        solid
        //        #000;

        //    font-size: 8.5px;

        //    line-height: 1.1;

        //    text-align: left;

        //    overflow-wrap: anywhere;

        //    word-break: break-word;
        //}}


        ///* ============================================================
        //   TWO COLUMN TABLE
        //============================================================ */

        //.ref-table {{

        //    width: 100%;

        //    margin: 0;

        //    padding: 0;

        //    border-collapse: collapse;

        //    table-layout: fixed;
        //}}

        //.ref-table td {{

        //    width: 50%;

        //    padding: 0;

        //    vertical-align: top;
        //}}

        //.ref-table td:first-child {{

        //    padding-right: 7px;
        //}}

        //.ref-table td:last-child {{

        //    padding-left: 7px;
        //}}

        //.ref-table .field-line {{

        //    margin:
        //        1.5px
        //        0;
        //}}

        //.ref-table .field-line label {{

        //    flex:
        //        0 0 auto;

        //    width: auto;

        //    margin-right: 4px;

        //    white-space: nowrap;
        //}}


        ///* ============================================================
        //   SECTION HEADER
        //============================================================ */

        //.section-title {{

        //    width: 100%;

        //    height: 17px;

        //    margin:
        //        4px
        //        0
        //        2px
        //        0;

        //    padding:
        //        3px
        //        6px;

        //    background: #f2f2f2;

        //    border-bottom:
        //        1px
        //        solid
        //        #d0d0d0;

        //    font-size: 8.5px;

        //    font-weight: bold;

        //    line-height: 1;
        //}}


        ///* ============================================================
        //   MARKDOWN
        //============================================================ */

        //.markdown-table {{

        //    width: 100%;

        //    border-collapse: collapse;

        //    table-layout: fixed;

        //    margin:
        //        2px
        //        0
        //        2px
        //        0;
        //}}

        //.markdown-table td {{

        //    width: 50%;

        //    padding:
        //        1px
        //        5px
        //        1px
        //        0;

        //    vertical-align: middle;
        //}}

        //.markdown-table td:last-child {{

        //    padding-left: 5px;
        //}}

        //.markdown-table .field-line {{

        //    display: flex;

        //    align-items: center;

        //    min-height: 22px;

        //    margin: 0;
        //}}

        //.markdown-table label {{

        //    flex:
        //        0 0 125px;

        //    width: 125px;

        //    margin:
        //        0
        //        4px
        //        0
        //        0;

        //    text-align: center;

        //    font-size: 7.2px;

        //    font-weight: normal;

        //    line-height: 1.1;

        //    white-space: normal;
        //}}

        //.markdown-table .readonly-field {{

        //    flex: 1;

        //    min-width: 0;

        //    min-height: 13px;

        //    padding:
        //        0
        //        2px
        //        1px
        //        2px;

        //    font-size: 8px;
        //}}


        ///* ============================================================
        //   ITEMS
        //============================================================ */

        //.items-section {{

        //    width: 100%;

        //    margin:
        //        2px
        //        0
        //        3px
        //        0;

        //    font-size: 8px;

        //    line-height: 1.1;
        //}}

        //.item-entry {{

        //    width: 100%;

        //    min-height: 36px;

        //    margin:
        //        2px
        //        0;

        //    padding:
        //        4px
        //        6px;

        //    border:
        //        1px
        //        solid
        //        #d5d5d5;

        //    background:
        //        rgba(
        //            255,
        //            255,
        //            255,
        //            0.75
        //        );

        //    page-break-inside: avoid;

        //    break-inside: avoid;
        //}}

        //.item-entry div {{

        //    margin:
        //        1px
        //        0;
        //}}

        //.item-entry strong {{

        //    font-weight: bold;
        //}}

        //.item-empty {{

        //    min-height: 25px;

        //    padding: 3px;
        //}}


        ///* ============================================================
        //   REMARKS
        //============================================================ */

        //.remarks-section {{

        //    width: 100%;

        //    margin:
        //        3px
        //        0
        //        3px
        //        0;

        //    padding:
        //        5px;

        //    border:
        //        1px
        //        solid
        //        #000;

        //    page-break-inside: avoid;

        //    break-inside: avoid;
        //}}

        //.remarks-label {{

        //    margin:
        //        0
        //        0
        //        2px
        //        0;

        //    font-size: 8.5px;

        //    font-weight: bold;
        //}}

        //.remarks-field {{

        //    width: 100%;

        //    height: 32px;

        //    min-height: 32px;

        //    border-bottom:
        //        1px
        //        solid
        //        #000;

        //    padding: 2px;
        //}}


        ///* ============================================================
        //   SIGNATURE BOX
        //============================================================ */

        //.stamp-box {{

        //    width: 270px;

        //    height: 65px;

        //    margin:
        //        4px
        //        auto
        //        0
        //        auto;

        //    padding: 4px;

        //    border:
        //        1px
        //        solid
        //        #000;

        //    text-align: center;

        //    background:
        //        rgba(
        //            255,
        //            255,
        //            255,
        //            0.9
        //        );

        //    page-break-inside: avoid;

        //    break-inside: avoid;
        //}}

        //.signature-text {{

        //    margin-top: 24px;

        //    padding-top: 3px;

        //    font-size: 8px;

        //    line-height: 1.15;

        //    font-weight: bold;
        //}}


        ///* ============================================================
        //   PRINT
        //============================================================ */

        //@media print {{

        //    @page {{

        //        size: A4 portrait;

        //        margin: 0;
        //    }}

        //    html,
        //    body {{

        //        width: 210mm;

        //        height: 297mm;

        //        margin: 0 !important;

        //        padding: 0 !important;

        //        background: #fff !important;

        //        overflow: hidden !important;
        //    }}

        //    .page {{

        //        width: 210mm !important;

        //        height: 297mm !important;

        //        min-height: 297mm !important;

        //        margin: 0 !important;

        //        padding:
        //            4mm
        //            7mm
        //            5mm
        //            7mm !important;

        //        border:
        //            1px
        //            solid
        //            #000;

        //        overflow: hidden !important;

        //        page-break-before: avoid !important;

        //        page-break-after: avoid !important;

        //        break-before: avoid-page !important;

        //        break-after: avoid-page !important;
        //    }}

        //    .watermark {{

        //        position: absolute !important;

        //        z-index: 0 !important;

        //        color: #d6d6d6 !important;

        //        opacity: 0.50 !important;

        //        -webkit-print-color-adjust: exact;

        //        print-color-adjust: exact;
        //    }}

        //    .page > *:not(.watermark) {{

        //        position: relative !important;

        //        z-index: 1 !important;
        //    }}
        //}}


        ///* ============================================================
        //   SCREEN
        //============================================================ */

        //@media screen {{

        //    .page {{

        //        box-shadow:
        //            0
        //            2px
        //            12px
        //            rgba(
        //                0,
        //                0,
        //                0,
        //                0.12
        //            );
        //    }}
        //}}

        //</style>

        //</head>


        //<body>


        //<div class=""page"">


        //    <!-- ========================================================
        //         WATERMARK
        //    ========================================================= -->

        //    <div class=""watermark"">
        //        {HtmlEncode(watermark)}
        //    </div>


        //    <!-- ========================================================
        //         LOGO
        //    ========================================================= -->

        //    {logoSection}


        //    <!-- ========================================================
        //         COMPANY HEADER
        //    ========================================================= -->

        //    <div class=""header"">

        //        No. 7, Basudev Street, Pondy Bazaar,
        //        T. Nagar, Chennai – 600 017

        //        <br />

        //        Contact: 044 24340714

        //    </div>


        //    <!-- ========================================================
        //         TITLE
        //    ========================================================= -->

        //    <h2>
        //        VENDOR REGISTRATION FORM
        //    </h2>


        //    <!-- ========================================================
        //         REF / CODE / DATE / LOCATION
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        Ref. No.:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {refNo}
        //                    </span>

        //                </div>


        //                <div class=""field-line"">

        //                    <label>
        //                        CODE NO.:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {codeNo}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        Date:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {formDate}
        //                    </span>

        //                </div>


        //                <div class=""field-line"">

        //                    <label>
        //                        LOCATION:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {location}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         VENDOR
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            1. Name of Vendor:
        //        </label>

        //        <span class=""readonly-field"">
        //            {vendorName}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         ADDRESS
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            2. Address:
        //        </label>

        //        <span class=""readonly-field"">
        //            {billingAddress}
        //        </span>

        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            Registered Office:
        //        </label>

        //        <span class=""readonly-field"">
        //            {registeredAddress}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         BUSINESS
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            3. Nature of Business:
        //        </label>

        //        <span class=""readonly-field"">
        //            {natureOfBusiness}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         CONTACT
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        4. Contact No. 1:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {mobileNumber}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        Contact No. 2:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {officeTelephone}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         EMAIL
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        5. Email ID 1:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {email}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        6. Email ID 2:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {agencyEmail}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         CONTACT PERSON
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            7. Contact Person:
        //        </label>

        //        <span class=""readonly-field"">
        //            {contactPerson}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         RTGS
        //    ========================================================= -->

        //    <div class=""section-title"">
        //        9. RTGS Details
        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            Name of Bank and Branch:
        //        </label>

        //        <span class=""readonly-field"">
        //            {bankName}
        //        </span>

        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            A/c No.:
        //        </label>

        //        <span class=""readonly-field"">
        //            {accountNumber}
        //        </span>

        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            IFSC Code:
        //        </label>

        //        <span class=""readonly-field"">
        //            {ifscCode}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         GOODS RETURN
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            10. Goods Return Address:
        //        </label>

        //        <span class=""readonly-field"">
        //            {goodsReturnAddress}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         PAYMENT
        //    ========================================================= -->

        //    <div class=""section-title"">
        //        Payment Days / Terms
        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            Days:
        //        </label>

        //        <span class=""readonly-field"">
        //            {creditDays}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         MARK DOWN
        //    ========================================================= -->

        //    <table class=""markdown-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (1) Mark Down % on MRP
        //                        <br />
        //                        (With Tax @ 0%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md0With}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (2) Mark Down % on MRP
        //                        <br />
        //                        (Without Tax @ 0%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md0Without}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>


        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (3) Mark Down % on MRP
        //                        <br />
        //                        (With Tax @ 3%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md3With}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (4) Mark Down % on MRP
        //                        <br />
        //                        (Without Tax @ 3%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md3Without}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>


        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (5) Mark Down % on MRP
        //                        <br />
        //                        (With Tax @ 5%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md5With}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (6) Mark Down % on MRP
        //                        <br />
        //                        (Without Tax @ 5%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md5Without}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>


        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (7) Mark Down % on MRP
        //                        <br />
        //                        (With Tax @ 18%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md18With}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        (8) Mark Down % on MRP
        //                        <br />
        //                        (Without Tax @ 18%)
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {md18Without}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         DISCOUNT
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            Discount:
        //        </label>

        //        <span class=""readonly-field"">
        //            {discount}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         GST / PAN
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        12. GST No.:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {gstNumber}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        13. PAN No.:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {panNumber}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         MSME
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        14. MSME No.:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {msmeNumber}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        Date:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {msmeDate}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         ACTIVITY
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            Major Activity:
        //        </label>

        //        <span class=""readonly-field"">
        //            {majorActivity}
        //        </span>

        //    </div>


        //    <div class=""field-line"">

        //        <label>
        //            Enterprise Type:
        //        </label>

        //        <span class=""readonly-field"">
        //            {enterpriseType}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         LEGAL / TRADE
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        15. Legal Name:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {legalName}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        16. Trade Name:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {tradeName}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         AGENCY
        //    ========================================================= -->

        //    <table class=""ref-table"">

        //        <tr>

        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        17. Agency / Direct:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {businessType}
        //                    </span>

        //                </div>

        //            </td>


        //            <td>

        //                <div class=""field-line"">

        //                    <label>
        //                        18. Agency Email:
        //                    </label>

        //                    <span class=""readonly-field"">
        //                        {agencyEmail}
        //                    </span>

        //                </div>

        //            </td>

        //        </tr>

        //    </table>


        //    <!-- ========================================================
        //         ITEMS
        //    ========================================================= -->

        //    <div class=""section-title"">
        //        19. Items Supplied
        //    </div>


        //    <div class=""items-section"">

        //        {goodsHtml}

        //    </div>


        //    <!-- ========================================================
        //         NHFS CONTACT
        //    ========================================================= -->

        //    <div class=""field-line"">

        //        <label>
        //            20. NHFS Contact Person:
        //        </label>

        //        <span class=""readonly-field"">
        //            {nhfsContactPerson}
        //        </span>

        //    </div>


        //    <!-- ========================================================
        //         REMARKS
        //    ========================================================= -->

        //    <div class=""remarks-section"">

        //        <div class=""remarks-label"">
        //            Remarks:
        //        </div>

        //        <div class=""remarks-field"">
        //        </div>

        //    </div>


        //    <!-- ========================================================
        //         SIGNATURE
        //    ========================================================= -->

        //    <div class=""stamp-box"">

        //        <div class=""signature-text"">

        //            (Signature with Rubber Stamp)

        //            <br />

        //            AUTHORISED SIGNATORY

        //        </div>

        //    </div>


        //</div>


        //</body>

        //</html>
        //";


        //            return html;
        //        }

        public string GenerateVendorHtmlWithData(
    Dictionary<string, object> data,
    IEnumerable<object> goodsList,
    bool includeWatermark)
        {
            // ============================================================
            // HELPERS
            // ============================================================

            string GetValue(string key)
            {
                if (data == null || !data.ContainsKey(key))
                    return "";

                return data[key]?.ToString() ?? "";
            }

            string GetFirstValue(params string[] keys)
            {
                foreach (var key in keys)
                {
                    var value = GetValue(key);

                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                return "";
            }

            string HtmlEncode(string value)
            {
                return System.Net.WebUtility.HtmlEncode(value ?? "");
            }

            // ============================================================
            // BASIC DETAILS
            // ============================================================

            string vendorName = HtmlEncode(
                GetFirstValue(
                    "Trade Name",
                    "Vendor Name",
                    "Name of Vendor"
                )
            );

            string natureOfBusiness = HtmlEncode(
                GetFirstValue(
                    "Nature of Business",
                    "Business"
                )
            );

            string mobileNumber = HtmlEncode(
                GetFirstValue(
                    "Mobile Number",
                    "Contact No. 1",
                    "Mobile No"
                )
            );

            string officeTelephone = HtmlEncode(
                GetFirstValue(
                    "Office Telephone",
                    "Contact No. 2",
                    "Office Phone"
                )
            );

            string email1 = HtmlEncode(
                GetFirstValue(
                    "Email ID",
                    "Email ID 1",
                    "Email"
                )
            );

            string email2 = HtmlEncode(
                GetFirstValue(
                    "Email ID 2",
                    "Agency Email"
                )
            );

            string proprietor = HtmlEncode(
                GetFirstValue(
                    "Name of Proprietor",
                    "Contact Person",
                    "Proprietor"
                )
            );

            string proprietorPhone = HtmlEncode(
                GetFirstValue(
                    "Phone No.",
                    "Mobile No",
                    "Proprietor Phone"
                )
            );

            // ============================================================
            // GST / PAN
            // ============================================================

            string gstNumber = HtmlEncode(
                GetFirstValue(
                    "GST Number",
                    "GST No.",
                    "GST"
                )
            );

            string panNumber = HtmlEncode(
                GetFirstValue(
                    "PAN Number",
                    "PAN No.",
                    "PAN"
                )
            );

            // ============================================================
            // LEGAL / TRADE
            // ============================================================

            string legalName = HtmlEncode(
                GetFirstValue(
                    "Legal Name",
                    "Legal_Name"
                )
            );

            string tradeName = HtmlEncode(
                GetFirstValue(
                    "Trade Name",
                    "Trade_Name"
                )
            );

            // ============================================================
            // BUSINESS TYPE
            // ============================================================

            string businessType = HtmlEncode(
                GetFirstValue(
                    "Business Type",
                    "Agency / Direct",
                    "AgencyDirect"
                )
            );

            string agencyEmail = HtmlEncode(
                GetFirstValue(
                    "Agency Email",
                    "Agency EMail",
                    "agencyEmail"
                )
            );

            string nhfsContactPerson = HtmlEncode(
                GetFirstValue(
                    "NHFS Contact Person",
                    "NHFS Contact",
                    "Selected Contact Person"
                )
            );

            // ============================================================
            // ADDRESS
            // ============================================================

            string billingAddress = HtmlEncode(
                GetFirstValue(
                    "Billing Address",
                    "Address"
                )
            );

            string registeredAddress = HtmlEncode(
                GetFirstValue(
                    "Registered Address",
                    "Registered Office"
                )
            );

            string goodsReturnAddress = HtmlEncode(
                GetFirstValue(
                    "Goods Return Address",
                    "Return Address"
                )
            );

            // ============================================================
            // BANK DETAILS
            // ============================================================

            string bankName = HtmlEncode(
                GetFirstValue(
                    "Bank Name",
                    "Bank and Branch"
                )
            );

            string accountNumber = HtmlEncode(
                GetFirstValue(
                    "Account Number",
                    "A/c No."
                )
            );

            string ifscCode = HtmlEncode(
                GetFirstValue(
                    "IFSC Code",
                    "IFSC"
                )
            );

            // ============================================================
            // PAYMENT DETAILS
            // ============================================================

            string creditDays = HtmlEncode(
                GetFirstValue(
                    "Credit Days",
                    "Days"
                )
            );

            string discount = HtmlEncode(
                GetFirstValue(
                    "Discount"
                )
            );

            // ============================================================
            // MARKDOWN
            // ============================================================

            string md0With = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (with Tax @0%)",
                    "md0With",
                    "MD0With"
                )
            );

            string md0Without = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (without Tax @0%)",
                    "md0Without",
                    "MD0Without"
                )
            );

            string md3With = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (with Tax @3%)",
                    "md3With",
                    "MD3With"
                )
            );

            string md3Without = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (without Tax @3%)",
                    "md3Without",
                    "MD3Without"
                )
            );

            string md5With = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (with Tax @5%)",
                    "md5With",
                    "MD5With"
                )
            );

            string md5Without = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (without Tax @5%)",
                    "md5Without",
                    "MD5Without"
                )
            );

            string md18With = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (with Tax @18%)",
                    "md18With",
                    "MD18With"
                )
            );

            string md18Without = HtmlEncode(
                GetFirstValue(
                    "Mark Down % on MRP (without Tax @18%)",
                    "md18Without",
                    "MD18Without"
                )
            );

            // ============================================================
            // MSME
            // ============================================================

            string msmeNumber = HtmlEncode(
                GetFirstValue(
                    "MSME Number",
                    "MSME No."
                )
            );

            string msmeDate = HtmlEncode(
                GetFirstValue(
                    "MSME Date",
                    "Date"
                )
            );

            string enterpriseType = HtmlEncode(
                GetFirstValue(
                    "Enterprise Type",
                    "Enterprise"
                )
            );

            string majorActivity = HtmlEncode(
                GetFirstValue(
                    "Major Activity",
                    "Activity"
                )
            );

            // ============================================================
            // DATE / REF / CODE / LOCATION
            // ============================================================

            string formDate = HtmlEncode(
                GetFirstValue(
                    "Date",
                    "Form Date"
                )
            );

            string refNo = HtmlEncode(
                GetFirstValue(
                    "Ref. No.",
                    "Ref No",
                    "Ref_No"
                )
            );

            string codeNo = HtmlEncode(
                GetFirstValue(
                    "CODE NO.",
                    "Code No",
                    "Code_No"
                )
            );

            string location = HtmlEncode(
                GetFirstValue(
                    "LOCATION",
                    "Location"
                )
            );

            // ============================================================
            // REMARKS
            // ============================================================

            string remarks = HtmlEncode(
                GetFirstValue(
                    "Remarks"
                )
            );

            // ============================================================
            // GOODS / ITEMS
            // ============================================================

            StringBuilder goodsHtml = new StringBuilder();

            int goodsIndex = 0;

            if (goodsList != null)
            {
                foreach (var item in goodsList)
                {
                    if (item == null)
                        continue;

                    var type = item.GetType();

                    string product = "";
                    string brand = "";
                    string size = "";

                    // ----------------------------------------------------
                    // PRODUCT
                    // ----------------------------------------------------

                    var property = type.GetProperty("Product");

                    if (property != null)
                    {
                        product =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        property = type.GetProperty("Description");

                        if (property != null)
                        {
                            product =
                                property.GetValue(item)?.ToString() ?? "";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        property = type.GetProperty("Goods");

                        if (property != null)
                        {
                            product =
                                property.GetValue(item)?.ToString() ?? "";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        property = type.GetProperty("Name");

                        if (property != null)
                        {
                            product =
                                property.GetValue(item)?.ToString() ?? "";
                        }
                    }

                    // ----------------------------------------------------
                    // BRAND
                    // ----------------------------------------------------

                    property = type.GetProperty("Brand");

                    if (property != null)
                    {
                        brand =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(brand))
                    {
                        property = type.GetProperty("Category");

                        if (property != null)
                        {
                            brand =
                                property.GetValue(item)?.ToString() ?? "";
                        }
                    }

                    // ----------------------------------------------------
                    // SIZE
                    // ----------------------------------------------------

                    property = type.GetProperty("Size");

                    if (property != null)
                    {
                        size =
                            property.GetValue(item)?.ToString() ?? "";
                    }

                    goodsHtml.Append($@"
                <div class=""item-entry"">

                    <div>
                        <strong>
                            {Convert.ToChar(97 + goodsIndex)})
                            Product:
                        </strong>
                        {HtmlEncode(product)}
                    </div>

                    <div>
                        <strong>
                            Brand:
                        </strong>
                        {HtmlEncode(brand)}
                    </div>

                    <div>
                        <strong>
                            Size:
                        </strong>
                        {HtmlEncode(size)}
                    </div>

                </div>");

                    goodsIndex++;
                }
            }

            if (goodsIndex == 0)
            {
                goodsHtml.Append(@"
            <div class=""item-empty"">
                -
            </div>");
            }

            // ============================================================
            // LOGO
            // ============================================================

            string logoPath = _configuration["Folder:LogoPath"];

            string logoHtml = "";

            if (File.Exists(logoPath))
            {
                try
                {
                    byte[] logoBytes =
                        File.ReadAllBytes(logoPath);

                    string base64Logo =
                        Convert.ToBase64String(logoBytes);

                    logoHtml =
                        $"data:image/png;base64,{base64Logo}";
                }
                catch
                {
                    logoHtml = "";
                }
            }

            // ============================================================
            // WATERMARK
            // ============================================================

            string watermark = includeWatermark
                ? "DRAFT"
                : "";

            string watermarkHtml = includeWatermark
                ? $@"
        <div class=""watermark"">
            <span>{HtmlEncode(watermark)}</span>
        </div>"
                : "";

           
            string html = $@"
<!DOCTYPE html>

<html>

<head>

<meta charset=""UTF-8"" />

<title>Vendor Registration Form</title>

<style>

* {{
    box-sizing: border-box;
}}

html,
body {{
    margin: 0;
    padding: 0;
}}

body {{
    font-family: ""Times New Roman"", serif;
    background: #f5f5f5;
    font-size: 10.5px;
    line-height: 1.1;
}}

@page {{
    size: A4 portrait;
    margin: 0;
}}

/* ============================================================
   PAGE
============================================================ */

.page {{
    width: 210mm;
    min-height: 297mm;
    height: auto;

    background: #fff;

    border: 1px solid #000;

    padding:
        5mm
        8mm
        15mm
        8mm;

    margin: 0 auto;

    position: relative;

    box-sizing: border-box;

    overflow: visible;

    transform-origin: top left;
}}

/* ============================================================
   WATERMARK
============================================================ */

.page .watermark {{
    position: absolute !important;

    top: 50% !important;
    left: 50% !important;

    transform:
        translate(-80%, -40%)
        rotate(-35deg) !important;

    width: 60%;

    text-align: center;

    font-size: 200px;

    font-weight: 900;

    font-family: ""Arial Black"", sans-serif;

    text-transform: uppercase;

    letter-spacing: 10px;

    color: rgba(0, 0, 0, 0.18);

    opacity: 0.25;

    pointer-events: none;

    user-select: none;

    z-index: 0;

    white-space: nowrap;
}}

.page > *:not(.watermark) {{
    position: relative;
    z-index: 1;
}}

/* ============================================================
   LOGO
============================================================ */

.logo-box {{
    margin-top: 0;

    border: 2px solid #000;

    width: 745px;
    height: 95px;

    margin:
        0
        auto
        8px;

    padding: 6px;

    text-align: center;

    display: flex;

    align-items: center;

    justify-content: center;

    background: #fff;
}}

.logo {{
    width: 425px;

    height: auto;

    max-height: 75px;

    display: block;

    object-fit: contain;
}}

/* ============================================================
   HEADER
============================================================ */

.header {{
    text-align: center;

    font-size: 10px;

    font-weight: bold;

    margin:
        0
        0
        10px
        0;

    line-height: 1.1;
}}

/* ============================================================
   TITLE
============================================================ */

h2 {{
    text-align: center;

    text-decoration: underline;

    margin:
        0
        0
        20px
        0;

    font-size: 14px;

    font-weight: bold;
}}

/* ============================================================
   FIELD
============================================================ */

.field-line {{
    page-break-inside: avoid;

    margin:
        15px
        0;

    font-size: 10.5px;

    display: flex;

    align-items: center;
}}

.field-line label {{
    font-weight: bold;

    width: 155px;

    flex-shrink: 0;

    margin-right: 10px;

    text-align: left;
}}

.readonly-field {{
    border-bottom: 1px solid #000;

    flex: 1;

    padding:
        2px
        4px;

    min-height: 15px;

    font-size: 10.5px;

    max-width: 330px;

    text-align: left;

    display: inline-block;

    overflow-wrap: anywhere;
}}

/* ============================================================
   TWO COLUMN
============================================================ */

.ref-table {{
    width: 100%;

    margin-bottom: 15px;

    font-size: 10.5px;

    table-layout: fixed;

    border-collapse: collapse;
}}

.ref-table td {{
    width: 50%;

    padding: 0;

    vertical-align: top;
}}

.ref-table td:first-child {{
    padding-right: 15px;
}}

.ref-table .field-line {{
    margin:
        6px
        0;

    padding:
        2px
        0;
}}

.right-align {{
    text-align: right;
}}

/* ============================================================
   SECTION
============================================================ */

.section-title {{
    font-weight: bold;

    font-size: 10.5px;

    background: #f3f3f3;

    border-bottom:
        1px solid
        #d0d0d0;

    padding:
        5px
        8px;

    margin:
        8px
        0;
}}

/* ============================================================
   MARKDOWN
============================================================ */

.markdown-table {{
    width: 100%;

    border-collapse: collapse;

    margin:
        10px
        0;
}}

.markdown-table td {{
    width: 50%;

    vertical-align: top;

    padding:
        6px
        20px
        6px
        0;
}}

.markdown-table .field-line {{
    display: flex;

    align-items: center;

    margin: 0;
}}

.markdown-table label {{
    width: 170px;

    text-align: center;

    line-height: 1.3;

    font-weight: normal;
}}

.markdown-table .readonly-field {{
    width: 100px;

    border-bottom:
        1px solid
        #000;

    min-height: 16px;

    max-width: 100px;
}}

/* ============================================================
   ITEMS
============================================================ */

.items-section {{
    margin:
        12px
        0
        20px
        0;

    font-size: 10.5px;

    line-height: 1.25;

    display: grid;

    grid-template-columns:
        repeat(3, 1fr);

    gap: 12px;
}}

.item-entry {{
    padding:
        6px
        8px;

    border:
        1px solid
        #ddd;

    border-radius: 4px;

    min-height: 60px;

    page-break-inside: avoid;
}}

.item-entry strong {{
    font-weight: bold;
}}

.item-empty {{
    grid-column:
        1 / -1;

    min-height: 30px;
}}

/* ============================================================
   REMARKS
============================================================ */

.remarks-section {{
    border:
        1px solid
        #000;

    padding: 11px;

    margin:
        5px
        0
        135px
        0;

    width: 100%;
}}

.remarks-label {{
    font-weight: bold;

    font-size: 10.5px;

    margin-bottom: 2px;
}}

.remarks-field {{
    border-bottom:
        1px solid
        #000;

    min-height: 40px;

    padding: 5px;

    width: 100%;

    font-size: 10.5px;
}}

/* ============================================================
   SIGNATURE
============================================================ */

.stamp-box {{
    width: 535px;

    height: 105px;

    border:
        2px solid
        #000;

    text-align: center;

    padding: 16px;

    background: #fff;

    margin:
        3px
        auto
        0
        auto;

    page-break-inside: avoid;
}}

.signature-text {{
    padding-top: 8px;

    margin-top: 50px;

    font-weight: bold;

    font-size: 10.5px;
}}

/* ============================================================
   PRINT
============================================================ */

@media print {{

    html,
    body {{
        width: 210mm;
        height: 297mm;

        margin: 0 !important;
        padding: 0 !important;

        background: #fff;
    }}

    .page {{
        width: 210mm !important;

        min-height: 297mm !important;

        margin: 0 !important;

        border:
            1px solid
            #000;

        padding:
            5mm
            8mm !important;

        box-sizing: border-box;

        page-break-after: avoid !important;

        page-break-before: avoid !important;

        break-after: avoid-page !important;

        break-before: avoid-page !important;
    }}
}}

</style>

</head>

<body>

<div class=""page"">

    <!-- ======================================================
         WATERMARK
    ======================================================= -->

    <div class=""watermark"">
        DRAFT
    </div>


    <!-- ======================================================
         LOGO
    ======================================================= -->

    
        <div class=""logo-box"">

            <img
                src=""data:image/png;base64,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8kHkLq4lpB398j04aNEDVHZkcMJB0B0CJ3oVKFMFrZTZKki0zOQQjAay6AOEcbgEou967oSYYyGmuQc6MtdZCDJu5xK+yMsIXnyek5DxlS6XO+GEE84///zBwcFbb7nlkksveemll2bPmXPzLTd3dnYee+yxJ598cl9f32GHHQY28CMbf/rTnx577DF0Lr/88mnTplU6Z597DnJ59913n3zqKf0D8397BfH/MWCEMbLLTMQ4qXIyrkyfOBZBfIK+0C1DwzwEz+u4a9kKOkACaC6b63K5P49Oixw8F1Pm4DIzLdifS65ceKFL5Wahdi4DEToAGulgECvnolR2KKayA8Tuhir9cpcYf5Xegi0T0Fw+6Ut7fJ6TEEFiRq222upTpkx56KGHxk+Y0N3de8ed/8Q8XHOtdX7xi59feOGF55577v333//1r3/9pJNOAj+gp6cnFovdeuutkydPbmpqevvtt3/0ox+1z24vlkoPP/zolBOm/P4a939YW8q2CUXzgOd1ll+XZ+D/iQx8npMQ8wRbHCbVTTfdMn36jI022qi2rj4ej+E3xrra2m222baSkQ8++ICZ33///cptOp3Gh83111//+eefv+2221ZbbbW33nqLWcXcP5ZXZ5993jqTJ4MTImiXw/IMfPUy8HlOQsyTW265Ze211+zr633jjVfb29ubGuoOOGD/e/551+hRbXfeeecLL7zQ1dX1zDPPtLS0jB49upLNl19+GZ26urogCPbdd1/shJ7n9fR0AbnjDtvmsumdd9oR0xu3nxLwGvApOZezLc/AlyEDn+ckRDwrr7xybW3dz37205NPPiWRSOyzzz4TJ0z461//ChJ2uYGBATx54qEUt1rP/QZ98cUX47a3t3fXXXf97ne/W11d3d3dffPNN2cymZqamt/85jegYnqj/RwAE3RxsATtixMBfnFSIC0BPoPU4kSA/3wNQRt0jgggLQ5G5K8gFycCfIVh0RakJcCi/BXMZxCB4GeQWoIISNA5IlRIlRbfiRbg+TwnIfarddddd7PNNkNnxRVXrK+vxyxCf9VVV4XFVCq14447YsdbffXVv/a1r2EzBBJUzNtKO2nSJHTGjBmzww47gFRVVbXpppsCg/4yAbO1c0OFnMHXbcEX7QqwEY5GhgrDiO3iRIAfkR/IJRgCCQwjAkjQOSKMyF9BjshfQVYYFm2XYAikRfkrGJAqahdtKwwjtosyz8eMyA/kEgyBBIYRYb7aRTsj8leQizLPxZiPy6bCOb+FD3N5Fq2lxUpZharER11XmooW+nChUKefF8zfr+Z3oHnBPm4/ARVqpQVpfgf9CiyKqeCX0OJHXyWM37DAw4TfZ32ieSABSWxkmM+zaGdxIsAvylzBLMEQSBWeRVuQoHNEWJR5PmZE/gpyPs8nOkswBNInmOffglRRu2g7n2fRzqLM8zGLMlcwSzAEUoVn0Xa+2kU7izLPxyzKPBezcOXM50cHPszlWbSWliDlJ8IYfhFGWRo3E3GdC2ru9St0wW/KRIyHXY4s0YAaHuTM0DxAf3Ewn2fRzuJEgF+UuYIBaQlQ4Vm0/QwiUPIZpJYgAhJ0jgggjQQZIEfkryBBXRxUGBZtF8dfwS/KX8FUqCO2FYYR2xH5K8gR+YGsUBfXguGToFCB2YGo2BvLDxORYlQmrnPhKzgJGb/PskVg3FhTPOhHhe8dUPze/nPhkHmd+ZhKZ3H4JVOXIPX5kpagDR4ujro4/BJEPhtpCYaWoHAJUp8vaQna/q/cK8DQwQdkDjnI/nD/8sxbaCtErZaRX6FG4T0S74VGUmNGTfjbJaMvP2P05WfOhcvmdeZjKp3F4ZdMXYLU50tagjZ4uDjq4vBLEPlspCUYWoLCJUh9vqQlaPs/dG/M5WeOuezMVX9/KWEXlIXm3UI3X5GZyKyIlUb7FQloeRhfqQygOhmPax/HpD7uLu8tz8DyDHwRGfh/aRJ+EflZbnN5Bv7nGVg+Cf/nKV5uYHkGlpyB/w8AAP//0q/88AAAAAZJREFUAwAO5wnfPb6udAAAAABJRU5ErkJggg==""
                alt=""Logo""
                class=""logo""
            />

        </div>
        


    <!-- ======================================================
         HEADER
    ======================================================= -->

    <div class=""header"">

        No. 7, Basudev Street, Pondy Bazaar,
        T. Nagar, Chennai – 600 017

        <br />

        Contact: 044 24340714

    </div>


    <!-- ======================================================
         TITLE
    ======================================================= -->

    <h2>
        VENDOR REGISTRATION FORM
    </h2>


    <!-- ======================================================
         REF / CODE / DATE / LOCATION
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            Ref. No.:
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>


                    <div class=""field-line"">

                        <label>
                            CODE NO.:
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td class=""right-align"">

                    <div class=""field-line"">

                        <label>
                            Date:
                        </label>

                        <span class=""readonly-field"">
                            05-10-2026
                        </span>

                    </div>


                    <div class=""field-line"">

                        <label>
                            LOCATION:
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         1. VENDOR
    ======================================================= -->

    <div class=""field-line"">

        <label>
            1. Name of Vendor:
        </label>

        <span class=""readonly-field"">
            ABC Enterprises
        </span>

    </div>


    <!-- ======================================================
         2. ADDRESS
    ======================================================= -->

    <div class=""field-line"">

        <label>
            2. Address:
        </label>

        <span class=""readonly-field"">
            No. 25, Anna Salai, Teynampet, Chennai - 600018
        </span>

    </div>


    <div class=""field-line"">

        <label>
            Registered Office:
        </label>

        <span class=""readonly-field"">
            No. 10, GST Road, Guindy, Chennai - 600032
        </span>

    </div>


    <!-- ======================================================
         3. NATURE OF BUSINESS
    ======================================================= -->

    <div class=""field-line"">

        <label>
            3. Nature of Business:
        </label>

        <span class=""readonly-field"">
            Wholesale and Distribution of Electrical Products
        </span>

    </div>


    <!-- ======================================================
         4. CONTACT
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            4. Contact No. 1:
                        </label>

                        <span class=""readonly-field"">
                            9876543210
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            Contact No. 2:
                        </label>

                        <span class=""readonly-field"">
                            044-45678901
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         5 / 6 EMAIL
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            5. Email ID 1:
                        </label>

                        <span class=""readonly-field"">
                            contact@abcenterprises.com
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            6. Email ID 2:
                        </label>

                        <span class=""readonly-field"">
                            agency@abcenterprises.com
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         7 / 8 PROPRIETOR
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            7. Name of Proprietor:
                        </label>

                        <span class=""readonly-field"">
                            Rajesh Kumar
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            8. Phone No.:
                        </label>

                        <span class=""readonly-field"">
                            9876543210
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         9. RTGS
    ======================================================= -->

    <div class=""section-title"">
        9. RTGS Details
    </div>


    <div class=""field-line"">

        <label>
            Name of Bank and Branch:
        </label>

        <span class=""readonly-field"">
            HDFC Bank
        </span>

    </div>


    <div class=""field-line"">

        <label>
            A/c No.:
        </label>

        <span class=""readonly-field"">
            50200012345678
        </span>

    </div>


    <div class=""field-line"">

        <label>
            IFSC Code:
        </label>

        <span class=""readonly-field"">
            HDFC0001234
        </span>

    </div>


    <!-- ======================================================
         10. GOODS RETURN
    ======================================================= -->

    <div class=""field-line"">

        <label>
            10. Goods Return Address:
        </label>

        <span class=""readonly-field"">
            No. 15, Industrial Estate, Ambattur, Chennai - 600058
        </span>

    </div>


    <!-- ======================================================
         PAYMENT
    ======================================================= -->

    <div class=""section-title"">
        Payment Days / Terms
    </div>


    <div class=""field-line"">

        <label>
            Days:
        </label>

        <span class=""readonly-field"">
            30
        </span>

    </div>


    <!-- ======================================================
         MARKDOWN
    ======================================================= -->

    <table class=""markdown-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            (1) Mark Down % on MRP
                            <br />
                            (With Tax @ 0%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            (2) Mark Down % on MRP
                            <br />
                            (Without Tax @ 0%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>

            </tr>


            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            (3) Mark Down % on MRP
                            <br />
                            (With Tax @ 3%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            (4) Mark Down % on MRP
                            <br />
                            (Without Tax @ 3%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>

            </tr>


            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            (5) Mark Down % on MRP
                            <br />
                            (With Tax @ 5%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            (6) Mark Down % on MRP
                            <br />
                            (Without Tax @ 5%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>

            </tr>


            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            (7) Mark Down % on MRP
                            <br />
                            (With Tax @ 18%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            (8) Mark Down % on MRP
                            <br />
                            (Without Tax @ 18%)
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         DISCOUNT
    ======================================================= -->

    <div class=""field-line"">

        <label>
            Discount:
        </label>

        <span class=""readonly-field"">
            5%
        </span>

    </div>


    <!-- ======================================================
         GST / PAN
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            12. GST No.:
                        </label>

                        <span class=""readonly-field"">
                            33ABCDE1234F1Z5
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            13. PAN No.:
                        </label>

                        <span class=""readonly-field"">
                            ABCDE1234F
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         MSME
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            14. MSME No.:
                        </label>

                        <span class=""readonly-field"">
                            UDYAM-TN-01-0012345
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            Date:
                        </label>

                        <span class=""readonly-field"">
                            05-10-2026
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         MAJOR ACTIVITY
    ======================================================= -->

    <div class=""field-line"">

        <label>
            Major Activity:
        </label>

        <span class=""readonly-field"">
            
        </span>

    </div>


    <!-- ======================================================
         ENTERPRISE TYPE
    ======================================================= -->

    <div class=""field-line"">

        <label>
            Enterprise Type:
        </label>

        <span class=""readonly-field"">
            Medium
        </span>

    </div>


    <!-- ======================================================
         LEGAL / TRADE
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            15. Legal Name:
                        </label>

                        <span class=""readonly-field"">
                            
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            16. Trade Name:
                        </label>

                        <span class=""readonly-field"">
                            ABC Enterprises
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         AGENCY
    ======================================================= -->

    <table class=""ref-table"">

        <tbody>

            <tr>

                <td>

                    <div class=""field-line"">

                        <label>
                            17. Agency / Direct:
                        </label>

                        <span class=""readonly-field"">
                            Partnership
                        </span>

                    </div>

                </td>


                <td>

                    <div class=""field-line"">

                        <label>
                            18. Agency Email:
                        </label>

                        <span class=""readonly-field"">
                            agency@abcenterprises.com
                        </span>

                    </div>

                </td>

            </tr>

        </tbody>

    </table>


    <!-- ======================================================
         19. ITEMS SUPPLIED
    ======================================================= -->

    <div class=""section-title"">
        19. Items Supplied
    </div>


    <div class=""items-section"">

        
            <div class=""item-empty"">
                -
            </div>

    </div>


    <!-- ======================================================
         20. NHFS CONTACT
    ======================================================= -->

    <div class=""field-line"">

        <label>
            20. NHFS Contact Person:
        </label>

        <span class=""readonly-field"">
            Rajesh Kumar
        </span>

    </div>


    <!-- ======================================================
         REMARKS
    ======================================================= -->

    <div class=""remarks-section"">

        <div class=""remarks-label"">
            Remarks:
        </div>

        <div class=""remarks-field"">
            
        </div>

    </div>


    <!-- ======================================================
         SIGNATURE
    ======================================================= -->

    <div class=""stamp-box"">

        <div class=""signature-text"">

            (Signature with Rubber Stamp)

            <br />

            AUTHORISED SIGNATORY

        </div>

    </div>

</div>

</body>

</html>";
//            string html = $@"
//<!DOCTYPE html>

//<html>

//<head>

//<meta charset=""UTF-8"" />

//<title>Vendor Registration Form</title>

//<style>

//* {{
//    box-sizing: border-box;
//}}

//html,
//body {{
//    margin: 0;
//    padding: 0;
//}}

//body {{
//    font-family: ""Times New Roman"", serif;
//    background: #f5f5f5;
//    font-size: 10.5px;
//    line-height: 1.1;
//}}

//@page {{
//    size: A4 portrait;
//    margin: 0;
//}}

///* ============================================================
//   PAGE
//============================================================ */

//.page {{
//    width: 210mm;
//    min-height: 297mm;

//    background: #fff;

//    border: 1px solid #000;

//    padding:
//        5mm
//        8mm
//        15mm
//        8mm;

//    margin: 0 auto;

//    position: relative;

//    box-sizing: border-box;

//    overflow: hidden;
//}}

///* ============================================================
//   WATERMARK
//============================================================ */

//.watermark {{
//    position: absolute;

//    top: 0;
//    left: 0;
//    right: 0;
//    bottom: 0;

//    width: 100%;
//    height: 100%;

//    display: flex;

//    align-items: center;

//    justify-content: center;

//    z-index: 0;

//    pointer-events: none;

//    overflow: hidden;
//}}

//.watermark span {{
//    display: block;

//    font-family: Arial, sans-serif;

//    font-size: 150px;

//    font-weight: 900;

//    color: rgba(128, 128, 128, 0.15);

//    white-space: nowrap;

//    letter-spacing: 8px;

//    transform: rotate(-35deg);

//    transform-origin: center center;

//    text-align: center;
//}}

//.page > *:not(.watermark) {{
//    position: relative;
//    z-index: 1;
//}}

///* ============================================================
//   LOGO
//============================================================ */

//.logo-box {{
//    margin-top: 0;

//    border: 2px solid #000;

//    width: 745px;

//    height: 95px;

//    margin:
//        0
//        auto
//        8px;

//    padding: 6px;

//    text-align: center;

//    display: flex;

//    align-items: center;

//    justify-content: center;

//    background: #fff;
//}}

//.logo {{
//    width: 425px;

//    height: auto;

//    max-height: 75px;

//    display: block;

//    object-fit: contain;
//}}

///* ============================================================
//   HEADER
//============================================================ */

//.header {{
//    text-align: center;

//    font-size: 10px;

//    font-weight: bold;

//    margin:
//        0
//        0
//        10px
//        0;

//    line-height: 1.1;
//}}

///* ============================================================
//   TITLE
//============================================================ */

//h2 {{
//    text-align: center;

//    text-decoration: underline;

//    margin:
//        0
//        0
//        20px
//        0;

//    font-size: 14px;

//    font-weight: bold;
//}}

///* ============================================================
//   FIELD
//============================================================ */

//.field-line {{
//    page-break-inside: avoid;

//    margin:
//        15px
//        0;

//    font-size: 10.5px;

//    display: flex;

//    align-items: center;
//}}

//.field-line label {{
//    font-weight: bold;

//    width: 155px;

//    flex-shrink: 0;

//    margin-right: 10px;

//    text-align: left;
//}}

//.readonly-field {{
//    border-bottom: 1px solid #000;

//    flex: 1;

//    padding:
//        2px
//        4px;

//    min-height: 15px;

//    font-size: 10.5px;

//    max-width: 330px;

//    text-align: left;

//    display: inline-block;

//    overflow-wrap: anywhere;
//}}

///* ============================================================
//   TWO COLUMN
//============================================================ */

//.ref-table {{
//    width: 100%;

//    margin-bottom: 15px;

//    font-size: 10.5px;

//    table-layout: fixed;

//    border-collapse: collapse;
//}}

//.ref-table td {{
//    width: 50%;

//    padding: 0;

//    vertical-align: top;
//}}

//.ref-table td:first-child {{
//    padding-right: 15px;
//}}

//.ref-table .field-line {{
//    margin:
//        6px
//        0;

//    padding:
//        2px
//        0;
//}}

//.right-align {{
//    text-align: right;
//}}

///* ============================================================
//   SECTION
//============================================================ */

//.section-title {{
//    font-weight: bold;

//    font-size: 10.5px;

//    background: #f3f3f3;

//    border-bottom:
//        1px solid
//        #d0d0d0;

//    padding:
//        5px
//        8px;

//    margin:
//        8px
//        0;
//}}

///* ============================================================
//   MARKDOWN
//============================================================ */

//.markdown-table {{
//    width: 100%;

//    border-collapse: collapse;

//    margin:
//        10px
//        0;
//}}

//.markdown-table td {{
//    width: 50%;

//    vertical-align: top;

//    padding:
//        6px
//        20px
//        6px
//        0;
//}}

//.markdown-table .field-line {{
//    display: flex;

//    align-items: center;

//    margin: 0;
//}}

//.markdown-table label {{
//    width: 170px;

//    text-align: center;

//    line-height: 1.3;

//    font-weight: normal;
//}}

//.markdown-table .readonly-field {{
//    width: 100px;

//    border-bottom:
//        1px solid
//        #000;

//    min-height: 16px;

//    max-width: 100px;
//}}

///* ============================================================
//   ITEMS
//============================================================ */

//.items-section {{
//    margin:
//        12px
//        0
//        20px
//        0;

//    font-size: 10.5px;

//    line-height: 1.25;

//    display: grid;

//    grid-template-columns:
//        repeat(3, 1fr);

//    gap: 12px;
//}}

//.item-entry {{
//    padding:
//        6px
//        8px;

//    border:
//        1px solid
//        #ddd;

//    border-radius: 4px;

//    min-height: 60px;

//    page-break-inside: avoid;
//}}

//.item-entry strong {{
//    font-weight: bold;
//}}

//.item-empty {{
//    grid-column:
//        1 / -1;

//    min-height: 30px;
//}}

///* ============================================================
//   REMARKS
//============================================================ */

//.remarks-section {{
//    border:
//        1px solid
//        #000;

//    padding: 11px;

//    margin:
//        5px
//        0
//        135px
//        0;

//    width: 100%;
//}}

//.remarks-label {{
//    font-weight: bold;

//    font-size: 10.5px;

//    margin-bottom: 2px;
//}}

//.remarks-field {{
//    border-bottom:
//        1px solid
//        #000;

//    min-height: 40px;

//    padding: 5px;

//    width: 100%;

//    font-size: 10.5px;
//}}

///* ============================================================
//   SIGNATURE
//============================================================ */

//.stamp-box {{
//    width: 535px;

//    height: 105px;

//    border:
//        2px solid
//        #000;

//    text-align: center;

//    padding: 16px;

//    background: #fff;

//    margin:
//        3px
//        auto
//        0
//        auto;

//    page-break-inside: avoid;
//}}

//.signature-text {{
//    padding-top: 8px;

//    margin-top: 50px;

//    font-weight: bold;

//    font-size: 10.5px;
//}}

///* ============================================================
//   PRINT
//============================================================ */

//@media print {{

//    html,
//    body {{
//        width: 210mm;
//        height: 297mm;

//        margin: 0 !important;
//        padding: 0 !important;

//        background: #fff;
//    }}

//    .page {{
//        width: 210mm !important;

//        min-height: 297mm !important;

//        margin: 0 !important;

//        border:
//            1px solid
//            #000;

//        padding:
//            5mm
//            8mm !important;

//        box-sizing: border-box;

//        position: relative;

//        overflow: hidden;

//        page-break-after: avoid !important;

//        page-break-before: avoid !important;

//        break-after: avoid-page !important;

//        break-before: avoid-page !important;
//    }}

//    .watermark {{
//        position: absolute !important;

//        top: 0 !important;
//        left: 0 !important;
//        right: 0 !important;
//        bottom: 0 !important;

//        width: 100% !important;
//        height: 100% !important;

//        display: flex !important;

//        align-items: center !important;
//        justify-content: center !important;

//        z-index: 0 !important;

//        overflow: hidden !important;
//    }}

//    .watermark span {{
//        transform:
//            rotate(-35deg) !important;

//        font-size: 150px !important;

//        font-weight: 900 !important;

//        color:
//            rgba(128, 128, 128, 0.15) !important;

//        white-space: nowrap !important;
//    }}

//    .page > *:not(.watermark) {{
//        position: relative !important;
//        z-index: 1 !important;
//    }}
//}}

//</style>

//</head>

//<body>

//<div class=""page"">

//    <!-- ======================================================
//         WATERMARK
//    ======================================================= -->

//    {watermarkHtml}

//    <!-- ======================================================
//         LOGO
//    ======================================================= -->

//    {(string.IsNullOrWhiteSpace(logoHtml)
//                ? ""
//                : $@"
//        <div class=""logo-box"">

//            <img
//                src=""{logoHtml}""
//                alt=""Logo""
//                class=""logo""
//            />

//        </div>
//        ")}

//    <!-- ======================================================
//         HEADER
//    ======================================================= -->

//    <div class=""header"">

//        No. 7, Basudev Street, Pondy Bazaar,
//        T. Nagar, Chennai – 600 017

//        <br />

//        Contact: 044 24340714

//    </div>

//    <!-- ======================================================
//         TITLE
//    ======================================================= -->

//    <h2>
//        VENDOR REGISTRATION FORM
//    </h2>

//    <!-- ======================================================
//         REF / CODE / DATE / LOCATION
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            Ref.No.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {refNo}
//                        </span>

//                    </div>

//                    <div class=""field-line"">

//                        <label>
//                            CODE NO.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {codeNo}
//                        </span>

//                    </div>

//                </td>

//                <td class=""right-align"">

//                    <div class=""field-line"">

//                        <label>
//                            Date:
//                        </label>

//                        <span class=""readonly-field"">
//                            {formDate}
//                        </span>

//                    </div>

//                    <div class=""field-line"">

//                        <label>
//                            LOCATION:
//                        </label>

//                        <span class=""readonly-field"">
//                            {location}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         1.VENDOR
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            1.Name of Vendor:
//        </label>

//        <span class=""readonly-field"">
//            {vendorName}
//        </span>

//    </div>

//    <!-- ======================================================
//         2.ADDRESS
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            2.Address:
//        </label>

//        <span class=""readonly-field"">
//            {billingAddress}
//        </span>

//    </div>

//    <div class=""field-line"">

//        <label>
//            Registered Office:
//        </label>

//        <span class=""readonly-field"">
//            {registeredAddress}
//        </span>

//    </div>

//    <!-- ======================================================
//         3.NATURE OF BUSINESS
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            3.Nature of Business:
//        </label>

//        <span class=""readonly-field"">
//            {natureOfBusiness}
//        </span>

//    </div>

//    <!-- ======================================================
//         4.CONTACT
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            4.Contact No. 1:
//                        </label>

//                        <span class=""readonly-field"">
//                            {mobileNumber}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            Contact No. 2:
//                        </label>

//                        <span class=""readonly-field"">
//                            {officeTelephone}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         5 / 6 EMAIL
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            5.Email ID 1:
//                        </label>

//                        <span class=""readonly-field"">
//                            {email1}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            6.Email ID 2:
//                        </label>

//                        <span class=""readonly-field"">
//                            {email2}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         7 / 8 PROPRIETOR
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            7.Name of Proprietor:
//                        </label>

//                        <span class=""readonly-field"">
//                            {proprietor}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            8.Phone No.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {proprietorPhone}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         9.RTGS
//    ======================================================= -->

//    <div class=""section-title"">
//        9.RTGS Details
//    </div>

//    <div class=""field-line"">

//        <label>
//            Name of Bank and Branch:
//        </label>

//        <span class=""readonly-field"">
//            {bankName}
//        </span>

//    </div>

//    <div class=""field-line"">

//        <label>
//            A/c No.:
//        </label>

//        <span class=""readonly-field"">
//            {accountNumber}
//        </span>

//    </div>

//    <div class=""field-line"">

//        <label>
//            IFSC Code:
//        </label>

//        <span class=""readonly-field"">
//            {ifscCode}
//        </span>

//    </div>

//    <!-- ======================================================
//         10.GOODS RETURN
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            10.Goods Return Address:
//        </label>

//        <span class=""readonly-field"">
//            {goodsReturnAddress}
//        </span>

//    </div>

//    <!-- ======================================================
//         PAYMENT
//    ======================================================= -->

//    <div class=""section-title"">
//        Payment Days / Terms
//    </div>

//    <div class=""field-line"">

//        <label>
//            Days:
//        </label>

//        <span class=""readonly-field"">
//            {creditDays}
//        </span>

//    </div>

//    <!-- ======================================================
//         MARKDOWN
//    ======================================================= -->

//    <table class=""markdown-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (1) Mark Down % on MRP
//                            <br />
//                            (With Tax @ 0%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md0With}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (2) Mark Down % on MRP
//                            <br />
//                            (Without Tax @ 0%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md0Without}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (3) Mark Down % on MRP
//                            <br />
//                            (With Tax @ 3%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md3With}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (4) Mark Down % on MRP
//                            <br />
//                            (Without Tax @ 3%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md3Without}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (5) Mark Down % on MRP
//                            <br />
//                            (With Tax @ 5%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md5With}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (6) Mark Down % on MRP
//                            <br />
//                            (Without Tax @ 5%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md5Without}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (7) Mark Down % on MRP
//                            <br />
//                            (With Tax @ 18%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md18With}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            (8) Mark Down % on MRP
//                            <br />
//                            (Without Tax @ 18%)
//                        </label>

//                        <span class=""readonly-field"">
//                            {md18Without}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         DISCOUNT
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            Discount:
//        </label>

//        <span class=""readonly-field"">
//            {discount}
//        </span>

//    </div>

//    <!-- ======================================================
//         GST / PAN
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            12.GST No.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {gstNumber}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            13.PAN No.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {panNumber}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         MSME
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            14.MSME No.:
//                        </label>

//                        <span class=""readonly-field"">
//                            {msmeNumber}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            Date:
//                        </label>

//                        <span class=""readonly-field"">
//                            {msmeDate}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         MAJOR ACTIVITY
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            Major Activity:
//        </label>

//        <span class=""readonly-field"">
//            {majorActivity}
//        </span>

//    </div>

//    <!-- ======================================================
//         ENTERPRISE TYPE
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            Enterprise Type:
//        </label>

//        <span class=""readonly-field"">
//            {enterpriseType}
//        </span>

//    </div>

//    <!-- ======================================================
//         LEGAL / TRADE
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            15.Legal Name:
//                        </label>

//                        <span class=""readonly-field"">
//                            {legalName}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            16.Trade Name:
//                        </label>

//                        <span class=""readonly-field"">
//                            {tradeName}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         AGENCY
//    ======================================================= -->

//    <table class=""ref-table"">

//        <tbody>

//            <tr>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            17.Agency / Direct:
//                        </label>

//                        <span class=""readonly-field"">
//                            {businessType}
//                        </span>

//                    </div>

//                </td>

//                <td>

//                    <div class=""field-line"">

//                        <label>
//                            18.Agency Email:
//                        </label>

//                        <span class=""readonly-field"">
//                            {agencyEmail}
//                        </span>

//                    </div>

//                </td>

//            </tr>

//        </tbody>

//    </table>

//    <!-- ======================================================
//         19.ITEMS SUPPLIED
//    ======================================================= -->

//    <div class=""section-title"">
//        19.Items Supplied
//    </div>

//    <div class=""items-section"">
//        {goodsHtml}
//    </div>

//    <!-- ======================================================
//         20.NHFS CONTACT
//    ======================================================= -->

//    <div class=""field-line"">

//        <label>
//            20.NHFS Contact Person:
//        </label>

//        <span class=""readonly-field"">
//            {nhfsContactPerson}
//        </span>

//    </div>

//    <!-- ======================================================
//         REMARKS
//    ======================================================= -->

//    <div class=""remarks-section"">

//        <div class=""remarks-label"">
//            Remarks:
//        </div>

//        <div class=""remarks-field"">
//            {remarks}
//        </div>

//    </div>

//    <!-- ======================================================
//         SIGNATURE
//    ======================================================= -->

//    <div class=""stamp-box"">

//        <div class=""signature-text"">

//            (Signature with Rubber Stamp)

//            <br />

//            AUTHORISED SIGNATORY

//        </div>

//    </div>

//</div>

//</body>

//</html>";

            return html;
        }
        //     public string GenerateVendorHtmlWithData(
        //Dictionary<string, object> data,
        //IEnumerable<object> goodsList)
        //     {
        //         string GetValue(string key)
        //         {
        //             if (data == null || !data.ContainsKey(key))
        //                 return "";

        //             return data[key]?.ToString() ?? "";
        //         }

        //         string HtmlEncode(string value)
        //         {
        //             return System.Net.WebUtility.HtmlEncode(
        //                 value ?? ""
        //             );
        //         }

        //         string vendorName =
        //             HtmlEncode(GetValue("Trade Name"));

        //         string billingAddress =
        //             HtmlEncode(GetValue("Billing Address"));

        //         string registeredAddress =
        //             HtmlEncode(GetValue("Registered Address"));

        //         string natureOfBusiness =
        //             HtmlEncode(GetValue("Nature of Business"));

        //         string mobileNumber =
        //             HtmlEncode(GetValue("Mobile Number"));

        //         string officeTelephone =
        //             HtmlEncode(GetValue("Office Telephone"));

        //         string email =
        //             HtmlEncode(GetValue("Email ID"));

        //         string agencyEmail =
        //             HtmlEncode(GetValue("Agency Email"));

        //         string contactPerson =
        //             HtmlEncode(GetValue("Contact Person"));

        //         string bankName =
        //             HtmlEncode(GetValue("Bank Name"));

        //         string accountNumber =
        //             HtmlEncode(GetValue("Account Number"));

        //         string ifscCode =
        //             HtmlEncode(GetValue("IFSC Code"));

        //         string goodsReturnAddress =
        //             HtmlEncode(GetValue("Goods Return Address"));

        //         string creditDays =
        //             HtmlEncode(GetValue("Credit Days"));

        //         string discount =
        //             HtmlEncode(GetValue("Discount"));

        //         string gstNumber =
        //             HtmlEncode(GetValue("GST Number"));

        //         string panNumber =
        //             HtmlEncode(GetValue("PAN Number"));

        //         string msmeNumber =
        //             HtmlEncode(GetValue("MSME Number"));

        //         string enterpriseType =
        //             HtmlEncode(GetValue("Enterprise Type"));

        //         string majorActivity =
        //             HtmlEncode(GetValue("Major Activity"));

        //         string legalName =
        //             HtmlEncode(GetValue("Legal Name"));

        //         string businessType =
        //             HtmlEncode(GetValue("Business Type"));

        //         string nhfsContactPerson =
        //             HtmlEncode(GetValue("NHFS Contact Person"));

        //         string formDate =
        //             HtmlEncode(GetValue("Date"));

        //         // ------------------------------------------------------------
        //         // GOODS TABLE
        //         // ------------------------------------------------------------

        //         StringBuilder goodsHtml =
        //             new StringBuilder();

        //         int goodsIndex = 1;

        //         if (goodsList != null)
        //         {
        //             foreach (var item in goodsList)
        //             {
        //                 if (item == null)
        //                     continue;

        //                 string description = "";
        //                 string category = "";
        //                 string code = "";

        //                 // Supports your existing model without forcing
        //                 // one exact property structure here.
        //                 var type = item.GetType();

        //                 var property =
        //                     type.GetProperty("Description");

        //                 if (property != null)
        //                 {
        //                     description =
        //                         property.GetValue(item)?.ToString() ?? "";
        //                 }

        //                 property =
        //                     type.GetProperty("Goods");

        //                 if (string.IsNullOrWhiteSpace(description) &&
        //                     property != null)
        //                 {
        //                     description =
        //                         property.GetValue(item)?.ToString() ?? "";
        //                 }

        //                 property =
        //                     type.GetProperty("Name");

        //                 if (string.IsNullOrWhiteSpace(description) &&
        //                     property != null)
        //                 {
        //                     description =
        //                         property.GetValue(item)?.ToString() ?? "";
        //                 }

        //                 property =
        //                     type.GetProperty("Category");

        //                 if (property != null)
        //                 {
        //                     category =
        //                         property.GetValue(item)?.ToString() ?? "";
        //                 }

        //                 property =
        //                     type.GetProperty("ItemCode");

        //                 if (property != null)
        //                 {
        //                     code =
        //                         property.GetValue(item)?.ToString() ?? "";
        //                 }

        //                 goodsHtml.Append($@"
        //                     <tr>
        //                         <td style=""text-align:center;"">
        //                             {goodsIndex}
        //                         </td>

        //                         <td>
        //                             {HtmlEncode(description)}
        //                         </td>

        //                         <td>
        //                             {HtmlEncode(category)}
        //                         </td>

        //                         <td>
        //                             {HtmlEncode(code)}
        //                         </td>
        //                     </tr>");

        //                 goodsIndex++;
        //             }
        //         }

        //         if (goodsIndex == 1)
        //         {
        //             goodsHtml.Append(@"
        //                 <tr>
        //                     <td colspan=""4"" style=""height:30px;"">
        //                         &nbsp;
        //                     </td>
        //                 </tr>");
        //         }

        //         // ------------------------------------------------------------
        //         // LOGO
        //         // ------------------------------------------------------------

        //         string logoPath =
        //             Path.Combine(
        //                 Directory.GetCurrentDirectory(),
        //                 "wwwroot",
        //                 "Images",
        //                 "Logo.png"
        //             );

        //         string logoHtml = "";

        //         if (File.Exists(logoPath))
        //         {
        //             byte[] logoBytes =
        //                 File.ReadAllBytes(logoPath);

        //             string base64Logo =
        //                 Convert.ToBase64String(logoBytes);

        //             logoHtml =
        //                 $"data:image/png;base64,{base64Logo}";
        //         }

        //         // ------------------------------------------------------------
        //         // WATERMARK
        //         // ------------------------------------------------------------

        //         string watermark = "DRAFT";

        //         // ------------------------------------------------------------
        //         // FINAL HTML
        //         // ------------------------------------------------------------

        //         string html = $@"
        //     <!DOCTYPE html>

        //     <html>

        //     <head>

        //     <meta charset=""UTF-8"" />

        //     <title>Vendor Registration Form</title>

        //     <style>

        //         @page {{
        //             size: A4;
        //             margin: 0;
        //         }}

        //         html,
        //         body {{
        //             margin: 0;
        //             padding: 0;
        //             background: white;
        //         }}

        //         body {{
        //             font-family: ""Times New Roman"", serif;
        //             font-size: 12px;
        //         }}

        //         .page {{
        //             position: relative;

        //             width: 1094px;
        //             min-height: 1123px;

        //             margin: 0 auto;

        //             padding:
        //                 25px
        //                 35px
        //                 25px
        //                 35px;

        //             box-sizing: border-box;

        //             background: white;

        //             overflow: hidden;
        //         }}

        //         .page * {{
        //             position: relative;
        //             z-index: 1;
        //         }}

        //         .watermark {{
        //             position: absolute !important;

        //             top: 50% !important;
        //             left: 50% !important;

        //             transform:
        //                 translate(-50%, -50%)
        //                 rotate(-35deg) !important;

        //             width: 90%;

        //             text-align: center;

        //             font-size: 260px;

        //             font-weight: 900;

        //             font-family: ""Arial Black"", sans-serif;

        //             text-transform: uppercase;

        //             letter-spacing: 10px;

        //             color: rgba(0, 0, 0, 0.18);

        //             opacity: 0.25;

        //             pointer-events: none;

        //             user-select: none;

        //             z-index: 0 !important;

        //             white-space: nowrap;
        //         }}

        //         .logo-box {{
        //             width: 145px;
        //             height: 55px;

        //             border: 1px solid #000;

        //             display: flex;

        //             align-items: center;

        //             justify-content: center;

        //             margin-bottom: 5px;
        //         }}

        //         .logo {{
        //             width: 125px;
        //             height: auto;
        //         }}

        //         .header {{
        //             text-align: center;

        //             font-size: 13px;

        //             font-weight: bold;

        //             margin-bottom: 10px;
        //         }}

        //         h2 {{
        //             text-align: center;

        //             font-size: 18px;

        //             margin:
        //                 8px
        //                 0
        //                 12px
        //                 0;

        //             text-decoration: underline;
        //         }}

        //         .ref-table {{
        //             width: 100%;

        //             border-collapse: collapse;

        //             margin-bottom: 5px;
        //         }}

        //         .ref-table td {{
        //             width: 50%;

        //             vertical-align: top;

        //             padding: 0;
        //         }}

        //         .right-align {{
        //             text-align: right;
        //         }}

        //         .field-line {{
        //             display: flex;

        //             align-items: center;

        //             min-height: 22px;
        //         }}

        //         .field-line label {{
        //             width: 230px;

        //             text-align: left;

        //             font-weight: bold;

        //             flex-shrink: 0;
        //         }}

        //         .readonly-field {{
        //             display: inline-block;

        //             text-align: left;

        //             flex: 1;

        //             border-bottom: 1px solid #000;

        //             min-height: 16px;

        //             padding-left: 3px;
        //         }}

        //         .section-title {{
        //             font-weight: bold;

        //             margin-top: 8px;

        //             margin-bottom: 3px;
        //         }}

        //         table.data-table {{
        //             width: 100%;

        //             border-collapse: collapse;

        //             margin-top: 5px;

        //             margin-bottom: 8px;
        //         }}

        //         table.data-table th,
        //         table.data-table td {{
        //             border: 1px solid #000;

        //             padding: 4px;

        //             vertical-align: top;
        //         }}

        //         table.data-table th {{
        //             text-align: center;

        //             font-weight: bold;
        //         }}

        //         .signature {{
        //             margin-top: 30px;

        //             width: 100%;
        //         }}

        //         .signature-table {{
        //             width: 100%;

        //             border-collapse: collapse;
        //         }}

        //         .signature-table td {{
        //             width: 50%;

        //             height: 80px;

        //             vertical-align: bottom;

        //             padding: 5px;
        //         }}

        //         .signature-line {{
        //             border-top: 1px solid #000;

        //             width: 80%;

        //             margin-top: 35px;
        //         }}

        //         .small {{
        //             font-size: 11px;
        //         }}

        //     </style>

        //     </head>

        //     <body>

        //     <div class=""page"">

        //         <div class=""watermark"">
        //             {HtmlEncode(watermark)}
        //         </div>

        //         {(string.IsNullOrWhiteSpace(logoHtml)
        //             ? ""
        //             : $@"<div class=""logo-box"">
        //                     <img
        //                         src=""{logoHtml}""
        //                         alt=""Logo""
        //                         class=""logo""
        //                     />
        //                 </div>")}

        //         <div class=""header"">
        //             No. 7, Basudev Street, Pondy Bazaar,
        //             T. Nagar, Chennai – 600 017
        //             Contact: 044 24340714
        //         </div>

        //         <h2>
        //             VENDOR REGISTRATION FORM
        //         </h2>

        //         <!-- REF / CODE / DATE / LOCATION -->

        //         <table class=""ref-table"">

        //             <tr>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>Ref. No.:</label>
        //                         <span class=""readonly-field"">
        //                         </span>
        //                     </div>

        //                     <div class=""field-line"">
        //                         <label>CODE NO.:</label>
        //                         <span class=""readonly-field"">
        //                         </span>
        //                     </div>

        //                 </td>

        //                 <td class=""right-align"">

        //                     <div class=""field-line"">
        //                         <label>Date:</label>
        //                         <span class=""readonly-field"">
        //                             {formDate}
        //                         </span>
        //                     </div>

        //                     <div class=""field-line"">
        //                         <label>LOCATION:</label>
        //                         <span class=""readonly-field"">
        //                         </span>
        //                     </div>

        //                 </td>

        //             </tr>

        //         </table>


        //         <!-- BASIC DETAILS -->

        //         <div class=""field-line"">
        //             <label>1. Name of Vendor:</label>
        //             <span class=""readonly-field"">
        //                 {vendorName}
        //             </span>
        //         </div>

        //         <div class=""field-line"">
        //             <label>2. Address:</label>
        //             <span class=""readonly-field"">
        //                 {billingAddress}
        //             </span>
        //         </div>

        //         <div class=""field-line"">
        //             <label>Registered Office:</label>
        //             <span class=""readonly-field"">
        //                 {registeredAddress}
        //             </span>
        //         </div>

        //         <div class=""field-line"">
        //             <label>3. Nature of Business:</label>
        //             <span class=""readonly-field"">
        //                 {natureOfBusiness}
        //             </span>
        //         </div>


        //         <!-- CONTACT -->

        //         <table class=""ref-table"">

        //             <tr>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>4. Contact No. 1:</label>
        //                         <span class=""readonly-field"">
        //                             {mobileNumber}
        //                         </span>
        //                     </div>

        //                 </td>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>Contact No. 2:</label>
        //                         <span class=""readonly-field"">
        //                             {officeTelephone}
        //                         </span>
        //                     </div>

        //                 </td>

        //             </tr>

        //         </table>


        //         <div class=""field-line"">
        //             <label>Email ID:</label>
        //             <span class=""readonly-field"">
        //                 {email}
        //             </span>
        //         </div>

        //         <div class=""field-line"">
        //             <label>Contact Person:</label>
        //             <span class=""readonly-field"">
        //                 {contactPerson}
        //             </span>
        //         </div>


        //         <!-- BANK -->

        //         <div class=""section-title"">
        //             RTGS / BANK DETAILS
        //         </div>

        //         <table class=""data-table"">

        //             <tr>
        //                 <th>Bank Name</th>
        //                 <th>Account Number</th>
        //                 <th>IFSC Code</th>
        //             </tr>

        //             <tr>
        //                 <td>{bankName}</td>
        //                 <td>{accountNumber}</td>
        //                 <td>{ifscCode}</td>
        //             </tr>

        //         </table>


        //         <!-- GOODS RETURN -->

        //         <div class=""field-line"">
        //             <label>Goods Return Address:</label>
        //             <span class=""readonly-field"">
        //                 {goodsReturnAddress}
        //             </span>
        //         </div>


        //         <!-- PAYMENT -->

        //         <div class=""section-title"">
        //             PAYMENT DETAILS
        //         </div>

        //         <table class=""data-table"">

        //             <tr>
        //                 <th>Credit Days</th>
        //                 <th>Discount</th>
        //             </tr>

        //             <tr>
        //                 <td>{creditDays}</td>
        //                 <td>{discount}</td>
        //             </tr>

        //         </table>


        //         <!-- GST / PAN -->

        //         <table class=""ref-table"">

        //             <tr>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>GST Number:</label>
        //                         <span class=""readonly-field"">
        //                             {gstNumber}
        //                         </span>
        //                     </div>

        //                 </td>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>PAN Number:</label>
        //                         <span class=""readonly-field"">
        //                             {panNumber}
        //                         </span>
        //                     </div>

        //                 </td>

        //             </tr>

        //         </table>


        //         <!-- MSME -->

        //         <div class=""section-title"">
        //             MSME DETAILS
        //         </div>

        //         <table class=""data-table"">

        //             <tr>

        //                 <th>MSME Number</th>

        //                 <th>Enterprise Type</th>

        //                 <th>Major Activity</th>

        //             </tr>

        //             <tr>

        //                 <td>
        //                     {msmeNumber}
        //                 </td>

        //                 <td>
        //                     {enterpriseType}
        //                 </td>

        //                 <td>
        //                     {majorActivity}
        //                 </td>

        //             </tr>

        //         </table>


        //         <!-- LEGAL -->

        //         <table class=""ref-table"">

        //             <tr>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>Legal Name:</label>
        //                         <span class=""readonly-field"">
        //                             {legalName}
        //                         </span>
        //                     </div>

        //                 </td>

        //                 <td>

        //                     <div class=""field-line"">
        //                         <label>Business Type:</label>
        //                         <span class=""readonly-field"">
        //                             {businessType}
        //                         </span>
        //                     </div>

        //                 </td>

        //             </tr>

        //         </table>


        //         <!-- AGENCY -->

        //         <div class=""field-line"">
        //             <label>Agency Email:</label>
        //             <span class=""readonly-field"">
        //                 {agencyEmail}
        //             </span>
        //         </div>


        //         <!-- MAJOR GOODS -->

        //         <div class=""section-title"">
        //             MAJOR GOODS AND SERVICES
        //         </div>

        //         <table class=""data-table"">

        //             <tr>

        //                 <th style=""width:8%;"">
        //                     S.No
        //                 </th>

        //                 <th>
        //                     Description
        //                 </th>

        //                 <th>
        //                     Category
        //                 </th>

        //                 <th>
        //                     Item Code
        //                 </th>

        //             </tr>

        //             {goodsHtml}

        //         </table>


        //         <!-- NHFS CONTACT -->

        //         <div class=""field-line"">
        //             <label>NHFS Contact Person:</label>
        //             <span class=""readonly-field"">
        //                 {nhfsContactPerson}
        //             </span>
        //         </div>


        //         <!-- SIGNATURE -->

        //         <div class=""signature"">

        //             <table class=""signature-table"">

        //                 <tr>

        //                     <td>

        //                         <div class=""signature-line""></div>

        //                         <div class=""small"">
        //                             Vendor Signature
        //                         </div>

        //                     </td>

        //                     <td>

        //                         <div class=""signature-line""></div>

        //                         <div class=""small"">
        //                             NHFS Authorized Signatory
        //                         </div>

        //                     </td>

        //                 </tr>

        //             </table>

        //         </div>

        //     </div>

        //     </body>

        //     </html>";

        //         return html;
        //     }
        //        public byte[] ConvertHtmlToPdf(string htmlContent)
        //{
        //    if (string.IsNullOrWhiteSpace(htmlContent))
        //    {
        //        throw new ArgumentException(
        //            "HTML content is empty.",
        //            nameof(htmlContent)
        //        );
        //    }

        //    try
        //    {
        //        // =========================================================
        //        // CHECK libwkhtmltox.dll
        //        // =========================================================

        //        string baseDirectory = AppContext.BaseDirectory;

        //        string dllPath = Path.Combine(
        //            baseDirectory,
        //            "libwkhtmltox.dll"
        //        );

        //        log.WriteToLogFile_Debug(
        //            $"[ConvertHtmlToPdf] Base Directory: {baseDirectory}",
        //            "ConvertHtmlToPdf"
        //        );

        //        log.WriteToLogFile_Debug(
        //            $"[ConvertHtmlToPdf] DLL Path: {dllPath}",
        //            "ConvertHtmlToPdf"
        //        );

        //        log.WriteToLogFile_Debug(
        //            $"[ConvertHtmlToPdf] DLL Exists: {File.Exists(dllPath)}",
        //            "ConvertHtmlToPdf"
        //        );

        //        if (!File.Exists(dllPath))
        //        {
        //            throw new FileNotFoundException(
        //                $"libwkhtmltox.dll was not found at: {dllPath}"
        //            );
        //        }

        //        // =========================================================
        //        // CREATE PDF DOCUMENT
        //        // =========================================================

        //        var document = new HtmlToPdfDocument
        //        {
        //            GlobalSettings =
        //            {
        //                ColorMode = ColorMode.Color,

        //                Orientation =
        //                    Orientation.Portrait,

        //                PaperSize =
        //                    PaperKind.A4,

        //                Margins =
        //                {
        //                    Top = 0,
        //                    Bottom = 0,
        //                    Left = 0,
        //                    Right = 0
        //                },

        //                DocumentTitle =
        //                    "Vendor Registration Form"
        //            },

        //            Objects =
        //            {
        //                new ObjectSettings
        //                {
        //                    HtmlContent = htmlContent,

        //                    WebSettings =
        //                    {
        //                        DefaultEncoding = "utf-8",

        //                        LoadImages = true,

        //                        EnableJavascript = false
        //                    },

        //                    UseLocalLinks = true
        //                }
        //            }
        //        };

        //        // =========================================================
        //        // CONVERT
        //        // =========================================================

        //        log.WriteToLogFile_Debug(
        //            "[ConvertHtmlToPdf] Starting PDF conversion",
        //            "ConvertHtmlToPdf"
        //        );

        //        byte[] pdfBytes =
        //            _converter.Convert(document);

        //        // =========================================================
        //        // VALIDATE RESULT
        //        // =========================================================

        //        if (pdfBytes == null ||
        //            pdfBytes.Length == 0)
        //        {
        //            throw new Exception(
        //                "PDF conversion returned an empty file."
        //            );
        //        }

        //        log.WriteToLogFile_Debug(
        //            $"[ConvertHtmlToPdf] PDF generated successfully. Size: {pdfBytes.Length} bytes",
        //            "ConvertHtmlToPdf"
        //        );

        //        return pdfBytes;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.WriteToLogFile_Debug(
        //            "[ConvertHtmlToPdf] ERROR - " +
        //            ex.ToString(),
        //            "ConvertHtmlToPdf"
        //        );

        //        throw;
        //    }
        //}
        public byte[] ConvertHtmlToPdf(string htmlContent)
        {
            // Logo as base64 (already working)
            string logoPath = _configuration["Folder:LogoPath"];
            if (System.IO.File.Exists(logoPath))
            {
                byte[] logoBytes = System.IO.File.ReadAllBytes(logoPath);
                string base64Logo = Convert.ToBase64String(logoBytes);
                htmlContent = htmlContent.Replace("../Images/Logo.png", $"data:image/png;base64,{base64Logo}");
            }

            HtmlToPdf converter = new HtmlToPdf();

            // ✅ CRITICAL PAGE SPLIT FIXES
            converter.Options.MaxPageLoadTime = 180;
            converter.Options.MinPageLoadTime = 5;
            converter.Options.KeepTextsTogether = true;  // MOST IMPORTANT

            // ✅ Precise page sizing
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;

            // ✅ EXACT margins matching your 794px page width
            converter.Options.MarginTop = 30;
            converter.Options.MarginBottom = 5;  // Space for stamp
            converter.Options.MarginLeft = 28;
            converter.Options.MarginRight = 28;

            SelectPdf.PdfDocument doc = converter.ConvertHtmlString(htmlContent);

            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);
                doc.Close();
                return ms.ToArray();
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

namespace VRF_API.Repository
{
    public class CommonRepo
    {
        public class PartnerDetails
        {
            public int SI_No { get; set; }
            public string Name { get; set; }
            public string Designation { get; set; }
            public string Contact_No { get; set; }
            public string Email_ID { get; set; }
            public int RowID { get; internal set; }
        }
        public class OperationalContact
        {
            public string Department { get; set; }
            public string Name { get; set; }
            public string Designation { get; set; }
            public string ContactNo { get; set; }
            public string Email { get; set; }
        }
        public class OtherInformation
        {
            public string Description { get; set; }
            public string TextMode { get; set; }
        }
        public class KYCDocument
        {
            public string DocumentType { get; set; }
            public string FileData { get; set; }
        }
        public class BusinessDetails
        {
            public string BusinessState { get; set; }
            public string GSTNumber { get; set; }
            public string AddressOfPlace { get; set; }
            public string GSTVendorClassification { get; set; }

        }
        public class MajorGoodsService
        {
            public int SI_No { get; set; }
            public string MaterialDescription { get; set; }
            public string HSNCode { get; set; }
            public string Brand { get; set; }
            public string Size { get; set; }
            public string Product { get; set; }
            public string TaxPercentage { get; set; }
        }
        public class MajorCustomers
        {
            public string CustomerName { get; set; }

        }
        public class State
        {
            public string StateName { get; set; }
            public string StateCode { get; set; }
        }
        public class Bank
        {
            public string BankName { get; set; }
            public string BankCode { get; set; }
        }
        public class Country
        {
            public string CountryName { get; set; }
            public string CountryCode { get; set; }
        }
        public class DocumentDetail
        {
            public string DocumentType { get; set; }
            public string DocumentName { get; set; }
        }

        public class UploadKYCFileRequest
        {
            public IFormFile file { get; set; }
            public string documentType { get; set; }
            public int rowIndex { get; set; }
        }
        public class BankDocumentResponse
        {
            public string? AccountNumber { get; set; }
            public string? AccountNameHolder { get; set; }
            public string? IfscCode { get; set; }
            public string? BankCode { get; set; }
        }

        public class GstDocumentResponse
        {
            public string? LegalName { get; set; }
            public string? TradeName { get; set; }
            public string? GstNumber { get; set; }

            public string? Building { get; set; }
            public string? Street { get; set; }
            public string? Locality { get; set; }
            public string? City { get; set; }
            public string? District { get; set; }
            public string? State { get; set; }
            public string? Pincode { get; set; }
        }

        public class MsmeDocumentResponse
        {
            public string? RegisterNumber { get; set; }
            public string? EnterpriseType { get; set; }
            public string? MajorActivity { get; set; }
            public bool IsValid { get; set; }
        }

        public class PanDocumentResponse
        {
            public string? PanNumber { get; set; }
            public string? DateOfIncorporation { get; set; }
        }

        public class KycUploadResponse
        {
            public string? DocumentType { get; set; }
            public int RowIndex { get; set; }
            public string? FileName { get; set; }
            public string? FilePath { get; set; }

            public BankDocumentResponse? BankDetails { get; set; }
            public GstDocumentResponse? GstDetails { get; set; }
            public MsmeDocumentResponse? MsmeDetails { get; set; }
            public PanDocumentResponse? PanDetails { get; set; }
        }
        public class GstDetails
        {
            public string? legal_name { get; set; }
            public string? trade_name { get; set; }
            public string? gst_number { get; set; }

            public GstAddress? address_in_7_separate_feilds { get; set; }
        }

        public class GstAddress
        {
            public string? building { get; set; }
            public string? street { get; set; }
            public string? locality { get; set; }
            public string? city { get; set; }
            public string? district { get; set; }
            public string? state { get; set; }
            public string? pincode { get; set; }
        }
        public class UdyamDetails
        {
            public string? register_number { get; set; }
            public string? enterprise_type { get; set; }
            public string? major_activity { get; set; }
        }
        public class PanDetails
        {
            public string? pan_no { get; set; }
            public string? date_of_incorporation { get; set; }
        }
        public class ViewKYCFile
        {
            public string fileName { get; set; }
            public string gstNumber { get; set; }
            public string documentType { get; set; }
        }

        public class FileResultModel
        {
            public byte[] FileBytes { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
        }
    }
}

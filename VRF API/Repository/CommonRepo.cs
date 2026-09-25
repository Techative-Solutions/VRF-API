using static VRF_API.Repository.CommonRepo;

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
            public string ImageFile { get; set; }
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

        public class NextPageCheckRequest
        {
            public string gstNumber { get; set; }
            public int page { get; set; }
        }
        public class GstNumberCheckRequest
        {
            public string gstNumber { get; set; }
        }
        public class GstResponse
        {
            public string PANNumber { get; set; }

            public OLEDDetailsDto VendorDetails { get; set; }
            public PaymentDetailsDto PaymentDetails { get; set; }

            public List<BusinessDetails> BusinessDetails { get; set; } = new List<BusinessDetails>();
            public List<PartnerDetails> PartnerDetails { get; set; } = new List<PartnerDetails>();
            public List<OperationalContact> OperationalContacts { get; set; } = new List<OperationalContact>();
            public List<MajorGoodsService> MajorGoodsServices { get; set; } = new List<MajorGoodsService>();
            public List<MajorCustomerDto> MajorCustomers { get; set; } = new List<MajorCustomerDto>();
            public List<OtherInformation> OtherInformation { get; set; } = new List<OtherInformation>();
            public List<KYCDocumentDto> KYCDocuments { get; set; }
                = new List<KYCDocumentDto>();
            public List<DocumentDetails> Documents { get; set; }
                = new List<DocumentDetails>();
            public DraftDetails draftDetails { get; set; }
        }
        public class DraftDetails
        {
            public bool DraftApproved { get; set; }
            public int page { get; set; }
        }
        public class DocumentDetails
        {
            public int LineId { get; set; }

            public string DocumentType { get; set; }

            public string DocumentName { get; set; }

            public string FileData { get; set; }
        }
        public class KYCDocumentDto
        {
            public int LineId { get; set; }

            public string DocumentType { get; set; }

            public string DocumentName { get; set; }

            public string FileData { get; set; }
        }
       
        public class OLEDDetailsDto
        {
            public string? TName { get; set; }
            public string? PartnerType { get; set; }

            public string? Raddress1 { get; set; }
            public string? Raddress2 { get; set; }
            public string? Raddress3 { get; set; }
            public string? Rcountry { get; set; }
            public string? Rstate { get; set; }
            public string? Rzipcode { get; set; }
            public string? RegisteredOfficeCity { get; set; }

            public string? Gaddress1 { get; set; }
            public string? Gaddress2 { get; set; }
            public string? Gaddress3 { get; set; }
            public string? Gcountry { get; set; }
            public string? Gstate { get; set; }
            public string? Gzipcode { get; set; }
            public string? Gcity { get; set; }

            public string? Saddress1 { get; set; }
            public string? Saddress2 { get; set; }
            public string? Saddress3 { get; set; }
            public string? Scountry { get; set; }
            public string? Sstate { get; set; }
            public string? Szipcode { get; set; }
            public string? Scity { get; set; }

            public string? Baddress1 { get; set; }
            public string? Baddress2 { get; set; }
            public string? Baddress3 { get; set; }
            public string? Bcountry { get; set; }
            public string? Bstate { get; set; }
            public string? Bzipcode { get; set; }
            public string? BusinessBillingCity { get; set; }

            public string? NatureOfBusinessActivity { get; set; }
            public string? DateOfEstablishment { get; set; }
            public string? ContactPersonName { get; set; }
            public string? Designation { get; set; }
            public string? EmailId { get; set; }
            public string? MobileNo { get; set; }
            public string? OfficeTelephoneNo { get; set; }

            public string? TANNo { get; set; }

            public string? MSMERegistrationStatus { get; set; }
            public string? MSMENo { get; set; }

            public string? BankName { get; set; }
            public string? AccountName { get; set; }
            public string? AccountNumber { get; set; }
            public string? IfscCode { get; set; }
            public string? BranchCode { get; set; }
            public string? BankAddress { get; set; }

            public string? DeclarationName { get; set; }
            public string? DeclarationDesignation { get; set; }

            public string? EnterpriseType { get; set; }
            public string? BusinessType { get; set; }

            public string? AgencyEmail { get; set; }
            public string? AgencyName { get; set; }

            public string? VerificationNo { get; set; }
            public string? ContactPerson { get; set; }
        }
        public class BusinessDetailsDto
        {
            public string? BusinessState { get; set; }
            public string? GSTNumber { get; set; }
            public string? AddressOfPlace { get; set; }
            public string? GSTVendorClassification { get; set; }
        }

        public class PartnerDetailsDto
        {
            public string? Name { get; set; }
            public string? Designation { get; set; }
            public string? ContactNo { get; set; }
            public string? EmailId { get; set; }
        }

        public class OperationalContactDto
        {
            public string? Department { get; set; }
            public string? Name { get; set; }
            public string? Designation { get; set; }
            public string? ContactNo { get; set; }
            public string? Email { get; set; }
        }

        public class MajorGoodsServiceDto
        {
            public string? MaterialDescription { get; set; }
            public string? HSNCode { get; set; }
            public string? Brand { get; set; }
            public string? Size { get; set; }
            public string? Product { get; set; }
            public string? TaxPercentage { get; set; }
        }

        public class MajorCustomerDto
        {
            public string? CustomerName { get; set; }
        }

        public class OtherInformationDto
        {
            public string? Description { get; set; }
            public string? TextMode { get; set; }
        }
        public class PaymentDetailsDto
        {
            public string? CreditDays { get; set; }
            public string? DisCount { get; set; }
            public string? PriceType { get; set; }

            public string? MarkDownTax0 { get; set; }
            public string? MarkDownWithoutTax0 { get; set; }

            public string? MarkDownTax3 { get; set; }
            public string? MarkDownWithoutTax3 { get; set; }

            public string? MarkDownTax5 { get; set; }
            public string? MarkDownWithoutTax5 { get; set; }

            public string? MarkDownTax18 { get; set; }
            public string? MarkDownWithoutTax18 { get; set; }

            public string? BusinessType { get; set; }
            public string? AgencyEMail { get; set; }
            public string? AgencyName { get; set; }
        }
        public class VendorEditDetailsDto
        {
            public OLEDDetailsDto? VendorDetails { get; set; }
            public PaymentDetailsDto? PaymentDetails { get; set; }

            public List<BusinessDetailsDto> BusinessDetails { get; set; } = new();
            public List<PartnerDetailsDto> PartnerDetails { get; set; } = new();
            public List<OperationalContactDto> OperationalContacts { get; set; } = new();
            public List<MajorGoodsServiceDto> MajorGoodsServices { get; set; } = new();
            public List<MajorCustomerDto> MajorCustomers { get; set; } = new();
            public List<OtherInformationDto> OtherInformation { get; set; } = new();
        }
        public class BankDetailsModel
        {
            public string AccountNameHolder { get; set; }

            public string AccountNumber { get; set; }

            public string BankCode { get; set; }

            public string BankName { get; set; }

            public string IfscCode { get; set; }

            public string BranchCode { get; set; }

            public string BankAddress { get; set; }
        }
        public class SaveRequest
        {
            public string GstNumber { get; set; }
            public PaymentDetailsModel? PaymentDetails { get; set; }
            public List<string> UploadedFiles { get; set; }
            public List<MajorGoodsServiceModel> MajorGoodsServices { get; set; }
        }

        public class SaveDraftRequest
        {
            public int Page { get; set; }

            public FormDataModel FormData { get; set; }

            public UploadedFilesModel UploadedFiles { get; set; }
        }
        public class SubmitVendorRequest
        {
            public bool IsExistingVendor { get; set; }

            public bool OtpValid { get; set; }

            public FormDataModel FormData { get; set; } = new();

            public UploadedFilesModel UploadedFiles { get; set; } = new();
        }
        public class SubmitVendorResult
        {
            public bool Success { get; set; }

            public int Id { get; set; }

            public string GstNumber { get; set; } = "";

            public string Message { get; set; } = "";
        }

        // =========================================================
        // FORM DATA MODEL
        // Matches React FormDataType
        // =========================================================
        public class TechOTP
        {
            public string MESSAGE { get; set; }
        }
        public class SendOtpRequest
        {
            public string GstNumber { get; set; }
            public string MobileNumber { get; set; }
        }

        public class VerifyOtpRequest
        {
            public string GstNumber { get; set; }
            public string MobileNumber { get; set; }
            public string Otp { get; set; }
        }
        public class FormDataModel
        {
            public GstDetailsModel GstDetails { get; set; }

            public string GstNumber { get; set; }

            public string PanNumber { get; set; }

            public string PartnerType { get; set; }
            public string ContactPerson { get; set; }


            public AddressModel RegisteredOffice { get; set; }

            public AddressModel BillingAddress { get; set; }

            public AddressModel GoodsReturnAddress { get; set; }

            public AddressModel ShippingAddress { get; set; }


            public string TradeName { get; set; }

            public string NatureOfBusiness { get; set; }

            public string DateOfEstablishment { get; set; }

            public string ContactPersonName { get; set; }

            public string Designation { get; set; }

            public string Email { get; set; }

            public string MobileNumber { get; set; }

            public string OfficeTelephoneNo { get; set; }
            public string DeclarationName { get; set; }
            public string DeclarationDesignation { get; set; }

            public string TanNumber { get; set; }


            public PanDetailsModel PanDetails { get; set; }

            public BankDetailsModel BankDetails { get; set; }

            public MsmeDetailsModel MsmeDetails { get; set; }

            public PaymentDetailsModel PaymentDetails { get; set; }


            public List<BusinessLocationModel> OtherBusinessLocations { get; set; }

            public List<BusinessPartnerModel> BusinessPartners { get; set; }
            public List<PartnerDetails> PartnerDetails { get; set; }

            public List<OperationalContactModel> OperationalContacts { get; set; }

            public List<MajorGoodsServiceModel> MajorGoodsServices { get; set; }
            public List<MajorCustomerDto> MajorCustomers { get; set; }
            public List<OtherInformation> OtherInformation { get; set; }
        }


        // =========================================================
        // ADDRESS
        // =========================================================

        public class AddressModel
        {
            public string Address1 { get; set; }

            public string Address2 { get; set; }

            public string Address3 { get; set; }

            public string Country { get; set; }

            public string State { get; set; }

            public string City { get; set; }

            public string Pincode { get; set; }
        }


        // =========================================================
        // PAN
        // =========================================================

        public class PanDetailsModel
        {
            public string DateOfIncorporation { get; set; }

            public string PanNumber { get; set; }
        }


        // =========================================================
        // GST
        // =========================================================

        public class GstDetailsModel
        {
            public string Building { get; set; }

            public string City { get; set; }

            public string District { get; set; }

            public string GstNumber { get; set; }

            public string LegalName { get; set; }

            public string Locality { get; set; }

            public string Pincode { get; set; }

            public string State { get; set; }

            public string Street { get; set; }

            public string TradeName { get; set; }
        }


       


        // =========================================================
        // MSME
        // =========================================================

        public class MsmeDetailsModel
        {
            public string MsmeNo { get; set; }

            public string MsmeRegistrationStatus { get; set; }

            public string EnterpriseType { get; set; }
        }


        // =========================================================
        // PAYMENT
        // =========================================================

        public class PaymentDetailsModel
        {
            public string TypeOfMargin { get; set; }

            public string TypeOfVendor { get; set; }

            public string? BusinessType { get; set; }

            public string AgencyName { get; set; }

            public string AgencyEmail { get; set; }

            public string CreditDays { get; set; }

            public string BillLevelDiscount { get; set; }

            public string? DisCount { get; set; }

            public string MarkDownWithTax0 { get; set; }

            public string MarkDownWithTax3 { get; set; }

            public string MarkDownWithTax5 { get; set; }

            public string MarkDownWithTax18 { get; set; }

            public string MarkDownWithoutTax0 { get; set; }

            public string MarkDownWithoutTax3 { get; set; }

            public string MarkDownWithoutTax5 { get; set; }

            public string MarkDownWithoutTax18 { get; set; }
        }


        // =========================================================
        // OTHER BUSINESS LOCATION
        // =========================================================

        public class BusinessLocationModel
        {
            public string State { get; set; }

            public string GstNumber { get; set; }

            public string Address { get; set; }

            public string GstClassification { get; set; }
        }


        // =========================================================
        // BUSINESS PARTNER
        // =========================================================

        public class BusinessPartnerModel
        {
            public string Name { get; set; }

            public string Designation { get; set; }

            public string ContactNo { get; set; }

            public string Email { get; set; }
        }


        // =========================================================
        // OPERATIONAL CONTACT
        //
        // Based on your current React interface:
        //
        // interface OperationalContact {
        //     name: string;
        //     contactNo: string;
        //     email: string;
        // }
        // =========================================================

        public class OperationalContactModel
        {
            public string Name { get; set; }

            public string ContactNo { get; set; }

            public string Email { get; set; }
        }


        // =========================================================
        // MAJOR GOODS / SERVICES
        //
        // Based on your current React interface:
        //
        // materialDescription
        // hsnCode
        // brand
        // size
        // imageUpload
        // imageFile
        //
        // imageFile is NOT sent to SaveDraft.
        // imageUpload contains the uploaded filename/path.
        // =========================================================

        public class MajorGoodsServiceModel
        {
            public string MaterialDescription { get; set; }

            public string HsnCode { get; set; }

            public string Brand { get; set; }

            public string Size { get; set; }

            public string? ImageUpload { get; set; }
        }


        // =========================================================
        // UPLOADED FILES
        //
        // React uploadedFiles state:
        //
        // panCard
        // gstCertificate
        // bankProof
        // msmeCertificate
        // performaInvoice
        // =========================================================

        public class UploadedFilesModel
        {
            public string PanCard { get; set; }

            public string GstCertificate { get; set; }

            public string BankProof { get; set; }

            public string MsmeCertificate { get; set; }

            public List<string> PerformaInvoice { get; set; } = new();
        }

        //public class VendorDetails
        //{
        //    public string TName { get; set; }
        //    public string PartnerType { get; set; }

        //    public string Raddress1 { get; set; }
        //    public string Raddress2 { get; set; }
        //    public string Raddress3 { get; set; }
        //    public string Rcountry { get; set; }
        //    public string Rstate { get; set; }
        //    public string Rzipcode { get; set; }
        //    public string RegisteredOfficeCity { get; set; }

        //    public string Gaddress1 { get; set; }
        //    public string Gaddress2 { get; set; }
        //    public string Gaddress3 { get; set; }
        //    public string Gcountry { get; set; }
        //    public string Gstate { get; set; }
        //    public string Gzipcode { get; set; }
        //    public string Gcity { get; set; }

        //    public string Saddress1 { get; set; }
        //    public string Saddress2 { get; set; }
        //    public string Saddress3 { get; set; }
        //    public string Scountry { get; set; }
        //    public string Sstate { get; set; }
        //    public string Szipcode { get; set; }
        //    public string Scity { get; set; }

        //    public string Baddress1 { get; set; }
        //    public string Baddress2 { get; set; }
        //    public string Baddress3 { get; set; }
        //    public string Bcountry { get; set; }
        //    public string Bstate { get; set; }
        //    public string Bzipcode { get; set; }
        //    public string BusinessBillingCity { get; set; }

        //    public string NatureOfBusinessActivity { get; set; }
        //    public string DateOfEstablishment { get; set; }
        //    public string ContactPersonName { get; set; }
        //    public string Designation { get; set; }
        //    public string EmailId { get; set; }
        //    public string MobileNo { get; set; }
        //    public string OfficeTelephoneNo { get; set; }

        //    public string TANNo { get; set; }
        //    public string MsmeRegistrationStatus { get; set; }
        //    public string MSMENo { get; set; }

        //    public string BankName { get; set; }
        //    public string AccountName { get; set; }
        //    public string AccountNumber { get; set; }
        //    public string IfscCode { get; set; }
        //    public string BranchCode { get; set; }
        //    public string BankAddress { get; set; }

        //    public string DeclarationName { get; set; }
        //    public string DeclarationDesignation { get; set; }

        //    public string EnterpriseType { get; set; }
        //    public string BusinessType { get; set; }

        //    public string AgencyEmail { get; set; }
        //    public string AgencyName { get; set; }

        //    public string VerificationNo { get; set; }
        //    public string ContactPerson { get; set; }
        //}
        //public class PaymentDetails
        //{
        //    public string CreditDays { get; set; }
        //    public string DisCount { get; set; }
        //    public string PriceType { get; set; }

        //    public string MarkDownTax0 { get; set; }
        //    public string MarkDownWithoutTax0 { get; set; }

        //    public string MarkDownTax3 { get; set; }
        //    public string MarkDownWithoutTax3 { get; set; }

        //    public string MarkDownTax5 { get; set; }
        //    public string MarkDownWithoutTax5 { get; set; }

        //    public string MarkDownTax18 { get; set; }
        //    public string MarkDownWithoutTax18 { get; set; }

        //    public string BusinessType { get; set; }
        //    public string AgencyEmail { get; set; }
        //    public string AgencyName { get; set; }
        //}
        //public class GstResponse
        //{
        //    public string PANNumber { get; set; }

        //    public VendorDetails VendorDetails { get; set; }
        //    public PaymentDetails PaymentDetails { get; set; }

        //    public List<BusinessDetails> BusinessDetails { get; set; } = new List<BusinessDetails>();
        //    public List<PartnerDetails> PartnerDetails { get; set; } = new List<PartnerDetails>();
        //    public List<OperationalContact> OperationalContacts { get; set; } = new List<OperationalContact>();
        //    public List<MajorGoodsService> MajorGoodsServices { get; set; } = new List<MajorGoodsService>();
        //    public List<MajorCustomer> MajorCustomers { get; set; } = new List<MajorCustomer>();
        //    public List<OtherInformation> OtherInformation { get; set; } = new List<OtherInformation>();
        //}
    }
}

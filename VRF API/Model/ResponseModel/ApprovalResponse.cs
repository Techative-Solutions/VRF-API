using System.Net;

namespace VRF_API.Model.ResponseModel
{


    public class AttachmentsWrapper
    {
        public List<Attachments2_Lines> Attachments2_Lines { get; set; }
    }

    public class Attachments2_Lines
    {
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string SourcePath { get; set; }
    }


    public class Vendor1
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public int GroupCode { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string EmailAddress { get; set; }
        public string FederalTaxID { get; set; }
        public double DiscountPercent { get; set; }
        public int PayTermsGrpCode { get; set; }
        public string FreeText { get; set; }
        public int AttachmentEntry { get; set; }

        public List<ContactEmployee> ContactEmployees { get; set; }
        public List<Address> BPAddresses { get; set; }
        public List<BankAccount> BPBankAccounts { get; set; }

        public string U_MSMENo { get; set; }
        public string U_VRFAppover { get; set; }
    }

    public class ContactEmployee
    {
        public string Name { get; set; }
    }

    public class Address
    {
        public string AddressName { get; set; }
        public string AddressType { get; set; }
        public string Street { get; set; }
        public string Block { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string GSTIN { get; set; }
    }
    public class BankAccount
    {
        public string BankCode { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string BICSwiftCode { get; set; }
    }
    public class GoodItem
    {
        public int SerialNo { get; set; }
        public string Product { get; set; }
        public string Brand { get; set; }
        public string Size { get; set; }
        public string MaterialDescription { get; set; }
        public string HSNCode { get; set; }
        public string TaxPercentage { get; set; }
    }
}

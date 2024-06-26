using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.ComponentModel;
using Newtonsoft.Json;

namespace CliReportCompiler.Sample.WinForms
{

    public class MGeneralLedgerVReport
    {
        [Key]
        [Display(Name = "General Ledger Id")]
        public virtual long GlId { get; set; }
        [Display(Name = "General Ledger Name")]
        [Required]
        public virtual string GlName { get; set; }
        [Display(Name = "General Ledger Description")]
        public virtual string GlDescription { get; set; }
        [Display(Name = "General Ledger Group")]
        [Required]
        public virtual int GlType { get; set; }
        [NotMapped]
        public virtual string T_GlTypeName { get; set; }
        [NotMapped]
        public virtual string GlTypeName { get; set; }
        [Display(Name = "General Ledger Type")]
        [Required]
        public virtual int GlGroup { get; set; }
        [NotMapped]
        public virtual string T_GlGroupName { get; set; }
        [NotMapped]
        public virtual string GlGroupName { get; set; }

        [Display(Name = "General Ledger Sub Type")]
        [CanBeNull]
        public virtual int? GlSubGroup { get; set; }
        [NotMapped]
        public virtual string T_GlSubGroupName { get; set; }
        [NotMapped]
        public virtual string GlSubGroupName { get; set; }

        [Display(Name = "Enable SubLedger")]
        [CanBeNull]
        public virtual bool? EnableSubLedger { get; set; }//Default False

        [Display(Name = "Openning Balance")]
        [CanBeNull]
        public virtual double? OpenningBalance { get; set; }

        [Display(Name = "Dr / Cr")]
        [CanBeNull]
        [MaxLength(2)]
        public virtual string DrCr { get; set; }

        [Display(Name = "Address I")]
        public virtual string Address1 { get; set; }
        [Display(Name = "Address II")]
        public virtual string Address2 { get; set; }
        [Display(Name = "Address III")]
        public virtual string Address3 { get; set; }
        //public virtual string Address { get => getAddress(); set => value = getAddress(); }
        //public virtual string FullAddress { get => getFullAddress(); set => value = getFullAddress(); }
        [Display(Name = "Town/City")]
        public virtual string City { get; set; }
        [Display(Name = "State / Province / Region")]
        public virtual string State { get; set; }
        [Display(Name = "Pin No.")]
        [DataType(DataType.PostalCode)]
        public virtual string Pin { get; set; }

        [Display(Name = "Contact")]
        [DataType(DataType.PhoneNumber)]
        public virtual string PhoneNo { get; set; }
        [Display(Name = "Alt. Contact")]
        [DataType(DataType.PhoneNumber)]
        public virtual string AltPhoneNo { get; set; }
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public virtual string Email { get; set; }
        [Display(Name = "Alt. Email")]
        [DataType(DataType.EmailAddress)]
        public virtual string AltEmail { get; set; }

        [Display(Name = "Name In Bank")]
        public virtual string NameInBank { get; set; }
        [Display(Name = "IFSC CODE")]
        public virtual string IFSCCODE { get; set; }
        [Display(Name = "Bank Name")]
        public virtual string BankName { get; set; }
        [Display(Name = "Bank Address")]
        public virtual string BranchAddress { get; set; }
        [Display(Name = "Bank Branch")]
        public virtual string Branch { get; set; }
        [Display(Name = "Bank MICR Code")]
        public virtual string MicrCode { get; set; }
        [Display(Name = "Bank Swift Code")]
        public virtual string SwiftCode { get; set; }
        [Display(Name = "Account Number")]
        public virtual string AccountNo { get; set; }
        [Display(Name = "Can Remove")]
        public virtual bool? CanRemove { get; set; }










        #region NotMapped Fields

        [NotMapped]
        public virtual string FinalAccountLayout { get; set; }
        [NotMapped]
        public virtual int? GRArangeOrder { get; set; }

        [NotMapped]
        public virtual double? AddToModOpenningBalance { get; set; }
        public virtual double? ModOpenningBalance => (string.IsNullOrEmpty(DrCr) ? 0 : DrCr == "DR" ? OpenningBalance : DrCr == "CR" ? -OpenningBalance : 0) + AddToModOpenningBalance.GetValueOrDefault(0);
        [NotMapped]
        public virtual double? LiveBalance { get; set; }
        [NotMapped]
        public virtual double? CurrentBalance { get; set; }


        #region ReportOpeningBalance
        [NotMapped]
        public virtual double? AddToReportOpeningBalanceMod { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalanceMod { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalance => Math.Abs(ReportOpeningBalanceMod.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportOpeningBalanceDRCR => ReportOpeningBalanceMod.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportOpeningBalanceDR
        [NotMapped]
        public virtual double? AddToReportOpeningBalanceModDR { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalanceModDR { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalanceDR => Math.Abs(ReportOpeningBalanceModDR.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportOpeningBalanceDR_DRCR => ReportOpeningBalanceModDR.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportOpeningBalanceCR
        [NotMapped]
        public virtual double? AddToReportOpeningBalanceModCR { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalanceModCR { get; set; }
        [NotMapped]
        public virtual double? ReportOpeningBalanceCR => Math.Abs(ReportOpeningBalanceModCR.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportOpeningBalanceCR_DRCR => ReportOpeningBalanceModCR.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportDateRangeBalance
        [NotMapped]
        public virtual double? AddToReportDateRangeBalanceMod { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalanceMod { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalance => Math.Abs(ReportDateRangeBalanceMod.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportDateRangeBalanceDRCR => ReportDateRangeBalanceMod.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportDateRangeBalanceDR
        [NotMapped]
        public virtual double? AddToReportDateRangeBalanceModDR { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalanceModDR { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalanceDR => Math.Abs(ReportDateRangeBalanceModDR.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportDateRangeBalanceDR_DRCR => ReportDateRangeBalanceModDR.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportDateRangeBalanceCR
        [NotMapped]
        public virtual double? AddToReportDateRangeBalanceModCR { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalanceModCR { get; set; }
        [NotMapped]
        public virtual double? ReportDateRangeBalanceCR => Math.Abs(ReportDateRangeBalanceModCR.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportDateRangeBalanceCR_DRCR => ReportDateRangeBalanceModCR.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        #region ReportClosingBalance
        [NotMapped]
        public virtual double? AddToReportClosingBalanceMod { get; set; }
        [NotMapped]
        public virtual double? ReportClosingBalanceMod => (ReportOpeningBalanceMod + ReportDateRangeBalanceMod + AddToReportClosingBalanceMod).GetValueOrDefault(0);//GetCBAsOnDateBalance(ReportTo.Value);
        [NotMapped]
        public virtual double? ReportClosingBalance => Math.Abs(ReportClosingBalanceMod.GetValueOrDefault(0));
        [NotMapped]
        public virtual string ReportClosingBalanceDRCR => ReportClosingBalanceMod.GetValueOrDefault(0) >= 0 ? "DR" : "CR";
        #endregion


        [NotMapped]
        public virtual DateTime? ReportFrom { get; set; }
        [NotMapped]
        public virtual DateTime? ReportTo { get; set; }

        #endregion
    }
    public class CompanyInfoVReport
    {
        [Key]
        public virtual string CompanyId { get; set; }

        public virtual string CompanyName { get; set; }
        [NotMapped, JsonIgnore]
        public virtual string CompanyNameWithSession => CompanyName + $"({SessionFrom.GetValueOrDefault():yyyy-MM} To {SessionTo.GetValueOrDefault():yyyy-MM})";

        public virtual string CompanyCode { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string Address1 { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string Address2 { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string Address3 { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string City { get; set; }
        [CanBeNull]
        [DataType(DataType.PostalCode)]
        public virtual string Pincode { get; set; }
        [CanBeNull]
        public virtual string State { get; set; }
        [CanBeNull]
        public virtual string Country { get; set; }
        [CanBeNull]
        [DataType(DataType.PhoneNumber)]
        public virtual string Phone { get; set; }
        [CanBeNull]
        [DataType(DataType.EmailAddress)]
        public virtual string Email { get; set; }
        [CanBeNull]
        public virtual string GstNo { get; set; }



        #region Bank Details
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string AccountNumber { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string IfscCode { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string BankName { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string BankBranch { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string BankAddress { get; set; }
        [CanBeNull]
        public virtual string MicrCode { get; set; }
        [CanBeNull]
        [DataType(DataType.Text)]
        public virtual string SwiftCode { get; set; }
        #endregion




        public string DbName { get; set; }
        [Timestamp]
        public virtual DateTime CreatedOn { get; set; }
        public virtual DateTime? SessionFrom { get; set; }
        public virtual DateTime? SessionTo { get; set; }
        [NotMapped]
        public virtual DateTime? From { get; set; }
        [NotMapped]
        public virtual DateTime? To { get; set; }
        [NotMapped]
        public string DataBaseStatus { get; set; }
        [CanBeNull]
        [DefaultValue(0)]
        public virtual long ParentPosition { get; set; }
        //[CanBeNull]
        //public virtual long OwnerId { get; set; }
        [CanBeNull]
        public virtual string ParentCompanyId { get; set; }
    }

}

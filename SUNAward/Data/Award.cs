using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SUNAward.Data
{
    [Table("Sun_Awards")]
    public class Award
    {
        [Column("SUN_Award_ID"), Key]
        [DisplayName("Asurite Id")]
        public int SUNAwardId { get; set; }

        [Column("Award_Date")]
        [DisplayName("Award Date")]
        public DateTime AwardDate { get; set; }

        [Column("Time_Stamp"), StringLength(100)]
        [DisplayName("Time Stamp")]
        public string TimeStamp { get; set; }

        [Column("Award_For"), StringLength(600)]
        public string AwardFor { get; set; }

        [Column("Unique_ID"), StringLength(50)]
        public string UniqueId { get; set; }

        [Column("e_Award")]
        public bool EAward { get; set; }

        [Column("P_Affil_ID"), StringLength(80)]
        public string P_AffilID { get; set; }

        [Column("P_ASURITE"), StringLength(16)]
        public string P_ASURITE { get; set; }

        [Column("P_Dept"), StringLength(42)]
        public string P_Dept { get; set; }

        [Column("P_Email"), StringLength(80)]
        public string P_Email { get; set; }

        [Column("P_Preferred_Name"), StringLength(255)]
        public string P_PreferredName { get; set; }

        [Column("P_Home_Dept_Code")]
        public string P_HomeDeptCode { get; set; }

        [Column("P_Title")]
        public string P_Title { get; set; }

        [Column("R_Affil_ID"), StringLength(80)]
        public string R_AffilID { get; set; }

        [Column("R_ASURITE"), StringLength(16)]
        public string R_ASURITE { get; set; }

        [Column("R_Dept"), StringLength(42)]
        public string R_Dept { get; set; }

        [Column("R_Email"), StringLength(80)]
        public string R_Email { get; set; }

        [Column("R_Preferred_Name"), StringLength(255)]
        public string R_PreferredName { get; set; }

        [Column("R_Home_Dept_Code")]
        public string R_HomeDeptCode { get; set; }

        [Column("R_Title")]
        public string R_Title { get; set; }

        [Column("Entered_By")]
        public string EnteredBy { get; set; }

        [Column("Award_Delete")]
        public bool AwardDelete { get; set; }

        [Column("Award_Delete_Comments")]
        public string AwardDeleteComments { get; set; }

        [Column("award_cat")]
        public string AwardCat { get; set; }

        [Column("isCatNew")]
        public bool IsCatNew { get; set; }

    }
}
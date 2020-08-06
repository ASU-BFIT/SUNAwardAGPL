using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

using SUNAward.Data;

namespace SUNAward.Models
{
    public class AwardFormModel
    {
        [DisplayName("Presented by")]
        public string PresenterName { get; set; }

        public List<Category> Categories { get; set; }

        public int[] SelectedCategories { get; set; }

        [DisplayName("Presented to"), Required]
        public string Recipient { get; set; }

        [Required]
        public string Supervisor { get; set; }

        [DisplayName("Category"), Required]
        public string CustomCategory { get; set; }

        public string Department { get; set; }

        public string For { get; set; }

        public DateTime Date { get; set; }
    }
}
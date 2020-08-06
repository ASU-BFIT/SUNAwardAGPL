using Microsoft.Ajax.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;

namespace SUNAward.Data
{
    [Table("PS_lookup_data_for_sunaward")]
    public class Person
    {
        [Column("ASU_ASURITE_ID"), Key]
        [DisplayName("ASURITE")]
        public string AsuriteId { get; set; }

        [Column("PERSON_ID"), StringLength(10)]
        [DisplayName("Affiliate ID")]
        public string AffiliateId { get; set; }

        [Column("NAME_DISPLAY"), StringLength(10)]
        [DisplayName("Name")]
        public string DisplayName { get; set; }

        [Column("TITLE"), StringLength(50)]
        [DisplayName("Title")]
        public string Title { get; set; }

        [Column("DEPT_LD"), StringLength(50)]
        [DisplayName("Department")]
        public string Department { get; set; }

        [Column("DEPTID"), StringLength(50)]
        [DisplayName("Department Code")]
        public string DepartmentId { get; set; }

        [Column("ASU_DIRECTORY_ADDR"), StringLength(150)]
        [DisplayName("Email")]
        public string Email { get; set; }

        public static List<Person> GetPersonByName(string name, int limit)
        {
            using (var context = new SUNAwardContext())
            {
                return context.Persons.Where(entity => entity.DisplayName.Contains(name)).Take(limit).ToList();
            }
        }

        public static Person GetEmpByAsuriteID(string asuriteID)
        {
            if (asuriteID == null)
            {
                return null;
            }

            using (var context = new SUNAwardContext())
            {
                return context.Persons.Where(entity => entity.AsuriteId == asuriteID).SingleOrDefault();
            }
        }
    }
}

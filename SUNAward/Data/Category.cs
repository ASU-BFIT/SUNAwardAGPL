using Microsoft.Owin.Security.Provider;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SUNAward.Data
{
    public class Category
    {
        [Column("cat_id"), Required, Key]
        [DisplayName("Category Id")]
        public int CategoryId { get; set; }

        [Column("cat_name"), StringLength(30)]
        [DisplayName("Category Name")]
        public string CategoryName { get; set; }

        [Column("cat_desc"), StringLength(255)]
        [DisplayName("Category Desc")]
        public string CategoryDesc { get; set; }

        [Column("active_flag")]
        [DisplayName("Is Active")]
        public bool ActiveFlag { get; set; }

        [NotMapped]
        public bool IsYouNameIt => CategoryName.ToLower() == "you name it!";

        public static List<Category> GetCategories(Expression<Func<Category, bool>> filter = null)
        {
            using (var context = new SUNAwardContext())
            {
                IQueryable<Category> query = context.Categories;
                
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                return query.OrderBy(o => o.CategoryId).ToList();
            }
        }

    }

   
}

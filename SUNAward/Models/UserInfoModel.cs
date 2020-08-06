using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using SUNAward.Data;

namespace SUNAward.Models
{
    public class UserInfoModel
    {
        public string PresenterName { get; set; }
        public List<Category> Categories { get; set; }
    }
}
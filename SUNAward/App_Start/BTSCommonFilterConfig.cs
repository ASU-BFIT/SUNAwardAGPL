using System.Web.Mvc;
using BTS.Common.Mvc;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(SUNAward.App_Start.BTSCommonFilterConfig), "RegisterFilters")]

namespace SUNAward.App_Start
{
    public class BTSCommonFilterConfig
    {
        public static void RegisterFilters()
        {
            // Sets up CSRF protection globally and enables additional AJAX features
            GlobalFilters.Filters.RegisterCommonFilters();
        }
    }
}
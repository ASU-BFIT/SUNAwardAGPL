using SUNAward.Data;
using SUNAward.Models;
using BTS.Common;
using BTS.Common.CAS;

using System.Collections.Generic;
using System.Web.Mvc;
using System.Security.Claims;

using iText;
using System.Linq;
using iText.Kernel.Pdf;
using System;
using System.Net;
using System.IO;
using iText.Forms;

namespace SUNAward.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(new UserInfoModel()
            {
                PresenterName = ((ClaimsPrincipal)User).FindFirst(ClaimTypes.Name).Value,
                Categories = Category.GetCategories(o => o.ActiveFlag == true)
            });
        }

        // called when someone visits /preview via HTTP (such as by refreshing the page in the Angular app)
        public ActionResult RedirectToIndex()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}

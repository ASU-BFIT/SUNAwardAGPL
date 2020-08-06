using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace BTS.Common.Mvc.Controllers
{
    /// <summary>
    /// Shared controller to load embedded dll resources
    /// </summary>
    public class ResourceController : Controller
    {
        /// <summary>
        /// Retrieves an embedded resource file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ActionResult Resource(string path)
        {
            var provider = new EmbeddedPathProvider();
            path = "~/BTS.Common.Mvc/" + path;

            if (!provider.FileExists(path))
            {
                return new HttpNotFoundResult();
            }

            var parts = path.Split('.');
            string ext = parts.Last();
            string contentType;

            switch (ext)
            {
                case "css":
                    contentType = "text/css";
                    break;
                case "js":
                    contentType = "text/javascript";
                    break;
                default:
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var file = provider.GetFile(path);
            var stream = file.Open();

            return new FileStreamResult(stream, contentType);
        }
    }
}
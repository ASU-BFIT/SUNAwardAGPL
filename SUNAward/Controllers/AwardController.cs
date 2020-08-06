using BTS.Common;

using iText.Forms;
using iText.Forms.Xfdf;
using iText.Kernel.Pdf;

using SUNAward.Data;
using SUNAward.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Ajax.Utilities;

namespace SUNAward.Controllers
{
    // This controller has custom routing. When adding a new method, be sure to modify RouteConfig.cs to recognize it!
    [Authorize]
    public class AwardController : Controller
    {
        const int CURRENT_AWARD_TYPE = 1;

        /// <summary>
        /// Displays the award PDF in the browser
        /// </summary>
        /// <param name="id">Award UUID</param>
        /// <param name="awardType">
        /// Award type:
        /// <list type="bullet">
        /// <item><term>0</term><description>old-style award/categories</description></item>
        /// <item><term>1</term><description>new-style award/categories</description></item>
        /// </list>
        /// </param>
        /// <returns></returns>
        [HttpGet, AllowAnonymous]
        public ActionResult Index(string id, int awardType = CURRENT_AWARD_TYPE)
        {
            Award award;
            using (var context = new SUNAwardContext())
            {
                award = context.Awards.Where(o => o.UniqueId == id).SingleOrDefault();
                if (award == null)
                {
                    return HttpNotFound();
                }
            }

            try
            {
                var outStream = GetPdf(awardType, award);
                Response.AddHeader("Content-Disposition", "inline; filename=\"SUNAward.pdf\"");
                return File(outStream, "application/pdf");
            }
            catch (ArgumentException e)
            {
                if (e.ParamName == "awardType")
                {
                    return HttpNotFound();
                }

                // else re-throw
                throw;
            }
        }

        /// <summary>
        /// Generate a PNG rendering of the award with the given parameters
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Preview(SunAwardSubmit model)
        {
            IntPtr instance = IntPtr.Zero;
            bool instanceCreated = false;
            bool haveException = false;

            var allCategories = Category.GetCategories(o => o.ActiveFlag == true).ToDictionary(o => o.CategoryId);
            string categoryList;
            try
            {
                categoryList = model.Categories
                    .Select(o => allCategories[o].IsYouNameIt
                        ? (!String.IsNullOrWhiteSpace(model.CustomCategory) ? model.CustomCategory : allCategories[o].CategoryName)
                        : allCategories[o].CategoryName)
                    .StringJoin(",");
            }
            catch (ArgumentOutOfRangeException)
            {
                return HttpNotFound();
            }

            var presenter = Person.GetEmpByAsuriteID(User.Identity.Name);
            var recipient = Person.GetEmpByAsuriteID(model.Recipient?.AsuriteId);
            if (recipient == null)
            {
                return HttpNotFound();
            }

            // only fill in the fields we actually use on the award, the rest aren't necessary for the preview
            var award = new Award()
            {
                AwardCat = categoryList,
                AwardDate = DateTime.Now,
                AwardFor = model.For,
                P_PreferredName = presenter.DisplayName,
                R_PreferredName = recipient.DisplayName,
                R_Dept = recipient.Department,
                IsCatNew = true
            };

            var stdin = GetPdf(CURRENT_AWARD_TYPE, award);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            // A function that, when called, produces a callback function suitable for passing into gsapi_set_stdio.
            // This is known as partial function application or currying in other languages, but C#'s support of it is less than stellar
            GhostScript.Output outputCallback(StringBuilder output) => (IntPtr callerHandle, IntPtr str, int len) =>
            {
                unsafe
                {
                    byte* strBytes = (byte*)str.ToPointer();
                    output.Append(System.Text.Encoding.UTF8.GetString(strBytes, len));
                }

                return len;
            };

            int stdinCallback(IntPtr callerHandle, IntPtr buf, int len)
            {
                byte[] stdinBuf = new byte[len];
                var read = stdin.Read(stdinBuf, 0, len);
                Marshal.Copy(stdinBuf, 0, buf, read);

                return read;
            }

            var stdoutCallback = outputCallback(stdout);
            var stderrCallback = outputCallback(stderr);
            var tempFile = Path.GetTempFileName();
            MemoryStream outputFile = new MemoryStream();

            string[] argv =
            {
                String.Empty, // Ignored
                "-sDEVICE=png16m",
                "-r144",
                "-dNOPAUSE",
                "-dBATCH",
                "-dSAFER",
                "-dTextAlphaBits=4",
                "-dGraphicsAlphaBits=4",
                $"-sOutputFile={tempFile}", // write to temporary file
                "-" // read from stdin (using stdinCallback)
            };

            // the ghostscript dll doesn't support multiple threads accessing it at once, so we need to singlethread this
            // (each web request to ASP.NET gets run on a separate thread within the same process)
            // do as much processing as possible outside of this lock
            lock (GhostScript.GhostScriptLock)
            {
                try
                {
                    var code = GhostScript.gsapi_new_instance(out instance, IntPtr.Zero);
                    if (code != 0)
                    {
                        throw new InvalidOperationException($"GhostScript returned code {code} on gsapi_new_instance.");
                    }

                    instanceCreated = true;

                    code = GhostScript.gsapi_set_arg_encoding(instance, GhostScript.GS_ARG_ENCODING_UTF8);
                    if (code != 0)
                    {
                        throw new InvalidOperationException($"GhostScript returned code {code} on gsapi_set_arg_encoding.");
                    }

                    code = GhostScript.gsapi_set_stdio(instance, stdinCallback, stdoutCallback, stderrCallback);
                    if (code != 0)
                    {
                        throw new InvalidOperationException($"GhostScript returned code {code} on gsapi_set_stdio.");
                    }

                    // -101 is gs_error_Quit which indicates no errors
                    code = GhostScript.gsapi_init_with_args(instance, argv.Length, argv);
                    if (code != 0 && code != -101)
                    {
                        throw new InvalidOperationException($"GhostScript returned code {code} on gsapi_init_with_args.");
                    }
                }
                catch
                {
                    // debugging aid, display the logs from ghostscript to visual studio output window
                    if (stdout.Length > 0)
                    {
                        Debug.WriteLine("***** GhostScript stdout *****");
                        Debug.Write(stdout.ToString());
                    }

                    if (stderr.Length > 0)
                    {
                        Debug.WriteLine("***** GhostScript stderr *****");
                        Debug.Write(stderr.ToString());
                    }

                    // tell our finally block to perform any cleanup we need since we're about to exit this method
                    haveException = true;

                    // re-throw exception, don't want to swallow it
                    throw;
                }
                finally
                {
                    if (instanceCreated)
                    {
                        GhostScript.gsapi_exit(instance);
                        GhostScript.gsapi_delete_instance(instance);
                    }

                    // tempFile is locked until GhostScript exits
                    // if we consistently have errors with temp files, may need to write a virtual fs driver and pass to gsapi_add_fs
                    // so that it gets backed by MemoryStreams or the like instead
                    if (haveException)
                    {
                        System.IO.File.Delete(tempFile);
                    }
                }
            }

            // crop out bottom half of image, the sun award image only occupies the top half of the page so that it is more compact
            // for printing and displaying in cubes or other boards
            using (var image = Image.Load(tempFile))
            {
                image.Mutate(o => o.Crop(new Rectangle(0, 0, image.Width, image.Height / 2)));
                image.SaveAsPng(outputFile);
            }

            // this will close/dispose outputFile
            outputFile.Seek(0, SeekOrigin.Begin);
            Response.AddHeader("Content-Disposition", "inline; filename=\"AwardPreview.png\"");
            return File(outputFile, "image/png");
        }

        private MemoryStream GetPdf(int awardType, Award award)
        {
            var pdfPath = Server.MapPath($"~/Content/SUNAwardPDF-{awardType}.pdf");
            if (!System.IO.File.Exists(pdfPath))
            {
                throw new ArgumentException(nameof(awardType));
            }

            // File() disposes this MemoryStream so we don't have to explicitly do it here in our code
            var outStream = new MemoryStream();
            var allCats = Category.GetCategories(o => o.ActiveFlag == award.IsCatNew)
                .Where(o => !o.IsYouNameIt)
                .Select(o => o.CategoryName)
                .ToList();
            // SQL string comparisons are case insensitive so we can search it by lowercased version and retrieve the correctly captialized form
            var youNameIt = Category.GetCategories(o => o.ActiveFlag == award.IsCatNew && o.CategoryName == "you name it!")[0].CategoryName;
            var ourCats = award.AwardCat.Split(',').ToHashSet();
            using (var pdf = new PdfDocument(new PdfReader(pdfPath), new PdfWriter(outStream)))
            {
                var form = PdfAcroForm.GetAcroForm(pdf, false);
                if (form == null)
                {
                    throw new InvalidOperationException($"Expected a PDF with an embedded form but didn't find one at {pdfPath}");
                }

                var values = new Dictionary<string, string>()
                {
                    { "Check Box1", form.GetField("Check Box1").GetCheckBoxState(ourCats.Contains(allCats[0])) },
                    { "Check Box2", form.GetField("Check Box2").GetCheckBoxState(ourCats.Contains(allCats[1])) },
                    { "Check Box3", form.GetField("Check Box3").GetCheckBoxState(ourCats.Contains(allCats[2])) },
                    { "Check Box4", form.GetField("Check Box4").GetCheckBoxState(ourCats.Contains(allCats[3])) },
                    { "Check Box5", form.GetField("Check Box5").GetCheckBoxState(ourCats.Contains(allCats[4])) },
                    { "Check Box6", form.GetField("Check Box6").GetCheckBoxState(ourCats.Contains(allCats[5])) },
                    { "Check Box7", form.GetField("Check Box7").GetCheckBoxState(ourCats.Contains(allCats[6])) },
                    { "Check Box8", form.GetField("Check Box8").GetCheckBoxState(ourCats.Contains(allCats[7])) },
                    // Box 9 is the You Name It! field so we detect its presence by seeing if we have a category that isn't one of the normal categories
                    { "Check Box9", form.GetField("Check Box9").GetCheckBoxState(ourCats.Any(c => !allCats.Contains(c))) },
                    { "chkBox1Text", allCats[0] },
                    { "chkBox2Text", allCats[1] },
                    { "chkBox3Text", allCats[2] },
                    { "chkBox4Text", allCats[3] },
                    { "chkBox5Text", allCats[4] },
                    { "chkBox6Text", allCats[5] },
                    { "chkBox7Text", allCats[6] },
                    { "chkBox8Text", allCats[7] },
                    { "chkBox9Text", ourCats.Where(c => !allCats.Contains(c)).StringJoinOrNull(",") ?? youNameIt },
                    { "Text1lbl", "Presented to" },
                    { "Text2lbl", "Department" },
                    { "Text3lbl", "For" },
                    { "Text4lbl", "Presented by" },
                    { "Text5lbl", "Date" },
                    { "Presented_to", award.R_PreferredName },
                    { "Recip_Dept", award.R_Dept ?? String.Empty },
                    { "Presented_by", award.P_PreferredName },
                    { "WhatFor", award.AwardFor },
                    { "Presented_on", award.AwardDate.ToAsuDateString() }
                };

                foreach (var (name, value) in values)
                {
                    form.GetField(name).SetValue(value);
                }

                form.FlattenFields();
                pdf.GetWriter().SetCloseStream(false); // leave MemoryStream open so we can return it later
            }

            outStream.Seek(0, SeekOrigin.Begin);
            return outStream;
        }
    }
}
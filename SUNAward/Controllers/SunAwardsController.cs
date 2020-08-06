using SUNAward.Data;
using SUNAward.Models;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web.Http;

namespace SUNAward.Controllers
{
    [Authorize]
    public class SunAwardsController : ApiController
    {
        // POST api/values
        public SunAwardSubmitResponse Post(SunAwardSubmit value)
        {
            var response = new SunAwardSubmitResponse();
            Award record;
            Person supervisor = null;

            try
            {
                record = ConvertRequestToRecord(value);
            }
            catch (ArgumentException)
            {
                response.Success = false;
                return response;
            }

            if (!String.IsNullOrEmpty(value.Supervisor?.AsuriteId))
            {
                supervisor = Person.GetEmpByAsuriteID(value.Supervisor.AsuriteId);
                if (supervisor == null)
                {
                    response.Success = false;
                    response.Message = "Unknown supervisor, please revise your selection.";
                    return response;
                }
            }

            // if this is being created by a security scan, abort so we don't pollute the database
            if (User.Identity.Name == ConfigurationManager.AppSettings["SecurityScannerUser"])
            {
                response.Success = true;
                response.Message = "Award would be successfully submitted, but was aborted due to scan.";
                return response;
            }

            // create the award
            using (var context = new SUNAwardContext())
            {
                context.Awards.Add(record);
                context.SaveChanges();
            }

            response.Success = true;
            response.Message = "Award successfully submited!";

            // send out emails
            SendPresenterEmail(record);
            SendRecipientEmail(record, supervisor);
            SendSupervisorEmail(record, supervisor);

            return response;
        }

        private List<string> GetAwardCategories(IEnumerable<int> categoryIds, string customCategory)
        {
            var cats = Category.GetCategories();
            var selectedCategories = new List<string>();
            foreach (var cat in cats)
            {
                if (categoryIds.Contains(cat.CategoryId))
                {
                    if (cat.IsYouNameIt && !String.IsNullOrWhiteSpace(customCategory))
                    {
                        selectedCategories.Add(customCategory);
                    }
                    else
                    {
                        selectedCategories.Add(cat.CategoryName);
                    }
                }
            }

            return selectedCategories;
        }

        private Award ConvertRequestToRecord(SunAwardSubmit value)
        {
            var presenter = Person.GetEmpByAsuriteID(User.Identity.Name);
            var recipient = Person.GetEmpByAsuriteID(value.Recipient?.AsuriteId);
            if (presenter == null || recipient == null)
            {
                throw new ArgumentException("Invalid presenter or recipient", nameof(value));
            }

            var id = Guid.NewGuid();


            var sunAwardRecord = new Award
            {
                AwardDate = DateTime.Now,
                TimeStamp = DateTime.Now.ToString(),
                AwardFor = value.For,
                UniqueId = id.ToString(),
                EAward = true,
                P_AffilID = presenter.AffiliateId,
                P_ASURITE = presenter.AsuriteId,
                P_Dept = presenter.Department,
                P_Email = presenter.Email,
                P_PreferredName = presenter.DisplayName,
                P_HomeDeptCode = presenter.DepartmentId,
                P_Title = presenter.Title,
                R_AffilID = recipient.AffiliateId,
                R_ASURITE = recipient.AsuriteId,
                R_Dept = recipient.Department,
                R_Email = recipient.Email,
                R_PreferredName = recipient.DisplayName,
                R_HomeDeptCode = recipient.DepartmentId,
                R_Title = recipient.Title,
                IsCatNew = true
            };

            var selectedCategories = GetAwardCategories(value.Categories, value.CustomCategory);
            sunAwardRecord.AwardCat = String.Join(",", selectedCategories);

            return sunAwardRecord;
        }

        private void SendPresenterEmail(Award record)
        {
            if (String.IsNullOrEmpty(record.P_Email))
            {
                return;
            }

            var msg = new MailMessage();
            msg.To.Add(record.P_Email);
            msg.From = new MailAddress(record.P_Email);
            msg.Subject = "SUN Award Notification";

            string title = "SUN Award Notification";
            string urlbase = new Uri(Request.RequestUri, Url.Content("~/")).AbsoluteUri.TrimEnd('/');
            string intro = $"Dear {record.P_PreferredName}";
            string body = $"Thank you for sending a SUN Award to {record.R_PreferredName}!<br><a href=\"{urlbase}/Award/{record.UniqueId}\"><span style=\"color: #8C1D40; text-decoration: none\">Click here to view the award you sent</span></a> or copy/paste the following URL in your browser: {urlbase}/Award/{record.UniqueId}.";

            msg.Body = String.Format(EmailTemplate, title, urlbase, intro, body, record.P_Email, DateTime.Now.Year);
            msg.IsBodyHtml = true;
            msg.Priority = MailPriority.Normal;

            if (ConfigurationManager.AppSettings["Environment"].ToString() == "QA" || ConfigurationManager.AppSettings["Environment"].ToString() == "Prod")
            {
                var client = new SmtpClient("smtp.asu.edu", 25);
                client.Send(msg);
            }
        }

        private void SendRecipientEmail(Award record, Person supervisor)
        {
            if (String.IsNullOrEmpty(record.R_Email) || String.IsNullOrEmpty(record.P_Email))
            {
                return;
            }

            var msg = new MailMessage();
            msg.To.Add(record.R_Email);
            msg.From = new MailAddress(record.P_Email);
            msg.Subject = "SUN Award Notification";

            string title = "SUN Award Notification";
            string urlbase = new Uri(Request.RequestUri, Url.Content("~/")).AbsoluteUri.TrimEnd('/');
            string intro = $"Dear {record.R_PreferredName}";
            string supv_notice = supervisor != null ? " A copy of this award was forwarded to your supervisor, " + supervisor.DisplayName + "." : String.Empty;
            string body = $"Congratulations, you have received a SUN Award!{supv_notice}<br><a href=\"{urlbase}/Award/{record.UniqueId}\"><span style=\"color: #8C1D40; text-decoration: none\">Click here to retrieve your certificate</span></a> or copy/paste the following URL in your browser: {urlbase}/Award/{record.UniqueId}.";

            msg.Body = String.Format(EmailTemplate, title, urlbase, intro, body, record.R_Email, DateTime.Now.Year);
            msg.IsBodyHtml = true;
            msg.Priority = MailPriority.Normal;

            if (ConfigurationManager.AppSettings["Environment"].ToString() != "Local")
            {
                var client = new SmtpClient("smtp.asu.edu", 25);
                client.Send(msg);
            }
        }

        private void SendSupervisorEmail(Award record, Person supervisor)
        {
            if (supervisor == null || String.IsNullOrEmpty(supervisor.Email))
            {
                return;
            }

            var msg = new MailMessage();
            msg.To.Add(supervisor.Email);
            msg.From = new MailAddress(record.P_Email);
            msg.Subject = "SUN Award Notification";

            string title = "SUN Award Notification";
            string urlbase = new Uri(Request.RequestUri, Url.Content("~/")).AbsoluteUri.TrimEnd('/');
            string intro = $"Dear {supervisor.DisplayName}";
            string recipientFirstName = record.R_PreferredName.Split(' ')[0];
            string body = $"You have been identified as {record.R_PreferredName}'s supervisor. {recipientFirstName} has received a SUN Award!<br><a href=\"{urlbase}/Award/{record.UniqueId}\"><span style=\"color: #8C1D40; text-decoration: none\">Click here to view the award</span></a> or copy/paste the following URL in your browser: {urlbase}/Award/{record.UniqueId}.";

            msg.Body = String.Format(EmailTemplate, title, urlbase, intro, body, supervisor.Email, DateTime.Now.Year);
            msg.IsBodyHtml = true;
            msg.Priority = MailPriority.Normal;

            if (ConfigurationManager.AppSettings["Environment"].ToString() == "QA" || ConfigurationManager.AppSettings["Environment"].ToString() == "Prod")
            {
                var client = new SmtpClient("smtp.asu.edu", 25);
                client.Send(msg);
            }
        }

        #region Email Template
        // {0} = title, {1} = urlbase, {2} = intro, {3} = body, {4} = recipient_email, {5} = current year
        private const string EmailTemplate = @"<html
    xmlns:v=""urn:schemas-microsoft-com:vml""
    xmlns:o=""urn:schemas-microsoft-com:office:office""
    xmlns:w=""urn:schemas-microsoft-com:office:word""
    xmlns:m=""http://schemas.microsoft.com/office/2004/12/omml""
    xmlns:mv=""http://macVmlSchemaUri""
    xmlns=""http://www.w3.org/TR/REC-html40"">
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
    <title>{0}</title>
</head>
<body bgcolor=""white"" lang=""EN-US"" link=""blue"" vlink=""purple"">
    <div class=""WordSection1"">
        <div align=""center"">
            <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""100%"" style=""width: 100.0%; border-collapse: collapse"">
                <tr>
                    <td style=""padding:0in 0in 0in 0in"">
                        <div align=""center"">
                            <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""600"" style=""width: 6.25in; border-collapse: collapse"">
                                <tr>
                                    <td style=""padding: 0in 0in 0in 0in"">
                                        <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""100%"" style=""width: 100.0%; background: white; border-collapse: collapse"">
                                            <tr>
                                                <td valign=""top"" style=""padding: 0in 0in 0in 0in"">
                                                    <p class=""MsoNormal"" align=""center"" style=""text-align: center"">
                                                        <span style=""font-family: Arial; color: black"">
                                                            <img border=""0"" width=""600"" height=""60"" src=""{1}/Content/sunaward-email-header.png"" alt=""SUN Award"">
                                                            <o:p></o:p>
                                                        </span>
                                                    </p>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td valign=""top"" style=""padding: 0in 0in 0in 0in"">
                                                    <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" align=""left"" width=""100%"" style=""width: 100.0%; border-collapse: collapse; max-width:200px"">
                                                        <tr>
                                                            <td style=""padding: 0in 0in 0in 0in"">
                                                                <div align=""center"">
                                                                    <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""560"" style=""width: 420.0pt; border-collapse: collapse"">
                                                                        <tr>
                                                                            <td style=""padding:0in 0in 0in 0in"">
                                                                                <p style=""line-height: 140%"">
                                                                                    <span style=""font-size: 18.0pt; line-height: 140%; font-family: Arial; color: black"">{2},</span>
                                                                                    <span style=""font-family: Arial; color: black"">
                                                                                        <br><br>{3}
                                                                                        <br><br><o:p></o:p>
                                                                                    </span>
                                                                                </p>
                                                                            </td>
                                                                        </tr>
                                                                    </table>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td style=""padding: 0in 0in 0in 0in"">
                                                                <div align=""center"">
                                                                    <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""560"" style=""width: 420.0pt; border-collapse: collapse"">
                                                                        <tr>
                                                                            <td style=""padding: 0in 0in 0in 0in"">
                                                                                <p style=""line-height: 140%"">
                                                                                    <span style=""font-size: 18.0pt; line-height: 140%; font-family: Arial; color: black"">What is a SUN Award?</span>
                                                                                    <span style=""font-family: Arial; color: black"">
                                                                                        <br><br>SUN Award is an easy way to give specific, immediate recognition to one of your ASU co-workers. It is a thoughtful, positive way to honor an employee for supporting university goals. For more information, see the <a href=""https://cfo.asu.edu/sun-award"" title=""SUN Award website""><span style=""color:#8C1D40; text-decoration:none"">SUN Award website</span></a>.
                                                                                        <br><br><o:p></o:p>
                                                                                    </span>
                                                                                </p>
                                                                            </td>
                                                                        </tr>
                                                                    </table>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </td>
                </tr>
                <tr>
                    <td valign=""top"" style=""padding: 0in 0in 0in 0in"">
                        <div align=""center"">
                            <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""600"" style=""width: 6.25in; border-collapse: collapse"">
                                <tr>
                                    <td style=""padding: 1.5pt 1.5pt 1.5pt 1.5pt"">
                                        <p class=""MsoNormal"" align=""center"" style=""margin-bottom: 12.0pt; text-align:center"">
                                            <span style=""font-size:7.5pt; font-family: Verdana; color: #777777"">
                                                This email was sent to: <b>{4}</b><br>
                                                <o:p></o:p>
                                            </span>
                                        </p>
                                        <div align=""center"">
                                            <table class=""MsoNormalTable"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""600"" style=""width: 6.25in; border-collapse: collapse"" id=""Table1"">
                                                <tr>
                                                    <td style=""padding: 1.5pt 1.5pt 1.5pt 1.5pt"">
                                                        <p class=""MsoNormal"" align=""center"" style=""text-align: center"">
                                                            <span style=""font-size: 7.5pt; font-family: Verdana; color: #777777"">Arizona State University<br>PO Box 877505, Tempe, AZ, 85287-7505, USA <o:p></o:p></span>
                                                        </p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                        <p align=""center"" style=""text-align: center"">
                                            <span style=""font-size: 7.5pt; font-family: Verdana; color: #777777"">
                                                Copyright © {5} Arizona Board of Regents | <a href=""https://www.asu.edu/privacy/""><span style=""color:#2D2A2A"">Privacy statement</span></a>
                                            </span>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </td>
                </tr>
            </table>
        </div>
    </div>
</body>
</html>";
        #endregion
    }
}

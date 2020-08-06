using BTS.Common.Web;

using Microsoft.Owin;

using Owin;

[assembly: OwinStartup(typeof(SUNAward.Startup))]

namespace SUNAward
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            app.UseCasAuthWithSessionCookies("SUNAward", PathString.Empty, new PathString("/NotEmployee.html"));
        }
    }
}

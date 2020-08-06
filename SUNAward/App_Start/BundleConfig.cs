using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace SUNAward
{
    public class BundleConfig : IBundleOrderer
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            var jsBundle = new Bundle("~/bundles/Site").Include(
                      "~/Scripts/libs/*.js");

            jsBundle.Orderer = new BundleConfig();
            bundles.Add(jsBundle);

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/Site.css",
                      "~/Scripts/libs/*.css"));

            // use this only to ship the files, don't combine them into a single file
            // because that plays havoc with angular
            BundleTable.EnableOptimizations = false;
        }

        public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
        {
            string[] order = { "runtime", "polyfills", "vendor", "styles", "main" };
            var comp = Comparer<string>.Create((a, b) =>
            {
                int aIndex = 0;
                int bIndex = 0;

                for (int i = 0; i < order.Length; ++i)
                {
                    if (a.StartsWith(order[i]))
                    {
                        aIndex = i;
                    }

                    if (b.StartsWith(order[i]))
                    {
                        bIndex = i;
                    }
                }

                return aIndex.CompareTo(bIndex);
            });

            return files.OrderBy(b => b.VirtualFile.Name, comp);
        }
    }
}

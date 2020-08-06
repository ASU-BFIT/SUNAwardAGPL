using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Reflection;
using System.Web.Caching;
using System.Web.Compilation;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Resolves paths to embedded dll files
    /// </summary>
    public sealed class EmbeddedPathProvider : VirtualPathProvider
    {
        private readonly Assembly _assembly;
        private readonly string[] _resources;
        private readonly string _slug;

        /// <summary>
        /// Constructs a new provider with the given URL slug (prefix).
        /// Do not call this directly, call EmbeddedPathProvider.Register() instead.
        /// </summary>
        /// <param name="slug"></param>
        public EmbeddedPathProvider(string slug = null)
        {
            _assembly = typeof(EmbeddedPathProvider).Assembly;
            _resources = _assembly.GetManifestResourceNames();
            _slug = slug;
        }

        /// <summary>
        /// Registers the provider into the hosting environment with the given URL slug (prefix)
        /// </summary>
        /// <param name="slug"></param>
        public static void Register(string slug = null)
        {
            if (BuildManager.IsPrecompiledApp)
            {
                throw new InvalidOperationException("Common controls cannot be used on a precompiled website.");
            }

            HostingEnvironment.RegisterVirtualPathProvider(new EmbeddedPathProvider(slug));
        }

        /// <summary>
        /// Check if embedded file exists
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath)
        {
            return IsEmbeddedResource(virtualPath) || base.FileExists(virtualPath);
        }

        /// <summary>
        /// Retrieves an embedded file
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsEmbeddedResource(virtualPath))
            {
                return new EmbeddedFile(virtualPath, this);
            }

            return base.GetFile(virtualPath);
        }

        /// <summary>
        /// No-op; caching is not employed for embedded files
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="virtualPathDependencies"></param>
        /// <param name="utcStart"></param>
        /// <returns></returns>
        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return null;
        }

        private bool IsEmbeddedResource(string virtualPath)
        {
            return _resources.Contains(NormalizePath(virtualPath));
        }

        internal string NormalizePath(string virtualPath)
        {
            string path = VirtualPathUtility.ToAppRelative(virtualPath).TrimStart('~', '/').Replace('/', '.');

            if (_slug != null && path.StartsWith(_slug + "."))
            {
                path = "BTS.Common.Mvc." + path.Substring(_slug.Length + 1);
            }

            return path;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Reflection;
using System.IO;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// A file embedded in the dll
    /// </summary>
    public sealed class EmbeddedFile : VirtualFile
    {
        private readonly Assembly _assembly;
        private readonly string _path;

        /// <summary>
        /// Constructs a new EmbeddedFile
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="provider"></param>
        public EmbeddedFile(string virtualPath, EmbeddedPathProvider provider)
            : base(virtualPath)
        {
            _assembly = typeof(EmbeddedFile).Assembly;
            _path = provider.NormalizePath(virtualPath);
        }

        /// <summary>
        /// Opens the file as a stream
        /// </summary>
        /// <returns></returns>
        public override Stream Open()
        {
            return _assembly.GetManifestResourceStream(_path);
        }
    }
}

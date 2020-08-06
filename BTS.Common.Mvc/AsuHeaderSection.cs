using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Web.config section to define how the global ASU header/footer are pulled in
    /// and what version of the header/footer is being rendered.
    /// </summary>
    public class AsuHeaderSection : ConfigurationSection
    {
        /// <summary>
        /// Version of the ASU header/footer to use. By default, stable is used.
        /// The special version "stable" can be used to always retrieve the latest stable version,
        /// however changes in the header/footer may necessitate application changes.
        /// </summary>
        [ConfigurationProperty("version", DefaultValue = "stable", IsRequired = false)]
        public string Version
        {
            get { return (string)base["version"]; }
            set { base["version"] = value; }
        }

        /// <summary>
        /// Where to cache the theme files, to reduce network latency instead of
        /// constantly reading from AFS or HTTPS.
        /// If not defined, by default we will cache to C:\inetpub\temp.
        /// </summary>
        [ConfigurationProperty("cacheDirectory", DefaultValue = null, IsRequired = false)]
        public string CacheDirectory
        {
            get { return (string)base["cacheDirectory"]; }
            set { base["cacheDirectory"] = value; }
        }

        /// <summary>
        /// How to fetch the theme files. By default, we pull from AFS, however
        /// it can switch to HTTPS instead for servers which do not have the AFS client installed
        /// or which do not wish to make use of AFS.
        /// </summary>
        [ConfigurationProperty("loadFrom", DefaultValue = FetchMode.AFS, IsRequired = false)]
        public FetchMode LoadFrom
        {
            get { return (FetchMode)base["loadFrom"]; }
            set { base["loadFrom"] = value; }
        }

        /// <summary>
        /// How to pull the header/footer files
        /// </summary>
        public enum FetchMode
        {
            /// <summary>
            /// Pull data using AFS. The AFS client must be installed.
            /// </summary>
            AFS,
            /// <summary>
            /// Pull data using HTTPS. Firewall ports to asu.edu:443 must be opened.
            /// </summary>
            HTTPS
        }
    }
}
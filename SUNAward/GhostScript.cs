using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace SUNAward
{
    // See https://www.ghostscript.com/doc/current/API.htm
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "P/Invoke names must match exactly")]
    public static class GhostScript
    {
        public static object GhostScriptLock = new object();
        public const int GS_ARG_ENCODING_UTF8 = 1;

        public delegate int Input(IntPtr callerHandle, IntPtr buf, int len);
        public delegate int Output(IntPtr callerHandle, IntPtr str, int len);

        [DllImport("gsdll64.dll")]
        public static extern int gsapi_new_instance(out IntPtr instance, IntPtr callerHandle);

        [DllImport("gsdll64.dll")]
        public static extern void gsapi_delete_instance(IntPtr instance);

        /// <summary>
        /// Run ghostscript with specified cli args. argv[0] is ignored.
        /// Run gsapi_set_arg_encoding() *before* calling this.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="argc"></param>
        /// <param name="argv">Args encoded as UTF8</param>
        /// <returns></returns>
        [DllImport("gsdll64.dll", CharSet = CharSet.Ansi)]
        public static extern int gsapi_init_with_args(IntPtr instance, int argc, string[] argv);

        /// <summary>
        /// Set encoding to UTF8
        /// </summary>
        /// <param name="instance">Instance retrieved from gsapi_new_instance</param>
        /// <param name="encoding">Use GhostScript>GS_ARG_ENCODING_UTF8</param>
        /// <returns></returns>
        [DllImport("gsdll64.dll")]
        public static extern int gsapi_set_arg_encoding(IntPtr instance, int encoding);

        [DllImport("gsdll64.dll")]
        public static extern int gsapi_exit(IntPtr instance);

        [DllImport("gsdll64.dll")]
        public static extern int gsapi_set_stdio(IntPtr instance, Input stdinFn, Output stdoutFn, Output stderrFn);
    }
}

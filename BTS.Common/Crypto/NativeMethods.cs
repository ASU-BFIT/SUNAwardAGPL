using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BTS.Common.Crypto
{
    /// <summary>
    /// P/Invoke wrappers to CNG DPAPI and related methods
    /// </summary>
    internal static class NativeMethods
    {
        internal const int NCRYPT_SILENT_FLAG = 0x00000040;

        /// <summary>
        /// Retrieve a handle to a protection descriptor object
        /// </summary>
        /// <param name="descriptorString">String of the form "SID=S-..." or a composite of SIDs with AND/OR between them</param>
        /// <param name="flags">Always set to 0</param>
        /// <param name="descriptor">Pointer to a protection descriptor object handle</param>
        /// <returns>Status code that indicates success or failure of the function. 0 is success.</returns>
        [DllImport("NCrypt.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int NCryptCreateProtectionDescriptor(string descriptorString, int flags, out IntPtr descriptor);

        /// <summary>
        /// Closes a protection descriptor object, freeing its memory
        /// </summary>
        /// <param name="descriptor">Descriptor to close</param>
        /// <returns>Status code that indicates success or failure of the function. 0 is success.</returns>
        [DllImport("NCrypt.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        internal static extern int NCryptCloseProtectionDescriptor(IntPtr descriptor);

        /// <summary>
        /// Encrypts data to the specified protection descriptor
        /// </summary>
        /// <param name="descriptor">Protection descriptor created via NCryptCreateProtectionDescriptor</param>
        /// <param name="flags">Set to NativeMethods.NCRYPT_SILENT_FLAG</param>
        /// <param name="data">Data to encrypt</param>
        /// <param name="dataSize">Number of bytes in the data array</param>
        /// <param name="memPara">Set to IntPtr.Zero</param>
        /// <param name="wnd">Set to IntPtr.Zero</param>
        /// <param name="protectedBlob">Encrypted data (pointer to byte array)</param>
        /// <param name="protectedSize">Number of bytes in encrypted data</param>
        /// <returns>Status code that indicates success or failure of the function. 0 is success.</returns>
        [DllImport("NCrypt.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int NCryptProtectSecret(IntPtr descriptor, int flags, byte[] data, int dataSize, IntPtr memPara, IntPtr wnd, out IntPtr protectedBlob, out int protectedSize);

        /// <summary>
        /// Decrypts data and retrieves its descriptor
        /// </summary>
        /// <param name="descriptor">Descriptor for the data</param>
        /// <param name="flags">Set to NativeMethods.NCRYPT_SILENT_FLAG</param>
        /// <param name="protectedBlob">Data to decrypt</param>
        /// <param name="protectedSize">Number of bytes in the protectedBlob array</param>
        /// <param name="memPara">Set to IntPtr.Zero</param>
        /// <param name="wnd">Set to IntPtr.Zero</param>
        /// <param name="data">Decrypted data (pointer to byte array)</param>
        /// <param name="dataSize">Number of bytes in decrypted data</param>
        /// <returns>Status code that indicates success or failure of the function. 0 is success.</returns>
        [DllImport("NCrypt.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int NCryptUnprotectSecret(out IntPtr descriptor, int flags, byte[] protectedBlob, int protectedSize, IntPtr memPara, IntPtr wnd, out IntPtr data, out int dataSize);

        /// <summary>
        /// Frees a pointer to unmanaged memory
        /// </summary>
        /// <param name="mem">Pointer</param>
        /// <returns>IntPtr.Zero on success, mem on failure</returns>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr mem);
    }
}

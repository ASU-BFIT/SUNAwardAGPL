using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BTS.Common.Crypto
{
    /// <summary>
    /// Wrapper around Windows CNG DPAPI, mimicking the same functionality as the
    /// legacy System.Cryptography.ProtectedData class (which uses the old DPAPI).
    /// This class should be preferred when protecting data that needs to be decrypted
    /// on multiple different machines.
    /// </summary>
    public sealed class ProtectedDataNg
    {
        /// <summary>
        /// Encrypts the data so that it can only be decrypted by the current user.
        /// </summary>
        /// <param name="userData">Data to encrypt</param>
        /// <returns>The encrypted bytes</returns>
        public static byte[] Protect(byte[] userData)
        {
            return Protect(userData, null);
        }

        /// <summary>
        /// Encrypts the data so that it can only be decrypted by specific users or groups
        /// </summary>
        /// <param name="userData">Data to encrypt</param>
        /// <param name="protectionDescriptor">
        /// Descriptor of which users and groups are allowed to decrypt this data.
        /// The user calling this function must belong to all of the specified values.
        /// Pass null to enforce that only the current user can decrypt.
        /// </param>
        /// <returns>The encrypted bytes</returns>
        public static byte[] Protect(byte[] userData, ProtectionDescriptorNg protectionDescriptor)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Calls to the Protect method are supported on Windows operating systems only.");
            }

            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }

            if (protectionDescriptor == null)
            {
                protectionDescriptor = new ProtectionDescriptorNg();
            }

            var success = NativeMethods.NCryptCreateProtectionDescriptor(protectionDescriptor.ToString(), 0, out var descriptor);
            if (success != 0)
            {
                throw new InvalidOperationException(String.Format("Unable to create protection descriptor, error code {0}", success));
            }

            success = NativeMethods.NCryptProtectSecret(descriptor, NativeMethods.NCRYPT_SILENT_FLAG, userData, userData.Length, IntPtr.Zero, IntPtr.Zero, out var protectedBlob, out var protectedSize);
            _ = NativeMethods.NCryptCloseProtectionDescriptor(descriptor);
            if (success != 0)
            {
                throw new InvalidOperationException(String.Format("Unable to protect data, error code {0}", success));
            }

            byte[] protectedData = new byte[protectedSize];
            Marshal.Copy(protectedBlob, protectedData, 0, protectedSize);
            NativeMethods.LocalFree(protectedBlob);

            return protectedData;
        }

        /// <summary>
        /// Decrypts the specified data
        /// </summary>
        /// <param name="encryptedData">Encrypted data</param>
        /// <returns>Decrypted data</returns>
        public static byte[] Unprotect(byte[] encryptedData)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Calls to the Protect method are supported on Windows operating systems only.");
            }

            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            var success = NativeMethods.NCryptUnprotectSecret(out var descriptor, NativeMethods.NCRYPT_SILENT_FLAG, encryptedData, encryptedData.Length, IntPtr.Zero, IntPtr.Zero, out var data, out var dataSize);
            if (success != 0)
            {
                throw new InvalidOperationException(String.Format("Unable to unprotect data, error code {0}", success));
            }

            byte[] decryptedData = new byte[dataSize];
            Marshal.Copy(data, decryptedData, 0, dataSize);
            NativeMethods.LocalFree(data);
            _ = NativeMethods.NCryptCloseProtectionDescriptor(descriptor);

            return decryptedData;
        }

        /// <summary>
        /// Convenience method to decrypt data encrypted using CryptUtil.exe
        /// </summary>
        /// <param name="encryptedData">Base64-encoded encrypted data</param>
        /// <returns>Decrypted string</returns>
        public static string Unprotect(string encryptedData)
        {
            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }
            else if (encryptedData.Length == 0)
            {
                throw new ArgumentException("Cannot unprotect an empty string", nameof(encryptedData));
            }

            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var decryptedBytes = Unprotect(encryptedBytes);

            return Encoding.ASCII.GetString(decryptedBytes);
        }
    }
}

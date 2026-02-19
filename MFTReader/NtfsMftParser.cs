using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MFTScanner.Models;

namespace MFTReader
{
    public class NtfsMftParser
    {
        #region WinAPI

        const uint GENERIC_READ = 0x80000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint OPEN_EXISTING = 3;

        const uint FSCTL_ENUM_USN_DATA = 0x000900b3;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            ref MFT_ENUM_DATA lpInBuffer,
            int nInBufferSize,
            byte[] lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        struct MFT_ENUM_DATA
        {
            public long StartFileReferenceNumber;
            public long LowUsn;
            public long HighUsn;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct USN_RECORD
        {
            public int RecordLength;
            public short MajorVersion;
            public short MinorVersion;
            public long FileReferenceNumber;
            public long ParentFileReferenceNumber;
            public long Usn;
            public long TimeStamp;
            public uint Reason;
            public uint SourceInfo;
            public uint SecurityId;
            public uint FileAttributes;
            public short FileNameLength;
            public short FileNameOffset;
        }

        const uint USN_REASON_FILE_DELETE = 0x00000200;

        #endregion


        public static IEnumerable<NtfsEntry> EnumerateVolume(string driveLetter)
        {
            string volumePath = @"\\.\" + driveLetter.TrimEnd('\\');

            var handle = CreateFile(volumePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var enumData = new MFT_ENUM_DATA
            {
                StartFileReferenceNumber = 0,
                LowUsn = 0,
                HighUsn = long.MaxValue
            };

            byte[] buffer = new byte[65536];
            int bytesReturned;

            while (DeviceIoControl(
                handle,
                FSCTL_ENUM_USN_DATA,
                ref enumData,
                Marshal.SizeOf(enumData),
                buffer,
                buffer.Length,
                out bytesReturned,
                IntPtr.Zero))
            {
                int offset = 8; // skip USN header

                while (offset < bytesReturned)
                {
                    IntPtr recordPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
                    var record = (USN_RECORD)Marshal.PtrToStructure(recordPtr, typeof(USN_RECORD));

                    string name = Marshal.PtrToStringUni(
                        recordPtr + record.FileNameOffset,
                        record.FileNameLength / 2);

                    yield return new NtfsEntry
                    {
                        FileReference = record.FileReferenceNumber,
                        ParentReference = record.ParentFileReferenceNumber,
                        Name = name,
                        IsDeleted = (record.Reason & USN_REASON_FILE_DELETE) != 0,
                        IsDirectory = (record.FileAttributes & 0x10) != 0,
                        IsSystem = (record.FileAttributes & 0x4) != 0,
                        IsTemporary = (record.FileAttributes & 0x100) != 0
                    };

                    offset += record.RecordLength;
                }

                enumData.StartFileReferenceNumber = BitConverter.ToInt64(buffer, 0);
            }
        }
    }
}

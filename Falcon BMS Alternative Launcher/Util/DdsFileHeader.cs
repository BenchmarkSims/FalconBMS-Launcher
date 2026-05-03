using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

namespace FalconBMS.Launcher
{
    internal static class DdsFileHeader
    {
        public static bool FileResolutionIs16Kx16K(string dds_path)
        {
            const uint sizeof_dds_header = 124;

            using (var mmf = MemoryMappedFile.CreateFromFile(dds_path, FileMode.Open))
            using (var mmfva = mmf.CreateViewAccessor(0, sizeof_dds_header, MemoryMappedFileAccess.Read))
            {
                //var mmfva = mmf.CreateViewAccessor(0, sizeof_dds_header, MemoryMappedFileAccess.Read);

                // Verify DDS file-header magic number.
                uint dds_dwMagic = mmfva.ReadUInt32(0);
                if (dds_dwMagic != 0x20534444) // "DDS " ascii chars
                    return false;

                // Read next fields from DDS_HEADER: https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
                uint dds_dwSize = mmfva.ReadUInt32(4);
                if (dds_dwSize != sizeof_dds_header)
                    return false;

                // Verify width and height are both 16k.
                const uint c_dds_flags_width_and_height = 0x2 + 0x4;
                uint dds_dwFlags = mmfva.ReadUInt32(8);
                dds_dwFlags &= c_dds_flags_width_and_height;
                if (dds_dwFlags != c_dds_flags_width_and_height)
                    return false;

                uint dds_dwHeight = mmfva.ReadUInt32(12);
                uint dds_dwWidth = mmfva.ReadUInt32(16);

                const uint c_16k = 16384;
                return (dds_dwHeight == c_16k && dds_dwWidth == c_16k);
            }
        }

    }
}

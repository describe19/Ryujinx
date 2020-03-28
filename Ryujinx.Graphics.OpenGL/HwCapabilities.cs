using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    static class HwCapabilities
    {
        private enum GpuVendor {
            AMD,
            INTEL,
            NVIDIA,
            UNKNOWN
        }

        private static Lazy<bool> _supportsAstcCompression = new Lazy<bool>(()
            => HasExtension("GL_KHR_texture_compression_astc_ldr"));

        private static Lazy<int> _maximumComputeSharedMemorySize = new Lazy<int>(()
            => GetLimit(All.MaxComputeSharedMemorySize));

        private static Lazy<int> _storageBufferOffsetAlignment   = new Lazy<int>(() 
            => GetLimit(All.ShaderStorageBufferOffsetAlignment));

        private static readonly Lazy<GpuVendor> _gpuVendor = new Lazy<GpuVendor>( GetGPUVendor );

        public static bool NeedsTextureViewFix
            => _gpuVendor.Value == GpuVendor.INTEL;

        public static bool SupportsAstcCompression
            => _supportsAstcCompression.Value;

        public static bool SupportsNonConstantTextureOffset
            => _gpuVendor.Value == GpuVendor.NVIDIA;

        public static int MaximumComputeSharedMemorySize
            => _maximumComputeSharedMemorySize.Value;

        public static int StorageBufferOffsetAlignment
            => _storageBufferOffsetAlignment.Value;

        private static bool HasExtension(string name)
        {
            int numExtensions = GL.GetInteger(GetPName.NumExtensions);

            for (int extension = 0; extension < numExtensions; extension++)
            {
                if (GL.GetString(StringNameIndexed.Extensions, extension) == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetLimit(All name)
        {
            return GL.GetInteger((GetPName)name);
        }

        private static GpuVendor GetGPUVendor() {
            string vendor = GL.GetString( StringName.Vendor ).ToLower();
            if ( vendor == "ati technologies inc." || vendor == "advanced micro devices, inc." || vendor == "mesa project" ) {
                return GpuVendor.AMD;
            } else if ( vendor == "Intel" ) {
                return GpuVendor.INTEL;
            } else if ( vendor == "nvidia corporation" ) {
                return GpuVendor.NVIDIA;
            } else {
                return GpuVendor.UNKNOWN;
            }
        }
    }
}

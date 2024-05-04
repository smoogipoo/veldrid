// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal static class WGPUFormats
    {
        public static AddressMode VdToWGPUAddressMode(SamplerAddressMode mode)
        {
            switch (mode)
            {
                case SamplerAddressMode.Wrap:
                    return AddressMode.Repeat;

                case SamplerAddressMode.Mirror:
                    return AddressMode.MirrorRepeat;

                case SamplerAddressMode.Clamp:
                    return AddressMode.ClampToEdge;

                case SamplerAddressMode.Border:
                    // Not supported right now.
                    return AddressMode.ClampToEdge;

                default:
                    throw Illegal.Value<SamplerAddressMode>();
            }
        }

        public static void GetFilterParams(
            SamplerFilter filter,
            out FilterMode minFilter,
            out FilterMode magFilter,
            out MipmapFilterMode mipmapFilter)
        {
            switch (filter)
            {
                case SamplerFilter.Anisotropic:
                    minFilter = FilterMode.Linear;
                    magFilter = FilterMode.Linear;
                    mipmapFilter = MipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinPointMagPointMipPoint:
                    minFilter = FilterMode.Nearest;
                    magFilter = FilterMode.Nearest;
                    mipmapFilter = MipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinPointMagPointMipLinear:
                    minFilter = FilterMode.Nearest;
                    magFilter = FilterMode.Nearest;
                    mipmapFilter = MipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinPointMagLinearMipPoint:
                    minFilter = FilterMode.Nearest;
                    magFilter = FilterMode.Linear;
                    mipmapFilter = MipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinPointMagLinearMipLinear:
                    minFilter = FilterMode.Nearest;
                    magFilter = FilterMode.Linear;
                    mipmapFilter = MipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinLinearMagPointMipPoint:
                    minFilter = FilterMode.Linear;
                    magFilter = FilterMode.Nearest;
                    mipmapFilter = MipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinLinearMagPointMipLinear:
                    minFilter = FilterMode.Linear;
                    magFilter = FilterMode.Nearest;
                    mipmapFilter = MipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinLinearMagLinearMipPoint:
                    minFilter = FilterMode.Linear;
                    magFilter = FilterMode.Linear;
                    mipmapFilter = MipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinLinearMagLinearMipLinear:
                    minFilter = FilterMode.Linear;
                    magFilter = FilterMode.Linear;
                    mipmapFilter = MipmapFilterMode.Linear;
                    break;

                default:
                    throw Illegal.Value<SamplerFilter>();
            }
        }

        public static CompareFunction VdToWGPUCompareFunction(ComparisonKind comparisonKind)
        {
            switch (comparisonKind)
            {
                case ComparisonKind.Never:
                    return CompareFunction.Never;

                case ComparisonKind.Less:
                    return CompareFunction.Less;

                case ComparisonKind.Equal:
                    return CompareFunction.Equal;

                case ComparisonKind.LessEqual:
                    return CompareFunction.LessEqual;

                case ComparisonKind.Greater:
                    return CompareFunction.Greater;

                case ComparisonKind.NotEqual:
                    return CompareFunction.NotEqual;

                case ComparisonKind.GreaterEqual:
                    return CompareFunction.GreaterEqual;

                case ComparisonKind.Always:
                    return CompareFunction.Always;

                default:
                    throw Illegal.Value<ComparisonKind>();
            }
        }

        public static uint VdToWGPUSampleCount(TextureSampleCount sampleCount)
        {
            switch (sampleCount)
            {
                case TextureSampleCount.Count1:
                    return 1;

                case TextureSampleCount.Count2:
                    return 2;

                case TextureSampleCount.Count4:
                    return 4;

                case TextureSampleCount.Count8:
                    return 8;

                case TextureSampleCount.Count16:
                    return 16;

                case TextureSampleCount.Count32:
                    return 32;

                default:
                    throw Illegal.Value<TextureSampleCount>();
            }
        }

        public static Silk.NET.WebGPU.TextureUsage VdToWGPUTextureUsage(TextureUsage usage)
        {
            var wgpuUsage = Silk.NET.WebGPU.TextureUsage.CopyDst | Silk.NET.WebGPU.TextureUsage.CopySrc;

            if ((usage & TextureUsage.Sampled) == TextureUsage.Sampled)
                wgpuUsage |= Silk.NET.WebGPU.TextureUsage.TextureBinding;

            if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
                wgpuUsage |= Silk.NET.WebGPU.TextureUsage.RenderAttachment;

            if ((usage & TextureUsage.Storage) == TextureUsage.Storage)
                wgpuUsage |= Silk.NET.WebGPU.TextureUsage.StorageBinding;

            return wgpuUsage;
        }

        public static TextureFormat VdToWGPUTextureFormat(PixelFormat format, bool toDepthFormat = true)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8UNorm:
                    return TextureFormat.Rgba8Unorm;

                case PixelFormat.B8G8R8A8UNorm:
                    return TextureFormat.Bgra8Unorm;

                case PixelFormat.R8UNorm:
                    return TextureFormat.R8Unorm;

                case PixelFormat.R32G32B32A32Float:
                    return TextureFormat.Rgba32float;

                case PixelFormat.R32Float:
                    return toDepthFormat ? TextureFormat.Depth32float : TextureFormat.R32float;

                case PixelFormat.Bc3UNorm:
                    return TextureFormat.BC3RgbaUnorm;

                case PixelFormat.D24UNormS8UInt:
                    return TextureFormat.Depth24PlusStencil8;

                case PixelFormat.D32FloatS8UInt:
                    return TextureFormat.Depth32floatStencil8;

                case PixelFormat.R32G32B32A32UInt:
                    return TextureFormat.Rgba32Uint;

                case PixelFormat.R8G8SNorm:
                    return TextureFormat.RG8Snorm;

                case PixelFormat.Bc1RgbUNorm:
                    throw Illegal.Value<PixelFormat>();

                case PixelFormat.Bc1RgbaUNorm:
                    return TextureFormat.BC1RgbaUnorm;

                case PixelFormat.Bc2UNorm:
                    return TextureFormat.BC2RgbaUnorm;

                case PixelFormat.R10G10B10A2UNorm:
                    return TextureFormat.Rgb10A2Unorm;

                case PixelFormat.R10G10B10A2UInt:
                    return TextureFormat.Rgb10A2Uint;

                case PixelFormat.R11G11B10Float:
                    return TextureFormat.RG11B10Ufloat;

                case PixelFormat.R8SNorm:
                    return TextureFormat.R8Snorm;

                case PixelFormat.R8UInt:
                    return TextureFormat.R8Uint;

                case PixelFormat.R8SInt:
                    return TextureFormat.R8Sint;

                case PixelFormat.R16UInt:
                    return TextureFormat.R16Uint;

                case PixelFormat.R16SInt:
                    return TextureFormat.R16Sint;

                case PixelFormat.R16Float:
                    return TextureFormat.R16float;

                case PixelFormat.R32UInt:
                    return TextureFormat.R32Uint;

                case PixelFormat.R32SInt:
                    return TextureFormat.R32Sint;

                case PixelFormat.R8G8UNorm:
                    return TextureFormat.RG8Unorm;

                case PixelFormat.R8G8UInt:
                    return TextureFormat.RG8Uint;

                case PixelFormat.R8G8SInt:
                    return TextureFormat.RG8Sint;

                case PixelFormat.R16G16UInt:
                    return TextureFormat.RG16Uint;

                case PixelFormat.R16G16SInt:
                    return TextureFormat.RG16Sint;

                case PixelFormat.R16G16Float:
                    return TextureFormat.RG16float;

                case PixelFormat.R32G32UInt:
                    return TextureFormat.RG32Uint;

                case PixelFormat.R32G32SInt:
                    return TextureFormat.RG32Sint;

                case PixelFormat.R32G32Float:
                    return TextureFormat.RG32float;

                case PixelFormat.R8G8B8A8SNorm:
                    return TextureFormat.Rgba8Snorm;

                case PixelFormat.R8G8B8A8UInt:
                    return TextureFormat.Rgba8Uint;

                case PixelFormat.R8G8B8A8SInt:
                    return TextureFormat.Rgba8Sint;

                case PixelFormat.R16G16B16A16UInt:
                    return TextureFormat.Rgba16Uint;

                case PixelFormat.R16G16B16A16SInt:
                    return TextureFormat.Rgba16Sint;

                case PixelFormat.R16G16B16A16Float:
                    return TextureFormat.Rgba16float;

                case PixelFormat.R32G32B32A32SInt:
                    return TextureFormat.Rgba32Sint;

                case PixelFormat.Etc2R8G8B8UNorm:
                    return TextureFormat.Etc2Rgb8Unorm;

                case PixelFormat.Etc2R8G8B8A1UNorm:
                    return TextureFormat.Etc2Rgb8A1Unorm;

                case PixelFormat.Etc2R8G8B8A8UNorm:
                    return TextureFormat.Etc2Rgba8Unorm;

                case PixelFormat.Bc4UNorm:
                    return TextureFormat.BC4RUnorm;

                case PixelFormat.Bc4SNorm:
                    return TextureFormat.BC4RSnorm;

                case PixelFormat.Bc5UNorm:
                    return TextureFormat.BC5RGUnorm;

                case PixelFormat.Bc5SNorm:
                    return TextureFormat.BC5RGSnorm;

                case PixelFormat.Bc7UNorm:
                    return TextureFormat.BC7RgbaUnorm;

                case PixelFormat.R8G8B8A8UNormSRgb:
                    return TextureFormat.Rgba8UnormSrgb;

                case PixelFormat.B8G8R8A8UNormSRgb:
                    return TextureFormat.Bgra8UnormSrgb;

                case PixelFormat.Bc1RgbUNormSRgb:
                case PixelFormat.Bc1RgbaUNormSRgb:
                    return TextureFormat.BC1RgbaUnormSrgb;

                case PixelFormat.Bc2UNormSRgb:
                    return TextureFormat.BC2RgbaUnormSrgb;

                case PixelFormat.Bc3UNormSRgb:
                    return TextureFormat.BC3RgbaUnormSrgb;

                case PixelFormat.Bc7UNormSRgb:
                    return TextureFormat.BC7RgbaUnormSrgb;

                // R16 norm values (non-depth) are only supported by the TEXTURE_FORMAT_16BIT_NORM extension.
                case PixelFormat.R16UNorm:
                    return toDepthFormat ? TextureFormat.Depth16Unorm : throw Illegal.Value<PixelFormat>();

                case PixelFormat.R16SNorm:
                case PixelFormat.R16G16UNorm:
                case PixelFormat.R16G16SNorm:
                case PixelFormat.R16G16B16A16UNorm:
                case PixelFormat.R16G16B16A16SNorm:
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        public static TextureDimension VdToWGPUTextureDimention(uint depth)
        {
            if (depth == 1)
                return TextureDimension.Dimension2D;

            if (depth > 1)
                return TextureDimension.Dimension3D;

            throw Illegal.Value<uint>();
        }

        public static TextureViewDimension VdToWGPUTextureViewDimention(uint depth)
        {
            if (depth == 1)
                return TextureViewDimension.Dimension2D;

            if (depth > 1)
                return TextureViewDimension.Dimension3D;

            throw Illegal.Value<uint>();
        }

        public static Silk.NET.WebGPU.BufferUsage VdToWGPUBufferUsage(BufferUsage usage)
        {
            var wgpuUsage = Silk.NET.WebGPU.BufferUsage.CopySrc | Silk.NET.WebGPU.BufferUsage.CopyDst;

            if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.Index;

            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.Indirect;

            if ((usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly
                || (usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite)
            {
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.Storage;
            }

            if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.Uniform;

            if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.Vertex;

            if ((usage & BufferUsage.Staging) == BufferUsage.Staging)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.MapRead | Silk.NET.WebGPU.BufferUsage.MapWrite;

            if ((usage & BufferUsage.Dynamic) == BufferUsage.Dynamic)
                wgpuUsage |= Silk.NET.WebGPU.BufferUsage.MapWrite;

            return wgpuUsage;
        }
    }
}

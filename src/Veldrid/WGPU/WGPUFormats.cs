// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using WebGPU;

namespace Veldrid.WGPU
{
    internal static class WGPUFormats
    {
        public static WGPUAddressMode VdToWGPUAddressMode(SamplerAddressMode mode)
        {
            switch (mode)
            {
                case SamplerAddressMode.Wrap:
                    return WGPUAddressMode.Repeat;

                case SamplerAddressMode.Mirror:
                    return WGPUAddressMode.MirrorRepeat;

                case SamplerAddressMode.Clamp:
                    return WGPUAddressMode.ClampToEdge;

                case SamplerAddressMode.Border:
                    // Not supported right now.
                    return WGPUAddressMode.ClampToEdge;

                default:
                    throw Illegal.Value<SamplerAddressMode>();
            }
        }

        public static void GetFilterParams(
            SamplerFilter filter,
            out WGPUFilterMode minFilter,
            out WGPUFilterMode magFilter,
            out WGPUMipmapFilterMode mipmapFilter)
        {
            switch (filter)
            {
                case SamplerFilter.Anisotropic:
                    minFilter = WGPUFilterMode.Linear;
                    magFilter = WGPUFilterMode.Linear;
                    mipmapFilter = WGPUMipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinPointMagPointMipPoint:
                    minFilter = WGPUFilterMode.Nearest;
                    magFilter = WGPUFilterMode.Nearest;
                    mipmapFilter = WGPUMipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinPointMagPointMipLinear:
                    minFilter = WGPUFilterMode.Nearest;
                    magFilter = WGPUFilterMode.Nearest;
                    mipmapFilter = WGPUMipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinPointMagLinearMipPoint:
                    minFilter = WGPUFilterMode.Nearest;
                    magFilter = WGPUFilterMode.Linear;
                    mipmapFilter = WGPUMipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinPointMagLinearMipLinear:
                    minFilter = WGPUFilterMode.Nearest;
                    magFilter = WGPUFilterMode.Linear;
                    mipmapFilter = WGPUMipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinLinearMagPointMipPoint:
                    minFilter = WGPUFilterMode.Linear;
                    magFilter = WGPUFilterMode.Nearest;
                    mipmapFilter = WGPUMipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinLinearMagPointMipLinear:
                    minFilter = WGPUFilterMode.Linear;
                    magFilter = WGPUFilterMode.Nearest;
                    mipmapFilter = WGPUMipmapFilterMode.Linear;
                    break;

                case SamplerFilter.MinLinearMagLinearMipPoint:
                    minFilter = WGPUFilterMode.Linear;
                    magFilter = WGPUFilterMode.Linear;
                    mipmapFilter = WGPUMipmapFilterMode.Nearest;
                    break;

                case SamplerFilter.MinLinearMagLinearMipLinear:
                    minFilter = WGPUFilterMode.Linear;
                    magFilter = WGPUFilterMode.Linear;
                    mipmapFilter = WGPUMipmapFilterMode.Linear;
                    break;

                default:
                    throw Illegal.Value<SamplerFilter>();
            }
        }

        public static WGPUCompareFunction VdToWGPUCompareFunction(ComparisonKind comparisonKind)
        {
            switch (comparisonKind)
            {
                case ComparisonKind.Never:
                    return WGPUCompareFunction.Never;

                case ComparisonKind.Less:
                    return WGPUCompareFunction.Less;

                case ComparisonKind.Equal:
                    return WGPUCompareFunction.Equal;

                case ComparisonKind.LessEqual:
                    return WGPUCompareFunction.LessEqual;

                case ComparisonKind.Greater:
                    return WGPUCompareFunction.Greater;

                case ComparisonKind.NotEqual:
                    return WGPUCompareFunction.NotEqual;

                case ComparisonKind.GreaterEqual:
                    return WGPUCompareFunction.GreaterEqual;

                case ComparisonKind.Always:
                    return WGPUCompareFunction.Always;

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

        public static WGPUTextureUsage VdToWGPUTextureUsage(TextureUsage usage)
        {
            var wgpuUsage = WGPUTextureUsage.CopyDst | WGPUTextureUsage.CopySrc;

            if ((usage & TextureUsage.Sampled) == TextureUsage.Sampled)
                wgpuUsage |= WGPUTextureUsage.TextureBinding;

            if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
                wgpuUsage |= WGPUTextureUsage.RenderAttachment;

            if ((usage & TextureUsage.Storage) == TextureUsage.Storage)
                wgpuUsage |= WGPUTextureUsage.StorageBinding;

            if ((usage & TextureUsage.GenerateMipmaps) == TextureUsage.GenerateMipmaps)
                wgpuUsage |= WGPUTextureUsage.TextureBinding | WGPUTextureUsage.StorageBinding;

            return wgpuUsage;
        }

        public static WGPUTextureFormat VdToWGPUTextureFormat(PixelFormat format, bool toDepthFormat = true)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8UNorm:
                    return WGPUTextureFormat.RGBA8Unorm;

                case PixelFormat.B8G8R8A8UNorm:
                    return WGPUTextureFormat.BGRA8Unorm;

                case PixelFormat.R8UNorm:
                    return WGPUTextureFormat.R8Unorm;

                case PixelFormat.R32G32B32A32Float:
                    return WGPUTextureFormat.RGBA32Float;

                case PixelFormat.R32Float:
                    return toDepthFormat ? WGPUTextureFormat.Depth32Float : WGPUTextureFormat.R32Float;

                case PixelFormat.Bc3UNorm:
                    return WGPUTextureFormat.BC3RGBAUnorm;

                case PixelFormat.D24UNormS8UInt:
                    return WGPUTextureFormat.Depth24PlusStencil8;

                case PixelFormat.D32FloatS8UInt:
                    return WGPUTextureFormat.Depth32FloatStencil8;

                case PixelFormat.R32G32B32A32UInt:
                    return WGPUTextureFormat.RGBA32Uint;

                case PixelFormat.R8G8SNorm:
                    return WGPUTextureFormat.RG8Snorm;

                case PixelFormat.Bc1RgbUNorm:
                    throw Illegal.Value<PixelFormat>();

                case PixelFormat.Bc1RgbaUNorm:
                    return WGPUTextureFormat.BC1RGBAUnorm;

                case PixelFormat.Bc2UNorm:
                    return WGPUTextureFormat.BC2RGBAUnorm;

                case PixelFormat.R10G10B10A2UNorm:
                    return WGPUTextureFormat.RGB10A2Unorm;

                case PixelFormat.R10G10B10A2UInt:
                    // Supported from 2.21.0 onwards.
                    // return WGPUTextureFormat.RGB10A2Uint;
                    throw Illegal.Value<PixelFormat>();

                case PixelFormat.R11G11B10Float:
                    return WGPUTextureFormat.RG11B10Ufloat;

                case PixelFormat.R8SNorm:
                    return WGPUTextureFormat.R8Snorm;

                case PixelFormat.R8UInt:
                    return WGPUTextureFormat.R8Uint;

                case PixelFormat.R8SInt:
                    return WGPUTextureFormat.R8Sint;

                case PixelFormat.R16UInt:
                    return WGPUTextureFormat.R16Uint;

                case PixelFormat.R16SInt:
                    return WGPUTextureFormat.R16Sint;

                case PixelFormat.R16Float:
                    return WGPUTextureFormat.R16Float;

                case PixelFormat.R32UInt:
                    return WGPUTextureFormat.R32Uint;

                case PixelFormat.R32SInt:
                    return WGPUTextureFormat.R32Sint;

                case PixelFormat.R8G8UNorm:
                    return WGPUTextureFormat.RG8Unorm;

                case PixelFormat.R8G8UInt:
                    return WGPUTextureFormat.RG8Uint;

                case PixelFormat.R8G8SInt:
                    return WGPUTextureFormat.RG8Sint;

                case PixelFormat.R16G16UInt:
                    return WGPUTextureFormat.RG16Uint;

                case PixelFormat.R16G16SInt:
                    return WGPUTextureFormat.RG16Sint;

                case PixelFormat.R16G16Float:
                    return WGPUTextureFormat.RG16Float;

                case PixelFormat.R32G32UInt:
                    return WGPUTextureFormat.RG32Uint;

                case PixelFormat.R32G32SInt:
                    return WGPUTextureFormat.RG32Sint;

                case PixelFormat.R32G32Float:
                    return WGPUTextureFormat.RG32Float;

                case PixelFormat.R8G8B8A8SNorm:
                    return WGPUTextureFormat.RGBA8Snorm;

                case PixelFormat.R8G8B8A8UInt:
                    return WGPUTextureFormat.RGBA8Uint;

                case PixelFormat.R8G8B8A8SInt:
                    return WGPUTextureFormat.RGBA8Sint;

                case PixelFormat.R16G16B16A16UInt:
                    return WGPUTextureFormat.RGBA16Uint;

                case PixelFormat.R16G16B16A16SInt:
                    return WGPUTextureFormat.RGBA16Sint;

                case PixelFormat.R16G16B16A16Float:
                    return WGPUTextureFormat.RGBA16Float;

                case PixelFormat.R32G32B32A32SInt:
                    return WGPUTextureFormat.RGBA32Sint;

                case PixelFormat.Etc2R8G8B8UNorm:
                    return WGPUTextureFormat.ETC2RGB8Unorm;

                case PixelFormat.Etc2R8G8B8A1UNorm:
                    return WGPUTextureFormat.ETC2RGB8A1Unorm;

                case PixelFormat.Etc2R8G8B8A8UNorm:
                    return WGPUTextureFormat.ETC2RGBA8Unorm;

                case PixelFormat.Bc4UNorm:
                    return WGPUTextureFormat.BC4RUnorm;

                case PixelFormat.Bc4SNorm:
                    return WGPUTextureFormat.BC4RSnorm;

                case PixelFormat.Bc5UNorm:
                    return WGPUTextureFormat.BC5RGUnorm;

                case PixelFormat.Bc5SNorm:
                    return WGPUTextureFormat.BC5RGSnorm;

                case PixelFormat.Bc7UNorm:
                    return WGPUTextureFormat.BC7RGBAUnorm;

                case PixelFormat.R8G8B8A8UNormSRgb:
                    return WGPUTextureFormat.RGBA8UnormSrgb;

                case PixelFormat.B8G8R8A8UNormSRgb:
                    return WGPUTextureFormat.BGRA8UnormSrgb;

                case PixelFormat.Bc1RgbUNormSRgb:
                case PixelFormat.Bc1RgbaUNormSRgb:
                    return WGPUTextureFormat.BC1RGBAUnormSrgb;

                case PixelFormat.Bc2UNormSRgb:
                    return WGPUTextureFormat.BC2RGBAUnormSrgb;

                case PixelFormat.Bc3UNormSRgb:
                    return WGPUTextureFormat.BC3RGBAUnormSrgb;

                case PixelFormat.Bc7UNormSRgb:
                    return WGPUTextureFormat.BC7RGBAUnormSrgb;

                // R16 norm values (non-depth) are only supported by the TEXTURE_FORMAT_16BIT_NORM extension.
                case PixelFormat.R16UNorm:
                    return toDepthFormat ? WGPUTextureFormat.Depth16Unorm : throw Illegal.Value<PixelFormat>();

                case PixelFormat.R16SNorm:
                case PixelFormat.R16G16UNorm:
                case PixelFormat.R16G16SNorm:
                case PixelFormat.R16G16B16A16UNorm:
                case PixelFormat.R16G16B16A16SNorm:
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        public static PixelFormat WGPUToVdPixelFormat(WGPUTextureFormat format)
        {
            switch (format)
            {
                case WGPUTextureFormat.RGBA8Unorm:
                    return PixelFormat.R8G8B8A8UNorm;

                case WGPUTextureFormat.BGRA8Unorm:
                    return PixelFormat.B8G8R8A8UNorm;

                case WGPUTextureFormat.R8Unorm:
                    return PixelFormat.R8UNorm;

                case WGPUTextureFormat.RGBA32Float:
                    return PixelFormat.R32G32B32A32Float;

                case WGPUTextureFormat.Depth32Float:
                case WGPUTextureFormat.R32Float:
                    return PixelFormat.R32Float;

                case WGPUTextureFormat.BC3RGBAUnorm:
                    return PixelFormat.Bc3UNorm;

                case WGPUTextureFormat.Depth24PlusStencil8:
                    return PixelFormat.D24UNormS8UInt;

                case WGPUTextureFormat.Depth32FloatStencil8:
                    return PixelFormat.D32FloatS8UInt;

                case WGPUTextureFormat.RGBA32Uint:
                    return PixelFormat.R32G32B32A32UInt;

                case WGPUTextureFormat.RG8Snorm:
                    return PixelFormat.R8G8SNorm;

                case WGPUTextureFormat.RGBA8Snorm:
                    return PixelFormat.R8G8B8A8SNorm;

                case WGPUTextureFormat.RGBA8Uint:
                    return PixelFormat.R8G8B8A8UInt;

                case WGPUTextureFormat.RGBA8Sint:
                    return PixelFormat.R8G8B8A8SInt;

                case WGPUTextureFormat.RGBA16Uint:
                    return PixelFormat.R16G16B16A16UInt;

                case WGPUTextureFormat.RGBA16Sint:
                    return PixelFormat.R16G16B16A16SInt;

                case WGPUTextureFormat.RGBA16Float:
                    return PixelFormat.R16G16B16A16Float;

                case WGPUTextureFormat.RGBA32Sint:
                    return PixelFormat.R32G32B32A32SInt;

                case WGPUTextureFormat.ETC2RGB8Unorm:
                    return PixelFormat.Etc2R8G8B8UNorm;

                case WGPUTextureFormat.ETC2RGB8A1Unorm:
                    return PixelFormat.Etc2R8G8B8A1UNorm;

                case WGPUTextureFormat.ETC2RGBA8Unorm:
                    return PixelFormat.Etc2R8G8B8A8UNorm;

                case WGPUTextureFormat.BC4RUnorm:
                    return PixelFormat.Bc4UNorm;

                case WGPUTextureFormat.BC4RSnorm:
                    return PixelFormat.Bc4SNorm;

                case WGPUTextureFormat.BC5RGUnorm:
                    return PixelFormat.Bc5UNorm;

                case WGPUTextureFormat.BC5RGSnorm:
                    return PixelFormat.Bc5SNorm;

                case WGPUTextureFormat.BC7RGBAUnorm:
                    return PixelFormat.Bc7UNorm;

                case WGPUTextureFormat.RGBA8UnormSrgb:
                    return PixelFormat.R8G8B8A8UNormSRgb;

                case WGPUTextureFormat.BGRA8UnormSrgb:
                    return PixelFormat.B8G8R8A8UNormSRgb;

                case WGPUTextureFormat.BC1RGBAUnorm:
                    return PixelFormat.Bc1RgbaUNorm;

                case WGPUTextureFormat.R8Snorm:
                    return PixelFormat.R8SNorm;

                case WGPUTextureFormat.R8Uint:
                    return PixelFormat.R8UInt;

                case WGPUTextureFormat.R8Sint:
                    return PixelFormat.R8SInt;

                case WGPUTextureFormat.R16Uint:
                    return PixelFormat.R16UInt;

                case WGPUTextureFormat.R16Sint:
                    return PixelFormat.R16SInt;

                case WGPUTextureFormat.R32Uint:
                    return PixelFormat.R32UInt;

                case WGPUTextureFormat.R32Sint:
                    return PixelFormat.R32SInt;

                case WGPUTextureFormat.RG8Unorm:
                    return PixelFormat.R8G8UNorm;

                case WGPUTextureFormat.RG8Uint:
                    return PixelFormat.R8G8UInt;

                case WGPUTextureFormat.RG8Sint:
                    return PixelFormat.R8G8SInt;

                case WGPUTextureFormat.RG16Uint:
                    return PixelFormat.R16G16UInt;

                case WGPUTextureFormat.RG16Sint:
                    return PixelFormat.R16G16SInt;

                case WGPUTextureFormat.RG16Float:
                    return PixelFormat.R16G16Float;

                case WGPUTextureFormat.RG32Uint:
                    return PixelFormat.R32G32UInt;

                case WGPUTextureFormat.RG32Sint:
                    return PixelFormat.R32G32SInt;

                case WGPUTextureFormat.RG32Float:
                    return PixelFormat.R32G32Float;

                case WGPUTextureFormat.BC1RGBAUnormSrgb:
                case WGPUTextureFormat.BC2RGBAUnormSrgb:
                    return PixelFormat.Bc1RgbaUNormSRgb;

                case WGPUTextureFormat.BC3RGBAUnormSrgb:
                    return PixelFormat.Bc3UNormSRgb;

                case WGPUTextureFormat.BC7RGBAUnormSrgb:
                    return PixelFormat.Bc7UNormSRgb;

                case WGPUTextureFormat.R16Float:
                    return PixelFormat.R16Float;

                case WGPUTextureFormat.Depth16Unorm:
                    return PixelFormat.R16UNorm;

                default:
                    throw Illegal.Value<WGPUTextureFormat>();
            }
        }

        public static WGPUTextureDimension VdToWGPUTextureDimention(uint depth)
        {
            if (depth == 1)
                return WGPUTextureDimension._2D;

            if (depth > 1)
                return WGPUTextureDimension._3D;

            throw Illegal.Value<uint>();
        }

        public static WGPUTextureViewDimension VdToWGPUTextureViewDimention(uint depth)
        {
            if (depth == 1)
                return WGPUTextureViewDimension._2D;

            if (depth > 1)
                return WGPUTextureViewDimension._3D;

            throw Illegal.Value<uint>();
        }

        public static WGPUBufferUsage VdToWGPUBufferUsage(BufferUsage usage)
        {
            var wgpuUsage = WGPUBufferUsage.CopySrc | WGPUBufferUsage.CopyDst;

            if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
                wgpuUsage |= WGPUBufferUsage.Index;

            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
                wgpuUsage |= WGPUBufferUsage.Indirect;

            if ((usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly
                || (usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite)
            {
                wgpuUsage |= WGPUBufferUsage.Storage;
            }

            if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
                wgpuUsage |= WGPUBufferUsage.Uniform;

            if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
                wgpuUsage |= WGPUBufferUsage.Vertex;

            // if ((usage & BufferUsage.Staging) == BufferUsage.Staging)
            //     wgpuUsage |= WGPUBufferUsage.MapRead | WGPUBufferUsage.MapWrite;
            //
            // if ((usage & BufferUsage.Dynamic) == BufferUsage.Dynamic)
            //     wgpuUsage |= WGPUBufferUsage.MapWrite;

            return wgpuUsage;
        }

        public static WGPUPrimitiveTopology VdToWGPUPrimitiveTopology(PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.TriangleList:
                    return WGPUPrimitiveTopology.TriangleList;

                case PrimitiveTopology.TriangleStrip:
                    return WGPUPrimitiveTopology.TriangleStrip;

                case PrimitiveTopology.LineList:
                    return WGPUPrimitiveTopology.LineList;

                case PrimitiveTopology.LineStrip:
                    return WGPUPrimitiveTopology.LineStrip;

                case PrimitiveTopology.PointList:
                    return WGPUPrimitiveTopology.PointList;

                default:
                    throw Illegal.Value<PrimitiveTopology>();
            }
        }

        public static WGPUFrontFace VdToWGPUFrontFace(FrontFace frontFace)
        {
            switch (frontFace)
            {
                case FrontFace.Clockwise:
                    return WGPUFrontFace.CW;

                case FrontFace.CounterClockwise:
                    return WGPUFrontFace.CCW;

                default:
                    throw Illegal.Value<FrontFace>();
            }
        }

        public static WGPUCullMode VdToWGPUCullMode(FaceCullMode cullMode)
        {
            switch (cullMode)
            {
                case FaceCullMode.None:
                    return WGPUCullMode.None;

                case FaceCullMode.Front:
                    return WGPUCullMode.Front;

                case FaceCullMode.Back:
                    return WGPUCullMode.Back;

                default:
                    throw Illegal.Value<FaceCullMode>();
            }
        }

        public static WGPUBlendOperation VdToWGPUBlendOperation(BlendFunction function)
        {
            switch (function)
            {
                case BlendFunction.Add:
                    return WGPUBlendOperation.Add;

                case BlendFunction.Subtract:
                    return WGPUBlendOperation.Subtract;

                case BlendFunction.ReverseSubtract:
                    return WGPUBlendOperation.ReverseSubtract;

                case BlendFunction.Minimum:
                    return WGPUBlendOperation.Min;

                case BlendFunction.Maximum:
                    return WGPUBlendOperation.Max;

                default:
                    throw Illegal.Value<BlendFunction>();
            }
        }

        public static WGPUBlendFactor VdToWGPUBlendFactor(BlendFactor factor)
        {
            switch (factor)
            {
                case BlendFactor.Zero:
                    return WGPUBlendFactor.Zero;

                case BlendFactor.One:
                    return WGPUBlendFactor.One;

                case BlendFactor.SourceAlpha:
                    return WGPUBlendFactor.SrcAlpha;

                case BlendFactor.InverseSourceAlpha:
                    return WGPUBlendFactor.OneMinusSrcAlpha;

                case BlendFactor.DestinationAlpha:
                    return WGPUBlendFactor.DstAlpha;

                case BlendFactor.InverseDestinationAlpha:
                    return WGPUBlendFactor.OneMinusDstAlpha;

                case BlendFactor.SourceColor:
                    return WGPUBlendFactor.Src;

                case BlendFactor.InverseSourceColor:
                    return WGPUBlendFactor.OneMinusSrc;

                case BlendFactor.DestinationColor:
                    return WGPUBlendFactor.Dst;

                case BlendFactor.InverseDestinationColor:
                    return WGPUBlendFactor.OneMinusDst;

                case BlendFactor.BlendFactor:
                    return WGPUBlendFactor.Constant;

                case BlendFactor.InverseBlendFactor:
                    return WGPUBlendFactor.OneMinusConstant;

                default:
                    throw Illegal.Value<BlendFactor>();
            }
        }

        public static WGPUColorWriteMask VdToWGPUColorWriteMask(ColorWriteMask mask)
        {
            var flags = WGPUColorWriteMask.None;

            if ((mask & ColorWriteMask.Red) == ColorWriteMask.Red)
                flags |= WGPUColorWriteMask.Red;

            if ((mask & ColorWriteMask.Green) == ColorWriteMask.Green)
                flags |= WGPUColorWriteMask.Green;

            if ((mask & ColorWriteMask.Blue) == ColorWriteMask.Blue)
                flags |= WGPUColorWriteMask.Blue;

            if ((mask & ColorWriteMask.Alpha) == ColorWriteMask.Alpha)
                flags |= WGPUColorWriteMask.Alpha;

            return flags;
        }

        public static WGPUStencilOperation VdToWGPUStencilOperation(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Keep:
                    return WGPUStencilOperation.Keep;

                case StencilOperation.Zero:
                    return WGPUStencilOperation.Zero;

                case StencilOperation.Replace:
                    return WGPUStencilOperation.Replace;

                case StencilOperation.IncrementAndClamp:
                    return WGPUStencilOperation.IncrementClamp;

                case StencilOperation.DecrementAndClamp:
                    return WGPUStencilOperation.DecrementClamp;

                case StencilOperation.Invert:
                    return WGPUStencilOperation.Invert;

                case StencilOperation.IncrementAndWrap:
                    return WGPUStencilOperation.IncrementWrap;

                case StencilOperation.DecrementAndWrap:
                    return WGPUStencilOperation.DecrementWrap;

                default:
                    throw Illegal.Value<StencilOperation>();
            }
        }

        public static WGPUVertexFormat VdToWGPUVertexFormat(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Float1:
                    return WGPUVertexFormat.Float32;

                case VertexElementFormat.Float2:
                    return WGPUVertexFormat.Float32x2;

                case VertexElementFormat.Float3:
                    return WGPUVertexFormat.Float32x3;

                case VertexElementFormat.Float4:
                    return WGPUVertexFormat.Float32x4;

                case VertexElementFormat.Byte2Norm:
                    return WGPUVertexFormat.Unorm8x2;

                case VertexElementFormat.Byte2:
                    return WGPUVertexFormat.Uint8x2;

                case VertexElementFormat.Byte4Norm:
                    return WGPUVertexFormat.Unorm8x4;

                case VertexElementFormat.Byte4:
                    return WGPUVertexFormat.Uint8x4;

                case VertexElementFormat.SByte2Norm:
                    return WGPUVertexFormat.Snorm8x2;

                case VertexElementFormat.SByte2:
                    return WGPUVertexFormat.Sint8x2;

                case VertexElementFormat.SByte4Norm:
                    return WGPUVertexFormat.Snorm8x4;

                case VertexElementFormat.SByte4:
                    return WGPUVertexFormat.Sint8x4;

                case VertexElementFormat.UShort2Norm:
                    return WGPUVertexFormat.Unorm16x2;

                case VertexElementFormat.UShort2:
                    return WGPUVertexFormat.Uint16x2;

                case VertexElementFormat.UShort4Norm:
                    return WGPUVertexFormat.Unorm16x4;

                case VertexElementFormat.UShort4:
                    return WGPUVertexFormat.Uint16x4;

                case VertexElementFormat.Short2Norm:
                    return WGPUVertexFormat.Snorm16x2;

                case VertexElementFormat.Short2:
                    return WGPUVertexFormat.Sint16x2;

                case VertexElementFormat.Short4Norm:
                    return WGPUVertexFormat.Snorm16x4;

                case VertexElementFormat.Short4:
                    return WGPUVertexFormat.Sint16x4;

                case VertexElementFormat.UInt1:
                    return WGPUVertexFormat.Uint32;

                case VertexElementFormat.UInt2:
                    return WGPUVertexFormat.Uint32x2;

                case VertexElementFormat.UInt3:
                    return WGPUVertexFormat.Uint32x3;

                case VertexElementFormat.UInt4:
                    return WGPUVertexFormat.Uint32x4;

                case VertexElementFormat.Int1:
                    return WGPUVertexFormat.Sint32;

                case VertexElementFormat.Int2:
                    return WGPUVertexFormat.Sint32x2;

                case VertexElementFormat.Int3:
                    return WGPUVertexFormat.Sint32x3;

                case VertexElementFormat.Int4:
                    return WGPUVertexFormat.Sint32x4;

                case VertexElementFormat.Half1:
                    throw Illegal.Value<VertexElementFormat>();

                case VertexElementFormat.Half2:
                    return WGPUVertexFormat.Float16x2;

                case VertexElementFormat.Half4:
                    return WGPUVertexFormat.Float16x4;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")] // Silk.NET issue.
        public static WGPUShaderStage VdToWGPUShaderStage(ShaderStages stage)
        {
            var ret = WGPUShaderStage.None;

            if ((stage & ShaderStages.Vertex) == ShaderStages.Vertex)
                ret |= WGPUShaderStage.Vertex;

            if ((stage & ShaderStages.Fragment) == ShaderStages.Fragment)
                ret |= WGPUShaderStage.Fragment;

            if ((stage & ShaderStages.Compute) == ShaderStages.Compute)
                ret |= WGPUShaderStage.Compute;

            return ret;
        }

        public static WGPUIndexFormat VdToWGPUIndexFormat(IndexFormat format)
        {
            switch (format)
            {
                case IndexFormat.UInt16:
                    return WGPUIndexFormat.Uint16;

                case IndexFormat.UInt32:
                    return WGPUIndexFormat.Uint32;

                default:
                    throw Illegal.Value<IndexFormat>();
            }
        }
    }
}

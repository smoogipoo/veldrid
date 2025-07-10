// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;

namespace Veldrid.SDL3
{
    internal static class SDL3Formats
    {
        public static SDL_GPUShaderFormat VdToSDLShaderFormat(GraphicsBackend backend)
        {
            return backend switch
            {
                GraphicsBackend.Direct3D11 => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXBC,
                GraphicsBackend.Vulkan => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                GraphicsBackend.Metal => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                _ => throw Illegal.Value<GraphicsBackend>()
            };
        }

        public static SDL_GPUSamplerAddressMode VdToSDLSamplerAddressMode(SamplerAddressMode mode)
        {
            return mode switch
            {
                SamplerAddressMode.Wrap => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
                SamplerAddressMode.Mirror => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
                SamplerAddressMode.Clamp => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
                _ => throw Illegal.Value<SamplerAddressMode>()
            };
        }

        public static void GetFilterParams(
            SamplerFilter filter,
            out SDL_GPUFilter minFilter,
            out SDL_GPUFilter magFilter,
            out SDL_GPUSamplerMipmapMode mipmapMode)
        {
            switch (filter)
            {
                case SamplerFilter.Anisotropic:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
                    break;

                case SamplerFilter.MinPointMagPointMipPoint:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
                    break;

                case SamplerFilter.MinPointMagPointMipLinear:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
                    break;

                case SamplerFilter.MinPointMagLinearMipPoint:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
                    break;

                case SamplerFilter.MinPointMagLinearMipLinear:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
                    break;

                case SamplerFilter.MinLinearMagPointMipPoint:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
                    break;

                case SamplerFilter.MinLinearMagPointMipLinear:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
                    break;

                case SamplerFilter.MinLinearMagLinearMipPoint:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
                    break;

                case SamplerFilter.MinLinearMagLinearMipLinear:
                    minFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    magFilter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
                    mipmapMode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR;
                    break;

                default:
                    throw Illegal.Value<SamplerFilter>();
            }
        }

        public static SDL_GPUCompareOp VdToSDLCompareOp(ComparisonKind comparisonKind)
        {
            return comparisonKind switch
            {
                ComparisonKind.Never => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER,
                ComparisonKind.Less => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS,
                ComparisonKind.Equal => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_EQUAL,
                ComparisonKind.LessEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
                ComparisonKind.Greater => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER,
                ComparisonKind.NotEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NOT_EQUAL,
                ComparisonKind.GreaterEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,
                ComparisonKind.Always => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_ALWAYS,
                _ => throw Illegal.Value<ComparisonKind>()
            };
        }

        public static SDL_GPUSampleCount VdToSDLSampleCount(TextureSampleCount sampleCount)
        {
            return sampleCount switch
            {
                TextureSampleCount.Count1 => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
                TextureSampleCount.Count2 => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_2,
                TextureSampleCount.Count4 => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_4,
                TextureSampleCount.Count8 => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_8,
                _ => throw Illegal.Value<TextureSampleCount>()
            };
        }

        public static SDL_GPUBufferUsageFlags VdToSDLBufferUsage(BufferUsage bufferUsage)
        {
            SDL_GPUBufferUsageFlags usage = default;

            if ((bufferUsage & BufferUsage.VertexBuffer) > 0)
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

            if ((bufferUsage & BufferUsage.IndexBuffer) > 0)
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

            if ((bufferUsage & BufferUsage.IndirectBuffer) > 0)
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDIRECT;

            if ((bufferUsage & (BufferUsage.UniformBuffer | BufferUsage.StructuredBufferReadOnly | BufferUsage.StructuredBufferReadWrite)) > 0)
            {
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
            }

            if ((bufferUsage & BufferUsage.StructuredBufferReadWrite) > 0)
                usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;

            return usage;
        }

        public static SDL_GPUShaderStage VdToSDLShaderStage(ShaderStages stage)
        {
            return stage switch
            {
                ShaderStages.Vertex => SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
                ShaderStages.Fragment => SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                _ => throw Illegal.Value<ShaderStages>()
            };
        }

        public static SDL_GPUTextureUsageFlags VdToSDLTextureUsage(TextureUsage usage)
        {
            SDL_GPUTextureUsageFlags flags = default;

            if ((usage & TextureUsage.Storage) > 0)
            {
                flags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ
                         | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ
                         | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE;
            }

            if ((usage & TextureUsage.Sampled) > 0)
                flags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

            if ((usage & TextureUsage.RenderTarget) > 0)
                flags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;

            if ((usage & TextureUsage.DepthStencil) > 0)
                flags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;

            return flags;
        }

        public static SDL_GPUTextureType VdToSDLTextureType(TextureType type, bool isCubeMap, bool isArray)
        {
            if (isCubeMap)
            {
                return isArray
                    ? SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE_ARRAY
                    : SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE;
            }

            switch (type)
            {
                case TextureType.Texture2D:
                    return isArray
                        ? SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D_ARRAY
                        : SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;

                case TextureType.Texture3D:
                    return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_3D;

                default:
                    throw Illegal.Value<TextureType>();
            }
        }

        public static SDL_GPUTextureFormat VdToSDLTextureFormat(PixelFormat format)
        {
            return format switch
            {
                PixelFormat.R8G8B8A8UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
                PixelFormat.B8G8R8A8UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM,
                PixelFormat.R8UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM,
                PixelFormat.R16UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UNORM,
                PixelFormat.R32G32B32A32Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT,
                PixelFormat.R32Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_FLOAT,
                PixelFormat.Bc3UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM,
                PixelFormat.D24UNormS8UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
                PixelFormat.D32FloatS8UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT,
                PixelFormat.R32G32B32A32UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_UINT,
                PixelFormat.R8G8SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_SNORM,
                PixelFormat.Bc1RgbaUNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM,
                PixelFormat.R10G10B10A2UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM,
                PixelFormat.R11G11B10Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT,
                PixelFormat.R8SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_SNORM,
                PixelFormat.R8UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UINT,
                PixelFormat.R8SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_INT,
                PixelFormat.R16SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_SNORM,
                PixelFormat.R16UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UINT,
                PixelFormat.R16SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_INT,
                PixelFormat.R16Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_FLOAT,
                PixelFormat.R32UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_UINT,
                PixelFormat.R32SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_INT,
                PixelFormat.R8G8UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM,
                PixelFormat.R8G8UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UINT,
                PixelFormat.R8G8SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_INT,
                PixelFormat.R16G16UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UNORM,
                PixelFormat.R16G16SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_SNORM,
                PixelFormat.R16G16UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UINT,
                PixelFormat.R16G16SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_INT,
                PixelFormat.R16G16Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT,
                PixelFormat.R32G32UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_UINT,
                PixelFormat.R32G32SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_INT,
                PixelFormat.R32G32Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT,
                PixelFormat.R8G8B8A8SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM,
                PixelFormat.R8G8B8A8UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT,
                PixelFormat.R8G8B8A8SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT,
                PixelFormat.R16G16B16A16UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM,
                PixelFormat.R16G16B16A16SNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM,
                PixelFormat.R16G16B16A16UInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT,
                PixelFormat.R16G16B16A16SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT,
                PixelFormat.R16G16B16A16Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,
                PixelFormat.R32G32B32A32SInt => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_INT,
                PixelFormat.Bc4UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM,
                PixelFormat.Bc5UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM,
                PixelFormat.Bc7UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,
                PixelFormat.R8G8B8A8UNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,
                PixelFormat.B8G8R8A8UNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB,
                PixelFormat.Bc1RgbaUNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB,
                PixelFormat.Bc2UNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB,
                PixelFormat.Bc3UNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB,
                PixelFormat.Bc7UNormSRgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB,
                PixelFormat.Bc2UNorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM,
                _ => throw Illegal.Value<PixelFormat>()
            };
        }

        public static PixelFormat SDLToVdTextureFormat(SDL_GPUTextureFormat format)
        {
            return format switch
            {
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM => PixelFormat.R8G8B8A8UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM => PixelFormat.B8G8R8A8UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM => PixelFormat.R8UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UNORM => PixelFormat.R16UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT => PixelFormat.R32G32B32A32Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_FLOAT => PixelFormat.R32Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM => PixelFormat.Bc3UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT => PixelFormat.D24UNormS8UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT => PixelFormat.D32FloatS8UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_UINT => PixelFormat.R32G32B32A32UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_SNORM => PixelFormat.R8G8SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM => PixelFormat.Bc1RgbaUNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM => PixelFormat.R10G10B10A2UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT => PixelFormat.R11G11B10Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_SNORM => PixelFormat.R8SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UINT => PixelFormat.R8UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_INT => PixelFormat.R8SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_SNORM => PixelFormat.R16SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UINT => PixelFormat.R16UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_INT => PixelFormat.R16SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_FLOAT => PixelFormat.R16Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_UINT => PixelFormat.R32UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_INT => PixelFormat.R32SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM => PixelFormat.R8G8UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UINT => PixelFormat.R8G8UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_INT => PixelFormat.R8G8SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UNORM => PixelFormat.R16G16UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_SNORM => PixelFormat.R16G16SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UINT => PixelFormat.R16G16UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_INT => PixelFormat.R16G16SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT => PixelFormat.R16G16Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_UINT => PixelFormat.R32G32UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_INT => PixelFormat.R32G32SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT => PixelFormat.R32G32Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM => PixelFormat.R8G8B8A8SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT => PixelFormat.R8G8B8A8UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT => PixelFormat.R8G8B8A8SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM => PixelFormat.R16G16B16A16UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM => PixelFormat.R16G16B16A16SNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT => PixelFormat.R16G16B16A16UInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT => PixelFormat.R16G16B16A16SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT => PixelFormat.R16G16B16A16Float,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_INT => PixelFormat.R32G32B32A32SInt,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM => PixelFormat.Bc4UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM => PixelFormat.Bc5UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM => PixelFormat.Bc7UNorm,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB => PixelFormat.R8G8B8A8UNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB => PixelFormat.B8G8R8A8UNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB => PixelFormat.Bc1RgbaUNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB => PixelFormat.Bc2UNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB => PixelFormat.Bc3UNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB => PixelFormat.Bc7UNormSRgb,
                SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM => PixelFormat.Bc2UNorm,
                _ => throw Illegal.Value<SDL_GPUTextureFormat>()
            };
        }

        public static SDL_GPUStencilOp VdToSDLStencilOp(StencilOperation operation)
        {
            return operation switch
            {
                StencilOperation.Keep => SDL_GPUStencilOp.SDL_GPU_STENCILOP_KEEP,
                StencilOperation.Zero => SDL_GPUStencilOp.SDL_GPU_STENCILOP_ZERO,
                StencilOperation.Replace => SDL_GPUStencilOp.SDL_GPU_STENCILOP_REPLACE,
                StencilOperation.IncrementAndClamp => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INCREMENT_AND_CLAMP,
                StencilOperation.DecrementAndClamp => SDL_GPUStencilOp.SDL_GPU_STENCILOP_DECREMENT_AND_CLAMP,
                StencilOperation.Invert => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INVERT,
                StencilOperation.IncrementAndWrap => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INCREMENT_AND_WRAP,
                StencilOperation.DecrementAndWrap => SDL_GPUStencilOp.SDL_GPU_STENCILOP_DECREMENT_AND_WRAP,
                _ => throw Illegal.Value<StencilOperation>()
            };
        }

        public static SDL_GPUPrimitiveType VdToSDLPrimitiveType(PrimitiveTopology topology)
        {
            return topology switch
            {
                PrimitiveTopology.TriangleList => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                PrimitiveTopology.TriangleStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                PrimitiveTopology.LineList => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                PrimitiveTopology.LineStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                PrimitiveTopology.PointList => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_POINTLIST,
                _ => throw Illegal.Value<PrimitiveTopology>()
            };
        }

        public static SDL_GPUVertexElementFormat VdToSDLVertexElementFormat(VertexElementFormat format)
        {
            return format switch
            {
                VertexElementFormat.Float1 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
                VertexElementFormat.Float2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                VertexElementFormat.Float3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                VertexElementFormat.Float4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
                VertexElementFormat.Byte2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2_NORM,
                VertexElementFormat.Byte2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2,
                VertexElementFormat.Byte4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
                VertexElementFormat.Byte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,
                VertexElementFormat.SByte2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE2_NORM,
                VertexElementFormat.SByte2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE2,
                VertexElementFormat.SByte4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4_NORM,
                VertexElementFormat.SByte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,
                VertexElementFormat.UShort2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2_NORM,
                VertexElementFormat.UShort2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
                VertexElementFormat.UShort4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4_NORM,
                VertexElementFormat.UShort4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,
                VertexElementFormat.Short2Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,
                VertexElementFormat.Short2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
                VertexElementFormat.Short4Norm => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,
                VertexElementFormat.Short4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,
                VertexElementFormat.UInt1 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT,
                VertexElementFormat.UInt2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT2,
                VertexElementFormat.UInt3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT3,
                VertexElementFormat.UInt4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UINT4,
                VertexElementFormat.Int1 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT,
                VertexElementFormat.Int2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT2,
                VertexElementFormat.Int3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT3,
                VertexElementFormat.Int4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_INT4,
                VertexElementFormat.Half2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_HALF2,
                VertexElementFormat.Half4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_HALF4,
                _ => throw Illegal.Value<VertexElementFormat>()
            };
        }

        public static SDL_GPUFillMode VdToSDLFillMode(PolygonFillMode fillMode)
        {
            return fillMode switch
            {
                PolygonFillMode.Solid => SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                PolygonFillMode.Wireframe => SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE,
                _ => throw Illegal.Value<PolygonFillMode>()
            };
        }

        public static SDL_GPUCullMode VdToSDLCullMode(FaceCullMode cullMode)
        {
            return cullMode switch
            {
                FaceCullMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
                FaceCullMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
                FaceCullMode.None => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                _ => throw Illegal.Value<FaceCullMode>()
            };
        }

        public static SDL_GPUFrontFace VdToSDLFrontFace(FrontFace frontFace)
        {
            return frontFace switch
            {
                FrontFace.Clockwise => SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
                FrontFace.CounterClockwise => SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE,
                _ => throw Illegal.Value<FrontFace>()
            };
        }

        public static SDL_GPUBlendFactor VdToSDLBlendFactor(BlendFactor factor)
        {
            return factor switch
            {
                BlendFactor.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                BlendFactor.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                BlendFactor.SourceAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                BlendFactor.InverseSourceAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                BlendFactor.DestinationAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                BlendFactor.InverseDestinationAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                BlendFactor.SourceColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                BlendFactor.InverseSourceColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                BlendFactor.DestinationColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                BlendFactor.InverseDestinationColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                BlendFactor.BlendFactor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_CONSTANT_COLOR,
                BlendFactor.InverseBlendFactor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_CONSTANT_COLOR,
                _ => throw Illegal.Value<BlendFactor>()
            };
        }

        public static SDL_GPUBlendOp VdToSDLBlendOp(BlendFunction function)
        {
            return function switch
            {
                BlendFunction.Add => SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                BlendFunction.Subtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_SUBTRACT,
                BlendFunction.ReverseSubtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
                BlendFunction.Minimum => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MIN,
                BlendFunction.Maximum => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MAX,
                _ => throw Illegal.Value<BlendFunction>()
            };
        }

        public static SDL_GPUColorComponentFlags VdToSDLColorComponentFlags(ColorWriteMask? mask)
        {
            if (mask == null)
            {
                return SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_R
                       | SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_G
                       | SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_B
                       | SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_A;
            }

            SDL_GPUColorComponentFlags flags = default;

            if ((mask & ColorWriteMask.Red) > 0)
                flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_R;

            if ((mask & ColorWriteMask.Green) > 0)
                flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_G;

            if ((mask & ColorWriteMask.Blue) > 0)
                flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_B;

            if ((mask & ColorWriteMask.Alpha) > 0)
                flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_A;

            return flags;
        }

        public static SDL_GPUIndexElementSize VdToSDLIndexElementSize(IndexFormat format)
        {
            return format switch
            {
                IndexFormat.UInt16 => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT,
                IndexFormat.UInt32 => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT,
                _ => throw Illegal.Value<IndexFormat>()
            };
        }
    }
}

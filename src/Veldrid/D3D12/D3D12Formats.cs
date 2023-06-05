using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrid.D3D12
{
    internal static class D3D12Formats
    {
        internal static Format ToDxgiFormat(PixelFormat format, bool depthFormat)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                    return Format.R8_UNorm;
                case PixelFormat.R8_SNorm:
                    return Format.R8_SNorm;
                case PixelFormat.R8_UInt:
                    return Format.R8_UInt;
                case PixelFormat.R8_SInt:
                    return Format.R8_SInt;

                case PixelFormat.R16_UNorm:
                    return depthFormat ? Format.D16_UNorm : Format.R16_UNorm;
                case PixelFormat.R16_SNorm:
                    return Format.R16_SNorm;
                case PixelFormat.R16_UInt:
                    return Format.R16_UInt;
                case PixelFormat.R16_SInt:
                    return Format.R16_SInt;
                case PixelFormat.R16_Float:
                    return Format.R16_Float;

                case PixelFormat.R32_UInt:
                    return Format.R32_UInt;
                case PixelFormat.R32_SInt:
                    return Format.R32_SInt;
                case PixelFormat.R32_Float:
                    return depthFormat ? Format.D32_Float : Format.R32_Float;

                case PixelFormat.R8_G8_UNorm:
                    return Format.R8G8_UNorm;
                case PixelFormat.R8_G8_SNorm:
                    return Format.R8G8_SNorm;
                case PixelFormat.R8_G8_UInt:
                    return Format.R8G8_UInt;
                case PixelFormat.R8_G8_SInt:
                    return Format.R8G8_SInt;

                case PixelFormat.R16_G16_UNorm:
                    return Format.R16G16_UNorm;
                case PixelFormat.R16_G16_SNorm:
                    return Format.R16G16_SNorm;
                case PixelFormat.R16_G16_UInt:
                    return Format.R16G16_UInt;
                case PixelFormat.R16_G16_SInt:
                    return Format.R16G16_SInt;
                case PixelFormat.R16_G16_Float:
                    return Format.R16G16_Float;

                case PixelFormat.R32_G32_UInt:
                    return Format.R32G32_UInt;
                case PixelFormat.R32_G32_SInt:
                    return Format.R32G32_SInt;
                case PixelFormat.R32_G32_Float:
                    return Format.R32G32_Float;

                case PixelFormat.R8_G8_B8_A8_UNorm:
                    return Format.R8G8B8A8_UNorm;
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                    return Format.R8G8B8A8_UNorm_SRgb;
                case PixelFormat.B8_G8_R8_A8_UNorm:
                    return Format.B8G8R8A8_UNorm;
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return Format.B8G8R8A8_UNorm_SRgb;
                case PixelFormat.R8_G8_B8_A8_SNorm:
                    return Format.R8G8B8A8_SNorm;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return Format.R8G8B8A8_UInt;
                case PixelFormat.R8_G8_B8_A8_SInt:
                    return Format.R8G8B8A8_SInt;

                case PixelFormat.R16_G16_B16_A16_UNorm:
                    return Format.R16G16B16A16_UNorm;
                case PixelFormat.R16_G16_B16_A16_SNorm:
                    return Format.R16G16B16A16_SNorm;
                case PixelFormat.R16_G16_B16_A16_UInt:
                    return Format.R16G16B16A16_UInt;
                case PixelFormat.R16_G16_B16_A16_SInt:
                    return Format.R16G16B16A16_SInt;
                case PixelFormat.R16_G16_B16_A16_Float:
                    return Format.R16G16B16A16_Float;

                case PixelFormat.R32_G32_B32_A32_UInt:
                    return Format.R32G32B32A32_UInt;
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return Format.R32G32B32A32_SInt;
                case PixelFormat.R32_G32_B32_A32_Float:
                    return Format.R32G32B32A32_Float;

                case PixelFormat.BC1_Rgb_UNorm:
                case PixelFormat.BC1_Rgba_UNorm:
                    return Format.BC1_UNorm;
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                    return Format.BC1_UNorm_SRgb;
                case PixelFormat.BC2_UNorm:
                    return Format.BC2_UNorm;
                case PixelFormat.BC2_UNorm_SRgb:
                    return Format.BC2_UNorm_SRgb;
                case PixelFormat.BC3_UNorm:
                    return Format.BC3_UNorm;
                case PixelFormat.BC3_UNorm_SRgb:
                    return Format.BC3_UNorm_SRgb;
                case PixelFormat.BC4_UNorm:
                    return Format.BC4_UNorm;
                case PixelFormat.BC4_SNorm:
                    return Format.BC4_SNorm;
                case PixelFormat.BC5_UNorm:
                    return Format.BC5_UNorm;
                case PixelFormat.BC5_SNorm:
                    return Format.BC5_SNorm;
                case PixelFormat.BC7_UNorm:
                    return Format.BC7_UNorm;
                case PixelFormat.BC7_UNorm_SRgb:
                    return Format.BC7_UNorm_SRgb;

                case PixelFormat.D24_UNorm_S8_UInt:
                    Debug.Assert(depthFormat);
                    return Format.R24G8_Typeless;
                case PixelFormat.D32_Float_S8_UInt:
                    Debug.Assert(depthFormat);
                    return Format.R32G8X24_Typeless;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                    return Format.R10G10B10A2_UNorm;
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return Format.R10G10B10A2_UInt;
                case PixelFormat.R11_G11_B10_Float:
                    return Format.R11G11B10_Float;

                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    throw new VeldridException("ETC2 formats are not supported on Direct3D 11.");

                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        internal static Format GetTypelessFormat(Format format)
        {
            switch (format)
            {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return Format.R32G32B32A32_Typeless;
                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return Format.R32G32B32_Typeless;
                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                    return Format.R16G16B16A16_Typeless;
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                    return Format.R32G32_Typeless;
                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                    return Format.R10G10B10A2_Typeless;
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                    return Format.R8G8B8A8_Typeless;
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                    return Format.R16G16_Typeless;
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                    return Format.R32_Typeless;
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                    return Format.R24G8_Typeless;
                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                    return Format.R8G8_Typeless;
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                    return Format.R16_Typeless;
                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                    return Format.R8_Typeless;
                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRgb:
                    return Format.BC1_Typeless;
                case Format.BC2_Typeless:
                case Format.BC2_UNorm:
                case Format.BC2_UNorm_SRgb:
                    return Format.BC2_Typeless;
                case Format.BC3_Typeless:
                case Format.BC3_UNorm:
                case Format.BC3_UNorm_SRgb:
                    return Format.BC3_Typeless;
                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return Format.BC4_Typeless;
                case Format.BC5_Typeless:
                case Format.BC5_UNorm:
                case Format.BC5_SNorm:
                    return Format.BC5_Typeless;
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8A8_UNorm_SRgb:
                    return Format.B8G8R8A8_Typeless;
                case Format.BC7_Typeless:
                case Format.BC7_UNorm:
                case Format.BC7_UNorm_SRgb:
                    return Format.BC7_Typeless;
                default:
                    return format;
            }
        }

        internal static TextureUsage GetVdUsage(ResourceFlags flags)
        {
            TextureUsage usage = 0;

            if ((flags & ResourceFlags.AllowRenderTarget) != 0)
            {
                usage |= TextureUsage.RenderTarget;
            }
            if ((flags & ResourceFlags.AllowDepthStencil) != 0)
            {
                usage |= TextureUsage.DepthStencil;
            }
            if ((flags & ResourceFlags.DenyShaderResource) == 0)
            {
                usage |= TextureUsage.Sampled;
            }
            if ((flags & ResourceFlags.AllowUnorderedAccess) != 0)
            {
                usage |= TextureUsage.Storage;
            }

            return usage;
        }

        internal static PixelFormat ToVdFormat(Format format)
        {
            switch (format)
            {
                case Format.R8_UNorm:
                    return PixelFormat.R8_UNorm;
                case Format.R8_SNorm:
                    return PixelFormat.R8_SNorm;
                case Format.R8_UInt:
                    return PixelFormat.R8_UInt;
                case Format.R8_SInt:
                    return PixelFormat.R8_SInt;

                case Format.R16_UNorm:
                case Format.D16_UNorm:
                    return PixelFormat.R16_UNorm;
                case Format.R16_SNorm:
                    return PixelFormat.R16_SNorm;
                case Format.R16_UInt:
                    return PixelFormat.R16_UInt;
                case Format.R16_SInt:
                    return PixelFormat.R16_SInt;
                case Format.R16_Float:
                    return PixelFormat.R16_Float;

                case Format.R32_UInt:
                    return PixelFormat.R32_UInt;
                case Format.R32_SInt:
                    return PixelFormat.R32_SInt;
                case Format.R32_Float:
                case Format.D32_Float:
                    return PixelFormat.R32_Float;

                case Format.R8G8_UNorm:
                    return PixelFormat.R8_G8_UNorm;
                case Format.R8G8_SNorm:
                    return PixelFormat.R8_G8_SNorm;
                case Format.R8G8_UInt:
                    return PixelFormat.R8_G8_UInt;
                case Format.R8G8_SInt:
                    return PixelFormat.R8_G8_SInt;

                case Format.R16G16_UNorm:
                    return PixelFormat.R16_G16_UNorm;
                case Format.R16G16_SNorm:
                    return PixelFormat.R16_G16_SNorm;
                case Format.R16G16_UInt:
                    return PixelFormat.R16_G16_UInt;
                case Format.R16G16_SInt:
                    return PixelFormat.R16_G16_SInt;
                case Format.R16G16_Float:
                    return PixelFormat.R16_G16_Float;

                case Format.R32G32_UInt:
                    return PixelFormat.R32_G32_UInt;
                case Format.R32G32_SInt:
                    return PixelFormat.R32_G32_SInt;
                case Format.R32G32_Float:
                    return PixelFormat.R32_G32_Float;

                case Format.R8G8B8A8_UNorm:
                    return PixelFormat.R8_G8_B8_A8_UNorm;
                case Format.R8G8B8A8_UNorm_SRgb:
                    return PixelFormat.R8_G8_B8_A8_UNorm_SRgb;

                case Format.B8G8R8A8_UNorm:
                    return PixelFormat.B8_G8_R8_A8_UNorm;
                case Format.B8G8R8A8_UNorm_SRgb:
                    return PixelFormat.B8_G8_R8_A8_UNorm_SRgb;
                case Format.R8G8B8A8_SNorm:
                    return PixelFormat.R8_G8_B8_A8_SNorm;
                case Format.R8G8B8A8_UInt:
                    return PixelFormat.R8_G8_B8_A8_UInt;
                case Format.R8G8B8A8_SInt:
                    return PixelFormat.R8_G8_B8_A8_SInt;

                case Format.R16G16B16A16_UNorm:
                    return PixelFormat.R16_G16_B16_A16_UNorm;
                case Format.R16G16B16A16_SNorm:
                    return PixelFormat.R16_G16_B16_A16_SNorm;
                case Format.R16G16B16A16_UInt:
                    return PixelFormat.R16_G16_B16_A16_UInt;
                case Format.R16G16B16A16_SInt:
                    return PixelFormat.R16_G16_B16_A16_SInt;
                case Format.R16G16B16A16_Float:
                    return PixelFormat.R16_G16_B16_A16_Float;

                case Format.R32G32B32A32_UInt:
                    return PixelFormat.R32_G32_B32_A32_UInt;
                case Format.R32G32B32A32_SInt:
                    return PixelFormat.R32_G32_B32_A32_SInt;
                case Format.R32G32B32A32_Float:
                    return PixelFormat.R32_G32_B32_A32_Float;

                case Format.BC1_UNorm:
                case Format.BC1_Typeless:
                    return PixelFormat.BC1_Rgba_UNorm;
                case Format.BC2_UNorm:
                    return PixelFormat.BC2_UNorm;
                case Format.BC3_UNorm:
                    return PixelFormat.BC3_UNorm;
                case Format.BC4_UNorm:
                    return PixelFormat.BC4_UNorm;
                case Format.BC4_SNorm:
                    return PixelFormat.BC4_SNorm;
                case Format.BC5_UNorm:
                    return PixelFormat.BC5_UNorm;
                case Format.BC5_SNorm:
                    return PixelFormat.BC5_SNorm;
                case Format.BC7_UNorm:
                    return PixelFormat.BC7_UNorm;

                case Format.D24_UNorm_S8_UInt:
                    return PixelFormat.D24_UNorm_S8_UInt;
                case Format.D32_Float_S8X24_UInt:
                    return PixelFormat.D32_Float_S8_UInt;

                case Format.R10G10B10A2_UInt:
                    return PixelFormat.R10_G10_B10_A2_UInt;
                case Format.R10G10B10A2_UNorm:
                    return PixelFormat.R10_G10_B10_A2_UNorm;
                case Format.R11G11B10_Float:
                    return PixelFormat.R11_G11_B10_Float;
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        internal static Format GetDepthFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R32_Float:
                    return Format.D32_Float;
                case PixelFormat.R16_UNorm:
                    return Format.D16_UNorm;
                case PixelFormat.D24_UNorm_S8_UInt:
                    return Format.D24_UNorm_S8_UInt;
                case PixelFormat.D32_Float_S8_UInt:
                    return Format.D32_Float_S8X24_UInt;
                default:
                    throw new VeldridException("Invalid depth texture format: " + format);
            }
        }
    }
}

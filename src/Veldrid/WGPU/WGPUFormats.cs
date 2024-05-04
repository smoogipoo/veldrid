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
    }
}

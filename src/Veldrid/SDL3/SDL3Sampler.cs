// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Sampler : Sampler
    {
        public override string Name { get; set; }

        public readonly SDL_GPUSampler* Sampler;

        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3Sampler(SDL3GraphicsDevice gd, ref SamplerDescription sd)
        {
            this.gd = gd;

            SDL3Formats.GetFilterParams(sd.Filter, out var minFilter, out var magFilter, out var mipmapMode);

            SDL_GPUSamplerCreateInfo ci = new SDL_GPUSamplerCreateInfo
            {
                min_filter = minFilter,
                mag_filter = magFilter,
                mipmap_mode = mipmapMode,
                address_mode_u = SDL3Formats.VdToSDLSamplerAddressMode(sd.AddressModeU),
                address_mode_v = SDL3Formats.VdToSDLSamplerAddressMode(sd.AddressModeV),
                address_mode_w = SDL3Formats.VdToSDLSamplerAddressMode(sd.AddressModeW),
                mip_lod_bias = sd.LodBias,
                max_anisotropy = sd.MaximumAnisotropy,
                compare_op = sd.ComparisonKind != null
                    ? SDL3Formats.VdToSDLCompareOp(sd.ComparisonKind.Value)
                    : SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER,
                min_lod = sd.MinimumLod,
                max_lod = sd.MaximumLod,
                enable_anisotropy = sd.Filter == SamplerFilter.Anisotropic,
                enable_compare = sd.ComparisonKind != null,
            };

            Sampler = SDL_CreateGPUSampler(gd.Device, &ci);
        }

        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Sampler != null)
                SDL_ReleaseGPUSampler(gd.Device, Sampler);

            isDisposed = true;
        }
    }
}

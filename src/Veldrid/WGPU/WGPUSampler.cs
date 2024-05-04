// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSampler : Sampler
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly Silk.NET.WebGPU.Sampler* Sampler;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUSampler(WGPUGraphicsDevice gd, ref SamplerDescription description)
        {
            this.gd = gd;

            WGPUFormats.GetFilterParams(description.Filter, out var minFilter, out var magFilter, out var mipmapFilter);

            Sampler = gd.WebGPU.DeviceCreateSampler(gd.NativeDevice, new SamplerDescriptor
            {
                NextInChain = null,
                Label = null,
                AddressModeU = WGPUFormats.VdToWGPUAddressMode(description.AddressModeU),
                AddressModeV = WGPUFormats.VdToWGPUAddressMode(description.AddressModeV),
                AddressModeW = WGPUFormats.VdToWGPUAddressMode(description.AddressModeW),
                MinFilter = minFilter,
                MagFilter = magFilter,
                MipmapFilter = mipmapFilter,
                LodMinClamp = description.MinimumLod,
                LodMaxClamp = description.MaximumLod,
                Compare = description.ComparisonKind != null
                    ? WGPUFormats.VdToWGPUCompareFunction(description.ComparisonKind.Value)
                    : CompareFunction.Never,
                MaxAnisotropy = (ushort)description.MaximumAnisotropy
            });
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Sampler != null)
                gd.WebGPU.SamplerRelease(Sampler);

            isDisposed = true;
        }
    }
}

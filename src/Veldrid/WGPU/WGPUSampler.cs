// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSampler : Sampler
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly WebGPU.WGPUSampler Sampler;

        private bool isDisposed;

        public WGPUSampler(WGPUGraphicsDevice gd, ref SamplerDescription description)
        {
            WGPUFormats.GetFilterParams(description.Filter, out var minFilter, out var magFilter, out var mipmapFilter);

            WGPUSamplerDescriptor desc = new WGPUSamplerDescriptor
            {
                addressModeU = WGPUFormats.VdToWGPUAddressMode(description.AddressModeU),
                addressModeV = WGPUFormats.VdToWGPUAddressMode(description.AddressModeV),
                addressModeW = WGPUFormats.VdToWGPUAddressMode(description.AddressModeW),
                minFilter = minFilter,
                magFilter = magFilter,
                mipmapFilter = mipmapFilter,
                lodMinClamp = description.MinimumLod,
                lodMaxClamp = description.MaximumLod,
                compare = description.ComparisonKind != null
                    ? WGPUFormats.VdToWGPUCompareFunction(description.ComparisonKind.Value)
                    : WGPUCompareFunction.Undefined,
                maxAnisotropy = (ushort)Math.Max(1, description.MaximumAnisotropy)
            };

            Sampler = wgpuDeviceCreateSampler(gd.NativeDevice, &desc);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Sampler.IsNotNull)
                wgpuSamplerRelease(Sampler);

            isDisposed = true;
        }
    }
}

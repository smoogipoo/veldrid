// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUTextureView : WGPUTextureViewBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public override WebGPU.WGPUTextureView View { get; }

        private bool isDisposed;

        public WGPUTextureView(WGPUGraphicsDevice gd, ref TextureViewDescription description)
            : base(ref description)
        {
            WGPUTexture wgpuTexture = Util.AssertSubtype<Texture, WGPUTexture>(description.Target);

            WGPUTextureViewDescriptor desc = new WGPUTextureViewDescriptor
            {
                format = WGPUFormats.VdToWGPUTextureFormat(Format, (Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil),
                dimension = WGPUFormats.VdToWGPUTextureViewDimention(Target.Depth),
                baseMipLevel = BaseMipLevel,
                mipLevelCount = MipLevels,
                baseArrayLayer = BaseArrayLayer,
                arrayLayerCount = ArrayLayers,
                aspect = WGPUTextureAspect.All
            };

            View = wgpuTextureCreateView(wgpuTexture.Texture, &desc);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (View.IsNotNull)
                wgpuTextureViewRelease(View);

            isDisposed = true;
        }
    }
}

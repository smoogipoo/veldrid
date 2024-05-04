// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUTextureView : TextureView
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly Silk.NET.WebGPU.TextureView* View;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUTextureView(WGPUGraphicsDevice gd, ref TextureViewDescription description)
            : this(gd, ref description, null)
        {
            WGPUTexture wgpuTexture = Util.AssertSubtype<Texture, WGPUTexture>(description.Target);

            View = gd.WebGPU.TextureCreateView(wgpuTexture.Texture, new TextureViewDescriptor
            {
                Format = WGPUFormats.VdToWGPUTextureFormat(Format, (Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil),
                Dimension = WGPUFormats.VdToWGPUTextureViewDimention(Target.Depth),
                BaseMipLevel = BaseMipLevel,
                MipLevelCount = MipLevels,
                BaseArrayLayer = BaseArrayLayer,
                ArrayLayerCount = ArrayLayers,
                Aspect = TextureAspect.All
            });
        }

        public WGPUTextureView(WGPUGraphicsDevice gd, ref TextureViewDescription description, Silk.NET.WebGPU.TextureView* view)
            : base(ref description)
        {
            this.gd = gd;

            View = view;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (View != null)
                gd.WebGPU.TextureViewRelease(View);

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUTexture : Texture
    {
        public override string Name { get; set; }
        public override PixelFormat Format { get; }
        public override uint Width { get; }
        public override uint Height { get; }
        public override uint Depth { get; }
        public override uint MipLevels { get; }
        public override uint ArrayLayers { get; }
        public override TextureUsage Usage { get; }
        public override TextureType Type { get; }
        public override TextureSampleCount SampleCount { get; }
        public override bool IsDisposed => isDisposed;

        public readonly WebGPU.WGPUTexture Texture;

        public readonly uint ActualArrayLayers;
        public readonly uint ActualSampleCount;
        public readonly WGPUTextureFormat ActualFormat;

        private bool isDisposed;

        public WGPUTexture(WGPUGraphicsDevice gd, ref TextureDescription description)
            : this(ref description, default)
        {
            WGPUTextureDescriptor desc = new WGPUTextureDescriptor
            {
                usage = WGPUFormats.VdToWGPUTextureUsage(Usage),
                dimension = WGPUFormats.VdToWGPUTextureDimention(Depth),
                size = new WGPUExtent3D(Width, Height, Depth * ActualArrayLayers),
                format = ActualFormat,
                mipLevelCount = MipLevels,
                sampleCount = ActualSampleCount
            };

            Texture = wgpuDeviceCreateTexture(gd.NativeDevice, &desc);
        }

        public WGPUTexture(ref TextureDescription description, WebGPU.WGPUTexture texture)
        {
            Texture = texture;

            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            MipLevels = description.MipLevels;
            ArrayLayers = description.ArrayLayers;
            Format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;

            ActualArrayLayers = (description.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap
                ? 6 * ArrayLayers
                : ArrayLayers;
            ActualSampleCount = WGPUFormats.VdToWGPUSampleCount(SampleCount);
            ActualFormat = WGPUFormats.VdToWGPUTextureFormat(Format, (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);
        }

        private protected override void DisposeCore()
        {
            if (isDisposed)
                return;

            if (Texture.IsNotNull)
                wgpuTextureRelease(Texture);

            isDisposed = true;
        }
    }
}

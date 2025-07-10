// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;

namespace Veldrid.SDL3
{
    public unsafe class SDL3ExternalTexture : SDL3TextureBase
    {
        public override PixelFormat Format => format;
        public override uint Width => width;
        public override uint Height => height;
        public override uint Depth => depth;
        public override uint MipLevels => mipLevels;
        public override uint ArrayLayers => arrayLayers;
        public override TextureUsage Usage => usage;
        public override TextureType Type => type;
        public override TextureSampleCount SampleCount => sampleCount;

        public override string Name { get; set; }

        public override SDL_GPUTexture* Texture => texture;

        private SDL_GPUTexture* texture;
        private PixelFormat format;
        private uint width;
        private uint height;
        private uint depth = 1;
        private uint mipLevels = 1;
        private uint arrayLayers = 1;
        private TextureUsage usage = TextureUsage.RenderTarget;
        private TextureType type = TextureType.Texture2D;
        private TextureSampleCount sampleCount;
        private bool isDisposed;

        public SDL3ExternalTexture()
        {
        }

        public SDL3ExternalTexture(PixelFormat format)
        {
            this.format = format;
        }

        public void SetNativeTexture(SDL_GPUTexture* texture, ref TextureDescription td)
        {
            this.texture = texture;

            format = td.Format;
            width = td.Width;
            height = td.Height;
            depth = td.Depth;
            mipLevels = td.MipLevels;
            arrayLayers = td.ArrayLayers;
            usage = td.Usage;
            type = td.Type;
            sampleCount = td.SampleCount;
        }

        public override bool IsDisposed => isDisposed;

        private protected override void DisposeCore()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

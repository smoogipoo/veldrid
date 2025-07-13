// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3SwapchainDepthTexture : SDL3TextureBase
    {
        public override PixelFormat Format { get; }
        public override uint Depth { get; }
        public override uint MipLevels { get; }
        public override uint ArrayLayers { get; }
        public override TextureUsage Usage { get; }
        public override TextureType Type { get; }
        public override TextureSampleCount SampleCount { get; }
        public override string Name { get; set; }

        private readonly SDL3GraphicsDevice gd;

        private SDL_GPUTexture* texture;
        private uint width;
        private uint height;
        private bool isDisposed;

        public SDL3SwapchainDepthTexture(SDL3GraphicsDevice gd, ref TextureDescription td)
        {
            this.gd = gd;

            Format = td.Format;
            Depth = td.Depth;
            MipLevels = td.MipLevels;
            ArrayLayers = td.ArrayLayers;
            Usage = td.Usage;
            Type = td.Type;
            SampleCount = td.SampleCount;

            Resize(td.Width, td.Height);
        }

        public override uint Width => width;

        public override uint Height => height;

        public override SDL_GPUTexture* Texture => texture;

        public override bool IsDisposed => isDisposed;

        public void Resize(uint width, uint height)
        {
            if (texture != null && this.width == width && this.height == height)
                return;

            this.width = width;
            this.height = height;

            if (texture != null)
                SDL.SDL3.SDL_ReleaseGPUTexture(gd.Device, texture);

            SDL_GPUTextureCreateInfo tci = new SDL_GPUTextureCreateInfo
            {
                type = SDL3Formats.VdToSDLTextureType(Type, (Usage & TextureUsage.Cubemap) > 0, ArrayLayers > 1),
                format = SDL3Formats.VdToSDLTextureFormat(Format, (Usage & TextureUsage.DepthStencil) > 0),
                usage = SDL3Formats.VdToSDLTextureUsage(Usage),
                width = width,
                height = height,
                layer_count_or_depth = ArrayLayers > 1 ? ArrayLayers : Depth,
                num_levels = MipLevels,
                sample_count = SDL3Formats.VdToSDLSampleCount(SampleCount)
            };

            texture = SDL.SDL3.SDL_CreateGPUTexture(gd.Device, &tci);
        }

        private protected override void DisposeCore()
        {
            if (isDisposed)
                return;

            if (texture != null)
                SDL.SDL3.SDL_ReleaseGPUTexture(gd.Device, texture);

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Texture : Texture
    {
        public override PixelFormat Format { get; }
        public override uint Width { get; }
        public override uint Height { get; }
        public override uint Depth { get; }
        public override uint MipLevels { get; }
        public override uint ArrayLayers { get; }
        public override TextureUsage Usage { get; }
        public override TextureType Type { get; }
        public override TextureSampleCount SampleCount { get; }
        public override string Name { get; set; }

        public readonly SDL_GPUTexture* Texture;
        public readonly SDL_GPUTransferBuffer* TransferBuffer;

        private readonly SDL3GraphicsDevice gd;

        private bool isDisposed;

        public SDL3Texture(SDL3GraphicsDevice gd, ref TextureDescription td)
        {
            this.gd = gd;

            Format = td.Format;
            Width = td.Width;
            Height = td.Height;
            Depth = td.Depth;
            MipLevels = td.MipLevels;
            ArrayLayers = td.ArrayLayers;
            Usage = td.Usage;
            Type = td.Type;
            SampleCount = td.SampleCount;

            // CPU buffer if staging
            if ((td.Usage & TextureUsage.Staging) == 0)
            {
                SDL_GPUTransferBufferCreateInfo uploadInfo = new SDL_GPUTransferBufferCreateInfo
                {
                    usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                    size = SDL_CalculateGPUTextureFormatSize(SDL3Formats.VdToSDLTextureFormat(td.Format), td.Width, td.Height, td.Depth)
                };

                TransferBuffer = SDL_CreateGPUTransferBuffer(gd.Device, &uploadInfo);
            }
            // GPU texture if NOT staging
            else
            {
                SDL_GPUTextureCreateInfo tci = new SDL_GPUTextureCreateInfo
                {
                    type = SDL3Formats.VdToSDLTextureType(td.Type, (td.Usage & TextureUsage.Cubemap) > 0, td.ArrayLayers > 0),
                    format = SDL3Formats.VdToSDLTextureFormat(td.Format),
                    usage = SDL3Formats.VdToSDLTextureUsage(td.Usage),
                    width = td.Width,
                    height = td.Height,
                    layer_count_or_depth = td.ArrayLayers + td.Depth,
                    num_levels = td.MipLevels,
                    sample_count = SDL3Formats.VdToSDLSampleCount(td.SampleCount)
                };

                Texture = SDL_CreateGPUTexture(gd.Device, &tci);
            }
        }

        public void GetSubresourceLayout(uint subresource, out uint sizeInBytes, out uint offset, out uint rowPitch, out uint depthPitch)
        {
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out uint arrayLayer);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
            offset = (uint)Util.ComputeSubresourceOffset(this, mipLevel, arrayLayer);
            rowPitch = FormatHelpers.GetRowPitch(mipWidth, Format);
            depthPitch = FormatHelpers.GetDepthPitch(rowPitch, mipHeight, Format);
            sizeInBytes = depthPitch;
        }

        public override bool IsDisposed => isDisposed;

        private protected override void DisposeCore()
        {
            if (isDisposed)
                return;

            if (Texture != null)
                SDL_ReleaseGPUTexture(gd.Device, Texture);

            if (TransferBuffer != null)
                SDL_ReleaseGPUTransferBuffer(gd.Device, TransferBuffer);

            isDisposed = true;
        }
    }
}

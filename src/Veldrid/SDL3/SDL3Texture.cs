// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    internal abstract unsafe class SDL3TextureBase : Texture
    {
        public abstract SDL_GPUTexture* Texture { get; }
    }

    internal unsafe class SDL3Texture : SDL3TextureBase
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
        public override SDL_GPUTexture* Texture { get; }

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
            if ((td.Usage & TextureUsage.Staging) > 0)
            {
                SDL_GPUTransferBufferCreateInfo uploadInfo = new SDL_GPUTransferBufferCreateInfo
                {
                    usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                    size = SDL_CalculateGPUTextureFormatSize(SDL3Formats.VdToSDLTextureFormat(td.Format, false), td.Width, td.Height, td.Depth)
                };

                TransferBuffer = SDL_CreateGPUTransferBuffer(gd.Device, &uploadInfo);
            }
            // GPU texture if NOT staging
            else
            {
                SDL_GPUTextureCreateInfo tci = new SDL_GPUTextureCreateInfo
                {
                    type = SDL3Formats.VdToSDLTextureType(td.Type, (td.Usage & TextureUsage.Cubemap) > 0, td.ArrayLayers > 1),
                    format = SDL3Formats.VdToSDLTextureFormat(td.Format, (td.Usage & TextureUsage.DepthStencil) > 0),
                    usage = SDL3Formats.VdToSDLTextureUsage(td.Usage),
                    width = td.Width,
                    height = td.Height,
                    layer_count_or_depth = td.ArrayLayers > 1 ? td.ArrayLayers : td.Depth,
                    num_levels = td.MipLevels,
                    sample_count = SDL3Formats.VdToSDLSampleCount(td.SampleCount)
                };

                Texture = SDL_CreateGPUTexture(gd.Device, &tci);
            }
        }

        internal void GetSubresourceLayout(uint mipLevel, out uint rowPitch, out uint depthPitch)
        {
            uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
            uint storageWidth = Math.Max(blockSize, mipWidth);
            uint storageHeight = Math.Max(blockSize, mipHeight);
            rowPitch = FormatHelpers.GetRowPitch(storageWidth, Format);
            depthPitch = FormatHelpers.GetDepthPitch(rowPitch, storageHeight, Format);
        }

        internal uint GetSubresourceSize(uint mipLevel)
        {
            uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
            Util.GetMipDimensions(this, mipLevel, out uint width, out uint height, out uint depth);
            uint storageWidth = Math.Max(blockSize, width);
            uint storageHeight = Math.Max(blockSize, height);
            return depth * FormatHelpers.GetDepthPitch(
                FormatHelpers.GetRowPitch(storageWidth, Format),
                storageHeight,
                Format);
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

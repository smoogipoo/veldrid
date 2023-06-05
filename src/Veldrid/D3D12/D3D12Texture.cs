// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal class D3D12Texture : Texture
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
        public override bool IsDisposed { get; }

        public ID3D12Resource DeviceResource { get; }
        public Vortice.DXGI.Format DxgiFormat { get; }

        public D3D12Texture(ID3D12Device device, ref TextureDescription description)
        {
            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            MipLevels = description.MipLevels;
            ArrayLayers = description.ArrayLayers;
            Format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;

            DxgiFormat = D3D12Formats.ToDxgiFormat(
                description.Format,
                (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);

            HeapType heapType = HeapType.Default;
            ResourceFlags resourceFlags = ResourceFlags.None;
            ResourceStates initialResourceState = ResourceStates.Common;
            ClearValue optimizedClearValue = new ClearValue(DxgiFormat, 1.0f);
            ResourceDescription resourceDescription;

            if ((description.Usage & TextureUsage.Staging) == TextureUsage.Staging)
            {
                heapType = HeapType.Upload;
                initialResourceState |= ResourceStates.GenericRead;
            }

            if ((description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
            {
                initialResourceState |= ResourceStates.DepthWrite;
                resourceFlags |= ResourceFlags.AllowDepthStencil;
            }

            if ((description.Usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            {
                initialResourceState |= ResourceStates.RenderTarget;
                resourceFlags |= ResourceFlags.AllowRenderTarget;
            }

            // if ((description.Usage & TextureUsage.Sampled) != TextureUsage.Sampled)
            // {
            //     resourceFlags |= ResourceFlags.DenyShaderResource;
            // }

            if ((description.Usage & TextureUsage.Storage) == TextureUsage.Storage)
            {
                resourceFlags |= ResourceFlags.AllowUnorderedAccess;
            }

            ushort arraySize = (ushort)description.ArrayLayers;
            uint roundedWidth = description.Width;
            uint roundedHeight = description.Height;
            ushort mipLevels = (ushort)description.MipLevels;

            if (FormatHelpers.IsCompressedFormat(description.Format))
            {
                roundedWidth = ((roundedWidth + 3) / 4) * 4;
                roundedHeight = ((roundedHeight + 3) / 4) * 4;
            }

            if (Type == TextureType.Texture1D)
            {
                resourceDescription = ResourceDescription.Texture1D(
                    DxgiFormat,
                    roundedWidth,
                    arraySize,
                    mipLevels,
                    resourceFlags);
            }
            else if (Type == TextureType.Texture2D)
            {
                resourceDescription = ResourceDescription.Texture2D(
                    DxgiFormat,
                    roundedWidth,
                    roundedHeight,
                    arraySize,
                    mipLevels,
                    (int)FormatHelpers.GetSampleCountUInt32(SampleCount),
                    0,
                    resourceFlags);
            }
            else
            {
                Debug.Assert(Type == TextureType.Texture3D);

                resourceDescription = ResourceDescription.Texture3D(
                    DxgiFormat,
                    roundedWidth,
                    roundedHeight,
                    description.Depth,
                    mipLevels,
                    resourceFlags);
            }

            DeviceResource = device.CreateCommittedResource(
                heapType,
                resourceDescription,
                initialResourceState,
                optimizedClearValue);
        }

        public D3D12Texture(ID3D12Resource existingTexture, TextureType type, PixelFormat format)
        {
            DeviceResource = existingTexture;
            Width = (uint)existingTexture.Description.Width;
            Height = (uint)existingTexture.Description.Height;
            Depth = 1;
            MipLevels = existingTexture.Description.MipLevels;
            ArrayLayers = (uint)existingTexture.Description.ArraySize;
            Format = format;
            SampleCount = FormatHelpers.GetSampleCount((uint)existingTexture.Description.SampleDescription.Count);
            Type = type;
            Usage = D3D12Formats.GetVdUsage(existingTexture.Description.Flags);

            DxgiFormat = D3D12Formats.ToDxgiFormat(
                format,
                (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);
        }

        private protected override void DisposeCore()
        {
        }
    }
}

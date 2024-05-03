using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Veldrid.D3D11
{
    internal class D3D11Texture : Texture
    {
        public override uint Width { get; }
        public override uint Height { get; }
        public override uint Depth { get; }
        public override uint MipLevels { get; }
        public override uint ArrayLayers { get; }
        public override PixelFormat Format { get; }
        public override TextureUsage Usage { get; }
        public override TextureType Type { get; }
        public override TextureSampleCount SampleCount { get; }
        public override bool IsDisposed => isDisposed;

        public ComPtr<ID3D11Resource> DeviceTexture { get; }

        public Format DxgiFormat { get; }
        public Format TypelessDxgiFormat { get; }

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // DeviceTexture.DebugName = value;
            }
        }

        private string name;
        private bool isDisposed;

        public unsafe D3D11Texture(ComPtr<ID3D11Device> device, ref TextureDescription description)
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

            DxgiFormat = D3D11Formats.ToDxgiFormat(
                description.Format,
                (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);
            TypelessDxgiFormat = D3D11Formats.GetTypelessFormat(DxgiFormat);

            var cpuFlags = CpuAccessFlag.None;
            var resourceUsage = Silk.NET.Direct3D11.Usage.Default;
            var bindFlags = BindFlag.None;
            var optionFlags = ResourceMiscFlag.None;

            if ((description.Usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) bindFlags |= BindFlag.RenderTarget;

            if ((description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) bindFlags |= BindFlag.DepthStencil;

            if ((description.Usage & TextureUsage.Sampled) == TextureUsage.Sampled) bindFlags |= BindFlag.ShaderResource;

            if ((description.Usage & TextureUsage.Storage) == TextureUsage.Storage) bindFlags |= BindFlag.UnorderedAccess;

            if ((description.Usage & TextureUsage.Staging) == TextureUsage.Staging)
            {
                cpuFlags = CpuAccessFlag.Read | CpuAccessFlag.Write;
                resourceUsage = Silk.NET.Direct3D11.Usage.Staging;
            }

            if ((description.Usage & TextureUsage.GenerateMipmaps) != 0)
            {
                bindFlags |= BindFlag.RenderTarget | BindFlag.ShaderResource;
                optionFlags |= ResourceMiscFlag.GenerateMips;
            }

            uint arraySize = description.ArrayLayers;

            if ((description.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
            {
                optionFlags |= ResourceMiscFlag.Texturecube;
                arraySize *= 6;
            }

            uint roundedWidth = description.Width;
            uint roundedHeight = description.Height;

            if (FormatHelpers.IsCompressedFormat(description.Format))
            {
                roundedWidth = (roundedWidth + 3) / 4 * 4;
                roundedHeight = (roundedHeight + 3) / 4 * 4;
            }

            if (Type == TextureType.Texture1D)
            {
                ComPtr<ID3D11Texture1D> comTexture = null;

                SilkMarshal.ThrowHResult(device.CreateTexture1D(new Texture1DDesc
                {
                    Width = roundedWidth,
                    MipLevels = description.MipLevels,
                    ArraySize = arraySize,
                    Format = TypelessDxgiFormat,
                    BindFlags = (uint)bindFlags,
                    CPUAccessFlags = (uint)cpuFlags,
                    Usage = resourceUsage,
                    MiscFlags = (uint)optionFlags
                }, Unsafe.NullRef<SubresourceData>(), ref comTexture));

                DeviceTexture = D3D11Util.ComCast<ID3D11Texture1D, ID3D11Resource>(comTexture);
            }
            else if (Type == TextureType.Texture2D)
            {
                ComPtr<ID3D11Texture2D> comTexture = null;

                SilkMarshal.ThrowHResult(device.CreateTexture2D(new Texture2DDesc
                {
                    Width = roundedWidth,
                    Height = roundedHeight,
                    MipLevels = description.MipLevels,
                    ArraySize = arraySize,
                    Format = TypelessDxgiFormat,
                    BindFlags = (uint)bindFlags,
                    CPUAccessFlags = (uint)cpuFlags,
                    Usage = resourceUsage,
                    SampleDesc = new SampleDesc(FormatHelpers.GetSampleCountUInt32(SampleCount), 0),
                    MiscFlags = (uint)optionFlags
                }, Unsafe.NullRef<SubresourceData>(), ref comTexture));

                DeviceTexture = D3D11Util.ComCast<ID3D11Texture2D, ID3D11Resource>(comTexture);
            }
            else
            {
                Debug.Assert(Type == TextureType.Texture3D);

                ComPtr<ID3D11Texture3D> comTexture = null;

                SilkMarshal.ThrowHResult(device.CreateTexture3D(new Texture3DDesc
                {
                    Width = roundedWidth,
                    Height = roundedHeight,
                    Depth = description.Depth,
                    MipLevels = description.MipLevels,
                    Format = TypelessDxgiFormat,
                    BindFlags = (uint)bindFlags,
                    CPUAccessFlags = (uint)cpuFlags,
                    Usage = resourceUsage,
                    MiscFlags = (uint)optionFlags
                }, Unsafe.NullRef<SubresourceData>(), ref comTexture));

                DeviceTexture = D3D11Util.ComCast<ID3D11Texture3D, ID3D11Resource>(comTexture);
            }
        }

        public unsafe D3D11Texture(ComPtr<ID3D11Texture2D> existingTexture, TextureType type, PixelFormat format)
        {
            DeviceTexture = new ComPtr<ID3D11Resource>((ID3D11Resource*)existingTexture.Handle);

            Texture2DDesc existingDescription = default;
            existingTexture.GetDesc(ref existingDescription);

            Width = existingDescription.Width;
            Height = existingDescription.Height;
            Depth = 1;
            MipLevels = existingDescription.MipLevels;
            ArrayLayers = existingDescription.ArraySize;
            Format = format;
            SampleCount = FormatHelpers.GetSampleCount(existingDescription.SampleDesc.Count);
            Type = type;
            Usage = D3D11Formats.GetVdUsage(
                (BindFlag)existingDescription.BindFlags,
                (CpuAccessFlag)existingDescription.CPUAccessFlags,
                (ResourceMiscFlag)existingDescription.MiscFlags);

            DxgiFormat = D3D11Formats.ToDxgiFormat(
                format,
                (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);
            TypelessDxgiFormat = D3D11Formats.GetTypelessFormat(DxgiFormat);
        }

        private protected override TextureView CreateFullTextureView(GraphicsDevice gd)
        {
            var desc = new TextureViewDescription(this);
            var d3d11Gd = Util.AssertSubtype<GraphicsDevice, D3D11GraphicsDevice>(gd);
            return new D3D11TextureView(d3d11Gd, ref desc);
        }

        private protected override void DisposeCore()
        {
            if (isDisposed)
                return;

            DeviceTexture.Dispose();

            isDisposed = true;
        }
    }
}

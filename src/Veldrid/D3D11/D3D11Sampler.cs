using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11Sampler : Sampler
    {
        public ComPtr<ID3D11SamplerState> DeviceSampler => deviceSampler;

        public override bool IsDisposed => isDisposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // DeviceSampler.DebugName = value;
            }
        }

        private readonly ComPtr<ID3D11SamplerState> deviceSampler;

        private string name;
        private bool isDisposed;

        public unsafe D3D11Sampler(ComPtr<ID3D11Device> device, ref SamplerDescription description)
        {
            RgbaFloat rgbaColor = description.BorderColor switch
            {
                SamplerBorderColor.TransparentBlack => new RgbaFloat(0, 0, 0, 0),
                SamplerBorderColor.OpaqueBlack => new RgbaFloat(0, 0, 0, 1),
                SamplerBorderColor.OpaqueWhite => new RgbaFloat(1, 1, 1, 1),
                _ => throw Illegal.Value<SamplerBorderColor>()
            };

            SilkMarshal.ThrowHResult(device.CreateSamplerState(new SamplerDesc
            {
                AddressU = D3D11Formats.VdToD3D11AddressMode(description.AddressModeU),
                AddressV = D3D11Formats.VdToD3D11AddressMode(description.AddressModeV),
                AddressW = D3D11Formats.VdToD3D11AddressMode(description.AddressModeW),
                Filter = D3D11Formats.ToD3D11Filter(description.Filter, description.ComparisonKind.HasValue),
                MinLOD = description.MinimumLod,
                MaxLOD = description.MaximumLod,
                MaxAnisotropy = description.MaximumAnisotropy,
                ComparisonFunc = description.ComparisonKind == null
                    ? ComparisonFunc.Never
                    : D3D11Formats.VdToD3D11ComparisonFunc(description.ComparisonKind.Value),
                MipLODBias = description.LodBias,
                BorderColor = (float*)&rgbaColor
            }, ref deviceSampler));
        }

        #region Disposal

        public override void Dispose()
        {
            if (isDisposed)
                return;

            DeviceSampler.Release();

            isDisposed = true;
        }

        #endregion
    }
}

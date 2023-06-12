using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Veldrid.D3D12
{
    internal class D3D12Sampler : Sampler
    {
        private string _name;

        public ID3D12DescriptorHeap DescriptorHeap { get; }

        public D3D12Sampler(ID3D12Device device, ref SamplerDescription description)
        {
            DescriptorHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.Sampler, 1));
            ComparisonFunction comparision = description.ComparisonKind == null ? ComparisonFunction.Never : D3D12Formats.VdToD3D12ComparisonFunc(description.ComparisonKind.Value);
            Vortice.Direct3D12.SamplerDescription samplerStateDesc = new Vortice.Direct3D12.SamplerDescription
            {
                AddressU = D3D12Formats.VdToD3D12AddressMode(description.AddressModeU),
                AddressV = D3D12Formats.VdToD3D12AddressMode(description.AddressModeV),
                AddressW = D3D12Formats.VdToD3D12AddressMode(description.AddressModeW),
                Filter = D3D12Formats.ToD3D12Filter(description.Filter, description.ComparisonKind.HasValue),
                MinLOD = description.MinimumLod,
                MaxLOD = description.MaximumLod,
                MaxAnisotropy = (int)description.MaximumAnisotropy,
                ComparisonFunction = comparision,
                MipLODBias = description.LodBias,
                BorderColor = ToRawColor4(description.BorderColor)
            };

            device.CreateSampler(ref samplerStateDesc, DescriptorHeap.GetCPUDescriptorHandleForHeapStart());
        }

        private static Color4 ToRawColor4(SamplerBorderColor borderColor)
        {
            switch (borderColor)
            {
                case SamplerBorderColor.TransparentBlack:
                    return new Color4(0, 0, 0, 0);

                case SamplerBorderColor.OpaqueBlack:
                    return new Color4(0, 0, 0, 1);

                case SamplerBorderColor.OpaqueWhite:
                    return new Color4(1, 1, 1, 1);

                default:
                    throw Illegal.Value<SamplerBorderColor>();
            }
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                DescriptorHeap.Name = value;
            }
        }

        public override bool IsDisposed { get; }

        public override void Dispose()
        {
        }
    }
}

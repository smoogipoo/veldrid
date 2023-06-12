using System;
using System.Collections.Generic;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrid.D3D12
{
    internal class D3D12Buffer : DeviceBuffer
    {
        private readonly ID3D12Device _device;
        private readonly object _accessViewLock = new object();

        private readonly Dictionary<OffsetSizePair, ID3D12DescriptorHeap> _srvs
            = new Dictionary<OffsetSizePair, ID3D12DescriptorHeap>();

        private readonly Dictionary<OffsetSizePair, ID3D12DescriptorHeap> _uavs
            = new Dictionary<OffsetSizePair, ID3D12DescriptorHeap>();

        private readonly uint _structureByteStride;
        private readonly bool _rawBuffer;
        private string _name;

        public override uint SizeInBytes { get; }

        public override BufferUsage Usage { get; }

        public override bool IsDisposed { get; }

        public ID3D12Resource DeviceResource { get; }

        public D3D12Buffer(ID3D12Device device, uint sizeInBytes, BufferUsage usage, uint structureByteStride, bool rawBuffer)
        {
            _device = device;
            SizeInBytes = sizeInBytes;
            Usage = usage;
            _structureByteStride = structureByteStride;
            _rawBuffer = rawBuffer;

            ResourceDescription bd = ResourceDescription.Buffer((int)sizeInBytes);
            HeapType heapType = HeapType.Default;
            ResourceStates initialState = ResourceStates.Common;

            if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer
                || (usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
            {
                initialState |= ResourceStates.VertexAndConstantBuffer;
            }

            if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
            {
                initialState |= ResourceStates.IndexBuffer;
            }

            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
            {
                initialState |= ResourceStates.IndirectArgument;
            }

            if ((usage & BufferUsage.Staging) == BufferUsage.Staging)
            {
                heapType = HeapType.Upload;
                initialState |= ResourceStates.GenericRead;
            }

            DeviceResource = device.CreateCommittedResource(
                heapType,
                bd,
                initialState);
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                DeviceResource.Name = value;

                foreach (KeyValuePair<OffsetSizePair, ID3D12DescriptorHeap> kvp in _srvs)
                {
                    kvp.Value.Name = value + "_SRV";
                }

                foreach (KeyValuePair<OffsetSizePair, ID3D12DescriptorHeap> kvp in _uavs)
                {
                    kvp.Value.Name = value + "_UAV";
                }
            }
        }

        public override void Dispose()
        {
        }

        internal ID3D12DescriptorHeap GetShaderResourceView(uint offset, uint size)
        {
            lock (_accessViewLock)
            {
                OffsetSizePair pair = new OffsetSizePair(offset, size);

                if (!_srvs.TryGetValue(pair, out ID3D12DescriptorHeap srv))
                {
                    srv = CreateShaderResourceView(offset, size);
                    _srvs.Add(pair, srv);
                }

                return srv;
            }
        }

        internal ID3D12DescriptorHeap GetUnorderedAccessView(uint offset, uint size)
        {
            lock (_accessViewLock)
            {
                OffsetSizePair pair = new OffsetSizePair(offset, size);

                if (!_uavs.TryGetValue(pair, out ID3D12DescriptorHeap uav))
                {
                    uav = CreateUnorderedAccessView(offset, size);
                    _uavs.Add(pair, uav);
                }

                return uav;
            }
        }

        private ID3D12DescriptorHeap CreateShaderResourceView(uint offset, uint size)
        {
            ID3D12DescriptorHeap heap = _device.CreateDescriptorHeap(
                new DescriptorHeapDescription(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                    1,
                    DescriptorHeapFlags.ShaderVisible));

            if (_rawBuffer)
            {
                ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
                {
                    Buffer = new BufferShaderResourceView
                    {
                        FirstElement = (ulong)offset / 4,
                        NumElements = (int)size / 4,
                        StructureByteStride = 4
                    },
                    Format = Format.R32_Typeless,
                    ViewDimension = ShaderResourceViewDimension.Buffer,
                };

                _device.CreateShaderResourceView(DeviceResource, srvDesc, heap.GetCPUDescriptorHandleForHeapStart());
            }
            else
            {
                ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
                {
                    Buffer = new BufferShaderResourceView
                    {
                        FirstElement = (ulong)offset / _structureByteStride,
                        NumElements = (int)(size / _structureByteStride),
                        StructureByteStride = (int)_structureByteStride
                    },
                    ViewDimension = ShaderResourceViewDimension.Buffer,
                };

                _device.CreateShaderResourceView(DeviceResource, srvDesc, heap.GetCPUDescriptorHandleForHeapStart());
            }

            return heap;
        }

        private ID3D12DescriptorHeap CreateUnorderedAccessView(uint offset, uint size)
        {
            ID3D12DescriptorHeap heap = _device.CreateDescriptorHeap(
                new DescriptorHeapDescription(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                    1,
                    DescriptorHeapFlags.ShaderVisible));

            if (_rawBuffer)
            {
                UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
                {
                    Buffer = new BufferUnorderedAccessView
                    {
                        FirstElement = (ulong)offset / 4,
                        NumElements = (int)size / 4,
                        StructureByteStride = 4
                    },
                    Format = Format.R32_Typeless,
                    ViewDimension = UnorderedAccessViewDimension.Buffer,
                };

                _device.CreateUnorderedAccessView(DeviceResource, null, uavDesc, heap.GetCPUDescriptorHandleForHeapStart());
            }
            else
            {
                UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
                {
                    Buffer = new BufferUnorderedAccessView
                    {
                        FirstElement = (ulong)offset / _structureByteStride,
                        NumElements = (int)(size / _structureByteStride),
                        StructureByteStride = (int)_structureByteStride
                    },
                    ViewDimension = UnorderedAccessViewDimension.Buffer,
                };

                _device.CreateUnorderedAccessView(DeviceResource, null, uavDesc, heap.GetCPUDescriptorHandleForHeapStart());
            }

            return heap;
        }

        private struct OffsetSizePair : IEquatable<OffsetSizePair>
        {
            public readonly uint Offset;
            public readonly uint Size;

            public OffsetSizePair(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public bool Equals(OffsetSizePair other) => Offset.Equals(other.Offset) && Size.Equals(other.Size);
            public override int GetHashCode() => HashHelper.Combine(Offset.GetHashCode(), Size.GetHashCode());
        }
    }
}

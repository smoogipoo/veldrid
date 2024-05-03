using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Veldrid.D3D11
{
    internal class D3D11Buffer : DeviceBuffer
    {
        public override uint SizeInBytes { get; }

        public override BufferUsage Usage { get; }

        public override bool IsDisposed => isDisposed;

        // ReSharper disable once InconsistentlySynchronizedField
        public ComPtr<ID3D11Buffer> Buffer => buffer;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // Buffer.DebugName = value;
                // foreach (var kvp in srvs) kvp.Value.DebugName = value + "_SRV";
                //
                // foreach (var kvp in uavs) kvp.Value.DebugName = value + "_UAV";
            }
        }

        private readonly ComPtr<ID3D11Device> device;
        private readonly ComPtr<ID3D11Buffer> buffer;
        private readonly object accessViewLock = new object();

        private readonly Dictionary<OffsetSizePair, ComPtr<ID3D11ShaderResourceView>> srvs = new Dictionary<OffsetSizePair, ComPtr<ID3D11ShaderResourceView>>();
        private readonly Dictionary<OffsetSizePair, ComPtr<ID3D11UnorderedAccessView>> uavs = new Dictionary<OffsetSizePair, ComPtr<ID3D11UnorderedAccessView>>();

        private readonly uint structureByteStride;
        private readonly bool rawBuffer;

        private string name;
        private bool isDisposed;

        public D3D11Buffer(ComPtr<ID3D11Device> device, uint sizeInBytes, BufferUsage usage, uint structureByteStride, bool rawBuffer)
        {
            this.device = device;
            this.structureByteStride = structureByteStride;
            this.rawBuffer = rawBuffer;

            SizeInBytes = sizeInBytes;
            Usage = usage;

            var bd = new BufferDesc
            {
                ByteWidth = sizeInBytes,
                BindFlags = (uint)D3D11Formats.VdToD3D11BindFlags(usage)
            };

            if ((usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly
                || (usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite)
            {
                if (rawBuffer)
                    bd.MiscFlags = (uint)ResourceMiscFlag.BufferAllowRawViews;
                else
                {
                    bd.MiscFlags = (uint)ResourceMiscFlag.BufferStructured;
                    bd.StructureByteStride = structureByteStride;
                }
            }

            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
                bd.MiscFlags = (uint)ResourceMiscFlag.DrawindirectArgs;

            if ((usage & BufferUsage.Dynamic) == BufferUsage.Dynamic)
            {
                bd.Usage = Silk.NET.Direct3D11.Usage.Dynamic;
                bd.CPUAccessFlags = (uint)CpuAccessFlag.Write;
            }
            else if ((usage & BufferUsage.Staging) == BufferUsage.Staging)
            {
                bd.Usage = Silk.NET.Direct3D11.Usage.Staging;
                bd.CPUAccessFlags = (uint)(CpuAccessFlag.Read | CpuAccessFlag.Write);
            }

            SilkMarshal.ThrowHResult(device.CreateBuffer(bd, Unsafe.NullRef<SubresourceData>(), ref buffer));
        }

        internal ComPtr<ID3D11ShaderResourceView> GetShaderResourceView(uint offset, uint size)
        {
            lock (accessViewLock)
            {
                var pair = new OffsetSizePair(offset, size);

                if (!srvs.TryGetValue(pair, out var srv))
                {
                    srv = createShaderResourceView(offset, size);
                    srvs.Add(pair, srv);
                }

                return srv;
            }
        }

        internal ComPtr<ID3D11UnorderedAccessView> GetUnorderedAccessView(uint offset, uint size)
        {
            lock (accessViewLock)
            {
                var pair = new OffsetSizePair(offset, size);

                if (!uavs.TryGetValue(pair, out var uav))
                {
                    uav = createUnorderedAccessView(offset, size);
                    uavs.Add(pair, uav);
                }

                return uav;
            }
        }

        private ComPtr<ID3D11ShaderResourceView> createShaderResourceView(uint offset, uint size)
        {
            ComPtr<ID3D11ShaderResourceView> result = null;

            if (rawBuffer)
            {
                SilkMarshal.ThrowHResult(device.CreateShaderResourceView(buffer, new ShaderResourceViewDesc
                {
                    Format = Format.FormatR32Typeless,
                    BufferEx = new BufferexSrv
                    {
                        FirstElement = offset / 4,
                        NumElements = size / 4,
                        Flags = (uint)BufferexSrvFlag.Raw
                    }
                }, ref result));
            }
            else
            {
                SilkMarshal.ThrowHResult(device.CreateShaderResourceView(buffer, new ShaderResourceViewDesc
                {
                    ViewDimension = D3DSrvDimension.D3DSrvDimensionBuffer,
                    Buffer = new BufferSrv
                    {
                        ElementOffset = offset / structureByteStride,
                        NumElements = size / structureByteStride
                    }
                }, ref result));
            }

            return result;
        }

        private ComPtr<ID3D11UnorderedAccessView> createUnorderedAccessView(uint offset, uint size)
        {
            ComPtr<ID3D11UnorderedAccessView> result = null;

            if (rawBuffer)
            {
                SilkMarshal.ThrowHResult(device.CreateUnorderedAccessView(buffer, new UnorderedAccessViewDesc
                {
                    Format = Format.FormatR32Typeless,
                    Buffer = new BufferUav
                    {
                        FirstElement = offset / 4,
                        NumElements = size / 4,
                        Flags = (uint)BufferUavFlag.Raw
                    }
                }, ref result));
            }
            else
            {
                SilkMarshal.ThrowHResult(device.CreateUnorderedAccessView(buffer, new UnorderedAccessViewDesc
                {
                    Format = Format.FormatUnknown,
                    Buffer = new BufferUav
                    {
                        FirstElement = offset / structureByteStride,
                        NumElements = size / structureByteStride
                    }
                }, ref result));
            }

            return result;
        }

        #region Disposal

        public override void Dispose()
        {
            if (isDisposed)
                return;

            foreach (var kvp in srvs)
                kvp.Value.Release();

            foreach (var kvp in uavs)
                kvp.Value.Release();

            Buffer.Release();

            isDisposed = true;
        }

        #endregion

        private readonly struct OffsetSizePair : IEquatable<OffsetSizePair>
        {
            public readonly uint Offset;
            public readonly uint Size;

            public OffsetSizePair(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public bool Equals(OffsetSizePair other)
            {
                return Offset.Equals(other.Offset) && Size.Equals(other.Size);
            }

            public override int GetHashCode()
            {
                return HashHelper.Combine(Offset.GetHashCode(), Size.GetHashCode());
            }
        }
    }
}

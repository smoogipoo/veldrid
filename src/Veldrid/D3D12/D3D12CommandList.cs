using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vortice;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Veldrid.D3D12
{
    internal class D3D12CommandList : CommandList
    {
        private readonly D3D12GraphicsDevice _gd;
        private bool _begun;
        private bool _disposed;
        private ID3D12GraphicsCommandList4 _commandList;

        private Viewport[] _viewports = new Viewport[0];
        private RawRect[] _scissors = new RawRect[0];
        private bool _viewportsChanged;
        private bool _scissorRectsChanged;

        private uint _numVertexBindings = 0;
        private ID3D12Resource[] _vertexBindings = new ID3D12Resource[1];
        private int[] _vertexStrides;
        private int[] _vertexOffsets = new int[1];

        // Cached pipeline State
        private DeviceBuffer _ib;
        private uint _ibOffset;
        private BlendDescription _blendState;
        private Color4 _blendFactor;
        private DepthStencilDescription _depthStencilState;
        private uint _stencilReference;
        private RasterizerDescription _rasterizerState;
        private Vortice.Direct3D.PrimitiveTopology _primitiveTopology;
        private InputLayoutDescription _inputLayout;
        private byte[] _vertexShader;
        private byte[] _geometryShader;
        private byte[] _hullShader;
        private byte[] _domainShader;
        private byte[] _pixelShader;

        private new D3D12Pipeline _graphicsPipeline;

        private BoundResourceSetInfo[] _graphicsResourceSets = new BoundResourceSetInfo[1];

        // Resource sets are invalidated when a new resource set is bound with an incompatible SRV or UAV.
        private bool[] _invalidatedGraphicsResourceSets = new bool[1];

        private new D3D12Pipeline _computePipeline;

        private BoundResourceSetInfo[] _computeResourceSets = new BoundResourceSetInfo[1];

        // Resource sets are invalidated when a new resource set is bound with an incompatible SRV or UAV.
        private bool[] _invalidatedComputeResourceSets = new bool[1];
        private string _name;
        private bool _vertexBindingsChanged;
        private ID3D12Resource[] _cbOut = new ID3D12Resource[1];
        private int[] _firstConstRef = new int[1];
        private int[] _numConstsRef = new int[1];

        // Cached resources
        private const int MaxCachedUniformBuffers = 15;
        private readonly D3D12BufferRange[] _vertexBoundUniformBuffers = new D3D12BufferRange[MaxCachedUniformBuffers];
        private readonly D3D12BufferRange[] _fragmentBoundUniformBuffers = new D3D12BufferRange[MaxCachedUniformBuffers];
        private const int MaxCachedTextureViews = 16;
        private readonly D3D12TextureView[] _vertexBoundTextureViews = new D3D12TextureView[MaxCachedTextureViews];
        private readonly D3D12TextureView[] _fragmentBoundTextureViews = new D3D12TextureView[MaxCachedTextureViews];
        private const int MaxCachedSamplers = 4;

        private readonly Dictionary<Texture, List<BoundTextureInfo>> _boundSRVs = new Dictionary<Texture, List<BoundTextureInfo>>();
        private readonly Dictionary<Texture, List<BoundTextureInfo>> _boundUAVs = new Dictionary<Texture, List<BoundTextureInfo>>();
        private readonly List<List<BoundTextureInfo>> _boundTextureInfoPool = new List<List<BoundTextureInfo>>(20);

        private const int MaxUAVs = 8;
        private readonly List<(DeviceBuffer, int)> _boundComputeUAVBuffers = new List<(DeviceBuffer, int)>(MaxUAVs);
        private readonly List<(DeviceBuffer, int)> _boundOMUAVBuffers = new List<(DeviceBuffer, int)>(MaxUAVs);

        private readonly List<ID3D12Resource> _availableStagingBuffers = new List<ID3D12Resource>();
        private readonly List<ID3D12Resource> _submittedStagingBuffers = new List<ID3D12Resource>();

        private readonly List<D3D12Swapchain> _referencedSwapchains = new List<D3D12Swapchain>();

        public D3D12CommandList(D3D12GraphicsDevice gd, ref CommandListDescription description)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            _gd = gd;
        }

        public ID3D12CommandList DeviceCommandList => _commandList;

        private D3D12Framebuffer D3D12Framebuffer => Util.AssertSubtype<Framebuffer, D3D12Framebuffer>(_framebuffer);

        public override bool IsDisposed => _disposed;

        public override void Begin()
        {
            _commandList?.Dispose();
            _commandList = null;
            ClearState();
            _begun = true;
        }

        private void ClearState()
        {
            ClearCachedState();

            // Todo:
            // _commandList.ClearState();
            ResetManagedState();
        }

        private void ResetManagedState()
        {
            _numVertexBindings = 0;
            Util.ClearArray(_vertexBindings);
            _vertexStrides = null;
            Util.ClearArray(_vertexOffsets);

            _framebuffer = null;

            Util.ClearArray(_viewports);
            Util.ClearArray(_scissors);
            _viewportsChanged = false;
            _scissorRectsChanged = false;

            _ib = null;
            _graphicsPipeline = null;
            _blendState = null;
            _depthStencilState = null;
            _rasterizerState = null;
            _primitiveTopology = Vortice.Direct3D.PrimitiveTopology.Undefined;
            _inputLayout = null;
            _vertexShader = null;
            _geometryShader = null;
            _hullShader = null;
            _domainShader = null;
            _pixelShader = null;

            ClearSets(_graphicsResourceSets);

            Util.ClearArray(_vertexBoundUniformBuffers);
            Util.ClearArray(_vertexBoundTextureViews);
            Util.ClearArray(_vertexBoundSamplers);

            Util.ClearArray(_fragmentBoundUniformBuffers);
            Util.ClearArray(_fragmentBoundTextureViews);
            Util.ClearArray(_fragmentBoundSamplers);

            _computePipeline = null;
            ClearSets(_computeResourceSets);

            foreach (KeyValuePair<Texture, List<BoundTextureInfo>> kvp in _boundSRVs)
            {
                List<BoundTextureInfo> list = kvp.Value;
                list.Clear();
                PoolBoundTextureList(list);
            }

            _boundSRVs.Clear();

            foreach (KeyValuePair<Texture, List<BoundTextureInfo>> kvp in _boundUAVs)
            {
                List<BoundTextureInfo> list = kvp.Value;
                list.Clear();
                PoolBoundTextureList(list);
            }

            _boundUAVs.Clear();
        }

        private void ClearSets(BoundResourceSetInfo[] boundSets)
        {
            foreach (BoundResourceSetInfo boundSetInfo in boundSets)
            {
                boundSetInfo.Offsets.Dispose();
            }

            Util.ClearArray(boundSets);
        }

        public override void End()
        {
            if (_commandList != null)
            {
                throw new VeldridException("Invalid use of End().");
            }

            _context.FinishCommandList(false, out _commandList).CheckError();
            _commandList.DebugName = _name;
            ResetManagedState();
            _begun = false;
        }

        public void Reset()
        {
            if (_commandList != null)
            {
                _commandList.Dispose();
                _commandList = null;
            }
            else if (_begun)
            {
                _context.ClearState();
                _context.FinishCommandList(false, out _commandList);
                _commandList.Dispose();
                _commandList = null;
            }

            ResetManagedState();
            _begun = false;
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            if (_ib != buffer || _ibOffset != offset)
            {
                _ib = buffer;
                _ibOffset = offset;
                D3D12Buffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(buffer);
                UnbindUAVBuffer(buffer);

                // Todo: Offset?

                _commandList.IASetIndexBuffer(
                    new IndexBufferView(
                        d3d12Buffer.DeviceResource.GPUVirtualAddress,
                        (int)d3d12Buffer.SizeInBytes,
                        D3D12Formats.ToDxgiFormat(format)));
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (!pipeline.IsComputePipeline && _graphicsPipeline != pipeline)
            {
                D3D12Pipeline d3dPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
                _graphicsPipeline = d3dPipeline;
                ClearSets(_graphicsResourceSets); // Invalidate resource set bindings -- they may be invalid.
                Util.ClearArray(_invalidatedGraphicsResourceSets);

                ID3D12BlendState blendState = d3dPipeline.BlendState;
                Color4 blendFactor = d3dPipeline.BlendFactor;

                if (_blendState != blendState || _blendFactor != blendFactor)
                {
                    _blendState = blendState;
                    _blendFactor = blendFactor;
                    _context.OMSetBlendState(blendState, blendFactor);
                }

                ID3D12DepthStencilState depthStencilState = d3dPipeline.DepthStencilState;
                uint stencilReference = d3dPipeline.StencilReference;

                if (_depthStencilState != depthStencilState || _stencilReference != stencilReference)
                {
                    _depthStencilState = depthStencilState;
                    _stencilReference = stencilReference;
                    _context.OMSetDepthStencilState(depthStencilState, (int)stencilReference);
                }

                ID3D12RasterizerState rasterizerState = d3dPipeline.RasterizerState;

                if (_rasterizerState != rasterizerState)
                {
                    _rasterizerState = rasterizerState;
                    _context.RSSetState(rasterizerState);
                }

                Vortice.Direct3D.PrimitiveTopology primitiveTopology = d3dPipeline.PrimitiveTopology;

                if (_primitiveTopology != primitiveTopology)
                {
                    _primitiveTopology = primitiveTopology;
                    _context.IASetPrimitiveTopology(primitiveTopology);
                }

                ID3D12InputLayout inputLayout = d3dPipeline.InputLayout;

                if (_inputLayout != inputLayout)
                {
                    _inputLayout = inputLayout;
                    _context.IASetInputLayout(inputLayout);
                }

                ID3D12VertexShader vertexShader = d3dPipeline.VertexShader;

                if (_vertexShader != vertexShader)
                {
                    _vertexShader = vertexShader;
                    _context.VSSetShader(vertexShader);
                }

                ID3D12GeometryShader geometryShader = d3dPipeline.GeometryShader;

                if (_geometryShader != geometryShader)
                {
                    _geometryShader = geometryShader;
                    _context.GSSetShader(geometryShader);
                }

                ID3D12HullShader hullShader = d3dPipeline.HullShader;

                if (_hullShader != hullShader)
                {
                    _hullShader = hullShader;
                    _context.HSSetShader(hullShader);
                }

                ID3D12DomainShader domainShader = d3dPipeline.DomainShader;

                if (_domainShader != domainShader)
                {
                    _domainShader = domainShader;
                    _context.DSSetShader(domainShader);
                }

                ID3D12PixelShader pixelShader = d3dPipeline.PixelShader;

                if (_pixelShader != pixelShader)
                {
                    _pixelShader = pixelShader;
                    _context.PSSetShader(pixelShader);
                }

                _vertexStrides = d3dPipeline.VertexStrides;

                if (_vertexStrides != null)
                {
                    int vertexStridesCount = _vertexStrides.Length;
                    Util.EnsureArrayMinimumSize(ref _vertexBindings, (uint)vertexStridesCount);
                    Util.EnsureArrayMinimumSize(ref _vertexOffsets, (uint)vertexStridesCount);
                }

                Util.EnsureArrayMinimumSize(ref _graphicsResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
                Util.EnsureArrayMinimumSize(ref _invalidatedGraphicsResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
            }
            else if (pipeline.IsComputePipeline && _computePipeline != pipeline)
            {
                D3D12Pipeline d3dPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
                _computePipeline = d3dPipeline;
                ClearSets(_computeResourceSets); // Invalidate resource set bindings -- they may be invalid.
                Util.ClearArray(_invalidatedComputeResourceSets);

                ID3D12ComputeShader computeShader = d3dPipeline.ComputeShader;
                _context.CSSetShader(computeShader);
                Util.EnsureArrayMinimumSize(ref _computeResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
                Util.EnsureArrayMinimumSize(ref _invalidatedComputeResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
            }
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (_graphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets))
            {
                return;
            }

            _graphicsResourceSets[slot].Offsets.Dispose();
            _graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            ActivateResourceSet(slot, _graphicsResourceSets[slot], true);
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (_computeResourceSets[slot].Equals(set, dynamicOffsetsCount, ref dynamicOffsets))
            {
                return;
            }

            _computeResourceSets[slot].Offsets.Dispose();
            _computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
            ActivateResourceSet(slot, _computeResourceSets[slot], false);
        }

        private void ActivateResourceSet(uint slot, BoundResourceSetInfo brsi, bool graphics)
        {
            D3D12ResourceSet d3d12RS = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(brsi.Set);

            int cbBase = GetConstantBufferBase(slot, graphics);
            int uaBase = GetUnorderedAccessBase(slot, graphics);
            int textureBase = GetTextureBase(slot, graphics);
            int samplerBase = GetSamplerBase(slot, graphics);

            D3D12ResourceLayout layout = d3d12RS.Layout;
            BindableResource[] resources = d3d12RS.Resources;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < resources.Length; i++)
            {
                BindableResource resource = resources[i];
                uint bufferOffset = 0;

                if (layout.IsDynamicBuffer(i))
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                D3D12ResourceLayout.ResourceBindingInfo rbi = layout.GetDeviceSlotIndex(i);

                switch (rbi.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    {
                        D3D12BufferRange range = GetBufferRange(resource, bufferOffset);
                        BindUniformBuffer(range, cbBase + rbi.Slot, rbi.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        D3D12BufferRange range = GetBufferRange(resource, bufferOffset);
                        BindStorageBufferView(range, textureBase + rbi.Slot, rbi.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        D3D12BufferRange range = GetBufferRange(resource, bufferOffset);
                        ID3D12DescriptorHeap uav = range.Buffer.GetUnorderedAccessView(range.Offset, range.Size);
                        BindUnorderedAccessView(null, range.Buffer, uav, uaBase + rbi.Slot, rbi.Stages, slot);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        TextureView texView = Util.GetTextureView(_gd, resource);
                        D3D12TextureView d3d12TexView = Util.AssertSubtype<TextureView, D3D12TextureView>(texView);
                        UnbindUAVTexture(d3d12TexView.Target);
                        BindTextureView(d3d12TexView, textureBase + rbi.Slot, rbi.Stages, slot);
                        break;

                    case ResourceKind.TextureReadWrite:
                        TextureView rwTexView = Util.GetTextureView(_gd, resource);
                        D3D12TextureView d3d12RWTexView = Util.AssertSubtype<TextureView, D3D12TextureView>(rwTexView);
                        UnbindSRVTexture(d3d12RWTexView.Target);
                        BindUnorderedAccessView(d3d12RWTexView.Target, null, d3d12RWTexView.UnorderedAccessView, uaBase + rbi.Slot, rbi.Stages, slot);
                        break;

                    case ResourceKind.Sampler:
                        D3D12Sampler sampler = Util.AssertSubtype<BindableResource, D3D12Sampler>(resource);
                        BindSampler(sampler, samplerBase + rbi.Slot, rbi.Stages);
                        break;

                    default: throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private D3D12BufferRange GetBufferRange(BindableResource resource, uint additionalOffset)
        {
            if (resource is D3D12Buffer d3d12Buff)
            {
                return new D3D12BufferRange(d3d12Buff, additionalOffset, d3d12Buff.SizeInBytes);
            }
            else if (resource is DeviceBufferRange range)
            {
                return new D3D12BufferRange(
                    Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(range.Buffer),
                    range.Offset + additionalOffset,
                    range.SizeInBytes);
            }
            else
            {
                throw new VeldridException($"Unexpected resource type used in a buffer type slot: {resource.GetType().Name}");
            }
        }

        private void UnbindSRVTexture(Texture target)
        {
            if (_boundSRVs.TryGetValue(target, out List<BoundTextureInfo> btis))
            {
                foreach (BoundTextureInfo bti in btis)
                {
                    BindTextureView(null, bti.Slot, bti.Stages, 0);

                    if ((bti.Stages & ShaderStages.Compute) == ShaderStages.Compute)
                    {
                        _invalidatedComputeResourceSets[bti.ResourceSet] = true;
                    }
                    else
                    {
                        _invalidatedGraphicsResourceSets[bti.ResourceSet] = true;
                    }
                }

                bool result = _boundSRVs.Remove(target);
                Debug.Assert(result);

                btis.Clear();
                PoolBoundTextureList(btis);
            }
        }

        private void PoolBoundTextureList(List<BoundTextureInfo> btis)
        {
            _boundTextureInfoPool.Add(btis);
        }

        private void UnbindUAVTexture(Texture target)
        {
            if (_boundUAVs.TryGetValue(target, out List<BoundTextureInfo> btis))
            {
                foreach (BoundTextureInfo bti in btis)
                {
                    BindUnorderedAccessView(null, null, null, bti.Slot, bti.Stages, bti.ResourceSet);

                    if ((bti.Stages & ShaderStages.Compute) == ShaderStages.Compute)
                    {
                        _invalidatedComputeResourceSets[bti.ResourceSet] = true;
                    }
                    else
                    {
                        _invalidatedGraphicsResourceSets[bti.ResourceSet] = true;
                    }
                }

                bool result = _boundUAVs.Remove(target);
                Debug.Assert(result);

                btis.Clear();
                PoolBoundTextureList(btis);
            }
        }

        private int GetConstantBufferBase(uint slot, bool graphics)
        {
            D3D12ResourceLayout[] layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].UniformBufferCount;
            }

            return ret;
        }

        private int GetUnorderedAccessBase(uint slot, bool graphics)
        {
            D3D12ResourceLayout[] layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].StorageBufferCount;
            }

            return ret;
        }

        private int GetTextureBase(uint slot, bool graphics)
        {
            D3D12ResourceLayout[] layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].TextureCount;
            }

            return ret;
        }

        private int GetSamplerBase(uint slot, bool graphics)
        {
            D3D12ResourceLayout[] layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].SamplerCount;
            }

            return ret;
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            D3D12Buffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(buffer);

            if (_vertexBindings[index] != d3d12Buffer.Buffer || _vertexOffsets[index] != offset)
            {
                _vertexBindingsChanged = true;
                UnbindUAVBuffer(buffer);
                _vertexBindings[index] = d3d12Buffer.Buffer;
                _vertexOffsets[index] = (int)offset;
                _numVertexBindings = Math.Max((index + 1), _numVertexBindings);
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            PreDrawCommand();

            if (instanceCount == 1 && instanceStart == 0)
            {
                _context.Draw((int)vertexCount, (int)vertexStart);
            }
            else
            {
                _context.DrawInstanced((int)vertexCount, (int)instanceCount, (int)vertexStart, (int)instanceStart);
            }
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            PreDrawCommand();

            Debug.Assert(_ib != null);

            if (instanceCount == 1 && instanceStart == 0)
            {
                _context.DrawIndexed((int)indexCount, (int)indexStart, vertexOffset);
            }
            else
            {
                _context.DrawIndexedInstanced((int)indexCount, (int)instanceCount, (int)indexStart, vertexOffset, (int)instanceStart);
            }
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            PreDrawCommand();

            D3D12Buffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(indirectBuffer);
            int currentOffset = (int)offset;

            for (uint i = 0; i < drawCount; i++)
            {
                _context.DrawInstancedIndirect(d3d12Buffer.Buffer, currentOffset);
                currentOffset += (int)stride;
            }
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            PreDrawCommand();

            D3D12Buffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(indirectBuffer);
            int currentOffset = (int)offset;

            for (uint i = 0; i < drawCount; i++)
            {
                _context.DrawIndexedInstancedIndirect(d3d12Buffer.Buffer, currentOffset);
                currentOffset += (int)stride;
            }
        }

        private void PreDrawCommand()
        {
            FlushViewports();
            FlushScissorRects();
            FlushVertexBindings();

            int graphicsResourceCount = _graphicsPipeline.ResourceLayouts.Length;

            for (uint i = 0; i < graphicsResourceCount; i++)
            {
                if (_invalidatedGraphicsResourceSets[i])
                {
                    _invalidatedGraphicsResourceSets[i] = false;
                    ActivateResourceSet(i, _graphicsResourceSets[i], true);
                }
            }
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            PreDispatchCommand();

            _context.Dispatch((int)groupCountX, (int)groupCountY, (int)groupCountZ);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            PreDispatchCommand();
            D3D12Buffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(indirectBuffer);
            _context.DispatchIndirect(d3d12Buffer.Buffer, (int)offset);
        }

        private void PreDispatchCommand()
        {
            int computeResourceCount = _computePipeline.ResourceLayouts.Length;

            for (uint i = 0; i < computeResourceCount; i++)
            {
                if (_invalidatedComputeResourceSets[i])
                {
                    _invalidatedComputeResourceSets[i] = false;
                    ActivateResourceSet(i, _computeResourceSets[i], false);
                }
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            D3D12Texture d3d12Source = Util.AssertSubtype<Texture, D3D12Texture>(source);
            D3D12Texture d3d12Destination = Util.AssertSubtype<Texture, D3D12Texture>(destination);
            _context.ResolveSubresource(
                d3d12Destination.DeviceTexture,
                0,
                d3d12Source.DeviceTexture,
                0,
                d3d12Destination.DxgiFormat);
        }

        private void FlushViewports()
        {
            if (_viewportsChanged)
            {
                _viewportsChanged = false;
                _context.RSSetViewports(_viewports);
            }
        }

        private void FlushScissorRects()
        {
            if (_scissorRectsChanged)
            {
                _scissorRectsChanged = false;

                if (_scissors.Length > 0)
                {
                    // Because this array is resized using Util.EnsureMinimumArraySize, this might set more scissor rectangles
                    // than are actually needed, but this is okay -- extras are essentially ignored and should be harmless.
                    _context.RSSetScissorRects(_scissors);
                }
            }
        }

        private unsafe void FlushVertexBindings()
        {
            if (_vertexBindingsChanged)
            {
                _context.IASetVertexBuffers(
                    0, (int)_numVertexBindings,
                    _vertexBindings,
                    _vertexStrides,
                    _vertexOffsets);

                _vertexBindingsChanged = false;
            }
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            _scissorRectsChanged = true;
            Util.EnsureArrayMinimumSize(ref _scissors, index + 1);
            _scissors[index] = new RawRect((int)x, (int)y, (int)(x + width), (int)(y + height));
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            _viewportsChanged = true;
            Util.EnsureArrayMinimumSize(ref _viewports, index + 1);
            _viewports[index] = viewport;
        }

        private void BindTextureView(D3D12TextureView texView, int slot, ShaderStages stages, uint resourceSet)
        {
            ID3D12ShaderResourceView srv = texView?.ShaderResourceView ?? null;

            if (srv != null)
            {
                if (!_boundSRVs.TryGetValue(texView.Target, out List<BoundTextureInfo> list))
                {
                    list = GetNewOrCachedBoundTextureInfoList();
                    _boundSRVs.Add(texView.Target, list);
                }

                list.Add(new BoundTextureInfo { Slot = slot, Stages = stages, ResourceSet = resourceSet });
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                bool bind = false;

                if (slot < MaxCachedUniformBuffers)
                {
                    if (_vertexBoundTextureViews[slot] != texView)
                    {
                        _vertexBoundTextureViews[slot] = texView;
                        bind = true;
                    }
                }
                else
                {
                    bind = true;
                }

                if (bind)
                {
                    _context.VSSetShaderResource(slot, srv);
                }
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry)
            {
                _context.GSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl)
            {
                _context.HSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation)
            {
                _context.DSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                bool bind = false;

                if (slot < MaxCachedUniformBuffers)
                {
                    if (_fragmentBoundTextureViews[slot] != texView)
                    {
                        _fragmentBoundTextureViews[slot] = texView;
                        bind = true;
                    }
                }
                else
                {
                    bind = true;
                }

                if (bind)
                {
                    _context.PSSetShaderResource(slot, srv);
                }
            }

            if ((stages & ShaderStages.Compute) == ShaderStages.Compute)
            {
                _context.CSSetShaderResource(slot, srv);
            }
        }

        private List<BoundTextureInfo> GetNewOrCachedBoundTextureInfoList()
        {
            if (_boundTextureInfoPool.Count > 0)
            {
                int index = _boundTextureInfoPool.Count - 1;
                List<BoundTextureInfo> ret = _boundTextureInfoPool[index];
                _boundTextureInfoPool.RemoveAt(index);
                return ret;
            }

            return new List<BoundTextureInfo>();
        }

        private void BindStorageBufferView(D3D12BufferRange range, int slot, ShaderStages stages)
        {
            bool compute = (stages & ShaderStages.Compute) != 0;
            UnbindUAVBuffer(range.Buffer);

            ID3D12ShaderResourceView srv = range.Buffer.GetShaderResourceView(range.Offset, range.Size);

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                _context.VSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry)
            {
                _context.GSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl)
            {
                _context.HSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation)
            {
                _context.DSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                _context.PSSetShaderResource(slot, srv);
            }

            if (compute)
            {
                _context.CSSetShaderResource(slot, srv);
            }
        }

        private void BindUniformBuffer(D3D12BufferRange range, int slot, ShaderStages stages)
        {
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                bool bind = false;

                if (slot < MaxCachedUniformBuffers)
                {
                    if (!_vertexBoundUniformBuffers[slot].Equals(range))
                    {
                        _vertexBoundUniformBuffers[slot] = range;
                        bind = true;
                    }
                }
                else
                {
                    bind = true;
                }

                if (bind)
                {
                    if (range.IsFullRange)
                    {
                        _context.VSSetConstantBuffer(slot, range.Buffer.Buffer);
                    }
                    else
                    {
                        PackRangeParams(range);

                        if (!_gd.SupportsCommandLists)
                        {
                            _context.VSUnsetConstantBuffer(slot);
                        }

                        _context1.VSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                    }
                }
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry)
            {
                if (range.IsFullRange)
                {
                    _context.GSSetConstantBuffer(slot, range.Buffer.Buffer);
                }
                else
                {
                    PackRangeParams(range);

                    if (!_gd.SupportsCommandLists)
                    {
                        _context.GSUnsetConstantBuffer(slot);
                    }

                    _context1.GSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                }
            }

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl)
            {
                if (range.IsFullRange)
                {
                    _context.HSSetConstantBuffer(slot, range.Buffer.Buffer);
                }
                else
                {
                    PackRangeParams(range);

                    if (!_gd.SupportsCommandLists)
                    {
                        _context.HSUnsetConstantBuffer(slot);
                    }

                    _context1.HSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                }
            }

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation)
            {
                if (range.IsFullRange)
                {
                    _context.DSSetConstantBuffer(slot, range.Buffer.Buffer);
                }
                else
                {
                    PackRangeParams(range);

                    if (!_gd.SupportsCommandLists)
                    {
                        _context.DSUnsetConstantBuffer(slot);
                    }

                    _context1.DSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                }
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                bool bind = false;

                if (slot < MaxCachedUniformBuffers)
                {
                    if (!_fragmentBoundUniformBuffers[slot].Equals(range))
                    {
                        _fragmentBoundUniformBuffers[slot] = range;
                        bind = true;
                    }
                }
                else
                {
                    bind = true;
                }

                if (bind)
                {
                    if (range.IsFullRange)
                    {
                        _context.PSSetConstantBuffer(slot, range.Buffer.Buffer);
                    }
                    else
                    {
                        PackRangeParams(range);

                        if (!_gd.SupportsCommandLists)
                        {
                            _context.PSUnsetConstantBuffer(slot);
                        }

                        _context1.PSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                    }
                }
            }

            if ((stages & ShaderStages.Compute) == ShaderStages.Compute)
            {
                if (range.IsFullRange)
                {
                    _context.CSSetConstantBuffer(slot, range.Buffer.Buffer);
                }
                else
                {
                    PackRangeParams(range);

                    if (!_gd.SupportsCommandLists)
                    {
                        _context.CSSetConstantBuffer(slot, (ID3D12Buffer)null);
                    }

                    _context1.CSSetConstantBuffers1(slot, 1, _cbOut, _firstConstRef, _numConstsRef);
                }
            }
        }

        private void PackRangeParams(D3D12BufferRange range)
        {
            _cbOut[0] = range.Buffer.Buffer;
            _firstConstRef[0] = (int)range.Offset / 16;
            uint roundedSize = range.Size < 256 ? 256u : range.Size;
            _numConstsRef[0] = (int)roundedSize / 16;
        }

        private void BindUnorderedAccessView(
            Texture texture,
            DeviceBuffer buffer,
            ID3D12DescriptorHeap uav,
            int slot,
            ShaderStages stages,
            uint resourceSet)
        {
            bool compute = stages == ShaderStages.Compute;
            Debug.Assert(compute || ((stages & ShaderStages.Compute) == 0));
            Debug.Assert(texture == null || buffer == null);

            if (texture != null && uav != null)
            {
                if (!_boundUAVs.TryGetValue(texture, out List<BoundTextureInfo> list))
                {
                    list = GetNewOrCachedBoundTextureInfoList();
                    _boundUAVs.Add(texture, list);
                }

                list.Add(new BoundTextureInfo { Slot = slot, Stages = stages, ResourceSet = resourceSet });
            }

            int baseSlot = 0;

            if (!compute)
            {
                baseSlot = _framebuffer.ColorTargets.Count;
            }

            int actualSlot = baseSlot + slot;

            if (buffer != null)
            {
                TrackBoundUAVBuffer(buffer, actualSlot, compute);
            }

            if (compute)
            {
                _commandList.SetComputeRootUnorderedAccessView(actualSlot, uav.GetGPUDescriptorHandleForHeapStart());
            }
            else
            {
                _commandList.SetGraphicsRootUnorderedAccessView(actualSlot, uav.GetGPUDescriptorHandleForHeapStart());
            }
        }

        private void TrackBoundUAVBuffer(DeviceBuffer buffer, int slot, bool compute)
        {
            List<(DeviceBuffer, int)> list = compute ? _boundComputeUAVBuffers : _boundOMUAVBuffers;
            list.Add((buffer, slot));
        }

        private void UnbindUAVBuffer(DeviceBuffer buffer)
        {
            UnbindUAVBufferIndividual(buffer, false);
            UnbindUAVBufferIndividual(buffer, true);
        }

        private void UnbindUAVBufferIndividual(DeviceBuffer buffer, bool compute)
        {
            List<(DeviceBuffer, int)> list = compute ? _boundComputeUAVBuffers : _boundOMUAVBuffers;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Item1 == buffer)
                {
                    int slot = list[i].Item2;

                    if (compute)
                    {
                        _commandList.SetComputeRootDescriptorTable(slot, GpuDescriptorHandle.Default);
                    }
                    else
                    {
                        _commandList.SetGraphicsRootDescriptorTable(slot, GpuDescriptorHandle.Default);
                    }

                    list.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        private void BindSampler(D3D12Sampler sampler, int slot, ShaderStages stages)
        {
            if ((stages & ShaderStages.Compute) == ShaderStages.Compute)
            {
                _commandList.SetGraphicsRootDescriptorTable(slot, sampler.DescriptorHeap.GetGPUDescriptorHandleForHeapStart());
            }
            else
            {
                _commandList.SetGraphicsRootDescriptorTable(slot, sampler.DescriptorHeap.GetGPUDescriptorHandleForHeapStart());
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            D3D12Framebuffer d3dFB = Util.AssertSubtype<Framebuffer, D3D12Framebuffer>(fb);

            if (d3dFB.Swapchain != null)
            {
                d3dFB.Swapchain.AddCommandListReference(this);
                _referencedSwapchains.Add(d3dFB.Swapchain);
            }

            for (int i = 0; i < fb.ColorTargets.Count; i++)
            {
                UnbindSRVTexture(fb.ColorTargets[i].Target);
            }

            _commandList.OMSetRenderTargets(d3dFB.RenderTargetHeap.GetCPUDescriptorHandleForHeapStart(), d3dFB.DepthStencilHeap?.GetCPUDescriptorHandleForHeapStart());
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            ID3D12DescriptorHeap heap = D3D12Framebuffer.RenderTargetHeap;
            int descriptorSize = _gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

            _commandList.ClearRenderTargetView(heap.GetCPUDescriptorHandleForHeapStart() + (int)(index * descriptorSize), new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            _commandList.ClearDepthStencilView(D3D12Framebuffer.DepthStencilHeap.GetCPUDescriptorHandleForHeapStart(), ClearFlags.Depth | ClearFlags.Stencil, depth, stencil);
        }

        private protected unsafe override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            D3D12Buffer d3dBuffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(buffer);

            if (sizeInBytes == 0)
            {
                return;
            }

            bool isDynamic = (buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            bool isStaging = (buffer.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            bool isUniformBuffer = (buffer.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer;
            bool useMap = isDynamic;
            bool updateFullBuffer = bufferOffsetInBytes == 0 && sizeInBytes == buffer.SizeInBytes;
            bool useUpdateSubresource = !isDynamic && !isStaging && (!isUniformBuffer || updateFullBuffer);

            if (useUpdateSubresource)
            {
                Box? subregion = new Box((int)bufferOffsetInBytes, 0, 0, (int)(sizeInBytes + bufferOffsetInBytes), 1, 1);

                if (isUniformBuffer)
                {
                    subregion = null;
                }

                if (bufferOffsetInBytes == 0)
                {
                    _context.UpdateSubresource(d3dBuffer.Buffer, 0, subregion, source, 0, 0);
                }
                else
                {
                    UpdateSubresource_Workaround(d3dBuffer.Buffer, 0, subregion.Value, source);
                }
            }
            else if (useMap && updateFullBuffer) // Can only update full buffer with WriteDiscard.
            {
                MappedSubresource msb = _context.Map(
                    d3dBuffer.Buffer,
                    0,
                    D3D12Formats.VdToD3D12MapMode(isDynamic, MapMode.Write),
                    MapFlags.None);

                if (sizeInBytes < 1024)
                {
                    Unsafe.CopyBlock(msb.DataPointer.ToPointer(), source.ToPointer(), sizeInBytes);
                }
                else
                {
                    Buffer.MemoryCopy(source.ToPointer(), msb.DataPointer.ToPointer(), buffer.SizeInBytes, sizeInBytes);
                }

                _context.Unmap(d3dBuffer.Buffer, 0);
            }
            else
            {
                D3D12Buffer staging = GetFreeStagingBuffer(sizeInBytes);
                _gd.UpdateBuffer(staging, 0, source, sizeInBytes);
                CopyBuffer(staging, 0, buffer, bufferOffsetInBytes, sizeInBytes);
                _submittedStagingBuffers.Add(staging);
            }
        }

        private unsafe void UpdateSubresource_Workaround(
            ID3D12Resource resource,
            int subresource,
            Box region,
            IntPtr data)
        {
            bool needWorkaround = !_gd.SupportsCommandLists;
            void* pAdjustedSrcData = data.ToPointer();

            if (needWorkaround)
            {
                Debug.Assert(region.Top == 0 && region.Front == 0);
                pAdjustedSrcData = (byte*)data - region.Left;
            }

            _context.UpdateSubresource(resource, subresource, region, (IntPtr)pAdjustedSrcData, 0, 0);
        }

        private D3D12Buffer GetFreeStagingBuffer(uint sizeInBytes)
        {
            foreach (D3D12Buffer buffer in _availableStagingBuffers)
            {
                if (buffer.SizeInBytes >= sizeInBytes)
                {
                    _availableStagingBuffers.Remove(buffer);
                    return buffer;
                }
            }

            DeviceBuffer staging = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(staging);
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            D3D12Buffer srcD3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(source);
            D3D12Buffer dstD3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12Buffer>(destination);

            Box region = new Box((int)sourceOffset, 0, 0, (int)(sourceOffset + sizeInBytes), 1, 1);

            _context.CopySubresourceRegion(dstD3D12Buffer.Buffer, 0, (int)destinationOffset, 0, 0, srcD3D12Buffer.Buffer, 0, region);
        }

        protected override void CopyTextureCore(
            Texture source,
            uint srcX, uint srcY, uint srcZ,
            uint srcMipLevel,
            uint srcBaseArrayLayer,
            Texture destination,
            uint dstX, uint dstY, uint dstZ,
            uint dstMipLevel,
            uint dstBaseArrayLayer,
            uint width, uint height, uint depth,
            uint layerCount)
        {
            D3D12Texture srcD3D12Texture = Util.AssertSubtype<Texture, D3D12Texture>(source);
            D3D12Texture dstD3D12Texture = Util.AssertSubtype<Texture, D3D12Texture>(destination);

            uint blockSize = FormatHelpers.IsCompressedFormat(source.Format) ? 4u : 1u;
            uint clampedWidth = Math.Max(blockSize, width);
            uint clampedHeight = Math.Max(blockSize, height);

            Box? region = null;

            if (srcX != 0 || srcY != 0 || srcZ != 0
                || clampedWidth != source.Width || clampedHeight != source.Height || depth != source.Depth)
            {
                region = new Box(
                    (int)srcX,
                    (int)srcY,
                    (int)srcZ,
                    (int)(srcX + clampedWidth),
                    (int)(srcY + clampedHeight),
                    (int)(srcZ + depth));
            }

            for (uint i = 0; i < layerCount; i++)
            {
                int srcSubresource = D3D12Util.ComputeSubresource(srcMipLevel, source.MipLevels, srcBaseArrayLayer + i);
                int dstSubresource = D3D12Util.ComputeSubresource(dstMipLevel, destination.MipLevels, dstBaseArrayLayer + i);

                _context.CopySubresourceRegion(
                    dstD3D12Texture.DeviceTexture,
                    dstSubresource,
                    (int)dstX,
                    (int)dstY,
                    (int)dstZ,
                    srcD3D12Texture.DeviceTexture,
                    srcSubresource,
                    region);
            }
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            TextureView fullTexView = texture.GetFullTextureView(_gd);
            D3D12TextureView d3d12View = Util.AssertSubtype<TextureView, D3D12TextureView>(fullTexView);
            ID3D12ShaderResourceView srv = d3d12View.ShaderResourceView;
            _context.GenerateMips(srv);
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                _context.DebugName = value;
            }
        }

        internal void OnCompleted()
        {
            _commandList.Dispose();
            _commandList = null;

            foreach (D3D12Swapchain sc in _referencedSwapchains)
            {
                sc.RemoveCommandListReference(this);
            }

            _referencedSwapchains.Clear();

            foreach (D3D12Buffer buffer in _submittedStagingBuffers)
            {
                _availableStagingBuffers.Add(buffer);
            }

            _submittedStagingBuffers.Clear();
        }

        private protected override void PushDebugGroupCore(string name)
        {
            _uda?.BeginEvent(name);
        }

        private protected override void PopDebugGroupCore()
        {
            _uda?.EndEvent();
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            _uda?.SetMarker(name);
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _uda?.Dispose();
                DeviceCommandList?.Dispose();
                _context1?.Dispose();
                _context.Dispose();

                foreach (BoundResourceSetInfo boundGraphicsSet in _graphicsResourceSets)
                {
                    boundGraphicsSet.Offsets.Dispose();
                }

                foreach (BoundResourceSetInfo boundComputeSet in _computeResourceSets)
                {
                    boundComputeSet.Offsets.Dispose();
                }

                foreach (D3D12Buffer buffer in _availableStagingBuffers)
                {
                    buffer.Dispose();
                }

                _availableStagingBuffers.Clear();

                _disposed = true;
            }
        }

        private struct BoundTextureInfo
        {
            public int Slot;
            public ShaderStages Stages;
            public uint ResourceSet;
        }

        private struct D3D12BufferRange : IEquatable<D3D12BufferRange>
        {
            public readonly D3D12Buffer Buffer;
            public readonly uint Offset;
            public readonly uint Size;

            public bool IsFullRange => Offset == 0 && Size == Buffer.SizeInBytes;

            public D3D12BufferRange(D3D12Buffer buffer, uint offset, uint size)
            {
                Buffer = buffer;
                Offset = offset;
                Size = size;
            }

            public bool Equals(D3D12BufferRange other)
            {
                return Buffer == other.Buffer && Offset.Equals(other.Offset) && Size.Equals(other.Size);
            }
        }
    }
}

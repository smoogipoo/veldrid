using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Vortice;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Veldrid.D3D11
{
    internal class D3D11CommandList : CommandList
    {
        public ID3D11CommandList DeviceCommandList => commandList;

        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                DeviceContext.DebugName = value;
            }
        }

        internal ComPtr<ID3D11DeviceContext> DeviceContext { get; }

        private readonly D3D11GraphicsDevice gd;
        private readonly ID3D11DeviceContext1 context1;
        private readonly ID3DUserDefinedAnnotation uda;

        // Cached resources
        private const int max_cached_uniform_buffers = 15;
        private readonly D3D11BufferRange[] vertexBoundUniformBuffers = new D3D11BufferRange[max_cached_uniform_buffers];
        private readonly D3D11BufferRange[] fragmentBoundUniformBuffers = new D3D11BufferRange[max_cached_uniform_buffers];
        private const int max_cached_texture_views = 16;
        private readonly D3D11TextureView[] vertexBoundTextureViews = new D3D11TextureView[max_cached_texture_views];
        private readonly D3D11TextureView[] fragmentBoundTextureViews = new D3D11TextureView[max_cached_texture_views];
        private const int max_cached_samplers = 4;
        private readonly D3D11Sampler[] vertexBoundSamplers = new D3D11Sampler[max_cached_samplers];
        private readonly D3D11Sampler[] fragmentBoundSamplers = new D3D11Sampler[max_cached_samplers];

        private readonly Dictionary<Texture, List<BoundTextureInfo>> boundSRVs = new Dictionary<Texture, List<BoundTextureInfo>>();
        private readonly Dictionary<Texture, List<BoundTextureInfo>> boundUaVs = new Dictionary<Texture, List<BoundTextureInfo>>();
        private readonly List<List<BoundTextureInfo>> boundTextureInfoPool = new List<List<BoundTextureInfo>>(20);

        private const int max_ua_vs = 8;
        private readonly List<(DeviceBuffer, int)> boundComputeUavBuffers = new List<(DeviceBuffer, int)>(max_ua_vs);
        private readonly List<(DeviceBuffer, int)> boundOmuavBuffers = new List<(DeviceBuffer, int)>(max_ua_vs);

        private readonly List<D3D11Buffer> availableStagingBuffers = new List<D3D11Buffer>();
        private readonly List<D3D11Buffer> submittedStagingBuffers = new List<D3D11Buffer>();

        private readonly List<D3D11Swapchain> referencedSwapchains = new List<D3D11Swapchain>();

        private D3D11Framebuffer d3D11Framebuffer => Util.AssertSubtype<Framebuffer, D3D11Framebuffer>(Framebuffer);
        private bool begun;
        private bool disposed;
        private ID3D11CommandList commandList;

        private Viewport[] viewports = new Viewport[0];
        private RawRect[] scissors = new RawRect[0];
        private bool viewportsChanged;
        private bool scissorRectsChanged;

        private uint numVertexBindings;
        private ID3D11Buffer[] vertexBindings = new ID3D11Buffer[1];
        private int[] vertexStrides = new int[1];
        private int[] vertexOffsets = new int[1];

        // Cached pipeline State
        private DeviceBuffer ib;
        private uint ibOffset;
        private ID3D11BlendState blendState;
        private Color4 blendFactor;
        private ID3D11DepthStencilState depthStencilState;
        private uint stencilReference;
        private ID3D11RasterizerState rasterizerState;
        private Vortice.Direct3D.PrimitiveTopology primitiveTopology;
        private ID3D11InputLayout inputLayout;
        private ID3D11VertexShader vertexShader;
        private ID3D11GeometryShader geometryShader;
        private ID3D11HullShader hullShader;
        private ID3D11DomainShader domainShader;
        private ID3D11PixelShader pixelShader;

        private D3D11Pipeline graphicsPipeline;

        private BoundResourceSetInfo[] graphicsResourceSets = new BoundResourceSetInfo[1];

        // Resource sets are invalidated when a new resource set is bound with an incompatible SRV or UAV.
        private bool[] invalidatedGraphicsResourceSets = new bool[1];

        private D3D11Pipeline computePipeline;

        private BoundResourceSetInfo[] computeResourceSets = new BoundResourceSetInfo[1];

        // Resource sets are invalidated when a new resource set is bound with an incompatible SRV or UAV.
        private bool[] invalidatedComputeResourceSets = new bool[1];
        private string name;
        private bool vertexBindingsChanged;
        private readonly ID3D11Buffer[] cbOut = new ID3D11Buffer[1];
        private readonly int[] firstConstRef = new int[1];
        private readonly int[] numConstsRef = new int[1];

        public D3D11CommandList(D3D11GraphicsDevice gd, ref CommandListDescription description)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            this.gd = gd;
            DeviceContext = gd.Device.CreateDeferredContext();
            context1 = DeviceContext.QueryInterfaceOrNull<ID3D11DeviceContext1>();
            uda = DeviceContext.QueryInterfaceOrNull<ID3DUserDefinedAnnotation>();
        }

        #region Disposal

        public override void Dispose()
        {
            if (!disposed)
            {
                uda?.Dispose();
                DeviceCommandList?.Dispose();
                context1?.Dispose();
                DeviceContext.Dispose();

                foreach (var boundGraphicsSet in graphicsResourceSets) boundGraphicsSet.Offsets.Dispose();

                foreach (var boundComputeSet in computeResourceSets) boundComputeSet.Offsets.Dispose();

                foreach (var buffer in availableStagingBuffers) buffer.Dispose();
                availableStagingBuffers.Clear();

                disposed = true;
            }
        }

        #endregion

        public override void Begin()
        {
            commandList?.Dispose();
            commandList = null;
            clearState();
            begun = true;
        }

        public override void End()
        {
            if (commandList != null) throw new VeldridException("Invalid use of End().");

            DeviceContext.FinishCommandList(false, out commandList).CheckError();
            commandList.DebugName = name;
            resetManagedState();
            begun = false;
        }

        public void Reset()
        {
            if (commandList != null)
            {
                commandList.Dispose();
                commandList = null;
            }
            else if (begun)
            {
                DeviceContext.ClearState();
                DeviceContext.FinishCommandList(false, out commandList);
                commandList.Dispose();
                commandList = null;
            }

            resetManagedState();
            begun = false;
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            preDispatchCommand();

            DeviceContext.Dispatch((int)groupCountX, (int)groupCountY, (int)groupCountZ);
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            scissorRectsChanged = true;
            Util.EnsureArrayMinimumSize(ref scissors, index + 1);
            scissors[index] = new RawRect((int)x, (int)y, (int)(x + width), (int)(y + height));
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            viewportsChanged = true;
            Util.EnsureArrayMinimumSize(ref viewports, index + 1);
            viewports[index] = viewport;
        }

        internal void OnCompleted()
        {
            commandList.Dispose();
            commandList = null;

            foreach (var sc in referencedSwapchains) sc.RemoveCommandListReference(this);
            referencedSwapchains.Clear();

            foreach (var buffer in submittedStagingBuffers) availableStagingBuffers.Add(buffer);

            submittedStagingBuffers.Clear();
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (graphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) return;

            graphicsResourceSets[slot].Offsets.Dispose();
            graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            activateResourceSet(slot, graphicsResourceSets[slot], true);
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (computeResourceSets[slot].Equals(set, dynamicOffsetsCount, ref dynamicOffsets)) return;

            computeResourceSets[slot].Offsets.Dispose();
            computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
            activateResourceSet(slot, computeResourceSets[slot], false);
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            preDrawCommand();

            var d3d11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(indirectBuffer);
            int currentOffset = (int)offset;

            for (uint i = 0; i < drawCount; i++)
            {
                DeviceContext.DrawInstancedIndirect(d3d11Buffer.Buffer, currentOffset);
                currentOffset += (int)stride;
            }
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            preDrawCommand();

            var d3d11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(indirectBuffer);
            int currentOffset = (int)offset;

            for (uint i = 0; i < drawCount; i++)
            {
                DeviceContext.DrawIndexedInstancedIndirect(d3d11Buffer.Buffer, currentOffset);
                currentOffset += (int)stride;
            }
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            preDispatchCommand();
            var d3d11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(indirectBuffer);
            DeviceContext.DispatchIndirect(d3d11Buffer.Buffer, (int)offset);
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            var d3d11Source = Util.AssertSubtype<Texture, D3D11Texture>(source);
            var d3d11Destination = Util.AssertSubtype<Texture, D3D11Texture>(destination);
            DeviceContext.ResolveSubresource(
                d3d11Destination.DeviceTexture,
                0,
                d3d11Source.DeviceTexture,
                0,
                d3d11Destination.DxgiFormat);
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            var d3dFb = Util.AssertSubtype<Framebuffer, D3D11Framebuffer>(fb);

            if (d3dFb.Swapchain != null)
            {
                d3dFb.Swapchain.AddCommandListReference(this);
                referencedSwapchains.Add(d3dFb.Swapchain);
            }

            for (int i = 0; i < fb.ColorTargets.Count; i++) unbindSrvTexture(fb.ColorTargets[i].Target);

            DeviceContext.OMSetRenderTargets(d3dFb.RenderTargetViews, d3dFb.DepthStencilView);
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            var srcD3D11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(source);
            var dstD3D11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(destination);

            var region = new Box((int)sourceOffset, 0, 0, (int)(sourceOffset + sizeInBytes), 1, 1);

            DeviceContext.CopySubresourceRegion(dstD3D11Buffer.Buffer, 0, (int)destinationOffset, 0, 0, srcD3D11Buffer.Buffer, 0, region);
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
            var srcD3D11Texture = Util.AssertSubtype<Texture, D3D11Texture>(source);
            var dstD3D11Texture = Util.AssertSubtype<Texture, D3D11Texture>(destination);

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
                int srcSubresource = D3D11Util.ComputeSubresource(srcMipLevel, source.MipLevels, srcBaseArrayLayer + i);
                int dstSubresource = D3D11Util.ComputeSubresource(dstMipLevel, destination.MipLevels, dstBaseArrayLayer + i);

                DeviceContext.CopySubresourceRegion(
                    dstD3D11Texture.DeviceTexture,
                    dstSubresource,
                    (int)dstX,
                    (int)dstY,
                    (int)dstZ,
                    srcD3D11Texture.DeviceTexture,
                    srcSubresource,
                    region);
            }
        }

        private void clearState()
        {
            ClearCachedState();
            DeviceContext.ClearState();
            resetManagedState();
        }

        private void resetManagedState()
        {
            numVertexBindings = 0;
            Util.ClearArray(vertexBindings);
            Util.ClearArray(vertexStrides);
            Util.ClearArray(vertexOffsets);

            Framebuffer = null;

            Util.ClearArray(viewports);
            Util.ClearArray(scissors);
            viewportsChanged = false;
            scissorRectsChanged = false;

            ib = null;
            graphicsPipeline = null;
            blendState = null;
            depthStencilState = null;
            rasterizerState = null;
            primitiveTopology = Vortice.Direct3D.PrimitiveTopology.Undefined;
            inputLayout = null;
            vertexShader = null;
            geometryShader = null;
            hullShader = null;
            domainShader = null;
            pixelShader = null;

            clearSets(graphicsResourceSets);

            Util.ClearArray(vertexBoundUniformBuffers);
            Util.ClearArray(vertexBoundTextureViews);
            Util.ClearArray(vertexBoundSamplers);

            Util.ClearArray(fragmentBoundUniformBuffers);
            Util.ClearArray(fragmentBoundTextureViews);
            Util.ClearArray(fragmentBoundSamplers);

            computePipeline = null;
            clearSets(computeResourceSets);

            foreach (var kvp in boundSRVs)
            {
                var list = kvp.Value;
                list.Clear();
                poolBoundTextureList(list);
            }

            boundSRVs.Clear();

            foreach (var kvp in boundUaVs)
            {
                var list = kvp.Value;
                list.Clear();
                poolBoundTextureList(list);
            }

            boundUaVs.Clear();
        }

        private void clearSets(BoundResourceSetInfo[] boundSets)
        {
            foreach (var boundSetInfo in boundSets) boundSetInfo.Offsets.Dispose();
            Util.ClearArray(boundSets);
        }

        private void activateResourceSet(uint slot, BoundResourceSetInfo brsi, bool graphics)
        {
            var d3d11Rs = Util.AssertSubtype<ResourceSet, D3D11ResourceSet>(brsi.Set);

            int cbBase = getConstantBufferBase(slot, graphics);
            int uaBase = getUnorderedAccessBase(slot, graphics);
            int textureBase = getTextureBase(slot, graphics);
            int samplerBase = getSamplerBase(slot, graphics);

            var layout = d3d11Rs.Layout;
            var resources = d3d11Rs.Resources;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];
                uint bufferOffset = 0;

                if (layout.IsDynamicBuffer(i))
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                var rbi = layout.GetDeviceSlotIndex(i);

                switch (rbi.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    {
                        var range = getBufferRange(resource, bufferOffset);
                        bindUniformBuffer(range, cbBase + rbi.Slot, rbi.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        var range = getBufferRange(resource, bufferOffset);
                        bindStorageBufferView(range, textureBase + rbi.Slot, rbi.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        var range = getBufferRange(resource, bufferOffset);
                        var uav = range.Buffer.GetUnorderedAccessView(range.Offset, range.Size);
                        bindUnorderedAccessView(null, range.Buffer, uav, uaBase + rbi.Slot, rbi.Stages, slot);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        var texView = Util.GetTextureView(gd, resource);
                        var d3d11TexView = Util.AssertSubtype<TextureView, D3D11TextureView>(texView);
                        unbindUavTexture(d3d11TexView.Target);
                        bindTextureView(d3d11TexView, textureBase + rbi.Slot, rbi.Stages, slot);
                        break;

                    case ResourceKind.TextureReadWrite:
                        var rwTexView = Util.GetTextureView(gd, resource);
                        var d3d11RwTexView = Util.AssertSubtype<TextureView, D3D11TextureView>(rwTexView);
                        unbindSrvTexture(d3d11RwTexView.Target);
                        bindUnorderedAccessView(d3d11RwTexView.Target, null, d3d11RwTexView.UnorderedAccessView, uaBase + rbi.Slot, rbi.Stages, slot);
                        break;

                    case ResourceKind.Sampler:
                        var sampler = Util.AssertSubtype<IBindableResource, D3D11Sampler>(resource);
                        bindSampler(sampler, samplerBase + rbi.Slot, rbi.Stages);
                        break;

                    default: throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private D3D11BufferRange getBufferRange(IBindableResource resource, uint additionalOffset)
        {
            if (resource is D3D11Buffer d3d11Buff)
                return new D3D11BufferRange(d3d11Buff, additionalOffset, d3d11Buff.SizeInBytes);

            if (resource is DeviceBufferRange range)
            {
                return new D3D11BufferRange(
                    Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(range.Buffer),
                    range.Offset + additionalOffset,
                    range.SizeInBytes);
            }

            throw new VeldridException($"Unexpected resource type used in a buffer type slot: {resource.GetType().Name}");
        }

        private void unbindSrvTexture(Texture target)
        {
            if (boundSRVs.TryGetValue(target, out var btis))
            {
                foreach (var bti in btis)
                {
                    bindTextureView(null, bti.Slot, bti.Stages, 0);

                    if ((bti.Stages & ShaderStages.Compute) == ShaderStages.Compute)
                        invalidatedComputeResourceSets[bti.ResourceSet] = true;
                    else
                        invalidatedGraphicsResourceSets[bti.ResourceSet] = true;
                }

                bool result = boundSRVs.Remove(target);
                Debug.Assert(result);

                btis.Clear();
                poolBoundTextureList(btis);
            }
        }

        private void poolBoundTextureList(List<BoundTextureInfo> btis)
        {
            boundTextureInfoPool.Add(btis);
        }

        private void unbindUavTexture(Texture target)
        {
            if (boundUaVs.TryGetValue(target, out var btis))
            {
                foreach (var bti in btis)
                {
                    bindUnorderedAccessView(null, null, null, bti.Slot, bti.Stages, bti.ResourceSet);
                    if ((bti.Stages & ShaderStages.Compute) == ShaderStages.Compute)
                        invalidatedComputeResourceSets[bti.ResourceSet] = true;
                    else
                        invalidatedGraphicsResourceSets[bti.ResourceSet] = true;
                }

                bool result = boundUaVs.Remove(target);
                Debug.Assert(result);

                btis.Clear();
                poolBoundTextureList(btis);
            }
        }

        private int getConstantBufferBase(uint slot, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].UniformBufferCount;
            }

            return ret;
        }

        private int getUnorderedAccessBase(uint slot, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].StorageBufferCount;
            }

            return ret;
        }

        private int getTextureBase(uint slot, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].TextureCount;
            }

            return ret;
        }

        private int getSamplerBase(uint slot, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            int ret = 0;

            for (int i = 0; i < slot; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].SamplerCount;
            }

            return ret;
        }

        private void preDrawCommand()
        {
            flushViewports();
            flushScissorRects();
            flushVertexBindings();

            int graphicsResourceCount = graphicsPipeline.ResourceLayouts.Length;

            for (uint i = 0; i < graphicsResourceCount; i++)
            {
                if (invalidatedGraphicsResourceSets[i])
                {
                    invalidatedGraphicsResourceSets[i] = false;
                    activateResourceSet(i, graphicsResourceSets[i], true);
                }
            }
        }

        private void preDispatchCommand()
        {
            int computeResourceCount = computePipeline.ResourceLayouts.Length;

            for (uint i = 0; i < computeResourceCount; i++)
            {
                if (invalidatedComputeResourceSets[i])
                {
                    invalidatedComputeResourceSets[i] = false;
                    activateResourceSet(i, computeResourceSets[i], false);
                }
            }
        }

        private void flushViewports()
        {
            if (viewportsChanged)
            {
                viewportsChanged = false;
                DeviceContext.RSSetViewports(viewports);
            }
        }

        private void flushScissorRects()
        {
            if (scissorRectsChanged)
            {
                scissorRectsChanged = false;

                if (scissors.Length > 0)
                {
                    // Because this array is resized using Util.EnsureMinimumArraySize, this might set more scissor rectangles
                    // than are actually needed, but this is okay -- extras are essentially ignored and should be harmless.
                    DeviceContext.RSSetScissorRects(scissors);
                }
            }
        }

        private void flushVertexBindings()
        {
            if (vertexBindingsChanged)
            {
                DeviceContext.IASetVertexBuffers(
                    0, (int)numVertexBindings,
                    vertexBindings,
                    vertexStrides,
                    vertexOffsets);

                vertexBindingsChanged = false;
            }
        }

        private void bindTextureView(D3D11TextureView texView, int slot, ShaderStages stages, uint resourceSet)
        {
            var srv = texView?.ShaderResourceView;

            if (srv != null)
            {
                if (!boundSRVs.TryGetValue(texView.Target, out var list))
                {
                    list = getNewOrCachedBoundTextureInfoList();
                    boundSRVs.Add(texView.Target, list);
                }

                list.Add(new BoundTextureInfo { Slot = slot, Stages = stages, ResourceSet = resourceSet });
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                bool bind = false;

                if (slot < max_cached_uniform_buffers)
                {
                    if (vertexBoundTextureViews[slot] != texView)
                    {
                        vertexBoundTextureViews[slot] = texView;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind) DeviceContext.VSSetShaderResource(slot, srv);
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry) DeviceContext.GSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl) DeviceContext.HSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation) DeviceContext.DSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                bool bind = false;

                if (slot < max_cached_uniform_buffers)
                {
                    if (fragmentBoundTextureViews[slot] != texView)
                    {
                        fragmentBoundTextureViews[slot] = texView;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind) DeviceContext.PSSetShaderResource(slot, srv!);
            }

            if ((stages & ShaderStages.Compute) == ShaderStages.Compute) DeviceContext.CSSetShaderResource(slot, srv);
        }

        private List<BoundTextureInfo> getNewOrCachedBoundTextureInfoList()
        {
            if (boundTextureInfoPool.Count > 0)
            {
                int index = boundTextureInfoPool.Count - 1;
                var ret = boundTextureInfoPool[index];
                boundTextureInfoPool.RemoveAt(index);
                return ret;
            }

            return new List<BoundTextureInfo>();
        }

        private void bindStorageBufferView(D3D11BufferRange range, int slot, ShaderStages stages)
        {
            bool compute = (stages & ShaderStages.Compute) != 0;
            unbindUavBuffer(range.Buffer);

            var srv = range.Buffer.GetShaderResourceView(range.Offset, range.Size);

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex) DeviceContext.VSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry) DeviceContext.GSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl) DeviceContext.HSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation) DeviceContext.DSSetShaderResource(slot, srv);

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment) DeviceContext.PSSetShaderResource(slot, srv);

            if (compute) DeviceContext.CSSetShaderResource(slot, srv);
        }

        private void bindUniformBuffer(D3D11BufferRange range, int slot, ShaderStages stages)
        {
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                bool bind = false;

                if (slot < max_cached_uniform_buffers)
                {
                    if (!vertexBoundUniformBuffers[slot].Equals(range))
                    {
                        vertexBoundUniformBuffers[slot] = range;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind)
                {
                    if (range.IsFullRange)
                        DeviceContext.VSSetConstantBuffer(slot, range.Buffer.Buffer);
                    else
                    {
                        packRangeParams(range);
                        if (!gd.SupportsCommandLists) DeviceContext.VSUnsetConstantBuffer(slot);
                        context1.VSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                    }
                }
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry)
            {
                if (range.IsFullRange)
                    DeviceContext.GSSetConstantBuffer(slot, range.Buffer.Buffer);
                else
                {
                    packRangeParams(range);
                    if (!gd.SupportsCommandLists) DeviceContext.GSUnsetConstantBuffer(slot);
                    context1.GSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                }
            }

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl)
            {
                if (range.IsFullRange)
                    DeviceContext.HSSetConstantBuffer(slot, range.Buffer.Buffer);
                else
                {
                    packRangeParams(range);
                    if (!gd.SupportsCommandLists) DeviceContext.HSUnsetConstantBuffer(slot);
                    context1.HSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                }
            }

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation)
            {
                if (range.IsFullRange)
                    DeviceContext.DSSetConstantBuffer(slot, range.Buffer.Buffer);
                else
                {
                    packRangeParams(range);
                    if (!gd.SupportsCommandLists) DeviceContext.DSUnsetConstantBuffer(slot);
                    context1.DSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                }
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                bool bind = false;

                if (slot < max_cached_uniform_buffers)
                {
                    if (!fragmentBoundUniformBuffers[slot].Equals(range))
                    {
                        fragmentBoundUniformBuffers[slot] = range;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind)
                {
                    if (range.IsFullRange)
                        DeviceContext.PSSetConstantBuffer(slot, range.Buffer.Buffer);
                    else
                    {
                        packRangeParams(range);
                        if (!gd.SupportsCommandLists) DeviceContext.PSUnsetConstantBuffer(slot);
                        context1.PSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                    }
                }
            }

            if ((stages & ShaderStages.Compute) == ShaderStages.Compute)
            {
                if (range.IsFullRange)
                    DeviceContext.CSSetConstantBuffer(slot, range.Buffer.Buffer);
                else
                {
                    packRangeParams(range);
                    if (!gd.SupportsCommandLists) DeviceContext.CSSetConstantBuffer(slot, null);
                    context1.CSSetConstantBuffers1(slot, 1, cbOut, firstConstRef, numConstsRef);
                }
            }
        }

        private void packRangeParams(D3D11BufferRange range)
        {
            cbOut[0] = range.Buffer.Buffer;
            firstConstRef[0] = (int)range.Offset / 16;
            uint roundedSize = range.Size < 256 ? 256u : range.Size;
            numConstsRef[0] = (int)roundedSize / 16;
        }

        private void bindUnorderedAccessView(
            Texture texture,
            DeviceBuffer buffer,
            ID3D11UnorderedAccessView uav,
            int slot,
            ShaderStages stages,
            uint resourceSet)
        {
            bool compute = stages == ShaderStages.Compute;
            Debug.Assert(compute || (stages & ShaderStages.Compute) == 0);
            Debug.Assert(texture == null || buffer == null);

            if (texture != null && uav != null)
            {
                if (!boundUaVs.TryGetValue(texture, out var list))
                {
                    list = getNewOrCachedBoundTextureInfoList();
                    boundUaVs.Add(texture, list);
                }

                list.Add(new BoundTextureInfo { Slot = slot, Stages = stages, ResourceSet = resourceSet });
            }

            int baseSlot = 0;
            if (!compute && fragmentBoundSamplers != null) baseSlot = Framebuffer.ColorTargets.Count;
            int actualSlot = baseSlot + slot;

            if (buffer != null) trackBoundUavBuffer(buffer, actualSlot, compute);

            if (compute)
                DeviceContext.CSSetUnorderedAccessView(actualSlot, uav);
            else
                DeviceContext.OMSetUnorderedAccessView(actualSlot, uav!);
        }

        private void trackBoundUavBuffer(DeviceBuffer buffer, int slot, bool compute)
        {
            var list = compute ? boundComputeUavBuffers : boundOmuavBuffers;
            list.Add((buffer, slot));
        }

        private void unbindUavBuffer(DeviceBuffer buffer)
        {
            unbindUavBufferIndividual(buffer, false);
            unbindUavBufferIndividual(buffer, true);
        }

        private void unbindUavBufferIndividual(DeviceBuffer buffer, bool compute)
        {
            var list = compute ? boundComputeUavBuffers : boundOmuavBuffers;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Item1 == buffer)
                {
                    int slot = list[i].Item2;
                    if (compute)
                        DeviceContext.CSUnsetUnorderedAccessView(slot);
                    else
                        DeviceContext.OMUnsetUnorderedAccessView(slot);

                    list.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        private void bindSampler(D3D11Sampler sampler, int slot, ShaderStages stages)
        {
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                bool bind = false;

                if (slot < max_cached_samplers)
                {
                    if (vertexBoundSamplers[slot] != sampler)
                    {
                        vertexBoundSamplers[slot] = sampler;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind) DeviceContext.VSSetSampler(slot, sampler.DeviceSampler);
            }

            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry) DeviceContext.GSSetSampler(slot, sampler.DeviceSampler);

            if ((stages & ShaderStages.TessellationControl) == ShaderStages.TessellationControl) DeviceContext.HSSetSampler(slot, sampler.DeviceSampler);

            if ((stages & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation) DeviceContext.DSSetSampler(slot, sampler.DeviceSampler);

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                bool bind = false;

                if (slot < max_cached_samplers)
                {
                    if (fragmentBoundSamplers[slot] != sampler)
                    {
                        fragmentBoundSamplers[slot] = sampler;
                        bind = true;
                    }
                }
                else
                    bind = true;

                if (bind) DeviceContext.PSSetSampler(slot, sampler.DeviceSampler);
            }

            if ((stages & ShaderStages.Compute) == ShaderStages.Compute) DeviceContext.CSSetSampler(slot, sampler.DeviceSampler);
        }

        private unsafe void UpdateSubresource_Workaround(
            ID3D11Resource resource,
            int subresource,
            Box? region,
            IntPtr data)
        {
            bool needWorkaround = !gd.SupportsCommandLists;
            var pAdjustedSrcData = data.ToPointer();

            if (needWorkaround && region is Box dstRegion)
            {
                Debug.Assert(dstRegion.Top == 0 && dstRegion.Front == 0);
                pAdjustedSrcData = (byte*)data - dstRegion.Left;
            }

            DeviceContext.UpdateSubresource(resource, subresource, region, (IntPtr)pAdjustedSrcData, 0, 0);
        }

        private D3D11Buffer getFreeStagingBuffer(uint sizeInBytes)
        {
            foreach (var buffer in availableStagingBuffers)
            {
                if (buffer.SizeInBytes >= sizeInBytes)
                {
                    availableStagingBuffers.Remove(buffer);
                    return buffer;
                }
            }

            var staging = gd.ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(staging);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            if (ib != buffer || ibOffset != offset)
            {
                ib = buffer;
                ibOffset = offset;
                var d3d11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(buffer);
                unbindUavBuffer(buffer);
                DeviceContext.IASetIndexBuffer(d3d11Buffer.Buffer, D3D11Formats.ToDxgiFormat(format), (int)offset);
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (!pipeline.IsComputePipeline && graphicsPipeline != pipeline)
            {
                var d3dPipeline = Util.AssertSubtype<Pipeline, D3D11Pipeline>(pipeline);
                graphicsPipeline = d3dPipeline;
                clearSets(graphicsResourceSets); // Invalidate resource set bindings -- they may be invalid.
                Util.ClearArray(invalidatedGraphicsResourceSets);

                if (blendState != d3dPipeline.BlendState || blendFactor != d3dPipeline.BlendFactor)
                {
                    blendState = d3dPipeline.BlendState;
                    blendFactor = d3dPipeline.BlendFactor;
                    DeviceContext.OMSetBlendState(blendState, blendFactor);
                }

                if (depthStencilState != d3dPipeline.DepthStencilState || stencilReference != d3dPipeline.StencilReference)
                {
                    depthStencilState = d3dPipeline.DepthStencilState;
                    stencilReference = d3dPipeline.StencilReference;
                    DeviceContext.OMSetDepthStencilState(depthStencilState, (int)stencilReference);
                }

                if (rasterizerState != d3dPipeline.RasterizerState)
                {
                    rasterizerState = d3dPipeline.RasterizerState;
                    DeviceContext.RSSetState(rasterizerState);
                }

                if (primitiveTopology != d3dPipeline.PrimitiveTopology)
                {
                    primitiveTopology = d3dPipeline.PrimitiveTopology;
                    DeviceContext.IASetPrimitiveTopology();
                }

                if (inputLayout != d3dPipeline.InputLayout)
                {
                    inputLayout = d3dPipeline.InputLayout;
                    DeviceContext.IASetInputLayout(inputLayout);
                }

                if (vertexShader != d3dPipeline.VertexShader)
                {
                    vertexShader = d3dPipeline.VertexShader;
                    DeviceContext.VSSetShader(vertexShader);
                }

                if (geometryShader != d3dPipeline.GeometryShader)
                {
                    geometryShader = d3dPipeline.GeometryShader;
                    DeviceContext.GSSetShader(geometryShader);
                }

                if (hullShader != d3dPipeline.HullShader)
                {
                    hullShader = d3dPipeline.HullShader;
                    DeviceContext.HSSetShader(hullShader);
                }

                if (domainShader != d3dPipeline.DomainShader)
                {
                    domainShader = d3dPipeline.DomainShader;
                    DeviceContext.DSSetShader(domainShader);
                }

                if (pixelShader != d3dPipeline.PixelShader)
                {
                    pixelShader = d3dPipeline.PixelShader;
                    DeviceContext.PSSetShader(pixelShader);
                }

                if (!Util.ArrayEqualsEquatable(vertexStrides, d3dPipeline.VertexStrides))
                {
                    vertexBindingsChanged = true;

                    if (d3dPipeline.VertexStrides != null)
                    {
                        Util.EnsureArrayMinimumSize(ref vertexStrides, (uint)d3dPipeline.VertexStrides.Length);
                        d3dPipeline.VertexStrides.CopyTo(vertexStrides, 0);
                    }
                }

                Util.EnsureArrayMinimumSize(ref vertexStrides, 1);
                Util.EnsureArrayMinimumSize(ref vertexBindings, (uint)vertexStrides.Length);
                Util.EnsureArrayMinimumSize(ref vertexOffsets, (uint)vertexStrides.Length);

                Util.EnsureArrayMinimumSize(ref graphicsResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
                Util.EnsureArrayMinimumSize(ref invalidatedGraphicsResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
            }
            else if (pipeline.IsComputePipeline && computePipeline != pipeline)
            {
                var d3dPipeline = Util.AssertSubtype<Pipeline, D3D11Pipeline>(pipeline);
                computePipeline = d3dPipeline;
                clearSets(computeResourceSets); // Invalidate resource set bindings -- they may be invalid.
                Util.ClearArray(invalidatedComputeResourceSets);

                var computeShader = d3dPipeline.ComputeShader;
                DeviceContext.CSSetShader(computeShader);
                Util.EnsureArrayMinimumSize(ref computeResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
                Util.EnsureArrayMinimumSize(ref invalidatedComputeResourceSets, (uint)d3dPipeline.ResourceLayouts.Length);
            }
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            var d3d11Buffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(buffer);

            if (vertexBindings[index] != d3d11Buffer.Buffer || vertexOffsets[index] != offset)
            {
                vertexBindingsChanged = true;
                unbindUavBuffer(buffer);
                vertexBindings[index] = d3d11Buffer.Buffer;
                vertexOffsets[index] = (int)offset;
                numVertexBindings = Math.Max(index + 1, numVertexBindings);
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            preDrawCommand();

            if (instanceCount == 1 && instanceStart == 0)
                DeviceContext.Draw((int)vertexCount, (int)vertexStart);
            else
                DeviceContext.DrawInstanced((int)vertexCount, (int)instanceCount, (int)vertexStart, (int)instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            preDrawCommand();

            Debug.Assert(ib != null);
            if (instanceCount == 1 && instanceStart == 0)
                DeviceContext.DrawIndexed((int)indexCount, (int)indexStart, vertexOffset);
            else
                DeviceContext.DrawIndexedInstanced((int)indexCount, (int)instanceCount, (int)indexStart, vertexOffset, (int)instanceStart);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            DeviceContext.ClearRenderTargetView(d3D11Framebuffer.RenderTargetViews[index], new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            DeviceContext.ClearDepthStencilView(d3D11Framebuffer.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, depth, stencil);
        }

        private protected override unsafe void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            var d3dBuffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(buffer);
            if (sizeInBytes == 0) return;

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
                    subregion = null;

                if (bufferOffsetInBytes == 0)
                    DeviceContext.UpdateSubresource(d3dBuffer.Buffer, 0, subregion, source, 0, 0);
                else
                    UpdateSubresource_Workaround(d3dBuffer.Buffer, 0, subregion, source);
            }
            else if (useMap && updateFullBuffer) // Can only update full buffer with WriteDiscard.
            {
                var msb = DeviceContext.Map(
                    d3dBuffer.Buffer,
                    0,
                    D3D11Formats.VdToD3D11MapMode(true, MapMode.Write));
                if (sizeInBytes < 1024)
                    Unsafe.CopyBlock(msb.DataPointer.ToPointer(), source.ToPointer(), sizeInBytes);
                else
                    Buffer.MemoryCopy(source.ToPointer(), msb.DataPointer.ToPointer(), buffer.SizeInBytes, sizeInBytes);
                DeviceContext.Unmap(d3dBuffer.Buffer, 0);
            }
            else
            {
                var staging = getFreeStagingBuffer(sizeInBytes);
                gd.UpdateBuffer(staging, 0, source, sizeInBytes);
                CopyBuffer(staging, 0, buffer, bufferOffsetInBytes, sizeInBytes);
                submittedStagingBuffers.Add(staging);
            }
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            var fullTexView = texture.GetFullTextureView(gd);
            var d3d11View = Util.AssertSubtype<TextureView, D3D11TextureView>(fullTexView);
            var srv = d3d11View.ShaderResourceView;
            DeviceContext.GenerateMips(srv);
        }

        private protected override void PushDebugGroupCore(string name)
        {
            uda?.BeginEvent(name);
        }

        private protected override void PopDebugGroupCore()
        {
            uda?.EndEvent();
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            uda?.SetMarker(name);
        }

        private struct BoundTextureInfo
        {
            public int Slot;
            public ShaderStages Stages;
            public uint ResourceSet;
        }

        private struct D3D11BufferRange : IEquatable<D3D11BufferRange>
        {
            public readonly D3D11Buffer Buffer;
            public readonly uint Offset;
            public readonly uint Size;

            public bool IsFullRange => Offset == 0 && Size == Buffer.SizeInBytes;

            public D3D11BufferRange(D3D11Buffer buffer, uint offset, uint size)
            {
                Buffer = buffer;
                Offset = offset;
                Size = size;
            }

            public bool Equals(D3D11BufferRange other)
            {
                return Buffer == other.Buffer && Offset.Equals(other.Offset) && Size.Equals(other.Size);
            }
        }
    }
}

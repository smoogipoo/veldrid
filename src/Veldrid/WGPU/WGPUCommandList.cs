// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUCommandList : CommandList
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;
        private WGPUCommandEncoder encoder;
        private WGPUCommandBuffer commandBuffer;

        private WGPURenderPassEncoder renderPass;
        private WGPUComputePassEncoder computePass;

        private WGPUPipeline graphicsPipeline;
        private WGPUPipeline computePipeline;
        private WGPUPipeline lastGraphicsPipeline;
        private WGPUPipeline lastComputePipeline;

        private WGPUColor[] clearColourValues = Array.Empty<WGPUColor>();
        private bool[] clearColourValuesValid = Array.Empty<bool>();

        private BoundResourceSetInfo[] graphicsResourceSets;
        private bool[] graphicsResourceSetsActive = Array.Empty<bool>();
        private uint graphicsResourceSetCount;

        private BoundResourceSetInfo[] computeResourceSets;
        private bool[] computeResourceSetsActive = Array.Empty<bool>();
        private uint computeResourceSetCount;

        private WGPUBuffer[] vertexBuffers;
        private uint[] vertexBufferOffsets;
        private bool[] vertexBuffersActive;
        private uint vertexBufferCount;

        private WGPUBuffer indexBuffer;
        private WGPUIndexFormat indexBufferFormat;
        private uint indexBufferOffset;
        private bool indexBufferValid;

        private Viewport viewportRect;
        private bool viewportRectValid;

        private readonly uint[] scissorRect = new uint[4];
        private bool scissorRectValid;

        private float? clearDepthValue;
        private byte? clearStencilValue;

        public WGPUCommandList(WGPUGraphicsDevice gd, ref CommandListDescription description)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            this.gd = gd;
        }

        public override void Begin()
        {
            encoder = wgpuDeviceCreateCommandEncoder(gd.NativeDevice);
        }

        public override void End()
        {
            endRenderPass();
            endComputePass();

            commandBuffer = wgpuCommandEncoderFinish(encoder);
            wgpuCommandEncoderRelease(encoder);

            resetState();
        }

        public WGPUCommandBuffer ConsumeCommandBuffer()
        {
            if (commandBuffer.IsNull)
                throw new VeldridException("CommandList.End() has not been called.");

            WGPUCommandBuffer buffer = commandBuffer;
            commandBuffer = default;
            return buffer;
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            viewportRect = viewport;
            viewportRectValid = false;
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            scissorRect[0] = x;
            scissorRect[1] = y;
            scissorRect[2] = width;
            scissorRect[3] = height;
            scissorRectValid = false;
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (graphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets))
                return;

            graphicsResourceSets[slot].Offsets.Dispose();
            graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            graphicsResourceSetsActive[slot] = false;
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (computeResourceSets[slot].Equals(set, dynamicOffsetsCount, ref dynamicOffsets))
                return;

            computeResourceSets[slot].Offsets.Dispose();
            computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
            computeResourceSetsActive[slot] = false;
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            endRenderPass();

            Util.EnsureArrayMinimumSize(ref clearColourValues, (uint)fb.ColorTargets.Count);
            Util.EnsureArrayMinimumSize(ref clearColourValuesValid, (uint)fb.ColorTargets.Count);

            scissorRectValid = false;
            viewportRectValid = false;
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            preDrawCommand();
            wgpuRenderPassEncoderDrawIndirect(renderPass, wgpuBuffer.Buffer, offset);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            preDrawCommand();
            wgpuRenderPassEncoderDrawIndexedIndirect(renderPass, wgpuBuffer.Buffer, offset);
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            preComputeCommand();
            wgpuComputePassEncoderDispatchWorkgroups(computePass, groupCountX, groupCountY, groupCountZ);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            preComputeCommand();
            wgpuComputePassEncoderDispatchWorkgroupsIndirect(computePass, wgpuBuffer.Buffer, offset);
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            WGPUTexture wgpuSrc = Util.AssertSubtype<Texture, WGPUTexture>(source);
            WGPUTexture wgpuDst = Util.AssertSubtype<Texture, WGPUTexture>(destination);

            endRenderPass();

            WGPUImageCopyTexture src = new WGPUImageCopyTexture
            {
                texture = wgpuSrc.Texture,
                mipLevel = 0,
                origin = new WGPUOrigin3D(0, 0, 0),
                aspect = WGPUTextureAspect.All
            };

            WGPUImageCopyTexture dest = new WGPUImageCopyTexture
            {
                texture = wgpuDst.Texture,
                mipLevel = 0,
                origin = new WGPUOrigin3D(0, 0, 0),
                aspect = WGPUTextureAspect.All
            };

            WGPUExtent3D writeSize = new WGPUExtent3D(source.Width, source.Height, wgpuSrc.ActualArrayLayers * wgpuSrc.Depth);

            wgpuCommandEncoderCopyTextureToTexture(encoder, &src, &dest, &writeSize);
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            WGPUBuffer wgpuSrc = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(source);
            WGPUBuffer wgpuDst = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(destination);

            wgpuCommandEncoderCopyBufferToBuffer(encoder, wgpuSrc.Buffer, sourceOffset, wgpuDst.Buffer, destinationOffset, sizeInBytes);
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ,
                                                uint dstMipLevel,
                                                uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
            WGPUTexture wgpuSrc = Util.AssertSubtype<Texture, WGPUTexture>(source);
            WGPUTexture wgpuDst = Util.AssertSubtype<Texture, WGPUTexture>(destination);

            WGPUImageCopyTexture src = new WGPUImageCopyTexture
            {
                texture = wgpuSrc.Texture,
                mipLevel = srcMipLevel,
                origin = new WGPUOrigin3D(srcX, srcY, srcZ),
                aspect = WGPUTextureAspect.All
            };

            WGPUImageCopyTexture dest = new WGPUImageCopyTexture
            {
                texture = wgpuDst.Texture,
                mipLevel = dstMipLevel,
                origin = new WGPUOrigin3D(dstX, dstY, dstZ),
                aspect = WGPUTextureAspect.All
            };

            WGPUExtent3D writeSize = new WGPUExtent3D(width, height, depth * layerCount);

            wgpuCommandEncoderCopyTextureToTexture(encoder, &src, &dest, &writeSize);
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline && computePipeline != pipeline)
            {
                computePipeline = Util.AssertSubtype<Pipeline, WGPUPipeline>(pipeline);

                computeResourceSetCount = (uint)pipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref computeResourceSets, computeResourceSetCount);
                Util.EnsureArrayMinimumSize(ref computeResourceSetsActive, computeResourceSetCount);
                Util.ClearArray(computeResourceSetsActive);
            }
            else if (!pipeline.IsComputePipeline && graphicsPipeline != pipeline)
            {
                graphicsPipeline = Util.AssertSubtype<Pipeline, WGPUPipeline>(pipeline);

                graphicsResourceSetCount = (uint)pipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref graphicsResourceSets, graphicsResourceSetCount);
                Util.EnsureArrayMinimumSize(ref graphicsResourceSetsActive, graphicsResourceSetCount);
                Util.ClearArray(graphicsResourceSetsActive);

                vertexBufferCount = graphicsPipeline.VertexBufferCount;
                Util.EnsureArrayMinimumSize(ref vertexBuffers, vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref vertexBufferOffsets, vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref vertexBuffersActive, vertexBufferCount);
                Util.ClearArray(vertexBuffersActive);
            }
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            Util.EnsureArrayMinimumSize(ref vertexBuffers, index + 1);
            Util.EnsureArrayMinimumSize(ref vertexBufferOffsets, index + 1);
            Util.EnsureArrayMinimumSize(ref vertexBuffersActive, index + 1);

            if (vertexBuffers[index] != buffer || vertexBufferOffsets[index] != offset)
            {
                vertexBuffers[index] = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(buffer);
                vertexBufferOffsets[index] = offset;
                vertexBuffersActive[index] = false;
            }
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            WGPUIndexFormat wgpuFormat = WGPUFormats.VdToWGPUIndexFormat(format);

            if (indexBuffer == buffer && indexBufferFormat == wgpuFormat && indexBufferOffset == offset)
                return;

            indexBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(buffer);
            indexBufferFormat = wgpuFormat;
            indexBufferOffset = offset;
            indexBufferValid = false;
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            endRenderPass();

            clearColourValues[index] = new WGPUColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            clearColourValuesValid[index] = true;
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            endRenderPass();

            clearDepthValue = depth;
            clearStencilValue = stencil;
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            preDrawCommand();
            wgpuRenderPassEncoderDraw(renderPass, vertexCount, instanceCount, vertexStart, instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            preDrawCommand();
            wgpuRenderPassEncoderDrawIndexed(renderPass, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            gd.UpdateBuffer(buffer, bufferOffsetInBytes, source, sizeInBytes);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            const string mipmap_shader_name = "WGPU_Mipmap";

            using var resourceStream = typeof(WGPUGraphicsDevice).Assembly.GetManifestResourceStream(mipmap_shader_name)!;
            using var ms = new MemoryStream((int)resourceStream.Length);
            resourceStream.CopyTo(ms);

            Shader shader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, ms.GetBuffer(), "mipmap"));
            ResourceLayout layout = gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription
            {
                Elements = new[]
                {
                    new ResourceLayoutElementDescription("previousLevel", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("nextLevel", ResourceKind.TextureWriteOnly, ShaderStages.Compute),
                }
            });

            Pipeline pipeline = gd.ResourceFactory.CreateComputePipeline(new ComputePipelineDescription(shader, [layout], 8, 8, 1));

            TextureView[] views = new TextureView[texture.MipLevels];
            ResourceSet[] resourceSets = new ResourceSet[texture.MipLevels];

            for (int i = 0; i < views.Length; i++)
            {
                views[i] = gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, texture.Format, (uint)i, 1, 0, 1));
                if (i > 0)
                    resourceSets[i] = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, views[i - 1], views[i]));
            }

            SetPipeline(pipeline);

            uint width = texture.Width;
            uint height = texture.Height;

            for (int level = 1; level < views.Length; level++)
            {
                width /= 2;
                height /= 2;

                const uint wg_size = 8;
                uint wgCountX = (width + wg_size - 1) / wg_size;
                uint wgCountY = (height + wg_size - 1) / wg_size;

                SetComputeResourceSet(0, resourceSets[level]);
                Dispatch(wgCountX, wgCountY, 1);
            }

            endComputePass();

            foreach (var set in resourceSets)
                set?.Dispose();

            foreach (var view in views)
                view?.Dispose();

            pipeline?.Dispose();
            shader?.Dispose();
        }

        private protected override void PushDebugGroupCore(string name)
        {
            if (renderPass.IsNotNull)
                wgpuRenderPassEncoderPushDebugGroup(renderPass, name.GetUtf8Span());
            else if (computePass.IsNotNull)
                wgpuComputePassEncoderPushDebugGroup(computePass, name.GetUtf8Span());
            else if (encoder.IsNotNull)
                wgpuCommandEncoderPushDebugGroup(encoder, name.GetUtf8Span());
        }

        private protected override void PopDebugGroupCore()
        {
            if (renderPass.IsNotNull)
                wgpuRenderPassEncoderPopDebugGroup(renderPass);
            else if (computePass.IsNotNull)
                wgpuComputePassEncoderPopDebugGroup(computePass);
            else if (encoder.IsNotNull)
                wgpuCommandEncoderPopDebugGroup(encoder);
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            if (renderPass.IsNotNull)
                wgpuRenderPassEncoderInsertDebugMarker(renderPass, name.GetUtf8Span());
            else if (computePass.IsNotNull)
                wgpuComputePassEncoderInsertDebugMarker(computePass, name.GetUtf8Span());
            else if (encoder.IsNotNull)
                wgpuCommandEncoderInsertDebugMarker(encoder, name.GetUtf8Span());
        }

        private void preDrawCommand()
        {
            beginCurrentRenderPass();

            if (graphicsPipeline != lastGraphicsPipeline)
            {
                wgpuRenderPassEncoderSetPipeline(renderPass, graphicsPipeline.RenderPipeline);
                lastGraphicsPipeline = graphicsPipeline;
            }

            for (uint i = 0; i < graphicsResourceSetCount; i++)
            {
                if (graphicsResourceSetsActive[i])
                    continue;

                activateResourceSet(i, graphicsResourceSets[i], true);
                graphicsResourceSetsActive[i] = true;
            }

            for (uint i = 0; i < vertexBufferCount; i++)
            {
                if (vertexBuffersActive[i])
                    continue;

                wgpuRenderPassEncoderSetVertexBuffer(renderPass, i, vertexBuffers[i].Buffer, vertexBufferOffsets[i], vertexBuffers[i].SizeInBytes - vertexBufferOffsets[i]);
                vertexBuffersActive[i] = true;
            }

            if (!indexBufferValid)
            {
                wgpuRenderPassEncoderSetIndexBuffer(renderPass, indexBuffer.Buffer, indexBufferFormat, indexBufferOffset, indexBuffer.SizeInBytes - indexBufferOffset);
                indexBufferValid = true;
            }

            if (!viewportRectValid)
            {
                // See validation rules: https://developer.mozilla.org/en-US/docs/Web/API/GPURenderPassEncoder/setVertexBuffer

                // x >= 0, x <= fb_width
                float x = Math.Clamp(viewportRect.X, 0, Framebuffer.Width);

                // y >= 0, y <= fb_height
                float y = Math.Clamp(viewportRect.Y, 0, Framebuffer.Height);

                // width >= 0, x + width <= fb_width
                float width = Math.Clamp(viewportRect.Width, 0, Framebuffer.Width - x);

                // height >= 0, y + height <= fb_height
                float height = Math.Clamp(viewportRect.Height, 0, Framebuffer.Height - y);

                // maxDepth in [0, 1]
                float maxDepth = Math.Clamp(viewportRect.MaxDepth, 0, 1);

                // minDepth in [0, 1], minDepth less than maxDepth
                float minDepth = Math.Clamp(viewportRect.MinDepth, 0, maxDepth - 0.0001f);

                wgpuRenderPassEncoderSetViewport(renderPass, x, y, width, height, minDepth, maxDepth);
                viewportRectValid = true;
            }

            if (!scissorRectValid)
            {
                // See validation rules: https://developer.mozilla.org/en-US/docs/Web/API/GPURenderPassEncoder/setScissorRect

                // x <= fb_width
                uint x = Math.Min(scissorRect[0], Framebuffer.Width);

                // y <= fb_width
                uint y = Math.Min(scissorRect[1], Framebuffer.Height);

                // x + width <= fb_width
                uint width = Math.Min(scissorRect[2], Framebuffer.Width - x);

                // y + height <= fb_height
                uint height = Math.Min(scissorRect[3], Framebuffer.Height - y);

                wgpuRenderPassEncoderSetScissorRect(renderPass, x, y, width, height);
                scissorRectValid = true;
            }
        }

        private void beginCurrentRenderPass()
        {
            if (renderPass.IsNotNull)
                return;

            if (computePass.IsNotNull)
                endComputePass();

            WGPURenderPassColorAttachment* colourAttachments = stackalloc WGPURenderPassColorAttachment[Framebuffer.ColorTargets.Count];

            for (int i = 0; i < Framebuffer.ColorTargets.Count; i++)
            {
                var texture = Util.AssertSubtype<Texture, WGPUTexture>(Framebuffer.ColorTargets[i].Target);
                var textureView = Util.AssertSubtype<TextureView, WGPUTextureViewBase>(texture.GetFullTextureView(gd));

                colourAttachments[i] = new WGPURenderPassColorAttachment
                {
                    view = textureView.View,
                    loadOp = clearColourValuesValid[i] ? WGPULoadOp.Clear : WGPULoadOp.Load,
                    storeOp = WGPUStoreOp.Store,
                    clearValue = new WGPUColor(clearColourValues[i].r, clearColourValues[i].g, clearColourValues[i].b, clearColourValues[i].a),
                };
            }

            WGPURenderPassDepthStencilAttachment depthStencilAttachment = default;

            if (Framebuffer.DepthTarget is FramebufferAttachment depthTarget)
            {
                var texture = Util.AssertSubtype<Texture, WGPUTexture>(depthTarget.Target);
                var textureView = Util.AssertSubtype<TextureView, WGPUTextureViewBase>(texture.GetFullTextureView(gd));

                depthStencilAttachment = new WGPURenderPassDepthStencilAttachment
                {
                    view = textureView.View,
                    depthLoadOp = clearDepthValue == null ? WGPULoadOp.Load : WGPULoadOp.Clear,
                    depthStoreOp = WGPUStoreOp.Store,
                    depthClearValue = clearDepthValue ?? 0,
                    stencilLoadOp = clearStencilValue == null ? WGPULoadOp.Load : WGPULoadOp.Clear,
                    stencilStoreOp = WGPUStoreOp.Store,
                    stencilClearValue = clearStencilValue ?? 0
                };
            }

            var renderPassDescriptor = new WGPURenderPassDescriptor
            {
                colorAttachmentCount = (uint)Framebuffer.ColorTargets.Count,
                colorAttachments = colourAttachments
            };

            if (Framebuffer.DepthTarget != null)
                renderPassDescriptor.depthStencilAttachment = &depthStencilAttachment;

            renderPass = wgpuCommandEncoderBeginRenderPass(encoder, &renderPassDescriptor);

            Util.ClearArray(clearColourValuesValid);
            clearDepthValue = null;
            clearStencilValue = null;
        }

        private void endRenderPass()
        {
            if (renderPass.IsNull)
                return;

            wgpuRenderPassEncoderEnd(renderPass);
            wgpuRenderPassEncoderRelease(renderPass);

            renderPass = default;

            lastGraphicsPipeline = null;
            Util.ClearArray(graphicsResourceSetsActive);
            Util.ClearArray(vertexBuffersActive);
            indexBufferValid = false;
            viewportRectValid = false;
            scissorRectValid = false;
        }

        private void preComputeCommand()
        {
            beginCurrentComputePass();

            if (computePipeline != lastComputePipeline)
            {
                wgpuComputePassEncoderSetPipeline(computePass, computePipeline.ComputePipeline);
                lastComputePipeline = computePipeline;
            }

            for (uint i = 0; i < computeResourceSetCount; i++)
            {
                if (computeResourceSetsActive[i])
                    continue;

                activateResourceSet(i, computeResourceSets[i], false);
                computeResourceSetsActive[i] = true;
            }
        }

        private void beginCurrentComputePass()
        {
            if (computePass.IsNotNull)
                return;

            if (renderPass.IsNotNull)
                endRenderPass();

            WGPUComputePassDescriptor computePassDescriptor;
            computePass = wgpuCommandEncoderBeginComputePass(encoder, &computePassDescriptor);
        }

        private void endComputePass()
        {
            if (computePass.IsNull)
                return;

            wgpuComputePassEncoderEnd(computePass);
            wgpuComputePassEncoderRelease(computePass);

            computePass = default;

            lastComputePipeline = null;
            Util.ClearArray(computeResourceSetsActive);
        }

        private void activateResourceSet(uint slot, BoundResourceSetInfo rsi, bool graphics)
        {
            WGPUResourceSet wgpuSet = Util.AssertSubtype<ResourceSet, WGPUResourceSet>(rsi.Set);

            int dynamicOffsetCount = rsi.Offsets.Count > 0 ? wgpuSet.Resources.Length : 0;
            uint* dynamicOffsets = stackalloc uint[dynamicOffsetCount];

            if (dynamicOffsetCount > 0)
            {
                uint currentOffsetIndex = 0;

                for (uint i = 0; i < wgpuSet.Resources.Length; i++)
                {
                    bool isDynamicBinding = (wgpuSet.Layout.Description.Elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding;

                    if (!isDynamicBinding)
                        continue;

                    dynamicOffsets[i] = rsi.Offsets.Get(currentOffsetIndex);
                    currentOffsetIndex++;
                }
            }

            if (graphics)
                wgpuRenderPassEncoderSetBindGroup(renderPass, slot, wgpuSet.BindGroup, (uint)dynamicOffsetCount, dynamicOffsets);
            else
                wgpuComputePassEncoderSetBindGroup(computePass, slot, wgpuSet.BindGroup, (uint)dynamicOffsetCount, dynamicOffsets);
        }

        private void resetState()
        {
            ClearCachedState();
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
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

        private WGPUColor[] clearColourValues = Array.Empty<WGPUColor>();
        private bool[] validClearColourValues = Array.Empty<bool>();
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
            beginRenderPass();
            wgpuRenderPassEncoderSetViewport(renderPass, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            beginRenderPass();
            wgpuRenderPassEncoderSetScissorRect(renderPass, x, y, width, height);
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            beginComputePass();
            wgpuComputePassEncoderDispatchWorkgroups(computePass, groupCountX, groupCountY, groupCountZ);
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            WGPUResourceSet wgpuResourceSet = Util.AssertSubtype<ResourceSet, WGPUResourceSet>(rs);

            beginRenderPass();
            wgpuRenderPassEncoderSetBindGroup(renderPass, slot, wgpuResourceSet.BindGroup, dynamicOffsetsCount, (uint*)Unsafe.AsPointer(ref dynamicOffsets));
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            WGPUResourceSet wgpuResourceSet = Util.AssertSubtype<ResourceSet, WGPUResourceSet>(set);

            beginComputePass();
            wgpuComputePassEncoderSetBindGroup(computePass, slot, wgpuResourceSet.BindGroup, dynamicOffsetsCount, (uint*)Unsafe.AsPointer(ref dynamicOffsets));
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            endRenderPass();

            Util.EnsureArrayMinimumSize(ref clearColourValues, (uint)fb.ColorTargets.Count);
            Util.EnsureArrayMinimumSize(ref validClearColourValues, (uint)fb.ColorTargets.Count);

            beginRenderPass();
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            beginRenderPass();
            wgpuRenderPassEncoderDrawIndirect(renderPass, wgpuBuffer.Buffer, offset);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            beginRenderPass();
            wgpuRenderPassEncoderDrawIndexedIndirect(renderPass, wgpuBuffer.Buffer, offset);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(indirectBuffer);

            beginComputePass();
            wgpuComputePassEncoderDispatchWorkgroupsIndirect(computePass, wgpuBuffer.Buffer, offset);
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            throw new NotImplementedException();
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ,
                                                uint dstMipLevel,
                                                uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
            WGPUTexture wgpuSrc = Util.AssertSubtype<Texture, WGPUTexture>(source);
            WGPUTexture wgpuDst = Util.AssertSubtype<Texture, WGPUTexture>(destination);

            // Todo: array layers?

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

            WGPUExtent3D writeSize = new WGPUExtent3D(width, height, depth);

            wgpuCommandEncoderCopyTextureToTexture(encoder, &src, &dest, &writeSize);
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            var wgpuPipeline = Util.AssertSubtype<Pipeline, WGPUPipeline>(pipeline);

            if (wgpuPipeline.IsComputePipeline)
            {
                beginComputePass();
                wgpuComputePassEncoderSetPipeline(computePass, wgpuPipeline.ComputePipeline);
            }
            else
            {
                beginRenderPass();
                wgpuRenderPassEncoderSetPipeline(renderPass, wgpuPipeline.RenderPipeline);
            }
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            var wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(buffer);

            beginRenderPass();
            wgpuRenderPassEncoderSetVertexBuffer(renderPass, index, wgpuBuffer.Buffer, offset, buffer.SizeInBytes - offset);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            var wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(buffer);

            beginRenderPass();
            wgpuRenderPassEncoderSetIndexBuffer(renderPass, wgpuBuffer.Buffer, WGPUFormats.VdToWGPUIndexFormat(format), offset, buffer.SizeInBytes - offset);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            clearColourValues[index] = new WGPUColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            validClearColourValues[index] = true;

            if (renderPass.IsNotNull)
            {
                endRenderPass();
                beginRenderPass();
            }
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            clearDepthValue = depth;
            clearStencilValue = stencil;

            if (renderPass.IsNotNull)
            {
                endRenderPass();
                beginRenderPass();
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            beginRenderPass();
            wgpuRenderPassEncoderDraw(renderPass, vertexCount, instanceCount, vertexStart, instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            beginRenderPass();
            wgpuRenderPassEncoderDrawIndexed(renderPass, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            gd.UpdateBuffer(buffer, bufferOffsetInBytes, source, sizeInBytes);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
        }

        private protected override void PushDebugGroupCore(string name)
        {
        }

        private protected override void PopDebugGroupCore()
        {
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
        }

        private void beginRenderPass()
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
                    loadOp = validClearColourValues[i] ? WGPULoadOp.Clear : WGPULoadOp.Load,
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

            Util.ClearArray(validClearColourValues);
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
        }

        private void beginComputePass()
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

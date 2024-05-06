// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUCommandList : CommandList
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;
        private CommandEncoder* encoder;
        private CommandBuffer* commandBuffer;

        private RenderPassEncoder* renderPass;

        private Color[] clearColourValues = Array.Empty<Color>();
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
            encoder = gd.WebGPU.DeviceCreateCommandEncoder(gd.NativeDevice, new CommandEncoderDescriptor());
        }

        public override void End()
        {
            endRenderPass();

            commandBuffer = gd.WebGPU.CommandEncoderFinish(encoder, new CommandBufferDescriptor());
            gd.WebGPU.CommandEncoderRelease(encoder);

            resetState();
        }

        public CommandBuffer* ConsumeCommandBuffer()
        {
            if (commandBuffer == null)
                throw new VeldridException("CommandList.End() has not been called.");

            CommandBuffer* buffer = commandBuffer;
            commandBuffer = null;
            return buffer;
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            throw new NotImplementedException();
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            throw new NotImplementedException();
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            throw new NotImplementedException();
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            throw new NotImplementedException();
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            throw new NotImplementedException();
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            throw new NotImplementedException();
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            throw new NotImplementedException();
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ,
                                                uint dstMipLevel,
                                                uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
            throw new NotImplementedException();
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            throw new NotImplementedException();
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            throw new NotImplementedException();
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            throw new NotImplementedException();
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            clearColourValues[index] = new Color(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            validClearColourValues[index] = true;

            if (renderPass != null)
            {
                endRenderPass();
                beginRenderPass();
            }
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            clearDepthValue = depth;
            clearStencilValue = stencil;

            if (renderPass != null)
            {
                endRenderPass();
                beginRenderPass();
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            throw new NotImplementedException();
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            throw new NotImplementedException();
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            throw new NotImplementedException();
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            throw new NotImplementedException();
        }

        private protected override void PushDebugGroupCore(string name)
        {
            throw new NotImplementedException();
        }

        private protected override void PopDebugGroupCore()
        {
            throw new NotImplementedException();
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            throw new NotImplementedException();
        }

        private void beginRenderPass()
        {
            if (renderPass != null)
                return;

            Span<RenderPassColorAttachment> colourAttachments = stackalloc RenderPassColorAttachment[Framebuffer.ColorTargets.Count];

            for (int i = 0; i < Framebuffer.ColorTargets.Count; i++)
            {
                var texture = Util.AssertSubtype<Texture, WGPUTexture>(Framebuffer.ColorTargets[i].Target);
                var textureView = Util.AssertSubtype<TextureView, WGPUTextureViewBase>(texture.GetFullTextureView(gd));

                colourAttachments[i] = new RenderPassColorAttachment
                {
                    View = textureView.View,
                    LoadOp = validClearColourValues[i] ? LoadOp.Clear : LoadOp.Load,
                    StoreOp = StoreOp.Store,
                    ClearValue = new Color(clearColourValues[i].R, clearColourValues[i].G, clearColourValues[i].B, clearColourValues[i].A),
                };
            }

            RenderPassDepthStencilAttachment depthStencilAttachment = default;

            if (Framebuffer.DepthTarget is FramebufferAttachment depthTarget)
            {
                var texture = Util.AssertSubtype<Texture, WGPUTexture>(depthTarget.Target);
                var textureView = Util.AssertSubtype<TextureView, WGPUTextureViewBase>(texture.GetFullTextureView(gd));

                depthStencilAttachment = new RenderPassDepthStencilAttachment
                {
                    View = textureView.View,
                    DepthLoadOp = clearDepthValue == null ? LoadOp.Load : LoadOp.Clear,
                    DepthStoreOp = StoreOp.Store,
                    DepthClearValue = clearDepthValue ?? 0,
                    StencilLoadOp = clearStencilValue == null ? LoadOp.Load : LoadOp.Clear,
                    StencilStoreOp = StoreOp.Store,
                    StencilClearValue = clearStencilValue ?? 0
                };
            }

            fixed (RenderPassColorAttachment* colourAttachmentPtr = &colourAttachments[0])
            {
                var renderPassDescriptor = new RenderPassDescriptor
                {
                    ColorAttachmentCount = (uint)colourAttachments.Length,
                    ColorAttachments = colourAttachmentPtr
                };

                if (Framebuffer.DepthTarget != null)
                    renderPassDescriptor.DepthStencilAttachment = &depthStencilAttachment;

                renderPass = gd.WebGPU.CommandEncoderBeginRenderPass(encoder, &renderPassDescriptor);
            }

            Util.ClearArray(validClearColourValues);
            clearDepthValue = null;
            clearStencilValue = null;
        }

        private void endRenderPass()
        {
            if (renderPass == null)
                return;

            gd.WebGPU.RenderPassEncoderEnd(renderPass);
            gd.WebGPU.RenderPassEncoderRelease(renderPass);

            renderPass = null;
        }

        private void resetState()
        {
            Framebuffer = null;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

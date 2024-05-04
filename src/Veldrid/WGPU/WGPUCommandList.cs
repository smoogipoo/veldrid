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
            commandBuffer = gd.WebGPU.CommandEncoderFinish(encoder, new CommandBufferDescriptor());
            gd.WebGPU.CommandEncoderRelease(encoder);
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            throw new NotImplementedException();
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

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3CommandList : CommandList
    {
        public override string Name { get; set; }

        private readonly SDL3GraphicsDevice gd;
        private SDL_GPUCommandBuffer* commandBuffer;
        private SDL_GPURenderPass* renderPass;
        private SDL_GPUComputePass* computePass;
        private SDL_GPUFence* completionFence;

        private bool hasAcquiredSwapchainTexture;
        private bool hasAcquiredFramebuffer;
        private bool hasIndexBuffer;
        private SDL_FColor? clearColor;
        private float? clearDepth;
        private byte? clearStencil;

        private bool isDisposed;

        public SDL3CommandList(SDL3GraphicsDevice gd, ref CommandListDescription description)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            this.gd = gd;
        }

        public override bool IsDisposed => isDisposed;

        public SDL_GPUFence* GetCompletionFence()
        {
            SDL_GPUFence* fence = completionFence;
            completionFence = null;
            return fence;
        }

        public override void Begin()
        {
            commandBuffer = SDL_AcquireGPUCommandBuffer(gd.Device);
        }

        public override void End()
        {
            endRenderPass();
            endComputePass();

            completionFence = SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer);
            hasAcquiredSwapchainTexture = false;
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            beginRenderPass();

            float vpY = gd.IsClipSpaceYInverted
                ? viewport.Y
                : viewport.Height + viewport.Y;
            float vpHeight = gd.IsClipSpaceYInverted
                ? viewport.Height
                : -viewport.Height;

            SDL_GPUViewport sdlViewport = new SDL_GPUViewport
            {
                x = viewport.X,
                y = vpY,
                w = viewport.Width,
                h = vpHeight,
                min_depth = viewport.MinDepth,
                max_depth = viewport.MaxDepth
            };

            SDL_SetGPUViewport(renderPass, &sdlViewport);
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            beginRenderPass();

            SDL_Rect scissor = new SDL_Rect
            {
                x = (int)x,
                y = (int)y,
                w = (int)width,
                h = (int)height
            };

            SDL_SetGPUScissor(renderPass, &scissor);
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            beginComputePass();

            SDL_DispatchGPUCompute(computePass, groupCountX, groupCountY, groupCountZ);
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            beginRenderPass();

            SDL3ResourceSet sdlRs = Util.AssertSubtype<ResourceSet, SDL3ResourceSet>(rs);

            SDL_GPUBuffer* buffer = null;
            SDL_GPUTexture* texture = null;
            SDL_GPUSampler* sampler = null;
            ShaderStages stages = ShaderStages.None;

            for (int i = 0; i < sdlRs.Layout.Elements.Length; i++)
            {
                ResourceLayoutElementDescription element = sdlRs.Layout.Elements[i];
                IBindableResource resource = sdlRs.BoundResources[i];
                stages = element.Stages;

                switch (element.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    case ResourceKind.StructuredBufferReadOnly:
                    case ResourceKind.StructuredBufferReadWrite:
                        buffer = Util.AssertSubtype<IBindableResource, SDL3Buffer>(resource).Buffer;
                        break;

                    case ResourceKind.TextureReadOnly:
                    case ResourceKind.TextureReadWrite:
                        texture = Util.AssertSubtype<IBindableResource, SDL3Texture>(resource).Texture;
                        break;

                    case ResourceKind.Sampler:
                        sampler = Util.AssertSubtype<IBindableResource, SDL3Sampler>(resource).Sampler;
                        break;
                }
            }

            if (buffer != null)
            {
                if ((stages & ShaderStages.Vertex) > 0)
                    SDL_BindGPUVertexStorageBuffers(renderPass, slot, &buffer, 1);
                if ((stages & ShaderStages.Fragment) > 0)
                    SDL_BindGPUFragmentStorageBuffers(renderPass, slot, &buffer, 1);
            }

            if (texture != null)
            {
                if (sampler != null)
                {
                    SDL_GPUTextureSamplerBinding pairBinding = new SDL_GPUTextureSamplerBinding
                    {
                        sampler = sampler,
                        texture = texture
                    };

                    if ((stages & ShaderStages.Vertex) > 0)
                        SDL_BindGPUVertexSamplers(renderPass, slot, &pairBinding, 1);
                    if ((stages & ShaderStages.Fragment) > 0)
                        SDL_BindGPUFragmentSamplers(renderPass, slot, &pairBinding, 1);
                }

                if ((stages & ShaderStages.Vertex) > 0)
                    SDL_BindGPUVertexStorageTextures(renderPass, slot, &texture, 1);
                if ((stages & ShaderStages.Fragment) > 0)
                    SDL_BindGPUFragmentStorageTextures(renderPass, slot, &texture, 1);
            }
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            beginComputePass();

            SDL3ResourceSet sdlSet = Util.AssertSubtype<ResourceSet, SDL3ResourceSet>(set);

            SDL_GPUBuffer* buffer = null;
            SDL_GPUTexture* texture = null;
            SDL_GPUSampler* sampler = null;

            for (int i = 0; i < sdlSet.Layout.Elements.Length; i++)
            {
                ResourceLayoutElementDescription element = sdlSet.Layout.Elements[i];
                IBindableResource resource = sdlSet.BoundResources[i];

                switch (element.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    case ResourceKind.StructuredBufferReadOnly:
                    case ResourceKind.StructuredBufferReadWrite:
                        buffer = Util.AssertSubtype<IBindableResource, SDL3Buffer>(resource).Buffer;
                        break;

                    case ResourceKind.TextureReadOnly:
                    case ResourceKind.TextureReadWrite:
                        texture = Util.AssertSubtype<IBindableResource, SDL3Texture>(resource).Texture;
                        break;

                    case ResourceKind.Sampler:
                        sampler = Util.AssertSubtype<IBindableResource, SDL3Sampler>(resource).Sampler;
                        break;
                }
            }

            if (buffer != null)
                SDL_BindGPUComputeStorageBuffers(computePass, slot, &buffer, 1);

            if (texture != null)
            {
                if (sampler != null)
                {
                    SDL_GPUTextureSamplerBinding pairBinding = new SDL_GPUTextureSamplerBinding
                    {
                        sampler = sampler,
                        texture = texture
                    };

                    SDL_BindGPUComputeSamplers(computePass, slot, &pairBinding, 1);
                }

                SDL_BindGPUComputeStorageTextures(computePass, slot, &texture, 1);
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            endRenderPass();

            Framebuffer = fb;
            hasAcquiredFramebuffer = true;

            if (Framebuffer is SDL3SwapchainFramebuffer swapchainFramebuffer && !hasAcquiredSwapchainTexture)
            {
                swapchainFramebuffer.AcquireTexture(commandBuffer);
                hasAcquiredSwapchainTexture = true;
            }

            beginRenderPass();
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            beginRenderPass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
            SDL_DrawGPUPrimitivesIndirect(renderPass, sdlBuffer.Buffer, offset, drawCount);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            beginRenderPass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
            SDL_DrawGPUIndexedPrimitivesIndirect(renderPass, sdlBuffer.Buffer, offset, drawCount);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            beginComputePass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
            SDL_DispatchGPUComputeIndirect(computePass, sdlBuffer.Buffer, offset);
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            beginRenderPass();

            SDL3TextureBase sdlSource = Util.AssertSubtype<Texture, SDL3TextureBase>(source);
            SDL3Texture sdlDestination = Util.AssertSubtype<Texture, SDL3Texture>(destination);

            SDL_GPUColorTargetInfo colorTarget = new SDL_GPUColorTargetInfo
            {
                texture = sdlDestination.Texture,
                mip_level = 1,
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_RESOLVE,
                resolve_texture = sdlSource.Texture,
                resolve_mip_level = 1,
            };

            SDL_EndGPURenderPass(SDL_BeginGPURenderPass(commandBuffer, &colorTarget, 1, null));
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            endComputePass();
            endRenderPass();

            SDL3Buffer sdlSource = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(source);
            SDL3Buffer sdlDestination = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(destination);

            SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

            if ((source.Usage & BufferUsage.Staging) > 0)
            {
                SDL_GPUTransferBufferLocation srcLocation = new SDL_GPUTransferBufferLocation
                {
                    transfer_buffer = sdlSource.TransferBuffer,
                    offset = sourceOffset
                };

                SDL_GPUBufferRegion dstRegion = new SDL_GPUBufferRegion
                {
                    buffer = sdlDestination.Buffer,
                    offset = destinationOffset,
                    size = sizeInBytes
                };

                SDL_UploadToGPUBuffer(copyPass, &srcLocation, &dstRegion, true);
            }
            else
            {
                SDL_GPUBufferLocation srcLocation = new SDL_GPUBufferLocation
                {
                    buffer = sdlSource.Buffer,
                    offset = sourceOffset
                };

                SDL_GPUBufferLocation dstLocation = new SDL_GPUBufferLocation
                {
                    buffer = sdlDestination.Buffer,
                    offset = destinationOffset
                };

                SDL_CopyGPUBufferToBuffer(copyPass, &srcLocation, &dstLocation, sizeInBytes, true);
            }

            SDL_EndGPUCopyPass(copyPass);
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ,
                                                uint dstMipLevel,
                                                uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
            endComputePass();
            endRenderPass();

            SDL3Texture sdlSource = Util.AssertSubtype<Texture, SDL3Texture>(source);
            SDL3Texture sdlDestination = Util.AssertSubtype<Texture, SDL3Texture>(destination);

            SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

            if ((source.Usage & TextureUsage.Staging) > 0)
            {
                SDL_GPUTextureTransferInfo srcInfo = new SDL_GPUTextureTransferInfo
                {
                    transfer_buffer = sdlSource.TransferBuffer,
                    offset = FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(source.Width, source.Format), srcY, source.Format) + FormatHelpers.GetRowPitch(srcX, source.Format),
                    pixels_per_row = width,
                    rows_per_layer = height
                };

                SDL_GPUTextureRegion dstRegion = new SDL_GPUTextureRegion
                {
                    texture = sdlDestination.Texture,
                    mip_level = dstMipLevel,
                    layer = dstBaseArrayLayer,
                    x = dstX,
                    y = dstY,
                    z = dstZ,
                    w = width,
                    h = height,
                    d = depth
                };

                SDL_UploadToGPUTexture(copyPass, &srcInfo, &dstRegion, true);
            }
            else
            {
                SDL_GPUTextureLocation srcLocation = new SDL_GPUTextureLocation
                {
                    texture = sdlSource.Texture,
                    mip_level = srcMipLevel,
                    layer = srcBaseArrayLayer,
                    x = srcX,
                    y = srcY,
                    z = srcZ
                };

                SDL_GPUTextureLocation dstLocation = new SDL_GPUTextureLocation
                {
                    texture = sdlDestination.Texture,
                    mip_level = dstMipLevel,
                    layer = dstBaseArrayLayer,
                    x = dstX,
                    y = dstY,
                    z = dstZ
                };

                SDL_CopyGPUTextureToTexture(copyPass, &srcLocation, &dstLocation, width, height, depth, true);
            }

            SDL_EndGPUCopyPass(copyPass);
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline)
            {
                beginComputePass();

                SDL3ComputePipeline computePipeline = Util.AssertSubtype<Pipeline, SDL3ComputePipeline>(pipeline);
                SDL_BindGPUComputePipeline(computePass, computePipeline.Pipeline);
            }
            else
            {
                beginRenderPass();

                SDL3GraphicsPipeline graphicsPipeline = Util.AssertSubtype<Pipeline, SDL3GraphicsPipeline>(pipeline);
                SDL_BindGPUGraphicsPipeline(renderPass, graphicsPipeline.Pipeline);
            }
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            beginRenderPass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(buffer);

            SDL_GPUBufferBinding binding = new SDL_GPUBufferBinding
            {
                buffer = sdlBuffer.Buffer,
                offset = offset
            };

            SDL_BindGPUVertexBuffers(renderPass, index, &binding, 1);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            beginRenderPass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(buffer);

            SDL_GPUBufferBinding binding = new SDL_GPUBufferBinding
            {
                buffer = sdlBuffer.Buffer,
                offset = offset
            };

            SDL_BindGPUIndexBuffer(renderPass, &binding, SDL3Formats.VdToSDLIndexElementSize(format));
            hasIndexBuffer = true;
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            endRenderPass();

            this.clearColor = new SDL_FColor
            {
                r = clearColor.R,
                g = clearColor.G,
                b = clearColor.B,
                a = clearColor.A
            };

            beginRenderPass();
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            endRenderPass();

            clearDepth = depth;
            clearStencil = stencil;

            beginRenderPass();
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            beginRenderPass();

            SDL_DrawGPUPrimitives(renderPass, vertexCount, instanceCount, vertexStart, instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            beginRenderPass();

            if (!hasIndexBuffer)
            {
                return;
            }

            SDL_DrawGPUIndexedPrimitives(renderPass, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            endComputePass();
            endRenderPass();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(buffer);

            bool mustDisposeTransferBuffer;
            SDL_GPUTransferBufferLocation transferRegion;
            SDL_GPUTransferBuffer* transferBuffer;

            if ((buffer.Usage & (BufferUsage.Staging | BufferUsage.Dynamic)) > 0)
            {
                mustDisposeTransferBuffer = false;

                transferBuffer = sdlBuffer.TransferBuffer;
                transferRegion = new SDL_GPUTransferBufferLocation
                {
                    transfer_buffer = transferBuffer,
                    offset = bufferOffsetInBytes
                };
            }
            else
            {
                mustDisposeTransferBuffer = true;

                SDL_GPUTransferBufferCreateInfo ci = new SDL_GPUTransferBufferCreateInfo
                {
                    usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                    size = sizeInBytes,
                };

                transferBuffer = SDL_CreateGPUTransferBuffer(gd.Device, &ci);
                transferRegion = new SDL_GPUTransferBufferLocation
                {
                    transfer_buffer = transferBuffer,
                    offset = 0
                };
            }

            byte* mapped = (byte*)SDL_MapGPUTransferBuffer(gd.Device, transferBuffer, true);
            Unsafe.CopyBlock(mapped + transferRegion.offset, (byte*)source, sizeInBytes);
            SDL_UnmapGPUTransferBuffer(gd.Device, transferBuffer);

            SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

            SDL_GPUBufferRegion dstRegion = new SDL_GPUBufferRegion
            {
                buffer = sdlBuffer.Buffer,
                offset = bufferOffsetInBytes,
                size = sizeInBytes
            };

            SDL_UploadToGPUBuffer(copyPass, &transferRegion, &dstRegion, true);
            SDL_EndGPUCopyPass(copyPass);

            if (mustDisposeTransferBuffer)
                SDL_ReleaseGPUTransferBuffer(gd.Device, transferBuffer);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            endComputePass();
            endRenderPass();

            SDL3Texture sdlTexture = Util.AssertSubtype<Texture, SDL3Texture>(texture);
            SDL_GenerateMipmapsForGPUTexture(commandBuffer, sdlTexture.Texture);
        }

        private protected override void PushDebugGroupCore(string name)
        {
            SDL_PushGPUDebugGroup(commandBuffer, name);
        }

        private protected override void PopDebugGroupCore()
        {
            SDL_PopGPUDebugGroup(commandBuffer);
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            SDL_InsertGPUDebugLabel(commandBuffer, name);
        }

        private void beginRenderPass()
        {
            endComputePass();

            if (renderPass != null)
                return;

            if (!hasAcquiredFramebuffer)
                return;

            SDL3Framebuffer sdlFb = Util.AssertSubtype<Framebuffer, SDL3Framebuffer>(Framebuffer);
            SDL_GPUColorTargetInfo* colorTargets = stackalloc SDL_GPUColorTargetInfo[Framebuffer.ColorTargets.Count];

            for (int i = 0; i < sdlFb.ColorTargets.Count; i++)
            {
                FramebufferAttachment attachment = sdlFb.ColorTargets[i];

                SDL3TextureBase sdlTarget = Util.AssertSubtype<Texture, SDL3TextureBase>(attachment.Target);

                colorTargets[i] = new SDL_GPUColorTargetInfo
                {
                    texture = sdlTarget.Texture,
                    mip_level = attachment.MipLevel,
                    layer_or_depth_plane = attachment.ArrayLayer,
                    clear_color = clearColor ?? default,
                    load_op = clearColor == null
                        ? SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD
                        : SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                };
            }

            if (Framebuffer.DepthTarget.HasValue)
            {
                SDL3TextureBase sdlTarget = Util.AssertSubtype<Texture, SDL3TextureBase>(Framebuffer.DepthTarget.Value.Target);

                SDL_GPUDepthStencilTargetInfo depthTarget = new SDL_GPUDepthStencilTargetInfo
                {
                    texture = sdlTarget.Texture,
                    clear_depth = clearDepth ?? 0,
                    load_op = clearDepth == null
                        ? SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD
                        : SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                    stencil_load_op = clearStencil == null
                        ? SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD
                        : SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                    clear_stencil = clearStencil ?? 0,
                };

                renderPass = SDL_BeginGPURenderPass(commandBuffer, colorTargets, (uint)Framebuffer.ColorTargets.Count, &depthTarget);
            }
            else
                renderPass = SDL_BeginGPURenderPass(commandBuffer, colorTargets, (uint)Framebuffer.ColorTargets.Count, null);

            clearColor = null;
            clearDepth = null;
            clearStencil = null;
        }

        private void endRenderPass()
        {
            if (renderPass == null)
                return;

            SDL_EndGPURenderPass(renderPass);
            renderPass = null;

            // The next render pass may only start after it has acquired a framebuffer.
            hasAcquiredFramebuffer = false;
            hasIndexBuffer = false;
            clearColor = null;
            clearDepth = null;
            clearStencil = null;
        }

        private void beginComputePass()
        {
            endRenderPass();

            if (computePass != null)
                return;

            // computePass = SDL_BeginGPUComputePass()
        }

        private void endComputePass()
        {
            if (computePass == null)
                return;

            SDL_EndGPUComputePass(computePass);
            computePass = null;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3CommandList : CommandList
    {
        public override string Name { get; set; }

        private readonly SDL3GraphicsDevice gd;
        private SDL_GPUCommandBuffer* commandBuffer;
        private SDL_GPURenderPass* renderPass;
        private SDL_GPUComputePass* computePass;
        private SDL_GPUCopyPass* copyPass;
        private SDL_GPUFence* completionFence;

        private bool acquiredSwapchainTexture;

        private SDL3Framebuffer currentFramebuffer;
        private bool currentFramebufferEverActive;

        private SDL3GraphicsPipeline currentGraphicsPipeline;
        private SDL3ResourceSet[] currentGraphicsResourceSets = [];

        private SDL3ComputePipeline currentComputePipeline;
        private SDL3ResourceSet[] currentComputeResourceSets = [];

        private (SDL_GPUBufferBinding, SDL_GPUIndexElementSize)? currentIndexBuffer;
        private SDL_GPUBufferBinding[] currentVertexBuffers = [];

        private SDL_GPUViewport? currentViewport;
        private SDL_Rect? currentScissor;

        private SDL_FColor? currentClearColor;
        private float? currentClearDepth;
        private byte? currentClearStencil;

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

            ClearCachedState();

            acquiredSwapchainTexture = false;

            currentFramebuffer = null;
            currentFramebufferEverActive = false;

            currentGraphicsPipeline = null;
            Util.ClearArray(currentGraphicsResourceSets);

            currentComputePipeline = null;
            Util.ClearArray(currentComputeResourceSets);

            currentIndexBuffer = null;
            Util.ClearArray(currentVertexBuffers);

            currentViewport = null;
            currentScissor = null;
            currentClearColor = null;
            currentClearDepth = null;
            currentClearStencil = null;
        }

        public override void End()
        {
            if (!currentFramebufferEverActive && currentFramebuffer != null)
            {
                // Flush any queued texture clears.
                ensureRenderPass();
            }

            ensureNoCopyPass();
            ensureNoRenderPass();

            completionFence = SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer);
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            currentViewport = new SDL_GPUViewport
            {
                x = viewport.X,
                y = viewport.Y,
                w = viewport.Width,
                h = viewport.Height,
                min_depth = viewport.MinDepth,
                max_depth = viewport.MaxDepth
            };
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            currentScissor = new SDL_Rect
            {
                x = (int)x,
                y = (int)y,
                w = (int)width,
                h = (int)height
            };
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            using (beginComputePass())
                SDL_DispatchGPUCompute(computePass, groupCountX, groupCountY, groupCountZ);
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            SDL3ResourceSet sdlRs = Util.AssertSubtype<ResourceSet, SDL3ResourceSet>(rs);
            currentGraphicsResourceSets[slot] = sdlRs;
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            SDL3ResourceSet sdlSet = Util.AssertSubtype<ResourceSet, SDL3ResourceSet>(set);
            currentComputeResourceSets[slot] = sdlSet;
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (renderPass != null)
            {
                // Finish the current render pass.
                ensureNoRenderPass();
            }
            else if (!currentFramebufferEverActive && currentFramebuffer != null)
            {
                // Flush any queued texture clears.
                ensureRenderPass();
                ensureNoRenderPass();
            }

            SDL3Framebuffer sdlFb = Util.AssertSubtype<Framebuffer, SDL3Framebuffer>(fb);

            Framebuffer = fb;
            currentFramebuffer = sdlFb;
            currentFramebufferEverActive = false;
            currentClearColor = null;
            currentClearDepth = null;
            currentClearStencil = null;
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            prepareDrawCommand();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
            SDL_DrawGPUPrimitivesIndirect(renderPass, sdlBuffer.Buffer, offset, drawCount);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            prepareDrawCommand();

            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
            SDL_DrawGPUIndexedPrimitivesIndirect(renderPass, sdlBuffer.Buffer, offset, drawCount);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            using (beginComputePass())
            {
                SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(indirectBuffer);
                SDL_DispatchGPUComputeIndirect(computePass, sdlBuffer.Buffer, offset);
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            ensureNoRenderPass();

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
            ensureCopyPass();

            SDL3Buffer sdlSource = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(source);
            SDL3Buffer sdlDestination = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(destination);

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

                SDL_UploadToGPUBuffer(copyPass, &srcLocation, &dstRegion, false);
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

                SDL_CopyGPUBufferToBuffer(copyPass, &srcLocation, &dstLocation, sizeInBytes, false);
            }
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ,
                                                uint dstMipLevel,
                                                uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
            ensureCopyPass();

            SDL3Texture sdlSource = Util.AssertSubtype<Texture, SDL3Texture>(source);
            SDL3Texture sdlDestination = Util.AssertSubtype<Texture, SDL3Texture>(destination);

            if ((source.Usage & TextureUsage.Staging) > 0)
            {
                Util.GetMipDimensions(sdlSource, srcMipLevel, out uint mipWidth, out uint mipHeight, out uint _);

                uint blockSize = FormatHelpers.IsCompressedFormat(sdlSource.Format) ? 4u : 1u;
                uint compressedSrcX = srcX / blockSize;
                uint compressedSrcY = srcY / blockSize;
                uint blockSizeInBytes = blockSize == 1
                    ? FormatSizeHelpers.GetSizeInBytes(sdlSource.Format)
                    : FormatHelpers.GetBlockSizeInBytes(sdlSource.Format);

                sdlSource.GetSubresourceLayout(srcMipLevel, out uint srcRowPitch, out uint srcDepthPitch);

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    ulong srcSubresourceBase = Util.ComputeSubresourceOffset(sdlSource, srcMipLevel, srcBaseArrayLayer + layer);
                    ulong sourceOffset = srcSubresourceBase
                                         + srcDepthPitch * srcZ
                                         + srcRowPitch * compressedSrcY
                                         + blockSizeInBytes * compressedSrcX;

                    uint copyWidth = width > mipWidth && width <= blockSize
                        ? mipWidth
                        : width;

                    uint copyHeight = height > mipHeight && height <= blockSize
                        ? mipHeight
                        : height;

                    SDL_GPUTextureTransferInfo srcInfo = new SDL_GPUTextureTransferInfo
                    {
                        offset = (uint)sourceOffset,
                        transfer_buffer = sdlSource.TransferBuffer,
                        pixels_per_row = srcRowPitch,
                        rows_per_layer = srcDepthPitch
                    };

                    SDL_GPUTextureRegion dstRegion = new SDL_GPUTextureRegion
                    {
                        texture = sdlDestination.Texture,
                        mip_level = dstMipLevel,
                        layer = dstBaseArrayLayer + layer,
                        x = dstX,
                        y = dstY,
                        z = dstZ,
                        w = copyWidth,
                        h = copyHeight,
                        d = depth
                    };

                    SDL_UploadToGPUTexture(copyPass, &srcInfo, &dstRegion, false);
                }
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

                SDL_CopyGPUTextureToTexture(copyPass, &srcLocation, &dstLocation, width, height, depth, false);
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline)
            {
                SDL3ComputePipeline computePipeline = Util.AssertSubtype<Pipeline, SDL3ComputePipeline>(pipeline);

                Util.EnsureArrayMinimumSize(ref currentComputeResourceSets, computePipeline.ResourceLayoutCount);
            }
            else
            {
                SDL3GraphicsPipeline graphicsPipeline = Util.AssertSubtype<Pipeline, SDL3GraphicsPipeline>(pipeline);

                Util.EnsureArrayMinimumSize(ref currentGraphicsResourceSets, graphicsPipeline.ResourceLayoutCount);
                Util.EnsureArrayMinimumSize(ref currentVertexBuffers, graphicsPipeline.VertexLayoutCount);
            }
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(buffer);

            Util.EnsureArrayMinimumSize(ref currentVertexBuffers, index + 1);

            currentVertexBuffers[index] = new SDL_GPUBufferBinding
            {
                buffer = sdlBuffer.Buffer,
                offset = offset
            };
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            SDL3Buffer sdlBuffer = Util.AssertSubtype<DeviceBuffer, SDL3Buffer>(buffer);

            currentIndexBuffer = (new SDL_GPUBufferBinding
            {
                buffer = sdlBuffer.Buffer,
                offset = offset
            }, SDL3Formats.VdToSDLIndexElementSize(format));
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            currentClearColor = new SDL_FColor
            {
                r = clearColor.R,
                g = clearColor.G,
                b = clearColor.B,
                a = clearColor.A
            };
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            currentClearDepth = depth;
            currentClearStencil = stencil;
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            prepareDrawCommand();

            SDL_DrawGPUPrimitives(renderPass, vertexCount, instanceCount, vertexStart, instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            prepareDrawCommand();

            SDL_DrawGPUIndexedPrimitives(renderPass, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            ensureCopyPass();

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

            byte* mapped = (byte*)SDL_MapGPUTransferBuffer(gd.Device, transferBuffer, mustDisposeTransferBuffer);
            Unsafe.CopyBlock(mapped + transferRegion.offset, (byte*)source, sizeInBytes);
            SDL_UnmapGPUTransferBuffer(gd.Device, transferBuffer);

            SDL_GPUBufferRegion dstRegion = new SDL_GPUBufferRegion
            {
                buffer = sdlBuffer.Buffer,
                offset = bufferOffsetInBytes,
                size = sizeInBytes
            };

            SDL_UploadToGPUBuffer(copyPass, &transferRegion, &dstRegion, false);

            if (mustDisposeTransferBuffer)
                SDL_ReleaseGPUTransferBuffer(gd.Device, transferBuffer);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            ensureNoCopyPass();
            ensureNoRenderPass();

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

        private void prepareDrawCommand()
        {
            ensureRenderPass();

            if (currentGraphicsPipeline != GraphicsPipeline)
            {
                currentGraphicsPipeline = Util.AssertSubtype<Pipeline, SDL3GraphicsPipeline>(GraphicsPipeline);
                SDL_BindGPUGraphicsPipeline(renderPass, currentGraphicsPipeline.Pipeline);
            }

            uint vertexUniformBufferIndex = 0;
            uint fragmentUniformBufferIndex = 0;
            uint vertexTextureIndex = 0;
            uint fragmentTextureIndex = 0;
            uint vertexSamplerIndex = 0;
            uint fragmentSamplerIndex = 0;

            for (int index = 0; index < currentGraphicsPipeline.ResourceLayoutCount; index++)
            {
                SDL3ResourceSet set = currentGraphicsResourceSets[index];

                if (set == null)
                    continue;

                SDL_GPUBuffer* buffer = null;
                SDL_GPUTexture* texture = null;
                SDL_GPUSampler* sampler = null;
                ShaderStages stages = ShaderStages.None;

                for (int i = 0; i < set.Layout.Elements.Length; i++)
                {
                    ResourceLayoutElementDescription element = set.Layout.Elements[i];
                    IBindableResource resource = set.Resources[i];
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
                        SDL_BindGPUVertexStorageBuffers(renderPass, 0, &buffer, 1);
                    if ((stages & ShaderStages.Fragment) > 0)
                        SDL_BindGPUFragmentStorageBuffers(renderPass, 0, &buffer, 1);
                }

                if (sampler != null)
                {
                    SDL_GPUTextureSamplerBinding pairBinding = new SDL_GPUTextureSamplerBinding
                    {
                        sampler = sampler,
                        texture = texture
                    };

                    if ((stages & ShaderStages.Vertex) > 0)
                        SDL_BindGPUVertexSamplers(renderPass, vertexSamplerIndex++, &pairBinding, 1);
                    if ((stages & ShaderStages.Fragment) > 0)
                        SDL_BindGPUFragmentSamplers(renderPass, fragmentSamplerIndex++, &pairBinding, 1);
                }
                else if (texture != null)
                {
                    if ((stages & ShaderStages.Vertex) > 0)
                        SDL_BindGPUVertexStorageTextures(renderPass, vertexTextureIndex++, &texture, 1);
                    if ((stages & ShaderStages.Fragment) > 0)
                        SDL_BindGPUFragmentStorageTextures(renderPass, fragmentTextureIndex++, &texture, 1);
                }
            }

            if (currentViewport is SDL_GPUViewport viewport)
            {
                SDL_SetGPUViewport(renderPass, &viewport);
                currentViewport = null;
            }

            if (currentScissor is SDL_Rect scissor)
            {
                SDL_SetGPUScissor(renderPass, &scissor);
                currentScissor = null;
            }

            fixed (SDL_GPUBufferBinding* vertexBindings = &currentVertexBuffers[0])
                SDL_BindGPUVertexBuffers(renderPass, 0, vertexBindings, currentGraphicsPipeline.VertexLayoutCount);

            if (currentIndexBuffer is var (binding, size))
                SDL_BindGPUIndexBuffer(renderPass, &binding, size);
        }

        private void ensureRenderPass()
        {
            if (renderPass != null)
                return;

            ensureNoCopyPass();

            if (Framebuffer is SDL3SwapchainFramebuffer swapchainFramebuffer && !acquiredSwapchainTexture)
            {
                swapchainFramebuffer.AcquireTexture(commandBuffer);
                acquiredSwapchainTexture = true;
            }

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
                    clear_color = currentClearColor ?? default,
                    load_op = currentClearColor == null
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
                    clear_depth = currentClearDepth ?? 0,
                    load_op = currentClearDepth == null
                        ? SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD
                        : SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                    stencil_load_op = currentClearStencil == null
                        ? SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD
                        : SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                    clear_stencil = currentClearStencil ?? 0,
                };

                renderPass = SDL_BeginGPURenderPass(commandBuffer, colorTargets, (uint)Framebuffer.ColorTargets.Count, &depthTarget);
            }
            else
                renderPass = SDL_BeginGPURenderPass(commandBuffer, colorTargets, (uint)Framebuffer.ColorTargets.Count, null);

            currentFramebufferEverActive = true;
            currentClearColor = null;
            currentClearDepth = null;
            currentClearStencil = null;
        }

        private void ensureNoRenderPass()
        {
            if (renderPass == null)
                return;

            SDL_EndGPURenderPass(renderPass);

            renderPass = null;
            currentGraphicsPipeline = null;
        }

        private ValueInvokeOnDisposal beginComputePass()
        {
            ensureNoRenderPass();
            ensureNoCopyPass();

            // ??? how many times do we need to define the storage bindings ???
            // 1: here...?
            // 2: SDL_CreateGPUComputePipeline?
            // 3: SDL_BindGPU* below?
            computePass = SDL_BeginGPUComputePass(commandBuffer, null, 0, null, 0);

            if (currentComputePipeline != ComputePipeline)
            {
                currentComputePipeline = Util.AssertSubtype<Pipeline, SDL3ComputePipeline>(ComputePipeline);
                SDL_BindGPUComputePipeline(computePass, currentComputePipeline.Pipeline);
            }

            uint bufferIndex = 0;
            uint textureIndex = 0;
            uint samplerIndex = 0;

            for (int index = 0; index < currentComputePipeline.ResourceLayoutCount; index++)
            {
                SDL3ResourceSet set = currentComputeResourceSets[index];

                SDL_GPUBuffer* buffer = null;
                SDL_GPUTexture* texture = null;
                SDL_GPUSampler* sampler = null;

                for (int i = 0; i < set.Layout.Elements.Length; i++)
                {
                    ResourceLayoutElementDescription element = set.Layout.Elements[i];
                    IBindableResource resource = set.Resources[i];

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
                    uint slot = bufferIndex++;
                    SDL_BindGPUComputeStorageBuffers(computePass, slot, &buffer, 1);
                }

                if (sampler != null)
                {
                    uint slot = samplerIndex++;

                    SDL_GPUTextureSamplerBinding pairBinding = new SDL_GPUTextureSamplerBinding
                    {
                        sampler = sampler,
                        texture = texture
                    };

                    SDL_BindGPUComputeSamplers(computePass, slot, &pairBinding, 1);
                }
                else if (texture != null)
                {
                    uint slot = textureIndex++;
                    SDL_BindGPUComputeStorageTextures(computePass, slot, &texture, 1);
                }
            }

            return new ValueInvokeOnDisposal(this, static sender =>
            {
                SDL3CommandList cl = Util.AssertSubtype<object, SDL3CommandList>(sender);

                SDL_EndGPUComputePass(cl.computePass);
                cl.computePass = null;
                cl.currentComputePipeline = null;
            });
        }

        private void ensureCopyPass()
        {
            if (copyPass != null)
                return;

            ensureNoRenderPass();
            copyPass = SDL_BeginGPUCopyPass(commandBuffer);
        }

        private void ensureNoCopyPass()
        {
            if (copyPass == null)
                return;

            SDL_EndGPUCopyPass(copyPass);
            copyPass = null;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

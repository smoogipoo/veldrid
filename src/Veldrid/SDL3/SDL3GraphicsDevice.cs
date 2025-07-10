// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3GraphicsDevice : GraphicsDevice
    {
        public override string DeviceName => string.Empty;
        public override string VendorName => string.Empty;
        public override GraphicsApiVersion ApiVersion => new GraphicsApiVersion();
        public override GraphicsBackend BackendType { get; }
        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;
        public override bool IsClipSpaceYInverted => false;
        public override ResourceFactory ResourceFactory { get; }
        public override Swapchain MainSwapchain => SDLSwapchain;

        public override GraphicsDeviceFeatures Features { get; }

        public readonly SDL_GPUDevice* Device;
        public readonly SDL_Window* Window;
        public readonly SDL3Swapchain SDLSwapchain;

        private readonly List<IntPtr> submittedCLs = new List<IntPtr>();

        public SDL3GraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? scDesc)
        {
            if (OperatingSystem.IsWindows())
                BackendType = GraphicsBackend.Direct3D11;
            else if (OperatingSystem.IsMacOS())
                BackendType = GraphicsBackend.Metal;
            else if (OperatingSystem.IsLinux())
                BackendType = GraphicsBackend.Vulkan;
            else
                throw new NotSupportedException("Current operating system not supported.");

            Device = SDL_CreateGPUDevice(SDL3Formats.VdToSDLShaderFormat(BackendType), true, (byte*)null);

            if (Device == null)
                throw new InvalidOperationException("Failed to initialise SDL GPU device.");

            Features = new GraphicsDeviceFeatures(
                true,
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                true,
                true,
                false,
                true,
                false,
                true);

            ResourceFactory = new SDL3ResourceFactory(this, Features);

            if (scDesc != null)
            {
                var desc = scDesc.Value;

                SDL3SwapchainSource sdlSource = Util.AssertSubtype<SwapchainSource, SDL3SwapchainSource>(desc.Source);
                Window = sdlSource.Window;

                if (Window != null)
                {
                    if (!SDL_ClaimWindowForGPUDevice(Device, Window))
                        throw new InvalidOperationException("Failed to claim window for SDL GPU device.");
                }

                SDLSwapchain = new SDL3Swapchain(this, ref desc);
            }
        }

        public override bool AllowTearing
        {
            get => SDLSwapchain.AllowTearing;
            set => SDLSwapchain.AllowTearing = value;
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            SDL3Fence sdlFence = Util.AssertSubtype<Fence, SDL3Fence>(fence);
            SDL_GPUFence* nativeFence = sdlFence.Fence;
            return SDL_WaitForGPUFences(Device, true, &nativeFence, 1);
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            SDL_GPUFence** nativeFences = stackalloc SDL_GPUFence*[fences.Length];

            for (int i = 0; i < fences.Length; i++)
            {
                SDL3Fence sdlFence = Util.AssertSubtype<Fence, SDL3Fence>(fences[i]);
                nativeFences[i] = sdlFence.Fence;
            }

            return SDL_WaitForGPUFences(Device, true, nativeFences, (uint)fences.Length);
        }

        public override void ResetFence(Fence fence)
        {
            SDL3Fence sdlFence = Util.AssertSubtype<Fence, SDL3Fence>(fence);
            sdlFence.Reset();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            return TextureSampleCount.Count16;
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore()
        {
            return 16;
        }

        internal override uint GetStructuredBufferMinOffsetAlignmentCore()
        {
            return 16;
        }

        protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource)
        {
            IntPtr mappedPtr;
            uint sizeInBytes;
            uint offset = 0;
            uint rowPitch = 0;
            uint depthPitch = 0;

            if (resource is SDL3Buffer buffer)
            {
                mappedPtr = SDL_MapGPUTransferBuffer(Device, buffer.TransferBuffer, true);
                sizeInBytes = buffer.SizeInBytes;
            }
            else
            {
                SDL3Texture texture = Util.AssertSubtype<IMappableResource, SDL3Texture>(resource);
                mappedPtr = SDL_MapGPUTransferBuffer(Device, texture.TransferBuffer, true);
                texture.GetSubresourceLayout(subresource, out sizeInBytes, out offset, out rowPitch, out depthPitch);
            }

            byte* dataPtr = (byte*)mappedPtr.ToPointer() + offset;
            return new MappedResource(resource, mode, (IntPtr)dataPtr, sizeInBytes, subresource, rowPitch, depthPitch);
        }

        protected override void UnmapCore(IMappableResource resource, uint subresource)
        {
            if (resource is SDL3Buffer buffer)
                SDL_UnmapGPUTransferBuffer(Device, buffer.TransferBuffer);
            else
            {
                SDL3Texture texture = Util.AssertSubtype<IMappableResource, SDL3Texture>(resource);
                SDL_UnmapGPUTransferBuffer(Device, texture.TransferBuffer);
            }
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence fence)
        {
            SDL3CommandList sdlCommandList = Util.AssertSubtype<CommandList, SDL3CommandList>(commandList);
            IntPtr fencePtr = (IntPtr)sdlCommandList.GetCompletionFence();

            if (fence != null)
            {
                SDL3Fence sdlFence = Util.AssertSubtype<Fence, SDL3Fence>(fence);
                sdlFence.SetNativeFence(sdlCommandList.GetCompletionFence());
            }

            submittedCLs.Add(fencePtr);
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            SDL_GPUFence** fences = stackalloc SDL_GPUFence*[submittedCLs.Count];

            for (int i = 0; i < submittedCLs.Count; i++)
                fences[i] = (SDL_GPUFence*)submittedCLs[i];

            SDL_WaitForGPUFences(Device, true, fences, (uint)submittedCLs.Count);
            for (int i = 0; i < submittedCLs.Count; i++)
                SDL_ReleaseGPUFence(Device, fences[i]);
        }

        private protected override void WaitForIdleCore()
        {
        }

        private protected override void WaitForNextFrameReadyCore()
        {
        }

        private protected override void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer)
        {
            SDL3Texture sdlTexture = Util.AssertSubtype<Texture, SDL3Texture>(texture);

            if ((sdlTexture.Usage & TextureUsage.Staging) > 0)
            {
                uint offset = FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(texture.Width, texture.Format), y, texture.Format) + FormatHelpers.GetRowPitch(x, texture.Format);

                byte* mapped = (byte*)SDL_MapGPUTransferBuffer(Device, sdlTexture.TransferBuffer, true);
                Unsafe.CopyBlock(mapped + offset, (byte*)source, sizeInBytes);
                SDL_UnmapGPUTransferBuffer(Device, sdlTexture.TransferBuffer);
            }
            else
            {
                SDL_GPUTransferBufferCreateInfo ci = new SDL_GPUTransferBufferCreateInfo
                {
                    usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                    size = sizeInBytes,
                };

                SDL_GPUTransferBuffer* copyBuffer = SDL_CreateGPUTransferBuffer(Device, &ci);

                byte* mapped = (byte*)SDL_MapGPUTransferBuffer(Device, copyBuffer, true);
                Unsafe.CopyBlock(mapped, (byte*)source, sizeInBytes);
                SDL_UnmapGPUTransferBuffer(Device, copyBuffer);

                SDL_GPUTextureTransferInfo srcInfo = new SDL_GPUTextureTransferInfo
                {
                    transfer_buffer = copyBuffer,
                    pixels_per_row = width,
                    rows_per_layer = height
                };

                SDL_GPUTextureRegion dstRegion = new SDL_GPUTextureRegion
                {
                    texture = sdlTexture.Texture,
                    mip_level = mipLevel,
                    layer = arrayLayer,
                    x = x,
                    y = y,
                    z = z,
                    w = width,
                    h = height,
                    d = depth
                };

                SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(Device);
                SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

                SDL_UploadToGPUTexture(copyPass, &srcInfo, &dstRegion, true);
                SDL_EndGPUCopyPass(copyPass);
                SDL_SubmitGPUCommandBuffer(commandBuffer);
                SDL_ReleaseGPUTransferBuffer(Device, copyBuffer);
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
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

                transferBuffer = SDL_CreateGPUTransferBuffer(Device, &ci);
                transferRegion = new SDL_GPUTransferBufferLocation
                {
                    transfer_buffer = transferBuffer,
                    offset = 0
                };
            }

            byte* mapped = (byte*)SDL_MapGPUTransferBuffer(Device, transferBuffer, true);
            Unsafe.CopyBlock(mapped + transferRegion.offset, (byte*)source, sizeInBytes);
            SDL_UnmapGPUTransferBuffer(Device, transferBuffer);

            SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(Device);
            SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

            SDL_GPUBufferRegion dstRegion = new SDL_GPUBufferRegion
            {
                buffer = sdlBuffer.Buffer,
                offset = bufferOffsetInBytes,
                size = sizeInBytes
            };

            SDL_UploadToGPUBuffer(copyPass, &transferRegion, &dstRegion, true);
            SDL_EndGPUCopyPass(copyPass);
            SDL_SubmitGPUCommandBuffer(commandBuffer);
            if (mustDisposeTransferBuffer)
                SDL_ReleaseGPUTransferBuffer(Device, transferBuffer);
        }

        private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties)
        {
            properties = new PixelFormatProperties(uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue);

            return SDL_GPUTextureSupportsFormat(Device,
                SDL3Formats.VdToSDLTextureFormat(format),
                SDL3Formats.VdToSDLTextureType(type, (usage & TextureUsage.Cubemap) > 0, false),
                SDL3Formats.VdToSDLTextureUsage(usage));
        }

        protected override void PlatformDispose()
        {
            SDLSwapchain?.Dispose();

            if (Device != null)
                SDL_DestroyGPUDevice(Device);
        }
    }
}

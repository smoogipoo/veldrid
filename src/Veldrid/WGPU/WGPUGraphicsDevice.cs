// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.WebGPU;
using Veldrid.MetalBindings;
using Veldrid.MTL;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUGraphicsDevice : GraphicsDevice
    {
        public override string DeviceName { get; }
        public override string VendorName { get; }

        public override GraphicsApiVersion ApiVersion => GraphicsApiVersion.Unknown;
        public override bool IsUvOriginTopLeft => true;
        public override bool IsDepthRangeZeroToOne => true;
        public override bool IsClipSpaceYInverted => false;

        public override ResourceFactory ResourceFactory { get; }
        public override Swapchain MainSwapchain { get; }
        public override GraphicsDeviceFeatures Features { get; }

        public readonly WebGPU WebGPU;

        public readonly Instance* NativeInstance;
        public readonly Surface* NativeSurface;
        public readonly Adapter* NativeAdapter;
        public readonly Device* NativeDevice;

        private readonly AdapterProperties adapterProperties;
        private readonly SupportedLimits deviceLimits;
        private readonly SurfaceCapabilities surfaceCapabilities;

        private readonly Queue* commandQueue;

        private readonly object resetEventsLock = new object();
        private readonly List<ManualResetEvent[]> resetEvents = new List<ManualResetEvent[]>();

        public WGPUGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription swapchainDesc)
        {
            WebGPU = WebGPU.GetApi();

            NativeInstance = WebGPU.CreateInstance(new InstanceDescriptor());
            NativeSurface = createSurface(swapchainDesc);
            NativeAdapter = requestAdapter(new RequestAdapterOptions
            {
                CompatibleSurface = NativeSurface,
            });

            uint featureCount = (uint)WebGPU.AdapterEnumerateFeatures(NativeAdapter, null);
            FeatureName[] features = new FeatureName[featureCount];
            WebGPU.AdapterEnumerateFeatures(NativeAdapter, features);

            // Todo: Not quite...
            Features = new GraphicsDeviceFeatures(
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true
            );

            NativeDevice = requestDevice(new DeviceDescriptor());
            WebGPU.DeviceSetUncapturedErrorCallback(NativeDevice, PfnErrorCallback.From(onUncapturedError), null);

            WebGPU.SurfaceGetCapabilities(NativeSurface, NativeAdapter, ref surfaceCapabilities);
            WebGPU.AdapterGetProperties(NativeAdapter, ref adapterProperties);
            WebGPU.DeviceGetLimits(NativeDevice, ref deviceLimits);

            commandQueue = WebGPU.DeviceGetQueue(NativeDevice);

            DeviceName = Marshal.PtrToStringUTF8((IntPtr)adapterProperties.DriverDescription);
            VendorName = Marshal.PtrToStringUTF8((IntPtr)adapterProperties.VendorName);

            ResourceFactory = new WGPUResourceFactory(this);
            MainSwapchain = new WGPUSwapchain(this, ref swapchainDesc);
        }

        private Surface* createSurface(SwapchainDescription swapchainDesc)
        {
            CAMetalLayer metalLayer;
            uint width;
            uint height;

            if (swapchainDesc.Source is NSWindowSwapchainSource nsWindowSource)
            {
                var nswindow = new NSWindow(nsWindowSource.NSWindow);
                var contentView = nswindow.contentView;
                var windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }
            }
            else if (swapchainDesc.Source is NSViewSwapchainSource nsViewSource)
            {
                var contentView = new NSView(nsViewSource.NSView);
                var windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }
            }
            else if (swapchainDesc.Source is UIViewSwapchainSource uiViewSource)
            {
                var uiView = new UIView(uiViewSource.UIView);
                var viewSize = uiView.frame.size;
                width = (uint)viewSize.width;
                height = (uint)viewSize.height;

                if (!CAMetalLayer.TryCast(uiView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    metalLayer.frame = uiView.frame;
                    metalLayer.opaque = true;
                    uiView.layer.addSublayer(metalLayer.NativePtr);
                }
            }
            else
                throw new VeldridException("A Metal Swapchain can only be created from an NSWindow, NSView, or UIView.");

            var format = swapchainDesc.ColorSrgb
                ? PixelFormat.B8G8R8A8UNormSRgb
                : PixelFormat.B8G8R8A8UNorm;

            metalLayer.pixelFormat = MtlFormats.VdToMtlPixelFormat(format, false);
            metalLayer.framebufferOnly = true;
            metalLayer.drawableSize = new CGSize(width, height);

            return WebGPU.InstanceCreateSurface(NativeInstance, new SurfaceDescriptor
            {
                NextInChain = WGPUUtil.Chain(new SurfaceDescriptorFromMetalLayer
                {
                    Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromMetalLayer },
                    Layer = (void*)metalLayer.NativePtr,
                })
            });
        }

        private Adapter* requestAdapter(RequestAdapterOptions options)
        {
            Adapter* result = null;

            using ManualResetEventSlim adapterRequestEvent = new ManualResetEventSlim();
            WebGPU.InstanceRequestAdapter(NativeInstance, options, PfnRequestAdapterCallback.From((status, adapter, message, userData) =>
            {
                if (status == RequestAdapterStatus.Success)
                    result = adapter;

                // ReSharper disable once AccessToDisposedClosure
                adapterRequestEvent.Set();
            }), null);

            adapterRequestEvent.Wait();

            return result;
        }

        private Device* requestDevice(DeviceDescriptor desc)
        {
            Device* result = null;

            using ManualResetEventSlim adapterRequestEvent = new ManualResetEventSlim();
            WebGPU.AdapterRequestDevice(NativeAdapter, desc, PfnRequestDeviceCallback.From((status, device, message, userData) =>
            {
                if (status == RequestDeviceStatus.Success)
                    result = device;

                // ReSharper disable once AccessToDisposedClosure
                adapterRequestEvent.Set();
            }), null);

            adapterRequestEvent.Wait();

            return result;
        }

        private void onUncapturedError(ErrorType type, byte* message, void* userData)
        {
        }

        public override GraphicsBackend BackendType => GraphicsBackend.WebGPU;

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            return Util.AssertSubtype<Fence, WGPUFence>(fence).Wait(nanosecondTimeout);
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            int msTimeout;
            if (nanosecondTimeout == ulong.MaxValue)
                msTimeout = -1;
            else
                msTimeout = (int)Math.Min(nanosecondTimeout / 1_000_000, int.MaxValue);

            var events = getResetEventArray(fences.Length);
            for (int i = 0; i < fences.Length; i++)
                events[i] = Util.AssertSubtype<Fence, WGPUFence>(fences[i]).ResetEvent;

            bool result;

            if (waitAll)
                result = WaitHandle.WaitAll(events.Cast<WaitHandle>().ToArray(), msTimeout);
            else
            {
                int index = WaitHandle.WaitAny(events.Cast<WaitHandle>().ToArray(), msTimeout);
                result = index != WaitHandle.WaitTimeout;
            }

            returnResetEventArray(events);

            return result;
        }

        public override void ResetFence(Fence fence)
        {
            Util.AssertSubtype<Fence, WGPUFence>(fence).Reset();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            throw new NotImplementedException();
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore() => deviceLimits.Limits.MinUniformBufferOffsetAlignment;

        internal override uint GetStructuredBufferMinOffsetAlignmentCore() => deviceLimits.Limits.MinStorageBufferOffsetAlignment;

        protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource)
        {
            throw new NotImplementedException();
        }

        protected override void UnmapCore(IMappableResource resource, uint subresource)
        {
            throw new NotImplementedException();
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence fence)
        {
            WGPUCommandList wgpuCommandList = Util.AssertSubtype<CommandList, WGPUCommandList>(commandList);

            CommandBuffer** buffer = stackalloc CommandBuffer*[1];
            buffer[0] = wgpuCommandList.ConsumeCommandBuffer();

            WebGPU.QueueSubmit(commandQueue, 1, buffer);
            WebGPU.CommandBufferRelease(buffer[0]);
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            WGPUSwapchain wgpuSwapchain = Util.AssertSubtype<Swapchain, WGPUSwapchain>(swapchain);
            wgpuSwapchain.Present();
        }

        private protected override void WaitForIdleCore()
        {
        }

        private protected override void WaitForNextFrameReadyCore()
        {
        }

        private protected override void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer)
        {
            throw new NotImplementedException();
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            throw new NotImplementedException();
        }

        private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties)
        {
            throw new NotImplementedException();
        }

        public override bool GetWebGPUInfo(out BackendInfoWebGPU info)
        {
            info = new BackendInfoWebGPU(this);
            return true;
        }

        private ManualResetEvent[] getResetEventArray(int length)
        {
            lock (resetEventsLock)
            {
                for (int i = resetEvents.Count - 1; i > 0; i--)
                {
                    var array = resetEvents[i];

                    if (array.Length == length)
                    {
                        resetEvents.RemoveAt(i);
                        return array;
                    }
                }
            }

            var newArray = new ManualResetEvent[length];
            return newArray;
        }

        private void returnResetEventArray(ManualResetEvent[] array)
        {
            lock (resetEventsLock) resetEvents.Add(array);
        }

        protected override void PlatformDispose()
        {
            if (NativeInstance != null)
                WebGPU.InstanceRelease(NativeInstance);
            if (NativeAdapter != null)
                WebGPU.AdapterRelease(NativeAdapter);
            if (NativeDevice != null)
                WebGPU.DeviceRelease(NativeDevice);

            MainSwapchain?.Dispose();
        }
    }
}

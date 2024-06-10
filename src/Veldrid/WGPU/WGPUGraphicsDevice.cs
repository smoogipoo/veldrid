// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Veldrid.MetalBindings;
using WebGPU;
using static WebGPU.WebGPU;

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

        public readonly WGPUInstance NativeInstance;
        public readonly WGPUSurface NativeSurface;
        public readonly WGPUAdapter NativeAdapter;
        public readonly WGPUDevice NativeDevice;

        private readonly WGPUAdapterProperties adapterProperties;
        private readonly WGPUSupportedLimits deviceLimits;
        private readonly WGPUSurfaceCapabilities surfaceCapabilities;

        private readonly WGPUQueue commandQueue;

        private readonly object resetEventsLock = new object();
        private readonly List<ManualResetEvent[]> resetEvents = new List<ManualResetEvent[]>();

        private static readonly Queue<WGPUFence> pending_submissions = new Queue<WGPUFence>();

        public WGPUGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription swapchainDesc)
        {
            WGPUInstanceDescriptor instanceDescriptor = default;
            NativeInstance = wgpuCreateInstance(&instanceDescriptor);
            NativeSurface = createSurface(swapchainDesc);
            NativeAdapter = requestAdapter(new WGPURequestAdapterOptions
            {
                compatibleSurface = NativeSurface
            });

            uint featureCount = (uint)wgpuAdapterEnumerateFeatures(NativeAdapter, null);
            WGPUFeatureName[] features = new WGPUFeatureName[featureCount];
            fixed (WGPUFeatureName* featurePtr = features)
                wgpuAdapterEnumerateFeatures(NativeAdapter, featurePtr);

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

            NativeDevice = requestDevice(new WGPUDeviceDescriptor());
            wgpuDeviceSetUncapturedErrorCallback(NativeDevice, &onUncapturedError, IntPtr.Zero);

            WGPUSurfaceCapabilities caps;
            wgpuSurfaceGetCapabilities(NativeSurface, NativeAdapter, &caps);
            surfaceCapabilities = caps;

            WGPUAdapterProperties props;
            wgpuAdapterGetProperties(NativeAdapter, &props);
            adapterProperties = props;

            WGPUSupportedLimits limits;
            wgpuDeviceGetLimits(NativeDevice, &limits);
            deviceLimits = limits;

            commandQueue = wgpuDeviceGetQueue(NativeDevice);

            DeviceName = Interop.GetString(adapterProperties.driverDescription);
            VendorName = Interop.GetString(adapterProperties.vendorName);

            ResourceFactory = new WGPUResourceFactory(this);
            MainSwapchain = new WGPUSwapchain(this, ref swapchainDesc);
        }

        private WGPUSurface createSurface(SwapchainDescription swapchainDesc)
        {
            if (swapchainDesc.Source is UwpSwapchainSource)
                throw new NotImplementedException();

            if (swapchainDesc.Source is Win32SwapchainSource win32Source)
            {
                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromWindowsHWND
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromWindowsHWND },
                        hinstance = win32Source.Hinstance,
                        hwnd = win32Source.Hwnd
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            if (swapchainDesc.Source is XlibSwapchainSource xlibSource)
            {
                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromXlibWindow
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromXlibWindow },
                        display = xlibSource.Display,
                        window = (uint)xlibSource.Window
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            if (swapchainDesc.Source is WaylandSwapchainSource waylandSource)
            {
                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromWaylandSurface
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromWaylandSurface },
                        display = waylandSource.Display,
                        surface = waylandSource.Surface
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            if (swapchainDesc.Source is NSWindowSwapchainSource nsWindowSource)
            {
                var nswindow = new NSWindow(nsWindowSource.NSWindow);
                var contentView = nswindow.contentView;

                if (!CAMetalLayer.TryCast(contentView.layer, out CAMetalLayer metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }

                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromMetalLayer
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromMetalLayer },
                        layer = metalLayer.NativePtr,
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            if (swapchainDesc.Source is NSViewSwapchainSource nsViewSource)
            {
                var contentView = new NSView(nsViewSource.NSView);

                if (!CAMetalLayer.TryCast(contentView.layer, out CAMetalLayer metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }

                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromMetalLayer
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromMetalLayer },
                        layer = metalLayer.NativePtr,
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            if (swapchainDesc.Source is UIViewSwapchainSource uiViewSource)
            {
                var uiView = new UIView(uiViewSource.UIView);

                if (!CAMetalLayer.TryCast(uiView.layer, out CAMetalLayer metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    metalLayer.frame = uiView.frame;
                    metalLayer.opaque = true;
                    uiView.layer.addSublayer(metalLayer.NativePtr);
                }

                WGPUSurfaceDescriptor desc = new WGPUSurfaceDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUSurfaceDescriptorFromMetalLayer
                    {
                        chain = new WGPUChainedStruct { sType = WGPUSType.SurfaceDescriptorFromMetalLayer },
                        layer = metalLayer.NativePtr,
                    })
                };

                return wgpuInstanceCreateSurface(NativeInstance, &desc);
            }

            throw new VeldridException($"Unsupported swap chain source: {swapchainDesc.Source.GetType()}.");
        }

        private WGPUAdapter requestAdapter(WGPURequestAdapterOptions options)
        {
            WGPUAdapter result;
            wgpuInstanceRequestAdapter(NativeInstance, &options, &onRequestAdapterCallback, new IntPtr(&result));
            return result;
        }

        private WGPUDevice requestDevice(WGPUDeviceDescriptor desc)
        {
            WGPUDevice result;
            wgpuAdapterRequestDevice(NativeAdapter, &desc, &onRequestDeviceCallback, new IntPtr(&result));
            return result;
        }

        [UnmanagedCallersOnly]
        private static void onRequestAdapterCallback(WGPURequestAdapterStatus status, WGPUAdapter adapter, sbyte* message, IntPtr userData)
        {
            if (status == WGPURequestAdapterStatus.Success)
                *(WGPUAdapter*)userData = adapter;
            else
                throw new VeldridException($"Could not get WebGPU adapter: {Interop.GetString(message)}");
        }

        [UnmanagedCallersOnly]
        private static void onRequestDeviceCallback(WGPURequestDeviceStatus status, WGPUDevice device, sbyte* message, IntPtr userData)
        {
            if (status == WGPURequestDeviceStatus.Success)
                *(WGPUDevice*)userData = device;
            else
                throw new VeldridException($"Could not get WebGPU device: {Interop.GetString(message)}");
        }

        [UnmanagedCallersOnly]
        private static void onUncapturedError(WGPUErrorType type, sbyte* message, IntPtr userData)
        {
            Console.WriteLine($"[{type}] WebGPU: {Interop.GetString(message)}");
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

        internal override uint GetUniformBufferMinOffsetAlignmentCore() => deviceLimits.limits.minUniformBufferOffsetAlignment;

        internal override uint GetStructuredBufferMinOffsetAlignmentCore() => deviceLimits.limits.minStorageBufferOffsetAlignment;

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
            WGPUFence wgpuFence = fence == null ? null : Util.AssertSubtype<Fence, WGPUFence>(fence);

            WGPUCommandBuffer buffer = wgpuCommandList.ConsumeCommandBuffer();
            wgpuQueueSubmit(commandQueue, 1, &buffer);

            if (wgpuFence != null)
            {
                pending_submissions.Enqueue(wgpuFence);
                wgpuQueueOnSubmittedWorkDone(commandQueue, &onQueueWorkDone, IntPtr.Zero);
            }

            wgpuCommandBufferRelease(buffer);
        }

        [UnmanagedCallersOnly]
        private static void onQueueWorkDone(WGPUQueueWorkDoneStatus status, IntPtr userData)
        {
            pending_submissions.Dequeue().Set();
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
            WGPUTexture wgpuTexture = Util.AssertSubtype<Texture, WGPUTexture>(texture);

            // Todo: array layers?

            WGPUImageCopyTexture dest = new WGPUImageCopyTexture
            {
                texture = wgpuTexture.Texture,
                mipLevel = mipLevel,
                origin = new WGPUOrigin3D(x, y, z),
                aspect = WGPUTextureAspect.All
            };

            WGPUTextureDataLayout layout = new WGPUTextureDataLayout
            {
                offset = 0,
                bytesPerRow = FormatHelpers.GetRowPitch(width, wgpuTexture.Format),
                rowsPerImage = FormatHelpers.GetNumRows(height, wgpuTexture.Format)
            };

            WGPUExtent3D writeSize = new WGPUExtent3D(width, height, depth);

            wgpuQueueWriteTexture(commandQueue, &dest, (void*)source, sizeInBytes, &layout, &writeSize);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            WGPUBuffer wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(buffer);
            wgpuQueueWriteBuffer(commandQueue, wgpuBuffer.Buffer, bufferOffsetInBytes, (void*)source, sizeInBytes);
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
            if (NativeInstance.IsNotNull)
                wgpuInstanceRelease(NativeInstance);
            if (NativeAdapter.IsNotNull)
                wgpuAdapterRelease(NativeAdapter);
            if (NativeDevice.IsNotNull)
                wgpuDeviceRelease(NativeDevice);

            MainSwapchain?.Dispose();
        }
    }
}

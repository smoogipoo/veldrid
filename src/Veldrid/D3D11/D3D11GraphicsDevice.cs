using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using Vortice.Mathematics;
using Feature = Vortice.Direct3D11.Feature;
using MapFlags = Vortice.Direct3D11.MapFlags;
using VorticeDXGI = Vortice.DXGI.DXGI;
using VorticeD3D11 = Vortice.Direct3D11.D3D11;

namespace Veldrid.D3D11
{
    internal class D3D11GraphicsDevice : GraphicsDevice
    {
        public override string DeviceName { get; }

        public override string VendorName { get; }

        public override GraphicsApiVersion ApiVersion { get; }

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D11;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => false;

        public override ResourceFactory ResourceFactory => d3d11ResourceFactory;

        public ComPtr<ID3D11Device> Device => device;

        public IDXGIAdapter Adapter => dxgiAdapter;

        public bool IsDebugEnabled { get; }

        public bool SupportsConcurrentResources => supportsConcurrentResources;

        public bool SupportsCommandLists => supportsCommandLists;

        public int DeviceId { get; }

        public override Swapchain MainSwapchain => mainSwapchain;

        public override GraphicsDeviceFeatures Features { get; }

        public override bool AllowTearing
        {
            get => mainSwapchain.AllowTearing;
            set => mainSwapchain.AllowTearing = value;
        }

        private readonly IDXGIAdapter dxgiAdapter;
        private readonly ID3D11Device device;
        private readonly ID3D11DeviceContext immediateContext;
        private readonly D3D11ResourceFactory d3d11ResourceFactory;
        private readonly D3D11Swapchain mainSwapchain;
        private readonly bool supportsConcurrentResources;
        private readonly bool supportsCommandLists;
        private readonly object immediateContextLock = new object();
        private readonly BackendInfoD3D11 d3d11Info;

        private readonly object mappedResourceLock = new object();

        private readonly Dictionary<MappedResourceCacheKey, MappedResourceInfo> mappedResources
            = new Dictionary<MappedResourceCacheKey, MappedResourceInfo>();

        private readonly object stagingResourcesLock = new object();
        private readonly List<D3D11Buffer> availableStagingBuffers = new List<D3D11Buffer>();

        private readonly object resetEventsLock = new object();
        private readonly List<ManualResetEvent[]> resetEvents = new List<ManualResetEvent[]>();

        public D3D11GraphicsDevice(GraphicsDeviceOptions options, D3D11DeviceOptions d3D11DeviceOptions, SwapchainDescription? swapchainDesc)
            : this(mergeOptions(d3D11DeviceOptions, options), swapchainDesc)
        {
        }

        public D3D11GraphicsDevice(D3D11DeviceOptions options, SwapchainDescription? swapchainDesc)
        {
            var flags = (DeviceCreationFlags)options.DeviceCreationFlags;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            // If debug flag set but SDK layers aren't available we can't enable debug.
            if (0 != (flags & DeviceCreationFlags.Debug) && !VorticeD3D11.SdkLayersAvailable()) flags &= ~DeviceCreationFlags.Debug;

            try
            {
                if (options.AdapterPtr != IntPtr.Zero)
                {
                    VorticeD3D11.D3D11CreateDevice(options.AdapterPtr,
                        DriverType.Hardware,
                        flags,
                        new[]
                        {
                            FeatureLevel.Level_11_1,
                            FeatureLevel.Level_11_0,
                            FeatureLevel.Level_10_0
                        },
                        out device).CheckError();
                }
                else
                {
                    VorticeD3D11.D3D11CreateDevice(IntPtr.Zero,
                        DriverType.Hardware,
                        flags,
                        new[]
                        {
                            FeatureLevel.Level_11_1,
                            FeatureLevel.Level_11_0,
                            FeatureLevel.Level_10_0
                        },
                        out device).CheckError();
                }
            }
            catch
            {
                VorticeD3D11.D3D11CreateDevice(IntPtr.Zero,
                    DriverType.Hardware,
                    flags,
                    Array.Empty<FeatureLevel>(),
                    out device).CheckError();
            }

            using (var dxgiDevice = device!.QueryInterface<IDXGIDevice>())
            {
                // Store a pointer to the DXGI adapter.
                // This is for the case of no preferred DXGI adapter, or fallback to WARP.
                dxgiDevice.GetAdapter(out dxgiAdapter).CheckError();

                var desc = dxgiAdapter.Description;
                DeviceName = desc.Description;
                VendorName = "id:" + ((uint)desc.VendorId).ToString("x8");
                DeviceId = desc.DeviceId;
            }

            switch (device.FeatureLevel)
            {
                case FeatureLevel.Level_10_0:
                    ApiVersion = new GraphicsApiVersion(10, 0, 0, 0);
                    break;

                case FeatureLevel.Level_10_1:
                    ApiVersion = new GraphicsApiVersion(10, 1, 0, 0);
                    break;

                case FeatureLevel.Level_11_0:
                    ApiVersion = new GraphicsApiVersion(11, 0, 0, 0);
                    break;

                case FeatureLevel.Level_11_1:
                    ApiVersion = new GraphicsApiVersion(11, 1, 0, 0);
                    break;

                case FeatureLevel.Level_12_0:
                    ApiVersion = new GraphicsApiVersion(12, 0, 0, 0);
                    break;

                case FeatureLevel.Level_12_1:
                    ApiVersion = new GraphicsApiVersion(12, 1, 0, 0);
                    break;

                case FeatureLevel.Level_12_2:
                    ApiVersion = new GraphicsApiVersion(12, 2, 0, 0);
                    break;
            }

            if (swapchainDesc != null)
            {
                var desc = swapchainDesc.Value;
                mainSwapchain = new D3D11Swapchain(this, ref desc);
            }

            immediateContext = device.ImmediateContext;
            device.CheckThreadingSupport(out supportsConcurrentResources, out supportsCommandLists);

            IsDebugEnabled = (flags & DeviceCreationFlags.Debug) != 0;

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
                device.FeatureLevel >= FeatureLevel.Level_11_0,
                true,
                device.FeatureLevel >= FeatureLevel.Level_11_1,
                device.FeatureLevel >= FeatureLevel.Level_11_1,
                device.CheckFeatureSupport<FeatureDataDoubles>(Silk.NET.Direct3D11.Feature.Doubles).DoublePrecisionFloatShaderOps);

            d3d11ResourceFactory = new D3D11ResourceFactory(this);
            d3d11Info = new BackendInfoD3D11(this);

            PostDeviceCreated();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            var dxgiFormat = D3D11Formats.ToDxgiFormat(format, depthFormat);
            if (checkFormatMultisample(dxgiFormat, 32))
                return TextureSampleCount.Count32;
            if (checkFormatMultisample(dxgiFormat, 16))
                return TextureSampleCount.Count16;
            if (checkFormatMultisample(dxgiFormat, 8))
                return TextureSampleCount.Count8;
            if (checkFormatMultisample(dxgiFormat, 4))
                return TextureSampleCount.Count4;
            if (checkFormatMultisample(dxgiFormat, 2)) return TextureSampleCount.Count2;

            return TextureSampleCount.Count1;
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            return Util.AssertSubtype<Fence, D3D11Fence>(fence).Wait(nanosecondTimeout);
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
                events[i] = Util.AssertSubtype<Fence, D3D11Fence>(fences[i]).ResetEvent;

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
            Util.AssertSubtype<Fence, D3D11Fence>(fence).Reset();
        }

        public override bool GetD3D11Info(out BackendInfoD3D11 info)
        {
            info = d3d11Info;
            return true;
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore()
        {
            return 256u;
        }

        internal override uint GetStructuredBufferMinOffsetAlignmentCore()
        {
            return 16;
        }

        protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource)
        {
            var key = new MappedResourceCacheKey(resource, subresource);

            lock (mappedResourceLock)
            {
                if (mappedResources.TryGetValue(key, out var info))
                {
                    if (info.Mode != mode) throw new VeldridException("The given resource was already mapped with a different MapMode.");

                    info.RefCount += 1;
                    mappedResources[key] = info;
                }
                else
                {
                    // No current mapping exists -- create one.

                    if (resource is D3D11Buffer buffer)
                    {
                        lock (immediateContextLock)
                        {
                            var msr = immediateContext.Map(
                                buffer.Buffer,
                                0,
                                D3D11Formats.VdToD3D11MapMode((buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic, mode));

                            info.MappedResource = new MappedResource(resource, mode, msr.DataPointer, buffer.SizeInBytes);
                            info.RefCount = 1;
                            info.Mode = mode;
                            mappedResources.Add(key, info);
                        }
                    }
                    else
                    {
                        var texture = Util.AssertSubtype<IMappableResource, D3D11Texture>(resource);

                        lock (immediateContextLock)
                        {
                            Util.GetMipLevelAndArrayLayer(texture, subresource, out uint mipLevel, out uint arrayLayer);
                            immediateContext.Map(
                                texture.DeviceTexture,
                                (int)mipLevel,
                                (int)arrayLayer,
                                D3D11Formats.VdToD3D11MapMode(false, mode),
                                MapFlags.None,
                                out int _,
                                out MappedSubresource msr);

                            info.MappedResource = new MappedResource(
                                resource,
                                mode,
                                msr.DataPointer,
                                texture.Height * (uint)msr.RowPitch,
                                subresource,
                                (uint)msr.RowPitch,
                                (uint)msr.DepthPitch);
                            info.RefCount = 1;
                            info.Mode = mode;
                            mappedResources.Add(key, info);
                        }
                    }
                }

                return info.MappedResource;
            }
        }

        protected override void UnmapCore(IMappableResource resource, uint subresource)
        {
            var key = new MappedResourceCacheKey(resource, subresource);

            lock (mappedResourceLock)
            {
                if (!mappedResources.TryGetValue(key, out var info)) throw new VeldridException($"The given resource ({resource}) is not mapped.");

                info.RefCount -= 1;
                bool commitUnmap = info.RefCount == 0;

                if (commitUnmap)
                {
                    lock (immediateContextLock)
                    {
                        if (resource is D3D11Buffer buffer)
                            immediateContext.Unmap(buffer.Buffer, 0);
                        else
                        {
                            var texture = Util.AssertSubtype<IMappableResource, D3D11Texture>(resource);
                            immediateContext.Unmap(texture.DeviceTexture, (int)subresource);
                        }

                        bool result = mappedResources.Remove(key);
                        Debug.Assert(result);
                    }
                }
                else
                    mappedResources[key] = info;
            }
        }

        protected override void PlatformDispose()
        {
            // Dispose staging buffers
            foreach (D3D11Buffer buffer in availableStagingBuffers)
                buffer.Dispose();

            availableStagingBuffers.Clear();

            d3d11ResourceFactory.Dispose();
            mainSwapchain?.Dispose();
            immediateContext.Dispose();

            if (IsDebugEnabled)
            {
                uint refCount = device.Release();

                if (refCount > 0)
                {
                    var deviceDebug = device.QueryInterfaceOrNull<ID3D11Debug>();

                    if (deviceDebug != null)
                    {
                        deviceDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Summary | ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
                        deviceDebug.Dispose();
                    }
                }

                dxgiAdapter.Dispose();

                // Report live objects using DXGI if available (DXGIGetDebugInterface1 will fail on pre Windows 8 OS).
                if (VorticeDXGI.DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Success)
                {
                    dxgiDebug.ReportLiveObjects(VorticeDXGI.DebugAll, ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.IgnoreInternal);
                    dxgiDebug.Dispose();
                }
            }
            else
            {
                device.Dispose();
                dxgiAdapter.Dispose();
            }
        }

        private static D3D11DeviceOptions mergeOptions(D3D11DeviceOptions d3D11DeviceOptions, GraphicsDeviceOptions options)
        {
            if (options.Debug) d3D11DeviceOptions.DeviceCreationFlags |= (uint)DeviceCreationFlags.Debug;

            return d3D11DeviceOptions;
        }

        private bool checkFormatMultisample(Format format, int sampleCount)
        {
            return device.CheckMultisampleQualityLevels(format, sampleCount) != 0;
        }

        private D3D11Buffer getFreeStagingBuffer(uint sizeInBytes)
        {
            lock (stagingResourcesLock)
            {
                foreach (var buffer in availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= sizeInBytes)
                    {
                        availableStagingBuffers.Remove(buffer);
                        return buffer;
                    }
                }
            }

            var staging = ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(staging);
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

        private protected override void SubmitCommandsCore(CommandList cl, Fence fence)
        {
            var d3d11Cl = Util.AssertSubtype<CommandList, D3D11CommandList>(cl);

            lock (immediateContextLock)
            {
                if (d3d11Cl.DeviceCommandList != null) // CommandList may have been reset in the meantime (resized swapchain).
                {
                    immediateContext.ExecuteCommandList(d3d11Cl.DeviceCommandList, false);
                    d3d11Cl.OnCompleted();
                }
            }

            if (fence is D3D11Fence d3d11Fence) d3d11Fence.Set();
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            lock (immediateContextLock)
            {
                var d3d11Sc = Util.AssertSubtype<Swapchain, D3D11Swapchain>(swapchain);
                d3d11Sc.DxgiSwapChain.Present(d3d11Sc.SyncInterval, d3d11Sc.PresentFlags);
            }
        }

        private protected override bool GetPixelFormatSupportCore(
            PixelFormat format,
            TextureType type,
            TextureUsage usage,
            out PixelFormatProperties properties)
        {
            if (D3D11Formats.IsUnsupportedFormat(format))
            {
                properties = default;
                return false;
            }

            var dxgiFormat = D3D11Formats.ToDxgiFormat(format, (usage & TextureUsage.DepthStencil) != 0);
            var fs = device.CheckFormatSupport(dxgiFormat);

            if (((usage & TextureUsage.RenderTarget) != 0 && (fs & FormatSupport.RenderTarget) == 0)
                || ((usage & TextureUsage.DepthStencil) != 0 && (fs & FormatSupport.DepthStencil) == 0)
                || ((usage & TextureUsage.Sampled) != 0 && (fs & FormatSupport.ShaderSample) == 0)
                || ((usage & TextureUsage.Cubemap) != 0 && (fs & FormatSupport.TextureCube) == 0)
                || ((usage & TextureUsage.Storage) != 0 && (fs & FormatSupport.TypedUnorderedAccessView) == 0))
            {
                properties = default;
                return false;
            }

            const uint max_texture_dimension = 16384;
            const uint max_volume_extent = 2048;

            uint sampleCounts = 0;
            if (checkFormatMultisample(dxgiFormat, 1)) sampleCounts |= 1 << 0;

            if (checkFormatMultisample(dxgiFormat, 2)) sampleCounts |= 1 << 1;

            if (checkFormatMultisample(dxgiFormat, 4)) sampleCounts |= 1 << 2;

            if (checkFormatMultisample(dxgiFormat, 8)) sampleCounts |= 1 << 3;

            if (checkFormatMultisample(dxgiFormat, 16)) sampleCounts |= 1 << 4;

            if (checkFormatMultisample(dxgiFormat, 32)) sampleCounts |= 1 << 5;

            properties = new PixelFormatProperties(
                max_texture_dimension,
                type == TextureType.Texture1D ? 1 : max_texture_dimension,
                type != TextureType.Texture3D ? 1 : max_volume_extent,
                uint.MaxValue,
                type == TextureType.Texture3D ? 1 : max_volume_extent,
                sampleCounts);
            return true;
        }

        private protected override unsafe void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            var d3dBuffer = Util.AssertSubtype<DeviceBuffer, D3D11Buffer>(buffer);
            if (sizeInBytes == 0) return;

            bool isDynamic = (buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            bool isStaging = (buffer.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            bool isUniformBuffer = (buffer.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer;
            bool updateFullBuffer = bufferOffsetInBytes == 0 && sizeInBytes == buffer.SizeInBytes;
            bool useUpdateSubresource = !isDynamic && !isStaging && (!isUniformBuffer || updateFullBuffer);
            bool useMap = (isDynamic && updateFullBuffer) || isStaging;

            if (useUpdateSubresource)
            {
                Box? subregion = new Box((int)bufferOffsetInBytes, 0, 0, (int)(sizeInBytes + bufferOffsetInBytes), 1, 1);

                if (isUniformBuffer) subregion = null;

                lock (immediateContextLock) immediateContext.UpdateSubresource(d3dBuffer.Buffer, 0, subregion, source, 0, 0);
            }
            else if (useMap)
            {
                var mr = MapCore(buffer, MapMode.Write, 0);

                if (sizeInBytes < 1024)
                    Unsafe.CopyBlock((byte*)mr.Data + bufferOffsetInBytes, source.ToPointer(), sizeInBytes);
                else
                {
                    Buffer.MemoryCopy(
                        source.ToPointer(),
                        (byte*)mr.Data + bufferOffsetInBytes,
                        buffer.SizeInBytes,
                        sizeInBytes);
                }

                UnmapCore(buffer, 0);
            }
            else
            {
                var staging = getFreeStagingBuffer(sizeInBytes);
                UpdateBuffer(staging, 0, source, sizeInBytes);
                var sourceRegion = new Box(0, 0, 0, (int)sizeInBytes, 1, 1);

                lock (immediateContextLock)
                {
                    immediateContext.CopySubresourceRegion(
                        d3dBuffer.Buffer, 0, (int)bufferOffsetInBytes, 0, 0,
                        staging.Buffer, 0,
                        sourceRegion);
                }

                lock (stagingResourcesLock) availableStagingBuffers.Add(staging);
            }
        }

        private protected override unsafe void UpdateTextureCore(
            Texture texture,
            IntPtr source,
            uint sizeInBytes,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
            var d3dTex = Util.AssertSubtype<Texture, D3D11Texture>(texture);
            bool useMap = (texture.Usage & TextureUsage.Staging) == TextureUsage.Staging;

            if (useMap)
            {
                uint subresource = texture.CalculateSubresource(mipLevel, arrayLayer);
                var map = MapCore(texture, MapMode.Write, subresource);

                uint denseRowSize = FormatHelpers.GetRowPitch(width, texture.Format);
                uint denseSliceSize = FormatHelpers.GetDepthPitch(denseRowSize, height, texture.Format);

                Util.CopyTextureRegion(
                    source.ToPointer(),
                    0, 0, 0,
                    denseRowSize, denseSliceSize,
                    map.Data.ToPointer(),
                    x, y, z,
                    map.RowPitch, map.DepthPitch,
                    width, height, depth,
                    texture.Format);

                UnmapCore(texture, subresource);
            }
            else
            {
                int subresource = D3D11Util.ComputeSubresource(mipLevel, texture.MipLevels, arrayLayer);
                var resourceRegion = new Box(
                    (int)x,
                    right: (int)(x + width),
                    top: (int)y,
                    front: (int)z,
                    bottom: (int)(y + height),
                    back: (int)(z + depth));

                uint srcRowPitch = FormatHelpers.GetRowPitch(width, texture.Format);
                uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, texture.Format);

                lock (immediateContextLock)
                {
                    immediateContext.UpdateSubresource(
                        d3dTex.DeviceTexture,
                        subresource,
                        resourceRegion,
                        source,
                        (int)srcRowPitch,
                        (int)srcDepthPitch);
                }
            }
        }

        private protected override void WaitForIdleCore()
        {
        }

        private protected override void WaitForNextFrameReadyCore()
        {
            mainSwapchain.WaitForNextFrameReady();
        }
    }
}

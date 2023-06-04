using System;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using VorticeDXGI = Vortice.DXGI.DXGI;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;

namespace Veldrid.D3D12
{
    internal class D3D12GraphicsDevice : GraphicsDevice
    {
        private readonly IDXGIAdapter _dxgiAdapter;
        private readonly ID3D12Device _device;
        private readonly string _deviceName;
        private readonly string _vendorName;
        private readonly GraphicsApiVersion _apiVersion;
        private readonly int _deviceId;
        private readonly bool _supportsConcurrentResources;
        private readonly bool _supportsCommandLists;

        public override string DeviceName => _deviceName;
        public override string VendorName => _vendorName;

        public override GraphicsApiVersion ApiVersion => _apiVersion;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => false;

        public override ResourceFactory ResourceFactory => null;

        public ID3D12Device Device => _device;

        public IDXGIAdapter Adapter => _dxgiAdapter;

        public bool IsDebugEnabled { get; }

        public bool SupportsConcurrentResources => _supportsConcurrentResources;

        public bool SupportsCommandLists => _supportsCommandLists;

        public int DeviceId => _deviceId;

        public override Swapchain MainSwapchain => null;

        public override GraphicsDeviceFeatures Features { get; }

        public D3D12GraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc)
        {
            VorticeD3D12.D3D12CreateDevice(IntPtr.Zero, out _device).CheckError();

            using (IDXGIDevice dxgiDevice = _device.QueryInterface<IDXGIDevice>())
            {
                // Store a pointer to the DXGI adapter.
                // This is for the case of no preferred DXGI adapter, or fallback to WARP.
                dxgiDevice.GetAdapter(out _dxgiAdapter).CheckError();

                AdapterDescription desc = _dxgiAdapter.Description;
                _deviceName = desc.Description;
                _vendorName = "id:" + ((uint)desc.VendorId).ToString("x8");
                _deviceId = desc.DeviceId;
            }

            switch (_device.CheckMaxSupportedFeatureLevel())
            {
                case FeatureLevel.Level_10_0:
                    _apiVersion = new GraphicsApiVersion(10, 0, 0, 0);
                    break;

                case FeatureLevel.Level_10_1:
                    _apiVersion = new GraphicsApiVersion(10, 1, 0, 0);
                    break;

                case FeatureLevel.Level_11_0:
                    _apiVersion = new GraphicsApiVersion(11, 0, 0, 0);
                    break;

                case FeatureLevel.Level_11_1:
                    _apiVersion = new GraphicsApiVersion(11, 1, 0, 0);
                    break;

                case FeatureLevel.Level_12_0:
                    _apiVersion = new GraphicsApiVersion(12, 0, 0, 0);
                    break;

                case FeatureLevel.Level_12_1:
                    _apiVersion = new GraphicsApiVersion(12, 1, 0, 0);
                    break;

                case FeatureLevel.Level_12_2:
                    _apiVersion = new GraphicsApiVersion(12, 2, 0, 0);
                    break;
            }

            PostDeviceCreated();
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore()
        {
            throw new NotImplementedException();
        }

        internal override uint GetStructuredBufferMinOffsetAlignmentCore()
        {
            throw new NotImplementedException();
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence fence)
        {
            throw new NotImplementedException();
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            throw new NotImplementedException();
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            throw new NotImplementedException();
        }

        public override void ResetFence(Fence fence)
        {
            throw new NotImplementedException();
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            throw new NotImplementedException();
        }

        private protected override void WaitForIdleCore()
        {
            throw new NotImplementedException();
        }

        private protected override void WaitForNextFrameReadyCore()
        {
            throw new NotImplementedException();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            throw new NotImplementedException();
        }

        protected override MappedResource MapCore(MappableResource resource, MapMode mode, uint subresource)
        {
            throw new NotImplementedException();
        }

        protected override void UnmapCore(MappableResource resource, uint subresource)
        {
            throw new NotImplementedException();
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

        protected override void PlatformDispose()
        {
            throw new NotImplementedException();
        }
    }
}

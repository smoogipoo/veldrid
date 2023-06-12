using System;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal class D3D12ResourceFactory : ResourceFactory, IDisposable
    {
        private readonly D3D12GraphicsDevice _gd;
        private readonly ID3D12Device _device;
        private readonly D3D12ResourceCache _cache;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        public D3D12ResourceFactory(D3D12GraphicsDevice gd)
            : base(gd.Features)
        {
            _gd = gd;
            _device = gd.Device;
            _cache = new D3D12ResourceCache(_device);
        }

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            return new D3D12CommandList(_gd, ref description);
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            return new D3D12Framebuffer(_device, ref description);
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            return new D3D12Pipeline(_cache, ref description);
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            return new D3D12Pipeline(_cache, ref description);
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            return new D3D12ResourceLayout(ref description);
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            ValidationHelpers.ValidateResourceSet(_gd, ref description);
            return new D3D12ResourceSet(ref description);
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            return new D3D12Sampler(_device, ref description);
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            return new D3D12Shader(_device, description);
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            return new D3D12Texture(_device, ref description);
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            ID3D12Resource existingTexture = new ID3D12Resource((IntPtr)nativeTexture);
            return new D3D12Texture(existingTexture, description.Type, description.Format);
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            return null;

            // Todo:
            // return new D3D12TextureView(_gd, ref description);
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            return new D3D12Buffer(
                _device,
                description.SizeInBytes,
                description.Usage,
                description.StructureByteStride,
                description.RawBuffer);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new D3D12Fence(signaled);
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            return new D3D12Swapchain(_gd, ref description);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Vortice.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11ResourceFactory : ResourceFactory, IDisposable
    {
        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D11;
        private readonly D3D11GraphicsDevice gd;
        private readonly ComPtr<ID3D11Device> device;
        private readonly D3D11ResourceCache cache;

        public D3D11ResourceFactory(D3D11GraphicsDevice gd)
            : base(gd.Features)
        {
            this.gd = gd;
            device = gd.Device;
            cache = new D3D11ResourceCache(device);
        }

        #region Disposal

        public void Dispose()
        {
            cache.Dispose();
        }

        #endregion

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            return new D3D11CommandList(gd, ref description);
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            return new D3D11Framebuffer(device, ref description);
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            return new D3D11Pipeline(cache, ref description);
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            return new D3D11ResourceLayout(ref description);
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            ValidationHelpers.ValidateResourceSet(gd, ref description);
            return new D3D11ResourceSet(ref description);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new D3D11Fence(signaled);
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            return new D3D11Swapchain(gd, ref description);
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            return new D3D11Pipeline(cache, ref description);
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            return new D3D11Sampler(device, ref description);
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            return new D3D11Shader(device, description);
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            return new D3D11Texture(device, ref description);
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            var existingTexture = new ComPtr<ID3D11Texture2D>(new ComObject(Marshal.GetObjectForIUnknown((IntPtr)nativeTexture)));
            return new D3D11Texture(existingTexture, description.Type, description.Format);
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            return new D3D11TextureView(gd, ref description);
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            return new D3D11Buffer(
                device,
                description.SizeInBytes,
                description.Usage,
                description.StructureByteStride,
                description.RawBuffer);
        }
    }
}

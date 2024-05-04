// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceFactory : ResourceFactory
    {
        public override GraphicsBackend BackendType => gd.BackendType;

        private readonly WGPUGraphicsDevice gd;

        public WGPUResourceFactory(WGPUGraphicsDevice gd)
            : base(gd.Features)
        {
            this.gd = gd;
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            throw new NotImplementedException();
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
            => new WGPUFramebuffer(gd, ref description);

        public override CommandList CreateCommandList(ref CommandListDescription description)
            => new WGPUCommandList(gd, ref description);

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            throw new NotImplementedException();
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            throw new NotImplementedException();
        }

        public override Fence CreateFence(bool signaled)
        {
            throw new NotImplementedException();
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
            => new WGPUSwapchain(gd, ref description);

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
            => new WGPUTexture(gd, ref description, (Silk.NET.WebGPU.Texture*)nativeTexture);

        protected override Texture CreateTextureCore(ref TextureDescription description)
            => new WGPUTexture(gd, ref description);

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
            => throw new NotImplementedException();

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
            => new WGPUBuffer(gd, ref description);

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
            => new WGPUSampler(gd, ref description);

        protected override Shader CreateShaderCore(ref ShaderDescription description)
            => new WGPUShader(gd, ref description);
    }
}

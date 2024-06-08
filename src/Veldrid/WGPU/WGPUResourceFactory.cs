// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            => new WGPUPipeline(gd, ref description);

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
            => new WGPUFramebuffer(gd, ref description);

        public override CommandList CreateCommandList(ref CommandListDescription description)
            => new WGPUCommandList(gd, ref description);

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
            => new WGPUResourceLayout(gd, ref description);

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
            => new WGPUResourceSet(gd, ref description);

        public override Fence CreateFence(bool signaled)
            => new WGPUFence(signaled);

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
            => new WGPUSwapchain(gd, ref description);

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
            => new WGPUPipeline(gd, ref description);

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
            => new WGPUTexture(gd, ref description, *(WebGPU.WGPUTexture*)nativeTexture);

        protected override Texture CreateTextureCore(ref TextureDescription description)
            => new WGPUTexture(gd, ref description);

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
            => new WGPUTextureView(gd, ref description);

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
            => new WGPUBuffer(gd, ref description);

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
            => new WGPUSampler(gd, ref description);

        protected override Shader CreateShaderCore(ref ShaderDescription description)
            => new WGPUShader(gd, ref description);
    }
}

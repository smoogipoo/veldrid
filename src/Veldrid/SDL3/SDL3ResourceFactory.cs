// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using SDL;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3ResourceFactory : ResourceFactory
    {
        private readonly SDL3GraphicsDevice gd;

        public SDL3ResourceFactory(SDL3GraphicsDevice gd, GraphicsDeviceFeatures features)
            : base(features)
        {
            this.gd = gd;
        }

        public override GraphicsBackend BackendType => gd.BackendType;

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
            => new SDL3ComputePipeline(gd, ref description);

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
            => new SDL3Framebuffer(ref description);

        public override CommandList CreateCommandList(ref CommandListDescription description)
            => new SDL3CommandList(gd, ref description);

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
            => new SDL3ResourceLayout(ref description);

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
            => new SDL3ResourceSet(ref description);

        public override Fence CreateFence(bool signaled)
            => new SDL3Fence(signaled);

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
            => new SDL3Swapchain(gd, ref description);

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
            => new SDL3GraphicsPipeline(gd, ref description);

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            SDL3ExternalTexture texture = new SDL3ExternalTexture();
            texture.SetNativeTexture((SDL_GPUTexture*)nativeTexture, ref description);
            return texture;
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
            => new SDL3Texture(gd, ref description);

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
            => throw new NotSupportedException();

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
            => new SDL3Buffer(gd, ref description);

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
            => new SDL3Sampler(gd, ref description);

        protected override Shader CreateShaderCore(ref ShaderDescription description)
            => new SDL3Shader(gd, ref description);
    }
}

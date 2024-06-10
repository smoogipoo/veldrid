// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTexture : WGPUTexture
    {
        private readonly WGPUSwapchainTextureView view;

        public WGPUSwapchainTexture(WGPUSwapchain swapchain, ref TextureDescription description)
            : base(ref description, default)
        {
            view = new WGPUSwapchainTextureView(swapchain, new TextureViewDescription(this));
        }

        private protected override TextureView CreateFullTextureView(GraphicsDevice gd) => view;

        public override void Dispose()
        {
            base.Dispose();
            view?.Dispose();
        }
    }
}

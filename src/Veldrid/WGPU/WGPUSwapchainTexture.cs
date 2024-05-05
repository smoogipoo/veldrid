// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU.Extensions.Dawn;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTexture : WGPUTexture
    {
        private readonly WGPUSwapchainTextureView view;

        public WGPUSwapchainTexture(WGPUGraphicsDevice gd, SwapChain* swapChain, ref TextureDescription description)
            : base(gd, ref description)
        {
            view = new WGPUSwapchainTextureView(gd, swapChain, new TextureViewDescription(this));
        }

        public void ReleaseView() => view.Release();

        private protected override TextureView CreateFullTextureView(GraphicsDevice gd) => view;
    }
}

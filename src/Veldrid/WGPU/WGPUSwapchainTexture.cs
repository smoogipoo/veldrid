// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTexture : WGPUTexture
    {
        public readonly WGPUSwapchain Swapchain;

        public WGPUSwapchainTexture(WGPUSwapchain swapchain, ref TextureDescription description)
            : base(ref description, default)
        {
            Swapchain = swapchain;
        }
    }
}

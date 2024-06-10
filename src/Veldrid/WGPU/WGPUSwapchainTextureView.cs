// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTextureView : WGPUTextureViewBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUSwapchain swapchain;

        private bool isDisposed;

        public WGPUSwapchainTextureView(WGPUSwapchain swapchain, TextureViewDescription description)
            : base(ref description)
        {
            this.swapchain = swapchain;
        }

        public override WebGPU.WGPUTextureView View => swapchain.TextureView;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

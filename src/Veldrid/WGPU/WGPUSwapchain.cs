// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchain : Swapchain
    {
        public override string Name { get; set; }
        public override Framebuffer Framebuffer => framebuffer;
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;
        private readonly WGPUSwapchainFramebuffer framebuffer;
        private readonly WGPUTextureFormat colorFormat;

        private bool isDisposed;

        public WGPUSwapchain(WGPUGraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;

            colorFormat = description.ColorSrgb ? WGPUTextureFormat.BGRA8UnormSrgb : WGPUTextureFormat.BGRA8Unorm;
            framebuffer = new WGPUSwapchainFramebuffer(gd, colorFormat, description.DepthFormat);

            Resize(description.Width, description.Height);
        }

        public override bool SyncToVerticalBlank { get; set; }

        public override void Resize(uint width, uint height)
        {
            WGPUSurfaceConfiguration config = new WGPUSurfaceConfiguration
            {
                device = gd.NativeDevice,
                width = width,
                height = height,
                format = colorFormat,
                usage = WGPUTextureUsage.RenderAttachment,
                presentMode = WGPUPresentMode.Fifo
            };

            wgpuSurfaceConfigure(gd.NativeSurface, &config);

            framebuffer.Resize(width, height);
        }

        public void Present()
        {
            framebuffer.ReleaseView();
            wgpuSurfacePresent(gd.NativeSurface);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            Framebuffer?.Dispose();

            isDisposed = true;
        }
    }
}

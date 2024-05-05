// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchain : Swapchain
    {
        public override string Name { get; set; }
        public override Framebuffer Framebuffer => framebuffer;
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;
        private readonly WGPUSwapchainFramebuffer framebuffer;
        private readonly TextureFormat colorFormat;

        private bool isDisposed;

        public WGPUSwapchain(WGPUGraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;

            colorFormat = description.ColorSrgb ? TextureFormat.Bgra8UnormSrgb : TextureFormat.Bgra8Unorm;
            framebuffer = new WGPUSwapchainFramebuffer(gd, colorFormat, description.DepthFormat);

            Resize(description.Width, description.Height);
        }

        public override bool SyncToVerticalBlank { get; set; }

        public override void Resize(uint width, uint height)
        {
            gd.WebGPU.SurfaceConfigure(gd.NativeSurface, new SurfaceConfiguration
            {
                Device = gd.NativeDevice,
                Width = width,
                Height = height,
                Format = colorFormat,
                Usage = Silk.NET.WebGPU.TextureUsage.RenderAttachment,
                PresentMode = PresentMode.Fifo
            });

            framebuffer.Resize(width, height);
        }

        public void Present()
        {
            framebuffer.ReleaseView();
            gd.WebGPU.SurfacePresent(gd.NativeSurface);
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

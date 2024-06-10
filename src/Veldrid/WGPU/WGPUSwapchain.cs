// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchain : Swapchain
    {
        public override string Name { get; set; }
        public override Framebuffer Framebuffer => framebuffer;
        public override bool IsDisposed => isDisposed;

        public WebGPU.WGPUTextureView TextureView { get; private set; }

        private readonly WGPUGraphicsDevice gd;
        private readonly WGPUSwapchainFramebuffer framebuffer;
        private readonly WGPUTextureFormat colorFormat;

        private WGPUSurfaceTexture surfaceTexture;

        private uint width;
        private uint height;
        private bool isDisposed;

        public WGPUSwapchain(WGPUGraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;

            colorFormat = description.ColorSrgb ? WGPUTextureFormat.BGRA8UnormSrgb : WGPUTextureFormat.BGRA8Unorm;
            framebuffer = new WGPUSwapchainFramebuffer(gd, this, colorFormat, description.DepthFormat);

            Resize(description.Width, description.Height);
        }

        public override bool SyncToVerticalBlank { get; set; }

        public override void Resize(uint width, uint height)
        {
            ReleaseImage();

            this.width = width;
            this.height = height;

            WGPUSurfaceConfiguration config = new WGPUSurfaceConfiguration
            {
                device = gd.NativeDevice,
                width = width,
                height = height,
                format = colorFormat,
                usage = WGPUTextureUsage.RenderAttachment,
                presentMode = WGPUPresentMode.Mailbox
            };

            wgpuSurfaceConfigure(gd.NativeSurface, &config);

            framebuffer.Resize(width, height);

            AcquireNextImage();
        }

        public void AcquireNextImage()
        {
            WGPUSurfaceTexture surfaceTex;
            wgpuSurfaceGetCurrentTexture(gd.NativeSurface, &surfaceTex);

            switch (surfaceTex.status)
            {
                case WGPUSurfaceGetCurrentTextureStatus.Success:
                    break;

                case WGPUSurfaceGetCurrentTextureStatus.Timeout:
                case WGPUSurfaceGetCurrentTextureStatus.Outdated:
                case WGPUSurfaceGetCurrentTextureStatus.Lost:
                    Thread.Yield();
                    Resize(width, height);
                    return;

                default:
                    throw new VeldridException($"Failed to acquire swapchain image: {surfaceTex.status}");
            }

            surfaceTexture = surfaceTex;
            TextureView = wgpuTextureCreateView(surfaceTex.texture, null);
        }

        public void Present()
        {
            wgpuSurfacePresent(gd.NativeSurface);
        }

        public void ReleaseImage()
        {
            if (TextureView.IsNotNull)
                wgpuTextureViewRelease(TextureView);

            if (surfaceTexture.texture.IsNotNull)
                wgpuTextureRelease(surfaceTexture.texture);

            TextureView = default;
            surfaceTexture = default;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            Framebuffer?.Dispose();
            ReleaseImage();

            isDisposed = true;
        }
    }
}

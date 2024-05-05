// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.Dawn;

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

        private SwapChain* swapchain;
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
            if (swapchain != null)
                gd.Dawn.SwapChainRelease(swapchain);

            swapchain = gd.Dawn.DeviceCreateSwapChain(gd.NativeDevice, gd.NativeSurface, new SwapChainDescriptor
            {
                Width = width,
                Height = height,
                Format = colorFormat,
                Usage = Silk.NET.WebGPU.TextureUsage.RenderAttachment,
                PresentMode = PresentMode.Mailbox
            });

            framebuffer.SetSwapChain(swapchain, width, height);
        }

        public void Present()
        {
            framebuffer.ReleaseView();
            gd.Dawn.SwapChainPresent(swapchain);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (swapchain != null)
                gd.Dawn.SwapChainRelease(swapchain);

            Framebuffer?.Dispose();

            isDisposed = true;
        }
    }
}

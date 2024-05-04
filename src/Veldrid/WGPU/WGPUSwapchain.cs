// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.Dawn;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchain : Swapchain
    {
        public override string Name { get; set; }
        public override Framebuffer Framebuffer { get; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private SwapChain* swapchain;
        private bool isDisposed;

        public WGPUSwapchain(WGPUGraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;
        }

        public override bool SyncToVerticalBlank { get; set; }

        public override void Resize(uint width, uint height)
        {
            if (swapchain != null)
            {
                gd.Dawn.SwapChainRelease(swapchain);
                swapchain = null;
            }

            gd.Dawn.DeviceCreateSwapChain(gd.NativeDevice, gd.NativeSurface, new SwapChainDescriptor
            {
                Width = width,
                Height = height,
                Format = gd.WebGPU.SurfaceGetPreferredFormat(gd.NativeSurface, gd.NativeAdapter),
                Usage = Silk.NET.WebGPU.TextureUsage.RenderAttachment,
                PresentMode = PresentMode.Mailbox
            });
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (swapchain != null)
                gd.Dawn.SwapChainRelease(swapchain);

            isDisposed = true;
        }
    }
}

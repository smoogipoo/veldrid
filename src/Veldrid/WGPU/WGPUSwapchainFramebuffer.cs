// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainFramebuffer : WGPUFramebufferBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => colorTargets;

        public override FramebufferAttachment? DepthTarget => depthTarget;

        public override OutputDescription OutputDescription => outputDescription;

        public override uint Width => width;

        public override uint Height => height;

        private readonly WGPUGraphicsDevice gd;
        private readonly WGPUSwapchain swapchain;
        private readonly WGPUTextureFormat colorFormat;
        private readonly PixelFormat? depthFormat;

        private FramebufferAttachment[] colorTargets;
        private FramebufferAttachment? depthTarget;
        private OutputDescription outputDescription;

        private uint width;
        private uint height;

        private bool isDisposed;

        public WGPUSwapchainFramebuffer(WGPUGraphicsDevice gd, WGPUSwapchain swapchain, WGPUTextureFormat colorFormat, PixelFormat? depthFormat)
            : base(gd)
        {
            this.gd = gd;
            this.swapchain = swapchain;
            this.colorFormat = colorFormat;
            this.depthFormat = depthFormat;
        }

        public void Resize(uint width, uint height)
        {
            this.width = width;
            this.height = height;

            colorTargets?[0].Target.Dispose();
            depthTarget?.Target.Dispose();

            Util.EnsureArrayMinimumSize(ref colorTargets, 1);

            TextureDescription colorDescription = TextureDescription.Texture2D(width, height, 1, 1, WGPUFormats.WGPUToVdPixelFormat(colorFormat), TextureUsage.RenderTarget);
            colorTargets![0] = new FramebufferAttachment(new WGPUSwapchainTexture(swapchain, ref colorDescription), 0);

            if (depthFormat is PixelFormat depth)
            {
                TextureDescription depthDescription = TextureDescription.Texture2D(width, height, 1, 1, depth, TextureUsage.RenderTarget | TextureUsage.DepthStencil);
                depthTarget = new FramebufferAttachment(new WGPUTexture(gd, ref depthDescription), 0);
            }

            outputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            colorTargets?[0].Target.Dispose();
            depthTarget?.Target.Dispose();

            isDisposed = true;
        }
    }
}

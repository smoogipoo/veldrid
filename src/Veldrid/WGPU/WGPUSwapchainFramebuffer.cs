// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainFramebuffer : WGPUFramebufferBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => colorTargets;

        public override FramebufferAttachment? DepthTarget => depthTarget;

        public override OutputDescription OutputDescription => outputDescription;

        private readonly WGPUGraphicsDevice gd;
        private readonly TextureFormat colorFormat;
        private readonly PixelFormat? depthFormat;

        private FramebufferAttachment[] colorTargets;
        private FramebufferAttachment? depthTarget;
        private OutputDescription outputDescription;

        private bool isDisposed;

        public WGPUSwapchainFramebuffer(WGPUGraphicsDevice gd, TextureFormat colorFormat, PixelFormat? depthFormat)
        {
            this.gd = gd;
            this.colorFormat = colorFormat;
            this.depthFormat = depthFormat;
        }

        public void Resize(uint width, uint height)
        {
            colorTargets?[0].Target.Dispose();
            depthTarget?.Target.Dispose();

            Util.EnsureArrayMinimumSize(ref colorTargets, 1);

            TextureDescription colorDescription = TextureDescription.Texture2D(width, height, 1, 1, WGPUFormats.WGPUToVdPixelFormat(colorFormat), TextureUsage.RenderTarget);
            colorTargets![0] = new FramebufferAttachment(new WGPUSwapchainTexture(gd, ref colorDescription), 0);

            if (depthFormat is PixelFormat depth)
            {
                TextureDescription depthDescription = TextureDescription.Texture2D(width, height, 1, 1, depth, TextureUsage.RenderTarget | TextureUsage.DepthStencil);
                depthTarget = new FramebufferAttachment(new WGPUTexture(gd, ref depthDescription), 0);
            }

            outputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        public void ReleaseView()
        {
            if (colorTargets == null)
                return;

            var wgpuColorTarget = Util.AssertSubtype<Texture, WGPUSwapchainTexture>(colorTargets[0].Target);
            wgpuColorTarget.ReleaseView();
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

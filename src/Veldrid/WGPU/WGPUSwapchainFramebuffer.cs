// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.Dawn;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainFramebuffer : WGPUFramebufferBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => colorTargets;

        public override FramebufferAttachment? DepthTarget => depthTarget;

        private readonly WGPUGraphicsDevice gd;
        private readonly TextureFormat colorFormat;
        private readonly PixelFormat? depthFormat;

        private FramebufferAttachment[] colorTargets;
        private FramebufferAttachment? depthTarget;

        private bool isDisposed;

        public WGPUSwapchainFramebuffer(WGPUGraphicsDevice gd, TextureFormat colorFormat, PixelFormat? depthFormat)
        {
            this.gd = gd;
            this.colorFormat = colorFormat;
            this.depthFormat = depthFormat;
        }

        public void SetSwapChain(SwapChain* swapChain, uint width, uint height)
        {
            colorTargets?[0].Target.Dispose();
            depthTarget?.Target.Dispose();

            Util.EnsureArrayMinimumSize(ref colorTargets, 1);

            TextureDescription colorDescription = new TextureDescription
            {
                Width = width,
                Height = height,
                Depth = 1,
                MipLevels = 1,
                ArrayLayers = 1,
                Format = WGPUFormats.WGPUToVdPixelFormat(colorFormat),
                Usage = TextureUsage.RenderTarget,
                Type = TextureType.Texture2D,
                SampleCount = TextureSampleCount.Count1
            };

            colorTargets![0] = new FramebufferAttachment(new WGPUSwapchainTexture(gd, swapChain, ref colorDescription), 0);

            if (depthFormat is PixelFormat depth)
            {
                TextureDescription depthDescription = new TextureDescription
                {
                    Width = width,
                    Height = height,
                    Depth = 1,
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Format = depth,
                    Usage = TextureUsage.DepthStencil,
                    Type = TextureType.Texture2D,
                    SampleCount = TextureSampleCount.Count1
                };

                depthTarget = new FramebufferAttachment(new WGPUSwapchainTexture(gd, swapChain, ref depthDescription), 0);
            }
        }

        public void ReleaseView()
        {
            var wgpuColorTarget = Util.AssertSubtype<Texture, WGPUSwapchainTexture>(colorTargets[0].Target);
            wgpuColorTarget.ReleaseView();

            if (depthTarget is FramebufferAttachment depth)
            {
                var wgpuDepthTarget = Util.AssertSubtype<Texture, WGPUSwapchainTexture>(depth.Target);
                wgpuDepthTarget.ReleaseView();
            }
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

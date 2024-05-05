// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU.Extensions.Dawn;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTextureView : WGPUTextureViewBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;
        private readonly SwapChain* swapChain;

        private Silk.NET.WebGPU.TextureView* view;
        private bool isDisposed;

        public WGPUSwapchainTextureView(WGPUGraphicsDevice gd, SwapChain* swapChain, TextureViewDescription description)
            : base(ref description)
        {
            this.gd = gd;
            this.swapChain = swapChain;
        }

        public override Silk.NET.WebGPU.TextureView* View
        {
            get
            {
                if (view == null)
                    view = gd.Dawn.SwapChainGetCurrentTextureView(swapChain);

                return view;
            }
        }

        public void Release()
        {
            if (view == null)
                return;

            gd.WebGPU.TextureViewRelease(view);
            view = null;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            Release();

            isDisposed = true;
        }
    }
}

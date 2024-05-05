// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTextureView : WGPUTextureViewBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private Silk.NET.WebGPU.Texture* texture;
        private Silk.NET.WebGPU.TextureView* view;

        private bool isDisposed;

        public WGPUSwapchainTextureView(WGPUGraphicsDevice gd, TextureViewDescription description)
            : base(ref description)
        {
            this.gd = gd;
        }

        public override Silk.NET.WebGPU.TextureView* View
        {
            get
            {
                if (view != null)
                    return view;

                SurfaceTexture surfaceTexture = default;
                gd.WebGPU.SurfaceGetCurrentTexture(gd.NativeSurface, ref surfaceTexture);

                if (surfaceTexture.Status != SurfaceGetCurrentTextureStatus.Success)
                {
                    // Todo:
                }

                return view = gd.WebGPU.TextureCreateView(surfaceTexture.Texture, null);
            }
        }

        public void Release()
        {
            if (view != null)
                gd.WebGPU.TextureViewRelease(view);

            if (texture != null)
                gd.WebGPU.TextureRelease(texture);

            view = null;
            texture = null;
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

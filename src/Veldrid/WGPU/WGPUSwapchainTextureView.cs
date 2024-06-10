// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUSwapchainTextureView : WGPUTextureViewBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private WebGPU.WGPUTextureView view;
        private WGPUSurfaceTexture texture;

        private bool isDisposed;

        public WGPUSwapchainTextureView(WGPUGraphicsDevice gd, TextureViewDescription description)
            : base(ref description)
        {
            this.gd = gd;
        }

        public override WebGPU.WGPUTextureView View
        {
            get
            {
                if (view.IsNotNull)
                    return view;

                WGPUSurfaceTexture surfaceTexture;
                wgpuSurfaceGetCurrentTexture(gd.NativeSurface, &surfaceTexture);

                if (surfaceTexture.status != WGPUSurfaceGetCurrentTextureStatus.Success)
                {
                    // Todo:
                }

                texture = surfaceTexture;
                return view = wgpuTextureCreateView(surfaceTexture.texture, null);
            }
        }

        public void Release()
        {
            if (view.IsNotNull)
                wgpuTextureViewRelease(view);

            if (texture.texture.IsNotNull)
                wgpuTextureRelease(texture.texture);

            view = default;
            texture = default;
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

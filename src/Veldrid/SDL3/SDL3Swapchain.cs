// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3Swapchain : Swapchain
    {
        public override string Name { get; set; }

        private readonly SDL3GraphicsDevice gd;
        private readonly SDL3SwapchainFramebuffer framebuffer;
        private readonly bool colorSrgb;
        private bool syncToVBlank;
        private bool allowTearing;
        private bool isDisposed;

        public SDL3Swapchain(SDL3GraphicsDevice gd, ref SwapchainDescription sd)
        {
            this.gd = gd;

            colorSrgb = sd.ColorSrgb;
            setParameters();

            SDL3SwapchainDepthTexture depthTexture = null;
            SDL3SwapchainColorTexture colorTexture = new SDL3SwapchainColorTexture(SDL3Formats.SDLToVdTextureFormat(SDL_GetGPUSwapchainTextureFormat(gd.Device, gd.Window)));

            if (sd.DepthFormat is PixelFormat depthFormat)
            {
                TextureDescription depthDesc = TextureDescription.Texture2D(sd.Width, sd.Height, 1, 1, depthFormat, TextureUsage.DepthStencil, TextureSampleCount.Count1);
                depthTexture = new SDL3SwapchainDepthTexture(gd, ref depthDesc);
            }

            framebuffer = new SDL3SwapchainFramebuffer(gd, depthTexture, colorTexture);
        }

        public override Framebuffer Framebuffer => framebuffer;
        public override bool IsDisposed => isDisposed;

        public override bool SyncToVerticalBlank
        {
            get => syncToVBlank;
            set
            {
                if (syncToVBlank == value)
                    return;

                syncToVBlank = value;
                setParameters();
            }
        }

        public bool AllowTearing
        {
            get => allowTearing;
            set
            {
                if (allowTearing == value)
                    return;

                allowTearing = value;
                setParameters();
            }
        }

        public override void Resize(uint width, uint height)
        {
            framebuffer.Resize(width, height);
        }

        private void setParameters()
        {
            SDL_GPUSwapchainComposition composition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;

            if (colorSrgb && SDL_WindowSupportsGPUSwapchainComposition(gd.Device, gd.Window, SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR))
                composition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR;

            SDL_GPUPresentMode presentMode;

            if (syncToVBlank && SDL_WindowSupportsGPUPresentMode(gd.Device, gd.Window, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC))
                presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;
            else if (allowTearing && SDL_WindowSupportsGPUPresentMode(gd.Device, gd.Window, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE))
                presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;
            else if (SDL_WindowSupportsGPUPresentMode(gd.Device, gd.Window, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX))
                presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX;
            else
                presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;

            SDL_SetGPUSwapchainParameters(gd.Device, gd.Window, composition, presentMode);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

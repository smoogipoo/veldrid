// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Swapchain : Swapchain
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
            framebuffer = new SDL3SwapchainFramebuffer(gd);

            setParameters();
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
            SDL_GPUSwapchainComposition composition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR;
            SDL_GPUPresentMode presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;

            if (colorSrgb && SDL_WindowSupportsGPUSwapchainComposition(gd.Device, gd.Window, SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR))
                composition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;

            if (syncToVBlank)
            {
                if (SDL_WindowSupportsGPUPresentMode(gd.Device, gd.Window, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX))
                    presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX;
            }
            else
            {
                if (SDL_WindowSupportsGPUPresentMode(gd.Device, gd.Window, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE))
                    presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;
            }

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

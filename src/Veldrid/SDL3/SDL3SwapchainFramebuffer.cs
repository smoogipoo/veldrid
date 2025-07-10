// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3SwapchainFramebuffer : SDL3Framebuffer
    {
        public override string Name { get; set; }

        public override uint Width => width;
        public override uint Height => height;

        private readonly SDL3ExternalTexture sdlTexture;
        private readonly SDL3GraphicsDevice gd;
        private uint width;
        private uint height;
        private bool isDisposed;

        public SDL3SwapchainFramebuffer(SDL3GraphicsDevice gd)
            : base(null, [new FramebufferAttachmentDescription(new SDL3ExternalTexture(), 0)])
        {
            this.gd = gd;
            sdlTexture = Util.AssertSubtype<Texture, SDL3ExternalTexture>(ColorTargets[0].Target);
        }

        public void Resize(uint width, uint height)
        {
            this.width = width;
            this.height = height;
        }

        public void AcquireTexture(SDL_GPUCommandBuffer* commandBuffer)
        {
            SDL_GPUTexture* tex = null;

            do
            {
                uint texWidth = width;
                uint texHeight = height;

                if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, gd.Window, &tex, &texWidth, &texHeight))
                    throw new InvalidOperationException("Failed to retrieve a swapchain texture.");

                if (tex != null)
                    break;

                // Swapchain texture can be null while the window is minimized.
                // Todo: Instead of this, we should early exit out of DrawFrame().
                Thread.Sleep(10);
            } while (true);

            TextureDescription td = TextureDescription.Texture2D(
                width,
                height,
                1,
                1,
                SDL3Formats.SDLToVdTextureFormat(SDL_GetGPUSwapchainTextureFormat(gd.Device, gd.Window)),
                TextureUsage.RenderTarget | TextureUsage.DepthStencil,
                TextureSampleCount.Count1);

            sdlTexture.SetNativeTexture(tex, ref td);
        }

        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

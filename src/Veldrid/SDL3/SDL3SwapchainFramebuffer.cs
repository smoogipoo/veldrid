// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3SwapchainFramebuffer : SDL3Framebuffer
    {
        public override string Name { get; set; }

        public override IReadOnlyList<FramebufferAttachment> ColorTargets { get; }

        private readonly SDL3ExternalTexture sdlTexture = new SDL3ExternalTexture();
        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3SwapchainFramebuffer(SDL3GraphicsDevice gd)
            : base(gd)
        {
            this.gd = gd;
            ColorTargets = [new FramebufferAttachment(sdlTexture, 0)];
        }

        public void SetTexture(SDL_GPUTexture* texture, uint width, uint height)
        {
            TextureDescription td = new TextureDescription
            {
                Format = SDL3Formats.SDLToVdTextureFormat(SDL_GetGPUSwapchainTextureFormat(gd.Device, gd.Window)),
                Width = width,
                Height = height
            };

            sdlTexture.SetNativeTexture(texture, ref td);
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

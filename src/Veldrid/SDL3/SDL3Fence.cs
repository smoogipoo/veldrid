// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Fence : Fence
    {
        public override bool Signaled => Fence != null ? SDL_QueryGPUFence(gd.Device, Fence) : signaled;

        public override string Name { get; set; }

        public SDL_GPUFence* Fence { get; private set; }

        private readonly SDL3GraphicsDevice gd;
        private bool signaled;
        private bool isDisposed;

        public SDL3Fence(SDL3GraphicsDevice gd, bool signaled)
        {
            this.gd = gd;
            this.signaled = signaled;
        }

        public override bool IsDisposed => isDisposed;

        public void SetNativeFence(SDL_GPUFence* fence)
        {
            Fence = fence;
        }

        public override void Reset()
        {
            Fence = null;
            signaled = false;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            // Native fence object is disposed by SDL3GraphicsDevice.

            isDisposed = true;
        }
    }
}

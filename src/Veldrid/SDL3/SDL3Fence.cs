// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3Fence : Fence
    {
        public override bool Signaled => signaled;

        public override string Name { get; set; }

        public SDL_GPUFence* Fence { get; set; }

        private bool signaled;
        private bool isDisposed;

        public SDL3Fence(bool signaled)
        {
            this.signaled = signaled;
        }

        public override bool IsDisposed => isDisposed;

        public void Signal()
        {
            signaled = true;
        }

        public override void Reset()
        {
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

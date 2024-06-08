// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUFramebuffer : WGPUFramebufferBase
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private bool isDisposed;

        public WGPUFramebuffer(WGPUGraphicsDevice gd, ref FramebufferDescription description)
            : base(ref description)
        {
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Framebuffer : Framebuffer
    {
        public override string Name { get; set; }

        private bool isDisposed;

        protected SDL3Framebuffer(FramebufferAttachmentDescription? depthTargetDesc, IReadOnlyList<FramebufferAttachmentDescription> colorTargetDescs)
            : base(depthTargetDesc, colorTargetDescs)
        {
        }

        public SDL3Framebuffer(SDL3GraphicsDevice gd, ref FramebufferDescription fd)
            : base(fd.DepthTarget, fd.ColorTargets)
        {
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

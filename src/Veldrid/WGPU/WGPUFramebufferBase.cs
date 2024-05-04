// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal abstract unsafe class WGPUFramebufferBase : Framebuffer
    {
        protected WGPUFramebufferBase(ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
        }
    }
}

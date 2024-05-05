// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal abstract unsafe class WGPUTextureViewBase : TextureView
    {
        public abstract Silk.NET.WebGPU.TextureView* View { get; }

        protected WGPUTextureViewBase(ref TextureViewDescription description)
            : base(ref description)
        {
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceLayout : ResourceLayout
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUResourceLayout(WGPUGraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            this.gd = gd;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.WGPU
{
    internal unsafe class WGPUPipeline : Pipeline
    {
        public override bool IsComputePipeline { get; }
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUPipeline(WGPUGraphicsDevice gd, ref GraphicsPipelineDescription graphicsDescription)
            : base(ref graphicsDescription)
        {
            this.gd = gd;
        }

        public WGPUPipeline(WGPUGraphicsDevice gd, ref ComputePipelineDescription computeDescription)
            : base(ref computeDescription)
        {
            this.gd = gd;

            IsComputePipeline = true;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

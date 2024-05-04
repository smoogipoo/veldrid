// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUBuffer : DeviceBuffer
    {
        public override string Name { get; set; }
        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }
        public override bool IsDisposed => isDisposed;

        public readonly Buffer* Buffer;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUBuffer(WGPUGraphicsDevice gd, ref BufferDescription description)
        {
            this.gd = gd;

            SizeInBytes = description.SizeInBytes;
            Usage = description.Usage;

            Buffer = gd.WebGPU.DeviceCreateBuffer(gd.NativeDevice, new BufferDescriptor
            {
                Usage = WGPUFormats.VdToWGPUBufferUsage(Usage),
                Size = SizeInBytes
            });
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Buffer != null)
                gd.WebGPU.BufferRelease(Buffer);

            isDisposed = true;
        }
    }
}

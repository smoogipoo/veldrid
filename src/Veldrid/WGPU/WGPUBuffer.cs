// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUBuffer : DeviceBuffer
    {
        public override string Name { get; set; }
        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }
        public override bool IsDisposed => isDisposed;

        public readonly WebGPU.WGPUBuffer Buffer;

        private bool isDisposed;

        public WGPUBuffer(WGPUGraphicsDevice gd, ref BufferDescription description)
        {
            SizeInBytes = description.SizeInBytes;
            Usage = description.Usage;

            WGPUBufferDescriptor desc = new WGPUBufferDescriptor
            {
                usage = WGPUFormats.VdToWGPUBufferUsage(Usage),
                size = SizeInBytes
            };

            Buffer = wgpuDeviceCreateBuffer(gd.NativeDevice, &desc);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Buffer.IsNotNull)
                wgpuBufferRelease(Buffer);

            isDisposed = true;
        }
    }
}

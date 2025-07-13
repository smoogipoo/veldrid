// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    internal unsafe class SDL3Buffer : DeviceBuffer
    {
        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }
        public override string Name { get; set; }

        public readonly SDL_GPUBuffer* Buffer;
        public readonly SDL_GPUTransferBuffer* TransferBuffer;

        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3Buffer(SDL3GraphicsDevice gd, ref BufferDescription bd)
        {
            this.gd = gd;

            SizeInBytes = bd.SizeInBytes;
            Usage = bd.Usage;

            // CPU buffer if staging or dynamic
            if ((bd.Usage & (BufferUsage.Staging | BufferUsage.Dynamic)) > 0)
            {
                SDL_GPUTransferBufferCreateInfo tci = new SDL_GPUTransferBufferCreateInfo
                {
                    usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                    size = bd.SizeInBytes
                };

                TransferBuffer = SDL_CreateGPUTransferBuffer(gd.Device, &tci);
            }

            // GPU buffer only if NOT staging, or if dynamic
            if ((bd.Usage & BufferUsage.Staging) == 0 || (bd.Usage & BufferUsage.Dynamic) > 0)
            {
                SDL_GPUBufferCreateInfo ci = new SDL_GPUBufferCreateInfo
                {
                    usage = SDL3Formats.VdToSDLBufferUsage(bd.Usage),
                    size = bd.SizeInBytes
                };

                Buffer = SDL_CreateGPUBuffer(gd.Device, &ci);
            }
        }

        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Buffer != null)
                SDL_ReleaseGPUBuffer(gd.Device, Buffer);

            if (TransferBuffer != null)
                SDL_ReleaseGPUTransferBuffer(gd.Device, TransferBuffer);

            isDisposed = true;
        }
    }
}

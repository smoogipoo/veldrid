// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.SDL3
{
    internal class SDL3ResourceLayout : ResourceLayout
    {
        public override string Name { get; set; }

        public readonly ResourceLayoutElementDescription[] Elements;
        public readonly uint UniformBufferCount;
        public readonly uint SamplerCount;
        public readonly uint ReadOnlyStorageBufferCount;
        public readonly uint ReadWriteStorageBufferCount;
        public readonly uint ReadOnlyTextureCount;
        public readonly uint ReadWriteTextureCount;
        private bool isDisposed;

        public SDL3ResourceLayout(ref ResourceLayoutDescription description)
            : base(ref description)
        {
            Elements = description.Elements;

            for (int i = 0; i < description.Elements.Length; i++)
            {
                switch (description.Elements[i].Kind)
                {
                    case ResourceKind.UniformBuffer:
                        UniformBufferCount++;
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        ReadOnlyStorageBufferCount++;
                        break;

                    case ResourceKind.StructuredBufferReadWrite:
                        ReadWriteStorageBufferCount++;
                        break;

                    case ResourceKind.TextureReadOnly:
                        ReadOnlyTextureCount++;
                        break;

                    case ResourceKind.TextureReadWrite:
                        ReadWriteTextureCount++;
                        break;

                    case ResourceKind.Sampler:
                        SamplerCount++;
                        break;
                }
            }
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

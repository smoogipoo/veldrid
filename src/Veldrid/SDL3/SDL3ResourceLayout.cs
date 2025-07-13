// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.SDL3
{
    public class SDL3ResourceLayout : ResourceLayout
    {
        public override string Name { get; set; }

        public readonly ResourceLayoutElementDescription[] Elements;
        public readonly uint[] BindingSlotByVdIndex;
        public readonly uint UniformBufferCount;
        public readonly uint SamplerCount;
        public readonly uint ReadOnlyStorageBufferCount;
        public readonly uint ReadWriteStorageBufferCount;
        public readonly uint ReadOnlyTextureCount;
        public readonly uint ReadWriteTextureCount;
        private bool isDisposed;

        public SDL3ResourceLayout(SDL3GraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            Elements = description.Elements;
            BindingSlotByVdIndex = new uint[Elements.Length];

            uint bufferIndex = 0;
            uint textureIndex = 0;
            uint samplerIndex = 0;

            for (int i = 0; i < description.Elements.Length; i++)
            {
                uint slot = 0;

                switch (description.Elements[i].Kind)
                {
                    case ResourceKind.UniformBuffer:
                        UniformBufferCount++;
                        slot = bufferIndex++;
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        ReadOnlyStorageBufferCount++;
                        slot = bufferIndex++;
                        break;

                    case ResourceKind.StructuredBufferReadWrite:
                        ReadWriteStorageBufferCount++;
                        slot = bufferIndex++;
                        break;

                    case ResourceKind.TextureReadOnly:
                        ReadOnlyTextureCount++;
                        slot = textureIndex++;
                        break;

                    case ResourceKind.TextureReadWrite:
                        ReadWriteTextureCount++;
                        slot = textureIndex++;
                        break;

                    case ResourceKind.Sampler:
                        SamplerCount++;
                        slot = samplerIndex++;
                        break;
                }

                BindingSlotByVdIndex[i] = slot;
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

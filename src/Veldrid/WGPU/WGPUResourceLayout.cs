// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceLayout : ResourceLayout
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly BindGroupLayout* Layout;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUResourceLayout(WGPUGraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            this.gd = gd;

            BindGroupLayoutEntry* entries = stackalloc BindGroupLayoutEntry[description.Elements.Length];

            for (int i = 0; i < description.Elements.Length; i++)
            {
                var element = description.Elements[i];

                entries[i] = new BindGroupLayoutEntry
                {
                    Binding = (uint)i,
                    Visibility = WGPUFormats.VdToWGPUShaderStage(element.Stages)
                };

                switch (element.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        entries[i].Buffer = new BufferBindingLayout
                        {
                            Type = BufferBindingType.Uniform,
                            HasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        entries[i].Buffer = new BufferBindingLayout
                        {
                            Type = BufferBindingType.ReadOnlyStorage,
                            HasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.StructuredBufferReadWrite:
                        entries[i].Buffer = new BufferBindingLayout
                        {
                            Type = BufferBindingType.Storage,
                            HasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.TextureReadOnly:
                        entries[i].Texture = new TextureBindingLayout
                        {
                            SampleType = TextureSampleType.Float,
                            ViewDimension = TextureViewDimension.Dimension2D,
                            Multisampled = true
                        };
                        break;

                    case ResourceKind.TextureReadWrite:
                        entries[i].StorageTexture = new StorageTextureBindingLayout
                        {
                            Access = StorageTextureAccess.WriteOnly,
                            Format = TextureFormat.Rgba32float,
                            ViewDimension = TextureViewDimension.Dimension2D
                        };
                        break;

                    case ResourceKind.Sampler:
                        entries[i].Sampler = new SamplerBindingLayout
                        {
                            Type = SamplerBindingType.Filtering
                        };
                        break;

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }

            Layout = gd.WebGPU.DeviceCreateBindGroupLayout(gd.NativeDevice, new BindGroupLayoutDescriptor
            {
                EntryCount = (uint)description.Elements.Length,
                Entries = entries
            });
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Layout != null)
                gd.WebGPU.BindGroupLayoutRelease(Layout);

            isDisposed = true;
        }
    }
}

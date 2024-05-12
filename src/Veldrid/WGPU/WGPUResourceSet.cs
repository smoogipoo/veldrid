// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceSet : ResourceSet
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly BindGroup* BindGroup;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUResourceSet(WGPUGraphicsDevice gd, ref ResourceSetDescription description)
            : base(ref description)
        {
            this.gd = gd;

            var wgpuResourceLayout = Util.AssertSubtype<ResourceLayout, WGPUResourceLayout>(description.Layout);

            BindGroupEntry* entries = stackalloc BindGroupEntry[description.BoundResources.Length];

            for (int i = 0; i < description.BoundResources.Length; i++)
            {
                var resource = description.BoundResources[i];
                var layout = description.Layout.Description.Elements[i];

                if (layout.Kind == ResourceKind.UniformBuffer || layout.Kind == ResourceKind.StructuredBufferReadOnly || layout.Kind == ResourceKind.StructuredBufferReadWrite)
                {
                    var range = Util.GetBufferRange(resource, 0);
                    var wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(range.Buffer);

                    entries[i] = new BindGroupEntry
                    {
                        Binding = (uint)i,
                        Buffer = wgpuBuffer.Buffer,
                        Offset = range.Offset,
                        Size = range.SizeInBytes
                    };
                }
                else if (layout.Kind == ResourceKind.TextureReadOnly || layout.Kind == ResourceKind.TextureReadWrite)
                {
                    var textureView = Util.GetTextureView(this.gd, resource);
                    var wgpuTextureView = Util.AssertSubtype<TextureView, WGPUTextureView>(textureView);

                    entries[i] = new BindGroupEntry
                    {
                        Binding = (uint)i,
                        TextureView = wgpuTextureView.View
                    };
                }
                else if (layout.Kind == ResourceKind.Sampler)
                {
                    var wgpuSampler = Util.AssertSubtype<IBindableResource, WGPUSampler>(resource);

                    entries[i] = new BindGroupEntry
                    {
                        Sampler = wgpuSampler.Sampler,
                    };
                }
            }

            BindGroup = gd.WebGPU.DeviceCreateBindGroup(gd.NativeDevice, new BindGroupDescriptor
            {
                EntryCount = (UIntPtr)description.BoundResources.Length,
                Entries = entries,
                Layout = wgpuResourceLayout.Layout
            });
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (BindGroup != null)
                gd.WebGPU.BindGroupRelease(BindGroup);

            isDisposed = true;
        }
    }
}

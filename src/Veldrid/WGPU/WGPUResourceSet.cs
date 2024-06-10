// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceSet : ResourceSet
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly WGPUBindGroup BindGroup;

        private bool isDisposed;

        public WGPUResourceSet(WGPUGraphicsDevice gd, ref ResourceSetDescription description)
            : base(ref description)
        {
            var wgpuResourceLayout = Util.AssertSubtype<ResourceLayout, WGPUResourceLayout>(description.Layout);

            WGPUBindGroupEntry* entries = stackalloc WGPUBindGroupEntry[description.BoundResources.Length];

            for (int i = 0; i < description.BoundResources.Length; i++)
            {
                var resource = description.BoundResources[i];
                var layout = description.Layout.Description.Elements[i];

                entries[i] = new WGPUBindGroupEntry
                {
                    binding = (uint)i,
                };

                if (layout.Kind == ResourceKind.UniformBuffer || layout.Kind == ResourceKind.StructuredBufferReadOnly || layout.Kind == ResourceKind.StructuredBufferReadWrite)
                {
                    var range = Util.GetBufferRange(resource, 0);
                    var wgpuBuffer = Util.AssertSubtype<DeviceBuffer, WGPUBuffer>(range.Buffer);

                    entries[i].buffer = wgpuBuffer.Buffer;
                    entries[i].offset = range.Offset;
                    entries[i].size = range.SizeInBytes;
                }
                else if (layout.Kind == ResourceKind.TextureReadOnly || layout.Kind == ResourceKind.TextureReadWrite || layout.Kind == ResourceKind.TextureWriteOnly)
                {
                    var textureView = Util.GetTextureView(gd, resource);
                    var wgpuTextureView = Util.AssertSubtype<TextureView, WGPUTextureView>(textureView);

                    entries[i].textureView = wgpuTextureView.View;
                }
                else if (layout.Kind == ResourceKind.Sampler)
                {
                    var wgpuSampler = Util.AssertSubtype<IBindableResource, WGPUSampler>(resource);

                    entries[i].sampler = wgpuSampler.Sampler;
                }
                else
                    throw Illegal.Value<IBindableResource>();
            }

            WGPUBindGroupDescriptor desc = new WGPUBindGroupDescriptor
            {
                entryCount = (UIntPtr)description.BoundResources.Length,
                entries = entries,
                layout = wgpuResourceLayout.Layout
            };

            BindGroup = wgpuDeviceCreateBindGroup(gd.NativeDevice, &desc);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (BindGroup.IsNotNull)
                wgpuBindGroupRelease(BindGroup);

            isDisposed = true;
        }
    }
}

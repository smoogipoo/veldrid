// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUResourceLayout : ResourceLayout
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly WGPUBindGroupLayout Layout;

        private bool isDisposed;

        public WGPUResourceLayout(WGPUGraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            WGPUBindGroupLayoutEntry* entries = stackalloc WGPUBindGroupLayoutEntry[description.Elements.Length];

            for (int i = 0; i < description.Elements.Length; i++)
            {
                var element = description.Elements[i];

                entries[i] = new WGPUBindGroupLayoutEntry
                {
                    binding = (uint)i,
                    visibility = WGPUFormats.VdToWGPUShaderStage(element.Stages)
                };

                switch (element.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        entries[i].buffer = new WGPUBufferBindingLayout
                        {
                            type = WGPUBufferBindingType.Uniform,
                            hasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        entries[i].buffer = new WGPUBufferBindingLayout
                        {
                            type = WGPUBufferBindingType.ReadOnlyStorage,
                            hasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.StructuredBufferReadWrite:
                        entries[i].buffer = new WGPUBufferBindingLayout
                        {
                            type = WGPUBufferBindingType.Storage,
                            hasDynamicOffset = (element.Options & ResourceLayoutElementOptions.DynamicBinding) == ResourceLayoutElementOptions.DynamicBinding
                        };
                        break;

                    case ResourceKind.TextureReadOnly:
                        entries[i].texture = new WGPUTextureBindingLayout
                        {
                            sampleType = WGPUTextureSampleType.Float,
                            viewDimension = WGPUTextureViewDimension._2D
                        };
                        break;

                    case ResourceKind.TextureReadWrite:
                        entries[i].storageTexture = new WGPUStorageTextureBindingLayout
                        {
                            access = WGPUStorageTextureAccess.WriteOnly,
                            format = WGPUTextureFormat.RGBA32Float,
                            viewDimension = WGPUTextureViewDimension._2D
                        };
                        break;

                    case ResourceKind.Sampler:
                        entries[i].sampler = new WGPUSamplerBindingLayout
                        {
                            type = WGPUSamplerBindingType.Filtering
                        };
                        break;

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }

            WGPUBindGroupLayoutDescriptor desc = new WGPUBindGroupLayoutDescriptor
            {
                entryCount = (uint)description.Elements.Length,
                entries = entries
            };

            Layout = wgpuDeviceCreateBindGroupLayout(gd.NativeDevice, &desc);
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Layout.IsNotNull)
                wgpuBindGroupLayoutRelease(Layout);

            isDisposed = true;
        }
    }
}

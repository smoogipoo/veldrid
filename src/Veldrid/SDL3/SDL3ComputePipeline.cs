// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3ComputePipeline : Pipeline
    {
        public override string Name { get; set; }

        public readonly SDL_GPUComputePipeline* Pipeline;
        public readonly uint ResourceLayoutCount;

        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3ComputePipeline(SDL3GraphicsDevice gd, ref ComputePipelineDescription pd)
            : base(ref pd)
        {
            this.gd = gd;

            SDL3Shader sdlShader = Util.AssertSubtype<Shader, SDL3Shader>(pd.ComputeShader);

            fixed (byte* codePtr = sdlShader.ShaderBytes)
            {
                fixed (char* entryPoint = sdlShader.EntryPoint)
                {
                    SDL_GPUComputePipelineCreateInfo pci = new SDL_GPUComputePipelineCreateInfo
                    {
                        code_size = (nuint)sdlShader.ShaderBytes.Length,
                        code = codePtr,
                        entrypoint = (byte*)entryPoint,
                        format = SDL3Formats.VdToSDLShaderFormat(gd.BackendType),
                        threadcount_x = pd.ThreadGroupSizeX,
                        threadcount_y = pd.ThreadGroupSizeY,
                        threadcount_z = pd.ThreadGroupSizeZ,
                    };

                    foreach (var layout in pd.ResourceLayouts)
                    {
                        SDL3ResourceLayout sdlLayout = Util.AssertSubtype<ResourceLayout, SDL3ResourceLayout>(layout);

                        pci.num_samplers += sdlLayout.SamplerCount;
                        pci.num_readonly_storage_buffers += sdlLayout.ReadOnlyStorageBufferCount;
                        pci.num_readwrite_storage_buffers += sdlLayout.ReadWriteStorageBufferCount;
                        pci.num_readonly_storage_textures += sdlLayout.ReadOnlyTextureCount;
                        pci.num_readwrite_storage_textures += sdlLayout.ReadWriteTextureCount;
                        pci.num_uniform_buffers += sdlLayout.UniformBufferCount;
                    }

                    Pipeline = SDL_CreateGPUComputePipeline(gd.Device, &pci);
                    ResourceLayoutCount = (uint)pd.ResourceLayouts.Length;
                }
            }
        }

        public override bool IsComputePipeline => true;
        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Pipeline != null)
                SDL_ReleaseGPUComputePipeline(gd.Device, Pipeline);

            isDisposed = true;
        }
    }
}

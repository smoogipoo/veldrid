// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUShader : Shader
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly WGPUShaderModule Module;

        private bool isDisposed;

        public WGPUShader(WGPUGraphicsDevice gd, ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            fixed (byte* codePtr = description.ShaderBytes)
            {
                WGPUShaderModuleDescriptor desc = new WGPUShaderModuleDescriptor
                {
                    nextInChain = WGPUUtil.Chain(new WGPUShaderModuleSPIRVDescriptor
                    {
                        chain = { sType = WGPUSType.ShaderModuleSPIRVDescriptor },
                        code = (uint*)codePtr,
                        codeSize = (uint)description.ShaderBytes.Length / sizeof(uint),
                    }),
                };

                Module = wgpuDeviceCreateShaderModule(gd.NativeDevice, &desc);
            }
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Module.IsNotNull)
                wgpuShaderModuleRelease(Module);

            isDisposed = true;
        }
    }
}

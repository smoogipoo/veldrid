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
            // SpirV magic bytes
            if (description.ShaderBytes.Length > 4
                && description.ShaderBytes[0] == 0x03
                && description.ShaderBytes[1] == 0x02
                && description.ShaderBytes[2] == 0x23
                && description.ShaderBytes[3] == 0x07)
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
            else
            {
                fixed (byte* codePtr = description.ShaderBytes)
                {
                    WGPUShaderModuleDescriptor desc = new WGPUShaderModuleDescriptor
                    {
                        nextInChain = WGPUUtil.Chain(new WGPUShaderModuleWGSLDescriptor
                        {
                            chain = { sType = WGPUSType.ShaderModuleWGSLDescriptor },
                            code = (sbyte*)codePtr
                        }),
                    };

                    Module = wgpuDeviceCreateShaderModule(gd.NativeDevice, &desc);
                }
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

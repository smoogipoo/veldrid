// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUShader : Shader
    {
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly ShaderModule* Module;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUShader(WGPUGraphicsDevice gd, ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            this.gd = gd;

            fixed (byte* codePtr = description.ShaderBytes)
            {
                Module = gd.WebGPU.DeviceCreateShaderModule(gd.NativeDevice, new ShaderModuleDescriptor
                {
                    NextInChain = WGPUUtil.Chain(new ShaderModuleSPIRVDescriptor
                    {
                        Chain = { SType = SType.ShaderModuleSpirvDescriptor },
                        Code = (uint*)codePtr,
                        CodeSize = (uint)description.ShaderBytes.Length / sizeof(uint),
                    }),
                });
            }
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Module != null)
                gd.WebGPU.ShaderModuleRelease(Module);

            isDisposed = true;
        }
    }
}

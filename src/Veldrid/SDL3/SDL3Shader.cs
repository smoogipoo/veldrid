// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3Shader : Shader
    {
        public override string Name { get; set; }

        public byte[] ShaderBytes { get; }

        public readonly SDL_GPUShader* Shader;

        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3Shader(SDL3GraphicsDevice gd, ref ShaderDescription sd)
            : base(sd.Stage, sd.EntryPoint)
        {
            this.gd = gd;

            ShaderBytes = sd.ShaderBytes;

            fixed (byte* codePtr = ShaderBytes)
            {
                fixed (char* entryPoint = sd.EntryPoint)
                {
                    SDL_GPUShaderCreateInfo ci = new SDL_GPUShaderCreateInfo
                    {
                        code_size = (nuint)ShaderBytes.Length,
                        code = codePtr,
                        entrypoint = (byte*)entryPoint,
                        stage = SDL3Formats.VdToSDLShaderStage(sd.Stage),
                        format = SDL3Formats.VdToSDLShaderFormat(gd.BackendType)
                    };

                    Shader = SDL_CreateGPUShader(gd.Device, &ci);
                }
            }
        }

        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Shader != null)
                SDL_ReleaseGPUShader(gd.Device, Shader);

            isDisposed = true;
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11Shader : Shader
    {
        public ComPtr<ID3D11DeviceChild> DeviceShader { get; }

        public override bool IsDisposed => DeviceShader.NativePointer == IntPtr.Zero;
        public byte[] Bytecode { get; internal set; }

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // DeviceShader.DebugName = value;
            }
        }

        private readonly ID3D11Device device;
        private string name;

        public D3D11Shader(ID3D11Device device, ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            this.device = device;

            if (description.ShaderBytes.Length > 4
                && description.ShaderBytes[0] == 0x44
                && description.ShaderBytes[1] == 0x58
                && description.ShaderBytes[2] == 0x42
                && description.ShaderBytes[3] == 0x43)
                Bytecode = Util.ShallowClone(description.ShaderBytes);
            else
                Bytecode = compileCode(description);

            switch (description.Stage)
            {
                case ShaderStages.Vertex:
                    ComPtr<ID3D11VertexShader> vsShader = default;
                    SilkMarshal.ThrowHResult(device.CreateVertexShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref vsShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11VertexShader, ID3D11DeviceChild>(vsShader);
                    break;

                case ShaderStages.Geometry:
                    ComPtr<ID3D11GeometryShader> gsShader = default;
                    SilkMarshal.ThrowHResult(device.CreateGeometryShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref gsShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11GeometryShader, ID3D11DeviceChild>(gsShader);
                    break;

                case ShaderStages.TessellationControl:
                    ComPtr<ID3D11HullShader> tcShader = default;
                    SilkMarshal.ThrowHResult(device.CreateHullShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref tcShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11HullShader, ID3D11DeviceChild>(tcShader);
                    break;

                case ShaderStages.TessellationEvaluation:
                    ComPtr<ID3D11DomainShader> teShader = default;
                    SilkMarshal.ThrowHResult(device.CreateDomainShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref teShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11DomainShader, ID3D11DeviceChild>(teShader);
                    break;

                case ShaderStages.Fragment:
                    ComPtr<ID3D11PixelShader> psShader = default;
                    SilkMarshal.ThrowHResult(device.CreatePixelShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref psShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11PixelShader, ID3D11DeviceChild>(psShader);
                    break;

                case ShaderStages.Compute:
                    ComPtr<ID3D11ComputeShader> csShader = default;
                    SilkMarshal.ThrowHResult(device.CreateComputeShader(in Bytecode[0], (UIntPtr)Bytecode.Length, ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref csShader));
                    DeviceShader = D3D11Util.ComCast<ID3D11ComputeShader, ID3D11DeviceChild>(csShader);
                    break;

                default:
                    throw Illegal.Value<ShaderStages>();
            }
        }

        #region Disposal

        public override void Dispose()
        {
            DeviceShader.Dispose();
        }

        #endregion

        private byte[] compileCode(ShaderDescription description)
        {
            string profile;

            switch (description.Stage)
            {
                case ShaderStages.Vertex:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "vs_5_0" : "vs_4_0";
                    break;

                case ShaderStages.Geometry:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "gs_5_0" : "gs_4_0";
                    break;

                case ShaderStages.TessellationControl:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "hs_5_0" : "hs_4_0";
                    break;

                case ShaderStages.TessellationEvaluation:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "ds_5_0" : "ds_4_0";
                    break;

                case ShaderStages.Fragment:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "ps_5_0" : "ps_4_0";
                    break;

                case ShaderStages.Compute:
                    profile = device.GetFeatureLevel() >= FeatureLevel.Level_11_0 ? "cs_5_0" : "cs_4_0";
                    break;

                default:
                    throw Illegal.Value<ShaderStages>();
            }

            Silk.NET.Direct3D.Compilers.D3DCompiler compiler = D3DCompiler.GetApi();
            compiler.Compile(description.ShaderBytes,);

            var flags = description.Debug ? ShaderFlags.Debug : ShaderFlags.OptimizationLevel3;

            Compiler.Compile(description.ShaderBytes, null!, null!,
                description.EntryPoint, null!,
                profile, flags, out var result, out var error);

            if (result == null) throw new VeldridException($"Failed to compile HLSL code: {Encoding.ASCII.GetString(error.AsBytes())}");

            return result.AsBytes();
        }
    }
}

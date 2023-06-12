using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Veldrid.D3D12
{
    internal class D3D12Pipeline : Pipeline
    {
        private string _name;
        private bool _disposed;

        public BlendDescription BlendState { get; }
        public Color4 BlendFactor { get; }
        public DepthStencilDescription DepthStencilState { get; }
        public uint StencilReference { get; }
        public RasterizerDescription RasterizerState { get; }
        public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        public InputLayoutDescription InputLayout { get; }
        public byte[] VertexShader { get; }
        public byte[] GeometryShader { get; } // May be null.
        public byte[] HullShader { get; } // May be null.
        public byte[] DomainShader { get; } // May be null.
        public byte[] PixelShader { get; }
        public byte[] ComputeShader { get; }
        public new D3D12ResourceLayout[] ResourceLayouts { get; }
        public int[] VertexStrides { get; }

        public override bool IsComputePipeline { get; }

        public D3D12Pipeline(D3D12ResourceCache cache, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            Shader[] stages = description.ShaderSet.Shaders;

            for (int i = 0; i < description.ShaderSet.Shaders.Length; i++)
            {
                if (stages[i].Stage == ShaderStages.Vertex)
                {
                    D3D12Shader d3d12VertexShader = ((D3D12Shader)stages[i]);
                    VertexShader = d3d12VertexShader.Bytecode;
                }

                if (stages[i].Stage == ShaderStages.Geometry)
                {
                    GeometryShader = ((D3D12Shader)stages[i]).Bytecode;
                }

                if (stages[i].Stage == ShaderStages.TessellationControl)
                {
                    HullShader = ((D3D12Shader)stages[i]).Bytecode;
                }

                if (stages[i].Stage == ShaderStages.TessellationEvaluation)
                {
                    DomainShader = ((D3D12Shader)stages[i]).Bytecode;
                }

                if (stages[i].Stage == ShaderStages.Fragment)
                {
                    PixelShader = ((D3D12Shader)stages[i]).Bytecode;
                }

                if (stages[i].Stage == ShaderStages.Compute)
                {
                    ComputeShader = ((D3D12Shader)stages[i]).Bytecode;
                }
            }

            cache.GetPipelineResources(
                ref description.BlendState,
                ref description.DepthStencilState,
                ref description.RasterizerState,
                description.Outputs.SampleCount != TextureSampleCount.Count1,
                description.ShaderSet.VertexLayouts,
                VertexShader,
                out BlendDescription blendState,
                out DepthStencilDescription depthStencilState,
                out RasterizerDescription rasterizerState,
                out InputLayoutDescription inputLayout);

            BlendState = blendState;
            BlendFactor = new Color4(description.BlendState.BlendFactor.ToVector4());
            DepthStencilState = depthStencilState;
            StencilReference = description.DepthStencilState.StencilReference;
            RasterizerState = rasterizerState;
            PrimitiveTopology = D3D12Formats.VdToD3D11PrimitiveTopology(description.PrimitiveTopology);

            ResourceLayout[] genericLayouts = description.ResourceLayouts;
            ResourceLayouts = new D3D12ResourceLayout[genericLayouts.Length];

            for (int i = 0; i < ResourceLayouts.Length; i++)
            {
                ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(genericLayouts[i]);
            }

            Debug.Assert(VertexShader != null || ComputeShader != null);

            if (VertexShader != null && description.ShaderSet.VertexLayouts.Length > 0)
            {
                InputLayout = inputLayout;
                int numVertexBuffers = description.ShaderSet.VertexLayouts.Length;
                VertexStrides = new int[numVertexBuffers];

                for (int i = 0; i < numVertexBuffers; i++)
                {
                    VertexStrides[i] = (int)description.ShaderSet.VertexLayouts[i].Stride;
                }
            }
            else
            {
                VertexStrides = Array.Empty<int>();
            }
        }

        public D3D12Pipeline(D3D12ResourceCache cache, ref ComputePipelineDescription description)
            : base(ref description)
        {
            IsComputePipeline = true;
            ComputeShader = ((D3D12Shader)description.ComputeShader).Bytecode;
            ResourceLayout[] genericLayouts = description.ResourceLayouts;
            ResourceLayouts = new D3D12ResourceLayout[genericLayouts.Length];

            for (int i = 0; i < ResourceLayouts.Length; i++)
            {
                ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(genericLayouts[i]);
            }
        }

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            _disposed = true;
        }
    }
}

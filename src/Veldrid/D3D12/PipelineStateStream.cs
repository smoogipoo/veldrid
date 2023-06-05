// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PipelineStateStream
    {
        public PipelineStateSubObjectTypeRootSignature RootSignature;
        public PipelineStateSubObjectTypeVertexShader VertexShader;
        public PipelineStateSubObjectTypePixelShader PixelShader;
        public PipelineStateSubObjectTypeInputLayout InputLayout;
        public PipelineStateSubObjectTypeSampleMask SampleMask;
        public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
        public PipelineStateSubObjectTypeRasterizer RasterizerState;
        public PipelineStateSubObjectTypeBlend BlendState;
        public PipelineStateSubObjectTypeDepthStencil DepthStencilState;
        public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
        public PipelineStateSubObjectTypeDepthStencilFormat DepthStencilFormat;
        public PipelineStateSubObjectTypeSampleDescription SampleDescription;
    }
}

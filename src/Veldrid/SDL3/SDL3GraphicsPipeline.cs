// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using static SDL.SDL3;

namespace Veldrid.SDL3
{
    public unsafe class SDL3GraphicsPipeline : Pipeline
    {
        public override string Name { get; set; }

        public readonly SDL_GPUGraphicsPipeline* Pipeline;

        private readonly SDL3GraphicsDevice gd;
        private bool isDisposed;

        public SDL3GraphicsPipeline(SDL3GraphicsDevice gd, ref GraphicsPipelineDescription pd)
            : base(ref pd)
        {
            this.gd = gd;

            uint bindingCount = (uint)pd.ShaderSet.VertexLayouts.Length;
            uint attributeCount = 0;
            for (int i = 0; i < pd.ShaderSet.VertexLayouts.Length; i++)
                attributeCount += (uint)pd.ShaderSet.VertexLayouts[i].Elements.Length;

            SDL_GPUVertexBufferDescription* vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[(int)bindingCount];
            SDL_GPUVertexAttribute* vertexAttributes = stackalloc SDL_GPUVertexAttribute[(int)attributeCount];

            int targetIndex = 0;
            int targetLocation = 0;

            for (int i = 0; i < pd.ShaderSet.VertexLayouts.Length; i++)
            {
                VertexLayoutDescription vertexLayout = pd.ShaderSet.VertexLayouts[i];

                vertexBufferDescriptions[i] = new SDL_GPUVertexBufferDescription
                {
                    slot = (uint)i,
                    pitch = vertexLayout.Stride,
                    input_rate = vertexLayout.InstanceStepRate != 0 ? SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_INSTANCE : SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    instance_step_rate = vertexLayout.InstanceStepRate,
                };

                uint currentOffset = 0;

                for (int location = 0; location < vertexLayout.Elements.Length; location++)
                {
                    var inputElement = vertexLayout.Elements[location];

                    vertexAttributes[targetIndex] = new SDL_GPUVertexAttribute
                    {
                        format = SDL3Formats.VdToSDLVertexElementFormat(inputElement.Format),
                        buffer_slot = (uint)i,
                        location = (uint)(targetLocation + location),
                        offset = inputElement.Offset != 0 ? inputElement.Offset : currentOffset
                    };

                    targetIndex += 1;
                    currentOffset += FormatSizeHelpers.GetSizeInBytes(inputElement.Format);
                }

                targetLocation += vertexLayout.Elements.Length;
            }

            SDL_GPUColorTargetDescription* colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[pd.Outputs.ColorAttachments.Length];

            for (int i = 0; i < pd.Outputs.ColorAttachments.Length; i++)
            {
                BlendAttachmentDescription blendDesc = pd.BlendState.AttachmentStates[i];
                OutputAttachmentDescription outputDesc = pd.Outputs.ColorAttachments[i];

                colorTargetDescriptions[i] = new SDL_GPUColorTargetDescription
                {
                    format = SDL3Formats.VdToSDLTextureFormat(outputDesc.Format),
                    blend_state = new SDL_GPUColorTargetBlendState
                    {
                        src_color_blendfactor = SDL3Formats.VdToSDLBlendFactor(blendDesc.SourceColorFactor),
                        dst_color_blendfactor = SDL3Formats.VdToSDLBlendFactor(blendDesc.DestinationColorFactor),
                        color_blend_op = SDL3Formats.VdToSDLBlendOp(blendDesc.ColorFunction),
                        src_alpha_blendfactor = SDL3Formats.VdToSDLBlendFactor(blendDesc.SourceAlphaFactor),
                        dst_alpha_blendfactor = SDL3Formats.VdToSDLBlendFactor(blendDesc.DestinationAlphaFactor),
                        alpha_blend_op = SDL3Formats.VdToSDLBlendOp(blendDesc.AlphaFunction),
                        color_write_mask = SDL3Formats.VdToSDLColorComponentFlags(blendDesc.ColorWriteMask),
                        enable_blend = blendDesc.BlendEnabled,
                        enable_color_write_mask = blendDesc.ColorWriteMask != null
                    }
                };
            }

            SDL_GPUGraphicsPipelineCreateInfo pci = new SDL_GPUGraphicsPipelineCreateInfo
            {
                vertex_input_state = new SDL_GPUVertexInputState
                {
                    vertex_buffer_descriptions = vertexBufferDescriptions,
                    num_vertex_buffers = bindingCount,
                    vertex_attributes = vertexAttributes,
                    num_vertex_attributes = attributeCount
                },
                primitive_type = SDL3Formats.VdToSDLPrimitiveType(pd.PrimitiveTopology),
                rasterizer_state = new SDL_GPURasterizerState
                {
                    fill_mode = SDL3Formats.VdToSDLFillMode(pd.RasterizerState.FillMode),
                    cull_mode = SDL3Formats.VdToSDLCullMode(pd.RasterizerState.CullMode),
                    front_face = SDL3Formats.VdToSDLFrontFace(pd.RasterizerState.FrontFace),
                    enable_depth_clip = pd.RasterizerState.DepthClipEnabled
                },
                multisample_state = new SDL_GPUMultisampleState
                {
                    sample_count = SDL3Formats.VdToSDLSampleCount(pd.Outputs.SampleCount),
                    enable_alpha_to_coverage = pd.BlendState.AlphaToCoverageEnabled,
                },
                depth_stencil_state = new SDL_GPUDepthStencilState
                {
                    compare_op = SDL3Formats.VdToSDLCompareOp(pd.DepthStencilState.DepthComparison),
                    back_stencil_state = new SDL_GPUStencilOpState
                    {
                        fail_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilBack.Fail),
                        pass_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilBack.Pass),
                        depth_fail_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilBack.DepthFail),
                        compare_op = SDL3Formats.VdToSDLCompareOp(pd.DepthStencilState.StencilBack.Comparison)
                    },
                    front_stencil_state = new SDL_GPUStencilOpState
                    {
                        fail_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilFront.Fail),
                        pass_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilFront.Pass),
                        depth_fail_op = SDL3Formats.VdToSDLStencilOp(pd.DepthStencilState.StencilFront.DepthFail),
                        compare_op = SDL3Formats.VdToSDLCompareOp(pd.DepthStencilState.StencilFront.Comparison)
                    },
                    compare_mask = pd.DepthStencilState.StencilReadMask,
                    write_mask = pd.DepthStencilState.StencilWriteMask,
                    enable_depth_test = pd.DepthStencilState.DepthTestEnabled,
                    enable_depth_write = pd.DepthStencilState.DepthWriteEnabled,
                    enable_stencil_test = pd.DepthStencilState.StencilTestEnabled,
                },
                target_info = new SDL_GPUGraphicsPipelineTargetInfo
                {
                    color_target_descriptions = colorTargetDescriptions,
                    num_color_targets = (uint)pd.Outputs.ColorAttachments.Length,
                    depth_stencil_format = pd.Outputs.DepthAttachment != null
                        ? SDL3Formats.VdToSDLTextureFormat(pd.Outputs.DepthAttachment.Value.Format)
                        : SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID,
                    has_depth_stencil_target = pd.Outputs.DepthAttachment != null
                },
            };

            foreach (var shader in pd.ShaderSet.Shaders)
            {
                SDL3Shader sdlShader = Util.AssertSubtype<Shader, SDL3Shader>(shader);

                if (sdlShader.Stage == ShaderStages.Vertex)
                    pci.vertex_shader = sdlShader.Shader;

                if (sdlShader.Stage == ShaderStages.Fragment)
                    pci.fragment_shader = sdlShader.Shader;
            }

            Pipeline = SDL_CreateGPUGraphicsPipeline(gd.Device, &pci);
        }

        public override bool IsComputePipeline => false;
        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Pipeline != null)
                SDL_ReleaseGPUGraphicsPipeline(gd.Device, Pipeline);

            isDisposed = true;
        }
    }
}

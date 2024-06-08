// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUPipeline : Pipeline
    {
        public override bool IsComputePipeline { get; }
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly WGPUPipelineLayout Layout;
        public readonly WGPURenderPipeline RenderPipeline;

        private bool isDisposed;

        public WGPUPipeline(WGPUGraphicsDevice gd, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            WGPUShader vertexShader = Util.AssertSubtype<Shader, WGPUShader>(description.ShaderSet.Shaders.Single(s => s.Stage == ShaderStages.Vertex));
            WGPUShader fragmentShader = Util.AssertSubtype<Shader, WGPUShader>(description.ShaderSet.Shaders.Single(s => s.Stage == ShaderStages.Fragment));

            WGPUColorTargetState* targets = stackalloc WGPUColorTargetState[description.BlendState.AttachmentStates.Length];
            WGPUBlendState* blendStates = stackalloc WGPUBlendState[description.BlendState.AttachmentStates.Length];
            WGPUConstantEntry* constants = stackalloc WGPUConstantEntry[description.ShaderSet.Specializations?.Length ?? 0];
            WGPUVertexBufferLayout* vertexBufferLayouts = stackalloc WGPUVertexBufferLayout[description.ShaderSet.VertexLayouts.Length];
            WGPUVertexAttribute* vertexAttributes = stackalloc WGPUVertexAttribute[description.ShaderSet.VertexLayouts.Sum(l => l.Elements.Length)];
            WGPUBindGroupLayout* bindGroups = stackalloc WGPUBindGroupLayout[description.ResourceLayouts.Length];

            WGPUDepthStencilState* depthStencilState = stackalloc WGPUDepthStencilState[1];
            WGPUFragmentState* fragmentState = stackalloc WGPUFragmentState[1];

            for (int i = 0; i < description.BlendState.AttachmentStates.Length; i++)
            {
                var blendState = description.BlendState.AttachmentStates[i];
                var outputState = description.Outputs.ColorAttachments[i];

                blendStates[i] = new WGPUBlendState
                {
                    color = new WGPUBlendComponent
                    {
                        operation = WGPUFormats.VdToWGPUBlendOperation(blendState.ColorFunction),
                        srcFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.SourceColorFactor),
                        dstFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.DestinationColorFactor)
                    },
                    alpha = new WGPUBlendComponent
                    {
                        operation = WGPUFormats.VdToWGPUBlendOperation(blendState.AlphaFunction),
                        srcFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.SourceAlphaFactor),
                        dstFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.DestinationAlphaFactor)
                    }
                };

                targets[i] = new WGPUColorTargetState
                {
                    format = WGPUFormats.VdToWGPUTextureFormat(outputState.Format),
                    blend = &blendStates[i],
                    writeMask = WGPUFormats.VdToWGPUColorWriteMask(blendState.ColorWriteMask.GetOrDefault())
                };
            }

            if (description.Outputs.DepthAttachment is OutputAttachmentDescription depthAttachment)
            {
                var depthState = description.DepthStencilState;

                depthStencilState[0] = new WGPUDepthStencilState
                {
                    format = WGPUFormats.VdToWGPUTextureFormat(depthAttachment.Format),
                    depthWriteEnabled = depthState.DepthWriteEnabled,
                    depthCompare = WGPUFormats.VdToWGPUCompareFunction(depthState.DepthComparison),
                    stencilFront = new WGPUStencilFaceState
                    {
                        compare = WGPUFormats.VdToWGPUCompareFunction(depthState.StencilFront.Comparison),
                        failOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.Fail),
                        depthFailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.DepthFail),
                        passOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.Pass)
                    },
                    stencilBack = new WGPUStencilFaceState
                    {
                        compare = WGPUFormats.VdToWGPUCompareFunction(depthState.StencilBack.Comparison),
                        failOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.Fail),
                        depthFailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.DepthFail),
                        passOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.Pass)
                    },
                    stencilReadMask = FormatHelpers.IsStencilFormat(depthAttachment.Format) ? depthState.StencilReadMask : 0u,
                    stencilWriteMask = FormatHelpers.IsStencilFormat(depthAttachment.Format) ? depthState.StencilWriteMask : 0u,
                };
            }

            if (description.ShaderSet.Specializations != null)
            {
                for (int i = 0; i < description.ShaderSet.Specializations.Length; i++)
                {
                    var spec = description.ShaderSet.Specializations[i];

                    constants[i] = new WGPUConstantEntry
                    {
                        key = (sbyte*)&spec.ID,
                        value = spec.Data
                    };
                }
            }

            int attribGroupStartIndex = 0;

            for (int i = 0; i < description.ShaderSet.VertexLayouts.Length; i++)
            {
                var layout = description.ShaderSet.VertexLayouts[i];
                uint currentOffset = 0;

                for (int j = 0; j < layout.Elements.Length; j++)
                {
                    var attrib = layout.Elements[j];

                    vertexAttributes[j] = new WGPUVertexAttribute
                    {
                        format = WGPUFormats.VdToWGPUVertexFormat(attrib.Format),
                        offset = attrib.Offset != 0 ? attrib.Offset : currentOffset,
                        shaderLocation = (uint)(attribGroupStartIndex + j)
                    };

                    currentOffset += FormatSizeHelpers.GetSizeInBytes(attrib.Format);
                }

                vertexBufferLayouts[i] = new WGPUVertexBufferLayout
                {
                    arrayStride = layout.Stride,
                    stepMode = layout.InstanceStepRate != 0 ? WGPUVertexStepMode.Instance : WGPUVertexStepMode.Vertex,
                    attributeCount = (uint)layout.Elements.Length,
                    attributes = &vertexAttributes[attribGroupStartIndex]
                };

                attribGroupStartIndex += layout.Elements.Length;
            }

            for (int i = 0; i < description.ResourceLayouts.Length; i++)
            {
                var layout = Util.AssertSubtype<ResourceLayout, WGPUResourceLayout>(description.ResourceLayouts[i]);
                bindGroups[i] = layout.Layout;
            }

            fixed (sbyte* vertexEntryPointPtr = vertexShader.EntryPoint.GetUtf8Span())
            fixed (sbyte* fragmentEntryPointPtr = fragmentShader.EntryPoint.GetUtf8Span())
            {
                fragmentState[0] = new WGPUFragmentState
                {
                    module = fragmentShader.Module,
                    entryPoint = fragmentEntryPointPtr,
                    constantCount = (uint)(description.ShaderSet.Specializations?.Length ?? 0),
                    constants = constants,
                    targetCount = (uint)description.BlendState.AttachmentStates.Length,
                    targets = targets
                };

                WGPUPipelineLayoutDescriptor pipelineLayoutDesc = new WGPUPipelineLayoutDescriptor
                {
                    bindGroupLayoutCount = (uint)description.ResourceLayouts.Length,
                    bindGroupLayouts = bindGroups
                };

                Layout = wgpuDeviceCreatePipelineLayout(gd.NativeDevice, &pipelineLayoutDesc);

                WGPURenderPipelineDescriptor renderPipelineDesc = new WGPURenderPipelineDescriptor
                {
                    layout = Layout,
                    vertex = new WGPUVertexState
                    {
                        module = vertexShader.Module,
                        entryPoint = vertexEntryPointPtr,
                        constantCount = (uint)(description.ShaderSet.Specializations?.Length ?? 0),
                        constants = constants,
                        bufferCount = (uint)description.ShaderSet.VertexLayouts.Length,
                        buffers = vertexBufferLayouts
                    },
                    primitive = new WGPUPrimitiveState
                    {
                        topology = WGPUFormats.VdToWGPUPrimitiveTopology(description.PrimitiveTopology),
                        frontFace = WGPUFormats.VdToWGPUFrontFace(description.RasterizerState.FrontFace),
                        cullMode = WGPUFormats.VdToWGPUCullMode(description.RasterizerState.CullMode),
                    },
                    depthStencil = depthStencilState,
                    multisample = new WGPUMultisampleState
                    {
                        count = WGPUFormats.VdToWGPUSampleCount(description.Outputs.SampleCount),
                        mask = ~0u,
                        alphaToCoverageEnabled = description.BlendState.AlphaToCoverageEnabled
                    },
                    fragment = fragmentState
                };

                RenderPipeline = wgpuDeviceCreateRenderPipeline(gd.NativeDevice, &renderPipelineDesc);
            }
        }

        public WGPUPipeline(WGPUGraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            IsComputePipeline = true;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Layout.IsNotNull)
                wgpuPipelineLayoutRelease(Layout);

            if (RenderPipeline.IsNotNull)
                wgpuRenderPipelineRelease(RenderPipeline);

            isDisposed = true;
        }
    }
}

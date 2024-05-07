// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Silk.NET.WebGPU;
using Veldrid.Vk;

namespace Veldrid.WGPU
{
    internal unsafe class WGPUPipeline : Pipeline
    {
        public override bool IsComputePipeline { get; }
        public override string Name { get; set; }
        public override bool IsDisposed => isDisposed;

        public readonly PipelineLayout* Layout;
        public readonly RenderPipeline* RenderPipeline;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        public WGPUPipeline(WGPUGraphicsDevice gd, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;

            WGPUShader vertexShader = Util.AssertSubtype<Shader, WGPUShader>(description.ShaderSet.Shaders.Single(s => s.Stage == ShaderStages.Vertex));
            WGPUShader fragmentShader = Util.AssertSubtype<Shader, WGPUShader>(description.ShaderSet.Shaders.Single(s => s.Stage == ShaderStages.Vertex));

            ColorTargetState* targets = stackalloc ColorTargetState[description.BlendState.AttachmentStates.Length];
            BlendState* blendStates = stackalloc BlendState[description.BlendState.AttachmentStates.Length];
            ConstantEntry* constants = stackalloc ConstantEntry[description.ShaderSet.Specializations?.Length ?? 0];
            VertexBufferLayout* vertexBufferLayouts = stackalloc VertexBufferLayout[description.ShaderSet.VertexLayouts.Length];
            VertexAttribute* vertexAttributes = stackalloc VertexAttribute[description.ShaderSet.VertexLayouts.Sum(l => l.Elements.Length)];
            BindGroupLayout** bindGroups = stackalloc BindGroupLayout*[description.ResourceLayouts.Length];

            DepthStencilState* depthStencilState = stackalloc DepthStencilState[1];
            FragmentState* fragmentState = stackalloc FragmentState[1];

            for (int i = 0; i < description.BlendState.AttachmentStates.Length; i++)
            {
                var blendState = description.BlendState.AttachmentStates[i];
                var outputState = description.Outputs.ColorAttachments[i];

                blendStates[i] = new BlendState
                {
                    Color = new BlendComponent
                    {
                        Operation = WGPUFormats.VdToWGPUBlendOperation(blendState.ColorFunction),
                        SrcFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.SourceColorFactor),
                        DstFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.DestinationColorFactor)
                    },
                    Alpha = new BlendComponent
                    {
                        Operation = WGPUFormats.VdToWGPUBlendOperation(blendState.AlphaFunction),
                        SrcFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.SourceAlphaFactor),
                        DstFactor = WGPUFormats.VdToWGPUBlendFactor(blendState.DestinationAlphaFactor)
                    }
                };

                targets[i] = new ColorTargetState
                {
                    Format = WGPUFormats.VdToWGPUTextureFormat(outputState.Format),
                    Blend = &blendStates[i],
                    WriteMask = WGPUFormats.VdToWGPUColorWriteMask(blendState.ColorWriteMask.GetOrDefault())
                };
            }

            if (description.Outputs.DepthAttachment is OutputAttachmentDescription depthAttachment)
            {
                var depthState = description.DepthStencilState;

                depthStencilState[0] = new DepthStencilState
                {
                    Format = WGPUFormats.VdToWGPUTextureFormat(depthAttachment.Format),
                    DepthWriteEnabled = depthState.DepthWriteEnabled,
                    DepthCompare = WGPUFormats.VdToWGPUCompareFunction(depthState.DepthComparison),
                    StencilFront = new StencilFaceState
                    {
                        Compare = WGPUFormats.VdToWGPUCompareFunction(depthState.StencilFront.Comparison),
                        FailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.Fail),
                        DepthFailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.DepthFail),
                        PassOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilFront.Pass)
                    },
                    StencilBack = new StencilFaceState
                    {
                        Compare = WGPUFormats.VdToWGPUCompareFunction(depthState.StencilBack.Comparison),
                        FailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.Fail),
                        DepthFailOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.DepthFail),
                        PassOp = WGPUFormats.VdToWGPUStencilOperation(depthState.StencilBack.Pass)
                    },
                    StencilReadMask = depthState.StencilReadMask,
                    StencilWriteMask = depthState.StencilWriteMask,
                };
            }

            if (description.ShaderSet.Specializations != null)
            {
                for (int i = 0; i < description.ShaderSet.Specializations.Length; i++)
                {
                    var spec = description.ShaderSet.Specializations[i];

                    constants[i] = new ConstantEntry
                    {
                        Key = (byte*)&spec.ID,
                        Value = spec.Data
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

                    vertexAttributes[j] = new VertexAttribute
                    {
                        Format = WGPUFormats.VdToWGPUVertexFormat(attrib.Format),
                        Offset = attrib.Offset != 0 ? attrib.Offset : currentOffset,
                        ShaderLocation = (uint)(attribGroupStartIndex + j)
                    };

                    currentOffset += FormatSizeHelpers.GetSizeInBytes(attrib.Format);
                }

                vertexBufferLayouts[i] = new VertexBufferLayout
                {
                    ArrayStride = layout.Stride,
                    StepMode = layout.InstanceStepRate != 0 ? VertexStepMode.Instance : VertexStepMode.Vertex,
                    AttributeCount = (uint)layout.Elements.Length,
                    Attributes = &vertexAttributes[attribGroupStartIndex]
                };

                attribGroupStartIndex += layout.Elements.Length;
            }

            for (int i = 0; i < description.ResourceLayouts.Length; i++)
            {
                var layout = Util.AssertSubtype<ResourceLayout, WGPUResourceLayout>(description.ResourceLayouts[i]);
                bindGroups[i] = layout.Layout;
            }

            fragmentState[0] = new FragmentState
            {
                Module = fragmentShader.Module,
                EntryPoint = new FixedUtf8String(fragmentShader.EntryPoint),
                ConstantCount = (uint)(description.ShaderSet.Specializations?.Length ?? 0),
                Constants = constants,
                TargetCount = (uint)description.BlendState.AttachmentStates.Length,
                Targets = targets
            };

            Layout = gd.WebGPU.DeviceCreatePipelineLayout(gd.NativeDevice, new PipelineLayoutDescriptor
            {
                BindGroupLayoutCount = (uint)description.ResourceLayouts.Length,
                BindGroupLayouts = bindGroups
            });

            RenderPipeline = gd.WebGPU.DeviceCreateRenderPipeline(gd.NativeDevice, new RenderPipelineDescriptor
            {
                Layout = Layout,
                Vertex = new VertexState
                {
                    Module = vertexShader.Module,
                    EntryPoint = new FixedUtf8String(vertexShader.EntryPoint),
                    ConstantCount = (uint)(description.ShaderSet.Specializations?.Length ?? 0),
                    Constants = constants,
                    BufferCount = (uint)description.ShaderSet.VertexLayouts.Length,
                    Buffers = vertexBufferLayouts
                },
                Primitive = new PrimitiveState
                {
                    Topology = WGPUFormats.VdToWGPUPrimitiveTopology(description.PrimitiveTopology),
                    FrontFace = WGPUFormats.VdToWGPUFrontFace(description.RasterizerState.FrontFace),
                    CullMode = WGPUFormats.VdToWGPUCullMode(description.RasterizerState.CullMode),
                },
                DepthStencil = depthStencilState,
                Multisample = new MultisampleState
                {
                    Count = WGPUFormats.VdToWGPUSampleCount(description.Outputs.SampleCount),
                    Mask = ~0u,
                    AlphaToCoverageEnabled = description.BlendState.AlphaToCoverageEnabled
                },
                Fragment = fragmentState
            });
        }

        public WGPUPipeline(WGPUGraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;

            IsComputePipeline = true;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            if (Layout != null)
                gd.WebGPU.PipelineLayoutRelease(Layout);

            if (RenderPipeline != null)
                gd.WebGPU.RenderPipelineRelease(RenderPipeline);

            isDisposed = true;
        }
    }
}

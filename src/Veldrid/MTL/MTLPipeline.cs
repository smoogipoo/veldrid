using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MtlPipeline : Pipeline
    {
        private static readonly StateCache<RenderPipelineStateLookup, CachedRenderPipelineState> render_pipeline_states =
            new StateCache<RenderPipelineStateLookup, CachedRenderPipelineState>(createRenderState);

        private static readonly StateCache<DepthStencilStateLookup, CachedDepthStencilState> depth_stencil_states
            = new StateCache<DepthStencilStateLookup, CachedDepthStencilState>(createDepthStencilState);

        private static readonly StateCache<ComputePipelineStateLookup, CachedComputePipelineState> compute_pipeline_states
            = new StateCache<ComputePipelineStateLookup, CachedComputePipelineState>(createComputeState);

        public MTLRenderPipelineState RenderPipelineState { get; }
        public MTLComputePipelineState ComputePipelineState { get; }
        public MTLPrimitiveType PrimitiveType { get; }
        public new MtlResourceLayout[] ResourceLayouts { get; }
        public ResourceBindingModel ResourceBindingModel { get; }
        public uint VertexBufferCount { get; }
        public uint NonVertexBufferCount { get; }
        public MTLCullMode CullMode { get; }
        public MTLWinding FrontFace { get; }
        public MTLTriangleFillMode FillMode { get; }
        public MTLDepthStencilState DepthStencilState { get; }
        public MTLDepthClipMode DepthClipMode { get; }
        public override bool IsComputePipeline { get; }
        public bool ScissorTestEnabled { get; }
        public MTLSize ThreadsPerThreadgroup { get; } = new MTLSize(1, 1, 1);
        public uint StencilReference { get; }
        public RgbaFloat BlendColor { get; }
        public override bool IsDisposed => disposed;
        public override string Name { get; set; }

        private readonly RenderPipelineStateLookup renderPipelineStateLookup;
        private readonly DepthStencilStateLookup depthStencilStateLookup;
        private readonly ComputePipelineStateLookup computePipelineStateLookup;

        private bool disposed;

        public MtlPipeline(ref GraphicsPipelineDescription description, MtlGraphicsDevice gd)
            : base(ref description)
        {
            PrimitiveType = MtlFormats.VdToMtlPrimitiveTopology(description.PrimitiveTopology);
            ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];
            NonVertexBufferCount = 0;

            for (int i = 0; i < ResourceLayouts.Length; i++)
            {
                ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);
                NonVertexBufferCount += ResourceLayouts[i].BufferCount;
            }

            ResourceBindingModel = description.ResourceBindingModel ?? gd.ResourceBindingModel;
            CullMode = MtlFormats.VdToMtlCullMode(description.RasterizerState.CullMode);
            FrontFace = MtlFormats.VdVoMtlFrontFace(description.RasterizerState.FrontFace);
            FillMode = MtlFormats.VdToMtlFillMode(description.RasterizerState.FillMode);
            ScissorTestEnabled = description.RasterizerState.ScissorTestEnabled;
            VertexBufferCount = (uint)description.ShaderSet.VertexLayouts.Length;
            BlendColor = description.BlendState.BlendFactor;
            StencilReference = description.DepthStencilState.StencilReference;
            DepthClipMode = description.DepthStencilState.DepthTestEnabled ? MTLDepthClipMode.Clip : MTLDepthClipMode.Clamp;

            renderPipelineStateLookup = new RenderPipelineStateLookup
            {
                Device = gd,
                ShaderSet = description.ShaderSet,
                BlendState = description.BlendState,
                Outputs = description.Outputs,
                ResourceBindingModel = ResourceBindingModel,
                NonVertexBufferCount = NonVertexBufferCount
            };

            depthStencilStateLookup = new DepthStencilStateLookup
            {
                Device = gd,
                DepthStencilState = description.DepthStencilState
            };

            RenderPipelineState = render_pipeline_states.Get(renderPipelineStateLookup).State;

            if (description.Outputs.DepthAttachment != null)
                DepthStencilState = depth_stencil_states.Get(depthStencilStateLookup).State;
        }

        public MtlPipeline(ref ComputePipelineDescription description, MtlGraphicsDevice gd)
            : base(ref description)
        {
            IsComputePipeline = true;
            ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];

            for (int i = 0; i < ResourceLayouts.Length; i++) ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);

            ThreadsPerThreadgroup = new MTLSize(
                description.ThreadGroupSizeX,
                description.ThreadGroupSizeY,
                description.ThreadGroupSizeZ);

            computePipelineStateLookup = new ComputePipelineStateLookup
            {
                Device = gd,
                ComputeShader = description.ComputeShader,
                ResourceLayouts = description.ResourceLayouts,
                Specializations = description.Specializations
            };

            ComputePipelineState = compute_pipeline_states.Get(computePipelineStateLookup).State;
        }

        private static CachedRenderPipelineState createRenderState(RenderPipelineStateLookup lookup)
        {
            var mtlDesc = MTLRenderPipelineDescriptor.New();
            List<MTLFunction> functions = null;

            foreach (var shader in lookup.ShaderSet.Shaders)
            {
                var mtlShader = Util.AssertSubtype<Shader, MtlShader>(shader);
                MTLFunction specializedFunction;

                if (mtlShader.HasFunctionConstants)
                {
                    // Need to create specialized MTLFunction.
                    var constantValues = createConstantValues(lookup.ShaderSet.Specializations);
                    specializedFunction = mtlShader.Library.newFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);

                    Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");

                    functions ??= new List<MTLFunction>();
                    functions.Add(specializedFunction);

                    ObjectiveCRuntime.release(constantValues.NativePtr);
                }
                else
                    specializedFunction = mtlShader.Function;

                if (shader.Stage == ShaderStages.Vertex)
                    mtlDesc.vertexFunction = specializedFunction;
                else if (shader.Stage == ShaderStages.Fragment)
                    mtlDesc.fragmentFunction = specializedFunction;
            }

            for (uint i = 0; i < lookup.ShaderSet.VertexLayouts.Length; i++)
            {
                uint layoutIndex = lookup.ResourceBindingModel == ResourceBindingModel.Improved
                    ? lookup.NonVertexBufferCount + i
                    : i;

                var mtlLayout = mtlDesc.vertexDescriptor.layouts[layoutIndex];
                uint stepRate = lookup.ShaderSet.VertexLayouts[i].InstanceStepRate;

                mtlLayout.stride = lookup.ShaderSet.VertexLayouts[i].Stride;
                mtlLayout.stepFunction = stepRate == 0 ? MTLVertexStepFunction.PerVertex : MTLVertexStepFunction.PerInstance;
                mtlLayout.stepRate = Math.Max(1, stepRate);
            }

            uint element = 0;

            for (uint i = 0; i < lookup.ShaderSet.VertexLayouts.Length; i++)
            {
                uint offset = 0;
                var vdDesc = lookup.ShaderSet.VertexLayouts[i];

                for (uint j = 0; j < vdDesc.Elements.Length; j++)
                {
                    var elementDesc = vdDesc.Elements[j];
                    var mtlAttribute = mtlDesc.vertexDescriptor.attributes[element];

                    mtlAttribute.bufferIndex = lookup.ResourceBindingModel == ResourceBindingModel.Improved
                        ? lookup.NonVertexBufferCount + i
                        : i;
                    mtlAttribute.format = MtlFormats.VdToMtlVertexFormat(elementDesc.Format);
                    mtlAttribute.offset = elementDesc.Offset != 0 ? elementDesc.Offset : (UIntPtr)offset;

                    offset += FormatSizeHelpers.GetSizeInBytes(elementDesc.Format);
                    element += 1;
                }
            }

            if (lookup.Outputs.SampleCount != TextureSampleCount.Count1)
                mtlDesc.sampleCount = FormatHelpers.GetSampleCountUInt32(lookup.Outputs.SampleCount);

            if (lookup.Outputs.DepthAttachment != null)
            {
                var depthFormat = lookup.Outputs.DepthAttachment.Value.Format;
                var mtlDepthFormat = MtlFormats.VdToMtlPixelFormat(depthFormat, true);

                mtlDesc.depthAttachmentPixelFormat = mtlDepthFormat;
                if (FormatHelpers.IsStencilFormat(depthFormat))
                    mtlDesc.stencilAttachmentPixelFormat = mtlDepthFormat;
            }

            for (uint i = 0; i < lookup.Outputs.ColorAttachments.Length; i++)
            {
                var attachmentBlendDesc = lookup.BlendState.AttachmentStates[i];
                var colorDesc = mtlDesc.colorAttachments[i];

                colorDesc.pixelFormat = MtlFormats.VdToMtlPixelFormat(lookup.Outputs.ColorAttachments[i].Format, false);
                colorDesc.blendingEnabled = attachmentBlendDesc.BlendEnabled;
                colorDesc.writeMask = MtlFormats.VdToMtlColorWriteMask(attachmentBlendDesc.ColorWriteMask.GetOrDefault());
                colorDesc.alphaBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.AlphaFunction);
                colorDesc.sourceAlphaBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceAlphaFactor);
                colorDesc.destinationAlphaBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationAlphaFactor);
                colorDesc.rgbBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.ColorFunction);
                colorDesc.sourceRGBBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceColorFactor);
                colorDesc.destinationRGBBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationColorFactor);
            }

            mtlDesc.alphaToCoverageEnabled = lookup.BlendState.AlphaToCoverageEnabled;

            MTLRenderPipelineState state = lookup.Device.Device.newRenderPipelineStateWithDescriptor(mtlDesc);
            ObjectiveCRuntime.release(mtlDesc.NativePtr);

            return new CachedRenderPipelineState(state, functions);
        }

        private static CachedDepthStencilState createDepthStencilState(DepthStencilStateLookup lookup)
        {
            var depthDescriptor = MTLUtil.AllocInit<MTLDepthStencilDescriptor>(
                nameof(MTLDepthStencilDescriptor));
            depthDescriptor.depthCompareFunction = MtlFormats.VdToMtlCompareFunction(
                lookup.DepthStencilState.DepthComparison);
            depthDescriptor.depthWriteEnabled = lookup.DepthStencilState.DepthWriteEnabled;

            bool stencilEnabled = lookup.DepthStencilState.StencilTestEnabled;

            if (stencilEnabled)
            {
                var vdFrontDesc = lookup.DepthStencilState.StencilFront;
                var front = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                front.readMask = lookup.DepthStencilState.StencilReadMask;
                front.writeMask = lookup.DepthStencilState.StencilWriteMask;
                front.depthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.DepthFail);
                front.stencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Fail);
                front.depthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Pass);
                front.stencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdFrontDesc.Comparison);
                depthDescriptor.frontFaceStencil = front;

                var vdBackDesc = lookup.DepthStencilState.StencilBack;
                var back = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                back.readMask = lookup.DepthStencilState.StencilReadMask;
                back.writeMask = lookup.DepthStencilState.StencilWriteMask;
                back.depthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.DepthFail);
                back.stencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Fail);
                back.depthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Pass);
                back.stencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdBackDesc.Comparison);
                depthDescriptor.backFaceStencil = back;

                ObjectiveCRuntime.release(front.NativePtr);
                ObjectiveCRuntime.release(back.NativePtr);
            }

            MTLDepthStencilState state = lookup.Device.Device.newDepthStencilStateWithDescriptor(depthDescriptor);
            ObjectiveCRuntime.release(depthDescriptor.NativePtr);

            return new CachedDepthStencilState(state);
        }

        private static CachedComputePipelineState createComputeState(ComputePipelineStateLookup lookup)
        {
            var mtlDesc = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(nameof(MTLComputePipelineDescriptor));
            var mtlShader = Util.AssertSubtype<Shader, MtlShader>(lookup.ComputeShader);

            MTLFunction specializedFunction;
            List<MTLFunction> functions = null;

            if (mtlShader.HasFunctionConstants)
            {
                // Need to create specialized MTLFunction.
                var constantValues = createConstantValues(lookup.Specializations);
                specializedFunction = mtlShader.Library.newFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);

                Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");

                functions = new List<MTLFunction> { specializedFunction };

                ObjectiveCRuntime.release(constantValues.NativePtr);
            }
            else
                specializedFunction = mtlShader.Function;

            mtlDesc.computeFunction = specializedFunction;
            var buffers = mtlDesc.buffers;
            uint bufferIndex = 0;

            foreach (var layout in lookup.ResourceLayouts)
            {
                foreach (var rle in layout.Description.Elements)
                {
                    var kind = rle.Kind;

                    if (kind == ResourceKind.UniformBuffer || kind == ResourceKind.StructuredBufferReadOnly)
                    {
                        var bufferDesc = buffers[bufferIndex];
                        bufferDesc.mutability = MTLMutability.Immutable;
                        bufferIndex += 1;
                    }
                    else if (kind == ResourceKind.StructuredBufferReadWrite)
                    {
                        var bufferDesc = buffers[bufferIndex];
                        bufferDesc.mutability = MTLMutability.Mutable;
                        bufferIndex += 1;
                    }
                }
            }

            MTLComputePipelineState state = lookup.Device.Device.newComputePipelineStateWithDescriptor(mtlDesc);
            ObjectiveCRuntime.release(mtlDesc.NativePtr);

            return new CachedComputePipelineState(state, functions);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!disposed)
            {
                render_pipeline_states.Remove(renderPipelineStateLookup);
                depth_stencil_states.Remove(depthStencilStateLookup);
                compute_pipeline_states.Remove(computePipelineStateLookup);

                disposed = true;
            }
        }

        #endregion

        private static unsafe MTLFunctionConstantValues createConstantValues(SpecializationConstant[] specializations)
        {
            var ret = MTLFunctionConstantValues.New();

            if (specializations != null)
            {
                foreach (var sc in specializations)
                {
                    var mtlType = MtlFormats.VdVoMtlShaderConstantType(sc.Type);
                    ret.setConstantValuetypeatIndex(&sc.Data, mtlType, sc.ID);
                }
            }

            return ret;
        }

        private readonly record struct RenderPipelineStateLookup(
            MtlGraphicsDevice Device,
            ShaderSetDescription ShaderSet,
            OutputDescription Outputs,
            BlendStateDescription BlendState,
            ResourceBindingModel ResourceBindingModel,
            uint NonVertexBufferCount);

        private readonly record struct DepthStencilStateLookup(
            MtlGraphicsDevice Device,
            DepthStencilStateDescription DepthStencilState);

        private readonly record struct ComputePipelineStateLookup(
            MtlGraphicsDevice Device,
            Shader ComputeShader,
            ResourceLayout[] ResourceLayouts,
            SpecializationConstant[] Specializations)
        {
            public bool Equals(ComputePipelineStateLookup other)
                => ComputeShader == other.ComputeShader
                   && Util.ArrayEquals(ResourceLayouts, other.ResourceLayouts)
                   && Util.ArrayEqualsEquatable(Specializations, other.Specializations);

            public override int GetHashCode()
                => HashCode.Combine(
                    ComputeShader,
                    HashHelper.Array(ResourceLayouts),
                    HashHelper.Array(Specializations));
        }

        private readonly record struct CachedRenderPipelineState(MTLRenderPipelineState State, List<MTLFunction> Functions) : IDisposable
        {
            public void Dispose()
            {
                ObjectiveCRuntime.release(State.NativePtr);

                if (Functions == null)
                    return;

                foreach (var function in Functions)
                    ObjectiveCRuntime.release(function.NativePtr);
            }
        }

        private readonly record struct CachedDepthStencilState(MTLDepthStencilState State) : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private readonly record struct CachedComputePipelineState(MTLComputePipelineState State, List<MTLFunction> Functions) : IDisposable
        {
            public void Dispose()
            {
                ObjectiveCRuntime.release(State.NativePtr);

                if (Functions == null)
                    return;

                foreach (var function in Functions)
                    ObjectiveCRuntime.release(function.NativePtr);
            }
        }

        private class StateCache<TLookup, TValue>
            where TLookup : IEquatable<TLookup>
            where TValue : IDisposable
        {
            private readonly Dictionary<TLookup, TValue> states = new Dictionary<TLookup, TValue>();
            private readonly Dictionary<TLookup, uint> refCounts = new Dictionary<TLookup, uint>();
            private readonly Func<TLookup, TValue> factory;

            public StateCache(Func<TLookup, TValue> factory)
            {
                this.factory = factory;
            }

            public TValue Get(TLookup lookup)
            {
                if (states.TryGetValue(lookup, out TValue existing))
                {
                    refCounts[lookup] += 1;
                    return existing;
                }

                TValue newState = factory(lookup);

                states[lookup] = newState;
                refCounts[lookup] = 1;

                return newState;
            }

            public void Remove(TLookup lookup)
            {
                if (!states.TryGetValue(lookup, out TValue existing))
                    return;

                if (--refCounts[lookup] > 0)
                    return;

                existing.Dispose();

                states.Remove(lookup);
                refCounts.Remove(lookup);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal class D3D12ResourceCache
    {
        private readonly ID3D12Device _device;
        private readonly object _lock = new object();

        private readonly Dictionary<BlendStateDescription, BlendDescription> _blendStates
            = new Dictionary<BlendStateDescription, BlendDescription>();

        private readonly Dictionary<DepthStencilStateDescription, DepthStencilDescription> _depthStencilStates
            = new Dictionary<DepthStencilStateDescription, DepthStencilDescription>();

        private readonly Dictionary<D3D11RasterizerStateCacheKey, RasterizerDescription> _rasterizerStates
            = new Dictionary<D3D11RasterizerStateCacheKey, RasterizerDescription>();

        private readonly Dictionary<InputLayoutCacheKey, InputLayoutDescription> _inputLayouts
            = new Dictionary<InputLayoutCacheKey, InputLayoutDescription>();

        public D3D12ResourceCache(ID3D12Device device)
        {
            _device = device;
        }

        public void GetPipelineResources(
            ref BlendStateDescription blendDesc,
            ref DepthStencilStateDescription dssDesc,
            ref RasterizerStateDescription rasterDesc,
            bool multisample,
            VertexLayoutDescription[] vertexLayouts,
            byte[] vsBytecode,
            out BlendDescription blendState,
            out DepthStencilDescription depthState,
            out RasterizerDescription rasterState,
            out InputLayoutDescription inputLayout)
        {
            lock (_lock)
            {
                blendState = GetBlendState(ref blendDesc);
                depthState = GetDepthStencilState(ref dssDesc);
                rasterState = GetRasterizerState(ref rasterDesc, multisample);
                inputLayout = GetInputLayout(vertexLayouts, vsBytecode);
            }
        }

        private BlendDescription GetBlendState(ref BlendStateDescription description)
        {
            Debug.Assert(Monitor.IsEntered(_lock));

            if (!_blendStates.TryGetValue(description, out BlendDescription blendState))
            {
                blendState = CreateNewBlendState(ref description);
                BlendStateDescription key = description;
                key.AttachmentStates = (BlendAttachmentDescription[])key.AttachmentStates.Clone();
                _blendStates.Add(key, blendState);
            }

            return blendState;
        }

        private BlendDescription CreateNewBlendState(ref BlendStateDescription description)
        {
            BlendAttachmentDescription[] attachmentStates = description.AttachmentStates;
            BlendDescription d3dBlendStateDesc = new BlendDescription();

            for (int i = 0; i < attachmentStates.Length; i++)
            {
                BlendAttachmentDescription state = attachmentStates[i];
                d3dBlendStateDesc.RenderTarget[i].BlendEnable = state.BlendEnabled;
                d3dBlendStateDesc.RenderTarget[i].RenderTargetWriteMask = D3D12Formats.VdToD3D12ColorWriteEnable(state.ColorWriteMask.GetOrDefault());
                d3dBlendStateDesc.RenderTarget[i].SourceBlend = D3D12Formats.VdToD3D12Blend(state.SourceColorFactor);
                d3dBlendStateDesc.RenderTarget[i].DestinationBlend = D3D12Formats.VdToD3D12Blend(state.DestinationColorFactor);
                d3dBlendStateDesc.RenderTarget[i].BlendOperation = D3D12Formats.VdToD3D12BlendOperation(state.ColorFunction);
                d3dBlendStateDesc.RenderTarget[i].SourceBlendAlpha = D3D12Formats.VdToD3D12Blend(state.SourceAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].DestinationBlendAlpha = D3D12Formats.VdToD3D12Blend(state.DestinationAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].BlendOperationAlpha = D3D12Formats.VdToD3D12BlendOperation(state.AlphaFunction);
            }

            d3dBlendStateDesc.AlphaToCoverageEnable = description.AlphaToCoverageEnabled;
            d3dBlendStateDesc.IndependentBlendEnable = true;

            return d3dBlendStateDesc;
        }

        private DepthStencilDescription GetDepthStencilState(ref DepthStencilStateDescription description)
        {
            Debug.Assert(Monitor.IsEntered(_lock));

            if (!_depthStencilStates.TryGetValue(description, out DepthStencilDescription dss))
            {
                dss = CreateNewDepthStencilState(ref description);
                DepthStencilStateDescription key = description;
                _depthStencilStates.Add(key, dss);
            }

            return dss;
        }

        private DepthStencilDescription CreateNewDepthStencilState(ref DepthStencilStateDescription description)
        {
            DepthStencilDescription dssDesc = new DepthStencilDescription
            {
                DepthFunc = D3D12Formats.VdToD3D12ComparisonFunc(description.DepthComparison),
                DepthEnable = description.DepthTestEnabled,
                DepthWriteMask = description.DepthWriteEnabled ? DepthWriteMask.All : DepthWriteMask.Zero,
                StencilEnable = description.StencilTestEnabled,
                FrontFace = ToD3D12StencilOpDesc(description.StencilFront),
                BackFace = ToD3D12StencilOpDesc(description.StencilBack),
                StencilReadMask = description.StencilReadMask,
                StencilWriteMask = description.StencilWriteMask
            };

            return dssDesc;
        }

        private DepthStencilOperationDescription ToD3D12StencilOpDesc(StencilBehaviorDescription sbd)
        {
            return new DepthStencilOperationDescription
            {
                StencilFunc = D3D12Formats.VdToD3D12ComparisonFunc(sbd.Comparison),
                StencilPassOp = D3D12Formats.VdToD3D12StencilOperation(sbd.Pass),
                StencilFailOp = D3D12Formats.VdToD3D12StencilOperation(sbd.Fail),
                StencilDepthFailOp = D3D12Formats.VdToD3D12StencilOperation(sbd.DepthFail)
            };
        }

        private RasterizerDescription GetRasterizerState(ref RasterizerStateDescription description, bool multisample)
        {
            Debug.Assert(Monitor.IsEntered(_lock));
            D3D11RasterizerStateCacheKey key = new D3D11RasterizerStateCacheKey(description, multisample);

            if (!_rasterizerStates.TryGetValue(key, out RasterizerDescription rasterizerState))
            {
                rasterizerState = CreateNewRasterizerState(ref key);
                _rasterizerStates.Add(key, rasterizerState);
            }

            return rasterizerState;
        }

        private RasterizerDescription CreateNewRasterizerState(ref D3D11RasterizerStateCacheKey key)
        {
            RasterizerDescription rssDesc = new RasterizerDescription
            {
                CullMode = D3D12Formats.VdToD3D12CullMode(key.VeldridDescription.CullMode),
                FillMode = D3D12Formats.VdToD3D12FillMode(key.VeldridDescription.FillMode),
                DepthClipEnable = key.VeldridDescription.DepthClipEnabled,
                FrontCounterClockwise = key.VeldridDescription.FrontFace == FrontFace.CounterClockwise,
                MultisampleEnable = key.Multisampled
            };

            return rssDesc;
        }

        private InputLayoutDescription GetInputLayout(VertexLayoutDescription[] vertexLayouts, byte[] vsBytecode)
        {
            Debug.Assert(Monitor.IsEntered(_lock));

            if (vsBytecode == null || vertexLayouts == null || vertexLayouts.Length == 0)
            {
                return null;
            }

            InputLayoutCacheKey tempKey = InputLayoutCacheKey.CreateTempKey(vertexLayouts);

            if (!_inputLayouts.TryGetValue(tempKey, out InputLayoutDescription inputLayout))
            {
                inputLayout = CreateNewInputLayout(vertexLayouts, vsBytecode);
                InputLayoutCacheKey permanentKey = InputLayoutCacheKey.CreatePermanentKey(vertexLayouts);
                _inputLayouts.Add(permanentKey, inputLayout);
            }

            return inputLayout;
        }

        private InputLayoutDescription CreateNewInputLayout(VertexLayoutDescription[] vertexLayouts, byte[] vsBytecode)
        {
            int totalCount = 0;

            for (int i = 0; i < vertexLayouts.Length; i++)
            {
                totalCount += vertexLayouts[i].Elements.Length;
            }

            int element = 0; // Total element index across slots.
            InputElementDescription[] elements = new InputElementDescription[totalCount];
            SemanticIndices si = new SemanticIndices();

            for (int slot = 0; slot < vertexLayouts.Length; slot++)
            {
                VertexElementDescription[] elementDescs = vertexLayouts[slot].Elements;
                uint stepRate = vertexLayouts[slot].InstanceStepRate;
                int currentOffset = 0;

                for (int i = 0; i < elementDescs.Length; i++)
                {
                    VertexElementDescription desc = elementDescs[i];
                    elements[element] = new InputElementDescription(
                        GetSemanticString(desc.Semantic),
                        SemanticIndices.GetAndIncrement(ref si, desc.Semantic),
                        D3D12Formats.ToDxgiFormat(desc.Format),
                        desc.Offset != 0 ? (int)desc.Offset : currentOffset,
                        slot,
                        stepRate == 0 ? InputClassification.PerVertexData : InputClassification.PerInstanceData,
                        (int)stepRate);

                    currentOffset += (int)FormatSizeHelpers.GetSizeInBytes(desc.Format);
                    element += 1;
                }
            }

            return new InputLayoutDescription(elements);
        }

        private string GetSemanticString(VertexElementSemantic semantic)
        {
            switch (semantic)
            {
                case VertexElementSemantic.Position:
                    return "POSITION";

                case VertexElementSemantic.Normal:
                    return "NORMAL";

                case VertexElementSemantic.TextureCoordinate:
                    return "TEXCOORD";

                case VertexElementSemantic.Color:
                    return "COLOR";

                default:
                    throw Illegal.Value<VertexElementSemantic>();
            }
        }

        public void Dispose()
        {
        }

        private struct SemanticIndices
        {
            private int _position;
            private int _texCoord;
            private int _normal;
            private int _color;

            public static int GetAndIncrement(ref SemanticIndices si, VertexElementSemantic type)
            {
                switch (type)
                {
                    case VertexElementSemantic.Position:
                        return si._position++;

                    case VertexElementSemantic.TextureCoordinate:
                        return si._texCoord++;

                    case VertexElementSemantic.Normal:
                        return si._normal++;

                    case VertexElementSemantic.Color:
                        return si._color++;

                    default:
                        throw Illegal.Value<VertexElementSemantic>();
                }
            }
        }

        private struct InputLayoutCacheKey : IEquatable<InputLayoutCacheKey>
        {
            public VertexLayoutDescription[] VertexLayouts;

            public static InputLayoutCacheKey CreateTempKey(VertexLayoutDescription[] original)
                => new InputLayoutCacheKey { VertexLayouts = original };

            public static InputLayoutCacheKey CreatePermanentKey(VertexLayoutDescription[] original)
            {
                VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[original.Length];

                for (int i = 0; i < original.Length; i++)
                {
                    vertexLayouts[i].Stride = original[i].Stride;
                    vertexLayouts[i].InstanceStepRate = original[i].InstanceStepRate;
                    vertexLayouts[i].Elements = (VertexElementDescription[])original[i].Elements.Clone();
                }

                return new InputLayoutCacheKey { VertexLayouts = vertexLayouts };
            }

            public bool Equals(InputLayoutCacheKey other)
            {
                return Util.ArrayEqualsEquatable(VertexLayouts, other.VertexLayouts);
            }

            public override int GetHashCode()
            {
                return HashHelper.Array(VertexLayouts);
            }
        }

        private struct D3D11RasterizerStateCacheKey : IEquatable<D3D11RasterizerStateCacheKey>
        {
            public RasterizerStateDescription VeldridDescription;
            public bool Multisampled;

            public D3D11RasterizerStateCacheKey(RasterizerStateDescription veldridDescription, bool multisampled)
            {
                VeldridDescription = veldridDescription;
                Multisampled = multisampled;
            }

            public bool Equals(D3D11RasterizerStateCacheKey other)
            {
                return VeldridDescription.Equals(other.VeldridDescription)
                       && Multisampled.Equals(other.Multisampled);
            }

            public override int GetHashCode()
            {
                return HashHelper.Combine(VeldridDescription.GetHashCode(), Multisampled.GetHashCode());
            }
        }
    }
}

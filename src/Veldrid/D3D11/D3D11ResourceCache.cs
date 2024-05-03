using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11ResourceCache : IDisposable
    {
        private readonly ComPtr<ID3D11Device> device;
        private readonly object @lock = new object();

        private readonly Dictionary<BlendStateDescription, ComPtr<ID3D11BlendState>> blendStates
            = new Dictionary<BlendStateDescription, ComPtr<ID3D11BlendState>>();

        private readonly Dictionary<DepthStencilStateDescription, ComPtr<ID3D11DepthStencilState>> depthStencilStates
            = new Dictionary<DepthStencilStateDescription, ComPtr<ID3D11DepthStencilState>>();

        private readonly Dictionary<D3D11RasterizerStateCacheKey, ComPtr<ID3D11RasterizerState>> rasterizerStates
            = new Dictionary<D3D11RasterizerStateCacheKey, ComPtr<ID3D11RasterizerState>>();

        private readonly Dictionary<InputLayoutCacheKey, ComPtr<ID3D11InputLayout>> inputLayouts
            = new Dictionary<InputLayoutCacheKey, ComPtr<ID3D11InputLayout>>();

        public D3D11ResourceCache(ComPtr<ID3D11Device> device)
        {
            this.device = device;
        }

        #region Disposal

        public void Dispose()
        {
            foreach (var kvp in blendStates) kvp.Value.Dispose();

            foreach (var kvp in depthStencilStates) kvp.Value.Dispose();

            foreach (var kvp in rasterizerStates) kvp.Value.Dispose();

            foreach (var kvp in inputLayouts) kvp.Value.Dispose();
        }

        #endregion

        public void GetPipelineResources(
            ref BlendStateDescription blendDesc,
            ref DepthStencilStateDescription dssDesc,
            ref RasterizerStateDescription rasterDesc,
            bool multisample,
            VertexLayoutDescription[] vertexLayouts,
            byte[] vsBytecode,
            out ComPtr<ID3D11BlendState> blendState,
            out ComPtr<ID3D11DepthStencilState> depthState,
            out ComPtr<ID3D11RasterizerState> rasterState,
            out ComPtr<ID3D11InputLayout> inputLayout)
        {
            lock (@lock)
            {
                blendState = getBlendState(ref blendDesc);
                depthState = getDepthStencilState(ref dssDesc);
                rasterState = getRasterizerState(ref rasterDesc, multisample);
                inputLayout = getInputLayout(vertexLayouts, vsBytecode);
            }
        }

        private ComPtr<ID3D11BlendState> getBlendState(ref BlendStateDescription description)
        {
            Debug.Assert(Monitor.IsEntered(@lock));

            if (!blendStates.TryGetValue(description, out var blendState))
            {
                blendState = createNewBlendState(ref description);
                var key = description;
                key.AttachmentStates = (BlendAttachmentDescription[])key.AttachmentStates.Clone();
                blendStates.Add(key, blendState);
            }

            return blendState;
        }

        private ComPtr<ID3D11BlendState> createNewBlendState(ref BlendStateDescription description)
        {
            var attachmentStates = description.AttachmentStates;
            var d3dBlendStateDesc = new BlendDesc();

            for (int i = 0; i < attachmentStates.Length; i++)
            {
                var state = attachmentStates[i];
                d3dBlendStateDesc.RenderTarget[i].BlendEnable = state.BlendEnabled;
                d3dBlendStateDesc.RenderTarget[i].RenderTargetWriteMask = (byte)D3D11Formats.VdToD3D11ColorWriteEnable(state.ColorWriteMask.GetOrDefault());
                d3dBlendStateDesc.RenderTarget[i].SrcBlend = D3D11Formats.VdToD3D11Blend(state.SourceColorFactor);
                d3dBlendStateDesc.RenderTarget[i].DestBlend = D3D11Formats.VdToD3D11Blend(state.DestinationColorFactor);
                d3dBlendStateDesc.RenderTarget[i].BlendOp = D3D11Formats.VdToD3D11BlendOperation(state.ColorFunction);
                d3dBlendStateDesc.RenderTarget[i].SrcBlendAlpha = D3D11Formats.VdToD3D11Blend(state.SourceAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].DestBlendAlpha = D3D11Formats.VdToD3D11Blend(state.DestinationAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].BlendOpAlpha = D3D11Formats.VdToD3D11BlendOperation(state.AlphaFunction);
            }

            d3dBlendStateDesc.AlphaToCoverageEnable = description.AlphaToCoverageEnabled;
            d3dBlendStateDesc.IndependentBlendEnable = true;

            ComPtr<ID3D11BlendState> result = null;
            SilkMarshal.ThrowHResult(device.CreateBlendState(d3dBlendStateDesc, ref result));

            return result;
        }

        private ComPtr<ID3D11DepthStencilState> getDepthStencilState(ref DepthStencilStateDescription description)
        {
            Debug.Assert(Monitor.IsEntered(@lock));

            if (!depthStencilStates.TryGetValue(description, out var dss))
            {
                dss = createNewDepthStencilState(ref description);
                var key = description;
                depthStencilStates.Add(key, dss);
            }

            return dss;
        }

        private ComPtr<ID3D11DepthStencilState> createNewDepthStencilState(ref DepthStencilStateDescription description)
        {
            ComPtr<ID3D11DepthStencilState> result = null;

            SilkMarshal.ThrowHResult(device.CreateDepthStencilState(new DepthStencilDesc
            {
                DepthFunc = D3D11Formats.VdToD3D11ComparisonFunc(description.DepthComparison),
                DepthEnable = description.DepthTestEnabled,
                DepthWriteMask = description.DepthWriteEnabled ? DepthWriteMask.All : DepthWriteMask.Zero,
                StencilEnable = description.StencilTestEnabled,
                FrontFace = toD3D11StencilOpDesc(description.StencilFront),
                BackFace = toD3D11StencilOpDesc(description.StencilBack),
                StencilReadMask = description.StencilReadMask,
                StencilWriteMask = description.StencilWriteMask
            }, ref result));

            return result;
        }

        private DepthStencilopDesc toD3D11StencilOpDesc(StencilBehaviorDescription sbd)
        {
            return new DepthStencilopDesc
            {
                StencilFunc = D3D11Formats.VdToD3D11ComparisonFunc(sbd.Comparison),
                StencilPassOp = D3D11Formats.VdToD3D11StencilOperation(sbd.Pass),
                StencilFailOp = D3D11Formats.VdToD3D11StencilOperation(sbd.Fail),
                StencilDepthFailOp = D3D11Formats.VdToD3D11StencilOperation(sbd.DepthFail)
            };
        }

        private ComPtr<ID3D11RasterizerState> getRasterizerState(ref RasterizerStateDescription description, bool multisample)
        {
            Debug.Assert(Monitor.IsEntered(@lock));
            var key = new D3D11RasterizerStateCacheKey(description, multisample);

            if (!rasterizerStates.TryGetValue(key, out var rasterizerState))
            {
                rasterizerState = createNewRasterizerState(ref key);
                rasterizerStates.Add(key, rasterizerState);
            }

            return rasterizerState;
        }

        private ComPtr<ID3D11RasterizerState> createNewRasterizerState(ref D3D11RasterizerStateCacheKey key)
        {
            ComPtr<ID3D11RasterizerState> result = null;

            SilkMarshal.ThrowHResult(device.CreateRasterizerState(new RasterizerDesc
            {
                CullMode = D3D11Formats.VdToD3D11CullMode(key.VeldridDescription.CullMode),
                FillMode = D3D11Formats.VdToD3D11FillMode(key.VeldridDescription.FillMode),
                DepthClipEnable = key.VeldridDescription.DepthClipEnabled,
                ScissorEnable = key.VeldridDescription.ScissorTestEnabled,
                FrontCounterClockwise = key.VeldridDescription.FrontFace == FrontFace.CounterClockwise,
                MultisampleEnable = key.Multisampled
            }, ref result));

            return result;
        }

        private ComPtr<ID3D11InputLayout> getInputLayout(VertexLayoutDescription[] vertexLayouts, byte[] vsBytecode)
        {
            Debug.Assert(Monitor.IsEntered(@lock));

            if (vsBytecode == null || vertexLayouts == null || vertexLayouts.Length == 0)
                return null;

            var tempKey = InputLayoutCacheKey.CreateTempKey(vertexLayouts);

            if (!inputLayouts.TryGetValue(tempKey, out var inputLayout))
            {
                inputLayout = createNewInputLayout(vertexLayouts, vsBytecode);
                var permanentKey = InputLayoutCacheKey.CreatePermanentKey(vertexLayouts);
                inputLayouts.Add(permanentKey, inputLayout);
            }

            return inputLayout;
        }

        private unsafe ComPtr<ID3D11InputLayout> createNewInputLayout(VertexLayoutDescription[] vertexLayouts, byte[] vsBytecode)
        {
            int totalCount = 0;
            for (int i = 0; i < vertexLayouts.Length; i++) totalCount += vertexLayouts[i].Elements.Length;

            int element = 0; // Total element index across slots.
            var elements = new InputElementDesc[totalCount];
            var si = new SemanticIndices();

            for (int slot = 0; slot < vertexLayouts.Length; slot++)
            {
                var elementDescs = vertexLayouts[slot].Elements;
                uint stepRate = vertexLayouts[slot].InstanceStepRate;
                uint currentOffset = 0;

                for (int i = 0; i < elementDescs.Length; i++)
                {
                    var desc = elementDescs[i];

                    elements[element] = new InputElementDesc
                    {
                        SemanticName = (byte*)Marshal.StringToHGlobalAuto(getSemanticString(desc.Semantic)).ToPointer(),
                        SemanticIndex = (uint)SemanticIndices.GetAndIncrement(ref si, desc.Semantic),
                        AlignedByteOffset = desc.Offset != 0 ? desc.Offset : currentOffset,
                        Format = D3D11Formats.ToDxgiFormat(desc.Format),
                        InputSlot = (uint)slot,
                        InputSlotClass = stepRate == 0 ? InputClassification.PerVertexData : InputClassification.PerInstanceData,
                        InstanceDataStepRate = stepRate
                    };

                    currentOffset += FormatSizeHelpers.GetSizeInBytes(desc.Format);
                    element += 1;
                }
            }

            ComPtr<ID3D11InputLayout> result = default;
            SilkMarshal.ThrowHResult(device.CreateInputLayout(in elements[0], (uint)elements.Length, in vsBytecode[0], (UIntPtr)vsBytecode.Length, ref result));

            return result;
        }

        private string getSemanticString(VertexElementSemantic semantic)
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

        private struct SemanticIndices
        {
            private int position;
            private int texCoord;
            private int normal;
            private int color;

            public static int GetAndIncrement(ref SemanticIndices si, VertexElementSemantic type)
            {
                switch (type)
                {
                    case VertexElementSemantic.Position:
                        return si.position++;

                    case VertexElementSemantic.TextureCoordinate:
                        return si.texCoord++;

                    case VertexElementSemantic.Normal:
                        return si.normal++;

                    case VertexElementSemantic.Color:
                        return si.color++;

                    default:
                        throw Illegal.Value<VertexElementSemantic>();
                }
            }
        }

        private struct InputLayoutCacheKey : IEquatable<InputLayoutCacheKey>
        {
            public VertexLayoutDescription[] VertexLayouts;

            public static InputLayoutCacheKey CreateTempKey(VertexLayoutDescription[] original)
            {
                return new InputLayoutCacheKey { VertexLayouts = original };
            }

            public static InputLayoutCacheKey CreatePermanentKey(VertexLayoutDescription[] original)
            {
                var vertexLayouts = new VertexLayoutDescription[original.Length];

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
            public readonly bool Multisampled;

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

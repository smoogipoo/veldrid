using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11Framebuffer : Framebuffer
    {
        public ComPtr<ID3D11RenderTargetView>[] RenderTargetViews { get; }
        public ComPtr<ID3D11DepthStencilView> DepthStencilView => depthStencilView;

        public override bool IsDisposed => isDisposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // for (int i = 0; i < RenderTargetViews.Length; i++) RenderTargetViews[i].DebugName = value + "_RTV" + i;
                //
                // if (DepthStencilView != null) DepthStencilView.DebugName = value + "_DSV";
            }
        }

        // Only non-null if this is the Framebuffer for a Swapchain.
        internal D3D11Swapchain Swapchain { get; set; }

        private readonly ComPtr<ID3D11DepthStencilView> depthStencilView;

        private string name;
        private bool isDisposed;

        public D3D11Framebuffer(ComPtr<ID3D11Device> device, ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
            if (description.DepthTarget != null)
            {
                var d3dDepthTarget = Util.AssertSubtype<Texture, D3D11Texture>(description.DepthTarget.Value.Target);
                var dsvDesc = new DepthStencilViewDesc
                {
                    Format = D3D11Formats.GetDepthFormat(d3dDepthTarget.Format)
                };

                if (d3dDepthTarget.ArrayLayers == 1)
                {
                    if (d3dDepthTarget.SampleCount == TextureSampleCount.Count1)
                    {
                        dsvDesc.ViewDimension = DsvDimension.Texture2D;
                        dsvDesc.Texture2D.MipSlice = description.DepthTarget.Value.MipLevel;
                    }
                    else
                        dsvDesc.ViewDimension = DsvDimension.Texture2Dms;
                }
                else
                {
                    if (d3dDepthTarget.SampleCount == TextureSampleCount.Count1)
                    {
                        dsvDesc.ViewDimension = DsvDimension.Texture2Darray;
                        dsvDesc.Texture2DArray.FirstArraySlice = description.DepthTarget.Value.ArrayLayer;
                        dsvDesc.Texture2DArray.ArraySize = 1;
                        dsvDesc.Texture2DArray.MipSlice = description.DepthTarget.Value.MipLevel;
                    }
                    else
                    {
                        dsvDesc.ViewDimension = DsvDimension.Texture2Dmsarray;
                        dsvDesc.Texture2DMSArray.FirstArraySlice = description.DepthTarget.Value.ArrayLayer;
                        dsvDesc.Texture2DMSArray.ArraySize = 1;
                    }
                }

                SilkMarshal.ThrowHResult(device.CreateDepthStencilView(d3dDepthTarget.DeviceTexture, dsvDesc, ref depthStencilView));
            }

            if (description.ColorTargets != null && description.ColorTargets.Length > 0)
            {
                RenderTargetViews = new ComPtr<ID3D11RenderTargetView>[description.ColorTargets.Length];

                for (int i = 0; i < RenderTargetViews.Length; i++)
                {
                    var d3dColorTarget = Util.AssertSubtype<Texture, D3D11Texture>(description.ColorTargets[i].Target);
                    var rtvDesc = new RenderTargetViewDesc
                    {
                        Format = D3D11Formats.ToDxgiFormat(d3dColorTarget.Format, false)
                    };

                    if (d3dColorTarget.ArrayLayers > 1 || (d3dColorTarget.Usage & TextureUsage.Cubemap) != 0)
                    {
                        if (d3dColorTarget.SampleCount == TextureSampleCount.Count1)
                        {
                            rtvDesc.ViewDimension = RtvDimension.Texture2Darray;
                            rtvDesc.Texture2DArray = new Tex2DArrayRtv
                            {
                                ArraySize = 1,
                                FirstArraySlice = description.ColorTargets[i].ArrayLayer,
                                MipSlice = description.ColorTargets[i].MipLevel
                            };
                        }
                        else
                        {
                            rtvDesc.ViewDimension = RtvDimension.Texture2Dmsarray;
                            rtvDesc.Texture2DMSArray = new Tex2DmsArrayRtv
                            {
                                ArraySize = 1,
                                FirstArraySlice = description.ColorTargets[i].ArrayLayer
                            };
                        }
                    }
                    else
                    {
                        if (d3dColorTarget.SampleCount == TextureSampleCount.Count1)
                        {
                            rtvDesc.ViewDimension = RtvDimension.Texture2D;
                            rtvDesc.Texture2D.MipSlice = description.ColorTargets[i].MipLevel;
                        }
                        else
                            rtvDesc.ViewDimension = RtvDimension.Texture2Dms;
                    }

                    SilkMarshal.ThrowHResult(device.CreateRenderTargetView(d3dColorTarget.DeviceTexture, rtvDesc, ref RenderTargetViews[i]));
                }
            }
            else
                RenderTargetViews = Array.Empty<ComPtr<ID3D11RenderTargetView>>();
        }

        #region Disposal

        public override void Dispose()
        {
            if (isDisposed)
                return;

            DepthStencilView.Release();

            foreach (var rtv in RenderTargetViews)
                rtv.Release();

            isDisposed = true;
        }

        #endregion
    }
}

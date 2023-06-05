// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal class D3D12Framebuffer : Framebuffer
    {
        public ID3D12DescriptorHeap DepthStencilHeap { get; }
        public ID3D12DescriptorHeap RenderTargetHeap { get; }

        // Only non-null if this is the Framebuffer for a Swapchain.
        internal D3D12Swapchain Swapchain { get; set; }

        public D3D12Framebuffer(ID3D12Device device, ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
            if (description.DepthTarget != null)
            {
                D3D12Texture d3dDepthTarget = Util.AssertSubtype<Texture, D3D12Texture>(description.DepthTarget.Value.Target);
                DepthStencilViewDescription dsvDesc = new DepthStencilViewDescription { Format = D3D12Formats.GetDepthFormat(d3dDepthTarget.Format) };

                if (d3dDepthTarget.ArrayLayers == 1)
                {
                    if (d3dDepthTarget.SampleCount == TextureSampleCount.Count1)
                    {
                        dsvDesc.ViewDimension = DepthStencilViewDimension.Texture2D;
                        dsvDesc.Texture2D.MipSlice = (int)description.DepthTarget.Value.MipLevel;
                    }
                    else
                    {
                        dsvDesc.ViewDimension = DepthStencilViewDimension.Texture2DMultisampled;
                    }
                }
                else
                {
                    if (d3dDepthTarget.SampleCount == TextureSampleCount.Count1)
                    {
                        dsvDesc.ViewDimension = DepthStencilViewDimension.Texture2DArray;
                        dsvDesc.Texture2DArray.FirstArraySlice = (int)description.DepthTarget.Value.ArrayLayer;
                        dsvDesc.Texture2DArray.ArraySize = 1;
                        dsvDesc.Texture2DArray.MipSlice = (int)description.DepthTarget.Value.MipLevel;
                    }
                    else
                    {
                        dsvDesc.ViewDimension = DepthStencilViewDimension.Texture2DMultisampledArray;
                        dsvDesc.Texture2DMSArray.FirstArraySlice = (int)description.DepthTarget.Value.ArrayLayer;
                        dsvDesc.Texture2DMSArray.ArraySize = 1;
                    }
                }

                DepthStencilHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.DepthStencilView, 1));
                device.CreateDepthStencilView(d3dDepthTarget.DeviceResource, dsvDesc, DepthStencilHeap.GetCPUDescriptorHandleForHeapStart());
            }

            if (description.ColorTargets != null && description.ColorTargets.Length > 0)
            {
                RenderTargetHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, description.ColorTargets.Length));

                CpuDescriptorHandle rtvHandle = RenderTargetHeap.GetCPUDescriptorHandleForHeapStart();
                int rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

                for (int i = 0; i < description.ColorTargets.Length; i++)
                {
                    D3D12Texture d3dColorTarget = Util.AssertSubtype<Texture, D3D12Texture>(description.ColorTargets[i].Target);
                    RenderTargetViewDescription rtvDesc = new RenderTargetViewDescription { Format = D3D12Formats.ToDxgiFormat(d3dColorTarget.Format, false) };

                    if (d3dColorTarget.ArrayLayers > 1 || (d3dColorTarget.Usage & TextureUsage.Cubemap) != 0)
                    {
                        if (d3dColorTarget.SampleCount == TextureSampleCount.Count1)
                        {
                            rtvDesc.ViewDimension = RenderTargetViewDimension.Texture2DArray;
                            rtvDesc.Texture2DArray = new Texture2DArrayRenderTargetView
                            {
                                ArraySize = 1, FirstArraySlice = (int)description.ColorTargets[i].ArrayLayer, MipSlice = (int)description.ColorTargets[i].MipLevel
                            };
                        }
                        else
                        {
                            rtvDesc.ViewDimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                            rtvDesc.Texture2DMSArray = new Texture2DMultisampledArrayRenderTargetView { ArraySize = 1, FirstArraySlice = (int)description.ColorTargets[i].ArrayLayer };
                        }
                    }
                    else
                    {
                        if (d3dColorTarget.SampleCount == TextureSampleCount.Count1)
                        {
                            rtvDesc.ViewDimension = RenderTargetViewDimension.Texture2D;
                            rtvDesc.Texture2D.MipSlice = (int)description.ColorTargets[i].MipLevel;
                        }
                        else
                        {
                            rtvDesc.ViewDimension = RenderTargetViewDimension.Texture2DMultisampled;
                        }
                    }

                    device.CreateRenderTargetView(d3dColorTarget.DeviceResource, rtvDesc, rtvHandle);
                    rtvHandle += rtvDescriptorSize;
                }
            }
        }

        public override string Name { get; set; }
        public override bool IsDisposed { get; }

        public override void Dispose()
        {
        }
    }
}

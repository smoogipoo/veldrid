using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Veldrid.D3D11
{
    internal class D3D11TextureView : TextureView
    {
        public ComPtr<ID3D11ShaderResourceView> ShaderResourceView => shaderResourceView;
        public ComPtr<ID3D11UnorderedAccessView> UnorderedAccessView => unorderedAccessView;

        public override bool IsDisposed => isDisposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                // if (ShaderResourceView != null) ShaderResourceView.DebugName = value + "_SRV";
                //
                // if (UnorderedAccessView != null) UnorderedAccessView.DebugName = value + "_UAV";
            }
        }

        private readonly ComPtr<ID3D11ShaderResourceView> shaderResourceView;
        private readonly ComPtr<ID3D11UnorderedAccessView> unorderedAccessView;

        private string name;
        private bool isDisposed;

        public D3D11TextureView(D3D11GraphicsDevice gd, ref TextureViewDescription description)
            : base(ref description)
        {
            var device = gd.Device;
            var d3dTex = Util.AssertSubtype<Texture, D3D11Texture>(description.Target);

            SilkMarshal.ThrowHResult(device.CreateShaderResourceView(
                d3dTex.DeviceTexture,
                D3D11Util.GetSrvDesc(
                    d3dTex,
                    description.BaseMipLevel,
                    description.MipLevels,
                    description.BaseArrayLayer,
                    description.ArrayLayers,
                    Format),
                ref shaderResourceView
            ));

            if ((d3dTex.Usage & TextureUsage.Storage) == TextureUsage.Storage)
            {
                var uavDesc = new UnorderedAccessViewDesc
                {
                    Format = D3D11Formats.GetViewFormat(d3dTex.DxgiFormat)
                };

                if ((d3dTex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
                    throw new NotSupportedException();

                if (d3dTex.Depth == 1)
                {
                    if (d3dTex.ArrayLayers == 1)
                    {
                        if (d3dTex.Type == TextureType.Texture1D)
                        {
                            uavDesc.ViewDimension = UavDimension.Texture1D;
                            uavDesc.Texture1D.MipSlice = description.BaseMipLevel;
                        }
                        else
                        {
                            uavDesc.ViewDimension = UavDimension.Texture2D;
                            uavDesc.Texture2D.MipSlice = description.BaseMipLevel;
                        }
                    }
                    else
                    {
                        if (d3dTex.Type == TextureType.Texture1D)
                        {
                            uavDesc.ViewDimension = UavDimension.Texture1Darray;
                            uavDesc.Texture1DArray.MipSlice = description.BaseMipLevel;
                            uavDesc.Texture1DArray.FirstArraySlice = description.BaseArrayLayer;
                            uavDesc.Texture1DArray.ArraySize = description.ArrayLayers;
                        }
                        else
                        {
                            uavDesc.ViewDimension = UavDimension.Texture2Darray;
                            uavDesc.Texture2DArray.MipSlice = description.BaseMipLevel;
                            uavDesc.Texture2DArray.FirstArraySlice = description.BaseArrayLayer;
                            uavDesc.Texture2DArray.ArraySize = description.ArrayLayers;
                        }
                    }
                }
                else
                {
                    uavDesc.ViewDimension = UavDimension.Texture3D;
                    uavDesc.Texture3D.MipSlice = description.BaseMipLevel;

                    // Map the entire range of the 3D texture.
                    uavDesc.Texture3D.FirstWSlice = 0;
                    uavDesc.Texture3D.WSize = d3dTex.Depth;
                }

                SilkMarshal.ThrowHResult(device.CreateUnorderedAccessView(d3dTex.DeviceTexture, uavDesc, ref unorderedAccessView));
            }
        }

        #region Disposal

        public override void Dispose()
        {
            if (isDisposed)
                return;

            ShaderResourceView.Release();
            UnorderedAccessView.Release();

            isDisposed = true;
        }

        #endregion
    }
}

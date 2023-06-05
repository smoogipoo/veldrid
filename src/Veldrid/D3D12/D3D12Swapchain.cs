// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SharpGen.Runtime;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrid.D3D12
{
    internal class D3D12Swapchain : Swapchain
    {
        public override Framebuffer Framebuffer => _framebuffer;

        private readonly D3D12GraphicsDevice _gd;
        private readonly SwapchainDescription _description;
        private readonly PixelFormat? _depthFormat;
        private readonly Format _colorFormat;

        private IDXGISwapChain3 _dxgiSwapChain;
        private Texture _depthTexture;
        private readonly Texture[] _backBufferTextures = new Texture[D3D12GraphicsDevice.BUFFER_COUNT];
        private Framebuffer _framebuffer;

        private uint _width;
        private uint _height;
        private float _pixelScale = 1f;

        public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description)
        {
            _gd = gd;
            _description = description;
            _description = description;
            _depthFormat = description.DepthFormat;
            SyncToVerticalBlank = description.SyncToVerticalBlank;

            _colorFormat = description.ColorSrgb
                ? Format.B8G8R8A8_UNorm_SRgb
                : Format.B8G8R8A8_UNorm;

            _width = description.Width;
            _height = description.Height;

            recreateSwapchain();
        }

        private void recreateSwapchain()
        {
            _dxgiSwapChain?.Release();
            _dxgiSwapChain?.Dispose();
            _dxgiSwapChain = null;

            _framebuffer?.Dispose();
            _framebuffer = null;

            _depthTexture?.Dispose();
            _depthTexture = null;

            if (_description.Source is Win32SwapchainSource win32Source)
            {
                SwapChainDescription1 dxgiSCDesc = new SwapChainDescription1
                {
                    AlphaMode = AlphaMode.Ignore,
                    BufferCount = D3D12GraphicsDevice.BUFFER_COUNT,
                    Format = _colorFormat,
                    Width = (int)_width,
                    Height = (int)_height,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipDiscard,
                    BufferUsage = Usage.RenderTargetOutput
                };

                using (IDXGISwapChain1 swapChain = _gd.DXGIFactory.CreateSwapChainForHwnd(_gd.GraphicsQueue, win32Source.Hwnd, dxgiSCDesc))
                {
                    _gd.DXGIFactory.MakeWindowAssociation(win32Source.Hwnd, WindowAssociationFlags.IgnoreAltEnter);
                    _dxgiSwapChain = swapChain.QueryInterface<IDXGISwapChain3>();
                }
            }
            else if (_description.Source is UwpSwapchainSource uwpSource)
            {
                _pixelScale = uwpSource.LogicalDpi / 96.0f;

                // Properties of the swap chain
                SwapChainDescription1 swapChainDescription = new SwapChainDescription1
                {
                    AlphaMode = AlphaMode.Ignore,
                    BufferCount = D3D12GraphicsDevice.BUFFER_COUNT,
                    Format = _colorFormat,
                    Height = (int)(_height * _pixelScale),
                    Width = (int)(_width * _pixelScale),
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipSequential,
                    BufferUsage = Usage.RenderTargetOutput
                };

                using (IDXGISwapChain1 swapChain = _gd.DXGIFactory.CreateSwapChainForComposition(_gd.GraphicsQueue, swapChainDescription))
                {
                    _dxgiSwapChain = swapChain.QueryInterface<IDXGISwapChain3>();
                }

                ComObject co = new ComObject(uwpSource.SwapChainPanelNative);

                ISwapChainPanelNative swapchainPanelNative = co.QueryInterfaceOrNull<ISwapChainPanelNative>();
                if (swapchainPanelNative != null)
                {
                    swapchainPanelNative.SetSwapChain(_dxgiSwapChain);
                }
                else
                {
                    ISwapChainBackgroundPanelNative bgPanelNative = co.QueryInterfaceOrNull<ISwapChainBackgroundPanelNative>();
                    if (bgPanelNative != null)
                    {
                        bgPanelNative.SetSwapChain(_dxgiSwapChain);
                    }
                }
            }

            Resize(_width, _height);
        }

        public override void Resize(uint width, uint height)
        {
            _width = width;
            _height = height;

            bool resizeBuffers = false;

            if (_framebuffer != null)
            {
                resizeBuffers = true;

                _depthTexture?.Dispose();

                foreach (Texture tex in _backBufferTextures)
                    tex.Dispose();

                _framebuffer.Dispose();
            }

            uint actualWidth = (uint)(width * _pixelScale);
            uint actualHeight = (uint)(height * _pixelScale);

            if (resizeBuffers)
            {
                _dxgiSwapChain.ResizeBuffers(2, (int)actualWidth, (int)actualHeight, _colorFormat).CheckError();
            }

            if (_depthFormat != null)
            {
                TextureDescription depthDesc = new TextureDescription(
                    actualWidth, actualHeight, 1, 1, 1,
                    _depthFormat.Value,
                    TextureUsage.DepthStencil,
                    TextureType.Texture2D);
                _depthTexture = new D3D12Texture(_gd.Device, ref depthDesc);
            }

            for (int i = 0; i < D3D12GraphicsDevice.BUFFER_COUNT; i++)
            {
                ID3D12Resource res = _dxgiSwapChain.GetBuffer<ID3D12Resource>(i);
                _backBufferTextures[i] = new D3D12Texture(
                    res,
                    TextureType.Texture2D,
                    D3D12Formats.ToVdFormat(_colorFormat));
            }

            FramebufferDescription desc = new FramebufferDescription(_depthTexture, _backBufferTextures);
            _framebuffer = new D3D12Framebuffer(_gd.Device, ref desc)
            {
                Swapchain = this
            };
        }

        public override bool SyncToVerticalBlank { get; set; }
        public override string Name { get; set; }
        public override bool IsDisposed { get; }

        public override void Dispose()
        {
        }
    }
}

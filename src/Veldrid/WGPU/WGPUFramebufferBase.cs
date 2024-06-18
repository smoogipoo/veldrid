// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Veldrid.WGPU
{
    internal abstract unsafe class WGPUFramebufferBase : Framebuffer
    {
        public override bool IsDisposed { get; }

        private TextureView[] colourTextureViews = Array.Empty<TextureView>();
        private TextureView depthTextureView;

        private readonly WGPUGraphicsDevice gd;

        private bool isDisposed;

        protected WGPUFramebufferBase(WGPUGraphicsDevice gd, ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
            this.gd = gd;
        }

        protected WGPUFramebufferBase(WGPUGraphicsDevice gd)
        {
            this.gd = gd;
        }

        public TextureView GetColourAttachmentTextureView(int index)
        {
            Util.EnsureArrayMinimumSize(ref colourTextureViews, (uint)ColorTargets.Count);

            // This is a special case for the swapchain framebuffer, where the colour target changes on a resize.
            if (colourTextureViews[index]?.Target != ColorTargets[index].Target)
                colourTextureViews[index] = null;

            return colourTextureViews[index] ??= createTextureView(ColorTargets[index]);
        }

        public TextureView GetDepthAttachmentTextureView()
        {
            // This is a special case for the swapchain framebuffer, where the depth target changes on a resize.
            if (depthTextureView?.Target != DepthTarget!.Value.Target)
                depthTextureView = null;

            return depthTextureView ??= createTextureView(DepthTarget!.Value);
        }

        private TextureView createTextureView(FramebufferAttachment attachment)
            => gd.ResourceFactory.CreateTextureView(new TextureViewDescription(attachment.Target, attachment.Target.Format, attachment.MipLevel, 1, attachment.ArrayLayer, 1));

        public override void Dispose()
        {
            if (isDisposed)
                return;

            for (int i = 0; i < colourTextureViews.Length; i++)
                colourTextureViews[i]?.Dispose();

            depthTextureView?.Dispose();

            isDisposed = true;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MtlSwapchain : Swapchain
    {
        public override Framebuffer Framebuffer => framebuffer;

        public override bool IsDisposed => disposed;

        public CAMetalDrawable CurrentDrawable => currentDrawable.Drawable;

        public double CurrentTargetTimestamp => currentDrawable.TargetTimestamp;

        public override bool SyncToVerticalBlank
        {
            get => syncToVerticalBlank;
            set
            {
                if (syncToVerticalBlank != value)
                    setSyncToVerticalBlank(value);
            }
        }

        private readonly ConcurrentQueue<DrawableUsage> pendingDrawables = new ConcurrentQueue<DrawableUsage>();
        private readonly SemaphoreSlim nextDrawableReady = new SemaphoreSlim(0);

        public override string Name { get; set; }
        private readonly MtlSwapchainFramebuffer framebuffer;
        private readonly MtlGraphicsDevice gd;

        private MtlcaDisplayLink displayLink;

        private DrawableUsage currentDrawable;
        private CAMetalLayer metalLayer;
        private UIView uiView; // Valid only when a UIViewSwapchainSource is used.
        private bool syncToVerticalBlank;
        private bool disposed;

        public MtlSwapchain(MtlGraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;
            syncToVerticalBlank = description.SyncToVerticalBlank;

            uint width;
            uint height;

            var source = description.Source;

            if (source is NSWindowSwapchainSource nsWindowSource)
            {
                var nswindow = new NSWindow(nsWindowSource.NSWindow);
                var contentView = nswindow.contentView;
                var windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }
            }
            else if (source is NSViewSwapchainSource nsViewSource)
            {
                var contentView = new NSView(nsViewSource.NSView);
                var windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = metalLayer.NativePtr;
                }
            }
            else if (source is UIViewSwapchainSource uiViewSource)
            {
                uiView = new UIView(uiViewSource.UIView);
                var viewSize = uiView.frame.size;
                width = (uint)viewSize.width;
                height = (uint)viewSize.height;

                if (!CAMetalLayer.TryCast(uiView.layer, out metalLayer))
                {
                    metalLayer = CAMetalLayer.New();
                    metalLayer.frame = uiView.frame;
                    metalLayer.opaque = true;
                    uiView.layer.addSublayer(metalLayer.NativePtr);
                }
            }
            else
                throw new VeldridException("A Metal Swapchain can only be created from an NSWindow, NSView, or UIView.");

            var format = description.ColorSrgb
                ? PixelFormat.B8G8R8A8UNormSRgb
                : PixelFormat.B8G8R8A8UNorm;

            metalLayer.maximumDrawableCount = 2;
            metalLayer.device = this.gd.Device;
            metalLayer.pixelFormat = MtlFormats.VdToMtlPixelFormat(format, false);
            metalLayer.framebufferOnly = true;
            metalLayer.drawableSize = new CGSize(width, height);

            framebuffer = new MtlSwapchainFramebuffer(gd, this, description.DepthFormat, format);

            setSyncToVerticalBlank(syncToVerticalBlank);
        }

        private void onDisplayLinkCallback(CAMetalDisplayLink link, CAMetalDisplayLinkUpdate update)
        {
            pendingDrawables.Enqueue(new DrawableUsage(update.drawable, update.targetTimestamp));
            nextDrawableReady.Release();
        }

        #region Disposal

        public override void Dispose()
        {
            framebuffer.Dispose();
            displayLink?.Dispose();

            ObjectiveCRuntime.release(metalLayer.NativePtr);

            disposed = true;
        }

        #endregion

        public override void Resize(uint width, uint height)
        {
            if (uiView.NativePtr != IntPtr.Zero)
                metalLayer.frame = uiView.frame;

            metalLayer.drawableSize = new CGSize(width, height);
        }

        public bool EnsureDrawableAvailable()
        {
            if (!CurrentDrawable.IsNull)
                return true;

            if (displayLink == null)
            {
                using (NSAutoreleasePool.Begin())
                {
                    var drawable = metalLayer.nextDrawable();
                    if (drawable.IsNull)
                        return false;

                    currentDrawable = new DrawableUsage(drawable, 0);
                    framebuffer.UpdateTextures(CurrentDrawable, metalLayer.drawableSize);
                    return true;
                }
            }

            // Should never time out, but add a timeout just in case.
            if (!nextDrawableReady.Wait(TimeSpan.FromSeconds(1)))
                return false;

            if (pendingDrawables.TryDequeue(out var pending))
            {
                currentDrawable = pending;
                framebuffer.UpdateTextures(CurrentDrawable, metalLayer.drawableSize);
                return true;
            }

            return false;
        }

        public void InvalidateDrawable()
        {
            currentDrawable.Dispose();
            currentDrawable = default;
        }

        private void setSyncToVerticalBlank(bool value)
        {
            syncToVerticalBlank = value;

            if (gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
                || gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
                || gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1)
            {
                metalLayer.displaySyncEnabled = value;
            }

            if (value)
                displayLink = new MtlcaDisplayLink(metalLayer, onDisplayLinkCallback);
            else
            {
                displayLink.Dispose();
                displayLink = null;

                while (pendingDrawables.TryDequeue(out var pending))
                    pending.Dispose();
            }
        }

        private readonly struct DrawableUsage : IDisposable
        {
            public readonly CAMetalDrawable Drawable;
            public readonly double TargetTimestamp;

            public DrawableUsage(CAMetalDrawable drawable, double targetTimestamp)
            {
                Drawable = drawable;
                TargetTimestamp = targetTimestamp;

                ObjectiveCRuntime.retain(Drawable);
            }

            public void Dispose()
            {
                ObjectiveCRuntime.release(Drawable);
            }
        }
    }
}

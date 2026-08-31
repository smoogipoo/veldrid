using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MtlSwapchain : Swapchain
    {
        public override Framebuffer Framebuffer => framebuffer;

        public override bool IsDisposed => disposed;

        public CAMetalDrawable CurrentDrawable
        {
            get
            {
                if (drawableQueue.TryPeek(out var item))
                    return item.drawable;

                return default;
            }
        }

        public double CurrentPresentationTime
        {
            get
            {
                if (drawableQueue.TryPeek(out var item))
                    return item.timestamp;

                return 0;
            }
        }

        public override bool SyncToVerticalBlank
        {
            get => syncToVerticalBlank;
            set
            {
                if (syncToVerticalBlank != value) setSyncToVerticalBlank(value);
            }
        }

        private readonly ConcurrentQueue<(CAMetalDrawable drawable, double timestamp)> drawableQueue = new ConcurrentQueue<(CAMetalDrawable, double)>();

        public override string Name { get; set; }
        private readonly MtlSwapchainFramebuffer framebuffer;
        private readonly MtlGraphicsDevice gd;
        private MtlcaDisplayLink displayLink;
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

            setSyncToVerticalBlank(syncToVerticalBlank);

            framebuffer = new MtlSwapchainFramebuffer(
                gd,
                this,
                description.DepthFormat,
                format);

            MtlcaDisplayLink.Callback += onDisplayLinkCallback;

            displayLink = new MtlcaDisplayLink(metalLayer);
            displayLink.Paused = false;
        }

        private Stopwatch stopwatch = new Stopwatch();

        private void onDisplayLinkCallback(CAMetalDisplayLink link, CAMetalDisplayLinkUpdate update)
        {
            Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);
            stopwatch.Restart();

            ObjectiveCRuntime.retain(update.drawable);
            drawableQueue.Enqueue((update.drawable, update.targetPresentationTimestamp));
        }

        #region Disposal

        public override void Dispose()
        {
            framebuffer.Dispose();
            displayLink.Dispose();

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
            if (drawableQueue.IsEmpty)
                return false;

            if (drawableQueue.TryPeek(out var item))
            {
                framebuffer.UpdateTextures(item.drawable, metalLayer.drawableSize);
                return true;
            }

            return false;
        }

        public void InvalidateDrawable()
        {
            if (drawableQueue.TryDequeue(out var item))
                ObjectiveCRuntime.release(item.drawable);
        }

        private void setSyncToVerticalBlank(bool value)
        {
            syncToVerticalBlank = value;

            if (gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
                || gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
                || gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1)
                metalLayer.displaySyncEnabled = value;
        }
    }
}

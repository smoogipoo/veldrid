using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MtlSwapchain : Swapchain
    {
        public override Framebuffer Framebuffer => framebuffer;

        public override bool IsDisposed => disposed;

        public CAMetalDrawable CurrentDrawable => currentDrawable.Drawable;

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

        private readonly Stopwatch frameStopwatch = new Stopwatch();

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

                    frameStopwatch.Restart();
                    return true;
                }
            }

            nextDrawableReady.Wait(TimeSpan.FromSeconds(1)); // Should never time out.

            if (pendingDrawables.TryDequeue(out var pending))
            {
                pending.Sleep(frameStopwatch.Elapsed);

                currentDrawable = pending;
                framebuffer.UpdateTextures(CurrentDrawable, metalLayer.drawableSize);

                frameStopwatch.Restart();
                return true;
            }

            return false;
        }

        public void InvalidateDrawable()
        {
            frameStopwatch.Stop();

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
            }
        }

        private readonly struct DrawableUsage : IDisposable
        {
            public readonly CAMetalDrawable Drawable;
            private readonly double targetTimestamp;

            public DrawableUsage(CAMetalDrawable drawable, double targetTimestamp)
            {
                Drawable = drawable;
                this.targetTimestamp = targetTimestamp;

                ObjectiveCRuntime.retain(Drawable);
            }

            public void Sleep(TimeSpan lastFrameTime)
            {
                double currentTime = CoreAnimation.CurrentMediaTime();

                // The amount of time that we are given to render the current frame. If we take too long, we'll fall into the next Vsync interval.
                TimeSpan timeToRender = TimeSpan.FromSeconds(targetTimestamp - currentTime);

                // To get as up-to-date input as possible, we'll delay drawing the current frame until as close to the Vsync interval as possible.
                // A simple heuristic is to assume that frame times are generally static or ramping between frames.
                // But we definitely do not want to miss the Vsync interval, so we apply a little lenience.
                TimeSpan timeToWake = timeToRender - lastFrameTime - TimeSpan.FromMilliseconds(1);

                if (timeToWake > TimeSpan.Zero)
                {
                    LibSystem.mach_timebase_info(out LibSystem.mach_timebase_info_data_t tb);
                    ulong duration = (ulong)(timeToWake.TotalNanoseconds * tb.denom / tb.numer);
                    LibSystem.mach_wait_until(LibSystem.mach_absolute_time() + duration);
                }
            }

            public void Dispose()
            {
                ObjectiveCRuntime.release(Drawable);
            }
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MtlcaDisplayLink : IMtlDisplayLink
    {
        private readonly Action<CAMetalDisplayLink, CAMetalDisplayLinkUpdate> callback;
        private readonly CAMetalDisplayLink.RunLoopDelegateCallback runLoopCallbackHandler;

        private CAMetalDisplayLink displayLink;
        private CAMetalDisplayLink.RunLoopDelegate runLoopDelegate;

        public MtlcaDisplayLink(CAMetalLayer layer, Action<CAMetalDisplayLink, CAMetalDisplayLinkUpdate> callback)
        {
            this.callback = callback;

            runLoopCallbackHandler = onCallback;
            runLoopDelegate = CAMetalDisplayLink.RunLoopDelegate.Create(runLoopCallbackHandler);

            displayLink = CAMetalDisplayLink.Create(layer);
            displayLink.@delegate = runLoopDelegate;
            displayLink.preferredFrameLatency = 1.0f;
            displayLink.addToRunLoop(NSRunLoop.mainRunLoop(), CFRunLoopMode.CommonModes);
        }

        private void onCallback(IntPtr self, IntPtr cmd, IntPtr link, IntPtr update)
        {
            callback?.Invoke(new CAMetalDisplayLink(link), new CAMetalDisplayLinkUpdate(update));
        }

        public bool Paused
        {
            get => displayLink.paused;
            set => displayLink.paused = value;
        }

        public double GetActualOutputVideoRefreshPeriod()
        {
            return -1.0f;
        }

        public void UpdateActiveDisplay(int x, int y, int w, int h)
        {
        }

        public void Dispose()
        {
            displayLink.invalidate();
            runLoopDelegate.Dispose();
        }
    }
}

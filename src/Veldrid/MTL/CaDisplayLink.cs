// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class CaDisplayLink : IMtlDisplayLink
    {
        private readonly Action<CADisplayLink> callback;
        private readonly CADisplayLink.RunLoopDelegateCallback targetCallbackHandler;

        private CADisplayLink displayLink;
        private CADisplayLink.RunLoopDelegate target;

        public CaDisplayLink(Action<CADisplayLink> callback)
        {
            this.callback = callback;

            targetCallbackHandler = onCallback;
            target = CADisplayLink.RunLoopDelegate.Create(targetCallbackHandler);

            displayLink = CADisplayLink.Create(target, CADisplayLink.RunLoopDelegate.StepSelector);
            displayLink.addToRunLoop(NSRunLoop.mainRunLoop(), CFRunLoopMode.CommonModes);
        }

        private void onCallback(IntPtr self, IntPtr cmd, IntPtr link)
        {
            callback?.Invoke(new CADisplayLink(link));
        }

        public bool Paused
        {
            get => displayLink.paused;
            set => displayLink.paused = value;
        }

        public double GetActualOutputVideoRefreshPeriod()
        {
            return displayLink.duration;
        }

        public void UpdateActiveDisplay(int x, int y, int w, int h)
        {
        }

        public void Dispose()
        {
            displayLink.invalidate();
            target.Dispose();
        }
    }
}

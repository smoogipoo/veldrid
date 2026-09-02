// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal unsafe class MtlcaDisplayLink : IMtlDisplayLink
    {
        // Todo: This is only static because I'm investigating some TOTALLY F***ED debugger behaviour.
        public static event Action<CAMetalDisplayLink, CAMetalDisplayLinkUpdate> Callback;

        private CAMetalDisplayLink displayLink;
        private CAMetalDisplayLink.RunLoopDelegate runLoopDelegate;

        public MtlcaDisplayLink(CAMetalLayer layer)
        {
            runLoopDelegate = CAMetalDisplayLink.RunLoopDelegate.Create(&onCallback);

            displayLink = CAMetalDisplayLink.Create(layer);
            displayLink.@delegate = runLoopDelegate;
            displayLink.paused = true;
            displayLink.preferredFrameRateRange = CAFrameRateRange.Create(120, 120, 120);
            displayLink.preferredFrameLatency = 1.0f;
            displayLink.addToRunLoop(NSRunLoop.mainRunLoop(), CFRunLoopMode.CommonModes);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void onCallback(IntPtr self, IntPtr cmd, IntPtr link, IntPtr update)
        {
            Callback?.Invoke(new CAMetalDisplayLink(link), new CAMetalDisplayLinkUpdate(update));
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

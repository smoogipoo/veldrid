// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using static Veldrid.MetalBindings.ObjectiveCRuntime;

namespace Veldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CADisplayLink
    {
        private static readonly ObjCClass s_class = new ObjCClass("CADisplayLink");

        private static readonly Selector sel_displayLinkWithTargetSelector = "displayLinkWithTarget:selector:";
        private static readonly Selector sel_isPaused = "isPaused";
        private static readonly Selector sel_setPaused = "setPaused:";
        private static readonly Selector sel_timestamp = "timestamp";
        private static readonly Selector sel_targetTimestamp = "targetTimestamp";
        private static readonly Selector sel_duration = "duration";
        private static readonly Selector sel_preferredFrameRateRange = "preferredFrameRateRange";
        private static readonly Selector sel_setPreferredFrameRateRange = "setPreferredFrameRateRange:";
        private static readonly Selector sel_addToRunLoop = "addToRunLoop:forMode:";
        private static readonly Selector sel_removeFromRunLoop = "removeFromRunLoop:forMode:";
        private static readonly Selector sel_invalidate = "invalidate";

        public readonly IntPtr NativePtr;

        public CADisplayLink(IntPtr ptr) => NativePtr = ptr;
        public static implicit operator IntPtr(CADisplayLink c) => c.NativePtr;

        public static CADisplayLink Create(IntPtr target, Selector sel)
        {
            IntPtr ptr = IntPtr_objc_msgSend(s_class, sel_displayLinkWithTargetSelector, target, sel);
            return new CADisplayLink(ptr);
        }

        public bool paused
        {
            get => bool8_objc_msgSend(NativePtr, sel_isPaused);
            set => objc_msgSend(NativePtr, sel_setPaused, value);
        }

        public double timestamp => double_objc_msgSend(NativePtr, sel_timestamp);
        public double targetTimestamp => double_objc_msgSend(NativePtr, sel_targetTimestamp);
        public double duration => double_objc_msgSend(NativePtr, sel_duration);

        public CAFrameRateRange preferredFrameRateRange
        {
            get => CAFrameRateRange_objc_msgSend(NativePtr, sel_preferredFrameRateRange);
            set => objc_msgSend(NativePtr, sel_setPreferredFrameRateRange, value);
        }

        public void addToRunLoop(IntPtr runLoop, IntPtr mode)
        {
            objc_msgSend(NativePtr, sel_addToRunLoop, runLoop, mode);
        }

        public void removeFromRunLoop(IntPtr runLoop, IntPtr mode)
        {
            objc_msgSend(NativePtr, sel_removeFromRunLoop, runLoop, mode);
        }

        public void invalidate()
        {
            objc_msgSend(NativePtr, sel_invalidate);
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RunLoopDelegate : IDisposable
        {
            public static readonly Selector StepSelector = "step:";

            public readonly IntPtr NativePtr;
            public static implicit operator IntPtr(RunLoopDelegate t) => t.NativePtr;

            public static RunLoopDelegate Create(RunLoopDelegateCallback callback)
            {
                var cls = ObjCClass.Create($"VeldridRunLoop-{Guid.NewGuid():N}", cls =>
                {
                    fixed (byte* typeNamesPtr = "v@:@\0"u8)
                        class_addMethod(cls, StepSelector, Marshal.GetFunctionPointerForDelegate(callback), typeNamesPtr);
                });

                return cls.AllocInit<RunLoopDelegate>();
            }

            public void Dispose()
            {
                release(this);
            }
        }

        public delegate void RunLoopDelegateCallback(IntPtr self, IntPtr cmd, IntPtr displayLink);
    }
}

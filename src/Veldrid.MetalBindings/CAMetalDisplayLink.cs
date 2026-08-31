// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using static Veldrid.MetalBindings.ObjectiveCRuntime;

namespace Veldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAMetalDisplayLink
    {
        private static ObjCClass s_class = new ObjCClass("CAMetalDisplayLink");

        private static readonly Selector sel_initWithMetalLayer = "initWithMetalLayer:";

        private static readonly Selector sel_isPaused = "isPaused";
        private static readonly Selector sel_setPaused = "setPaused:";

        private static readonly Selector sel_preferredFrameLatency = "preferredFrameLatency";
        private static readonly Selector sel_setPreferredFrameLatency = "setPreferredFrameLatency:";

        private static readonly Selector sel_delegate = "delegate";
        private static readonly Selector sel_setDelegate = "setDelegate:";

        private static readonly Selector sel_addToRunLoop = "addToRunLoop:forMode:";
        private static readonly Selector sel_invalidate = "invalidate";

        public readonly IntPtr NativePtr;
        public CAMetalDisplayLink(IntPtr ptr) => NativePtr = ptr;
        public static implicit operator IntPtr(CAMetalDisplayLink c) => c.NativePtr;

        public static CAMetalDisplayLink Create(CAMetalLayer layer)
        {
            var ret = s_class.Alloc<CAMetalDisplayLink>();
            objc_msgSend(ret.NativePtr, sel_initWithMetalLayer, layer);
            return ret;
        }

        public bool paused
        {
            get => bool8_objc_msgSend(NativePtr, sel_isPaused);
            set => objc_msgSend(NativePtr, sel_setPaused, value);
        }

        public float preferredFrameLatency
        {
            get => float_objc_msgSend(NativePtr, sel_preferredFrameLatency);
            set => objc_msgSend(NativePtr, sel_setPreferredFrameLatency, value);
        }

        public RunLoopDelegate @delegate
        {
            set => objc_msgSend(NativePtr, sel_setDelegate, value);
        }

        public void addToRunLoop(IntPtr runLoop, IntPtr mode)
        {
            objc_msgSend(NativePtr, sel_addToRunLoop, runLoop, mode);
        }

        public void invalidate()
        {
            objc_msgSend(NativePtr, sel_invalidate);
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RunLoopDelegate : IDisposable
        {
            private static readonly Selector sel_needsUpdate = "metalDisplayLink:needsUpdate:";

            public readonly IntPtr NativePtr;
            public static implicit operator IntPtr(RunLoopDelegate l) => l.NativePtr;

            public static RunLoopDelegate Create(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void> callback)
            {
                var cls = ObjCClass.Create($"VeldridRunLoop", cls =>
                {
                    fixed (byte* typeNamesPtr = "v@:@@\0"u8)
                        class_addMethod(cls, sel_needsUpdate, (IntPtr)callback, typeNamesPtr);
                });

                return cls.AllocInit<RunLoopDelegate>();
            }

            public void Dispose()
            {
                release(this);
            }
        }
    }
}

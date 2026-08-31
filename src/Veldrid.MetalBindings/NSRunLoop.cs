// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using static Veldrid.MetalBindings.ObjectiveCRuntime;

namespace Veldrid.MetalBindings
{
    public struct NSRunLoop
    {
        private static readonly ObjCClass s_class = new ObjCClass("NSRunLoop");
        private static readonly Selector sel_mainRunLoop = "mainRunLoop";
        private static readonly Selector sel_currentRunLoop = "currentRunLoop";
        private static readonly Selector sel_run = "run";

        public readonly IntPtr NativePtr;
        public static implicit operator IntPtr(NSRunLoop l) => l.NativePtr;

        public static NSRunLoop mainRunLoop() => objc_msgSend<NSRunLoop>(s_class, sel_mainRunLoop);

        public static NSRunLoop currentRunLoop() => objc_msgSend<NSRunLoop>(s_class, sel_currentRunLoop);

        public void run() => objc_msgSend(NativePtr, sel_run);
    }
}

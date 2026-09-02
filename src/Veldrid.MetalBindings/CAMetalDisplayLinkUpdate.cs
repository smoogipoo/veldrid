// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using static Veldrid.MetalBindings.ObjectiveCRuntime;

namespace Veldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAMetalDisplayLinkUpdate
    {
        private static readonly Selector sel_targetPresentationTimestamp = "targetPresentationTimestamp";
        private static readonly Selector sel_targetTimestamp = "targetTimestamp";
        private static readonly Selector sel_drawable = "drawable";

        public readonly IntPtr NativePtr;
        public CAMetalDisplayLinkUpdate(IntPtr ptr) => NativePtr = ptr;
        public static implicit operator IntPtr(CAMetalDisplayLinkUpdate c) => c.NativePtr;

        public double targetPresentationTimestamp => double_objc_msgSend(NativePtr, sel_targetPresentationTimestamp);

        public double targetTimestamp => double_objc_msgSend(NativePtr, sel_targetTimestamp);

        public CAMetalDrawable drawable => objc_msgSend<CAMetalDrawable>(NativePtr, sel_drawable);
    }
}

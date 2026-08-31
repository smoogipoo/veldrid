// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    public static class CFRunLoopMode
    {
        private const string CFFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        public static IntPtr CommonModes
        {
            get
            {
                IntPtr handle = NativeLibrary.Load(CFFramework);
                IntPtr symbolPtr = NativeLibrary.GetExport(handle, "kCFRunLoopCommonModes");
                return Marshal.ReadIntPtr(symbolPtr);
            }
        }
    }
}

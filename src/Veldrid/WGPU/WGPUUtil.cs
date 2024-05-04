// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using Silk.NET.WebGPU;

namespace Veldrid.WGPU
{
    internal static unsafe class WGPUUtil
    {
        public static ChainedStruct* Chain<T>(T data)
            where T : unmanaged
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(data, ptr, false);
            return (ChainedStruct*)ptr;
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using Silk.NET.WebGPU;
using Veldrid.WGPU;

namespace Veldrid
{
    public class BackendInfoWebGPU
    {
        public readonly Limits Limits;
        public readonly string VendorName;
        public readonly string DriverDescription;
        public readonly string AdapterName;
        public readonly AdapterType AdapterType;
        public readonly BackendType BackendType;

        internal unsafe BackendInfoWebGPU(WGPUGraphicsDevice gd)
        {
            SupportedLimits deviceLimits = default;
            gd.WebGPU.DeviceGetLimits(gd.NativeDevice, ref deviceLimits);

            AdapterProperties adapterProperties = default;
            gd.WebGPU.AdapterGetProperties(gd.NativeAdapter, ref adapterProperties);

            Limits = deviceLimits.Limits;
            VendorName = Marshal.PtrToStringUTF8((IntPtr)adapterProperties.VendorName);
            DriverDescription = Marshal.PtrToStringUTF8((IntPtr)adapterProperties.DriverDescription);
            AdapterName = Marshal.PtrToStringUTF8((IntPtr)adapterProperties.Name);
            AdapterType = adapterProperties.AdapterType;
            BackendType = adapterProperties.BackendType;
        }
    }
}

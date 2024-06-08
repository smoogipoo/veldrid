// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid.WGPU;
using WebGPU;
using static WebGPU.WebGPU;

namespace Veldrid
{
    public class BackendInfoWebGPU
    {
        public readonly WGPULimits Limits;
        public readonly string VendorName;
        public readonly string DriverDescription;
        public readonly string AdapterName;
        public readonly WGPUAdapterType AdapterType;
        public readonly WGPUBackendType BackendType;

        internal unsafe BackendInfoWebGPU(WGPUGraphicsDevice gd)
        {
            WGPUSupportedLimits deviceLimits;
            wgpuDeviceGetLimits(gd.NativeDevice, &deviceLimits);

            WGPUAdapterProperties adapterProperties;
            wgpuAdapterGetProperties(gd.NativeAdapter, &adapterProperties);

            Limits = deviceLimits.limits;
            VendorName = Interop.GetString(adapterProperties.vendorName);
            DriverDescription = Interop.GetString(adapterProperties.driverDescription);
            AdapterName = Interop.GetString(adapterProperties.name);
            AdapterType = adapterProperties.adapterType;
            BackendType = adapterProperties.backendType;
        }
    }
}

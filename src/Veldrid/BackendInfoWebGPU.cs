// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Silk.NET.WebGPU;
using Veldrid.WGPU;

namespace Veldrid
{
    public class BackendInfoWebGPU
    {
        public readonly Limits Limits;

        internal unsafe BackendInfoWebGPU(WGPUGraphicsDevice gd)
        {
            SupportedLimits deviceLimits = default;
            gd.WebGPU.DeviceGetLimits(gd.NativeDevice, ref deviceLimits);

            Limits = deviceLimits.Limits;
        }
    }
}

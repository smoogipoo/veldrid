// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    public partial class LibSystem
    {
        [DllImport("libSystem.dylib")]
        public static extern int mach_timebase_info(out mach_timebase_info_data_t info);

        [DllImport("libSystem.dylib")]
        public static extern ulong mach_absolute_time();

        [DllImport("libSystem.dylib")]
        public static extern int mach_wait_until(ulong deadline);

        [StructLayout(LayoutKind.Sequential)]
        public struct mach_timebase_info_data_t
        {
            public uint numer;
            public uint denom;
        }
    }
}

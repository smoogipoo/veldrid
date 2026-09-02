// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public partial struct CAFrameRateRange
    {
        private const string QCFramework = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";

        public float minimum;
        public float maximum;
        public float preferred;

        [LibraryImport(QCFramework, EntryPoint = "CAFrameRateRangeMake")]
        public static partial CAFrameRateRange Create(float minimum, float maximum, float preferred);
    }
}

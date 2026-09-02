// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    public static partial class CoreAnimation
    {
        private const string QCFramework = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";

        [LibraryImport(QCFramework, EntryPoint = "CACurrentMediaTime")]
        public static partial double CurrentMediaTime();
    }
}

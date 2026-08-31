// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Veldrid.MTL
{
    internal interface IMtlDisplayLink : IDisposable
    {
        public double GetActualOutputVideoRefreshPeriod();
        public void UpdateActiveDisplay(int x, int y, int w, int h);
    }
}

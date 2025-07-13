// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace Veldrid.SDL3
{
    [DebuggerNonUserCode]
    internal readonly struct ValueInvokeOnDisposal : IDisposable
    {
        private readonly object sender;
        private readonly Action<object> func;

        public ValueInvokeOnDisposal(object sender, Action<object> func)
        {
            this.sender = sender;
            this.func = func;
        }

        public void Dispose()
        {
            func(sender);
        }
    }
}

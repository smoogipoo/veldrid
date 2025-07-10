// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrid.SDL3
{
    public class SDL3ResourceSet : ResourceSet
    {
        public override string Name { get; set; }

        public readonly SDL3ResourceLayout Layout;
        public readonly IBindableResource[] BoundResources;

        private bool isDisposed;

        public SDL3ResourceSet(SDL3GraphicsDevice gd, ref ResourceSetDescription description)
            : base(ref description)
        {
            Layout = Util.AssertSubtype<ResourceLayout, SDL3ResourceLayout>(description.Layout);
            BoundResources = description.BoundResources;
        }

        public override bool IsDisposed => isDisposed;

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }
    }
}

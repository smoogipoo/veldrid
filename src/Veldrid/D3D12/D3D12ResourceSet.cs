namespace Veldrid.D3D12
{
    internal class D3D12ResourceSet : ResourceSet
    {
        private string _name;
        private bool _disposed;

        public new BindableResource[] Resources { get; }
        public new D3D12ResourceLayout Layout { get; }

        public D3D12ResourceSet(ref ResourceSetDescription description) : base(ref description)
        {
            Resources = Util.ShallowClone(description.BoundResources);
            Layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
        }

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            _disposed = true;
        }
    }
}

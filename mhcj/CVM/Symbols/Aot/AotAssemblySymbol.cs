using System;
using System.Collections.Generic;
using CVM.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// aot assembly
    /// </summary>
    internal sealed class AotAssemblySymbol : MetadataOrSourceAssemblySymbol
    {
        private static AotAssemblySymbol _inst;
        internal  static AotAssemblySymbol Inst
        {

            get
            {
                if(_inst==null)
                {
                    _inst = new AotAssemblySymbol();
                }
                return _inst;
            }
        }
        internal AotModuleSymbol Aot;
        AssemblyIdentity _id;
        private readonly ImmutableArray<ModuleSymbol> _modules;
        ImmutableArray<Location> loc;
        internal AotAssemblySymbol()
        {
            _id = new AssemblyIdentity("CVM_Core", new Version(1, 2, 3, 4),System.Globalization.CultureInfo.CurrentCulture.Name, default, false);
            _modules = ImmutableArray<ModuleSymbol>.Empty;
            Aot = new AotModuleSymbol(this);
            _modules = _modules.Add(Aot);
            loc = new ImmutableArray<Location>(new Location[] { new AotLocation() });

      
            SetCorLibrary(this);
        }

     
      
        public override AssemblyIdentity Identity => _id;

        public override Version AssemblyVersionPattern => null;

        public override ImmutableArray<ModuleSymbol> Modules => _modules;

        public override bool MightContainExtensionMethods => true;

        public override ImmutableArray<Location> Locations => loc;

        internal override bool IsLinked => false;

        internal override ImmutableArray<byte> PublicKey => throw new NotImplementedException();

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol other)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            throw new NotImplementedException();
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw new NotImplementedException();
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            throw new NotImplementedException();
        }
    }
}

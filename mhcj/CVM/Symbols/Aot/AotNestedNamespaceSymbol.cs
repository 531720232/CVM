using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AotNestedNamespaceSymbol : AotNamespaceSymbol
    {
        /// <summary>
        /// The parent namespace. There is always one, Global namespace contains all
        /// top level namespaces. 
        /// </summary>
        /// <remarks></remarks>
        private readonly AotNamespaceSymbol _containingNamespaceSymbol;

        /// <summary>
        /// The name of the namespace.
        /// </summary>
        /// <remarks></remarks>
        private readonly string _name;
        /// <summary>
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids are grouped by the 
        /// fully-qualified namespace name case-sensitively. There could be multiple groups 
        /// for each fully-qualified namespace name. The groups are sorted by their 
        /// key in case-sensitive manner. Empty string is used as namespace name for types 
        /// immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        /// will be in several first IGroupings.
        /// 
        /// This member is initialized by constructor and is cleared in EnsureAllMembersLoaded 
        /// as soon as symbols for children are created.
        /// </summary>
        /// <remarks></remarks>
        private IEnumerable<IGrouping<string, Type>> _typesByNS;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">
        /// Name of the namespace, must be not empty.
        /// </param>
        /// <param name="containingNamespace">
        /// Containing namespace.
        /// </param>
        /// <param name="typesByNS">
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids are grouped by the 
        /// fully-qualified namespace name case-sensitively. There could be multiple groups 
        /// for each fully-qualified namespace name. The groups are sorted by their 
        /// key in case-sensitive manner. Empty string is used as namespace name for types 
        /// immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        /// will be in several first IGroupings.
        /// </param>
        internal AotNestedNamespaceSymbol(
            string name,
            AotNamespaceSymbol containingNamespace,
            IEnumerable<IGrouping<string, Type>> typesByNS)
        {
       
            _containingNamespaceSymbol = containingNamespace;
            _name = name;
            _typesByNS = typesByNS;
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingNamespaceSymbol; }
        }
        internal override AotModuleSymbol ContainingAotModule
        {
            get { return _containingNamespaceSymbol.ContainingAotModule; }
        }


        //internal override PEModuleSymbol ContainingPEModule
        //{
        //    get { return _containingNamespaceSymbol.ContainingPEModule; }
        //}
        public override string Name
        {
            get
            {
                return _name;
            }
        }
        public override bool IsGlobalNamespace
        {
            get
            {
                return false;
            }
        }
        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return ContainingModule.ContainingAssembly;
            }
        }

        protected override void EnsureAllMembersLoaded()
        {
            var typesByNS = _typesByNS;

            if (lazyTypes == null || lazyNamespaces == null)
            {
                System.Diagnostics.Debug.Assert(typesByNS != null);
                LoadAllMembers(typesByNS);
                CVM.AHelper.Exchange(ref _typesByNS, null);
            }
        }
        internal override ModuleSymbol ContainingModule => _containingNamespaceSymbol.ContainingModule;

    }
}

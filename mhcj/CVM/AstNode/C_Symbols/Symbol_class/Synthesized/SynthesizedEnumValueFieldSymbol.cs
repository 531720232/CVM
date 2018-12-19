
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents __value field of an enum.
    /// </summary>
    internal sealed class SynthesizedEnumValueFieldSymbol : SynthesizedFieldSymbolBase
    {
        public SynthesizedEnumValueFieldSymbol(SourceNamedTypeSymbol containingEnum)
            : base(containingEnum, WellKnownMemberNames.EnumBackingFieldName, isPublic: true, isReadOnly: false, isStatic: false)
        {
        }

        internal override bool SuppressDynamicAttribute
        {
            get { return true; }
        }

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return TypeSymbolWithAnnotations.Create(((SourceNamedTypeSymbol)ContainingType).EnumUnderlyingType);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // no attributes should be emitted
        }
    }
}

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Ts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.AstNode
{
 internal   class TypeNode:Node
    {
        internal DeclarationKind type;

        internal DeclarationModifiers m_Modifiers;


        internal TypeNode base_type;
        internal TypeNode[] imps;

        internal string Namespace { get;  set; }
        internal string Name { get;  set; }
        internal DeclarationModifiers Modifiers
        {
            get { return m_Modifiers; }
            set { m_Modifiers = value; }
        }
        internal string FullName { get {  
                    
                    
                    if(CVM.AHelper.IsNullOrWhiteSpace(Namespace))
                {
                    return Name;
                }
                    
                   return  Namespace + "." + Name; } }//{ get; set; }

        public readonly Guid guid;
     public int Hash { get { return FullName.GetHashCode(); } }

        public void SetFullName(string fullname)
        {

        }
        public TypeNode Parent;
       
        public bool IsNest { get { return Parent != null; } }

        internal TypeNode(BoundKind kind, SyntaxNode node, bool error = false) : base(kind, node)
        {
            guid = Guid.NewGuid();
        }
        internal CVM_Type ToType()
        {
            var t1 = new CVM_Type(this);

            return t1;
        }
    }
}

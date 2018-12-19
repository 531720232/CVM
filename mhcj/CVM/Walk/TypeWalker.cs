using CVM.AstNode;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CVM
{
    public class TypeWalker : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor<AstNode.Node>
    {
     
        
        public override Node VisitClassDeclaration(ClassDeclarationSyntax node)
        {


            return VisitTypeDeclarationCore(node);
        }

        private Node VisitTypeDeclarationCore(TypeDeclarationSyntax node)
        {
            //if (!LookupPosition.IsInTypeDeclaration(_position, parent))
            //{
            //    return VisitCore(parent.Parent);
            //}
            return VisitTypeDeclarationCore(node, 0);

        }
        private Node VisitTypeDeclarationCore(TypeDeclarationSyntax parent,int ia)
        {
         
            

            return null;
        }

        public override Node VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            return base.VisitDelegateDeclaration(node);
        }

        public override Node VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return base.VisitEnumDeclaration(node);
        }

        public override Node VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return base.VisitInterfaceDeclaration(node);
        }

        public override Node VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return VisitTypeDeclarationCore(node);
        }
    }
}

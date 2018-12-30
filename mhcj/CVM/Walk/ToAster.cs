using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.AstNode;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{

    internal partial class ToAster : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor<Node>
    {

     //  .. public CVM_Zone OutZone;
        public AssemblyAttribute AssemblyInfo { get; private set; }
        public int Apos;
        Microsoft.CodeAnalysis.CSharp.CVM_Zone zone;
        public readonly System.Guid guid;
        private SyntaxTree SyntaxTree;
      
        public ToAster(Microsoft.CodeAnalysis.CSharp.CVM_Zone zone, SyntaxTree tree)
        {
     // ..      AssemblyInfo = new AssemblyAttribute(BoundKind.Attribute,null);
            this.zone = zone;
            type_cache = new List<TypeNode>();
            SyntaxTree = tree;
            guid = System.Guid.NewGuid();
        }
        public void Start()
        {
            Visit(SyntaxTree.GetRoot());
        }
        public override Node DefaultVisit(SyntaxNode node)
        {
            return base.DefaultVisit(node);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override Node Visit(SyntaxNode node)
        {
            return base.Visit(node);
        }

        public override Node VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            return base.VisitAccessorDeclaration(node);
        }

        public override Node VisitAccessorList(AccessorListSyntax node)
        {
            return base.VisitAccessorList(node);
        }

        public override Node VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            return base.VisitAliasQualifiedName(node);
        }

        public override Node VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            return base.VisitAnonymousMethodExpression(node);
        }

        public override Node VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            return base.VisitAnonymousObjectCreationExpression(node);
        }

        public override Node VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
        {
          
            return base.VisitAnonymousObjectMemberDeclarator(node);
        }
     
        public override Node VisitArgument(ArgumentSyntax node)
        {

switch(node.Expression)
            {
                case LiteralExpressionSyntax a:
                   switch((Microsoft.CodeAnalysis.CSharp.SyntaxKind)a.Token.RawKind)
                    {
                        case Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression
                            :


                            break;
                        case Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharacterLiteralExpression:

                            break;
                        
                    }
                    break;
            }
        
            return base.VisitArgument(node);
        }

        public override Node VisitArgumentList(ArgumentListSyntax node)
        {
            return base.VisitArgumentList(node);
        }

        public override Node VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            return base.VisitArrayCreationExpression(node);
        }

        public override Node VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node)
        {
            return base.VisitArrayRankSpecifier(node);
        }

        public override Node VisitArrayType(ArrayTypeSyntax node)
        {
            return base.VisitArrayType(node);
        }

        public override Node VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            return base.VisitArrowExpressionClause(node);
        }

        public override Node VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            return base.VisitAssignmentExpression(node);
        }

        public override Node VisitAttribute(AttributeSyntax node)
        {
            return base.VisitAttribute(node);
        }

        public override Node VisitAttributeArgument(AttributeArgumentSyntax node)
        {
        
            return base.VisitAttributeArgument(node);
        }

        public override Node VisitAttributeArgumentList(AttributeArgumentListSyntax node)
        {
         
            return base.VisitAttributeArgumentList(node);
        }

        public override Node VisitAttributeList(AttributeListSyntax node)
        {
            return base.VisitAttributeList(node);
        }

        public override Node VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node)
        {
            return base.VisitAttributeTargetSpecifier(node);
        }

        public override Node VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            return base.VisitAwaitExpression(node);
        }

        public override Node VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node)
        {
            return base.VisitBadDirectiveTrivia(node);
        }

        public override Node VisitBaseExpression(BaseExpressionSyntax node)
        {
            return base.VisitBaseExpression(node);
        }

        public override Node VisitBaseList(BaseListSyntax node)
        {
            return base.VisitBaseList(node);
        }

        public override Node VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            return base.VisitBinaryExpression(node);
        }

        public override Node VisitBlock(BlockSyntax node)
        {
            return base.VisitBlock(node);
        }

        public override Node VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            return base.VisitBracketedArgumentList(node);
        }

        public override Node VisitBracketedParameterList(BracketedParameterListSyntax node)
        {
            return base.VisitBracketedParameterList(node);
        }

        public override Node VisitBreakStatement(BreakStatementSyntax node)
        {
            return base.VisitBreakStatement(node);
        }

        public override Node VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            return base.VisitCasePatternSwitchLabel(node);
        }

        public override Node VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            return base.VisitCaseSwitchLabel(node);
        }

        public override Node VisitCastExpression(CastExpressionSyntax node)
        {
            return base.VisitCastExpression(node);
        }

        public override Node VisitCatchClause(CatchClauseSyntax node)
        {
            return base.VisitCatchClause(node);
        }

        public override Node VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            return base.VisitCatchDeclaration(node);
        }

        public override Node VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            return base.VisitCatchFilterClause(node);
        }

        public override Node VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            return base.VisitCheckedExpression(node);
        }

        public override Node VisitCheckedStatement(CheckedStatementSyntax node)
        {
            return base.VisitCheckedStatement(node);
        }


        private List<TypeNode> type_cache;

        private TypeNode GetOrAdd(string namespaces,string type,SyntaxNode node)
        {
            var type_name = namespaces != null ? namespaces + "." + type : type;
            var item = type_cache.Find((a)=> { if (a.FullName == type_name) return true; return false; });

            if(item==null)

            {
                var a1 = new TypeNode(BoundKind.TypeExpression, node) { Namespace = namespaces, Name = type };
                 type_cache.Add(a1);
                return a1;
            }
            return
                default;
        
        }
        private TypeNode GetOrAdd(int hash)
        {
           
            var item = type_cache.Find((a) => { if (a.Hash == hash) return true; return false; });

            if (item == null)

            {
                var a1 =  new TypeNode(BoundKind.TypeExpression, null);
                type_cache.Add(a1);
                return a1;
            }
            return
                default;

        }
        public static List<string> GetFullNs(SyntaxNode node)
        {
            List<string> strs=new List<string>();
         
          if(node is NamespaceDeclarationSyntax a)
            {
                strs.Add(a.Name.ToString());

            }
          if(node.Parent!=null)
            {
                strs.AddRange(GetFullNs(node.Parent));
            }
            return strs;
        }
        public static  string ToNs(List<string> ns)
        {
            ns.Reverse();
            var str = "";
         
            str = string.Join(".", ns.ToArray());
            return str;
        }
        public static bool IsNest(SyntaxNode ns,ref ClassDeclarationSyntax parent)
        {
            if (ns is null)
                return false;
            if (ns.Parent is null)
                return false;
            if(ns.Parent is ClassDeclarationSyntax b)
            {
                parent = b;
                return true;
            }
         
            else {
            return     IsNest(ns.Parent?.Parent,ref parent);
            }

        }
        
        public override Node VisitClassDeclaration(ClassDeclarationSyntax node)
        {

         
            TypeNode type = null;
            var has_p = Has_partial(node, ref type);


            var w1 = node.BaseList;
         var ts=   w1?.Types[0];

            var t2 = type?.ToType();
            foreach(var m in node.Members)
            {
                if(m is MethodDeclarationSyntax m1)
                {
                  
                }
            }

            return default;
         //   var bag = new DiagnosticBag();
         //   ClassDeclarationSyntax w1 = null;
        
         //   //    var isnest = IsNest(node,ref  w1);
         //   var ns = ToNs(GetFullNs(node));
         //   var class_name = node.Identifier.ValueText;
         ////   var tn = ToTypeName(GetFullNs(node),node.Identifier.ValueText);
         //   var type = GetOrAdd(ns,class_name,node);
         //   var mod1 = node.Modifiers.ToDeclarationModifiers(bag);
         //   type.Modifiers = mod1;

         //   foreach(var m in node.Members)
         //   {
         //       if (m is ClassDeclarationSyntax cl)
         //       {
         //           var item = (TypeNode)cl.Accept(this);
         //           item.Parent = type;
         //           //    System.Console.WriteLine("" + item.FullName + "镶嵌在" + type.FullName);
         //       }

         //       if(m is FieldDeclarationSyntax f1)
         //       {
         //           var mods = f1.Modifiers.ToDeclarationModifiers(bag);
         //        //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //       if (m is PropertyDeclarationSyntax p1)
         //       {
         //           var mods = p1.Modifiers.ToDeclarationModifiers(bag);


         //           //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //       if (m is MethodDeclarationSyntax m1)
         //       {
         //           var mods = m1.Modifiers.ToDeclarationModifiers(bag);
               

         //           //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //       if (m is DelegateDeclarationSyntax d1)
         //       {
         //           var mods = d1.Modifiers.ToDeclarationModifiers(bag);


         //           //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //       if (m is EventDeclarationSyntax e1)
         //       {
         //           var mods = e1.Modifiers.ToDeclarationModifiers(bag);

                 
         //           //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //       if (m is EventFieldDeclarationSyntax e2)
         //       {
         //           var mods = e2.Modifiers.ToDeclarationModifiers(bag);

         //        var e2_1=   e2.Declaration;
              
         //           //if(f1.Modifiers[0].Kind()==SyntaxKind.ConstKeyword
         //       }
         //   }
          

         //   var w =     node.Modifiers;
         //   foreach(var m in w.AsImmutable())
         //   {
           
         //       var typ = m.Value;

             
         //   }
         //   return type;
        }

        public override Node VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node)
        {
          
            return base.VisitClassOrStructConstraint(node);
        }
               /// <summary>
        /// 入口
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override Node VisitCompilationUnit(CompilationUnitSyntax node)
        {
           var ms= node.Members;
         
            foreach(var item in ms)
            {
                item.Accept(this);
                  
            }
            var ats = node.AttributeLists;

            foreach (var item in ats)
            {

                if (item.Target.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.AttributeTargetSpecifier)
                {
                    if (item.Target.Identifier.ValueText == "assembly")
                    {
                    VisitAssemblyAttr(item);
                }
                }
                    //  item.Accept(this);
            }
            var us = node.Usings;

            AddUsings(us);



            return base.VisitCompilationUnit(node);
        }
    
        private void VisitAssemblyAttr(AttributeListSyntax attrs)
        {
            foreach(var v in attrs.Attributes)
            {
               
         switch(v.Name)
                {
                    
                }
         //  var text=     v.ArgumentList.GetText();

            }
        }

        public override Node VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            return base.VisitConditionalAccessExpression(node);
        }

        public override Node VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            return base.VisitConditionalExpression(node);
        }

        public override Node VisitConstantPattern(ConstantPatternSyntax node)
        {
            return base.VisitConstantPattern(node);
        }

        public override Node VisitConstructorConstraint(ConstructorConstraintSyntax node)
        {
            return base.VisitConstructorConstraint(node);
        }

        public override Node VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return base.VisitConstructorDeclaration(node);
        }

        public override Node VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            return base.VisitConstructorInitializer(node);
        }

        public override Node VisitContinueStatement(ContinueStatementSyntax node)
        {
            return base.VisitContinueStatement(node);
        }

        public override Node VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            return base.VisitConversionOperatorDeclaration(node);
        }

        public override Node VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node)
        {
            return base.VisitConversionOperatorMemberCref(node);
        }

        public override Node VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node)
        {
            return base.VisitCrefBracketedParameterList(node);
        }

        public override Node VisitCrefParameter(CrefParameterSyntax node)
        {
            return base.VisitCrefParameter(node);
        }

        public override Node VisitCrefParameterList(CrefParameterListSyntax node)
        {
            return base.VisitCrefParameterList(node);
        }

        public override Node VisitDeclarationExpression(DeclarationExpressionSyntax node)
        {
            return base.VisitDeclarationExpression(node);
        }

        public override Node VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            return base.VisitDeclarationPattern(node);
        }

        public override Node VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            return base.VisitDefaultExpression(node);
        }

        public override Node VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
        {
            return base.VisitDefaultSwitchLabel(node);
        }

        public override Node VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node)
        {
            return base.VisitDefineDirectiveTrivia(node);
        }

        public override Node VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            return base.VisitDelegateDeclaration(node);
        }

        public override Node VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            return base.VisitDestructorDeclaration(node);
        }

        public override Node VisitDiscardDesignation(DiscardDesignationSyntax node)
        {
            return base.VisitDiscardDesignation(node);
        }

        public override Node VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node)
        {
            return base.VisitDocumentationCommentTrivia(node);
        }

        public override Node VisitDoStatement(DoStatementSyntax node)
        {
            return base.VisitDoStatement(node);
        }

        public override Node VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            return base.VisitElementAccessExpression(node);
        }

        public override Node VisitElementBindingExpression(ElementBindingExpressionSyntax node)
        {
            return base.VisitElementBindingExpression(node);
        }

        public override Node VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node)
        {
            return base.VisitElifDirectiveTrivia(node);
        }

        public override Node VisitElseClause(ElseClauseSyntax node)
        {
            return base.VisitElseClause(node);
        }

        public override Node VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
        {
            return base.VisitElseDirectiveTrivia(node);
        }

        public override Node VisitEmptyStatement(EmptyStatementSyntax node)
        {
            return base.VisitEmptyStatement(node);
        }

        public override Node VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
        {
            return base.VisitEndIfDirectiveTrivia(node);
        }

        public override Node VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            return base.VisitEndRegionDirectiveTrivia(node);
        }

        public override Node VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return base.VisitEnumDeclaration(node);
        }

        public override Node VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            return base.VisitEnumMemberDeclaration(node);
        }

        public override Node VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            return base.VisitEqualsValueClause(node);
        }

        public override Node VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node)
        {
            return base.VisitErrorDirectiveTrivia(node);
        }

        public override Node VisitEventDeclaration(EventDeclarationSyntax node)
        {
            return base.VisitEventDeclaration(node);
        }

        public override Node VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            return base.VisitEventFieldDeclaration(node);
        }

        public override Node VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node)
        {
            return base.VisitExplicitInterfaceSpecifier(node);
        }

        public override Node VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(node);
        }

        public override Node VisitExternAliasDirective(ExternAliasDirectiveSyntax node)
        {
            return base.VisitExternAliasDirective(node);
        }

        public override Node VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            return base.VisitFieldDeclaration(node);
        }

        public override Node VisitFinallyClause(FinallyClauseSyntax node)
        {
            return base.VisitFinallyClause(node);
        }

        public override Node VisitFixedStatement(FixedStatementSyntax node)
        {
            return base.VisitFixedStatement(node);
        }

        public override Node VisitForEachStatement(ForEachStatementSyntax node)
        {
            return base.VisitForEachStatement(node);
        }

        public override Node VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
        {
            return base.VisitForEachVariableStatement(node);
        }

        public override Node VisitForStatement(ForStatementSyntax node)
        {
            return base.VisitForStatement(node);
        }

        public override Node VisitFromClause(FromClauseSyntax node)
        {
            return base.VisitFromClause(node);
        }

        public override Node VisitGenericName(GenericNameSyntax node)
        {
            return base.VisitGenericName(node);
        }

        public override Node VisitGlobalStatement(GlobalStatementSyntax node)
        {
            return base.VisitGlobalStatement(node);
        }

        public override Node VisitGotoStatement(GotoStatementSyntax node)
        {
            return base.VisitGotoStatement(node);
        }

        public override Node VisitGroupClause(GroupClauseSyntax node)
        {
            return base.VisitGroupClause(node);
        }

        public override Node VisitIdentifierName(IdentifierNameSyntax node)
        {
            return base.VisitIdentifierName(node);
        }

        public override Node VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
        {
            return base.VisitIfDirectiveTrivia(node);
        }

        public override Node VisitIfStatement(IfStatementSyntax node)
        {
            return base.VisitIfStatement(node);
        }

        public override Node VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            return base.VisitImplicitArrayCreationExpression(node);
        }

        public override Node VisitImplicitElementAccess(ImplicitElementAccessSyntax node)
        {
            return base.VisitImplicitElementAccess(node);
        }

        public override Node VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node)
        {
            return base.VisitImplicitStackAllocArrayCreationExpression(node);
        }

        public override Node VisitIncompleteMember(IncompleteMemberSyntax node)
        {
            return base.VisitIncompleteMember(node);
        }

        public override Node VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            return base.VisitIndexerDeclaration(node);
        }

        public override Node VisitIndexerMemberCref(IndexerMemberCrefSyntax node)
        {
            return base.VisitIndexerMemberCref(node);
        }

        public override Node VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            return base.VisitInitializerExpression(node);
        }

        public override Node VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return base.VisitInterfaceDeclaration(node);
        }

        public override Node VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            return base.VisitInterpolatedStringExpression(node);
        }

        public override Node VisitInterpolatedStringText(InterpolatedStringTextSyntax node)
        {
            return base.VisitInterpolatedStringText(node);
        }

        public override Node VisitInterpolation(InterpolationSyntax node)
        {
            return base.VisitInterpolation(node);
        }

        public override Node VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node)
        {
            return base.VisitInterpolationAlignmentClause(node);
        }

        public override Node VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node)
        {
            return base.VisitInterpolationFormatClause(node);
        }

        public override Node VisitInvocationExpression(InvocationExpressionSyntax node)
        {
        

            return base.VisitInvocationExpression(node);
        }

        public override Node VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            return base.VisitIsPatternExpression(node);
        }

        public override Node VisitJoinClause(JoinClauseSyntax node)
        {
            return base.VisitJoinClause(node);
        }

        public override Node VisitJoinIntoClause(JoinIntoClauseSyntax node)
        {
            return base.VisitJoinIntoClause(node);
        }

        public override Node VisitLabeledStatement(LabeledStatementSyntax node)
        {
            return base.VisitLabeledStatement(node);
        }

        public override Node VisitLetClause(LetClauseSyntax node)
        {
            return base.VisitLetClause(node);
        }

        public override Node VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node)
        {
            return base.VisitLineDirectiveTrivia(node);
        }

        public override Node VisitLiteralExpression(LiteralExpressionSyntax node)
        {

            return base.VisitLiteralExpression(node);
        }

        public override Node VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node)
        {
            return base.VisitLoadDirectiveTrivia(node);
        }

        public override Node VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return base.VisitLocalDeclarationStatement(node);
        }

        public override Node VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            return base.VisitLocalFunctionStatement(node);
        }

        public override Node VisitLockStatement(LockStatementSyntax node)
        {
            return base.VisitLockStatement(node);
        }

        public override Node VisitMakeRefExpression(MakeRefExpressionSyntax node)
        {
            return base.VisitMakeRefExpression(node);
        }

        public override Node VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            return base.VisitMemberAccessExpression(node);
        }

        public override Node VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
        {
            return base.VisitMemberBindingExpression(node);
        }

        public override Node VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return base.VisitMethodDeclaration(node);
        }

        public override Node VisitNameColon(NameColonSyntax node)
        {
            return base.VisitNameColon(node);
        }

        public override Node VisitNameEquals(NameEqualsSyntax node)
        {
            return base.VisitNameEquals(node);
        }

        public override Node VisitNameMemberCref(NameMemberCrefSyntax node)
        {
            return base.VisitNameMemberCref(node);
        }
    
        public override Node VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
          
            //    Apos = node.Name.SpanStart;
            var us = node.Usings;

            foreach(var u in us)
            {
           if(u.StaticKeyword.Kind()==SyntaxKind.None)
                {
                    A2Factory.AddUsing(this,u.Name.ToString());
                }
            }

                var ms =    node.Members;
            foreach(var m in ms)
            {
                m.Accept(this);
            }
            return base.VisitNamespaceDeclaration(node);
        }

        public override Node VisitNullableDirectiveTrivia(NullableDirectiveTriviaSyntax node)
        {
            return base.VisitNullableDirectiveTrivia(node);
        }

        public override Node VisitNullableType(NullableTypeSyntax node)
        {
            return base.VisitNullableType(node);
        }

        public override Node VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            return base.VisitObjectCreationExpression(node);
        }

        public override Node VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node)
        {
            return base.VisitOmittedArraySizeExpression(node);
        }

        public override Node VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node)
        {
            return base.VisitOmittedTypeArgument(node);
        }

        public override Node VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            return base.VisitOperatorDeclaration(node);
        }

        public override Node VisitOperatorMemberCref(OperatorMemberCrefSyntax node)
        {
            return base.VisitOperatorMemberCref(node);
        }

        public override Node VisitOrderByClause(OrderByClauseSyntax node)
        {
            return base.VisitOrderByClause(node);
        }

        public override Node VisitOrdering(OrderingSyntax node)
        {
            return base.VisitOrdering(node);
        }

        public override Node VisitParameter(ParameterSyntax node)
        {
            return base.VisitParameter(node);
        }

        public override Node VisitParameterList(ParameterListSyntax node)
        {
            return base.VisitParameterList(node);
        }

        public override Node VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            return base.VisitParenthesizedExpression(node);
        }

        public override Node VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            return base.VisitParenthesizedLambdaExpression(node);
        }

        public override Node VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node)
        {
            return base.VisitParenthesizedVariableDesignation(node);
        }

        public override Node VisitPointerType(PointerTypeSyntax node)
        {
            return base.VisitPointerType(node);
        }

        public override Node VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            return base.VisitPostfixUnaryExpression(node);
        }

        public override Node VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node)
        {
            return base.VisitPragmaChecksumDirectiveTrivia(node);
        }

        public override Node VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node)
        {
            return base.VisitPragmaWarningDirectiveTrivia(node);
        }

        public override Node VisitPredefinedType(PredefinedTypeSyntax node)
        {
            return base.VisitPredefinedType(node);
        }

        public override Node VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            return base.VisitPrefixUnaryExpression(node);
        }

        public override Node VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            return base.VisitPropertyDeclaration(node);
        }

        public override Node VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            return base.VisitQualifiedCref(node);
        }

        public override Node VisitQualifiedName(QualifiedNameSyntax node)
        {
            return base.VisitQualifiedName(node);
        }

        public override Node VisitQueryBody(QueryBodySyntax node)
        {
            return base.VisitQueryBody(node);
        }

        public override Node VisitQueryContinuation(QueryContinuationSyntax node)
        {
            return base.VisitQueryContinuation(node);
        }

        public override Node VisitQueryExpression(QueryExpressionSyntax node)
        {
            return base.VisitQueryExpression(node);
        }

        public override Node VisitRangeExpression(RangeExpressionSyntax node)
        {
            return base.VisitRangeExpression(node);
        }

        public override Node VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node)
        {
            return base.VisitReferenceDirectiveTrivia(node);
        }

        public override Node VisitRefExpression(RefExpressionSyntax node)
        {
            return base.VisitRefExpression(node);
        }

        public override Node VisitRefType(RefTypeSyntax node)
        {
            return base.VisitRefType(node);
        }

        public override Node VisitRefTypeExpression(RefTypeExpressionSyntax node)
        {
            return base.VisitRefTypeExpression(node);
        }

        public override Node VisitRefValueExpression(RefValueExpressionSyntax node)
        {
            return base.VisitRefValueExpression(node);
        }

        public override Node VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            return base.VisitRegionDirectiveTrivia(node);
        }

        public override Node VisitReturnStatement(ReturnStatementSyntax node)
        {
            return base.VisitReturnStatement(node);
        }

        public override Node VisitSelectClause(SelectClauseSyntax node)
        {
            return base.VisitSelectClause(node);
        }

        public override Node VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node)
        {
            return base.VisitShebangDirectiveTrivia(node);
        }

        public override Node VisitSimpleBaseType(SimpleBaseTypeSyntax node)
        {
            return base.VisitSimpleBaseType(node);
        }

        public override Node VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            return base.VisitSimpleLambdaExpression(node);
        }

        public override Node VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
        {
            return base.VisitSingleVariableDesignation(node);
        }

        public override Node VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            return base.VisitSizeOfExpression(node);
        }

        public override Node VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node)
        {
            return base.VisitSkippedTokensTrivia(node);
        }

        public override Node VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node)
        {
            return base.VisitStackAllocArrayCreationExpression(node);
        }

        public override Node VisitStructDeclaration(StructDeclarationSyntax node)
        {
            TypeNode type = null;
            var has_p = Has_partial(node, ref type);
            return base.VisitStructDeclaration(node);
        }

        public override Node VisitSwitchSection(SwitchSectionSyntax node)
        {
            return base.VisitSwitchSection(node);
        }

        public override Node VisitSwitchStatement(SwitchStatementSyntax node)
        {
            return base.VisitSwitchStatement(node);
        }

        public override Node VisitThisExpression(ThisExpressionSyntax node)
        {
            return base.VisitThisExpression(node);
        }

        public override Node VisitThrowExpression(ThrowExpressionSyntax node)
        {
            return base.VisitThrowExpression(node);
        }

        public override Node VisitThrowStatement(ThrowStatementSyntax node)
        {
            return base.VisitThrowStatement(node);
        }

        public override Node VisitTryStatement(TryStatementSyntax node)
        {
            return base.VisitTryStatement(node);
        }

        public override Node VisitTupleElement(TupleElementSyntax node)
        {
            return base.VisitTupleElement(node);
        }

        public override Node VisitTupleExpression(TupleExpressionSyntax node)
        {
            return base.VisitTupleExpression(node);
        }

        public override Node VisitTupleType(TupleTypeSyntax node)
        {
            return base.VisitTupleType(node);
        }

        public override Node VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            return base.VisitTypeArgumentList(node);
        }

        public override Node VisitTypeConstraint(TypeConstraintSyntax node)
        {
            return base.VisitTypeConstraint(node);
        }

        public override Node VisitTypeCref(TypeCrefSyntax node)
        {
            return base.VisitTypeCref(node);
        }

        public override Node VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            return base.VisitTypeOfExpression(node);
        }

        public override Node VisitTypeParameter(TypeParameterSyntax node)
        {
            return base.VisitTypeParameter(node);
        }

        public override Node VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
        {
            return base.VisitTypeParameterConstraintClause(node);
        }

        public override Node VisitTypeParameterList(TypeParameterListSyntax node)
        {
            return base.VisitTypeParameterList(node);
        }

        public override Node VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node)
        {
            return base.VisitUndefDirectiveTrivia(node);
        }

        public override Node VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            return base.VisitUnsafeStatement(node);
        }

        public override Node VisitUsingDirective(UsingDirectiveSyntax node)
        {
            return base.VisitUsingDirective(node);
        }

        public override Node VisitUsingStatement(UsingStatementSyntax node)
        {
            return base.VisitUsingStatement(node);
        }

        public override Node VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            return base.VisitVariableDeclaration(node);
        }

        public override Node VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            return base.VisitVariableDeclarator(node);
        }

        public override Node VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node)
        {
            return base.VisitWarningDirectiveTrivia(node);
        }

        public override Node VisitWhenClause(WhenClauseSyntax node)
        {
            return base.VisitWhenClause(node);
        }

        public override Node VisitWhereClause(WhereClauseSyntax node)
        {
            return base.VisitWhereClause(node);
        }

        public override Node VisitWhileStatement(WhileStatementSyntax node)
        {
            return base.VisitWhileStatement(node);
        }

        public override Node VisitXmlCDataSection(XmlCDataSectionSyntax node)
        {
            return base.VisitXmlCDataSection(node);
        }

        public override Node VisitXmlComment(XmlCommentSyntax node)
        {
            return base.VisitXmlComment(node);
        }

        public override Node VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
        {
            return base.VisitXmlCrefAttribute(node);
        }

        public override Node VisitXmlElement(XmlElementSyntax node)
        {
            return base.VisitXmlElement(node);
        }

        public override Node VisitXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            return base.VisitXmlElementEndTag(node);
        }

        public override Node VisitXmlElementStartTag(XmlElementStartTagSyntax node)
        {
            return base.VisitXmlElementStartTag(node);
        }

        public override Node VisitXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            return base.VisitXmlEmptyElement(node);
        }

        public override Node VisitXmlName(XmlNameSyntax node)
        {
            return base.VisitXmlName(node);
        }

        public override Node VisitXmlNameAttribute(XmlNameAttributeSyntax node)
        {
            return base.VisitXmlNameAttribute(node);
        }

        public override Node VisitXmlPrefix(XmlPrefixSyntax node)
        {
            return base.VisitXmlPrefix(node);
        }

        public override Node VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            return base.VisitXmlProcessingInstruction(node);
        }

        public override Node VisitXmlText(XmlTextSyntax node)
        {
            return base.VisitXmlText(node);
        }

        public override Node VisitXmlTextAttribute(XmlTextAttributeSyntax node)
        {
            return base.VisitXmlTextAttribute(node);
        }

        public override Node VisitYieldStatement(YieldStatementSyntax node)
        {
            return base.VisitYieldStatement(node);
        }
        private bool IsInUsing(CSharpSyntaxNode containingNode)
        {
            TextSpan containingSpan = containingNode.Span;
          
            SyntaxToken token;
            if (containingNode.Kind() != SyntaxKind.CompilationUnit && Apos == containingSpan.End)
            {
                // This occurs at EOF
                token = containingNode.GetLastToken();
                Debug.Assert(token == SyntaxTree.GetRoot().GetLastToken());
            }
            else if (Apos < containingSpan.Start || Apos > containingSpan.End) //NB: > not >=
            {
                return false;
            }
            else
            {
                token = containingNode.FindToken(Apos);
            }

            var node = token.Parent;
            while (node != null && node != containingNode)
            {
                // ACASEY: the restriction that we're only interested in children
                // of containingNode (vs descendants) seems to be required for cases like
                // GetSemanticInfoTests.BindAliasQualifier, which binds an alias name
                // within a using directive.
                if (node.IsKind(SyntaxKind.UsingDirective) && node.Parent == containingNode)
                {
                    return true;
                }

                node = node.Parent;
            }
            return false;
        }
    }
}

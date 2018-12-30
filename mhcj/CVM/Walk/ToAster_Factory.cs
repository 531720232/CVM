using CVM;
using Microsoft.CodeAnalysis.CSharp.AstNode;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{


    internal partial class ToAster 
    {
        private  List<string> usings = new List<string>();
        private List<string> usingstatic = new List<string>();
        private CVM.Collections.Concurrent.ConcurrentDictionary<string, string> usingalias = new CVM.Collections.Concurrent.ConcurrentDictionary<string, string>();



        private CVM.Collections.Concurrent.ConcurrentDictionary<string, AstNode.TypeNode> cvm_types = new CVM.Collections.Concurrent.ConcurrentDictionary<string, AstNode.TypeNode>();


        private void AddUsings(SyntaxList<UsingDirectiveSyntax> us)
        {
          
        
        foreach(var u in us)
            {
               
                if (u.StaticKeyword.Kind() == SyntaxKind.None && u.Alias == null)
                {
                    var str = u.Name.ToString();
                    if (!usings.Contains(str))
                    {
                        usings.Add(str);
                    }
                    else
                    {
                        AddError(ErrorCode.WRN_DuplicateUsing, u.Location);


                    }
                }
                else if (u.StaticKeyword.Kind() != SyntaxKind.None)
                {
                    var str = u.Name.ToString();
                    if (!usingstatic.Contains(str))
                    {
                        usingstatic.Add(str);
                    }
                    else
                    {
              
                        AddError(ErrorCode.WRN_DuplicateUsing, u.Location);

                    }
                }
                else
                {

                    var str = u.Name.ToString();
                    var alias =u.Alias.Name.ToString();
                    if (!usingalias.ContainsKey(str))
                    {
                        usingalias.TryAdd(alias,str);
                    }
                    else
                    {
                        AddError(ErrorCode.ERR_DuplicateAlias, u.Location);
                        //var cs = new CSDiagnosticInfo(ErrorCode.ERR_DuplicateAlias);

                        //this.zone.diag.Add(cs, u.Location);

                    }
                }

            }
        }


        private void AddError(ErrorCode error,Location loc)
        {
            var cs = new CSDiagnosticInfo(error);

            this.zone.diag.Add(cs, loc);
        }
        private bool Has_partial(TypeDeclarationSyntax node,ref TypeNode type )
        {

            var b2 = Have_mod(node, DeclarationModifiers.Partial);
         
            var ns = ToNs(GetFullNs(node));
            var class_name = node.Identifier.ValueText;
            var type_name =!AHelper.IsNullOrWhiteSpace(ns) ? ns + "." + class_name : class_name;
            var item = type_cache.Find((a) => { if (a.FullName == type_name) return true; return false; });
            var k = EnumConversions.ToDeclarationKind(node.Kind());



            if (item == null)

            {
                var a1 = new TypeNode(BoundKind.TypeExpression, node) {type=k, Namespace = ns, Name = class_name };
              
                a1.Modifiers = node.Modifiers.ToDeclarationModifiers(zone.diag);

                type_cache.Add(a1);
                type = a1;
                return true;
            }
            else
            {
                if (!b2)
                {
                    AddError(ErrorCode.ERR_DuplicateBound, node.Location);
                    return false;
                }

                //  throw new Exception("存在相同的类");
                if (item.type != k)
                {
                    AddError(ErrorCode.ERR_BadModifiersOnNamespace, node.Location);
                    return false;
                }
                if (item.Modifiers!=node.Modifiers.ToDeclarationModifiers(zone.diag))
                {
                    AddError(ErrorCode.ERR_BadModifiersOnNamespace, node.Location);
                    return false;
                }
                type = item;
                return true;
            }

           

        }
        private bool Have_mod(TypeDeclarationSyntax node, DeclarationModifiers de)
        {

            var w1= node.Modifiers.ToDeclarationModifiers(zone.diag);
            if((de&w1)!=0)
            {
                return true;
            }

            return false;

           // if(w1.)
        }
    }
}

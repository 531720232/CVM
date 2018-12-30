namespace CVM.Runtime
{
    public    class CVM_AppDomain
    {
        //public  delegate object CLRRedirectionDelegate(ILIntepreter intp, object esp, IList<object> mStack, CLRMethod method, bool isNewObj);

    //    Dictionary<System.Reflection.MethodBase, CLRRedirectionDelegate> redirectMap = new Dictionary<System.Reflection.MethodBase, CLRRedirectionDelegate>();

        public CVM_AppDomain()
        {
            //foreach (var i in typeof(System.Activator).GetMethods())
            //{
            //    if (i.Name == "CreateInstance" && i.IsGenericMethodDefinition)
            //    {
            //        RegisterCLRMethodRedirection(i, CLRRedirections.CreateInstance);
            //    }
            //    else if (i.Name == "CreateInstance" && i.GetParameters().Length == 1)
            //    {
            //        RegisterCLRMethodRedirection(i, CLRRedirections.CreateInstance2);
            //    }
            //    else if (i.Name == "CreateInstance" && i.GetParameters().Length == 2)
            //    {
            //        RegisterCLRMethodRedirection(i, CLRRedirections.CreateInstance3);
            //    }
            //}

        }
        //    public void RegisterCLRMethodRedirection(MethodBase mi, CLRRedirectionDelegate func)
        //{
        //    if (!redirectMap.ContainsKey(mi))
        //        redirectMap[mi] = func;
        //}
       
    }
}

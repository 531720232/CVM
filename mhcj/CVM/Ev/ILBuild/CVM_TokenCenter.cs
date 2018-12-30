namespace Microsoft.CodeAnalysis.CodeGen
{
    internal   class CVM_TokenCenter
    {
      static  CVM_TokenCenter center;
       internal static CVM_TokenCenter Instance
        {
            get
            {
                if(center==null)
                {
                    center = new CVM_TokenCenter();
                }
                return center;
            }
        }
        protected CVM_TokenCenter()
        {
            str = new string[1];//= new List<string>();
            tok = 0;
        }
        private string[] str;
        private uint tok;
     
    }
}

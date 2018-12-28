using System.Collections.Generic;

namespace CVM
{
    /// <summary>
    /// 全局预处理器
    /// </summary>
    public   class GlobalDefine
    {

        public List<object> ils = new List<object>();
        public List<object> ils2 = new List<object>();

        private static GlobalDefine g1;
        public static  GlobalDefine Instance
        {

            get
            {
                if(g1==null)
                {
                    g1 = new GlobalDefine();
                }
                return g1;
            }
        }
        public List<string> defines { get; private set; }
        public GlobalDefine()
        {
            defines = new List<string>();
            AutoReg();
          var bd=  clr_types.Contains(typeof( System.Data.DataColumn));
        }
        private void AutoReg()
        {
            var asd = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in asd)
            {
                foreach (var b in a.GetTypes())
                {
                    RegType(b);
                }
            }
        }
        public void InDebug()
        {
            defines.Add("DEBUG");
            defines.Add("TRACE");

        }
        public void AddDefine(string a)
        {
            defines.Add(a);
        }
        public void UnDefine(string a)
        {
            defines.RemoveAll(x=> {return x == a; });
        }

        public List<System.Type> clr_types = new List<System.Type>();
        public void RegType(System.Type type)
        {
            if(!clr_types.Contains(type))
            {
                clr_types.Add(type);
            }
        }
    }
}

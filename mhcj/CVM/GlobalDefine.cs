using System.Collections.Generic;

namespace CVM
{
    /// <summary>
    /// 全局预处理器
    /// </summary>
    public   class GlobalDefine
    {
        public static readonly GlobalDefine Instance = new GlobalDefine();
        public List<string> defines { get; private set; }
        public GlobalDefine()
        {
            defines = new List<string>();
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
    }
}

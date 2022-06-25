using System.Collections.Generic;

namespace SLBr
{
    public class URLScheme
    {
        public string Name;
        public string RootFolder = "Resources";
        public List<Scheme> Schemes;
        public bool IsStandard = false;
        public bool IsSecure = true;
        public bool IsLocal = true;

        public class Scheme
        {
            public string PageName;
            public string FileName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Controls
{
    public class UrlScheme
    {
        public string Name;
        public string RootFolder = "Resources";
        public List<Scheme> Schemes;
        public bool IsStandard = false;
        public bool IsSecure = true;
        public bool IsLocal = true;
        public bool IsCorsEnabled = false;

        public class Scheme
        {
            public string PageName;
            public string FileName;
        }
    }
}

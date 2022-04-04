using CefSharp;
using System;

namespace SLBr_Lite
{
    public class SchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new SchemeHandler();
        }
    }
}

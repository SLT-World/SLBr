using CefSharp;
using SLBr.Protocols;

namespace SLBr.Handlers
{
    /*public class IPFSSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new IPFSSchemeHandler();
        }
    }
    public class IPNSSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new IPNSSchemeHandler();
        }
    }*/
    public class GeminiSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public static GeminiSchemeHandlerFactory Instance;
        public TextGemini TextGeminiInstance;

        public GeminiSchemeHandlerFactory()
        {
            TextGeminiInstance = new TextGemini();
        }
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new GeminiSchemeHandler();
        }
    }
    public class GopherSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public static GopherSchemeHandlerFactory Instance;
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new GopherSchemeHandler();
        }
    }
}

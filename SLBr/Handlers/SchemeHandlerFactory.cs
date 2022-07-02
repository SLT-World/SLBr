
using CefSharp;

namespace SLBr
{
    public class BlankSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new BlankSchemeHandler();
        }
    }
}

using CefSharp.Internals;
using CefSharp;
using System.Collections.Concurrent;

namespace SLBr.Handlers
{
    public class ResourceRequestHandlerFactory : IResourceRequestHandlerFactory
    {
        public ConcurrentDictionary<string, SLBrResourceRequestHandlerFactoryItem> Handlers { get; private set; }

        RequestHandler Handler;

        public ResourceRequestHandlerFactory(RequestHandler _Handler, IEqualityComparer<string> Comparer = null)
        {
            Handler = _Handler;
            Handlers = new ConcurrentDictionary<string, SLBrResourceRequestHandlerFactoryItem>(Comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        public virtual bool RegisterHandler(string Url, byte[] Data, string MimeType = ResourceHandler.DefaultMimeType/*, bool limitedUse = false*/, int Uses = 1, string Error = "")
        {
            if (Uri.TryCreate(Url, UriKind.Absolute, out Uri URI))
            {
                var _Entry = new SLBrResourceRequestHandlerFactoryItem(Data, MimeType/*, limitedUse*/, Uses, Error);
                Handlers.AddOrUpdate(URI.AbsoluteUri, _Entry, (k, v) => _Entry);
                return true;
            }
            return false;
        }

        public virtual bool UnregisterHandler(string Url)
        {
            return Handlers.TryRemove(Url, out _);
        }

        bool IResourceRequestHandlerFactory.HasHandlers
        {
            get { return Handlers.Count > 0; }
        }

        IResourceRequestHandler IResourceRequestHandlerFactory.GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }

        public class SLBrResourceRequestHandlerFactoryItem
        {
            public byte[] Data;
            public string MimeType;
            //public bool LimitedUse;
            public int Uses;
            public string Error;

            public SLBrResourceRequestHandlerFactoryItem(byte[] _Data, string _MimeType, /*bool limitedUse, */int _Uses = 1, string _Error = "")
            {
                Data = _Data;
                MimeType = _MimeType;
                //LimitedUse = limitedUse;
                Uses = _Uses;
                Error = _Error;
            }
        }

        protected virtual IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            try
            {
                if (Handlers.TryGetValue(request.Url, out SLBrResourceRequestHandlerFactoryItem Entry))
                {
                    if (Entry.Uses != -1)
                    {
                        Entry.Uses -= 1;
                        if (Entry.Uses == 0)
                            Handlers.TryRemove(request.Url, out Entry);
                    }
                    return new InMemoryResourceRequestHandler(Entry.Data, Entry.MimeType);
                }
                return new ResourceRequestHandler(Handler);
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}

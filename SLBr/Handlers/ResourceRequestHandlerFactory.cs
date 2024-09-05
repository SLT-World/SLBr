using CefSharp.Internals;
using CefSharp;
using System.Collections.Concurrent;

namespace SLBr.Handlers
{
    public class ResourceRequestHandlerFactory : IResourceRequestHandlerFactory
    {
        public ConcurrentDictionary<string, SLBrResourceRequestHandlerFactoryItem> Handlers { get; private set; }

        RequestHandler Handler;

        public ResourceRequestHandlerFactory(RequestHandler _Handler, IEqualityComparer<string> comparer = null)
        {
            Handler = _Handler;
            Handlers = new ConcurrentDictionary<string, SLBrResourceRequestHandlerFactoryItem>(comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        public virtual bool RegisterHandler(string url, byte[] data, string mimeType = ResourceHandler.DefaultMimeType/*, bool limitedUse = false*/, int uses = 1, string error = "")
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri URI))
            {
                var entry = new SLBrResourceRequestHandlerFactoryItem(data, mimeType/*, limitedUse*/, uses, error);
                Handlers.AddOrUpdate(URI.AbsoluteUri, entry, (k, v) => entry);
                return true;
            }
            return false;
        }

        public virtual bool UnregisterHandler(string url)
        {
            return Handlers.TryRemove(url, out _);
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

            public SLBrResourceRequestHandlerFactoryItem(byte[] data, string mimeType, /*bool limitedUse, */int uses = 1, string _Error = "")
            {
                Data = data;
                MimeType = mimeType;
                //LimitedUse = limitedUse;
                Uses = uses;
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
                        Entry.Uses -= 1;
                    //if (Entry.LimitedUse && Entry.Uses == 0)
                    if (Entry.Uses == 0)
                        Handlers.TryRemove(request.Url, out Entry);
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

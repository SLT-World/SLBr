/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using Microsoft.Web.WebView2.Core;

namespace SLBr.WebView
{
    public interface IWebCookieManager : IDisposable
    {
        Task<List<IWebCookie>> GetCookies(string Url);
        Task SetCookie(string Url, IWebCookie Cookie);
        Task DeleteCookie(string Url, string Name);
        Task DeleteAll(string? Url = null);
    }
    public enum WebCookieSameSite
    {
        Unspecified = 0,
        None = 1,
        Lax = 2,
        Strict = 3
    }
    public enum WebCookiePriority
    {
        Low = -1,
        Medium = 0,
        High = 1
    }

    public class ChromiumCookieManager(ICookieManager _Core) : IWebCookieManager
    {
        ICookieManager Core = _Core;

        public async Task<List<IWebCookie>> GetCookies(string Url)
        {
            List<Cookie> CoreCookiesList = await Core.VisitUrlCookiesAsync(Url, true);
            List<IWebCookie> Cookies = [];
            foreach (Cookie CefCookie in CoreCookiesList)
                Cookies.Add(new ChromiumWebCookie(CefCookie));
            return Cookies;
        }

        public async Task SetCookie(string Url, IWebCookie Cookie)
        {
            if (Cookie is ChromiumWebCookie ChromiumCookie)
                await Core.SetCookieAsync(Url, ChromiumCookie.BaseCookie);
        }

        public async Task DeleteCookie(string Url, string Name)
        {
            await Core.DeleteCookiesAsync(Url, Name);
        }

        public async Task DeleteAll(string? Url = null)
        {
            await Core.DeleteCookiesAsync(Url);
        }

        public void Dispose()
        {
        }
    }

    public class ChromiumEdgeCookieManager(CoreWebView2CookieManager _Core) : IWebCookieManager
    {
        CoreWebView2CookieManager Core = _Core;

        public async Task<List<IWebCookie>> GetCookies(string Url)
        {
            List<CoreWebView2Cookie> CoreCookiesList = await Core.GetCookiesAsync(Url);
            List<IWebCookie> Cookies = [];
            foreach (CoreWebView2Cookie CoreCookie in CoreCookiesList)
                Cookies.Add(new ChromiumEdgeWebCookie(CoreCookie));
            return Cookies;
        }

        public async Task SetCookie(string Url, IWebCookie Cookie)
        {
            if (Cookie is ChromiumEdgeWebCookie ChromiumCookie)
                Core.AddOrUpdateCookie(ChromiumCookie.Core);
        }

        public async Task DeleteCookie(string Url, string Name)
        {
            Core.DeleteCookies(Name, Url);
        }

        public async Task DeleteAll(string? Url = null)
        {
            if (Url == null)
                Core.DeleteAllCookies();
            else
            {
                List<CoreWebView2Cookie> CoreCookiesList = await Core.GetCookiesAsync(Url);
                foreach (CoreWebView2Cookie CoreCookie in CoreCookiesList)
                    Core.DeleteCookie(CoreCookie);
            }
        }

        public void Dispose()
        {
        }
    }

    public class TridentCookieManager : IWebCookieManager
    {
        //TODO: InternetGetCookieEx
        public async Task<List<IWebCookie>> GetCookies(string Url)
        {
            return [];
        }

        public async Task SetCookie(string Url, IWebCookie Cookie)
        {
        }

        public async Task DeleteCookie(string Url, string Name)
        {
        }

        public async Task DeleteAll(string? Url = null)
        {
        }

        public void Dispose()
        {
        }
    }

    public interface IWebCookie
    {
        string Name { get; }
        string Value { get; set; }
        string Domain { get; }
        string Path { get; }
        bool Secure { get; set; }
        bool HttpOnly { get; set; }
        DateTime Expires { get; set; }
        WebCookieSameSite SameSite { get; set; }
        //WebCookiePriority Priority { get; set; }
    }

    public class ChromiumWebCookie(Cookie _BaseCookie) : IWebCookie
    {
        public Cookie BaseCookie = _BaseCookie;
        public string Name
        {
            get { return BaseCookie.Name; }
        }
        public string Value
        {
            get { return BaseCookie.Value; }
            set { BaseCookie.Value = value; }
        }
        public string Domain
        {
            get { return BaseCookie.Domain; }
        }
        public string Path
        {
            get { return BaseCookie.Path; }
        }
        public bool Secure
        {
            get { return BaseCookie.Secure; }
            set { BaseCookie.Secure = value; }
        }
        public bool HttpOnly
        {
            get { return BaseCookie.HttpOnly; }
            set { BaseCookie.HttpOnly = value; }
        }
        public DateTime Expires
        {
            get { return BaseCookie.Expires ?? DateTime.MinValue; }
            set { BaseCookie.Expires = value; }
        }
        public WebCookieSameSite SameSite
        {
            get { return BaseCookie.SameSite.ToWebCookieSameSite(); }
            set { BaseCookie.SameSite = value.ToCefCookieSameSite(); }
        }
    }

    public class ChromiumEdgeWebCookie(CoreWebView2Cookie _BaseCookie) : IWebCookie
    {
        public CoreWebView2Cookie Core = _BaseCookie;
        public string Name
        {
            get { return Core.Name; }
        }
        public string Value
        {
            get { return Core.Value; }
            set { Core.Value = value; }
        }
        public string Domain
        {
            get { return Core.Domain; }
        }
        public string Path
        {
            get { return Core.Path; }
        }
        public bool Secure
        {
            get { return Core.IsSecure; }
            set { Core.IsSecure = value; }
        }
        public bool HttpOnly
        {
            get { return Core.IsHttpOnly; }
            set { Core.IsHttpOnly = value; }
        }
        public DateTime Expires
        {
            get { return Core.Expires; }
            set { Core.Expires = value; }
        }
        public WebCookieSameSite SameSite
        {
            get { return Core.SameSite.ToWebCookieSameSite(); }
            set { Core.SameSite = value.ToWebView2CookieSameSite(); }
        }
    }
}

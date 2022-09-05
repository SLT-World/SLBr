using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;

namespace SLBr.Controls
{
    public class IEWebBrowser
    {
        public IEWebBrowser(string Url)
        {
            BrowserCore = new WebBrowser();
            BrowserCore.Navigating += Navigating;
            BrowserCore.Navigated += Navigated;
            Navigate(Url);
        }

        private void Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            IsLoading = false;
        }
        private void Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            IsLoading = true;
        }

        public void Navigate(string Url) =>
            BrowserCore.Navigate(Url);

        /*public async void Navigate(string Url)
        {
            await NavigateAsync(Url);
        }

        private async Task NavigateAsync(string Url)//, int Timeout
        {
            using (System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(0, 1))
            {
                bool loaded = false;
                BrowserCore.LoadCompleted += (s, e) =>
                {
                    semaphore.Release();
                    loaded = true;
                };
                BrowserCore.Navigate(Url);

                //await semaphore.WaitAsync(TimeSpan.FromSeconds(Timeout));

                IsLoading = !loaded;
            }
        }*/

        public bool IsLoading;
        public WebBrowser BrowserCore;

        public void Dispose()
        {
            BrowserCore.Dispose();
        }
    }
}

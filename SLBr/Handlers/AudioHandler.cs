using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using SLBr.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace SLBr.Handlers
{
    public class AudioHandler : IAudioHandler
    {
        private bool isDisposed;

        public bool IsDisposed
        {
            get { return isDisposed; }
        }

        bool IAudioHandler.GetAudioParameters(IWebBrowser chromiumWebBrowser, IBrowser browser, ref AudioParameters parameters)
        {
            return GetAudioParameters(chromiumWebBrowser, browser, ref parameters);
        }

        protected virtual bool GetAudioParameters(IWebBrowser chromiumWebBrowser, IBrowser browser, ref AudioParameters parameters)
        {
            return false;
        }
        void IAudioHandler.OnAudioStreamStarted(IWebBrowser chromiumWebBrowser, IBrowser browser, AudioParameters parameters, int channels)
        {
            OnAudioStreamStarted(chromiumWebBrowser, browser, parameters, channels);
        }

        protected virtual void OnAudioStreamStarted(IWebBrowser chromiumWebBrowser, IBrowser browser, AudioParameters parameters, int channels)
        {
            AudioPlaying = true;
            MessageBox.Show(AudioPlaying.ToString());
        }

        void IAudioHandler.OnAudioStreamPacket(IWebBrowser chromiumWebBrowser, IBrowser browser, IntPtr data, int noOfFrames, long pts)
        {
            OnAudioStreamPacket(chromiumWebBrowser, browser, data, noOfFrames, pts);
        }

        protected virtual void OnAudioStreamPacket(IWebBrowser chromiumWebBrowser, IBrowser browser, IntPtr data, int noOfFrames, long pts)
        {
        }

        void IAudioHandler.OnAudioStreamStopped(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            OnAudioStreamStopped(chromiumWebBrowser, browser);
        }
        protected virtual void OnAudioStreamStopped(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            AudioPlaying = false;
            MessageBox.Show(AudioPlaying.ToString());
        }

        void IAudioHandler.OnAudioStreamError(IWebBrowser chromiumWebBrowser, IBrowser browser, string errorMessage)
        {
            OnAudioStreamError(chromiumWebBrowser, browser, errorMessage);
        }

        protected virtual void OnAudioStreamError(IWebBrowser chromiumWebBrowser, IBrowser browser, string errorMessage)
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            isDisposed = true;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public bool AudioPlaying;
    }
}

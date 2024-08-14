using CefSharp;
using CefSharp.Enums;
using CefSharp.Structs;
using Microsoft.VisualBasic;
using SLBr.Pages;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using static SLBr.Handlers.AudioHandler;

namespace SLBr.Handlers
{
    public class AudioHandler : IAudioHandler
    {
        private bool isDisposed;
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
        protected virtual void Dispose(bool disposing)
        {
            isDisposed = true;
        }

        Browser BrowserView;
        private const int WAVE_FORMAT_PCM = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct WaveFormat
        {
            public short wFormatTag;
            public short nChannels;
            public int nSamplesPerSec;
            public int nAvgBytesPerSec;
            public short nBlockAlign;
            public short wBitsPerSample;
            public short cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WaveHeader
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        public delegate void WaveDelegate();

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, WaveFormat lpFormat, WaveDelegate dwCallback, int dwInstance, int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        private IntPtr waveOutHandle;
        private WaveHeader waveHeader;

        public AudioHandler(Browser _BrowserView)
        {
            BrowserView = _BrowserView;
            waveOutHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            waveOutClose(waveOutHandle);
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool GetAudioParameters(IWebBrowser chromiumWebBrowser, IBrowser browser, ref AudioParameters parameters)
        {
            /*parameters = new AudioParameters
            {
                SampleRate = 44100,
                ChannelLayout = ChannelLayout.LayoutStereo,
                FramesPerBuffer = 1024,
            };*/
            return true;
        }
        public void OnAudioStreamError(IWebBrowser chromiumWebBrowser, IBrowser browser, string errorMessage)
        {
        }
        private int ChannelCount;
        public void OnAudioStreamPacket(IWebBrowser chromiumWebBrowser, IBrowser browser, IntPtr data, int noOfFrames, long pts)
        {
            if (noOfFrames < 2)
                return;
            int bytesPerSample = 2;
            int size = ChannelCount * noOfFrames * bytesPerSample;
            byte[] samples = new byte[size];

            unsafe
            {
                float** channelData = (float**)data.ToPointer();

                fixed (byte* pDestByte = samples)
                {
                    short* pDest = (short*)pDestByte;

                    for (int i = 0; i < noOfFrames; i++)
                    {
                        for (int c = 0; c < ChannelCount; c++)
                        {
                            float sample = channelData[c][i];
                            *pDest++ = (short)(sample * 32767.0f);
                        }
                    }
                }
            }

            GCHandle hSamples = GCHandle.Alloc(samples, GCHandleType.Pinned);
            waveHeader = new WaveHeader()
            {
                lpData = hSamples.AddrOfPinnedObject(),
                dwBufferLength = (uint)samples.Length,
                dwFlags = 0,
                dwLoops = 1
            };

            waveOutPrepareHeader(waveOutHandle, ref waveHeader, Marshal.SizeOf(typeof(WaveHeader)));
            waveOutWrite(waveOutHandle, ref waveHeader, Marshal.SizeOf(typeof(WaveHeader)));

            hSamples.Free();
        }
        public void OnAudioStreamStarted(IWebBrowser chromiumWebBrowser, IBrowser browser, AudioParameters parameters, int channels)
        {
            ChannelCount = channels;
            var format = new WaveFormat
            {
                wFormatTag = WAVE_FORMAT_PCM,
                nChannels = (short)channels,
                nSamplesPerSec = parameters.SampleRate,
                nAvgBytesPerSec = parameters.SampleRate * channels * 2,
                nBlockAlign = (short)(channels * 2),
                wBitsPerSample = 16,
                cbSize = 0
            };

            waveOutOpen(out waveOutHandle, -1, format, null, 0, 0);
            BrowserView.SetAudioState(true);
        }

        public void OnAudioStreamStopped(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            waveOutClose(waveOutHandle);
            BrowserView.SetAudioState(false);
        }
    }
}

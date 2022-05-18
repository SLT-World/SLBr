using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace SLBr
{
    /* class ChromiumWebBrowser : CefSharp.Wpf.ChromiumWebBrowser
	{
		public ChromiumWebBrowser() : base()
		{
		}
		public ChromiumWebBrowser(string path) : base(path)
		{
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);*/

		/*protected override void WndProc(ref Message m)
		{
			const int WM_PARENTNOTIFY = 0x0210;
			const int WM_NCLBUTTONDOWN = 0x00A1;
			//Console.WriteLine(m.Msg);
			if (m.Msg == WM_PARENTNOTIFY)
			{
				PostMessage(Parent.Handle, WM_NCLBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
			}
			base.WndProc(ref m);
		}*/
	//}
}

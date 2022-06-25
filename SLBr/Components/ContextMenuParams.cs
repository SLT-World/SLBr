using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr
{
    class ContextMenuParams : IContextMenuParams
    {
        public ContextMenuParams(IContextMenuParams Parameters)
        {
            YCoord = Parameters.YCoord;
            XCoord = Parameters.XCoord;
            TypeFlags = Parameters.TypeFlags;
            LinkUrl = Parameters.LinkUrl;
            UnfilteredLinkUrl = Parameters.UnfilteredLinkUrl;
            SourceUrl = Parameters.SourceUrl;
            HasImageContents = Parameters.HasImageContents;
            PageUrl = Parameters.PageUrl;
            FrameUrl = Parameters.FrameUrl;
            FrameCharset = Parameters.FrameCharset;
            MediaType = Parameters.MediaType;
            MediaStateFlags = Parameters.MediaStateFlags;
            SelectionText = Parameters.SelectionText;
            MisspelledWord = Parameters.MisspelledWord;
            DictionarySuggestions = Parameters.DictionarySuggestions;
            IsEditable = Parameters.IsEditable;
            IsSpellCheckEnabled = Parameters.IsSpellCheckEnabled;
            EditStateFlags = Parameters.EditStateFlags;
            IsCustomMenu = Parameters.IsCustomMenu;
            IsDisposed = Parameters.IsDisposed;
        }

        private bool disposedValue;

        public int YCoord { get; set; }

        public int XCoord { get; set; }

        public ContextMenuType TypeFlags { get; set; }

        public string LinkUrl { get; set; }

        public string UnfilteredLinkUrl { get; set; }

        public string SourceUrl { get; set; }

        public bool HasImageContents { get; set; }

        public string PageUrl { get; set; }

        public string FrameUrl { get; set; }

        public string FrameCharset { get; set; }

        public ContextMenuMediaType MediaType { get; set; }

        public ContextMenuMediaState MediaStateFlags { get; set; }

        public string SelectionText { get; set; }

        public string MisspelledWord { get; set; }

        public List<string> DictionarySuggestions { get; set; }

        public bool IsEditable { get; set; }

        public bool IsSpellCheckEnabled { get; set; }

        public ContextMenuEditState EditStateFlags { get; set; }

        public bool IsCustomMenu { get; set; }

        public bool IsDisposed { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ContextMenuParams()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

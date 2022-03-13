// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr
{
    public class FileAssociation
    {
        public string Extension { get; set; }
        public string ProgId { get; set; }
        public string FileTypeDescription { get; set; }
        public string ExecutableFilePath { get; set; }
    }

    public class FileAssociations
    {
        // needed so that Explorer windows get refreshed after the registry is updated
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        public static void EnsureAssociationsSet()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                /*new FileAssociation
                {
                    Extension = ".htm",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },*/
                new FileAssociation
                {
                    Extension = ".html",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                }
                /*new FileAssociation
                {
                    Extension = ".shtml",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },
                /*new FileAssociation
                {
                    Extension = ".mht",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },
                /*new FileAssociation
                {
                    Extension = ".mhtml",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },
                /*new FileAssociation
                {
                    Extension = ".xht",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },
                /*new FileAssociation
                {
                    Extension = ".xhtml",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr HTML Document",
                    ExecutableFilePath = filePath
                },
                new FileAssociation
                {
                    Extension = ".php",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr PHP Document",
                    ExecutableFilePath = filePath
                },
                new FileAssociation
                {
                    Extension = ".pdf",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr PDF Document",
                    ExecutableFilePath = filePath
                },*/
                /*new FileAssociation
                {
                    Extension = ".asp",
                    ProgId = "SLBr",
                    FileTypeDescription = "SLBr ASP Document",
                    ExecutableFilePath = filePath
                },
                new FileAssociation
                {
                    Extension = "FTP",
                    ProgId = "SLBr",
                    FileTypeDescription = "URL:File Transfer Protocol",
                    ExecutableFilePath = filePath
                },*/
                /*new FileAssociation
                {
                    Extension = "HTTP",
                    ProgId = "SLBr",
                    FileTypeDescription = "URL:HyperText Transfer Protocol",
                    ExecutableFilePath = filePath
                },
                new FileAssociation
                {
                    Extension = "HTTPS",
                    ProgId = "SLBr",
                    FileTypeDescription = "URL:HyperText Transfer Protocol with Privacy",
                    ExecutableFilePath = filePath
                }*/
                );
        }

        public static void EnsureAssociationsSet(params FileAssociation[] associations)
        {
            bool madeChanges = false;
            foreach (var association in associations)
            {
                madeChanges |= SetAssociation(
                    association.Extension,
                    association.ProgId,
                    association.FileTypeDescription,
                    association.ExecutableFilePath);
            }

            if (madeChanges)
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool SetAssociation(string extension, string progId, string fileTypeDescription, string applicationFilePath)
        {
            /*bool madeChanges = false;
            madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
            madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
            return madeChanges;*/
            bool madeChanges = false;
            //madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
            //madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
            //madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", $"\"{applicationFilePath}\" %1");/*\"*//*\"*/
            //madeChanges |= SetKeyDefaultValue($@"Software\Classes\{extension}\shell\open\command", $"\"{applicationFilePath}\" \"%1\"");
            madeChanges |= NewSetKeyDefaultValue(".html", "SLBr");
            return madeChanges;
        }

        private static bool NewSetKeyDefaultValue(string ExtensionName, string ApplicationName, string Value = "")
        {
            RegistryKey _rk = Registry.ClassesRoot.OpenSubKey(ExtensionName);
            //string _defaultapp = _rk.GetValue("").ToString();
            string[] _subkeys = _rk.GetSubKeyNames();
            for (int i = 0; i < _subkeys.Length; i++)
            {
                if (_subkeys[i] == "OpenWithProgids")
                {
                    RegistryKey _rkh = _rk.OpenSubKey(_subkeys[i],  true);
                    _rkh.SetValue(ApplicationName, Value);
                    return true;
                    //string[] _names = _rkh.GetValueNames();
                    //for (int j = 0; j < _names.Length; j++)
                    //{
                    //    if (_names[j] == "")
                    //        continue;

                    //    Microsoft.Win32.RegistryKey _rhelp = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(_names[j] + "\\shell\\open\\command");

                    //    _ret.Add(_rhelp.GetValue("").ToString());
                    //    _rhelp.Close();
                    //}

                }
            }
            _rk.Close();

            return false;
        }

        private static bool OldSetKeyDefaultValue(string keyPath, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key.GetValue(null) as string != value)
                {
                    key.SetValue(null, value);
                    return true;
                }
            }

            return false;
        }
    }
}
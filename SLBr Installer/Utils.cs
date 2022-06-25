using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SLBr_Installer
{
    internal class Utils
    {
        public static bool CheckForInternetConnection(int timeoutMs = 1500, string url = null)
        {
            try
            {
                url = "http://www.gstatic.com/generate_204";

                switch (CultureInfo.InstalledUICulture.Name)
                {
                    case string s when s.StartsWith("fa"):
                        url = "http://www.aparat.com";
                        break;
                    case string s when s.StartsWith("zh"):
                        url = "http://www.baidu.com";
                        break;
                };

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public class Saving
        {
            string KeySeparator = "<,>";
            string ValueSeparator = "<|>";
            string KeyValueSeparator = "<:>";
            Dictionary<string, string> Data = new Dictionary<string, string>();
            public string SaveFolderPath;
            public string SaveFilePath;
            public bool UseContinuationIndex;

            public Saving(bool Custom = false, string FileName = "Save2.bin", string FolderPath = "EXECUTINGASSEMBLYFOLDERPATHUTILSSAVING")
            {
                if (Custom)
                {
                    if (FolderPath != "EXECUTINGASSEMBLYFOLDERPATHUTILSSAVING")
                        SaveFolderPath = FolderPath;
                    else
                        SaveFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    SaveFilePath = Path.Combine(SaveFolderPath, FileName);
                }
                else
                    SaveFilePath = Path.Combine(SaveFolderPath, "Save.bin");
                Load();
            }

            public bool Has(string Key, bool IsValue = false)
            {
                //Load();
                if (IsValue)
                    return Data.ContainsValue(Key);
                return Data.ContainsKey(Key);
            }
            public void Remove(string Key) =>
                Data.Remove(Key);
            public void Set(string Key, string Value, bool _Save = true)
            {
                Data[Key] = Value;
                if (_Save)
                    Save();
            }
            public void Set(string Key, string Value_1, string Value_2, bool _Save = true)
            {
                string Value = string.Join(ValueSeparator, Value_1, Value_2);
                Set(Key, Value, _Save);
            }
            public string Get(string Key)
            {
                //Load();
                if (Has(Key))
                    return Data[Key];

                return "NOTFOUND";
            }
            public string[] Get(string Key, bool UseListParameter)
            {
                return Get(Key).Split(new[] { ValueSeparator }, StringSplitOptions.None);
            }
            public void Clear() =>
                Data.Clear();

            public void Save()
            {
                if (!Directory.Exists(SaveFolderPath))
                    Directory.CreateDirectory(SaveFolderPath);

                if (!File.Exists(SaveFilePath))
                    File.Create(SaveFilePath).Close();
                HashSet<string> Contents = new HashSet<string>();
                foreach (KeyValuePair<string, string> KVP in Data)
                    Contents.Add(KVP.Key + KeyValueSeparator + KVP.Value);
                File.WriteAllText(SaveFilePath, string.Join(KeySeparator, Contents));
            }
            public void Load()
            {
                if (!Directory.Exists(SaveFolderPath))
                    Directory.CreateDirectory(SaveFolderPath);
                if (!File.Exists(SaveFilePath))
                    File.Create(SaveFilePath).Close();

                //FastHashSet<string> Contents = new FastHashSet<string>(File.ReadAllText(SaveFilePath).Split(new string[] { KeySeparator }, StringSplitOptions.None));
                HashSet<string> Contents = File.ReadAllText(SaveFilePath).Split(new string[] { KeySeparator }, StringSplitOptions.None).ToHashSet();
                foreach (string Content in Contents)
                {
                    if (string.IsNullOrWhiteSpace(Content))
                        continue;
                    string[] Values = Content.Split(new string[] { KeyValueSeparator }, 2, StringSplitOptions.None);
                    Data[Values[0]] = Values[1];
                }
            }
        }
    }
}

using CefSharp.DevTools.Profiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for ProfileManagerWindow.xaml
    /// </summary>
    public partial class ProfileManagerWindow : Window
    {
        public ProfileManagerWindow()
        {
            InitializeComponent();
            ProfilesList.ItemsSource = App.Instance.Profiles;
            ApplyTheme(App.Instance.CurrentTheme);
            StartupProfilesCheckBox.IsChecked = bool.Parse(App.Instance.AppSave.Get("StartupProfiles"));
        }

        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle());
            int trueValue = 0x01;
            int falseValue = 0x00;
            if (_Theme.DarkTitleBar)
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DllUtils.DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));
        }

        private void StartupProfilesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.AppSave.Set("StartupProfiles", StartupProfilesCheckBox.IsChecked.ToBool());
            App.Instance.AppSave.Save();
        }

        private void OpenProfile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border _Border && _Border.DataContext is Profile _Profile)
                OpenProfile(_Profile);
        }

        private void OpenProfile(Profile _Profile)
        {
            if (App.Instance.AppInitialized)
            {
                if (App.Instance.CurrentProfile.Name == _Profile.Name && App.Instance.CurrentProfile.Type == _Profile.Type)
                {
                    MainWindow CurrentWindow = App.Instance.CurrentFocusedWindow();
                    CurrentWindow.WindowState = WindowState.Normal;
                    CurrentWindow.Activate();
                }
                else
                {
                    if (_Profile.Type == ProfileType.User)
                        Process.Start(new ProcessStartInfo() { FileName = App.Instance.ExecutablePath, Arguments = $"--user={_Profile.Name}" });
                    else if (_Profile.Name == "Guest")
                        Process.Start(new ProcessStartInfo() { FileName = App.Instance.ExecutablePath, Arguments = $"--guest" });
                }
            }
            else
            {
                Hide();
                App.Instance.InitializeApp(Environment.GetCommandLineArgs().Skip(1), _Profile);
            }
            Close();
        }

        private void NewProfile(object sender, MouseButtonEventArgs e)
        {
            DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Add Profile", new List<InputField> { new InputField { Name = "Username", IsRequired = true, Type = DialogInputType.Text, Value = "" } }, "\xE77B");
            _DynamicDialogWindow.Topmost = true;
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                string Input = _DynamicDialogWindow.InputFields[0].Value.Trim();
                Profile NewProfile = App.Instance.Profiles.FirstOrDefault(i => i.Name == Input);
                if (NewProfile == null)
                {
                    bool HasDefault = App.Instance.Profiles.Any(i => i.Default);
                    NewProfile = new Profile { Name = Input, Type = ProfileType.User, Default = !HasDefault };
                    App.Instance.Profiles.Insert(0, NewProfile);
                    if (!HasDefault)
                    {
                        App.Instance.AppSave.Set("Default", NewProfile.Name);
                        App.Instance.AppSave.Save();
                    }
                }
                OpenProfile(NewProfile);
            }
        }

        private void SetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is Profile _Profile)
            {
                foreach (Profile _FProfile in App.Instance.Profiles)
                    _FProfile.Default = false;
                _Profile.Default = true;
                App.Instance.AppSave.Set("Default", _Profile.Name);
                App.Instance.AppSave.Save();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button _Button && _Button.DataContext is Profile _Profile)
            {
                InformationDialogWindow InfoWindow = new("Warning", "Delete Profile", "This will permanently delete all profile data. Do you want to continue?", "\ue74d", "Yes", "No");
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    StartupManager.DisableStartup(_Profile.Name);
                    Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", _Profile.Name), true);
                    App.Instance.Profiles.Remove(_Profile);
                    if (_Profile.Default)
                    {
                        Profile? DefaultProfile = App.Instance.Profiles.FirstOrDefault(i => i.Type == ProfileType.User);
                        if (DefaultProfile != null)
                        {
                            DefaultProfile.Default = true;
                            App.Instance.AppSave.Set("Default", DefaultProfile.Name);
                        }
                        else
                            App.Instance.AppSave.Remove("Default");
                        App.Instance.AppSave.Save();
                    }
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Windows;
using Forms = System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Win32;


namespace UnitAlert
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly FileWatcher _fileWatcher;
        private UserSettings? _userSettings;
        private Forms.NotifyIcon? _notifyIcon;
        

        private readonly IsolatedStorageFile _isoStore =
            IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public MainWindow()
        {
            _fileWatcher = new FileWatcher();
            InitializeComponent();
            // If the config file exists, deserialize the json into an UserSettings object then load them.
            if (_isoStore.FileExists("unit_config.json"))
            {
                using var isoStream = new IsolatedStorageFileStream("unit_config.json", FileMode.Open, _isoStore);
                using var streamReader = new StreamReader(isoStream);
                _userSettings = JsonSerializer.Deserialize<UserSettings>(streamReader.ReadToEnd());
                LoadUserSettings();
            }
            DataContext = this;
        }

        private void LoadUserSettings()
        {
            // Loads the variables in _fileWatcher to match the user settings and also sets the UI.
            _fileWatcher.ChatlogFilePath = _userSettings?.ChatlogFilePath;
            _fileWatcher.SoundFilePath = _userSettings?.SoundFilePath;
            _fileWatcher.Unit = _userSettings?.LastUnit;
            ChatlogTextBox.Text = _fileWatcher.ChatlogFilePath;
            SoundFileTextBox.Text = _fileWatcher.SoundFilePath;
            UnitTextBox.Text = _fileWatcher.Unit;
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinButton_OnClick(object sender, RoutedEventArgs e)
        {
            // If the minimized button is clicked, set the window as minimized and disable it in the taskbar.
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            // Get the app icon from the Resources folder.
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/appicon.ico"))?.Stream;
            if (iconStream is null)
            {
                return;
            }
            // Create the notification icon and assign it to _notifyIcon
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = new Icon(iconStream);
            _notifyIcon.Text = "Unit Alert";
            _notifyIcon.Click += NotifyIcon_Click;
            _notifyIcon.Visible = true;

        }

        private void NotifyIcon_Click(object? sender, EventArgs e)
        {
            // If the notification icon is clicked, bring the window back, dispose the icon and show the taskbar.
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            _notifyIcon?.Dispose();
        }
        
        private void Border1_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Border1.Focus();
        }
        
        
        private void RagePickButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Opens a file dialog for TXT and gets the file path of the selected file.
            var openFileDialog = new OpenFileDialog {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            openFileDialog.ShowDialog();
            _fileWatcher.ChatlogFilePath = openFileDialog.FileName;
            ChatlogTextBox.Text = _fileWatcher.ChatlogFilePath;
            // Sets the variables to the filename and cancels any previous workers if there are any.
            _fileWatcher.CancelWatch();
        }
        
        private void SoundFilePickButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Opens a file dialog for the sound file then sets the UI and variables to it.
            var openFileDialog = new OpenFileDialog {
                Filter = "Sound files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };
            openFileDialog.ShowDialog();
            _fileWatcher.SoundFilePath = openFileDialog.FileName;
            SoundFileTextBox.Text = _fileWatcher.SoundFilePath;
        }


        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Clicking the reset buttons hides it, enabling all the UI.
            _fileWatcher.CancelWatch();
            ActivateButton.Visibility = Visibility.Visible;
            ResetButton.Visibility = Visibility.Collapsed;
            UnitTextBox.IsEnabled = true;
            SoundFileTextBox.IsEnabled = true;
            ChatlogTextBox.IsEnabled = true;
            RagePickButton.IsEnabled = true;
            SoundFilePickButton.IsEnabled = true;
            WindowChromeUnitText.Text = "";
        }

        private void ActivateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (UnitTextBox.Text.Length == 0)
            {
                MessageBox.Show("Unit not selected.");
                return;
            }
            if (string.IsNullOrEmpty(_fileWatcher.ChatlogFilePath))
            {
                MessageBox.Show("Chatlog file not selected.");
                return;
            }
            if (string.IsNullOrEmpty(_fileWatcher.SoundFilePath))
            {
                MessageBox.Show("Sound file not selected.");
                return;
            }
            
            // This block disables all the UI and changes the Select button to a Reset button
            _fileWatcher.Unit = UnitTextBox.Text;
            WindowChromeUnitText.Text = "- Active";
            ActivateButton.Visibility = Visibility.Collapsed;
            ResetButton.Visibility = Visibility.Visible;
            UnitTextBox.IsEnabled = false;
            SoundFileTextBox.IsEnabled = false;
            ChatlogTextBox.IsEnabled = false;
            RagePickButton.IsEnabled = false;
            SoundFilePickButton.IsEnabled = false;

            // If user settings is null, create a new one.
            _userSettings ??= new UserSettings();
            _userSettings.ChatlogFilePath = _fileWatcher.ChatlogFilePath;
            _userSettings.SoundFilePath = _fileWatcher.SoundFilePath;
            _userSettings.LastUnit = _fileWatcher.Unit;

            // Writes the _userSettings to an isolated json file.
            var jsonData = JsonSerializer.Serialize(_userSettings);
            using var isoStream = new IsolatedStorageFileStream("unit_config.json", FileMode.OpenOrCreate, _isoStore);
            isoStream.SetLength(0);
            using var streamWriter = new StreamWriter(isoStream);
            streamWriter.WriteLine(jsonData);
            
            // Starts the watcher
            _fileWatcher.StartWatch();
        }


    }
}
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Wave;

namespace UnitAlert
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _filePath;
        private string? _soundFilePath;
        private string? _unit;
        private readonly BackgroundWorker _worker = new();
        private readonly BackgroundWorker _soundWorker = new();
        private UserSettings? _userSettings;

        private readonly IsolatedStorageFile _isoStore =
            IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public MainWindow()
        {
            InitializeComponent();
            if (_isoStore.FileExists("unit_config.json"))
            {
                using var isoStream = new IsolatedStorageFileStream("unit_config.json", FileMode.Open, _isoStore);
                using var streamReader = new StreamReader(isoStream);
                _userSettings = JsonSerializer.Deserialize<UserSettings>(streamReader.ReadToEnd());
                LoadUserSettings();
            }
            _worker.DoWork += WatchFile;
            _worker.WorkerSupportsCancellation = true;
            _soundWorker.DoWork += PlaySound;
            DataContext = this;
        }

        private void LoadUserSettings()
        {
            _filePath = _userSettings?.ChatlogFilePath;
            _soundFilePath = _userSettings?.ChatlogFilePath;
            _unit = _userSettings?.LastUnit;
            ChatlogTextBox.Text = _filePath;
            SoundFileTextBox.Text = _soundFilePath;
            UnitTextBox.Text = _unit;
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinButton_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        private void Border1_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Border1.Focus();
        }
        
        private void WatchFile(object? sender, DoWorkEventArgs e)
        {
            // Gets the file path from the worker and check if it is not null.
            var filePath = (string?) e.Argument;
            if (filePath is null)
            {
                return;
            }
            // Opens a FileStream of that file, seeks to the end then starts reading from it.
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.End);
            var lastFileSize = fileStream.Length;
            using var streamReader = new StreamReader(fileStream);
            while (true)
            {
                // We check if the worker was cancelled
                if(_worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                // If the current file size is smaller than the last saved file size, seek to the end.
                if (fileStream.Length < lastFileSize)
                {
                    fileStream.Seek(0, SeekOrigin.End);
                }
                // Read a line and just skip the rest if it is null.
                var line = streamReader.ReadLine();
                if (line == null)
                {
                    continue;
                }
                
                Thread.Sleep(250);
                if (_unit != null && line.Contains(_unit))
                {
                    _soundWorker.RunWorkerAsync();
                }
                lastFileSize = fileStream.Length;
            }
            
        }

        private void PlaySound(object? sender, DoWorkEventArgs e)
        {
            using var audioFile = new AudioFileReader(_soundFilePath);
            using var outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
        
        private void RagePickButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            openFileDialog.ShowDialog();
            _filePath = openFileDialog.FileName;
            ChatlogTextBox.Text = _filePath;
            if (_worker.IsBusy)
            {
                _worker.CancelAsync();
            }
            
        }
        
        private void SoundFilePickButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog {
                Filter = "Sound files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };
            openFileDialog.ShowDialog();
            _soundFilePath = openFileDialog.FileName;
            SoundFileTextBox.Text = _soundFilePath;
        }


        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_worker.IsBusy)
            {
                _worker.CancelAsync();
            }

            SelectButton.Visibility = Visibility.Visible;
            ResetButton.Visibility = Visibility.Collapsed;
            UnitTextBox.IsEnabled = true;
            SoundFileTextBox.IsEnabled = true;
            ChatlogTextBox.IsEnabled = true;
            RagePickButton.IsEnabled = true;
            SoundFilePickButton.IsEnabled = true;
            _unit = null;
            WindowChromeUnitText.Text = "";
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (UnitTextBox.Text.Length == 0)
            {
                MessageBox.Show("Unit not selected.");
                return;
            }
            if (string.IsNullOrEmpty(_filePath))
            {
                MessageBox.Show("Chatlog file not selected.");
                return;
            }
            if (string.IsNullOrEmpty(_filePath))
            {
                MessageBox.Show("Sound file not selected.");
                return;
            }
            
            _unit = UnitTextBox.Text;
            WindowChromeUnitText.Text = $"({_unit})";
            SelectButton.Visibility = Visibility.Collapsed;
            ResetButton.Visibility = Visibility.Visible;
            UnitTextBox.IsEnabled = false;
            SoundFileTextBox.IsEnabled = false;
            ChatlogTextBox.IsEnabled = false;
            RagePickButton.IsEnabled = false;
            SoundFilePickButton.IsEnabled = false;

            // If user settings is null, create a new one.
            _userSettings ??= new UserSettings();
            _userSettings.ChatlogFilePath = _filePath;
            _userSettings.SoundFilePath = _soundFilePath;
            _userSettings.LastUnit = _unit;

            var jsonData = JsonSerializer.Serialize(_userSettings);
            using var isoStream = new IsolatedStorageFileStream("unit_config.json", FileMode.OpenOrCreate, _isoStore);
            isoStream.SetLength(0);
            using var streamWriter = new StreamWriter(isoStream);
            streamWriter.WriteLine(jsonData);
            
            _worker.RunWorkerAsync(argument: _filePath);
            
        }


    }
}
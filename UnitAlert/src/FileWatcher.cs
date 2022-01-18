using System.ComponentModel;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace UnitAlert;

public class FileWatcher
{
    public string? ChatlogFilePath { get; set; }
    public string? SoundFilePath { get; set; }
    public string? Unit { get; set; }
    private readonly BackgroundWorker _worker;
    private readonly BackgroundWorker _soundWorker;
    
    public FileWatcher()
    {
        // In the constructor we initialize the workers and assign their functions.
        _worker = new BackgroundWorker();
        _soundWorker = new BackgroundWorker();
        _worker.DoWork += WatchFile;
        _worker.WorkerSupportsCancellation = true;
        _soundWorker.DoWork += PlaySound;
    }

    public void StartWatch()
    {
        _worker.RunWorkerAsync();
    }

    public void CancelWatch()
    {
        if (_worker.IsBusy)
        {
            _worker.CancelAsync();
        }
    }
    
    private void WatchFile(object? sender, DoWorkEventArgs e)
    {
        // Gets the file path from the worker and check if it is not null.
        if (string.IsNullOrEmpty(ChatlogFilePath) || string.IsNullOrEmpty(Unit))
        {
            return;
        }
        // Opens a FileStream of that file, seeks to the end then starts reading from it.
        using var fileStream = File.Open(ChatlogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
            if (Unit != null && line.Contains(Unit))
            {
                _soundWorker.RunWorkerAsync();
            }
            lastFileSize = fileStream.Length;
        }
            
    }
    
    private void PlaySound(object? sender, DoWorkEventArgs e)
    {
        // Checks the string then plays the sound.
        if (string.IsNullOrEmpty(SoundFilePath))
        {
            return;
        }
        using var audioFile = new AudioFileReader(SoundFilePath);
        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(audioFile);
        outputDevice.Play();
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(1000);
        }
    }
}
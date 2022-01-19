using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Wave;

namespace UnitAlert;

public class FileWatcher
{
    public string? ChatlogFilePath { get; set; }
    public string? SoundFilePath { get; set; }

    private List<string>? _unit;

    public string? Unit
    {
        get => _unit!.First();
        set => _unit = value?.Split(';').ToList();
    }

    private readonly BackgroundWorker _worker;
    private readonly BackgroundWorker _soundWorker;
    
    public FileWatcher()
    {
        // In the constructor we initialize the workers and assign their functions.
        _worker = new BackgroundWorker();
        _soundWorker = new BackgroundWorker();
        _worker.DoWork += WatchFileNew;
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


    private IEnumerable<string> FollowGenerator(string filePath)
    {
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fileStream.Seek(0, SeekOrigin.End);
        using var streamReader = new StreamReader(fileStream);
        var lastFileSize = fileStream.Length;
        while (!_worker.CancellationPending)
        {
            var line = streamReader.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                Thread.Sleep(250);
                continue;
            }
            Trace.WriteLine(line);
            yield return line;
        }
    }
    
    private void WatchFileNew(object? sender, DoWorkEventArgs e)
    {
        // Gets the file path from the worker and check if it is not null.
        if (string.IsNullOrEmpty(ChatlogFilePath) || _unit is null || !_unit.Any())
        {
            return;
        }
        // Opens a FileStream of that file, seeks to the end then starts reading from it.
        using var fileStream = File.Open(ChatlogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fileStream.Seek(0, SeekOrigin.End);
        using var streamReader = new StreamReader(fileStream);
        var chatLog = FollowGenerator(ChatlogFilePath);
        foreach (var line in chatLog)
        {
            if (_worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            
            // If the unit is null or the line doesn't contain unit or the sound worker is busy, skip the iteration.
            if (_unit is null || !_unit.Any(s => line.Contains(s)) || _soundWorker.IsBusy)
            {
                continue;
            }
            // Otherwise just play the sound.
            _soundWorker.RunWorkerAsync();
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
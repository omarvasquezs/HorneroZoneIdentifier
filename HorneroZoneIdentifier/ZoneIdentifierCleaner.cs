using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HorneroZoneIdentifier;

internal sealed class ZoneIdentifierCleaner : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly HashSet<string> _monitoredPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _disposed;
    private HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? FileProcessed;
    public event Action<string, Exception>? ErrorOccurred;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteFile(string lpFileName);

    public IReadOnlyCollection<string> MonitoredPaths
    {
        get { lock (_lock) return _monitoredPaths.ToList().AsReadOnly(); }
    }

    public void SetAllowedExtensions(IEnumerable<string> extensions)
    {
        lock (_lock)
        {
            _allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        }
    }

    private bool IsExtensionAllowed(string filePath)
    {
        lock (_lock)
        {
            if (_allowedExtensions.Count == 0) return true;
            return _allowedExtensions.Contains(Path.GetExtension(filePath));
        }
    }

    public void AddPath(string path)
    {
        if (!Directory.Exists(path))
            return;

        lock (_lock)
        {
            if (!_monitoredPaths.Add(path))
                return;

            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileEvent;
            watcher.Changed += OnFileEvent;
            watcher.Renamed += OnFileEvent;

            _watchers.Add(watcher);
        }
    }

    public void RemovePath(string path)
    {
        lock (_lock)
        {
            if (!_monitoredPaths.Remove(path))
                return;

            var watcher = _watchers.FirstOrDefault(w =>
                string.Equals(w.Path, path, StringComparison.OrdinalIgnoreCase));

            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= OnFileEvent;
                watcher.Changed -= OnFileEvent;
                watcher.Renamed -= OnFileEvent;
                watcher.Dispose();
                _watchers.Remove(watcher);
            }
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath == null || Directory.Exists(e.FullPath))
            return;

        if (!IsExtensionAllowed(e.FullPath))
            return;

        Task.Run(async () =>
        {
            // 1000ms delay to let Outlook finish writing large attachments
            await Task.Delay(1000);
            TryRemoveZoneIdentifier(e.FullPath);
        });
    }

    public bool TryRemoveZoneIdentifier(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            string adsPath = filePath + ":Zone.Identifier";

            // DeleteFile on an ADS path removes only that stream
            if (DeleteFile(adsPath))
            {
                FileProcessed?.Invoke(filePath);
                return true;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                // ERROR_FILE_NOT_FOUND (2) means no Zone.Identifier stream — not an error
                if (error == 2)
                    return false;

                throw new Win32Exception(error);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(filePath, ex);
            return false;
        }
    }

    public int CleanFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return 0;

        int count = 0;
        foreach (var file in EnumerateFilesSafe(folderPath))
        {
            if (!IsExtensionAllowed(file))
                continue;

            if (TryRemoveZoneIdentifier(file))
                count++;
        }
        return count;
    }

    private static IEnumerable<string> EnumerateFilesSafe(string rootPath)
    {
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var file in files)
                yield return file;

            IEnumerable<string> subdirs;
            try { subdirs = Directory.EnumerateDirectories(dir); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var sub in subdirs)
                pending.Push(sub);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();
            _monitoredPaths.Clear();
        }
    }
}

using Microsoft.Win32;

namespace HorneroZoneIdentifier;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ZoneIdentifierCleaner _cleaner;
    private readonly AppSettings _settings;
    private readonly ToolStripMenuItem _statusItem;
    private int _processedCount;

    private const string AppName = "HorneroZoneIdentifier";
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _cleaner = new ZoneIdentifierCleaner();
        _cleaner.FileProcessed += OnFileProcessed;
        _cleaner.ErrorOccurred += OnError;

        _statusItem = new ToolStripMenuItem("Archivos procesados: 0")
        {
            Enabled = false
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(new ToolStripMenuItem("Hornero Zone Identifier Cleaner") { Enabled = false, Font = new Font(contextMenu.Font, FontStyle.Bold) });
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_statusItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Carpetas monitoreadas...", null, OnManageFolders);
        contextMenu.Items.Add("Limpiar carpetas ahora", null, OnCleanNow);
        contextMenu.Items.Add(new ToolStripSeparator());

        var startupItem = new ToolStripMenuItem("Iniciar con Windows")
        {
            Checked = _settings.StartWithWindows,
            CheckOnClick = true
        };
        startupItem.CheckedChanged += OnStartupChanged;
        contextMenu.Items.Add(startupItem);

        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Salir", null, OnExit);

        _notifyIcon = new NotifyIcon
        {
            Text = "Hornero Zone Identifier Cleaner\nMonitoreando archivos...",
            Icon = LoadIcon(),
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += OnManageFolders;

        // Start monitoring
        _cleaner.SetAllowedExtensions(_settings.AllowedExtensions);
        foreach (var folder in _settings.MonitoredFolders)
            _cleaner.AddPath(folder);

        // Re-apply startup setting on every launch to self-heal after reinstall/update
        SetStartup(_settings.StartWithWindows);

        ShowBalloon("Agente iniciado",
            $"Monitoreando {_settings.MonitoredFolders.Count} carpeta(s) para eliminar Zone Identifiers.");
    }

    private static Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }
        return SystemIcons.Shield;
    }

    private void OnFileProcessed(string filePath)
    {
        Interlocked.Increment(ref _processedCount);
        var fileName = Path.GetFileName(filePath);

        if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _notifyIcon.ContextMenuStrip.BeginInvoke(() =>
                _statusItem.Text = $"Archivos procesados: {_processedCount}");
        }
        else
        {
            _statusItem.Text = $"Archivos procesados: {_processedCount}";
        }

        ShowBalloon("Zone Identifier eliminado", fileName);
    }

    private void OnError(string filePath, Exception ex)
    {
        // Log silently — don't annoy the user with error balloons
        System.Diagnostics.Debug.WriteLine($"Error processing {filePath}: {ex.Message}");
    }

    private void OnManageFolders(object? sender, EventArgs e)
    {
        using var form = new FolderManagerForm(_settings.MonitoredFolders, _settings.AllowedExtensions);
        if (form.ShowDialog() == DialogResult.OK)
        {
            foreach (var folder in _settings.MonitoredFolders.ToList())
                _cleaner.RemovePath(folder);

            _settings.MonitoredFolders = form.Folders;
            _settings.AllowedExtensions = form.Extensions;
            _settings.Save();

            foreach (var folder in _settings.MonitoredFolders)
                _cleaner.AddPath(folder);

            _cleaner.SetAllowedExtensions(_settings.AllowedExtensions);

            var extInfo = _settings.AllowedExtensions.Count > 0
                ? $"{_settings.AllowedExtensions.Count} extension(es)"
                : "todas las extensiones";
            ShowBalloon("Configuracion actualizada",
                $"Monitoreando {_settings.MonitoredFolders.Count} carpeta(s) - {extInfo}.");
        }
    }

    private async void OnCleanNow(object? sender, EventArgs e)
    {
        if (_settings.MonitoredFolders.Count == 0)
        {
            ShowBalloon("Limpiar carpetas", "No hay carpetas monitoreadas configuradas.");
            return;
        }

        using var picker = new FolderPickerForm(_settings.MonitoredFolders);
        if (picker.ShowDialog() != DialogResult.OK || picker.SelectedFolder is null)
            return;

        var folder = picker.SelectedFolder;
        ShowBalloon("Limpiando...", $"Limpiando {Path.GetFileName(folder)}...");

        int total = await Task.Run(() => _cleaner.CleanFolder(folder));

        ShowBalloon("Limpieza completa",
            total > 0
                ? $"Se eliminaron {total} Zone Identifier(s) en {Path.GetFileName(folder)}."
                : $"No se encontraron Zone Identifiers en {Path.GetFileName(folder)}.");
    }

    private void OnStartupChanged(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item) return;

        _settings.StartWithWindows = item.Checked;
        _settings.Save();

        bool ok = SetStartup(item.Checked);
        if (item.Checked)
        {
            ShowBalloon("Inicio con Windows",
                ok ? "Iniciara automaticamente con Windows."
                   : "No se pudo registrar el inicio automatico.");
        }
    }

    private static bool SetStartup(bool enable)
    {
        // Prefer Startup folder: not subject to Windows startup suppression,
        // and .appref-ms reference always launches the latest ClickOnce version.
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var startupLink = Path.Combine(startupFolder, AppName + ".appref-ms");

        if (enable)
        {
            var appRef = FindAppRef();
            if (appRef != null)
            {
                try
                {
                    File.Copy(appRef, startupLink, overwrite: true);
                    TryRemoveRegistryStartup();
                    return File.Exists(startupLink);
                }
                catch { }
            }

            // Fallback for non-ClickOnce (development / direct .exe run)
            return TrySetRegistryStartup();
        }
        else
        {
            try { if (File.Exists(startupLink)) File.Delete(startupLink); }
            catch { }
            TryRemoveRegistryStartup();
            return true;
        }
    }

    private static string? FindAppRef()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (!Directory.Exists(root)) return null;

        // First attempt: fast path with AllDirectories
        try
        {
            var found = Directory.EnumerateFiles(root, "*.appref-ms", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.IndexOf("Hornero", StringComparison.OrdinalIgnoreCase) >= 0);
            if (found != null) return found;
        }
        catch { }

        // Second attempt: enumerate subdirectories one level at a time to avoid
        // UnauthorizedAccessException from AllDirectories stopping the search
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                try
                {
                    var found = Directory.EnumerateFiles(dir, "*.appref-ms")
                        .FirstOrDefault(f => f.IndexOf("Hornero", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (found != null) return found;
                }
                catch { }
            }
        }
        catch { }

        return null;
    }

    private static bool TrySetRegistryStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
            if (key == null) return false;
            var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(AppName, $"\"{exePath}\"");
            return key.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    private static void TryRemoveRegistryStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
            key?.DeleteValue(AppName, false);
        }
        catch { }
    }

    private void ShowBalloon(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(2000);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _cleaner.Dispose();
        _notifyIcon.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cleaner.Dispose();
            _notifyIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

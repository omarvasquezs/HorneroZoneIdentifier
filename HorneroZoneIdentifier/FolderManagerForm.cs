namespace HorneroZoneIdentifier;

internal sealed class FolderManagerForm : Form
{
    private ListBox _foldersListBox = null!;
    private ListBox _extensionsListBox = null!;
    private TextBox _extensionInput = null!;
    private readonly List<string> _folders;
    private readonly List<string> _extensions;

    public List<string> Folders => [.. _folders];
    public List<string> Extensions => [.. _extensions];

    public FolderManagerForm(IEnumerable<string> currentFolders, IEnumerable<string> currentExtensions)
    {
        _folders = [.. currentFolders];
        _extensions = [.. currentExtensions];
        Text = "Configuracion";
        ClientSize = new Size(556, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        var bottomPanel = new Panel { Location = new Point(0, 376), Size = new Size(556, 44) };
        var okButton = new Button { Text = "Aceptar", Location = new Point(452, 8), Size = new Size(96, 28), DialogResult = DialogResult.OK };
        bottomPanel.Controls.Add(okButton);
        AcceptButton = okButton;
        var tabs = new TabControl { Location = new Point(0, 0), Size = new Size(556, 376) };
        tabs.TabPages.Add(BuildFoldersTab());
        tabs.TabPages.Add(BuildExtensionsTab());
        Controls.AddRange([tabs, bottomPanel]);
    }

    private TabPage BuildFoldersTab()
    {
        var tab = new TabPage("Carpetas");
        var label = new Label { Text = "Carpetas monitoreadas por el agente:", Location = new Point(10, 10), AutoSize = true };
        _foldersListBox = new ListBox { Location = new Point(10, 34), Size = new Size(390, 290), SelectionMode = SelectionMode.One };
        foreach (var f in _folders) _foldersListBox.Items.Add(f);
        var addBtn = new Button { Text = "Agregar...", Location = new Point(408, 34), Size = new Size(106, 28) };
        addBtn.Click += OnAddFolder;
        var removeBtn = new Button { Text = "Eliminar", Location = new Point(408, 68), Size = new Size(106, 28) };
        removeBtn.Click += OnRemoveFolder;
        tab.Controls.AddRange([label, _foldersListBox, addBtn, removeBtn]);
        return tab;
    }

    private TabPage BuildExtensionsTab()
    {
        var tab = new TabPage("Extensiones");
        var label = new Label { Text = "Solo se procesaran archivos con estas extensiones (vacio = todas):", Location = new Point(10, 10), AutoSize = true };
        _extensionsListBox = new ListBox { Location = new Point(10, 34), Size = new Size(190, 290), SelectionMode = SelectionMode.One };
        foreach (var ext in _extensions) _extensionsListBox.Items.Add(ext);
        var inputLabel = new Label { Text = "Extension:", Location = new Point(214, 34), AutoSize = true };
        _extensionInput = new TextBox { Location = new Point(214, 54), Size = new Size(110, 24), PlaceholderText = ".pdf" };
        _extensionInput.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) OnAddExtension(null, EventArgs.Empty); };
        var addBtn = new Button { Text = "Agregar", Location = new Point(330, 52), Size = new Size(90, 28) };
        addBtn.Click += OnAddExtension;
        var removeBtn = new Button { Text = "Eliminar", Location = new Point(330, 86), Size = new Size(90, 28) };
        removeBtn.Click += OnRemoveExtension;
        var hint = new Label { Text = "Ej: .pdf  .docx  .xlsx  .zip", Location = new Point(214, 88), ForeColor = SystemColors.GrayText, AutoSize = true };
        tab.Controls.AddRange([label, _extensionsListBox, inputLabel, _extensionInput, addBtn, removeBtn, hint]);
        return tab;
    }

    private void OnAddFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Seleccione una carpeta para monitorear", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var path = dialog.SelectedPath;
            if (!_folders.Contains(path, StringComparer.OrdinalIgnoreCase))
            { _folders.Add(path); _foldersListBox.Items.Add(path); }
        }
    }

    private void OnRemoveFolder(object? sender, EventArgs e)
    {
        if (_foldersListBox.SelectedIndex >= 0)
        { _folders.RemoveAt(_foldersListBox.SelectedIndex); _foldersListBox.Items.RemoveAt(_foldersListBox.SelectedIndex); }
    }

    private void OnAddExtension(object? sender, EventArgs e)
    {
        var ext = _extensionInput.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) return;
        if (!ext.StartsWith('.')) ext = "." + ext;
        if (!_extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
        { _extensions.Add(ext); _extensionsListBox.Items.Add(ext); }
        _extensionInput.Clear(); _extensionInput.Focus();
    }

    private void OnRemoveExtension(object? sender, EventArgs e)
    {
        if (_extensionsListBox.SelectedIndex >= 0)
        { _extensions.RemoveAt(_extensionsListBox.SelectedIndex); _extensionsListBox.Items.RemoveAt(_extensionsListBox.SelectedIndex); }
    }
}

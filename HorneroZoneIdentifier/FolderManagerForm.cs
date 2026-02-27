namespace HorneroZoneIdentifier;

internal sealed class FolderManagerForm : Form
{
    private readonly ListBox _listBox;
    private readonly Button _addButton;
    private readonly Button _removeButton;
    private readonly Button _closeButton;
    private readonly List<string> _folders;

    public List<string> Folders => [.. _folders];

    public FolderManagerForm(IEnumerable<string> currentFolders)
    {
        _folders = [.. currentFolders];

        Text = "Carpetas monitoreadas";
        Size = new Size(520, 370);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = "El agente eliminará el Zone Identifier de archivos guardados en estas carpetas:",
            Location = new Point(12, 12),
            Size = new Size(480, 36),
            AutoSize = false
        };

        _listBox = new ListBox
        {
            Location = new Point(12, 52),
            Size = new Size(380, 220),
            SelectionMode = SelectionMode.One
        };
        foreach (var f in _folders) _listBox.Items.Add(f);

        _addButton = new Button
        {
            Text = "Agregar...",
            Location = new Point(400, 52),
            Size = new Size(95, 30)
        };
        _addButton.Click += OnAdd;

        _removeButton = new Button
        {
            Text = "Eliminar",
            Location = new Point(400, 90),
            Size = new Size(95, 30)
        };
        _removeButton.Click += OnRemove;

        _closeButton = new Button
        {
            Text = "Aceptar",
            Location = new Point(400, 280),
            Size = new Size(95, 30),
            DialogResult = DialogResult.OK
        };

        AcceptButton = _closeButton;

        Controls.AddRange([label, _listBox, _addButton, _removeButton, _closeButton]);
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleccione una carpeta para monitorear",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var path = dialog.SelectedPath;
            if (!_folders.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                _folders.Add(path);
                _listBox.Items.Add(path);
            }
        }
    }

    private void OnRemove(object? sender, EventArgs e)
    {
        if (_listBox.SelectedIndex >= 0)
        {
            var idx = _listBox.SelectedIndex;
            _folders.RemoveAt(idx);
            _listBox.Items.RemoveAt(idx);
        }
    }
}

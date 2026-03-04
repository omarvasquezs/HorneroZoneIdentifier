namespace HorneroZoneIdentifier;

internal sealed class FolderPickerForm : Form
{
    private readonly ListBox _listBox;

    public string? SelectedFolder { get; private set; }

    public FolderPickerForm(IEnumerable<string> folders)
    {
        Text = "Limpiar carpeta";
        Size = new Size(480, 240);
        MinimumSize = new Size(360, 200);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = "Seleccione la carpeta a limpiar:",
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(6, 7, 4, 0)
        };

        _listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One
        };
        foreach (var f in folders)
            _listBox.Items.Add(f);
        if (_listBox.Items.Count > 0)
            _listBox.SelectedIndex = 0;

        _listBox.DoubleClick += (_, _) =>
        {
            SelectedFolder = _listBox.SelectedItem as string;
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 38,
            Padding = new Padding(6, 4, 6, 4)
        };

        var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 85 };
        var btnOk = new Button { Text = "Limpiar", Width = 85 };
        btnOk.Click += (_, _) =>
        {
            SelectedFolder = _listBox.SelectedItem as string;
            DialogResult = DialogResult.OK;
            Close();
        };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);

        Controls.Add(_listBox);
        Controls.Add(btnPanel);
        Controls.Add(label);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}

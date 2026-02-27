namespace HorneroZoneIdentifier;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Prevent multiple instances
        using var mutex = new Mutex(true, "HorneroZoneIdentifier_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show(
                "Hornero Zone Identifier Cleaner ya se encuentra en ejecución.",
                "Hornero Zone Identifier",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new TrayApplicationContext());
    }
}

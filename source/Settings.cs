namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
public partial class Island
{

    CheckBox Check(string label, bool value) => new() { Content = label, IsChecked = value, Foreground = Fg, Margin = new Thickness(0, 7, 0, 7), VerticalContentAlignment = VerticalAlignment.Center };
    StackPanel Field(string label, UIElement control)
    {
        var p = Col(T(label, 12, Muted), control);
        p.Margin = new Thickness(0, 8, 12, 8);
        return p;
    }
    double Number(TextBox field, double min, double max)
    {
        if (!double.TryParse(field.Text, out double v) || !double.IsFinite(v) || v < min || v > max)
            throw new Exception($"{field.ToolTip}: enter a number from {min} to {max}.");
        return v;
    }
    void ExportBackup()
    {
        var f = new SaveFileDialog { Filter = "JSON backup|*.json", FileName = "Orbit-backup-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json" };
        if (f.ShowDialog() == true)
        {
            Save();
            File.Copy(store.FilePath, f.FileName, true);
            Notify("Backup exported", "Keep this file somewhere safe.", false);
        }
    }
    void ImportBackup(Window owner)
    {
        var f = new OpenFileDialog { Filter = "JSON backup|*.json" };
        if (f.ShowDialog() != true)
            return;
        var old = D;
        try
        {
            var imported = JsonSerializer.Deserialize<AppData>(File.ReadAllText(f.FileName), Store.JsonOptions) ?? throw new Exception("The backup is empty.");
            if (imported.Settings == null || imported.Todos == null || imported.Notes == null || imported.Events == null || imported.Days == null || imported.Notices == null || imported.Timer == null || imported.Shelf == null || imported.Settings.Modules == null || imported.Settings.Hidden == null)
                throw new Exception("The backup is incomplete.");
            if (MessageBox.Show(owner, "Replace current data with this backup? Your current data will be saved to a separate before-import file.", "Import backup", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            File.Copy(store.FilePath, store.FilePath + ".before-import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true);
            store.Data = imported;
            store.Normalize();
            Save();
            owner.Close();
            SetExpanded(true);
            Notify("Backup imported", "Your previous data is preserved in the data folder.", false);
        }
        catch (Exception e) { store.Data = old; MessageBox.Show(owner, e.Message, "Could not import backup"); }
    }
    void SetStartup(bool enabled)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var link = System.IO.Path.Combine(folder, "Orbit Island.lnk");
        if (enabled)
        {
            var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new Exception("Windows shortcut service unavailable.");
            dynamic shellObj = Activator.CreateInstance(type)!;
            dynamic shortcut = shellObj.CreateShortcut(link);
            shortcut.TargetPath = Environment.ProcessPath;
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.Arguments = "";
            shortcut.Save();
        }
        else if (File.Exists(link))
            File.Delete(link);
        Notify("Startup updated", enabled ? "Orbit will launch when you sign in." : "Orbit startup shortcut removed.", false);
    }
}



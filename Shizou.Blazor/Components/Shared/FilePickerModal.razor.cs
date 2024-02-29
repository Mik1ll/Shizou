using System.Runtime.InteropServices;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Extensions;

namespace Shizou.Blazor.Components.Shared;

public enum FilePickerType
{
    File = 1,
    Directory = 2,
    FileOrDirectory = 3
}

public partial class FilePickerModal
{
    private readonly List<(string Name, bool IsFile)> _entries = new();
    private string _typeStr = string.Empty;
    private string _parentPath = string.Empty;
    private (string Name, bool IsFile)? _selectedEntry;
    private Modal _modal = default!;

    [Parameter]
    public FilePickerType FilePickerType { get; set; }

    [Parameter]
    public string? InitialPath { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(FilePickerType));
        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        _typeStr = FilePickerType switch
        {
            FilePickerType.File => "File",
            FilePickerType.Directory => "Folder",
            FilePickerType.FileOrDirectory => "File or Folder",
            _ => throw new ArgumentOutOfRangeException()
        };
        if (string.IsNullOrWhiteSpace(InitialPath))
        {
            _parentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            GetEntries();
        }
        else
        {
            var fullPath = Path.GetFullPath(InitialPath);
            _parentPath = GetParentPath(fullPath);
            GetEntries();
            SelectEntry(GetFileName(fullPath));
        }
    }

    private void SelectEntry(string name)
    {
        _selectedEntry = _entries.FirstOrDefault(e => e.Name == name);
    }

    private string GetFileName(string path)
    {
        return Path.GetPathRoot(path) == path ? path : Path.GetFileName(path);
    }

    private string GetParentPath(string path)
    {
        return Path.GetDirectoryName(path) ?? string.Empty;
    }

    private void GetEntries()
    {
        _entries.Clear();
        if (_parentPath == string.Empty)
        {
            _entries.AddRange(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Directory.GetLogicalDrives().Select(s => (s, false))
                : new[] { ("/", false) });
            return;
        }

        _entries.AddRange(Directory.EnumerateDirectories(_parentPath, "*", new EnumerationOptions { IgnoreInaccessible = true })
            .Select(s => (GetFileName(s), false)));
        if (FilePickerType.HasFlag(FilePickerType.File))
            _entries.AddRange(Directory.EnumerateFiles(_parentPath, "*", new EnumerationOptions { IgnoreInaccessible = true })
                .Select(s => (GetFileName(s), true)));
    }

    private async Task ConfirmAsync()
    {
        if (ValidSelection() && _selectedEntry is not null)
            await _modal.CloseAsync(ModalResult.Ok(Path.Combine(_parentPath, _selectedEntry.Value.Name)));
        else
            await CancelAsync();
    }

    private bool ValidSelection()
    {
        return _selectedEntry is not null && (FilePickerType == FilePickerType.FileOrDirectory || (_selectedEntry.Value.IsFile
            ? FilePickerType.HasFlag(FilePickerType.File)
            : FilePickerType.HasFlag(FilePickerType.Directory)));
    }

    private async Task CancelAsync()
    {
        await _modal.CancelAsync();
    }

    private void GoDown()
    {
        if (_selectedEntry?.IsFile is true or null)
            return;
        _parentPath = Path.Combine(_parentPath, _selectedEntry.Value.Name);
        _selectedEntry = null;
        GetEntries();
    }

    private void GoUp()
    {
        if (_parentPath == string.Empty)
            return;
        _selectedEntry = (GetFileName(_parentPath), false);
        _parentPath = GetParentPath(_parentPath);
        GetEntries();
    }
}

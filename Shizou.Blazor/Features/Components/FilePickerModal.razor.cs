using System.Runtime.InteropServices;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Extensions;

namespace Shizou.Blazor.Features.Components;

public enum FilePickerType
{
    File = 1,
    Directory = 2,
    FileOrDirectory = 3
}

public partial class FilePickerModal
{
    private string _pathParent = string.Empty;
    private (string Name, bool IsFile)? _pathChild;

    private readonly List<(string Name, bool IsFile)> _entries = new();


    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public string? FolderPath { get; set; }

    [Parameter]
    public FilePickerType FilePickerType { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(FolderPath));
        parameters.EnsureParametersSet(nameof(FilePickerType));
        return base.SetParametersAsync(parameters);
    }

    private void SelectEntry(ChangeEventArgs e)
    {
        _pathChild = _entries.FirstOrDefault(en => en.Name == e.Value as string);
    }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            _pathParent = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            GetEntries();
        }
        else
        {
            var fullPath = Path.GetFullPath(FolderPath);
            _pathParent = GetParentPath(fullPath);
            GetEntries();
            _pathChild = _entries.FirstOrDefault(e => e.Name == GetFileName(fullPath));
        }
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
        if (_pathParent == string.Empty)
        {
            _entries.AddRange(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Directory.GetLogicalDrives().Select(s => (s, false))
                : new[] { ("/", false) });
            return;
        }

        if (FilePickerType.HasFlag(FilePickerType.File))
            _entries.AddRange(Directory.EnumerateFiles(_pathParent, "*", new EnumerationOptions { IgnoreInaccessible = true })
                .Select(s => (GetFileName(s), true)));
        _entries.AddRange(Directory.EnumerateDirectories(_pathParent, "*", new EnumerationOptions { IgnoreInaccessible = true })
            .Select(s => (GetFileName(s), false)));
    }

    private async Task Confirm()
    {
        if (ValidSelection() && _pathChild is not null)
            await ModalInstance.CloseAsync(ModalResult.Ok(Path.Combine(_pathParent, _pathChild.Value.Name)));
        else
            await Cancel();
    }

    private bool ValidSelection()
    {
        return _pathChild is not null && (FilePickerType == FilePickerType.FileOrDirectory || (_pathChild.Value.IsFile
            ? FilePickerType.HasFlag(FilePickerType.File)
            : FilePickerType.HasFlag(FilePickerType.Directory)));
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private void GoDown()
    {
        if (_pathChild?.IsFile is true or null)
            return;
        _pathParent = Path.Combine(_pathParent, _pathChild.Value.Name);
        _pathChild = null;
        GetEntries();
    }

    private void GoUp()
    {
        if (_pathParent == string.Empty)
            return;
        _pathChild = (GetFileName(_pathParent), false);
        _pathParent = GetParentPath(_pathParent);
        GetEntries();
    }
}

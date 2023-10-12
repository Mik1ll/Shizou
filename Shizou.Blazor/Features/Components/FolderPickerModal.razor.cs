using System.Runtime.InteropServices;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class FolderPickerModal
{
    private string _pathParent = string.Empty;
    private string _pathChild = string.Empty;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public string? FolderPath { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            _pathParent = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else
        {
            _pathParent = GetFolderParent(FolderPath);
            _pathChild = GetFolderName(FolderPath);
        }
    }

    private string GetFolderName(string path)
    {
        return Path.GetPathRoot(path) == path ? path : Path.GetFileName(path);
    }

    private string GetFolderParent(string path)
    {
        return Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
    }

    private string[] GetSubFolders()
    {
        if (_pathParent == string.Empty)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Directory.GetLogicalDrives();
            else
                return new[] { "/" };
        return Directory.EnumerateDirectories(_pathParent, "*", new EnumerationOptions { IgnoreInaccessible = true }).ToArray();
    }

    private async Task Close()
    {
        await ModalInstance.CloseAsync(ModalResult.Ok(Path.Combine(_pathParent, _pathChild)));
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private void GoDown()
    {
        _pathParent = Path.Combine(_pathParent, _pathChild);
        _pathChild = string.Empty;
    }

    private void GoUp()
    {
        if (_pathParent == string.Empty)
            return;
        _pathChild = GetFolderName(_pathParent);
        _pathParent = GetFolderParent(_pathParent);
    }
}

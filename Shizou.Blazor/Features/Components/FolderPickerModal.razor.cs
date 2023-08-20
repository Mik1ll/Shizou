using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class FolderPickerModal
{
    private string _pathParent = string.Empty;
    private string _pathChild = string.Empty;

    [Parameter]
    public string? FolderPath { get; set; }

    [Parameter]
    public EventCallback<string> FolderPathChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public EventCallback OnFolderPickerClose { get; set; }

    private string GetFolderName(string path)
    {
        return Path.GetPathRoot(path) == path ? path : Path.GetFileName(path);
    }

    private string GetFolderParent(string path)
    {
        return Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
    }

    private string[] GetSubFolders(string path)
    {
        if (_pathParent == string.Empty)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Directory.GetLogicalDrives();
            else
                return new[] { "/" };
        return Directory.EnumerateDirectories(_pathParent, "*", new EnumerationOptions { IgnoreInaccessible = true }).ToArray();
    }

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

    public void OnClose(bool accepted)
    {
        if (accepted)
            FolderPathChanged.InvokeAsync(Path.Combine(_pathParent, _pathChild));
        OnFolderPickerClose.InvokeAsync();
    }

    public void GoDown()
    {
        _pathParent = Path.Combine(_pathParent, _pathChild);
        _pathChild = string.Empty;
    }

    public void GoUp()
    {
        if (_pathParent == string.Empty)
            return;
        _pathChild = GetFolderName(_pathParent);
        _pathParent = GetFolderParent(_pathParent);
    }
}

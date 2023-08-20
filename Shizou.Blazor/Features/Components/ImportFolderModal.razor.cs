using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class ImportFolderModal
{
    private ImportFolder _myImportFolder = NewImportFolder();
    private bool _dialogIsOpen = false;
    private bool _isDelete = false;
    private bool _folderPickerOpen = false;
    private ModalDisplay? _modalDisplay;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    public void OnFolderPickerClose()
    {
        _folderPickerOpen = false;
    }

    public void NewDialog()
    {
        _isDelete = false;
        _myImportFolder = NewImportFolder();
        _dialogIsOpen = true;
    }

    public void EditDialog(ImportFolder importFolder)
    {
        _isDelete = false;
        _myImportFolder = importFolder;
        _dialogIsOpen = true;
    }

    public void DeleteDialog(ImportFolder importFolder)
    {
        _isDelete = true;
        _myImportFolder = importFolder;
        _dialogIsOpen = true;
    }

    private static ImportFolder NewImportFolder()
    {
        return new ImportFolder
        {
            Name = string.Empty,
            Path = string.Empty
        };
    }

    private void OnDialogClose(bool accepted)
    {
        if (accepted)
        {
            using var context = ContextFactory.CreateDbContext();
            if (_isDelete)
            {
                context.ImportFolders.Remove(_myImportFolder);
            }
            else
            {
                if (_myImportFolder.Id == 0)
                {
                    context.ImportFolders.Add(_myImportFolder);
                }
                else
                {
                    var importFolder = context.ImportFolders.Find(_myImportFolder.Id);
                    if (importFolder is not null)
                        context.Entry(importFolder).CurrentValues.SetValues(_myImportFolder);
                }
            }
            context.SaveChanges();
        }
        _dialogIsOpen = false;
        _myImportFolder = NewImportFolder();
        OnClose.InvokeAsync();
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Shared;

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

    private void OnValidate(ValidationMessageStore messageStore)
    {
        using var context = ContextFactory.CreateDbContext();
        if (context.ImportFolders.Where(i => i.Id != _myImportFolder.Id).Any(i => i.Name == _myImportFolder.Name))
            messageStore.Add(() => _myImportFolder.Name, "Import folder name must be unique");
        if (context.ImportFolders.Where(i => i.Id != _myImportFolder.Id).Any(i => i.Path == _myImportFolder.Path))
            messageStore.Add(() => _myImportFolder.Path, "Import folder path must be unique");
    }

    private void OnDialogClose(bool accepted)
    {
        if (accepted)
        {
            using var context = ContextFactory.CreateDbContext();
            var myImportFolderModel = _myImportFolder;
            if (_isDelete)
            {
                context.ImportFolders.Remove(myImportFolderModel);
            }
            else
            {
                if (_myImportFolder.Id == 0)
                {
                    context.ImportFolders.Add(myImportFolderModel);
                }
                else
                {
                    var importFolder = context.ImportFolders.Find(_myImportFolder.Id);
                    if (importFolder is not null)
                        context.Entry(importFolder).CurrentValues.SetValues(myImportFolderModel);
                }
            }
            context.SaveChanges();
        }
        _dialogIsOpen = false;
        _myImportFolder = NewImportFolder();
        OnClose.InvokeAsync();
    }
}

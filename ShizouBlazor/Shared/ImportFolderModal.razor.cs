using AutoMapper;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Shizou.Services;
using ShizouContracts.Dtos;
using ShizouData.Database;
using ShizouData.Models;

namespace ShizouBlazor.Shared;

public partial class ImportFolderModal
{
    private ImportFolderDto _myImportFolder = new();
    private bool _dialogIsOpen = false;
    private bool _isDelete = false;
    private bool _folderPickerOpen = false;
    private ModalDisplay? _modalDisplay;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IMapper Mapper { get; set; } = default!;

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
        _myImportFolder = new ImportFolderDto();
        _dialogIsOpen = true;
    }

    public void EditDialog(ImportFolder importFolder)
    {
        _isDelete = false;
        _myImportFolder = Mapper.Map<ImportFolderDto>(importFolder);
        _dialogIsOpen = true;
    }

    public void DeleteDialog(ImportFolder importFolder)
    {
        _isDelete = true;
        _myImportFolder = Mapper.Map<ImportFolderDto>(importFolder);
        _dialogIsOpen = true;
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
            var myImportFolderModel = Mapper.Map<ImportFolder>(_myImportFolder);
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
        _myImportFolder = new ImportFolderDto();
        OnClose.InvokeAsync();
    }
}

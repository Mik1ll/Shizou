namespace Shizou.Blazor.Features.Components;

public partial class ManuallyLinkModal
{
    private bool _dialogIsOpen;
    private bool _addAnimeOpen;
    private AddAnimeModal _addAnimeModal;

    private void OnClose(bool accepted)
    {
        _dialogIsOpen = false;
        _addAnimeOpen = false;
    }
}

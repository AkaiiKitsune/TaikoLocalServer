using ZXing.QrCode.Internal;

namespace TaikoWebUI.Pages.Dialogs;

public partial class AccessCodeDeleteConfirmDialog
{
    
    [CascadingParameter]
    MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public User User { get; set; } = new();
    
    [Parameter]
    public string AccessCode { get; set; } = "";
    public string LegacyAccessCode { get; set; } = "";

    private void Cancel() => MudDialog.Cancel();
    
    private async Task DeleteAccessCode()
    {
        if (User.AccessCodes.Count == 1)
        {
            Snackbar.Add("Cannot delete last access code!", Severity.Error);
            MudDialog.Close(DialogResult.Ok(false));
            return;
        }

        LegacyAccessCode = LoginService.ConvertOldUID(AccessCode, new DashboardResponse(), force: true);


        var cardResponseMessage = await Client.DeleteAsync($"api/Cards/{AccessCode}");
        if(LegacyAccessCode != "") await Client.DeleteAsync($"api/Cards/{LegacyAccessCode}");

        if (!cardResponseMessage.IsSuccessStatusCode)
        {
            Snackbar.Add("Something went wrong when deleting access code!", Severity.Error);
            MudDialog.Close(DialogResult.Ok(false));
            return;
        }

        Snackbar.Add("Delete success!", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }
}
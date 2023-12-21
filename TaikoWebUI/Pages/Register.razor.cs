namespace TaikoWebUI.Pages;

public partial class Register
{
    private string accessCode = "";
    private string confirmPassword = "";
    private string password = "";
    private MudForm registerForm = default!;
    private bool isSaving;

    private MudDatePicker datePicker = new();
    private MudTimePicker timePicker = new();
    private DateTime? date = DateTime.Today;
    private TimeSpan? time = new TimeSpan(00, 45, 00);

    private DashboardResponse? response;
    private readonly DialogOptions options = new DialogOptions() { DisableBackdropClick = true };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        response = await Client.GetFromJsonAsync<DashboardResponse>("api/Dashboard");
    }

    private async Task OnRegister()
    {
        isSaving = true;
        var inputDateTime = date!.Value.Date + time!.Value;
        if (response != null)
        {
            //Checking if the Card the player used is a real UID that corresponds to the value used to be stored in the DB.
            accessCode = accessCode.ToUpper().Trim();
            var oldUID = LoginService.ConvertOldUID(accessCode, response);
            var result = await LoginService.Register(oldUID == "" ? accessCode : oldUID, inputDateTime, password, confirmPassword, response, Client);
            switch (result)
            {
                case 0:
                    await DialogService.ShowMessageBox(
                        "Error",
                        "Only admin can log in.",
                        "Ok", null, null, options);
                    NavigationManager.NavigateTo("/Users");
                    break;
                case 1:
                    await DialogService.ShowMessageBox(
                        "Success",
                        "Access code registered successfully.",
                        "Ok", null, null, options);
                    NavigationManager.NavigateTo("/Users");
                    break;
                case 2:
                    await DialogService.ShowMessageBox(
                        "Error",
                        "Confirm password is not the same as password.",
                        "Ok", null, null, options);
                    break;
                case 3:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code not found.<br />Please play one game with this access code to register it.",
                        "Ok", null, null, options);
                    break;
                case 4:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code is already registered, please use set password to login.",
                        "Ok", null, null, options);
                    NavigationManager.NavigateTo("/Users");
                    break;
                case 5:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Wrong last play time.<br />If you have forgotten when you last played, please play another game with this access code.",
                        "Ok", null, null, options);
                    break;
            }
        }
        isSaving = false;
    }
}
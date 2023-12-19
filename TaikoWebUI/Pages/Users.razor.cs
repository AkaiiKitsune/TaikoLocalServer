using SharedProject.Models;
using TaikoWebUI.Pages.Dialogs;

namespace TaikoWebUI.Pages;

public partial class Users
{
    private string inputAccessCode = "";
    private MudForm loginForm = default!;
    private string inputPassword = "";
    private DashboardResponse? response;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        response = await Client.GetFromJsonAsync<DashboardResponse>("api/Dashboard");
    }

    private async Task DeleteUser(User user)
    {
        if (!LoginService.AllowUserDelete)
        {
            await DialogService.ShowMessageBox(
                "Error",
                "User deletion is disabled by admin.",
                "Ok");
            return;
        }
        var parameters = new DialogParameters
        {
            ["user"] = user
        };

        var options = new DialogOptions() { DisableBackdropClick = true };
        var dialog = DialogService.Show<UserDeleteConfirmDialog>("Delete User", parameters, options);
        var result = await dialog.Result;

        if (result.Canceled) return;

        response = await Client.GetFromJsonAsync<DashboardResponse>("api/Dashboard");
        if(user.Baid == LoginService.GetLoggedInUser().Baid) await OnLogout();
    }

    private async Task ResetPassword(User user)
    {
        if (!LoginService.IsAdmin)
        {
            await DialogService.ShowMessageBox(
                "Error",
                "Only admin can reset password.",
                "Ok");
            return;
        }
        var parameters = new DialogParameters
        {
            ["user"] = user
        };

        var options = new DialogOptions() { DisableBackdropClick = true };
        var dialog = DialogService.Show<ResetPasswordConfirmDialog>("Reset Password", parameters, options);
        var result = await dialog.Result;

        if (result.Canceled) return;

        response = await Client.GetFromJsonAsync<DashboardResponse>("api/Dashboard");
    }
    
    private async Task OnLogin()
    {
        if (response != null)
        {
            //Checking if the Card the player used is a real UID that corresponds to the value used to be stored in the DB.
            inputAccessCode = inputAccessCode.ToUpper().Trim();
            var oldUID = LoginService.ConvertOldUID(inputAccessCode, response);
            var result = LoginService.Login(oldUID == "" ? inputAccessCode : oldUID, inputPassword, response);

            switch (result)
            {
                case 0:
                    await DialogService.ShowMessageBox(
                        "Error",
                        "Only admin can log in.",
                        "Ok");
                    await loginForm.ResetAsync();
                    break;
                case 1:
                    StateContainer.currentUser = LoginService.GetLoggedInUser();
                    await LocalStorage.SaveStringAsync("user", inputAccessCode);
                    await LocalStorage.SaveStringAsync("pass", inputPassword);
                    //If the card given is a real UID that corresponds to a converted card, bind it to the user so they can authenticate with it on the webui
                    if (oldUID != "")
                    {
                        Console.WriteLine("Binding " + inputAccessCode);
                        await LoginService.BindAccessCode(inputAccessCode, LoginService.GetLoggedInUser(), Client);
                        //this is far from ideal, forces the users page to update after editing the AccessCode for current user
                        NavigationManager.NavigateTo($"/");
                    }
                    NavigationManager.NavigateTo($"/Users/");
                    break;
                case 2:
                    await DialogService.ShowMessageBox(
                        "Error",
                        "Wrong password!",
                        "Ok");
                    break;
                case 3:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code not found.<br />Please play one game with this access code to register it.",
                        "Ok");
                    break;
                case 4:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code not registered.<br />Please use register button to create a password first.",
                        "Ok");
                    break;
            }
        }
    }

    private async Task OnLogout()
    {
        LoginService.Logout();
        await LocalStorage.RemoveAsync("user");
        await LocalStorage.RemoveAsync("pass");
        NavigationManager.NavigateTo("/Users");
    }
    
    private Task ShowQrCode(User user)
    {
        var parameters = new DialogParameters
        {
            ["user"] = user
        };

        var options = new DialogOptions() { DisableBackdropClick = true };
        DialogService.Show<UserQrCodeDialog>("QR Code", parameters, options);

        return Task.CompletedTask;
    }
}
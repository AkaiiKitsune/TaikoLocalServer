﻿using Microsoft.AspNetCore.Http;
using System.Linq;
using TaikoWebUI.Pages.Dialogs;

namespace TaikoWebUI.Pages;

public partial class AccessCode
{
    [Parameter]
    public int Baid { get; set; }
    
    private string inputAccessCode = "";
    private MudForm bindAccessCodeForm = default!;

    private User? User { get; set; } = new();

    private List<String> Codes { get; set; } = new();
    private List<String> InternalCodes { get; set; } = new();

    private DashboardResponse? response;
    
    private readonly List<BreadcrumbItem> breadcrumbs = new()
    {
        new BreadcrumbItem("Users", href: "/Users"),
    };


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await InitializeUser();
        breadcrumbs.Add(new BreadcrumbItem($"User: {Baid}", href: null, disabled: true));
        breadcrumbs.Add(new BreadcrumbItem("Access Code Management", href: $"/Users/{Baid}/AccessCode", disabled: false));
    }
    
    private async Task InitializeUser()
    {
        response = await Client.GetFromJsonAsync<DashboardResponse>("api/Dashboard");
        LoginService.ResetLoggedInUser(response);
        if (LoginService.IsAdmin || !LoginService.LoginRequired)
        {
            if (response is not null)
            {
                User = response.Users.FirstOrDefault(u => u.Baid == Baid);
            }
        }
        else if (LoginService.IsLoggedIn)
        {
            User = LoginService.GetLoggedInUser();
        }

        Codes = new();
        InternalCodes = new();

        foreach(String code in User!.AccessCodes)
        {
            var oldUID = LoginService.ConvertOldUID(code, response!, force: true);
            var exists = User.AccessCodes.FirstOrDefault(u => u == oldUID);

            try
            {
                exists.ThrowIfNull();
                Console.WriteLine(code + " is " + oldUID);
                Codes.Add(code);
                InternalCodes.Add(oldUID);
            }
            catch
            {
                Console.WriteLine(code + " has no correspondance");
            }
        }
        foreach (String code in User!.AccessCodes)
        {
            if (!InternalCodes.Contains(code) && !Codes.Contains(code))
            {
                Codes.Add(code);
            }
        }

        foreach (String code in Codes)
        {
            Console.WriteLine(code);
        }
    }
    
    private async Task DeleteAccessCode(string accessCode)
    {
        var parameters = new DialogParameters<AccessCodeDeleteConfirmDialog>
        {
            { x => x.User, User },
            { x => x.AccessCode, accessCode }
        };

        var dialog = DialogService.Show<AccessCodeDeleteConfirmDialog>("Delete Access Code", parameters);
        var result = await dialog.Result;

        if (result.Canceled) return;
        
        await InitializeUser();
        NavigationManager.NavigateTo(NavigationManager.Uri);
    }

    private async Task OnBind()
    {
        if (response != null)
        {
            inputAccessCode = inputAccessCode.ToUpper().Trim();
            var oldUID = LoginService.ConvertOldUID(inputAccessCode, response, force: true);
            Console.WriteLine(inputAccessCode + " can be converted to " + oldUID);

            var result = await LoginService.BindAccessCode(inputAccessCode, response.Users.First(u => u.Baid == Baid), Client);
            await LoginService.BindAccessCode(oldUID, response.Users.First(u => u.Baid == Baid), Client);
            switch (result)
            {
                case 0:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Not logged in.<br />Please log in first and try again.",
                        "Ok");
                    break;
                case 1:
                    await DialogService.ShowMessageBox(
                        "Success",
                        "New access code bound successfully.",
                        "Ok");
                    await InitializeUser();
                    NavigationManager.NavigateTo(NavigationManager.Uri);
                    break;
                case 2:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Bound access code upper limit reached.<br />Please delete one access code first.",
                        "Ok");
                    break;
                case 3:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code already bound.<br />Please delete it from the bound user first.",
                        "Ok");
                    break;
                case 4:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "Access code cannot be empty.<br />Please enter a valid access code.",
                        "Ok");
                    break;
                case 5:
                    await DialogService.ShowMessageBox(
                        "Error",
                        (MarkupString)
                        "You can't do that!<br />You need to be an admin to edit someone else's access codes.",
                        "Ok");
                    break;
            }
        }
    }
}
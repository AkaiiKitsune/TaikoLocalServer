﻿using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaikoWebUI.Settings;

namespace TaikoWebUI.Services;

public class LoginService
{
    private readonly string adminPassword;
    private readonly string adminUsername;
    public bool LoginRequired { get; }
    public bool OnlyAdmin { get; }
    private readonly int boundAccessCodeUpperLimit;
    public bool RegisterWithLastPlayTime { get; }
    public bool AllowUserDelete { get; }
    public bool AllowFreeProfileEditing { get; }

    private string NewCardUID = "";
    private bool NewCardFormat = false;

    public LoginService(IOptions<WebUiSettings> settings)
    {
        IsLoggedIn = false;
        IsAdmin = false;
        var webUiSettings = settings.Value;
        adminUsername = webUiSettings.AdminUsername;
        adminPassword = webUiSettings.AdminPassword;
        LoginRequired = webUiSettings.LoginRequired;
        OnlyAdmin = webUiSettings.OnlyAdmin;
        boundAccessCodeUpperLimit = webUiSettings.BoundAccessCodeUpperLimit;
        RegisterWithLastPlayTime = webUiSettings.RegisterWithLastPlayTime;
        AllowUserDelete = webUiSettings.AllowUserDelete;
        AllowFreeProfileEditing = webUiSettings.AllowFreeProfileEditing;
    }

    public bool IsLoggedIn { get; private set; }
    private User LoggedInUser { get; set; } = new();
    public bool IsAdmin { get; private set; }

    static string PadLeftWithZeros(string input, int desiredLength)
    {
        int zerosToAdd = Math.Max(0, desiredLength - input.Length);
        return new string('0', zerosToAdd) + input;
    }

    public string ConvertOldUID(string inputCardNum, DashboardResponse response)
    {
        // Convert hexadecimal string to a byte array
        try
        {
            byte[] byteArray = new byte[inputCardNum.Length / 2];
            for (int i = 0; i < inputCardNum.Length; i += 2)
            {
                byteArray[i / 2] = Convert.ToByte(inputCardNum.Substring(i, 2), 16);
            }

            // Reverse the array if needed (depends on endianness)
            Array.Reverse(byteArray);

            // Convert byte array to an unsigned long integer
            string convertedNumber = PadLeftWithZeros(BitConverter.ToUInt64(byteArray, 0).ToString(), 20);

            //Console.WriteLine($"Hexadecimal: {inputCardNum}");
            //Console.WriteLine($"Decimal: {convertedNumber}");

            NewCardUID = convertedNumber;
            foreach (var user in response.Users.Where(user => user.AccessCodes.Contains(NewCardUID)))
            {
                NewCardFormat = true;
                Console.WriteLine($"This is a new card ! : {inputCardNum} used to be {convertedNumber}");
                return convertedNumber;
            }
        }
        finally { }
        return "";
    }

    public int Login(string inputCardNum, string inputPassword, DashboardResponse response)
    {
        if (inputCardNum == adminUsername && inputPassword == adminPassword)
        {
            IsLoggedIn = true;
            IsAdmin = true;
            return 1;
        }

        if (OnlyAdmin) return 0;

        foreach (var user in response.Users.Where(user => user.AccessCodes.Contains(inputCardNum)))
        {
            foreach (var userCredential in response.UserCredentials.Where(userCredential => userCredential.Baid == user.Baid))
            {
                if (userCredential.Password == "") return 4;
                if (ComputeHash(inputPassword, userCredential.Salt) != userCredential.Password) return 2;
                IsLoggedIn = true;
                LoggedInUser = user;
                IsAdmin = false;
                return 1;
            }
        }
        return 3;
    }

    public async Task<int> Register(string inputCardNum, DateTime inputDateTime, string inputPassword, string inputConfirmPassword,
        DashboardResponse response, HttpClient client)
    {
        if (OnlyAdmin) return 0;

        foreach (var user in response.Users.Where(user => user.AccessCodes.Contains(inputCardNum)))
        {
            foreach (var userCredential in response.UserCredentials.Where(userCredential => userCredential.Baid == user.Baid))
            {
                if (RegisterWithLastPlayTime)
                {
                    var userSettingResponse = await client.GetFromJsonAsync<UserSetting>($"api/UserSettings/{user.Baid}");
                    if (userSettingResponse is null) return 3;
                    var lastPlayDateTime = userSettingResponse.LastPlayDateTime;
                    var diffMinutes = (inputDateTime - lastPlayDateTime).Duration().TotalMinutes;
                    if (diffMinutes > 5) return 5;
                }
                if (userCredential.Password != "") return 4;
                if (inputPassword != inputConfirmPassword) return 2;
                var salt = CreateSalt();
                var request = new SetPasswordRequest
                {
                    Baid = user.Baid,
                    Password = ComputeHash(inputPassword, salt),
                    Salt = salt
                };
                var responseMessage = await client.PostAsJsonAsync("api/Credentials", request);
                return responseMessage.IsSuccessStatusCode ? 1 : 3;
            }
        }

        return 3;
    }

    private static string CreateSalt()
    {
        //Generate a cryptographic random number.
        var randomNumber = new byte[32];
        var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var salt = Convert.ToBase64String(randomNumber);

        // Return a Base64 string representation of the random number.
        return salt;
    }

    private static string ComputeHash(string inputPassword, string salt)
    {
        var encDataByte = Encoding.UTF8.GetBytes(inputPassword + salt);
        var encodedData = Convert.ToBase64String(encDataByte);
        encDataByte = Encoding.UTF8.GetBytes(encodedData);
        encodedData = Convert.ToBase64String(encDataByte);
        encDataByte = Encoding.UTF8.GetBytes(encodedData);
        encodedData = Convert.ToBase64String(encDataByte);
        encDataByte = Encoding.UTF8.GetBytes(encodedData);
        encodedData = Convert.ToBase64String(encDataByte);
        return encodedData;
    }

    public async Task<int> ChangePassword(string inputCardNum, string inputOldPassword, string inputNewPassword,
        string inputConfirmNewPassword, DashboardResponse response, HttpClient client)
    {
        if (OnlyAdmin) return 0;
        foreach (var user in response.Users.Where(user => user.AccessCodes.Contains(inputCardNum)))
        {
            foreach (var userCredential in response.UserCredentials.Where(userCredential => userCredential.Baid == user.Baid))
            {
                if (userCredential.Password != ComputeHash(inputOldPassword, userCredential.Salt)) return 4;
                if (inputNewPassword != inputConfirmNewPassword) return 2;
                var request = new SetPasswordRequest
                {
                    Baid = user.Baid,
                    Password = ComputeHash(inputNewPassword, userCredential.Salt),
                    Salt = userCredential.Salt
                };
                var responseMessage = await client.PostAsJsonAsync("api/Credentials", request);
                return responseMessage.IsSuccessStatusCode ? 1 : 3;
            }
        }

        return 3;
    }

    public void Logout()
    {
        IsLoggedIn = false;
        LoggedInUser = new User();
        IsAdmin = false;
    }

    public User GetLoggedInUser()
    {
        return LoggedInUser;
    }

    public void ResetLoggedInUser(DashboardResponse? response)
    {
        if (response is null) return;
        var baid = LoggedInUser.Baid;
        var newLoggedInUser = response.Users.FirstOrDefault(u => u.Baid == baid);
        if (newLoggedInUser is null) return;
        LoggedInUser = newLoggedInUser;
    }

    public async Task<int> BindAccessCode(string inputAccessCode, HttpClient client)
    {
        if (inputAccessCode.Trim() == "") return 4;
        if (!IsLoggedIn) return 0;
        if (LoggedInUser.AccessCodes.Count >= boundAccessCodeUpperLimit) return 2;
        var request = new BindAccessCodeRequest
        {
            AccessCode = inputAccessCode,
            Baid = LoggedInUser.Baid
        };
        var responseMessage = await client.PostAsJsonAsync("api/Cards", request);
        return responseMessage.IsSuccessStatusCode ? 1 : 3;
    }
}
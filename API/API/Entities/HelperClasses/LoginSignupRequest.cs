namespace API.Entities.Users;

public class LoginSignupRequest
{
    public string username { get; set; }
    public string password { get; set; }

    public LoginSignupRequest(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}
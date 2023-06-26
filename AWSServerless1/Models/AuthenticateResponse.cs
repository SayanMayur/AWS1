namespace WebApi.Models;

using WebApi.Entities;

public class AuthenticateResponse
{
    public int user_key { get; set; }
    public string user_name { get; set; }
    public string user_type { get; set; }
    public int user_center_link_count { get; set; }
    public int center_key { get; set; }

    public string Token { get; set; }


    public AuthenticateResponse(User user, string token)
    {
        user_key = user.user_key;
        user_name = user.user_name;
        user_type = user.user_type;
        user_center_link_count = user.user_center_link_count; 
        center_key = user.center_key;
        
        Token = token;
    }
}

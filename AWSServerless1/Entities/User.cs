namespace WebApi.Entities;

using System.Text.Json.Serialization;

public class User
{
    public int user_key { get; set; }
    public string user_name { get; set; }

    public string user_id { get; set; }
    public string user_type { get; set; }
    public int user_center_link_count { get; set; }
    public int center_key { get; set; }

    [JsonIgnore]
    public string user_password { get; set; }
}
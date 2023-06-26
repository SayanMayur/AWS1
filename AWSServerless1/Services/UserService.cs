namespace WebApi.Services;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models;

using TeleradiologyDataAccess;
using TeleradiologyCore.Extensions;
using MySqlX.XDevAPI;

public interface ICommonService
{ 

}

public interface IUserService:ICommonService
{
    AuthenticateResponse Authenticate(AuthenticateRequest model);
    IReadOnlyList<User> GetAll();

    IEnumerable<User> GetPatient();

    User GetById(int user_key);

    
}

public class UserService : IUserService
{
    // users hardcoded for simplicity, store in a db with hashed passwords in production applications
    /*private List<User> _users = new List<User>
    {
        new User { user_key = 1, user_name = "test", user_type="A", user_password = "test" }
    };*/
    
private List<User> _users;

    private readonly AppSettings _appSettings;

    public UserService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    
    public AuthenticateResponse Authenticate(AuthenticateRequest model)
    {
        /*
        using (MySql.Data.MySqlClient.MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection(_appSettings.ConnectionString))
        {
            conn.Open();
            MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand("SELECT u.user_key, u.user_id, u.user_name, u.user_type, u.user_password from m_user u;", conn);

            //admin_login
            //cmd.Parameters.Add("api_center_id", MySql.Data.MySqlClient.MySqlDbType.VarChar, 500,"");
            //cmd.Parameters.Add("api_user_id", MySql.Data.MySqlClient.MySqlDbType.VarChar, 500, model.Username);
            //cmd.Parameters.Add("api_user_password", MySql.Data.MySqlClient.MySqlDbType.VarChar, 500, model.Password);

            //cmd.Parameters.AddWithValue("@api_user_id", model.Username);
            //cmd.Parameters.AddWithValue("@api_user_password", model.Password);

            MySql.Data.MySqlClient.MySqlDataAdapter da = new MySql.Data.MySqlClient.MySqlDataAdapter();
            DataSet ds = new DataSet();
            da.SelectCommand = cmd;
            var retval = da.Fill(ds);
            //_users = ds.Tables[0].TableToList<User>();


        }
        */
        //_appSettings.ConnectionString = "Server=mymariadbtest1.ci4xfhuv9q33.us-east-1.rds.amazonaws.com;Port=3306;User ID=admin;Password=mymariadb1;Database=mymariadb1";
        var _db = new DatabaseContext(_appSettings.ConnectionString);

        _db.OpenConnection();
        
        string sql = "call admin_login('','" + model.Username + "','" + model.Password + "')";

        var ds = _db.ExecuteDataSet(sql, CommandType.Text);
        
        _users = ds.Tables[0].TableToList<User>();
        var user = _users.SingleOrDefault(x => x.user_id == model.Username && x.user_password == model.Password);

        // return null if user not found
        if (user == null) return null;

        // authentication successful so generate jwt token
        var token = generateJwtToken(user);

        
        return new AuthenticateResponse(user, token);
    }

    public IReadOnlyList<User> GetAll()
    {
        return _users;
    }

    public IEnumerable<User> GetPatient()
    {
        return _users;
    }
    
    public User GetById(int user_key)
    {
        var _db = new DatabaseContext(_appSettings.ConnectionString);
        _db.OpenConnection();

        string sql = "call fetch_user_dtls(" + user_key + ")";
        var ds = _db.ExecuteDataSet(sql, CommandType.Text);

        _users = ds.Tables[0].TableToList<User>();

        return _users.FirstOrDefault(x => x.user_key == user_key);
    }

    // helper methods

    private string generateJwtToken(User user)
    {
        // generate token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", user.user_key.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(_appSettings.TokenExpiryInMinute),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
        //return "MTR";
    }


}
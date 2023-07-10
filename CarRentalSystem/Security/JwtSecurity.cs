using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRentalSystem.Security
{
    public class JwtSecurity
    {
        public readonly string SecretKey;
        public int TokenDuration { get; set; }

        public readonly IConfiguration configuration;
        public JwtSecurity(IConfiguration _configuration)
        {
            configuration = _configuration;
            this.SecretKey = configuration.GetSection("jwtConfig").GetSection("Key").Value;
            this.TokenDuration = Int32.Parse(configuration.GetSection("jwtConfig").GetSection("Duration").Value);
        }
        //Generating Token
        public string GenerateToken(
        Guid UserId,
        string Firstname,
        string Lastname,
        string Email,
        long Mobilenumber,
        string Role)
        {
            //comming from appsettings.json file to this class and then we are using this as a property
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.SecretKey));

            //key is our secretKey and we are using one algorithm for creating our signature 
            //signature is a combination of {  Header+payload+secretKey+Algoritm  }
            var signature = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //this will carry all of the details of user. { using claims } 
            var payload = new[]
            {
                new Claim("id",UserId.ToString()),
                new Claim("firstname",Firstname),
                new Claim("lastname",Lastname),
                new Claim("email",Email),
                new Claim("mobilenumber",Convert.ToString(Mobilenumber)),
                new Claim("role",Role),
            };
            //we are generating token using these variables and then we will convert in to string 
            //so that we can send this to browser
            var jwtToken = new JwtSecurityToken(
                issuer: "localhost",
                audience: "localhost",
                claims: payload,
                expires: DateTime.Now.AddMinutes(TokenDuration),
                signingCredentials: signature
                );
            //converting this jwtToken into string for returning purpose
            string finalToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return finalToken;
        }
    }
}

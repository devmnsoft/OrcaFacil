using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OrcaFacil.Web.Email;
namespace OrcaFacil.Web.Security;
public interface IPasswordResetTokenService { string Generate(); string Hash(string token); bool FixedTimeEquals(string left,string right); }
public sealed class PasswordResetTokenService(IOptions<SecuritySecretOptions> options):IPasswordResetTokenService
{
 public string Generate()=>WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
 public string Hash(string token){using var h=new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.PasswordResetPepper));return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();}
 public bool FixedTimeEquals(string left,string right)=>CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(left),Encoding.ASCII.GetBytes(right));
}

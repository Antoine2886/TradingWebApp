using System;
using System.Threading.Tasks;

namespace WebApp.Utilities.Token
{
    public interface ITokenRepository
    {
        Task<string> CreateTokenAsync(Guid userId);
        Task<bool> IsValidTokenAsync(Guid userId, string token);
        Task DeleteTokenAsync(Guid userId, string token);
    }
}

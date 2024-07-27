using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace WebApp.Utilities.Token
{
    /// <summary>
    /// Utility class for managing tokens associated with user authentication.
    /// Antoine Bélanger
    /// </summary>
    public class TokenUtility : ITokenRepository
    {
        private readonly Context _context;


        public TokenUtility(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates a unique token value, creates a new token record in the database associated with the specified user ID, and returns the generated token value.
        /// </summary>
        /// <param name="userId">The unique identifier of the user for whom the token is being created.</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated token value.</returns>
        public async Task<string> CreateTokenAsync(Guid userId)
        {
            var tokenValue = GenerateUniqueTokenValue();

            var token = new Bd.Infrastructure.Token
            {
                Id = Guid.NewGuid(), // Generate a new unique identifier for the token
                UserId = userId,
                TokenValue = tokenValue,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            };

            await _context.Tokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return tokenValue; // Return the generated token value
        }


        /// <summary>
        /// Generate a random token of 32 bytes
        /// </summary>
        /// <returns></returns>
        private string GenerateUniqueTokenValue()
        {
            // Generate a random token value
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // Convert the token bytes to a string
            string tokenValue = Convert.ToBase64String(tokenBytes);

            return tokenValue;
        }

        /// <summary>
        /// Deletes the token associated with the specified user ID and token value from the database, if it exists.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose token is to be deleted.</param>
        /// <param name="token">The token value to be deleted.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task DeleteTokenAsync(Guid userId, string token)
        {
            var tokenToDelete = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenValue == token);
            if (tokenToDelete != null)
            {
                _context.Tokens.Remove(tokenToDelete);
                await _context.SaveChangesAsync();
            }
        }



        /// <summary>
        /// Checks if the specified token is valid for the given user ID and has not expired.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose token is being validated.</param>
        /// <param name="token">The token value to be validated.</param>
        /// <returns>A Task representing the asynchronous operation that returns true if the token is valid and false otherwise.</returns>
        public async Task<bool> IsValidTokenAsync(Guid userId, string token)
        {
            var existingToken = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenValue == token && t.Expiration > DateTime.UtcNow);
            return existingToken != null;
        }
    }

}

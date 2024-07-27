using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bd.Infrastructure
{
    /// <summary>
    /// Token class for security purpose (we have 2 style of token) 1st is Identity and the second is this one (custom
    /// </summary>
    public class Token
    {
        public Guid Id { get; set; } // Unique identifier for the token
        public Guid UserId { get; set; } // User ID associated with the token
        public string? TokenValue { get; set; } // The token value itself
        public DateTime Expiration { get; set; } // Expiration timestamp for the token
    }
}

using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bd.Enums
{
    public sealed class Roles
    {
        public static IdentityRole<Guid> Administateur => new IdentityRole<Guid>("Administrateur");
        public static IdentityRole<Guid> Utilisateur => new IdentityRole<Guid>("Utilisateur");
    }
}

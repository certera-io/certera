using Certera.Data.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Certera.Data
{
    public partial class ApplicationUser : IdentityUser<long>
    {
        public NotificationSetting NotificationSetting { get; set; }

        public virtual ICollection<UserConfiguration> UserConfigurations { get; set; } = new List<UserConfiguration>();
    }
}

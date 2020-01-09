using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Certera.Data
{
    public partial class DataContext :
        IdentityDbContext<ApplicationUser, Role, long, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public DataContext()
        {
        }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }
        
        static DataContext()
        {
            // required to initialise native SQLite libraries on some platforms.
            SQLitePCL.Batteries_V2.Init();

            // https://github.com/aspnet/EntityFrameworkCore/issues/9994#issuecomment-508588678
            SQLitePCL.raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=Certera.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>()
                .HasIndex(x => x.ApiKey1)
                .IsUnique();
            builder.Entity<ApplicationUser>()
                .HasIndex(x => x.ApiKey2)
                .IsUnique();

            ConfigureDataModels(builder);

            base.OnModelCreating(builder);
        }
    }

    public partial class ApplicationUser : IdentityUser<long>
    {
        [DisplayName("API Key 1")]
        public string ApiKey1 { get; set; }

        [DisplayName("API Key 2")]
        public string ApiKey2 { get; set; }
    }

    public class UserLogin : IdentityUserLogin<long> { }
    public class UserRole : IdentityUserRole<long> { }
    public class UserClaim : IdentityUserClaim<long> { }

    public class Role : IdentityRole<long>
    {
        public Role() { }
        public Role(string role) : base(role) { }
    }

    public class RoleClaim : IdentityRoleClaim<long> { }
    public class UserToken : IdentityUserToken<long> { }
}

using Microsoft.EntityFrameworkCore.Migrations;
using System.Linq;

namespace Certera.Data.Migrations
{
    public partial class SetApiKeyValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using (var ctx = new DataContext())
            {
                var certs = ctx.AcmeCertificates.Where(x => x.ApiKey1 == null).ToList();
                foreach (var cert in certs)
                {
                    cert.ApiKey1 = ApiKeyGenerator.CreateApiKey();
                    cert.ApiKey2 = ApiKeyGenerator.CreateApiKey();
                }

                var keys = ctx.Keys.Where(x => x.ApiKey1 == null).ToList();
                foreach (var key in keys)
                {
                    key.ApiKey1 = ApiKeyGenerator.CreateApiKey();
                    key.ApiKey2 = ApiKeyGenerator.CreateApiKey();
                }
                ctx.SaveChanges();
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

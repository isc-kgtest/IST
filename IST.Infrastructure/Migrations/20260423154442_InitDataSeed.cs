using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IST.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitDataSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "access_control",
                table: "roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("e2c2c0d5-1c5c-482a-904b-3549cd0ebba0"), new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), null, null, null, "admin role", false, "admin", null, null });

            migrationBuilder.InsertData(
                schema: "access_control",
                table: "users",
                columns: new[] { "Id", "CertificateThumbprint", "CertificateValidUntil", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Department", "EMail", "IsActive", "LastDateLogin", "Login", "Name", "OrganizationId", "Password", "PasswordExpiryDate", "Patronymic", "PhoneNumber", "Position", "Surname", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("8a92bd3a-58c3-433b-8255-8abef51421b0"), null, null, new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), null, null, null, "IT Отдел", "admin@system.local", true, new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), "admin", "Админ", new Guid("00000000-0000-0000-0000-000000000000"), "$argon2id$v=19$m=32768,t=4,p=1$j8Hfb0sAcmWRKWanHyDh9A$spaN8XOINtX1M0PNUV4esKl02Fdv/2Fxr45V5aSJbfo", new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), "Админович", "+00000000000", "Системный администратор", "Системов", null, null });

            migrationBuilder.InsertData(
                schema: "access_control",
                table: "user_roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EndDate", "IsDeleted", "RoleId", "StartDate", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("c8a2b5e3-4f91-4d9a-8b8a-7c9b8e1a2f3b"), new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), null, null, null, new DateTime(2035, 7, 3, 10, 48, 0, 0, DateTimeKind.Utc), false, new Guid("e2c2c0d5-1c5c-482a-904b-3549cd0ebba0"), new DateTime(2026, 4, 21, 10, 48, 0, 0, DateTimeKind.Utc), null, null, new Guid("8a92bd3a-58c3-433b-8255-8abef51421b0") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "access_control",
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("c8a2b5e3-4f91-4d9a-8b8a-7c9b8e1a2f3b"));

            migrationBuilder.DeleteData(
                schema: "access_control",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("e2c2c0d5-1c5c-482a-904b-3549cd0ebba0"));

            migrationBuilder.DeleteData(
                schema: "access_control",
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("8a92bd3a-58c3-433b-8255-8abef51421b0"));
        }
    }
}

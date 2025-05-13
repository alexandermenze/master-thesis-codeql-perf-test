using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerPortal.UserAuthService.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNoOnApprove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerNo",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerNo",
                table: "Users");
        }
    }
}

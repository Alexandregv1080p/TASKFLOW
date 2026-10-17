using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectAttachmentBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AttachmentBytes",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Projetos com anexos de antes da coluna começam com o total que já usam.
            migrationBuilder.Sql("""
                UPDATE "Projects" p
                SET "AttachmentBytes" = COALESCE((
                    SELECT SUM(a."SizeBytes")
                    FROM "TaskAttachments" a
                    JOIN "Tasks" t ON t."Id" = a."TaskId"
                    WHERE t."ProjectId" = p."Id"), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentBytes",
                table: "Projects");
        }
    }
}

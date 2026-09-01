using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenoDev.BusinessTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMailing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "mail_snippets",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_snippets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "smtp_accounts",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    use_start_tls = table.Column<bool>(type: "boolean", nullable: false),
                    user_name = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reply_to_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_smtp_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mail_templates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    smtp_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    html_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_mail_templates_smtp_accounts_smtp_account_id",
                        column: x => x.smtp_account_id,
                        principalSchema: "sales",
                        principalTable: "smtp_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mail_template_attachments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_template_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_mail_template_attachments_mail_templates_mail_template_id",
                        column: x => x.mail_template_id,
                        principalSchema: "sales",
                        principalTable: "mail_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outgoing_emails",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    smtp_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resent_from_email_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subject = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outgoing_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_outgoing_emails_mail_templates_mail_template_id",
                        column: x => x.mail_template_id,
                        principalSchema: "sales",
                        principalTable: "mail_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_outgoing_emails_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "business_tracker",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outgoing_emails_outgoing_emails_resent_from_email_id",
                        column: x => x.resent_from_email_id,
                        principalSchema: "sales",
                        principalTable: "outgoing_emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_outgoing_emails_smtp_accounts_smtp_account_id",
                        column: x => x.smtp_account_id,
                        principalSchema: "sales",
                        principalTable: "smtp_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outgoing_email_attachments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outgoing_email_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: true),
                    content_deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outgoing_email_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_outgoing_email_attachments_outgoing_emails_outgoing_email_id",
                        column: x => x.outgoing_email_id,
                        principalSchema: "sales",
                        principalTable: "outgoing_emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mail_snippets_key",
                schema: "sales",
                table: "mail_snippets",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mail_snippets_name",
                schema: "sales",
                table: "mail_snippets",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mail_template_attachments_mail_template_id_sort_order",
                schema: "sales",
                table: "mail_template_attachments",
                columns: new[] { "mail_template_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_mail_templates_name",
                schema: "sales",
                table: "mail_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mail_templates_smtp_account_id",
                schema: "sales",
                table: "mail_templates",
                column: "smtp_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_email_attachments_outgoing_email_id_sort_order",
                schema: "sales",
                table: "outgoing_email_attachments",
                columns: new[] { "outgoing_email_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_emails_mail_template_id",
                schema: "sales",
                table: "outgoing_emails",
                column: "mail_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_emails_order_id_created_at_utc",
                schema: "sales",
                table: "outgoing_emails",
                columns: new[] { "order_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_emails_resent_from_email_id",
                schema: "sales",
                table: "outgoing_emails",
                column: "resent_from_email_id");

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_emails_smtp_account_id",
                schema: "sales",
                table: "outgoing_emails",
                column: "smtp_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_outgoing_emails_status_created_at_utc",
                schema: "sales",
                table: "outgoing_emails",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_smtp_accounts_is_default",
                schema: "sales",
                table: "smtp_accounts",
                column: "is_default",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_smtp_accounts_name",
                schema: "sales",
                table: "smtp_accounts",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mail_snippets",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "mail_template_attachments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "outgoing_email_attachments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "outgoing_emails",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "mail_templates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "smtp_accounts",
                schema: "sales");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketingApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    changed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_log_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rolename = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salesman",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    salesmancode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    salesmanname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mobileno = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    joiningdate = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    createddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifieddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("salesman_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passwordhash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    roleid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_roles",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marketingteam",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    teamcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    teamname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    teamleaderid = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    createddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    createdby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modifieddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modifiedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("marketingteam_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_marketingteam_teamleader",
                        column: x => x.teamleaderid,
                        principalTable: "salesman",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marketingteamdetail",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    marketingteamid = table.Column<long>(type: "bigint", nullable: false),
                    salesmanid = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<short>(type: "smallint", nullable: false),
                    targetamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("marketingteamdetail_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_teamdetail_salesman",
                        column: x => x.salesmanid,
                        principalTable: "salesman",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_teamdetail_team",
                        column: x => x.marketingteamid,
                        principalTable: "marketingteam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "salesman_deactivation_request",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    salesmanid = table.Column<long>(type: "bigint", nullable: false),
                    salesmancode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    salesmanname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    teamid = table.Column<long>(type: "bigint", nullable: true),
                    teamcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    teamname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    roleinteam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requestedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    requesteddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    processeddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processedby = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("salesman_deactivation_request_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_salesman_deactivation_request_marketingteam_team_id",
                        column: x => x.teamid,
                        principalTable: "marketingteam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_salesman_deactivation_request_salesman_salesman_id",
                        column: x => x.salesmanid,
                        principalTable: "salesman",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_changed_at",
                table: "audit_log",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_name_entity_id",
                table: "audit_log",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_marketingteam_team_code",
                table: "marketingteam",
                column: "teamcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marketingteam_team_leader_id",
                table: "marketingteam",
                column: "teamleaderid");

            migrationBuilder.CreateIndex(
                name: "ix_marketingteamdetail_marketing_team_id_salesman_id",
                table: "marketingteamdetail",
                columns: new[] { "marketingteamid", "salesmanid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marketingteamdetail_salesman_id",
                table: "marketingteamdetail",
                column: "salesmanid",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_roles_role_name",
                table: "roles",
                column: "rolename",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salesman_email",
                table: "salesman",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salesman_mobile_no",
                table: "salesman",
                column: "mobileno",
                unique: true,
                filter: "mobileno IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_salesman_salesman_code",
                table: "salesman",
                column: "salesmancode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salesman_deactivation_request_salesman_id",
                table: "salesman_deactivation_request",
                column: "salesmanid");

            migrationBuilder.CreateIndex(
                name: "ix_salesman_deactivation_request_team_id",
                table: "salesman_deactivation_request",
                column: "teamid");

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "users",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "marketingteamdetail");

            migrationBuilder.DropTable(
                name: "salesman_deactivation_request");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "marketingteam");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "salesman");
        }
    }
}

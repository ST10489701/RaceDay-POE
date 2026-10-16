using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
                    table.ForeignKey(
                        name: "FK_Events_Users_OrganiserId",
                        column: x => x.OrganiserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EntryFee = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                    table.ForeignKey(
                        name: "FK_Categories_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ElevationGainM = table.Column<int>(type: "int", nullable: true),
                    MapUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteId);
                    table.ForeignKey(
                        name: "FK_Routes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventEnrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BibNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEnrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_EventEnrolments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventEnrolments_Users_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Finished")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DQ')");
                    table.ForeignKey(
                        name: "FK_Results_EventEnrolments_EnrolmentId",
                        column: x => x.EnrolmentId,
                        principalTable: "EventEnrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventId",
                table: "Categories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryId",
                table: "EventEnrolments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_Participant_Category",
                table: "EventEnrolments",
                columns: new[] { "ParticipantId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserId",
                table: "Events",
                column: "OrganiserId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrolmentId",
                table: "Results",
                column: "EnrolmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_EventId",
                table: "Routes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "EventEnrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

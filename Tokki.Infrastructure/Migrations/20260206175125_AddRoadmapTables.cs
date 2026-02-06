using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tokki.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRoadmaps",
                columns: table => new
                {
                    UserRoadmapId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TargetAim = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    OverallAiAssessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemPromptContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoadmaps", x => x.UserRoadmapId);
                    table.ForeignKey(
                        name: "FK_UserRoadmaps_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapKnowledgeProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UserRoadmapId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    QuestionTypeId = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MasteryScore = table.Column<double>(type: "float", nullable: false),
                    IsWeakness = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapKnowledgeProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_RoadmapKnowledgeProfiles_QuestionTypes_QuestionTypeId",
                        column: x => x.QuestionTypeId,
                        principalTable: "QuestionTypes",
                        principalColumn: "QuestionTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadmapKnowledgeProfiles_UserRoadmaps_UserRoadmapId",
                        column: x => x.UserRoadmapId,
                        principalTable: "UserRoadmaps",
                        principalColumn: "UserRoadmapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapWeeks",
                columns: table => new
                {
                    RoadmapWeekId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UserRoadmapId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    WeekIndex = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekFocusGoal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WeeklyExamId = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true) 
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapWeeks", x => x.RoadmapWeekId);
                    table.ForeignKey(
                        name: "FK_RoadmapWeeks_Exams_WeeklyExamId",
                        column: x => x.WeeklyExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_RoadmapWeeks_UserRoadmaps_UserRoadmapId",
                        column: x => x.UserRoadmapId,
                        principalTable: "UserRoadmaps",
                        principalColumn: "UserRoadmapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapDailyTasks",
                columns: table => new
                {
                    TaskId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    RoadmapWeekId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DayIndex = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    AiGeneratedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetQuestionTypeId = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapDailyTasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_RoadmapDailyTasks_QuestionTypes_TargetQuestionTypeId",
                        column: x => x.TargetQuestionTypeId,
                        principalTable: "QuestionTypes",
                        principalColumn: "QuestionTypeId");
                    table.ForeignKey(
                        name: "FK_RoadmapDailyTasks_RoadmapWeeks_RoadmapWeekId",
                        column: x => x.RoadmapWeekId,
                        principalTable: "RoadmapWeeks",
                        principalColumn: "RoadmapWeekId",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_RoadmapDailyTasks_RoadmapWeekId",
                table: "RoadmapDailyTasks",
                column: "RoadmapWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapDailyTasks_TargetQuestionTypeId",
                table: "RoadmapDailyTasks",
                column: "TargetQuestionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapKnowledgeProfiles_QuestionTypeId",
                table: "RoadmapKnowledgeProfiles",
                column: "QuestionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapKnowledgeProfiles_UserRoadmapId",
                table: "RoadmapKnowledgeProfiles",
                column: "UserRoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapWeeks_UserRoadmapId",
                table: "RoadmapWeeks",
                column: "UserRoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapWeeks_WeeklyExamId",
                table: "RoadmapWeeks",
                column: "WeeklyExamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoadmaps_UserId",
                table: "UserRoadmaps",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "RoadmapDailyTasks");

            migrationBuilder.DropTable(
                name: "RoadmapKnowledgeProfiles");

            migrationBuilder.DropTable(
                name: "RoadmapWeeks");

            migrationBuilder.DropTable(
                name: "UserRoadmaps");
        }
    }
}
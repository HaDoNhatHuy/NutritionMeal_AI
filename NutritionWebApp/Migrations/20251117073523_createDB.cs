using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class createDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MuscleGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YoutubeVideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseVideo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<float>(type: "real", nullable: true),
                    Weight = table.Column<float>(type: "real", nullable: true),
                    Gender = table.Column<bool>(type: "bit", nullable: true),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pathology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "BodyMeasurements",
                columns: table => new
                {
                    MeasurementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MeasureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: true),
                    Waist = table.Column<float>(type: "real", nullable: true),
                    Chest = table.Column<float>(type: "real", nullable: true),
                    Hips = table.Column<float>(type: "real", nullable: true),
                    Arms = table.Column<float>(type: "real", nullable: true),
                    Thighs = table.Column<float>(type: "real", nullable: true),
                    BodyFatPercentage = table.Column<float>(type: "real", nullable: true),
                    MuscleMass = table.Column<float>(type: "real", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMeasurements", x => x.MeasurementId);
                    table.ForeignKey(
                        name: "FK_BodyMeasurements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FoodName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Calories = table.Column<double>(type: "float", nullable: false),
                    Protein = table.Column<double>(type: "float", nullable: false),
                    Carbs = table.Column<double>(type: "float", nullable: false),
                    Fat = table.Column<double>(type: "float", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MealType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_FoodHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Habits",
                columns: table => new
                {
                    HabitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HabitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HabitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GoalValue = table.Column<int>(type: "int", nullable: true),
                    IconEmoji = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habits", x => x.HabitId);
                    table.ForeignKey(
                        name: "FK_Habits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    MealPlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyCalories = table.Column<int>(type: "int", nullable: false),
                    DailyProtein = table.Column<double>(type: "float", nullable: false),
                    DailyCarbs = table.Column<double>(type: "float", nullable: false),
                    DailyFat = table.Column<double>(type: "float", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Budget = table.Column<double>(type: "float", nullable: true),
                    MealPlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.MealPlanId);
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RecipeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CookingTime = table.Column<int>(type: "int", nullable: true),
                    Servings = table.Column<int>(type: "int", nullable: false),
                    Calories = table.Column<double>(type: "float", nullable: false),
                    Protein = table.Column<double>(type: "float", nullable: false),
                    Carbs = table.Column<double>(type: "float", nullable: false),
                    Fat = table.Column<double>(type: "float", nullable: false),
                    Ingredients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Likes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.RecipeId);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WaterIntake",
                columns: table => new
                {
                    IntakeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IntakeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalMl = table.Column<int>(type: "int", nullable: false),
                    GoalMl = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterIntake", x => x.IntakeId);
                    table.ForeignKey(
                        name: "FK_WaterIntake_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPlans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_WorkoutPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HabitId = table.Column<int>(type: "int", nullable: false),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    ActualValue = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_HabitLogs_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "HabitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeLikes",
                columns: table => new
                {
                    LikeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeLikes", x => x.LikeId);
                    table.ForeignKey(
                        name: "FK_RecipeLikes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaterLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntakeId = table.Column<int>(type: "int", nullable: false),
                    AmountMl = table.Column<int>(type: "int", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_WaterLogs_WaterIntake_IntakeId",
                        column: x => x.IntakeId,
                        principalTable: "WaterIntake",
                        principalColumn: "IntakeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutProgress",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    ExerciseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SetsCompleted = table.Column<int>(type: "int", nullable: false),
                    RepsCompleted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutProgress", x => x.ProgressId);
                    table.ForeignKey(
                        name: "FK_WorkoutProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutProgress_WorkoutPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "WorkoutPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyMeasurements_UserId",
                table: "BodyMeasurements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_UserId",
                table: "ChatHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodHistory_UserId",
                table: "FoodHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitLogs_HabitId_LogDate",
                table: "HabitLogs",
                columns: new[] { "HabitId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Habits_UserId",
                table: "Habits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId",
                table: "MealPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeLikes_RecipeId_UserId",
                table: "RecipeLikes",
                columns: new[] { "RecipeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeLikes_UserId",
                table: "RecipeLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedByUserId",
                table: "Recipes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterIntake_UserId_IntakeDate",
                table: "WaterIntake",
                columns: new[] { "UserId", "IntakeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaterLogs_IntakeId",
                table: "WaterLogs",
                column: "IntakeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_UserId",
                table: "WorkoutPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgress_PlanId",
                table: "WorkoutProgress",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgress_UserId",
                table: "WorkoutProgress",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyMeasurements");

            migrationBuilder.DropTable(
                name: "ChatHistory");

            migrationBuilder.DropTable(
                name: "ExerciseVideo");

            migrationBuilder.DropTable(
                name: "FoodHistory");

            migrationBuilder.DropTable(
                name: "HabitLogs");

            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "RecipeLikes");

            migrationBuilder.DropTable(
                name: "WaterLogs");

            migrationBuilder.DropTable(
                name: "WorkoutProgress");

            migrationBuilder.DropTable(
                name: "Habits");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "WaterIntake");

            migrationBuilder.DropTable(
                name: "WorkoutPlans");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Seeds;

public static class DevelopmentDataSeeder
{
    private const string ThemeName = "ITP Workshop";
    private const string QuizTitle = "ITP Workshop: Objects";

    private static readonly SeedQuestion[] ObjectQuestions =
    [
        new(
            "What is logged by console.log(person1.name)?",
            QuestionDifficulty.Easy,
            ["Abdi", "Shadi", "London", "undefined"],
            0),
        new(
            "What is logged by console.log(person2[\"name\"])?",
            QuestionDifficulty.Easy,
            ["Abdi", "Shadi", "Software Engineer", "undefined"],
            1),
        new(
            "What is logged by console.log(person1.id_number > person2[\"id_number\"])?",
            QuestionDifficulty.Easy,
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.job)?",
            QuestionDifficulty.Medium,
            ["Software Engineer", "London", "undefined", "null"],
            2),
        new(
            "What is logged by console.log(person1.location === person2.location)?",
            QuestionDifficulty.Medium,
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.location === person3.location)?",
            QuestionDifficulty.Hard,
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person2.location === person3.location) after person3.location is changed?",
            QuestionDifficulty.Hard,
            ["true, because person2 and person3 reference the same object", "false, because person3 is a copy of person2", "undefined", "It throws an error"],
            0),
    ];

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var theme = await dbContext.QuizThemes
            .SingleOrDefaultAsync(current => current.Name == ThemeName, cancellationToken);

        if (theme is null)
        {
            theme = new QuizTheme { Name = ThemeName };
            dbContext.QuizThemes.Add(theme);
        }

        var quiz = await dbContext.Quizes
            .SingleOrDefaultAsync(current => current.Title == QuizTitle, cancellationToken);

        if (quiz is null)
        {
            quiz = new Quiz
            {
                Title = QuizTitle,
                ThemeId = theme.Id,
                QuestionsPerGame = ObjectQuestions.Length,
            };
            dbContext.Quizes.Add(quiz);
        }

        var existingQuestionTexts = await dbContext.Questions
            .Where(question => question.ThemeId == theme.Id)
            .Select(question => question.Text)
            .ToHashSetAsync(cancellationToken);

        foreach (var seedQuestion in ObjectQuestions.Where(question => !existingQuestionTexts.Contains(question.Text)))
        {
            var questionId = Guid.NewGuid();
            var options = seedQuestion.Options
                .Select(text => new AnswerOption { QuestionId = questionId, Text = text })
                .ToList();

            dbContext.Questions.Add(new Question
            {
                Id = questionId,
                ThemeId = theme.Id,
                Text = seedQuestion.Text,
                Difficulty = seedQuestion.Difficulty,
                Options = options,
                CorrectOptionId = options[seedQuestion.CorrectOptionIndex].Id,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedQuestion(
        string Text,
        QuestionDifficulty Difficulty,
        string[] Options,
        int CorrectOptionIndex);
}

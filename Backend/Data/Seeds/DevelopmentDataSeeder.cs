using Backend.Features.Quizes;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data.Seeds;

public static class DevelopmentDataSeeder
{
    private const string ThemeName = "ITP Workshop";
    private const string QuizTitle = "ITP Workshop: Objects";
    private const string ObjectsCode = """
        const person1 = {
            "name": "Abdi",
            "location": "London",
            "id_number": 17,
        };

        const person2 = {
            "name": "Shadi",
            "job": "Software Engineer",
            "location": "London",
            "id_number": 28,
        };

        const person3 = person2;

        person3.location = "Manchester";
        """;

    private static readonly SeedQuestion[] ObjectQuestions =
    [
        new(
            "What is logged by console.log(person1.name)?",
            QuestionDifficulty.Easy,
            "person1 has its own name property with the value \"Abdi\", so dot notation reads that string.",
            ["Abdi", "Shadi", "London", "undefined"],
            0),
        new(
            "What is logged by console.log(person2[\"name\"])?",
            QuestionDifficulty.Easy,
            "Bracket notation reads the property whose key is the string \"name\". On person2 that value is \"Shadi\".",
            ["Abdi", "Shadi", "Software Engineer", "undefined"],
            1),
        new(
            "What is logged by console.log(person1.id_number > person2[\"id_number\"])?",
            QuestionDifficulty.Easy,
            "The comparison is 17 > 28. Since 17 is not greater than 28, JavaScript logs false.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.job)?",
            QuestionDifficulty.Medium,
            "person1 has no job property. Reading a missing object property does not throw an error; it evaluates to undefined.",
            ["Software Engineer", "London", "undefined", "null"],
            2),
        new(
            "What is logged by console.log(person1.location === person2.location)?",
            QuestionDifficulty.Medium,
            "person1.location stays \"London\". person2.location becomes \"Manchester\" through person3, so the strings are not equal.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.location === person3.location)?",
            QuestionDifficulty.Hard,
            "person3 refers to person2 and its location was changed to \"Manchester\". person1.location is still \"London\", so the comparison is false.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person2.location === person3.location) after person3.location is changed?",
            QuestionDifficulty.Hard,
            "person3 = person2 copies the reference, not the object. Changing person3.location also changes person2.location, so both read \"Manchester\" and the comparison is true.",
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

        var existingQuestions = await dbContext.Questions
            .Where(question => question.ThemeId == theme.Id)
            .ToListAsync(cancellationToken);
        var existingQuestionsByText = existingQuestions.ToDictionary(question => question.Text);

        foreach (var seedQuestion in ObjectQuestions)
        {
            if (existingQuestionsByText.TryGetValue(seedQuestion.Text, out var existingQuestion))
            {
                existingQuestion.CodeContext = ObjectsCode;
                existingQuestion.Explanation = seedQuestion.Explanation;
                continue;
            }

            var questionId = Guid.NewGuid();
            var options = seedQuestion.Options
                .Select(text => new AnswerOption { QuestionId = questionId, Text = text })
                .ToList();

            dbContext.Questions.Add(new Question
            {
                Id = questionId,
                ThemeId = theme.Id,
                Text = seedQuestion.Text,
                CodeContext = ObjectsCode,
                Explanation = seedQuestion.Explanation,
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
        string Explanation,
        string[] Options,
        int CorrectOptionIndex);
}

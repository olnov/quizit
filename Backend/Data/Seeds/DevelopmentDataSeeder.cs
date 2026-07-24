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
    private const string ObjectSyntaxCode = """
        const person = {
            "name": "Jemima",
            "location",
            "id_number" = 9,
        };

        console.assert(person.name === "Jemima");
        console.assert(person.location === "Glasgow");
        console.assert(person.id_number === 9);
        """;
    private const string CyfLocationsCode = """
        function checkLivesNearCYF(person) {
            const cyfLocations = ["Birmingham", "Cape Town", "Glasgow", "London", "Manchester"];
            return cyfLocations.includes(person.location);
        }

        const mo = {
            "name": "Mo",
            "city": "Glasgow",
            "focus": "React",
        };

        const sayed = {
            "name": "Sayed",
            "city": "Edinburgh",
            "focus": "SQL",
        };

        console.assert(checkLivesNearCYF(mo));
        console.assert(!checkLivesNearCYF(sayed));
        """;
    private const string SaladRecipeCode = """
        function printSaladRecipe(forPerson) {
            const saladRecipe = {
                "name": "salad",
                "ingredients": ["lettuce", "corn", "carrots", "cucumber"],
                "rating_out_of_10": 8,
                "steps": [
                    "Shred the lettuce",
                    "Cut the carrots into small pieces",
                    "Slice the cucumber",
                    "Mix all the vegetables together in a bowl",
                ]
            };

            if (forPerson.eatsMeat) {
                saladRecipe.push("chicken");
                saladRecipe.steps.push("Mix in the chicken");
            }

            console.log(`For ${forPerson.name} to make ${saladRecipe.name}:`);
            console.log("Get:");
            for (const ingredient of saladRecipe.ingredients) {
                console.log(` * ${ingredient}`);
            }
            for (const step of saladRecipe.steps) {
                console.log(step);
            }
        }

        const person1 = { "name": "Ola", "eatsMeat": true };
        const person2 = { "name": "Steve", "eatsMeat": false };

        printSaladRecipe(person1);
        console.log("");
        printSaladRecipe(person2);
        """;
    private const string FavouriteFoodsCode = """
        const people = [];

        people.push({ "name": "Saqib", "favourite_food": "salad" });
        people.push({ "name": "Shadi", "favourite_food": "mango" });
        people.push({ "name": "Navid", "favourite_food": "macarons" });

        const favouriteFoods = [];

        // TODO: Fill favouriteFoods without typing the food strings.

        console.assert(favouriteFoods.length === 3);
        console.assert(favouriteFoods.includes("salad"));
        console.assert(favouriteFoods.includes("mango"));
        console.assert(favouriteFoods.includes("macarons"));
        """;
    private const string DynamicFieldCode = """
        const person = {
            "name": "Manu",
            "favourite_ice_cream": "vanilla",
            "favourite_topping": "marshmallows",
        };

        function assertFieldEquals(object, field, targetValue) {
            console.assert(object.field === targetValue);
        }

        assertFieldEquals(person, "favourite_ice_cream", "vanilla");
        """;

    private static readonly SeedQuestion[] ObjectQuestions =
    [
        new(
            "What is logged by console.log(person1.name)?",
            100,
            "person1 has its own name property with the value \"Abdi\", so dot notation reads that string.",
            ["Abdi", "Shadi", "London", "undefined"],
            0),
        new(
            "What is logged by console.log(person2[\"name\"])?",
            100,
            "Bracket notation reads the property whose key is the string \"name\". On person2 that value is \"Shadi\".",
            ["Abdi", "Shadi", "Software Engineer", "undefined"],
            1),
        new(
            "What is logged by console.log(person1.id_number > person2[\"id_number\"])?",
            100,
            "The comparison is 17 > 28. Since 17 is not greater than 28, JavaScript logs false.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.job)?",
            100,
            "person1 has no job property. Reading a missing object property does not throw an error; it evaluates to undefined.",
            ["Software Engineer", "London", "undefined", "null"],
            2),
        new(
            "What is logged by console.log(person1.location === person2.location)?",
            100,
            "person1.location stays \"London\". person2.location becomes \"Manchester\" through person3, so the strings are not equal.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person1.location === person3.location)?",
            100,
            "person3 refers to person2 and its location was changed to \"Manchester\". person1.location is still \"London\", so the comparison is false.",
            ["true", "false", "undefined", "It throws an error"],
            1),
        new(
            "What is logged by console.log(person2.location === person3.location) after person3.location is changed?",
            100,
            "person3 = person2 copies the reference, not the object. Changing person3.location also changes person2.location, so both read \"Manchester\" and the comparison is true.",
            ["true, because person2 and person3 reference the same object", "false, because person3 is a copy of person2", "undefined", "It throws an error"],
            0),
    ];

    private static readonly SeedQuestion[] ObjectSyntaxQuestions =
    [
        new(
            "What happens when this program is run?",
            200,
            "The object literal has invalid syntax, so JavaScript throws a SyntaxError while parsing the file. Execution stops before any console.assert call can run.",
            ["It throws a SyntaxError before any assertion runs", "All assertions pass", "Only the location assertion fails", "It logs undefined three times"],
            0,
            ObjectSyntaxCode),
        new(
            "Which line correctly defines the location property so the second assertion can pass?",
            200,
            "Object properties use a colon between a key and a value. The program expects person.location to be the string \"Glasgow\".",
            ["\"location\": \"Glasgow\",", "\"location\" = \"Glasgow\",", "location === \"Glasgow\",", "\"location\", \"Glasgow\","],
            0,
            ObjectSyntaxCode),
        new(
            "Which line correctly defines id_number with the value 9?",
            200,
            "Inside an object literal, a property is written as key: value. The equals sign is assignment syntax and is invalid in this position.",
            ["\"id_number\": 9,", "\"id_number\" = 9,", "id_number === 9,", "\"id_number\", 9,"],
            0,
            ObjectSyntaxCode),
        new(
            "After fixing location to \"Glasgow\" and id_number to 9, what is logged?",
            200,
            "All three conditions are true. console.assert only logs a message when its condition is false, so this program completes with no console output.",
            ["Nothing; all assertions pass", "Three true values", "One assertion failure for location", "undefined"],
            0,
            ObjectSyntaxCode),
    ];

    private static readonly SeedQuestion[] CyfLocationsQuestions =
    [
        new(
            "What happens when the CYF locations program is run?",
            300,
            "mo has a city property but no location property. checkLivesNearCYF(mo) therefore returns false, so the first console.assert fails. The second assertion passes because sayed is also not found in the list.",
            ["The first assertion fails; the second passes", "Both assertions pass", "Both assertions fail", "It throws a SyntaxError"],
            0,
            CyfLocationsCode),
        new(
            "Why does checkLivesNearCYF(mo) return false?",
            300,
            "The function reads person.location, which is undefined for mo. The array does not include undefined, even though mo.city is Glasgow.",
            ["The function reads location, but the objects use city", "Glasgow is missing from cyfLocations", "includes cannot compare strings", "mo.focus must be React"],
            0,
            CyfLocationsCode),
        new(
            "Which change fixes the function while keeping the object data unchanged?",
            300,
            "The objects already consistently use city. Updating the function to read person.city makes it compare Glasgow and Edinburgh against the allowed locations.",
            ["return cyfLocations.includes(person.city);", "return cyfLocations.includes(person.focus);", "return person.location === cyfLocations;", "return cyfLocations.includes(location);"],
            0,
            CyfLocationsCode),
        new(
            "After changing person.location to person.city, what is logged?",
            300,
            "Mo's city, Glasgow, is in the list. Sayed's city, Edinburgh, is not, so the negated second condition is true. Both assertions pass and console.assert produces no output.",
            ["Nothing; both assertions pass", "One failure for Mo", "One failure for Sayed", "true and false"],
            0,
            CyfLocationsCode),
    ];

    private static readonly SeedQuestion[] SaladRecipeQuestions =
    [
        new(
            "What happens when the salad recipe program is run?",
            400,
            "Ola eats meat, so the if block runs. saladRecipe is an object, not an array, and objects do not have a push method. JavaScript throws a TypeError before the recipe is logged, and execution stops before Steve's recipe.",
            ["It throws a TypeError before any recipe is logged", "It prints chicken for Ola and no chicken for Steve", "It logs undefined for chicken", "It throws a SyntaxError"],
            0,
            SaladRecipeCode),
        new(
            "Which change correctly adds chicken to the list of ingredients?",
            400,
            "ingredients is the array inside the saladRecipe object. Array.prototype.push must be called on that array, not on the enclosing object.",
            ["saladRecipe.ingredients.push(\"chicken\");", "saladRecipe.push(\"chicken\");", "saladRecipe.name.push(\"chicken\");", "saladRecipe.steps = \"chicken\";"],
            0,
            SaladRecipeCode),
        new(
            "After fixing the ingredients push, which ingredient list is printed for Ola?",
            400,
            "Ola has eatsMeat set to true, so chicken is appended to the four original ingredients before the ingredient loop runs.",
            ["lettuce, corn, carrots, cucumber, chicken", "lettuce, corn, carrots, cucumber", "chicken only", "The program still throws an error"],
            0,
            SaladRecipeCode),
        new(
            "After fixing the ingredients push, which extra step is printed for Ola?",
            400,
            "The same if block appends a final step for people who eat meat. That step is added after the four original preparation steps.",
            ["Mix in the chicken", "Cook the chicken for ten minutes", "Add chicken to every salad", "No extra step is printed"],
            0,
            SaladRecipeCode),
        new(
            "After the fix, why does Steve's recipe not include chicken even though Ola's did?",
            400,
            "printSaladRecipe creates a new saladRecipe object each time it is called. Steve also has eatsMeat set to false, so the meat-specific block does not run for his separate recipe.",
            ["A new recipe object is created and Steve does not eat meat", "Chicken is removed after Ola's recipe", "The ingredients array is shared but filtered for Steve", "Steve's recipe throws a TypeError"],
            0,
            SaladRecipeCode),
    ];

    private static readonly SeedQuestion[] FavouriteFoodsQuestions =
    [
        new(
            "What happens when the favourite foods program is run before the TODO is completed?",
            500,
            "favouriteFoods is still an empty array. Its length is 0 and none of the three food strings are included, so all four console.assert calls fail. Assertions report failures but do not throw an exception by default.",
            ["All four assertions fail", "All assertions pass", "It throws a TypeError", "Only the length assertion fails"],
            0,
            FavouriteFoodsCode),
        new(
            "Which loop fills favouriteFoods without typing any food strings?",
            500,
            "Each object in people already stores its food under favourite_food. The loop reads that property and pushes the resulting value into the target array.",
            ["for (const person of people) { favouriteFoods.push(person.favourite_food); }", "for (const person of people) { favouriteFoods.push(\"salad\"); }", "for (const person of people) { people.push(person.favourite_food); }", "favouriteFoods.push(people.favourite_food);"],
            0,
            FavouriteFoodsCode),
        new(
            "Why does person.favourite_food satisfy the rule about not typing food strings?",
            500,
            "The strings already exist in the input objects. Accessing person.favourite_food copies each stored value instead of hard-coding salad, mango, or macarons in the solution.",
            ["It reads existing values from the person objects", "It creates random food names", "It changes each person's favourite_food", "It removes the strings from people"],
            0,
            FavouriteFoodsCode),
        new(
            "After the correct loop runs, what is favouriteFoods?",
            500,
            "The loop visits the three people in array order and pushes their favourite_food values. The resulting array has exactly the three strings required by the assertions.",
            ["[\"salad\", \"mango\", \"macarons\"]", "[\"Saqib\", \"Shadi\", \"Navid\"]", "[]", "[\"favourite_food\"]"],
            0,
            FavouriteFoodsCode),
        new(
            "Which alternative also fills the existing favouriteFoods array correctly?",
            500,
            "map creates an array of the favourite_food values. The spread operator passes those values as individual arguments to push, which appends all of them to the existing array.",
            ["favouriteFoods.push(...people.map((person) => person.favourite_food));", "favouriteFoods = people.map((person) => person.favourite_food);", "people.map((person) => favouriteFoods);", "favouriteFoods.push(people.map);"],
            0,
            FavouriteFoodsCode),
    ];

    private static readonly SeedQuestion[] DynamicFieldQuestions =
    [
        new(
            "What happens when the dynamic field program is run?",
            600,
            "object.field reads a property literally named field. The person object has no such property, so the expression is undefined === \"vanilla\", which is false and causes the assertion to fail.",
            ["The assertion fails", "The assertion passes", "It throws a TypeError", "It logs vanilla"],
            0,
            DynamicFieldCode),
        new(
            "Why does object.field not read favourite_ice_cream?",
            600,
            "Dot notation uses the identifier written after the dot as a literal property name. Here that identifier is field, not the string value stored in the field parameter.",
            ["Dot notation treats field as the literal property name \"field\"", "field is not a string", "favourite_ice_cream is private", "object has no properties"],
            0,
            DynamicFieldCode),
        new(
            "Which expression correctly reads the property named by the field parameter?",
            600,
            "Bracket notation evaluates the expression inside the brackets. Since field contains \"favourite_ice_cream\", object[field] reads person.favourite_ice_cream.",
            ["object[field]", "object.field", "object.(field)", "field.object"],
            0,
            DynamicFieldCode),
        new(
            "After replacing object.field with object[field], what happens?",
            600,
            "object[field] evaluates to \"vanilla\", which equals the target value. The assertion passes, so console.assert produces no output.",
            ["The assertion passes with no output", "The assertion fails because vanilla is a string", "It logs favourite_ice_cream", "It throws a SyntaxError"],
            0,
            DynamicFieldCode),
    ];

    private static readonly SeedQuestion[] AllObjectQuestions = [.. ObjectQuestions, .. ObjectSyntaxQuestions, .. CyfLocationsQuestions, .. SaladRecipeQuestions, .. FavouriteFoodsQuestions, .. DynamicFieldQuestions];

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
                QuestionsPerGame = AllObjectQuestions.Length,
                Status = QuizStatus.Published,
            };
            dbContext.Quizes.Add(quiz);
        }
        else
        {
            quiz.QuestionsPerGame = AllObjectQuestions.Length;
            quiz.Status = QuizStatus.Published;
        }

        var existingQuestions = await dbContext.Questions
            .Where(question => question.ThemeId == theme.Id)
            .ToListAsync(cancellationToken);
        var existingQuestionsByText = existingQuestions.ToDictionary(question => question.Text);

        foreach (var seedQuestion in AllObjectQuestions)
        {
            if (existingQuestionsByText.TryGetValue(seedQuestion.Text, out var existingQuestion))
            {
                existingQuestion.CodeContext = seedQuestion.CodeContext ?? ObjectsCode;
                existingQuestion.Explanation = seedQuestion.Explanation;
                existingQuestion.Difficulty = seedQuestion.Difficulty;
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
                CodeContext = seedQuestion.CodeContext ?? ObjectsCode,
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
        int Difficulty,
        string Explanation,
        string[] Options,
        int CorrectOptionIndex,
        string? CodeContext = null);
}

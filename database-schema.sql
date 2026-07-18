-- QuizIt PostgreSQL schema
--
-- This file documents the schema created by the EF Core migrations in
-- Backend/Migrations. Application code generates UUID values.

CREATE TABLE "QuizThemes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    CONSTRAINT "PK_QuizThemes" PRIMARY KEY ("Id")
);

CREATE TABLE "Questions" (
    "Id" uuid NOT NULL,
    "ThemeId" uuid NOT NULL,
    "Text" text NOT NULL,
    "CodeContext" text NULL,
    "Explanation" text NULL,
    "Difficulty" integer NOT NULL,
    "CorrectOptionId" uuid NOT NULL,
    CONSTRAINT "PK_Questions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Questions_QuizThemes_ThemeId"
        FOREIGN KEY ("ThemeId") REFERENCES "QuizThemes" ("Id")
        ON DELETE RESTRICT
);

CREATE INDEX "IX_Questions_ThemeId" ON "Questions" ("ThemeId");

COMMENT ON COLUMN "Questions"."Difficulty" IS
    'Difficulty is an application-validated integer from 0 to 1000 in steps of 100.';
COMMENT ON COLUMN "Questions"."CorrectOptionId" IS
    'Correct AnswerOptions identifier. No database FK exists because options are inserted after the question.';

CREATE TABLE "AnswerOptions" (
    "Id" uuid NOT NULL,
    "QuestionId" uuid NOT NULL,
    "Text" text NOT NULL,
    CONSTRAINT "PK_AnswerOptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AnswerOptions_Questions_QuestionId"
        FOREIGN KEY ("QuestionId") REFERENCES "Questions" ("Id")
        ON DELETE CASCADE
);

CREATE INDEX "IX_AnswerOptions_QuestionId" ON "AnswerOptions" ("QuestionId");

COMMENT ON TABLE "AnswerOptions" IS
    'Each question must have exactly four options; the cardinality is validated by application code.';

CREATE TABLE "Quizes" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "ThemeId" uuid NOT NULL,
    "QuestionsPerGame" integer NOT NULL,
    CONSTRAINT "PK_Quizes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Quizes_QuizThemes_ThemeId"
        FOREIGN KEY ("ThemeId") REFERENCES "QuizThemes" ("Id")
        ON DELETE RESTRICT
);

CREATE INDEX "IX_Quizes_ThemeId" ON "Quizes" ("ThemeId");

CREATE TABLE "GameSessions" (
    "Id" uuid NOT NULL,
    "GameRoomId" text NOT NULL,
    "QuizId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_GameSessions" PRIMARY KEY ("Id")
);

COMMENT ON COLUMN "GameSessions"."Status" IS
    '0 = InProgress, 1 = Completed, 2 = Cancelled.';
COMMENT ON COLUMN "GameSessions"."QuizId" IS
    'Quiz snapshot source. No database FK currently exists.';

CREATE TABLE "GameSessionPlayers" (
    "Id" uuid NOT NULL,
    "GameSessionId" uuid NOT NULL,
    "PlayerId" text NOT NULL,
    "Name" text NOT NULL,
    "Score" integer NOT NULL,
    CONSTRAINT "PK_GameSessionPlayers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GameSessionPlayers_GameSessions_GameSessionId"
        FOREIGN KEY ("GameSessionId") REFERENCES "GameSessions" ("Id")
        ON DELETE CASCADE
);

CREATE INDEX "IX_GameSessionPlayers_GameSessionId"
    ON "GameSessionPlayers" ("GameSessionId");

CREATE TABLE "GameSessionQuestions" (
    "Id" uuid NOT NULL,
    "GameSessionId" uuid NOT NULL,
    "QuestionId" uuid NOT NULL,
    "Order" integer NOT NULL,
    CONSTRAINT "PK_GameSessionQuestions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GameSessionQuestions_GameSessions_GameSessionId"
        FOREIGN KEY ("GameSessionId") REFERENCES "GameSessions" ("Id")
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_GameSessionQuestions_GameSessionId_Order"
    ON "GameSessionQuestions" ("GameSessionId", "Order");

COMMENT ON COLUMN "GameSessionQuestions"."QuestionId" IS
    'Selected question snapshot reference. No database FK currently exists.';

CREATE TABLE "GameSessionAnswers" (
    "Id" uuid NOT NULL,
    "GameSessionId" uuid NOT NULL,
    "PlayerId" text NOT NULL,
    "QuestionId" uuid NOT NULL,
    "AnswerOptionId" uuid NOT NULL,
    "SubmittedAt" timestamp with time zone NOT NULL,
    "IsCorrect" boolean NOT NULL,
    "ScoreAwarded" integer NOT NULL,
    CONSTRAINT "PK_GameSessionAnswers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GameSessionAnswers_GameSessions_GameSessionId"
        FOREIGN KEY ("GameSessionId") REFERENCES "GameSessions" ("Id")
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_GameSessionAnswers_GameSessionId_PlayerId_QuestionId"
    ON "GameSessionAnswers" ("GameSessionId", "PlayerId", "QuestionId");

COMMENT ON COLUMN "GameSessionAnswers"."QuestionId" IS
    'Question identifier recorded at answer time. No database FK currently exists.';
COMMENT ON COLUMN "GameSessionAnswers"."AnswerOptionId" IS
    'Option identifier recorded at answer time. No database FK currently exists.';

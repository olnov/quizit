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
    "Status" integer NOT NULL DEFAULT 1,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "DeletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_Quizes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Quizes_QuizThemes_ThemeId"
        FOREIGN KEY ("ThemeId") REFERENCES "QuizThemes" ("Id")
        ON DELETE RESTRICT
);

CREATE INDEX "IX_Quizes_ThemeId" ON "Quizes" ("ThemeId");

COMMENT ON COLUMN "Quizes"."Status" IS
    '0 = Draft, 1 = Published, 2 = Archived.';
COMMENT ON COLUMN "Quizes"."IsDeleted" IS
    'Soft-deleted quizzes are not returned by public or authoring lists.';

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

-- ASP.NET Core Identity. These tables contain administrator and quiz-author
-- accounts. Roles are assigned through AspNetUserRoles.

CREATE TABLE "AspNetRoles" (
    "Id" text NOT NULL,
    "Name" character varying(256) NULL,
    "NormalizedName" character varying(256) NULL,
    "ConcurrencyStamp" text NULL,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE TABLE "AspNetUsers" (
    "Id" text NOT NULL,
    "DisplayName" text NULL,
    "UserName" character varying(256) NULL,
    "NormalizedUserName" character varying(256) NULL,
    "Email" character varying(256) NULL,
    "NormalizedEmail" character varying(256) NULL,
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text NULL,
    "SecurityStamp" text NULL,
    "ConcurrencyStamp" text NULL,
    "PhoneNumber" text NULL,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone NULL,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE TABLE "AspNetRoleClaims" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    "RoleId" text NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
        FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE TABLE "AspNetUserClaims" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    "UserId" text NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text NULL,
    "UserId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE TABLE "AspNetUserRoles" (
    "UserId" text NOT NULL,
    "RoleId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
        FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE TABLE "AspNetUserTokens" (
    "UserId" text NOT NULL,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- OpenIddict persists first-party OAuth/OIDC applications, grants, scopes,
-- and refresh-token state. Access tokens themselves are standard JWTs.

CREATE TABLE "OpenIddictApplications" (
    "Id" text NOT NULL,
    "ApplicationType" character varying(50) NULL,
    "ClientId" character varying(100) NULL,
    "ClientSecret" text NULL,
    "ClientType" character varying(50) NULL,
    "ConcurrencyToken" character varying(50) NULL,
    "ConsentType" character varying(50) NULL,
    "DisplayName" text NULL,
    "DisplayNames" text NULL,
    "JsonWebKeySet" text NULL,
    "Permissions" text NULL,
    "PostLogoutRedirectUris" text NULL,
    "Properties" text NULL,
    "RedirectUris" text NULL,
    "Requirements" text NULL,
    "Settings" text NULL,
    CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_OpenIddictApplications_ClientId"
    ON "OpenIddictApplications" ("ClientId");

CREATE TABLE "OpenIddictScopes" (
    "Id" text NOT NULL,
    "ConcurrencyToken" character varying(50) NULL,
    "Description" text NULL,
    "Descriptions" text NULL,
    "DisplayName" text NULL,
    "DisplayNames" text NULL,
    "Name" character varying(200) NULL,
    "Properties" text NULL,
    "Resources" text NULL,
    CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_OpenIddictScopes_Name" ON "OpenIddictScopes" ("Name");

CREATE TABLE "OpenIddictAuthorizations" (
    "Id" text NOT NULL,
    "ApplicationId" text NULL,
    "ConcurrencyToken" character varying(50) NULL,
    "CreationDate" timestamp with time zone NULL,
    "Properties" text NULL,
    "Scopes" text NULL,
    "Status" character varying(50) NULL,
    "Subject" character varying(400) NULL,
    "Type" character varying(50) NULL,
    CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId"
        FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id")
);

CREATE INDEX "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type"
    ON "OpenIddictAuthorizations" ("ApplicationId", "Status", "Subject", "Type");

CREATE TABLE "OpenIddictTokens" (
    "Id" text NOT NULL,
    "ApplicationId" text NULL,
    "AuthorizationId" text NULL,
    "ConcurrencyToken" character varying(50) NULL,
    "CreationDate" timestamp with time zone NULL,
    "ExpirationDate" timestamp with time zone NULL,
    "Payload" text NULL,
    "Properties" text NULL,
    "RedemptionDate" timestamp with time zone NULL,
    "ReferenceId" character varying(100) NULL,
    "Status" character varying(50) NULL,
    "Subject" character varying(400) NULL,
    "Type" character varying(150) NULL,
    CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId"
        FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id"),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId"
        FOREIGN KEY ("AuthorizationId") REFERENCES "OpenIddictAuthorizations" ("Id")
);

CREATE INDEX "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type"
    ON "OpenIddictTokens" ("ApplicationId", "Status", "Subject", "Type");
CREATE INDEX "IX_OpenIddictTokens_AuthorizationId"
    ON "OpenIddictTokens" ("AuthorizationId");
CREATE UNIQUE INDEX "IX_OpenIddictTokens_ReferenceId"
    ON "OpenIddictTokens" ("ReferenceId");

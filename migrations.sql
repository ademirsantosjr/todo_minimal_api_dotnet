﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Todos" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "UserId" integer NOT NULL,
    CONSTRAINT "PK_Todos" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "PasswordHash" text NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241221133536_InitialCreate', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE "Users" ADD "Role" text NOT NULL DEFAULT 'PENDING';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241225191959_AddUserRole', '8.0.11');

COMMIT;

START TRANSACTION;

ALTER TABLE "Users" DROP COLUMN "Role";

ALTER TABLE "Users" ADD "RoleId" integer NOT NULL DEFAULT 0;

CREATE TABLE "Roles" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

INSERT INTO "Roles" ("Id", "Name")
VALUES (1, 'ADMIN');
INSERT INTO "Roles" ("Id", "Name")
VALUES (2, 'PENDING');
INSERT INTO "Roles" ("Id", "Name")
VALUES (3, 'USER');

CREATE INDEX "IX_Users_RoleId" ON "Users" ("RoleId");

ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE;

SELECT setval(
    pg_get_serial_sequence('"Roles"', 'Id'),
    GREATEST(
        (SELECT MAX("Id") FROM "Roles") + 1,
        nextval(pg_get_serial_sequence('"Roles"', 'Id'))),
    false);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241225235218_CreateRolesTable', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE INDEX "IX_Todos_UserId" ON "Todos" ("UserId");

ALTER TABLE "Todos" ADD CONSTRAINT "FK_Todos_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241226001942_CreateUserTodosRelationship', '8.0.11');

COMMIT;


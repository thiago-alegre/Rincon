-- Rincon - puesta en cero para inicio real de operacion
-- Motor: PostgreSQL
--
-- IMPORTANTE:
-- 1. Este script borra datos operativos y usuarios existentes.
-- 2. No borra la tabla "__EFMigrationsHistory", por lo que mantiene el historial de migraciones EF Core.
-- 3. Antes de ejecutarlo, confirmar que existe un backup reciente y probado.
-- 4. Luego de ejecutarlo, ingresar con el usuario inicial y cambiar la contrasena.
--
-- Usuario inicial:
--   Email: admin@rinconweb.online
--   Password inicial: Cambiar123Aa!
--
-- El usuario inicial queda con rol Admin + Dios.
-- Admin permite acceder a las rutas existentes.
-- Dios permite ocultarlo/protegerlo desde la pantalla normal de usuarios.

BEGIN;

TRUNCATE TABLE
    "SaleExchangeBatches",
    "SaleExchanges",
    "SaleReturnDetailBatches",
    "SaleReturnDetails",
    "SaleReturns",
    "SaleDetailBatches",
    "SaleDetails",
    "Sales",
    "PersonalAccountPayments",
    "PersonalAccounts",
    "CashRegisterSessions",
    "ArticleBatches",
    "Articles",
    "Categories",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserRoles",
    "AspNetUserTokens",
    "AspNetUsers",
    "AspNetRoleClaims",
    "AspNetRoles"
RESTART IDENTITY CASCADE;

INSERT INTO "AspNetRoles"
    ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
    ('role-admin-rincon', 'Admin', 'ADMIN', 'stamp-role-admin-rincon'),
    ('role-employee-rincon', 'Employee', 'EMPLOYEE', 'stamp-role-employee-rincon'),
    ('role-dios-rincon', 'Dios', 'DIOS', 'stamp-role-dios-rincon');

INSERT INTO "AspNetUsers"
    (
        "Id",
        "FullName",
        "DNI",
        "Address",
        "Date",
        "IsActive",
        "UserName",
        "NormalizedUserName",
        "Email",
        "NormalizedEmail",
        "EmailConfirmed",
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "PhoneNumber",
        "PhoneNumberConfirmed",
        "TwoFactorEnabled",
        "LockoutEnd",
        "LockoutEnabled",
        "AccessFailedCount"
    )
VALUES
    (
        'user-dios-rincon',
        'Administrador Rincon',
        '00000000',
        NULL,
        NOW()::timestamp without time zone,
        TRUE,
        'admin@rinconweb.online',
        'ADMIN@RINCONWEB.ONLINE',
        'admin@rinconweb.online',
        'ADMIN@RINCONWEB.ONLINE',
        TRUE,
        'AQAAAAIAAYagAAAAEHqMg/zaNJzg9vx8abCAz1JidQZQtDXgCFgKxq/3LGFq3PKEsoFBir1thj3ROcYQ0Q==',
        'stamp-user-dios-rincon-security',
        'stamp-user-dios-rincon-concurrency',
        NULL,
        FALSE,
        FALSE,
        NULL,
        TRUE,
        0
    );

INSERT INTO "AspNetUserRoles"
    ("UserId", "RoleId")
VALUES
    ('user-dios-rincon', 'role-admin-rincon'),
    ('user-dios-rincon', 'role-dios-rincon');

COMMIT;

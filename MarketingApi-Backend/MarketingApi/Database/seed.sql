-- Seed data for Salesman & Marketing Team Management
-- Run after applying EF Core migrations (`dotnet ef database update`).
-- After inserting users with placeholder passwords, run `dotnet run -- --hash-passwords`
-- to hash them using ASP.NET Core Identity PasswordHasher<User>.

INSERT INTO roles (rolename)
VALUES
    ('Admin'),
    ('Supervisor Master'),
    ('Marketing Master')
ON CONFLICT (rolename) DO NOTHING;

-- Example initial accounts (change passwords immediately after running --hash-passwords):
INSERT INTO users (username, passwordhash, name, roleid)
SELECT 'supervisor', 'ChangeMe!123', 'Supervisor User', r.id
FROM roles r
WHERE r.rolename = 'Supervisor Master'
ON CONFLICT (username) DO NOTHING;

INSERT INTO users (username, passwordhash, name, roleid)
SELECT 'marketing', 'ChangeMe!123', 'Marketing Master User', r.id
FROM roles r
WHERE r.rolename = 'Marketing Master'
ON CONFLICT (username) DO NOTHING;

/* =========================================================
   BAUMANAGEMENT - TESTDATEN
   CompanyId:
   7769CBA9-F8D0-4851-A5A3-C37B13DAD859
   ========================================================= */

SET NOCOUNT ON;

DECLARE @CompanyId UNIQUEIDENTIFIER =
    '7769CBA9-F8D0-4851-A5A3-C37B13DAD859';


/* =========================================================
   1. CHECK COMPANY
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM Companies
    WHERE Id = @CompanyId
)
BEGIN
    PRINT 'FEHLER: Company wurde nicht gefunden.';
    RETURN;
END;


/* =========================================================
   2. COMPANY AUF XL SETZEN
   ========================================================= */

UPDATE Companies
SET SubscriptionPlan = 3
WHERE Id = @CompanyId;


/* =========================================================
   3. MITARBEITER
   ========================================================= */

DECLARE @Employee1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee3 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee4 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee5 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee6 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee7 UNIQUEIDENTIFIER = NEWID();
DECLARE @Employee8 UNIQUEIDENTIFIER = NEWID();


INSERT INTO Employees
(
    Id,
    CompanyId,
    FirstName,
    LastName,
    Phone,
    Email,
    ApplicationUserId,
    Position,
    IsActive,
    CreatedAt
)
VALUES

(
    @Employee1,
    @CompanyId,
    'Marko',
    'Keller',
    '+49 171 2345678',
    'marko.keller@bau-demo.de',
    NULL,
    'Bauleiter',
    1,
    DATEADD(DAY, -45, GETUTCDATE())
),

(
    @Employee2,
    @CompanyId,
    'Stefan',
    'Müller',
    '+49 172 3456789',
    'stefan.mueller@bau-demo.de',
    NULL,
    'Maurer',
    1,
    DATEADD(DAY, -42, GETUTCDATE())
),

(
    @Employee3,
    @CompanyId,
    'Daniel',
    'Adler',
    '+49 173 4567890',
    'daniel.adler@bau-demo.de',
    NULL,
    'Elektriker',
    1,
    DATEADD(DAY, -40, GETUTCDATE())
),

(
    @Employee4,
    @CompanyId,
    'Thomas',
    'Weber',
    '+49 174 5678901',
    'thomas.weber@bau-demo.de',
    NULL,
    'Trockenbauer',
    1,
    DATEADD(DAY, -38, GETUTCDATE())
),

(
    @Employee5,
    @CompanyId,
    'Michael',
    'Schmidt',
    '+49 175 6789012',
    'michael.schmidt@bau-demo.de',
    NULL,
    'Maler',
    1,
    DATEADD(DAY, -35, GETUTCDATE())
),

(
    @Employee6,
    @CompanyId,
    'Andreas',
    'Fischer',
    '+49 176 7890123',
    'andreas.fischer@bau-demo.de',
    NULL,
    'Installateur',
    1,
    DATEADD(DAY, -32, GETUTCDATE())
),

(
    @Employee7,
    @CompanyId,
    'Martin',
    'Wagner',
    '+49 177 8901234',
    'martin.wagner@bau-demo.de',
    NULL,
    'Fliesenleger',
    1,
    DATEADD(DAY, -28, GETUTCDATE())
),

(
    @Employee8,
    @CompanyId,
    'Jan',
    'Hoffmann',
    '+49 178 9012345',
    'jan.hoffmann@bau-demo.de',
    NULL,
    'Zimmermann',
    1,
    DATEADD(DAY, -25, GETUTCDATE())
);


/* =========================================================
   4. BAUSTELLEN
   ========================================================= */

DECLARE @Baustelle1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Baustelle2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Baustelle3 UNIQUEIDENTIFIER = NEWID();
DECLARE @Baustelle4 UNIQUEIDENTIFIER = NEWID();
DECLARE @Baustelle5 UNIQUEIDENTIFIER = NEWID();
DECLARE @Baustelle6 UNIQUEIDENTIFIER = NEWID();


INSERT INTO Baustellen
(
    Id,
    CompanyId,
    Name,
    Address,
    PostalCode,
    City,
    CustomerName,
    CustomerPhone,
    StartDate,
    EndDate,
    IsActive,
    Description,
    CreatedAt
)
VALUES

(
    @Baustelle1,
    @CompanyId,
    'Neubau Einfamilienhaus Müller',
    'Hauptstraße 15',
    '85221',
    'Dachau',
    'Familie Müller',
    '+49 170 1111111',
    DATEADD(DAY, -10, GETUTCDATE()),
    DATEADD(DAY, 45, GETUTCDATE()),
    1,
    'Kompletter Neubau eines Einfamilienhauses.',
    DATEADD(DAY, -20, GETUTCDATE())
),

(
    @Baustelle2,
    @CompanyId,
    'Sanierung Weber',
    'Bahnhofstraße 22',
    '80331',
    'München',
    'Thomas Weber',
    '+49 170 2222222',
    DATEADD(DAY, -5, GETUTCDATE()),
    DATEADD(DAY, 25, GETUTCDATE()),
    1,
    'Sanierung eines Altbaus inklusive Elektro und Sanitär.',
    DATEADD(DAY, -15, GETUTCDATE())
),

(
    @Baustelle3,
    @CompanyId,
    'Bürogebäude Zentrum',
    'Maximilianstraße 45',
    '80539',
    'München',
    'Bürozentrum GmbH',
    '+49 170 3333333',
    DATEADD(DAY, 2, GETUTCDATE()),
    DATEADD(DAY, 70, GETUTCDATE()),
    1,
    'Innenausbau und Modernisierung eines Bürogebäudes.',
    DATEADD(DAY, -12, GETUTCDATE())
),

(
    @Baustelle4,
    @CompanyId,
    'Dachsanierung Schmidt',
    'Gartenstraße 8',
    '86150',
    'Augsburg',
    'Peter Schmidt',
    '+49 170 4444444',
    DATEADD(DAY, -20, GETUTCDATE()),
    DATEADD(DAY, 10, GETUTCDATE()),
    1,
    'Komplette Dachsanierung.',
    DATEADD(DAY, -30, GETUTCDATE())
),

(
    @Baustelle5,
    @CompanyId,
    'Wohnanlage Sonnenhof',
    'Sonnenstraße 12',
    '84028',
    'Landshut',
    'Sonnenhof Immobilien GmbH',
    '+49 170 5555555',
    DATEADD(DAY, 7, GETUTCDATE()),
    DATEADD(DAY, 100, GETUTCDATE()),
    1,
    'Sanierung und Modernisierung einer Wohnanlage.',
    DATEADD(DAY, -8, GETUTCDATE())
),

(
    @Baustelle6,
    @CompanyId,
    'Lagerhalle Gewerbepark',
    'Industriestraße 31',
    '84030',
    'Landshut',
    'Gewerbepark Süd GmbH',
    '+49 170 6666666',
    DATEADD(DAY, -3, GETUTCDATE()),
    DATEADD(DAY, 35, GETUTCDATE()),
    1,
    'Umbau und Ausbau einer Lagerhalle.',
    DATEADD(DAY, -10, GETUTCDATE())
);


/* =========================================================
   5. WORK TASKS
   TaskStatus:
   0 = Open
   1 = InProgress
   2 = Completed
   3 = Cancelled
   ========================================================= */

INSERT INTO WorkTasks
(
    Id,
    CompanyId,
    BaustelleId,
    EmployeeId,
    Title,
    Description,
    Status,
    DueDate,
    CreatedAt
)
VALUES

(
    NEWID(),
    @CompanyId,
    @Baustelle1,
    @Employee2,
    'Mauerwerk Erdgeschoss',
    'Außen- und Innenwände im Erdgeschoss fertigstellen.',
    1,
    DATEADD(DAY, 2, GETUTCDATE()),
    DATEADD(DAY, -3, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle1,
    @Employee3,
    'Elektroinstallation vorbereiten',
    'Kabelwege und Unterputzdosen vorbereiten.',
    0,
    DATEADD(DAY, 5, GETUTCDATE()),
    DATEADD(DAY, -2, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle2,
    @Employee4,
    'Trockenbau Wohnzimmer',
    'Decken und Wände im Wohnzimmer verkleiden.',
    1,
    DATEADD(DAY, 3, GETUTCDATE()),
    DATEADD(DAY, -5, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle2,
    @Employee6,
    'Sanitärinstallation',
    'Neue Wasserleitungen im Badezimmer installieren.',
    0,
    DATEADD(DAY, 7, GETUTCDATE()),
    DATEADD(DAY, -4, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle3,
    @Employee1,
    'Baustellenbesprechung',
    'Besprechung mit Bauherr und allen Gewerken.',
    2,
    DATEADD(DAY, -1, GETUTCDATE()),
    DATEADD(DAY, -8, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle3,
    @Employee5,
    'Wände streichen',
    'Erster Anstrich der Büroräume.',
    0,
    DATEADD(DAY, 10, GETUTCDATE()),
    DATEADD(DAY, -1, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle4,
    @Employee8,
    'Dachkonstruktion prüfen',
    'Dachstuhl kontrollieren und beschädigte Teile ersetzen.',
    1,
    DATEADD(DAY, 1, GETUTCDATE()),
    DATEADD(DAY, -6, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle5,
    @Employee7,
    'Fliesenarbeiten Badezimmer',
    'Boden- und Wandfliesen im Badezimmer verlegen.',
    0,
    DATEADD(DAY, 15, GETUTCDATE()),
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Baustelle6,
    @Employee2,
    'Boden vorbereiten',
    'Industrieboden reinigen und für Beschichtung vorbereiten.',
    2,
    DATEADD(DAY, -2, GETUTCDATE()),
    DATEADD(DAY, -12, GETUTCDATE())
),

(
    NEWID(),
    @CompanyId,
    @Baustelle6,
    @Employee5,
    'Wandbeschichtung',
    'Wände der Lagerhalle beschichten.',
    0,
    DATEADD(DAY, 8, GETUTCDATE()),
    DATEADD(DAY, -1, GETUTCDATE())
);


/* =========================================================
   6. WORK ASSIGNMENTS
   ========================================================= */

INSERT INTO WorkAssignments
(
    Id,
    CompanyId,
    EmployeeId,
    BaustelleId,
    Start,
    [End],
    Notes,
    CreatedAt
)
VALUES

(
    NEWID(),
    @CompanyId,
    @Employee1,
    @Baustelle1,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 16, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Bauleitung und Kontrolle',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee2,
    @Baustelle1,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 16, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Mauerarbeiten',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee3,
    @Baustelle2,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 14, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Elektroarbeiten',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee4,
    @Baustelle2,
    DATEADD(HOUR, 9, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 17, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Trockenbau',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee5,
    @Baustelle3,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 16, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Malerarbeiten',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee6,
    @Baustelle2,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 15, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Sanitär',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee7,
    @Baustelle5,
    DATEADD(HOUR, 7, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 15, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Fliesenarbeiten',
    GETUTCDATE()
),

(
    NEWID(),
    @CompanyId,
    @Employee8,
    @Baustelle4,
    DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    DATEADD(HOUR, 16, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)),
    'Dacharbeiten',
    GETUTCDATE()
);


/* =========================================================
   7. RESULT
   ========================================================= */

SELECT
    'Company' AS Entity,
    COUNT(*) AS Anzahl
FROM Companies
WHERE Id = @CompanyId

UNION ALL

SELECT
    'Employees',
    COUNT(*)
FROM Employees
WHERE CompanyId = @CompanyId

UNION ALL

SELECT
    'Baustellen',
    COUNT(*)
FROM Baustellen
WHERE CompanyId = @CompanyId

UNION ALL

SELECT
    'WorkTasks',
    COUNT(*)
FROM WorkTasks
WHERE CompanyId = @CompanyId

UNION ALL

SELECT
    'WorkAssignments',
    COUNT(*)
FROM WorkAssignments
WHERE CompanyId = @CompanyId;


/* =========================================================
   8. TESTDATEN ANZEIGEN
   ========================================================= */

SELECT
    e.FirstName,
    e.LastName,
    e.Position,
    e.Email,
    e.IsActive
FROM Employees e
WHERE e.CompanyId = @CompanyId
ORDER BY e.LastName, e.FirstName;


SELECT
    b.Name,
    b.City,
    b.CustomerName,
    b.StartDate,
    b.EndDate,
    b.IsActive
FROM Baustellen b
WHERE b.CompanyId = @CompanyId
ORDER BY b.StartDate;


SELECT
    t.Title,
    t.Status,
    t.DueDate,
    e.FirstName + ' ' + e.LastName AS Mitarbeiter,
    b.Name AS Baustelle
FROM WorkTasks t
INNER JOIN Baustellen b
    ON b.Id = t.BaustelleId
LEFT JOIN Employees e
    ON e.Id = t.EmployeeId
WHERE t.CompanyId = @CompanyId
ORDER BY t.DueDate;
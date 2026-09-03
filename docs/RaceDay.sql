IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.EventEnrolments', 'U') IS NOT NULL DROP TABLE dbo.EventEnrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* TABLE: Users */
CREATE TABLE dbo.Users (
    UserId          INT             IDENTITY(1,1)   PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    Role            NVARCHAR(20)    NOT NULL
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* TABLE: Events */
CREATE TABLE dbo.Events (
    EventId         INT             IDENTITY(1,1)   PRIMARY KEY,
    OrganiserId     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(150)   NULL,
    Description     NVARCHAR(MAX)   NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users(UserId)
);
GO

/* TABLE: Routes */
CREATE TABLE dbo.Routes (
    RouteId         INT             IDENTITY(1,1)   PRIMARY KEY,
    EventId         INT             NOT NULL,
    RouteName       NVARCHAR(100)   NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    ElevationGainM  INT             NULL,
    MapUrl          NVARCHAR(255)   NULL,
    CONSTRAINT FK_Routes_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId) ON DELETE CASCADE
);
GO

/* TABLE: Categories */
CREATE TABLE dbo.Categories (
    CategoryId      INT             IDENTITY(1,1)   PRIMARY KEY,
    EventId         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    MaxParticipants INT             NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId) ON DELETE CASCADE
);
GO

/* TABLE: EventEnrolments */
CREATE TABLE dbo.EventEnrolments (
    EnrolmentId     INT             IDENTITY(1,1)   PRIMARY KEY,
    ParticipantId   INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    BibNumber       NVARCHAR(10)    NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

/* TABLE: Results */
CREATE TABLE dbo.Results (
    ResultId        INT             IDENTITY(1,1)   PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DQ')),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.EventEnrolments(EnrolmentId) ON DELETE CASCADE
);
GO

/* SEED DATA */
INSERT INTO dbo.Users (FullName, Email, PasswordHash, PhoneNumber, Role) VALUES
('Thandiwe Nkosi', 'thandiwe.nkosi@raceday.co.za', 'HASHED_PASSWORD_1', '0821234567', 'Organiser'),
('Johan van der Merwe', 'johan.vdm@raceday.co.za', 'HASHED_PASSWORD_2', '0837654321', 'Organiser');

INSERT INTO dbo.Users (FullName, Email, PasswordHash, PhoneNumber, Role) VALUES
('Lerato Dlamini', 'lerato.dlamini@gmail.com', 'HASHED_PASSWORD_3', '0721112222', 'Participant'),
('Sipho Khumalo', 'sipho.khumalo@gmail.com', 'HASHED_PASSWORD_4', '0733334444', 'Participant');

INSERT INTO dbo.Events (OrganiserId, EventName, EventDate, Location, Description) VALUES
(1, 'Johannesburg City Half Marathon', '2026-10-18', 'Johannesburg, Gauteng', 'A scenic half marathon through the streets of Johannesburg.'),
(1, 'Soweto Community Fun Walk', '2026-11-08', 'Soweto, Gauteng', 'A family-friendly community walk supporting local charities.'),
(2, 'Cape Winelands Cycle Challenge', '2026-09-27', 'Stellenbosch, Western Cape', 'A charity cycling event through the Cape Winelands.');

INSERT INTO dbo.Routes (EventId, RouteName, DistanceKm, ElevationGainM, MapUrl) VALUES
(1, 'JHB Half Marathon Route', 21.10, 180, 'https://maps.raceday.co.za/jhb-half'),
(2, 'Soweto Fun Walk Route', 5.00, 40, 'https://maps.raceday.co.za/soweto-walk'),
(3, 'Winelands Cycle Route', 94.70, 620, 'https://maps.raceday.co.za/winelands-cycle');

INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants) VALUES
(1, '21km Half Marathon', 21.10, 250.00, 2000),
(1, '10km Fun Run', 10.00, 150.00, 1500),
(2, '5km Fun Walk', 5.00, 50.00, 1000),
(2, '2km Kids Walk', 2.00, 0.00, 500),
(3, '94km Challenge Ride', 94.70, 450.00, 800),
(3, '45km Half Challenge Ride', 45.00, 300.00, 600);

INSERT INTO dbo.EventEnrolments (ParticipantId, CategoryId, BibNumber, Status) VALUES
(3, 1, 'A1001', 'Confirmed'),
(4, 2, 'A1002', 'Confirmed'),
(3, 5, 'C2001', 'Confirmed');

INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position, Status) VALUES
(1, '01:45:32', 112, 'Finished'),
(2, '00:52:10', 45, 'Finished');
GO
SELECT * FROM dbo.Users;
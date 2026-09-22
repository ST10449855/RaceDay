-- 1. Create Database if it does not exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RaceDayDb')
BEGIN
    EXEC('CREATE DATABASE RaceDayDb');
END
GO

USE RaceDayDb;
GO

-- 2. Drop existing tables for clean re-execution
DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS Enrolments;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;
GO

-- 3. Roles Entity
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(20) NOT NULL UNIQUE
);

-- 4. Users Entity
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- 5. Events Entity
CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    Title VARCHAR(150) NOT NULL,
    Description TEXT NULL,
    Location VARCHAR(150) NOT NULL,
    EventDate DATETIME NOT NULL,
    OrganiserId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId)
);

-- 6. Categories Entity
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    Name VARCHAR(50) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

-- 7. Enrolments Entity
CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status VARCHAR(20) DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Participant_Category UNIQUE (ParticipantId, CategoryId)
);

-- 8. Results Entity
CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTimeSeconds INT NOT NULL,
    Position INT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
);

-- =============================================
-- SEED DATA (Required by Assessment Rubric)
-- =============================================

-- Seed Roles
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');

-- Seed Users (2 Organisers, 2 Participants)
INSERT INTO Users (FullName, Email, PasswordHash, RoleId) VALUES
('Sibusiso Khumalo', 'sibu@raceevents.co.za', 'hashedpass1', 1),
('Anika Meyer', 'anika@capetownevents.co.za', 'hashedpass2', 1),
('David Naidoo', 'david.n@gmail.com', 'hashedpass3', 2),
('Lerato Mokoena', 'lerato.m@gmail.com', 'hashedpass4', 2);

-- Seed Events (3 Events)
INSERT INTO Events (Title, Description, Location, EventDate, OrganiserId) VALUES
('Soweto Half Marathon', 'Annual road running event through Soweto.', 'Soweto, Johannesburg', '2026-11-05 06:00:00', 1),
('Cape Town Coastal Challenge', 'Scenic coastal road cycling and running.', 'Cape Town, Western Cape', '2026-12-10 07:00:00', 2),
('Durban Beachfront Run', '5km and 10km fun run along the promenade.', 'Durban, KwaZulu-Natal', '2027-01-15 06:30:00', 1);

-- Seed Categories
INSERT INTO Categories (EventId, Name, DistanceKm, EntryFee) VALUES
(1, '21km Half Marathon', 21.10, 250.00),
(1, '10km Road Race', 10.00, 150.00),
(2, '100km Cycle Tour', 100.00, 500.00),
(2, '42km Marathon Run', 42.20, 350.00),
(3, '10km Beachfront Run', 10.00, 120.00),
(3, '5km Fun Walk', 5.00, 80.00);

-- Seed Enrolments
INSERT INTO Enrolments (ParticipantId, CategoryId) VALUES
(3, 1),
(3, 3),
(4, 2),
(4, 5);

-- Seed Results
INSERT INTO Results (EnrolmentId, FinishTimeSeconds, Position) VALUES
(1, 6300, 12),
(3, 2700, 5);
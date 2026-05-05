-- Database Schema for Societies Management System
-- SQL Server

CREATE DATABASE SocietiesManagementDB;
GO

USE SocietiesManagementDB;
GO

-- Users table
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL, -- Store hashed passwords
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Student', 'SocietyHead', 'Admin')),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- Societies table
CREATE TABLE Societies (
    SocietyID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    HeadID INT,
    Status NVARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Suspended', 'Deleted')),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (HeadID) REFERENCES Users(UserID)
);

-- Memberships table
CREATE TABLE Memberships (
    MembershipID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NOT NULL,
    SocietyID INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Rejected')),
    JoinDate DATETIME DEFAULT GETDATE(),
    ApprovedDate DATETIME NULL,
    FOREIGN KEY (StudentID) REFERENCES Users(UserID),
    FOREIGN KEY (SocietyID) REFERENCES Societies(SocietyID)
);

-- Events table
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    SocietyID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(200),
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Cancelled')),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SocietyID) REFERENCES Societies(SocietyID)
);

-- EventRegistrations table
CREATE TABLE EventRegistrations (
    RegistrationID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NOT NULL,
    EventID INT NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (StudentID) REFERENCES Users(UserID),
    FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

-- Tasks table
CREATE TABLE Tasks (
    TaskID INT IDENTITY(1,1) PRIMARY KEY,
    SocietyID INT NOT NULL,
    AssignedTo INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Status NVARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'InProgress', 'Completed')),
    DueDate DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SocietyID) REFERENCES Societies(SocietyID),
    FOREIGN KEY (AssignedTo) REFERENCES Users(UserID)
);

-- Indexes for performance
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Memberships_StudentID ON Memberships(StudentID);
CREATE INDEX IX_Memberships_SocietyID ON Memberships(SocietyID);
CREATE INDEX IX_Events_SocietyID ON Events(SocietyID);
CREATE INDEX IX_EventRegistrations_StudentID ON EventRegistrations(StudentID);
CREATE INDEX IX_EventRegistrations_EventID ON EventRegistrations(EventID);
CREATE INDEX IX_Tasks_SocietyID ON Tasks(SocietyID);
CREATE INDEX IX_Tasks_AssignedTo ON Tasks(AssignedTo);
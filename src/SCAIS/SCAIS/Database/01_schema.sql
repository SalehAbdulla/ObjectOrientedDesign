/* =====================================================================
   SCAIS - Smart Course Advising Information System
   01_schema.sql - database schema (Third Normal Form)

   Applied automatically by SCAIS.DataAccess.DatabaseManager.EnsureDatabase()
   the first time the application starts, and only when the schema is absent.

   Normalisation summary (see the project report for the full explanation):
     1NF - no repeating groups; a student's courses are rows in Enrollments,
           never a comma separated column.
     2NF - every non-key attribute depends on the whole primary key.
     3NF - no transitive dependencies. Students stores SpecializationId, not
           the specialization name; Enrollments stores foreign keys, never
           the student name or the course title.
   ===================================================================== */

/* ---------------------------------------------------------------
   Specializations - the four academic tracks
   --------------------------------------------------------------- */
CREATE TABLE dbo.Specializations
(
    SpecializationId INT            IDENTITY(1,1) NOT NULL,
    Name             NVARCHAR(50)   NOT NULL,
    Description      NVARCHAR(200)  NULL,
    CONSTRAINT PK_Specializations PRIMARY KEY (SpecializationId),
    CONSTRAINT UQ_Specializations_Name UNIQUE (Name)
);
GO

/* ---------------------------------------------------------------
   Users - base identity for every account (single login screen)
   --------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    UserId       INT            IDENTITY(1,1) NOT NULL,
    Username     NVARCHAR(50)   NOT NULL,
    PasswordHash NVARCHAR(128)  NOT NULL,
    FullName     NVARCHAR(100)  NOT NULL,
    Email        NVARCHAR(100)  NOT NULL,
    Role         NVARCHAR(20)   NOT NULL,
    IsActive     BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedDate  DATETIME       NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT (GETDATE()),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Administrator', 'Adviser', 'Student'))
);
GO

/* ---------------------------------------------------------------
   Advisers - role data for faculty members
   --------------------------------------------------------------- */
CREATE TABLE dbo.Advisers
(
    AdviserId  INT           IDENTITY(1,1) NOT NULL,
    UserId     INT           NOT NULL,
    Department NVARCHAR(50)  NOT NULL,
    Office     NVARCHAR(50)  NULL,
    CONSTRAINT PK_Advisers PRIMARY KEY (AdviserId),
    CONSTRAINT UQ_Advisers_UserId UNIQUE (UserId),
    CONSTRAINT FK_Advisers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

/* ---------------------------------------------------------------
   Students - role data for advisees
   --------------------------------------------------------------- */
CREATE TABLE dbo.Students
(
    StudentId        INT            IDENTITY(1,1) NOT NULL,
    UserId           INT            NOT NULL,
    SpecializationId INT            NOT NULL,
    AdviserId        INT            NULL,
    EnrollmentYear   INT            NOT NULL,
    GPA              DECIMAL(3, 2)  NULL,
    CONSTRAINT PK_Students PRIMARY KEY (StudentId),
    CONSTRAINT UQ_Students_UserId UNIQUE (UserId),
    CONSTRAINT FK_Students_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Students_Specializations FOREIGN KEY (SpecializationId)
        REFERENCES dbo.Specializations (SpecializationId),
    CONSTRAINT FK_Students_Advisers FOREIGN KEY (AdviserId) REFERENCES dbo.Advisers (AdviserId)
);
GO

/* ---------------------------------------------------------------
   Administrators - role data for system administrators
   --------------------------------------------------------------- */
CREATE TABLE dbo.Administrators
(
    AdminId     INT           IDENTITY(1,1) NOT NULL,
    UserId      INT           NOT NULL,
    AccessLevel NVARCHAR(20)  NOT NULL CONSTRAINT DF_Administrators_AccessLevel DEFAULT ('Full'),
    CONSTRAINT PK_Administrators PRIMARY KEY (AdminId),
    CONSTRAINT UQ_Administrators_UserId UNIQUE (UserId),
    CONSTRAINT FK_Administrators_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

/* ---------------------------------------------------------------
   Courses - the course catalogue. SpecializationId is NULL for a
   core subject, because a core subject belongs to no single track.
   --------------------------------------------------------------- */
CREATE TABLE dbo.Courses
(
    CourseId         INT            IDENTITY(1,1) NOT NULL,
    Code             NVARCHAR(10)   NOT NULL,
    Title            NVARCHAR(100)  NOT NULL,
    CreditHours      INT            NOT NULL,
    IsCore           BIT            NOT NULL CONSTRAINT DF_Courses_IsCore DEFAULT (0),
    SpecializationId INT            NULL,
    Description      NVARCHAR(255)  NULL,
    CONSTRAINT PK_Courses PRIMARY KEY (CourseId),
    CONSTRAINT UQ_Courses_Code UNIQUE (Code),
    CONSTRAINT CK_Courses_CreditHours CHECK (CreditHours BETWEEN 1 AND 6),
    CONSTRAINT FK_Courses_Specializations FOREIGN KEY (SpecializationId)
        REFERENCES dbo.Specializations (SpecializationId)
);

/* ---------------------------------------------------------------
   Prerequisites - dependency rules linking courses to courses.
   This is a many-to-many self relationship on Courses: CourseId is
   the dependent course, RequiredCourseId is the course that must be
   completed first (or taken concurrently, when RuleType is Corequisite).
   --------------------------------------------------------------- */
CREATE TABLE dbo.Prerequisites
(
    PrerequisiteId   INT           IDENTITY(1,1) NOT NULL,
    CourseId         INT           NOT NULL,
    RequiredCourseId INT           NOT NULL,
    RuleType         NVARCHAR(20)  NOT NULL CONSTRAINT DF_Prerequisites_RuleType DEFAULT ('Prerequisite'),
    CONSTRAINT PK_Prerequisites PRIMARY KEY (PrerequisiteId),
    CONSTRAINT UQ_Prerequisites UNIQUE (CourseId, RequiredCourseId, RuleType),
    CONSTRAINT CK_Prerequisites_RuleType CHECK (RuleType IN ('Prerequisite', 'Corequisite')),
    CONSTRAINT CK_Prerequisites_NotSelf CHECK (CourseId <> RequiredCourseId),
    CONSTRAINT FK_Prerequisites_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses (CourseId),
    CONSTRAINT FK_Prerequisites_RequiredCourses FOREIGN KEY (RequiredCourseId)
        REFERENCES dbo.Courses (CourseId)
);
GO

/* ---------------------------------------------------------------
   Semesters - the academic terms
   --------------------------------------------------------------- */
CREATE TABLE dbo.Semesters
(
    SemesterId   INT           IDENTITY(1,1) NOT NULL,
    TermName     NVARCHAR(20)  NOT NULL,
    AcademicYear INT           NOT NULL,
    StartDate    DATE          NULL,
    EndDate      DATE          NULL,
    CONSTRAINT PK_Semesters PRIMARY KEY (SemesterId),
    CONSTRAINT UQ_Semesters UNIQUE (TermName, AcademicYear)
);
GO

/* ---------------------------------------------------------------
   CourseOfferings - which course runs in which semester
   --------------------------------------------------------------- */
CREATE TABLE dbo.CourseOfferings
(
    OfferingId INT IDENTITY(1,1) NOT NULL,
    CourseId   INT NOT NULL,
    SemesterId INT NOT NULL,
    Capacity   INT NOT NULL CONSTRAINT DF_CourseOfferings_Capacity DEFAULT (30),
    CONSTRAINT PK_CourseOfferings PRIMARY KEY (OfferingId),
    CONSTRAINT UQ_CourseOfferings UNIQUE (CourseId, SemesterId),
    CONSTRAINT FK_CourseOfferings_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses (CourseId),
    CONSTRAINT FK_CourseOfferings_Semesters FOREIGN KEY (SemesterId) REFERENCES dbo.Semesters (SemesterId)
);
GO

/* ---------------------------------------------------------------
   Enrollments - the student's academic record, and the table that
   carries course status (Completed / InProgress / Pending).
   --------------------------------------------------------------- */
CREATE TABLE dbo.Enrollments
(
    EnrollmentId INT           IDENTITY(1,1) NOT NULL,
    StudentId    INT           NOT NULL,
    CourseId     INT           NOT NULL,
    SemesterId   INT           NOT NULL,
    Status       NVARCHAR(20)  NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT ('Pending'),
    Grade        NVARCHAR(3)   NULL,
    CONSTRAINT PK_Enrollments PRIMARY KEY (EnrollmentId),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN ('Completed', 'InProgress', 'Pending')),
    CONSTRAINT CK_Enrollments_Grade CHECK
    (
        (Status = 'Completed' AND Grade IS NOT NULL) OR (Status <> 'Completed')
    ),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students (StudentId),
    CONSTRAINT FK_Enrollments_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses (CourseId),
    CONSTRAINT FK_Enrollments_Semesters FOREIGN KEY (SemesterId) REFERENCES dbo.Semesters (SemesterId)
);
GO

/* ---------------------------------------------------------------
   CourseRequests - a course plan submitted by a student for review
   --------------------------------------------------------------- */
CREATE TABLE dbo.CourseRequests
(
    RequestId     INT            IDENTITY(1,1) NOT NULL,
    StudentId     INT            NOT NULL,
    AdviserId     INT            NULL,
    SemesterId    INT            NOT NULL,
    Status        NVARCHAR(20)   NOT NULL CONSTRAINT DF_CourseRequests_Status DEFAULT ('Draft'),
    SubmittedDate DATETIME       NULL,
    AdviserRemark NVARCHAR(255)  NULL,
    CONSTRAINT PK_CourseRequests PRIMARY KEY (RequestId),
    CONSTRAINT CK_CourseRequests_Status CHECK
    (
        Status IN ('Draft', 'Submitted', 'Approved', 'Rejected')
    ),
    CONSTRAINT FK_CourseRequests_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students (StudentId),
    CONSTRAINT FK_CourseRequests_Advisers FOREIGN KEY (AdviserId) REFERENCES dbo.Advisers (AdviserId),
    CONSTRAINT FK_CourseRequests_Semesters FOREIGN KEY (SemesterId) REFERENCES dbo.Semesters (SemesterId)
);
GO

/* ---------------------------------------------------------------
   CourseRequestLines - the courses inside a plan. Identifying child
   of CourseRequests: the lines have no lifetime without the request,
   which is why the foreign key cascades on delete.
   --------------------------------------------------------------- */
CREATE TABLE dbo.CourseRequestLines
(
    LineId          INT            IDENTITY(1,1) NOT NULL,
    RequestId       INT            NOT NULL,
    CourseId        INT            NOT NULL,
    EligibilityNote NVARCHAR(255)  NULL,
    CONSTRAINT PK_CourseRequestLines PRIMARY KEY (LineId),
    CONSTRAINT FK_CourseRequestLines_CourseRequests FOREIGN KEY (RequestId)
        REFERENCES dbo.CourseRequests (RequestId) ON DELETE CASCADE,
    CONSTRAINT FK_CourseRequestLines_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses (CourseId)
);
GO

/* ---------------------------------------------------------------
   Notifications - written by the Observer participants when a course
   request changes status, which makes the Observer pattern visible in
   the data as well as in the code.
   --------------------------------------------------------------- */
CREATE TABLE dbo.Notifications
(
    NotificationId INT            IDENTITY(1,1) NOT NULL,
    UserId         INT            NOT NULL,
    Message        NVARCHAR(255)  NOT NULL,
    CreatedDate    DATETIME       NOT NULL CONSTRAINT DF_Notifications_CreatedDate DEFAULT (GETDATE()),
    IsRead         BIT            NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO


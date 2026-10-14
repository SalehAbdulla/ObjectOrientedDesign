# 03 — Database Design Specification (SQL Server LocalDB)

Target: **SQL Server LocalDB**, database file `SCAIS.mdf` stored **inside the
Visual Studio project** (required by the brief). 12 tables, 3rd Normal Form,
**≥15 mock records in every table**.

---

## 1. Normalisation notes (quote these in the report)

| Normal form | How SCAIS satisfies it |
|---|---|
| **1NF** | No repeating groups and no multi-valued columns. A student's courses are *rows in `Enrollments`*, not a comma-separated `Courses` column. |
| **2NF** | Every non-key attribute depends on the **whole** primary key. Relationship tables (`Enrollments`, `Prerequisites`, `CourseRequestLines`) use a surrogate identity PK, and every attribute describes that whole entity. |
| **3NF** | No transitive dependencies. `Students` does **not** store `SpecializationName`; it stores `SpecializationId`, and the name lives once in `Specializations`. `Enrollments` stores only FKs — never the student name or course title. |

---

## 2. Entity list

| # | Table | Purpose | PK | FKs |
|---|---|---|---|---|
| 1 | `Users` | Base identity for every account (single login) | `UserId` | — |
| 2 | `Specializations` | The 4 tracks | `SpecializationId` | — |
| 3 | `Advisers` | Role data for faculty | `AdviserId` | `UserId`→Users |
| 4 | `Students` | Role data for advisees | `StudentId` | `UserId`→Users, `SpecializationId`→Specializations, `AdviserId`→Advisers |
| 5 | `Administrators` | Role data for admins | `AdminId` | `UserId`→Users |
| 6 | `Courses` | Course catalog | `CourseId` | `SpecializationId`→Specializations (nullable = core) |
| 7 | `Prerequisites` | Dependency rules | `PrerequisiteId` | `CourseId`→Courses, `RequiredCourseId`→Courses |
| 8 | `Semesters` | Academic terms | `SemesterId` | — |
| 9 | `CourseOfferings` | Which course runs in which semester | `OfferingId` | `CourseId`→Courses, `SemesterId`→Semesters |
| 10 | `Enrollments` | Student ↔ course + **status** | `EnrollmentId` | `StudentId`→Students, `CourseId`→Courses, `SemesterId`→Semesters |
| 11 | `CourseRequests` | A submitted plan | `RequestId` | `StudentId`→Students, `AdviserId`→Advisers, `SemesterId`→Semesters |
| 12 | `CourseRequestLines` | Courses inside a plan | `LineId` | `RequestId`→CourseRequests, `CourseId`→Courses |
| 13 | `Notifications` | Written by the Observer's `StudentNotificationService` | `NotificationId` | `UserId`→Users |

> `Notifications` is the supporting table that makes the **Observer pattern**
> demonstrable in the database, not just in the UML.

---

## 3. Relationship / cardinality summary for the ERD

| Relationship | Cardinality | Notes |
|---|---|---|
| `Users` 1 — 1 `Students` / `Advisers` / `Administrators` | 1:1 (exclusive arc) | A user row is exactly one role |
| `Specializations` 1 — 0..* `Students` | 1:N | A student declares one track |
| `Specializations` 1 — 0..* `Courses` | 1:N | Specialised courses only (core courses have NULL) |
| `Advisers` 1 — 0..* `Students` | 1:N | Optional assignment |
| `Courses` 1 — 0..* `Prerequisites` | 1:N | Rules where this course is the dependent one |
| `Courses` 1 — 0..* `Prerequisites` | 1:N | Rules where this course is the required one (**M:N self-relationship**) |
| `Students` 1 — 0..* `Enrollments` | 1:N | Academic record |
| `Courses` 1 — 0..* `Enrollments` | 1:N | |
| `Semesters` 1 — 0..* `Enrollments` | 1:N | |
| `Students` 1 — 0..* `CourseRequests` | 1:N | |
| `CourseRequests` 1 — 1..* `CourseRequestLines` | 1:N (identifying) | Child rows deleted with the parent |
| `Courses` 1 — 0..* `CourseRequestLines` | 1:N | |
| `Semesters` 1 — 0..* `CourseOfferings` | 1:N | |


---

## 4. Schema (DDL — run this to create the LocalDB database)

```sql
-- =====================================================================
--  SCAIS  —  Smart Course Advising Information System
--  SQL Server LocalDB schema (3NF)
-- =====================================================================

CREATE TABLE Users (
    UserId        INT IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(128) NOT NULL,       -- stored hashed, never plain text
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(100) NOT NULL,
    Role          NVARCHAR(20)  NOT NULL
        CHECK (Role IN ('Administrator','Adviser','Student')),
    IsActive      BIT           NOT NULL DEFAULT 1,
    CreatedDate   DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Specializations (
    SpecializationId INT IDENTITY(1,1) PRIMARY KEY,
    Name             NVARCHAR(50)  NOT NULL UNIQUE,
    Description      NVARCHAR(200) NULL
);

CREATE TABLE Advisers (
    AdviserId  INT IDENTITY(1,1) PRIMARY KEY,
    UserId     INT          NOT NULL UNIQUE,
    Department NVARCHAR(50) NOT NULL,
    Office     NVARCHAR(50) NULL,
    CONSTRAINT FK_Advisers_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Students (
    StudentId        INT IDENTITY(1,1) PRIMARY KEY,
    UserId           INT          NOT NULL UNIQUE,
    SpecializationId INT          NOT NULL,
    AdviserId        INT          NULL,        -- nullable: not yet assigned
    EnrollmentYear   INT          NOT NULL,
    GPA              DECIMAL(3,2) NULL,
    CONSTRAINT FK_Students_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Students_Spec
        FOREIGN KEY (SpecializationId) REFERENCES Specializations(SpecializationId),
    CONSTRAINT FK_Students_Adviser
        FOREIGN KEY (AdviserId) REFERENCES Advisers(AdviserId)
);

CREATE TABLE Administrators (
    AdminId     INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT          NOT NULL UNIQUE,
    AccessLevel NVARCHAR(20) NOT NULL DEFAULT 'Full',
    CONSTRAINT FK_Admins_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Courses (
    CourseId         INT IDENTITY(1,1) PRIMARY KEY,
    Code             NVARCHAR(10)  NOT NULL UNIQUE,
    Title            NVARCHAR(100) NOT NULL,
    CreditHours      INT           NOT NULL CHECK (CreditHours BETWEEN 1 AND 6),
    IsCore           BIT           NOT NULL DEFAULT 0,
    SpecializationId INT           NULL,   -- NULL => core subject
    Description      NVARCHAR(255) NULL,
    CONSTRAINT FK_Courses_Spec
        FOREIGN KEY (SpecializationId) REFERENCES Specializations(SpecializationId)
);

```sql
CREATE TABLE Prerequisites (
    PrerequisiteId   INT IDENTITY(1,1) PRIMARY KEY,
    CourseId         INT          NOT NULL,   -- the dependent course
    RequiredCourseId INT          NOT NULL,   -- must be passed first
    RuleType         NVARCHAR(20) NOT NULL DEFAULT 'Prerequisite'
        CHECK (RuleType IN ('Prerequisite','Corequisite')),
    CONSTRAINT FK_Prereq_Course
        FOREIGN KEY (CourseId) REFERENCES Courses(CourseId),
    CONSTRAINT FK_Prereq_Required
        FOREIGN KEY (RequiredCourseId) REFERENCES Courses(CourseId),
    CONSTRAINT UQ_Prereq UNIQUE (CourseId, RequiredCourseId, RuleType),
    CONSTRAINT CK_Prereq_NotSelf CHECK (CourseId <> RequiredCourseId)
);

CREATE TABLE Semesters (
    SemesterId   INT IDENTITY(1,1) PRIMARY KEY,
    TermName     NVARCHAR(20) NOT NULL,   -- 'Semester 1' .. 'Semester 4'
    AcademicYear INT          NOT NULL,
    StartDate    DATE         NULL,
    EndDate      DATE         NULL,
    CONSTRAINT UQ_Semester UNIQUE (TermName, AcademicYear)
);

CREATE TABLE CourseOfferings (
    OfferingId INT IDENTITY(1,1) PRIMARY KEY,
    CourseId   INT NOT NULL,
    SemesterId INT NOT NULL,
    Capacity   INT NOT NULL DEFAULT 30,
    CONSTRAINT FK_Offering_Course
        FOREIGN KEY (CourseId) REFERENCES Courses(CourseId),
    CONSTRAINT FK_Offering_Semester
        FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId),
    CONSTRAINT UQ_Offering UNIQUE (CourseId, SemesterId)
);

CREATE TABLE Enrollments (
    EnrollmentId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId    INT          NOT NULL,
    CourseId     INT          NOT NULL,
    SemesterId   INT          NOT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Completed','InProgress','Pending')),
    Grade        NVARCHAR(3)  NULL,
    CONSTRAINT FK_Enroll_Student
        FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Enroll_Course
        FOREIGN KEY (CourseId) REFERENCES Courses(CourseId),
    CONSTRAINT FK_Enroll_Semester
        FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId),
    CONSTRAINT CK_Enroll_Grade CHECK (
        (Status = 'Completed' AND Grade IS NOT NULL) OR (Status <> 'Completed')
    )
);

CREATE TABLE CourseRequests (
    RequestId     INT IDENTITY(1,1) PRIMARY KEY,
    StudentId     INT           NOT NULL,
    AdviserId     INT           NULL,
    SemesterId    INT           NOT NULL,
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'Draft'
        CHECK (Status IN ('Draft','Submitted','Approved','Rejected')),
    SubmittedDate DATETIME      NULL,
    AdviserRemark NVARCHAR(255) NULL,
    CONSTRAINT FK_Req_Student
        FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Req_Adviser
        FOREIGN KEY (AdviserId) REFERENCES Advisers(AdviserId),
    CONSTRAINT FK_Req_Semester
        FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId)
);

CREATE TABLE CourseRequestLines (
    LineId          INT IDENTITY(1,1) PRIMARY KEY,
    RequestId       INT           NOT NULL,
    CourseId        INT           NOT NULL,
    EligibilityNote NVARCHAR(255) NULL,
    CONSTRAINT FK_Line_Request
        FOREIGN KEY (RequestId) REFERENCES CourseRequests(RequestId) ON DELETE CASCADE,
    CONSTRAINT FK_Line_Course
        FOREIGN KEY (CourseId) REFERENCES Courses(CourseId)
);

CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId         INT           NOT NULL,
    Message        NVARCHAR(255) NOT NULL,
    CreatedDate    DATETIME      NOT NULL DEFAULT GETDATE(),
    IsRead         BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Notif_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
```


---

## 5. Data layer implementation (10 marks)

```
IRepository<T>                       (interface — the contract)
      ▲
      │ implements
SqlRepositoryBase<T>                 (abstract — shared ADO.NET plumbing)
      ▲
      ├── UserRepository
      ├── CourseRepository
      ├── PrerequisiteRepository
      ├── EnrollmentRepository
      ├── CourseRequestRepository
      └── SpecializationRepository
              │
              ▼
       DatabaseManager  (Singleton — owns the one connection)
```

**`DatabaseManager` (Singleton) — the reason it exists, not just the fact:**
```csharp
public sealed class DatabaseManager
{
    private static DatabaseManager _instance;
    private static readonly object _lock = new object();

    private readonly string _connectionString;

    // Private constructor => callers cannot create a second manager.
    private DatabaseManager()
    {
        _connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;
                              AttachDbFilename=|DataDirectory|\SCAIS.mdf;
                              Integrated Security=True;";
    }

    public static DatabaseManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null) { _instance = new DatabaseManager(); }
                return _instance;
            }
        }
    }

    public SqlConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

**Repository pattern — parameterised only:**
```csharp
public class CourseRepository : SqlRepositoryBase<Course>, IRepository<Course>
{
    public override List<Course> GetAll()
    {
        const string sql = @"SELECT CourseId, Code, Title, CreditHours, IsCore,
                                    SpecializationId, Description
                             FROM Courses ORDER BY Code";
        // ... SqlDataReader -> List<Course>
    }

    public override bool Add(Course c)
    {
        const string sql = @"INSERT INTO Courses
                (Code, Title, CreditHours, IsCore, SpecializationId, Description)
                VALUES (@Code, @Title, @CreditHours, @IsCore, @SpecId, @Description)";
        // NEVER: "... VALUES ('" + c.Code + "', ...)"  <- SQL injection, marks lost
    }
}
```

---

## 6. Mock data plan (≥15 records in every table)

| Table | Rows to seed | Notes |
|---|---|---|
| `Specializations` | 4 | Programming, Networking, Cybersecurity, Database |
| `Users` | 40 | 2 admins + 8 advisers + 30 students |
| `Administrators` | 2 | maps to the 2 admin users |
| `Advisers` | 8 | maps to 8 adviser users |
| `Students` | 30 | maps to 30 student users, spread over 4 tracks |
| `Courses` | 28 | 8 core + 5 specialised per track |
| `Prerequisites` | 20 | incl. `Database 1 → Database 2`, `Advanced Networking → Networking & Data Communications 2` |
| `Semesters` | 15 | 4 terms × several academic years (to give ≥15) |
| `CourseOfferings` | 40 | courses × semesters |
| `Enrollments` | 120 | mix of Completed (with grades) / InProgress / Pending |
| `CourseRequests` | 20 | Draft / Submitted / Approved / Rejected |
| `CourseRequestLines` | 45 | 2–3 courses per request |
| `Notifications` | 25 | generated by the Observer when requests change status |

**Seed the data so the demo tells a story:** include at least one student who is
**blocked** from `Database 2` because `Database 1` is not completed — that is the
live proof of the validation logic in the demo video.

### 6.1 Sample courses (Programming track)

| Code | Title | Core? | Specialization | Prerequisite |
|---|---|---|---|---|
| IT101 | Computer Systems | Yes | — | — |
| IT102 | Math for Computing | Yes | — | — |
| IT103 | Unix System | Yes | — | — |
| IT210 | Database 1 | Yes | — | — |
| IT211 | Database 2 | No | Database | IT210 |
| IT220 | Advanced Networking | No | Networking | IT103 |
| IT221 | Networking & Data Communications 2 | No | Networking | IT220 |
| IT230 | Object Oriented Design | No | Programming | IT102 |
| IT240 | Wireless Communication | No | Networking | IT220 |
| IT250 | Defense in Depth | No | Cybersecurity | IT220 |
| IT260 | Database Administration | No | Database | IT211 |

---

## 7. Connection string & packaging notes

```csharp
// App.config
<connectionStrings>
  <add name="SCAIS"
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;
                         AttachDbFilename=|DataDirectory|\SCAIS.mdf;
                         Integrated Security=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- Ship `SCAIS.mdf` (and `SCAIS_log.ldf` if generated) **inside the project** and
  set **Copy to Output Directory = Copy if newer** so the marker's machine
  attaches it automatically.
- Add a short "How to run" note to the project README: build → press F5 → login
  with the seeded demo credentials (`admin / Admin@123`, `adviser1 / Adviser@123`,
  `student1 / Student@123`).

```

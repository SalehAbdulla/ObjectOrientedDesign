# 02 — Architecture & Class Design Specification

This document is the blueprint the class diagram, the database and the code are
all generated from. If these three artefacts disagree, marks are lost under
*"UML consistency"*. Keep this file as the single definition.

---

## 1. Layered architecture

```
┌──────────────────────────────────────────────────────────────┐
│  Presentation Layer  (Windows Forms)                         │
│  frmLogin · frmAdminDashboard · frmAdviserDashboard          │
│  frmStudentDashboard · frmCourseCatalog · frmUserManagement  │
│  frmCourseRequestReview · frmReports                         │
└───────────────▲──────────────────────────────────────────────┘
                │ depends on abstractions only
┌───────────────┴──────────────────────────────────────────────┐
│  Business / Domain Layer                                     │
│  User(abstract) · Student · Adviser · Administrator          │
│  Course · Specialization · Prerequisite · Enrollment         │
│  CourseRequest ◆ CourseRequestLine · Semester                │
│  Services: AuthenticationService · EligibilityValidator      │
│            CourseAdvisingService · ReportService             │
│  Patterns: DatabaseManager(Singleton) · IUserObserver(Observer)│
│            IEligibilityRule(Strategy)                        │
└───────────────▲──────────────────────────────────────────────┘
                │ IRepository<T>
┌───────────────┴──────────────────────────────────────────────┐
│  Data Access Layer                                           │
│  IRepository<T> · SqlRepositoryBase<T>                       │
│  UserRepository · CourseRepository · EnrollmentRepository    │
│  PrerequisiteRepository · CourseRequestRepository            │
│  DatabaseManager (Singleton, owns the LocalDB connection)    │
└───────────────▲──────────────────────────────────────────────┘
                │ ADO.NET (parameterised)
┌───────────────┴──────────────────────────────────────────────┐
│  SQL Server LocalDB  —  SCAIS.mdf  (13 tables, 3NF)          │
└──────────────────────────────────────────────────────────────┘
```

**Dependency rule:** arrows point *inwards/downwards*. The UI never contains SQL;
the data layer never contains business rules.

---

## 2. The inheritance hierarchy (inheritance + polymorphism marks)

```
                 ┌────────────────────────────┐
                 │  <<abstract>>  User        │
                 ├────────────────────────────┤
                 │ - userId        : int      │
                 │ - username      : string   │
                 │ - passwordHash  : string   │
                 │ - fullName      : string   │
                 │ - email         : string   │
                 │ - isActive      : bool     │
                 ├────────────────────────────┤
                 │ + UserId                {get}   │
                 │ + Username              {get}   │
                 │ + FullName              {get;set}│
                 │ + Email                 {get;set}│
                 │ + IsActive              {get;set}│
                 │ + Authenticate(pwd):bool        │
                 │ + SetPassword(pwd):void         │
                 │ + GetProfileSummary():string  «abstract» │
                 │ + GetDashboardTitle():string  «abstract» │
                 │ + GetRole():UserRole          «abstract» │
                 └───────────┬────────────────┘
              ┌──────────────┼───────────────────┐
              │              │                   │
   ┌──────────▼───────┐ ┌────▼─────────────┐ ┌───▼────────────────┐
   │ Student          │ │ Adviser          │ │ Administrator      │
   ├──────────────────┤ ├──────────────────┤ ├────────────────────┤
   │ - studentId : int│ │ - adviserId: int │ │ - adminId    : int │
   │ - gpa       : dec│ │ - department:str │ │ - accessLevel:str  │
   │ - enrollYear:int │ │ - office    :str │ │ - canManageUsers:bool│
   ├──────────────────┤ ├──────────────────┤ ├────────────────────┤
   │ + StudentId      │ │ + AdviserId      │ │ + AdminId          │
   │ + GPA       {get}│ │ + Department     │ │ + AccessLevel      │
   │ + EnrollmentYear │ │ + Office         │ │ + CanManageUsers   │
   │ + Specialization │ │ + Advisees {get} │ │ + GetProfileSummary│
   │ + AdviserId      │ │ + AssignAdvisee()│ │ + GetDashboardTitle│
   │ + GetCompleted() │ │ + ReviewRequest()│ │ + GetRole()        │
   │ + GetInProgress()│ │ + GetProfileSummary()                 │
   │ + GetPending()   │ │ + GetDashboardTitle()                 │
   │ + GetProfileSummary()                                    │
   │ + GetDashboardTitle()                                    │
   │ + GetRole()                                              │
   └──────────────────┘ └──────────────────┘ └────────────────────┘
```

**Why this inheritance is justified (write this in the report):**
- It is a genuine **is-a**: every `Student`, `Adviser` and `Administrator` **is a** `User`
  and can be used anywhere a `User` is expected (e.g. the login service returns a `User`).
- The **common responsibilities** — identity, credentials, activation state,
  authentication — are implemented **once** in `User` (reuse).
- The **specialised behaviour** — which dashboard to open, what the profile summary
  contains, what the role enum returns — is deferred to the children (extension).
- `User` is **abstract** because there is no such thing as a "generic user" in
  SCAIS: a row in `Users` always belongs to one of the three concrete roles.


---

## 3. Domain classes

### 3.1 `Specialization`
| Member | Type | Visibility | Note |
|---|---|---|---|
| `SpecializationId` | int | public get | PK |
| `Name` | string | public get/set | Programming / Networking / Cybersecurity / Database |
| `Description` | string | public get/set | |
| `Courses` | `List<Course>` | public get | Aggregation — courses belong to a track |

### 3.2 `Course`
| Member | Type | Visibility | Note |
|---|---|---|---|
| `courseId` | int | **private** | field, exposed via `CourseId { get; }` |
| `code` | string | **private** | e.g. `IT210` |
| `title` | string | **private** | |
| `creditHours` | int | **private** | setter validates 1–6 |
| `isCore` | bool | **private** | core vs specialised |
| `specializationId` | `int?` | **private** | null for core subjects |
| `CreditHours` | int | public get / public set (validated) | throws `ArgumentOutOfRangeException` |
| `GetDisplayName()` | string | public | `"IT210 — Database 1"` |
| `IsCoreSubject()` | bool | public | |

### 3.3 `Prerequisite`  (self-referencing many-to-many)
| Member | Type | Note |
|---|---|---|
| `PrerequisiteId` | int | PK |
| `CourseId` | int | the *dependent* course |
| `RequiredCourseId` | int | the course that must be passed first |
| `RuleType` | `RuleType` enum | `Prerequisite` or `Corequisite` |
| `IsSatisfiedBy(Student)` | bool | delegates to `EligibilityValidator` |

### 3.4 `Enrollment`  (the join that carries status)
| Member | Type | Note |
|---|---|---|
| `enrollmentId` | int | **private** |
| `status` | `EnrollmentStatus` | **private** — cannot be set from outside |
| `grade` | string | **private** |
| `EnrollmentId` | int | public get |
| `StudentId` / `CourseId` | int | public get |
| `Status` | `EnrollmentStatus` | **public get only** |
| `Grade` | string | public get only |
| `Semester` / `AcademicYear` | string / int | public get/set |
| `MarkCompleted(grade)` | void | public — validates grade format before changing state |
| `MarkInProgress()` | void | public |
| `MarkPending()` | void | public |
| `IsCompleted()` | bool | public |

> This is the strongest **encapsulation** example in the system: `Status` and
> `Grade` are read-only properties mutated only through behaviour methods, so an
> `Enrollment` can never be put into an invalid state (e.g. *completed* with no grade).

### 3.5 `CourseRequest` + `CourseRequestLine`  (composition)
```
CourseRequest ◆──────────── 1..* CourseRequestLine
   requestId                        lineId
   studentId                        requestId (FK)
   semester, academicYear           courseId
   status : RequestStatus           eligibilityNote
   submittedDate
   adviserRemark
```
- **Composition**, not aggregation: a `CourseRequestLine` has no identity or
  lifetime without its `CourseRequest`. Deleting the request deletes its lines.
- `CourseRequest` is the **Subject** in the Observer pattern (see `04-design-patterns.md`).
- State transitions are guarded:
  `Submit()` → `Approve(remark)` → `Reject(remark)`, each firing `NotifyObservers()`.

### 3.6 `CourseOffering`  (the timetable: which course runs in which term)
| Member | Type | Visibility | Note |
|---|---|---|---|
| `offeringId` | int | **private** | PK, exposed via `OfferingId { get; }` |
| `course` | `Course` | **private** | the course that runs; null is rejected |
| `semester` | `Semester` | **private** | the term it runs in; null is rejected |
| `capacity` | int | **private** | setter validates 1–500 |
| `enrolled` | int | **private** | read from `Enrollments`, **not** a column of the table |
| `Capacity` | int | public get / public set (validated) | throws `ArgumentOutOfRangeException` |
| `Enrolled` | int | public get / public set | the setter exists for the repository only |
| `SeatsRemaining` | int | public get | `Capacity - Enrolled`, never negative |
| `HasSpace()` | bool | public | is there a place left? |
| `HasEnrollments()` | bool | public | the rule that stops an offering being withdrawn |
| `GetDisplayName()` · `GetAvailabilityText()` | string | public | display strings for the screen |

- The catalogue says **what may be taught** and the prerequisite rules say **what may follow
  what**; neither says what **actually runs** this term. `CourseOffering` holds that third fact,
  which is what UC-07 maintains.
- It holds a `Course` and a `Semester` rather than two integer keys, so the screen can display
  *"IT221 … in Semester 2, 2026"* without a second query.
- `Enrolled` is deliberately derived: the number of students taking a course is a fact about
  `Enrollments`, and storing it twice would let the two copies disagree.

### 3.7 Enumerations
| Enum | Values |
|---|---|
| `UserRole` | `Administrator`, `Adviser`, `Student` |
| `EnrollmentStatus` | `Completed`, `InProgress`, `Pending` |
| `RequestStatus` | `Draft`, `Submitted`, `Approved`, `Rejected` |
| `RuleType` | `Prerequisite`, `Corequisite` |


---

## 4. Service classes (business layer)

| Class | Responsibility (single!) | Key operations |
|---|---|---|
| `AuthenticationService` | Verify credentials, return the right subclass, open a session | `Login(username, password) : LoginResult` |
| `EligibilityValidator` | Run the rule pipeline and decide eligibility | `Validate(Student, Course) : EligibilityResult` |
| `CourseAdvisingService` | Recommend courses; submit/approve requests | `GetEligibleCourses(Student)` · `SubmitRequest()` · `ApproveRequest()` |
| `ReportService` | Build report data sets | `GetCourseProgression(Student)` · `GetPrerequisiteCompliance()` |
| `UserFactory` | Create the correct `User` subclass from a DB row | `CreateUser(DataRow) : User` |
| `SessionManager` | Hold the currently logged-in user for the app lifetime | `CurrentUser` |

**`UserFactory` is where polymorphism pays off:**
```csharp
// One call site, three behaviours — this is the polymorphism evidence for the report.
public static User CreateUser(DataRow row)
{
    UserRole role = (UserRole)Enum.Parse(typeof(UserRole), row["Role"].ToString());
    switch (role)
    {
        case UserRole.Student:       return new Student(row);
        case UserRole.Adviser:       return new Adviser(row);
        case UserRole.Administrator: return new Administrator(row);
        default: throw new NotSupportedException("Unknown role: " + role);
    }
}
```
The login form then does:
```csharp
User user = authenticationService.Login(txtUsername.Text, txtPassword.Text).User;

// Polymorphic dispatch — no if/else on role anywhere in the UI:
lblWelcome.Text = user.GetProfileSummary();
OpenDashboard(user.GetDashboardTitle(), user);   // "Student Dashboard", "Adviser Dashboard", ...
```


---

## 5. Interfaces (interface marks)

| Interface | Members | Implemented by | Why it exists |
|---|---|---|---|
| `IRepository<T>` | `T GetById(int)` · `List<T> GetAll()` · `bool Add(T)` · `bool Update(T)` · `bool Delete(int)` | `SqlRepositoryBase<T>` → `UserRepository`, `CourseRepository`, `CourseOfferingRepository`, `SemesterRepository`, `EnrollmentRepository`, `PrerequisiteRepository`, `CourseRequestRepository`, `NotificationRepository`, `SpecializationRepository` | One CRUD contract for all tables; the UI/services depend on the contract, so swapping LocalDB for a web API later (the brief mentions web/mobile expansion) requires no change to callers |
| `IEligibilityRule` | `string RuleName { get; }` · `EligibilityResult Evaluate(Student, Course)` | `PrerequisiteRule`, `CorequisiteRule`, `CreditLimitRule`, `AlreadyCompletedRule` | Strategy pattern; new academic rules are added by adding a class, not by editing `EligibilityValidator` |
| `IUserObserver` | `void OnRequestStatusChanged(CourseRequest, RequestStatus)` | `StudentNotificationService`, `AuditLogService` | Observer pattern; decouples approval logic from notification |

---

## 6. Relationship summary (copy straight into the class diagram)

| From | → | To | Type | Multiplicity | Justification |
|---|---|---|---|---|---|
| `User` | ◁— | `Student`, `Adviser`, `Administrator` | Inheritance | — | Genuine is-a |
| `Adviser` | ◇— | `Student` | Aggregation | `1 — 0..*` | A student exists without an adviser; assignment is Admin-managed and reversible |
| `Student` | — | `Specialization` | Association | `* — 1` | Every student declares exactly one track |
| `Specialization` | ◇— | `Course` | Aggregation | `1 — 0..*` | A track groups courses, but a core course exists independently |
| `Course` | — | `Prerequisite` | Association | `1 — 0..*` | A course may have many rules |
| `Prerequisite` | — | `Course` | Association (self-ref) | `* — 1` | Links dependent course to required course |
| `Student` | — | `Enrollment` | Association | `1 — 0..*` | Academic record |
| `Enrollment` | — | `Course` | Association | `* — 1` | Which course was taken |
| `Course` | — | `CourseOffering` | Association | `1 — 0..*` | A course may run in many terms |
| `Semester` | — | `CourseOffering` | Association | `1 — 0..*` | A term contains many offerings (UC-07) |
| `Student` | — | `CourseRequest` | Association | `1 — 0..*` | A student submits many requests over time |
| `CourseRequest` | ◆— | `CourseRequestLine` | **Composition** | `1 — 1..*` | Lines have no life without the request |
| `CourseRequestLine` | — | `Course` | Association | `* — 1` | Each line requests one course |
| `Adviser` | — | `CourseRequest` | Association | `1 — 0..*` | Adviser reviews requests (approve/modify) |
| `SqlRepositoryBase<T>` | ▷-- | `IRepository<T>` | Realisation | — | Interface implementation |
| `PrerequisiteRule` etc. | ▷-- | `IEligibilityRule` | Realisation | — | Interface implementation |
| `EligibilityValidator` | — | `IEligibilityRule` | Association | `1 — 1..*` | Holds the rule pipeline (Strategy) |
| `CourseRequest` | — | `IUserObserver` | Association | `1 — 0..*` | Observer subject |

---

## 7. Sequence diagrams to draw (4 minimum)

| ID | Scenario | Lifelines | Key messages to show |
|---|---|---|---|
| **SD-1** | Login + role redirect | `User`, `frmLogin`, `AuthenticationService`, `UserRepository`, `DatabaseManager`, `User`(subclass) | `Login(user, pwd)` → `GetByUsername()` → `Authenticate()` → `CreateUser(row)` → return `User` → `GetDashboardTitle()` → `OpenDashboard()` |
| **SD-2** | Student views eligible courses & submits a request | `Student`, `frmStudentDashboard`, `CourseAdvisingService`, `EligibilityValidator`, `PrerequisiteRule`, `CourseRequestRepository` | `GetEligibleCourses()` → loop `Validate()` → `Evaluate()` → `EligibilityResult` → `SubmitRequest()` → `Insert()` |
| **SD-3** | Adviser approves a request (Observer fires) | `Adviser`, `frmCourseRequestReview`, `CourseAdvisingService`, `CourseRequest`, `IUserObserver`, `StudentNotificationService` | `ApproveRequest()` → `Approve(remark)` → `NotifyObservers()` → `OnRequestStatusChanged()` |
| **SD-4** | Admin defines a prerequisite rule | `Administrator`, `frmCourseCatalog`, `CourseRepository`, `PrerequisiteRepository`, `DatabaseManager` | `AddCourse()` → `Add()` → `AddPrerequisite()` → `Insert()` |

**Consistency rule:** the lifeline names in these diagrams must be the **same
class names** as the class diagram. Markers check this for the "UML consistency"
marks.

---

## 8. UI forms → which classes each form uses

| Form | Uses | Purpose |
|---|---|---|
| `frmLogin` | `AuthenticationService`, `SessionManager`, `User` | Single login for all roles |
| `frmAdminDashboard` | `SessionManager`, navigation only | Admin menu |
| `frmUserManagement` | `IRepository<User>` → `UserRepository`, `UserFactory` | Create/update/deactivate users |
| `frmCourseCatalog` | `CourseRepository`, `PrerequisiteRepository` | CRUD course + prerequisite rules |
| `frmAssignAdvisees` | `AdviserRepository`, `StudentRepository` | Assign advisees |
| `frmAdviserDashboard` | `SessionManager` | Adviser menu |
| `frmAdviseeList` | `StudentRepository`, `EnrollmentRepository` | Assigned advisees + history |
| `frmRecommendCourses` | `CourseAdvisingService`, `EligibilityValidator` | Recommend eligible courses |
| `frmCourseRequestReview` | `CourseRequestRepository`, `CourseAdvisingService`, `CourseRequest` | Approve / modify / reject |
| `frmStudentDashboard` | `SessionManager` | Student menu |
| `frmMyRecord` | `EnrollmentRepository` | Completed / in-progress / pending |
| `frmEligibleCourses` | `CourseAdvisingService` | View + select courses |
| `frmMyRequests` | `CourseRequestRepository` | Approved courses + adviser remarks |
| `frmReports` | `ReportService` | ≥2 reports |


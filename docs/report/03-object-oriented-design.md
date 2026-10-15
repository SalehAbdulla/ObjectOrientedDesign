# 3. Object-Oriented Design

**Rubric: 25 marks — encapsulation 5, abstraction 4, inheritance 4, polymorphism 5,
interface 3, OO design quality 4.**

The rubric states the marking principle for this section explicitly:

> *"We distinguish between using an OO feature and demonstrating OO competency."*

Each sub-section below therefore gives the class, the code, and **the reason the design is
that way**. A feature with no reason behind it earns the lower band.

---

## 3.1 Encapsulation — 5 marks

### The example: `Enrollment`

`Enrollment` is one entry in a student's academic record. Its two meaningful attributes —
the course status and the grade — have **no public setter at all**. They are private fields
exposed through get-only properties, and the only way to change them is through the
behaviour methods.

```csharp
private EnrollmentStatus status;   // private, with no public setter
private string grade;              // private, with no public setter

public EnrollmentStatus Status { get { return this.status; } }
public string Grade            { get { return this.grade; } }

public void MarkCompleted(string grade)
{
    if (!IsValidGrade(grade))
    {
        throw new ArgumentException(
            "A completed course must have a valid grade such as A, B+ or C.", "grade");
    }

    this.status = EnrollmentStatus.Completed;
    this.grade = grade.Trim().ToUpperInvariant();
}

public void MarkInProgress()
{
    this.status = EnrollmentStatus.InProgress;
    this.grade = null;      // a course in progress has no result yet
}
```

`[CODE: Domain/Enrollment.cs — the private fields, the get-only properties and MarkCompleted]`

**Why encapsulation is used here.** The rule "a completed course must have a grade" is a
fact about the domain, not a preference. If `Status` had a public setter, any screen could
write `enrollment.Status = Completed` and leave the grade empty, producing a record that
says a student passed a subject without saying how well. That record would then be read by
the progression report and by the prerequisite rule, both of which treat the subject as
passed.

By making the state change only through `MarkCompleted`, the rule is enforced in one place
and cannot be bypassed. The same rule is *also* enforced by the database with the
`CK_Enrollments_Grade` check constraint, so the invariant holds even if a row is written by
something other than this class.

### Three levels of access in `User`

`User` demonstrates that visibility is not a binary choice between private and public.

| Member | Visibility | Why |
|---|---|---|
| `passwordHash` | `private` | Nothing outside the class may read the credential |
| `IsValidEmail` | `private static` | An implementation detail of the `Email` setter |
| `PasswordHashForPersistence` | `internal` | The data layer must be able to write the hash, but no screen may read it |
| `UserId`, `Username`, `Role` | `public` get-only | Identity is read by screens, assigned by the database, and must never change |
| `FullName`, `Email` | `public` get/set | Editable, but only through setters that validate |
| `Authenticate`, `SetPassword` | `public` methods | The only ways to work with the credential |

```csharp
private string passwordHash;        // no public accessor at all

internal string PasswordHashForPersistence
{
    get { return this.passwordHash; }   // visible inside the SCAIS assembly only
}

public bool Authenticate(string plainPassword)
{
    if (!this.isActive || string.IsNullOrEmpty(plainPassword))
    {
        return false;                   // a deactivated account cannot sign in
    }

    return PasswordHasher.Verify(plainPassword, this.passwordHash);
}
```

`[CODE: Domain/User.cs — the private field, the internal accessor and Authenticate]`

**Why.** A password must never leave the object. `internal` rather than `public` on the
persistence accessor means the repository can store the hash while every screen is
structurally unable to read it. There is no `Password` property anywhere in the system, so
there is nowhere for a plain text password to be stored by accident.

### Validation in the setter

`Course` and `User` refuse to hold invalid values rather than validating later:

```csharp
public int CreditHours
{
    get { return this.creditHours; }
    set
    {
        if (value < MinimumCreditHours || value > MaximumCreditHours)
        {
            throw new ArgumentOutOfRangeException("value", ...);
        }

        this.creditHours = value;
    }
}
```

`[CODE: Domain/Course.cs — the CreditHours setter with its guard]`

**Why.** If the validation were left to the user interface, a `Course` with 99 credit hours
could exist and would corrupt the credit limit rule. Enforcing it in the setter means an
invalid `Course` cannot be constructed at all, which is a stronger guarantee than
validating before saving.

---

## 3.2 Inheritance — 4 marks

### Parent class: `User` (abstract)

### Child classes: `Student`, `Adviser`, `Administrator`

```csharp
public abstract class User
{
    protected User(int userId, string username, string passwordHash, string fullName,
                   string email, UserRole role, bool isActive) { ... }

    public int UserId { get { return this.userId; } }
    public string Username { get { return this.username; } }
    public UserRole Role { get { return this.role; } }

    public abstract string GetProfileSummary();
    public abstract string GetDashboardTitle();
    public abstract bool CanApproveCourseRequests { get; }

    public bool Authenticate(string plainPassword) { ... }
    public void SetPassword(string plainPassword) { ... }
}

public class Student : User { ... }
public class Adviser : User { ... }
public class Administrator : User { ... }
```

`[CODE: Domain/User.cs and the three class declarations in Student.cs, Adviser.cs and Administrator.cs]`

### Why the is-a relationship is genuine

The test is not "do the classes look similar" but "can every child be used wherever the
parent is expected". Three questions decide it:

1. **Can an adviser be passed to anything expecting a user?** Yes. All three roles sign in
   through the same screen and the same `AuthenticationService`, which returns a `User`.
2. **Do the children share real state and behaviour?** Yes. Every account has a user
   identifier, a username, a credential and an activation flag, and every account
   authenticates in exactly the same way. That is a substantial body of code, not a single
   method.
3. **Is there a member on the parent that makes no sense for one of the children?** No.
   Every public member of `User` applies to all three roles.

### What is shared and what is specialised

| Member | Where it lives | Why |
|---|---|---|
| `UserId`, `Username`, `Role`, `IsActive` | `User` | Identical for every role |
| `Authenticate`, `SetPassword` | `User` | Implemented **once**. If each subclass hashed its own password, the three implementations could drift apart and one of them would eventually be wrong |
| `FullName`, `Email` setters with validation | `User` | The same rule for everyone |
| `GetProfileSummary()` | `User` abstract, overridden | The summary differs per role: a student shows their track and GPA, an adviser their department and advisee count, an administrator their access level |
| `GetDashboardTitle()` | `User` abstract, overridden | Drives the role redirect |
| `CanApproveCourseRequests` | `User` abstract, overridden | True only for an adviser |
| `StudentId`, `Specialization`, `Enrollments`, `HasCompleted()` | `Student` only | Only a student has an academic record |
| `AdviserId`, `Department`, `Advisees`, `AssignAdvisee()` | `Adviser` only | Only an adviser is assigned students |
| `AdminId`, `AccessLevel`, `CanManageUsers` | `Administrator` only | Only an administrator maintains the system |

### Why `User` is abstract

There is no such thing as a generic user in SCAIS. Every row in the `Users` table has a
`Role` constrained by a check constraint to one of the three values, and `UserFactory`
builds the matching subclass. Marking `User` abstract states that in the type system:
`new User(...)` will not compile, so no code can ever create an account that has no role.

### Why inheritance was **not** used elsewhere

The rubric warns that inheritance should not be created simply to demonstrate the concept.
Three places where it was deliberately rejected:

| Rejected inheritance | Why it would have been wrong |
|---|---|
| `Student extends Enrollment` | A student is not a kind of enrolment. The relationship is that a student **has** enrolments, which is an association |
| A base class for `Course` and `Prerequisite` | The two share nothing but a word. A course is a subject; a rule is a dependency between two subjects. There is no common behaviour to reuse, so the "base class" would have contained nothing |
| A separate `StudentValidator`, `AdviserValidator`, `AdministratorValidator` hierarchy | The rules differ by **course and semester**, not by the type of account. Inheritance would have been the wrong axis of variation; that is why Strategy is used for eligibility instead |

---

## 3.3 Polymorphism — 5 marks

There are **three** distinct polymorphic mechanisms in SCAIS, not one. The first is the
one the login screen depends on.

### 3.3.1 One call site, three behaviours

The login form holds a `User` reference. It never asks which subclass it has, and there is
no `if` or `switch` on the role anywhere in the user interface except the one factory.

```csharp
// Domain/User.cs - three abstract members, one for each differing behaviour
public abstract string GetProfileSummary();
public abstract string GetDashboardTitle();
public abstract bool CanApproveCourseRequests { get; }

// Domain/Student.cs
public override string GetProfileSummary()
{
    string track = this.specialization == null ? "no track declared" : this.specialization.Name;
    string average = this.gpa.HasValue ? this.gpa.Value.ToString("0.00") : "not yet graded";
    return string.Format("Student - {0} - GPA {1}", track, average);
}
public override string GetDashboardTitle() { return "Student Dashboard"; }
public override bool CanApproveCourseRequests { get { return false; } }

// Domain/Adviser.cs
public override string GetProfileSummary()
{
    return string.Format("Adviser - {0} - {1} advisee(s)", this.department, this.advisees.Count);
}
public override string GetDashboardTitle() { return "Adviser Dashboard"; }
public override bool CanApproveCourseRequests { get { return true; } }

// Domain/Administrator.cs
public override string GetProfileSummary()
{
    return string.Format("Administrator - access level {0}", this.accessLevel);
}
public override string GetDashboardTitle() { return "Administrator Dashboard"; }
public override bool CanApproveCourseRequests { get { return false; } }
```

`[CODE: Domain/User.cs, Student.cs, Adviser.cs, Administrator.cs — the four overrides side by side]`

The **single call site** is in the dashboard shell:

```csharp
// UI/DashboardForm.cs
Label summary = UiTheme.CreateLabel(this.Account.GetProfileSummary());
```

`[CODE: UI/DashboardForm.cs — the one call to GetProfileSummary]`

and in the login form:

```csharp
// UI/frmLogin.cs
UiTheme.ShowInfo(this, string.Format(
    "Welcome, {0}.\r\n\r\n{1}\r\n\r\nOpening the {2}.",
    result.User.FullName,
    result.User.GetProfileSummary(),
    result.User.GetDashboardTitle()));
```

`[CODE: UI/frmLogin.cs — the single call that produces three different messages]`

**The same two calls therefore produce:**

| Account signed in | `GetProfileSummary()` returns | `GetDashboardTitle()` returns |
|---|---|---|
| `student01` | `Student - Networking - GPA 3.15` | `Student Dashboard` |
| `adviser01` | `Adviser - Information Technology - 4 advisee(s)` | `Adviser Dashboard` |
| `admin` | `Administrator - access level Full` | `Administrator Dashboard` |

**Why this is polymorphic use and not a coincidence of naming.** The `DashboardForm` class
is written once and is inherited by all three dashboards. It contains no knowledge of
students, advisers or administrators, yet it renders the correct summary for each, because
the *object* decides what the method does. Adding a fourth role would require no change to
`DashboardForm` at all.

### 3.3.2 Polymorphic use in the user interface, not just in the domain

`CanApproveCourseRequests` is not decorative. It is what decides whether a menu item exists:

```csharp
// UI/frmAdviserDashboard.cs
if (this.adviser.CanApproveCourseRequests)
{
    this.AddMenuItem("Review submitted course plans", this.OnReviewPlans);
}
```

`[CODE: UI/frmAdviserDashboard.cs — the capability flag driving the menu]`

**Why it matters.** The adviser dashboard asks the **account** whether it may approve plans
rather than testing the role. If the institute later decides that senior administrators may
also approve, the change is one `return true;` in `Administrator` and no screen changes.

### 3.3.3 Polymorphism through an interface: the eligibility rules

`EligibilityValidator` runs a list of `IEligibilityRule` and never knows which concrete
rules it holds:

```csharp
// Business/EligibilityValidator.cs
for (int index = 0; index < this.rules.Count; index++)
{
    IEligibilityRule rule = this.rules[index];
    EligibilityResult result = rule.Evaluate(student, course);

    if (result == null || !result.IsEligible)
    {
        failures.Add(result == null ? rule.RuleName + ": returned no verdict" : result.Reason);
    }
}
```

`[CODE: Business/EligibilityValidator.cs — the loop that runs every rule]`

Five different classes answer the same message with five different algorithms. This is
runtime polymorphism: the set of rules is decided when the validator is constructed, not
when it is compiled.

### 3.3.4 Polymorphism through an abstract generic base

`SqlRepositoryBase<T>` implements `IRepository<T>` and declares the five CRUD operations
abstract. Eight repositories override them with their own SQL. Callers hold an
`IRepository<T>` and never know which repository they have.

---

## 3.4 Abstraction — 4 marks

### Example 1: `User` is abstract and exposes three operations instead of a role

`User` is an abstraction in both senses: it cannot be instantiated, and it exposes only
what a caller needs.

**What is hidden:**
- which concrete subclass an account is
- how the password is hashed, salted and verified
- how a student's GPA or track is obtained
- how an adviser counts their advisees
- how an administrator's access level is interpreted

**What is exposed:** identity (`UserId`, `Username`, `Role`, `FullName`, `Email`,
`IsActive`), three operations that differ by role (`GetProfileSummary`,
`GetDashboardTitle`, `CanApproveCourseRequests`), and the two credential operations.

**Why.** The login screen needs to greet the person, open the right dashboard and know
whether they may approve plans. That is the whole requirement. Exposing the role as a
`UserRole` enum and letting every screen switch on it would also have worked, but it would
have put the same three-way decision in every screen and made adding a role a change to all
of them. The abstraction reduces a dozen potential call sites to three methods.

### Example 2: `EligibilityValidator` hides the rule pipeline

```csharp
public EligibilityResult Validate(Student student, Course course)
```

The caller passes a student and a course and receives a verdict with a reason. Hidden behind
that one signature are five rule objects, the order they run in, the fact that the rules
read the curriculum from the database, and the fact that **all** failures are collected
rather than the first one being returned.

`[CODE: Business/EligibilityValidator.cs — Validate and CreateDefaultRules]`

**Why.** The student's screen and the adviser's screen both need an eligibility verdict.
Neither needs to know that there are five rules, and neither should break when a sixth is
added. The abstraction is what makes the administrator's requirement — "maintain the rules
of the system" — satisfiable without touching a screen.

### Example 3: `Student` answers questions about its own record

`Student` exposes `HasCompleted(courseId)`, `GetCompletedCourses()`,
`GetInProgressCourses()`, `GetPendingCourses()`, `GetCreditsForSemester(semesterId)` and
`GetEarnedCredits()`. It does **not** expose the `List<Enrollment>` for callers to filter.

**Why.** If the rule classes each filtered the list themselves, the definition of
"completed" would be duplicated in five places, and the fifth one would eventually be
wrong. The abstraction is the answer to "is this course passed", not the data needed to work
it out.

### The line that was drawn

Abstraction is not the same as hiding everything. Three things are deliberately exposed
because hiding them would have made the system harder to use, not safer:

| Exposed | Why |
|---|---|
| `CourseRequestLine.IsEligible` | The screen must colour a blocked course differently; recomputing the verdict would duplicate the rules |
| `Course.CreditHours` with a public setter | An administrator edits it, and the setter validates, so exposure is safe |
| `UserRecord` with public properties | It is a data transfer object between the repository and the factory; it holds no rules of its own |

---

## 3.5 Interface — 3 marks

Three interfaces are used. Each represents a genuine contract with more than one
implementation.

### 3.5.1 `IRepository<T>`

```csharp
public interface IRepository<T>
{
    T GetById(int id);
    List<T> GetAll();
    bool Add(T entity);
    bool Update(T entity);
    bool Delete(int id);
}
```

**Implementing classes:** `SqlRepositoryBase<T>` (abstract) and, through it,
`UserRepository`, `CourseRepository`, `PrerequisiteRepository`, `EnrollmentRepository`,
`CourseRequestRepository`, `NotificationRepository`, `SpecializationRepository` and
`SemesterRepository`.

**Purpose.** One CRUD contract for every table. The business layer depends on the contract
rather than on a concrete repository, which is what makes the stated plan for a web front
end feasible: replacing LocalDB with a web API would mean writing new implementations of
`IRepository<T>` and changing nothing in `CourseAdvisingService` or `ReportService`.

**Why an interface *and* an abstract class.** Each is used for what it is good at. The
interface carries the **contract** that callers depend on; the abstract class carries the
**shared ADO.NET code** that implementations reuse. A class can therefore satisfy the
contract without being forced into a particular implementation.

### 3.5.2 `IEligibilityRule`

```csharp
public interface IEligibilityRule
{
    string RuleName { get; }
    EligibilityResult Evaluate(Student student, Course course);
}
```

**Implementing classes:** `PrerequisiteRule`, `CorequisiteRule`, `CreditLimitRule`,
`AlreadyCompletedRule`, `SpecializationRule`.

**Purpose.** Each rule is a self-contained algorithm behind a common contract, so
`EligibilityValidator` can run a list of them without knowing what they are. This is the
Strategy pattern's contract, and it is the reason a new academic rule is a new class rather
than an edit to existing code.

**Why an interface rather than an abstract base class.** The five rules share no state and
no implementation — only the shape of the operation. `AlreadyCompletedRule` has no
dependencies at all while `PrerequisiteRule` holds a repository, so a common base class
would have contained nothing. An interface expresses exactly what they do share.

### 3.5.3 `IUserObserver`

```csharp
public interface IUserObserver
{
    void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus);
}
```

**Implementing classes:** `StudentNotificationService`, `AuditLogService`.

**Purpose.** The contract that lets `CourseRequest` announce a status change without knowing
who is listening. The two implementations do genuinely unrelated things — one writes a
database row, the other appends to a log file — which is precisely why they cannot be
related by inheritance and can only be related by a contract.

**Why an interface is required here.** Without it, `CourseRequest` would need a reference to
each listener, which would make a domain class depend on the notification table and on file
logging. The interface is what inverts that dependency.

### What is **not** behind an interface

Two things are concrete classes rather than interfaces, and the reason is stated so that it
does not read as an omission:

| Concrete class | Why no interface |
|---|---|
| `PasswordHasher` | It has exactly one correct implementation. An interface would suggest it is replaceable, which would invite a weaker implementation to be substituted |
| The domain entities | `Student`, `Course` and the rest are the subject matter of the system, not services. Interfaces would add indirection without adding a choice |

---

## 3.6 Overall OO design quality — 4 marks

### Responsibility assignment

Each class has one reason to change. This is the table a marker can check against the code.

| Class | Its single responsibility | What it deliberately does **not** do |
|---|---|---|
| `User` and its three subclasses | Represent an account and its identity | Do not open a database, do not know about screens |
| `Course` | Represent a catalogue subject | Does not know which students take it |
| `Enrollment` | Represent one entry in a record and protect its own state | Does not compute eligibility |
| `CourseRequest` | Own a plan, guard its state transitions, and announce changes | Does not know who listens, does not write to the database |
| `CourseRequestLine` | Represent one requested course and its verdict | Does not evaluate eligibility |
| `EligibilityValidator` | Run the rules and combine the verdicts | Does not contain any rule |
| Each rule class | Implement exactly one academic rule | Do not combine with other rules |
| `CourseAdvisingService` | Coordinate the advising workflow and own its transaction boundaries | Does not render anything |
| `ReportService` | Shape report rows | Does not render or print them |
| `DatabaseManager` | Own the connection configuration | Does not contain SQL |
| Each repository | The SQL and mapping for one table | Does not contain business rules |
| `UiTheme`, `DashboardForm` | Define the shared appearance | Do not contain business logic |
| Each form | Present one screen and call a service | Does not contain SQL or eligibility rules |

### Cohesion

Every rule class is an example of high cohesion. `CreditLimitRule` does one thing and every
line in it is about credit totals. `PrerequisiteRule` does one thing and every line in it is
about prerequisites. Neither contains a method that belongs to the other.

The counter-example that was avoided: a single `EligibilityChecker` with a `switch` on rule
type would have had low cohesion, because its methods would have addressed five unrelated
concerns, and every change to any rule would have meant editing the same class.

### Coupling

Coupling was kept low deliberately, and the evidence is the direction of the dependencies:

| From | To | Coupling |
|---|---|---|
| Forms | Services | Low — a form knows `CourseAdvisingService`, not which repository it uses |
| Services | `IRepository<T>` | Low — the service depends on the contract, not on a concrete class |
| Repositories | `DatabaseManager` | Low — one named dependency, obtained through the Singleton rather than constructed |
| Rules | `Student`, `Course`, `IEligibilityRule` | Low — a rule reads the student's in-memory record rather than querying |
| `CourseRequest` | `IUserObserver` | Low — the subject depends on an interface and on no concrete observer |

The one deliberate piece of higher coupling is `CourseAdvisingService` to its five
repositories. That is intentional: a service whose job is to coordinate a workflow across
several tables is exactly where that knowledge belongs, and concentrating it there is what
keeps it out of the screens.

### Reuse

| Reused | By | How many times |
|---|---|---|
| `User.Authenticate` and the hashing | All three roles | 1 implementation, 3 roles |
| `IRepository<T>` contract | All 8 repositories | 1 contract, 8 implementations |
| `SqlRepositoryBase<T>` plumbing | All 8 repositories | ~190 lines of ADO.NET written once |
| `IEligibilityRule` | 5 rules, 3 calling screens | 1 contract, 5 algorithms, 3 callers |
| `UiTheme` and `DashboardForm` | 11 forms, 3 dashboards | 1 definition of the appearance |
| `EligibilityValidator` | Student screen, adviser screen, compliance report | 1 validation, 3 consumers |

### Maintainability

Four properties that make the design maintainable, each with a concrete consequence:

1. **A new academic rule** costs one new class and one line in `CreateDefaultRules`.
   Nothing else changes.
2. **A new reaction to a decision** costs one new `IUserObserver` implementation and one
   `Attach` call. `CourseRequest` is not edited.
3. **A change to the appearance** is made once, in `UiTheme`.
4. **A change of database** affects only the `DataAccess` namespace, because everything
   above it holds `IRepository<T>`.

### The deliberate imperfections

Stating these is part of demonstrating understanding rather than hiding them. Each is also
repeated in the conclusion.

| Imperfection | Consequence | What would fix it |
|---|---|---|
| `DatabaseManager` is a Singleton, so it is global state | The instance cannot easily be substituted with a fake in a unit test | Dependency injection with a container |
| `SessionManager` is a static class | One user at a time | An instance held by the application, or per-request state for a web front end |
| The rules are composed in code rather than configured in the database | Adding a rule needs a rebuild | Store the active rule set per semester and build the pipeline from it |
| Forms are laid out in code rather than in designer files | The Visual Studio designer cannot edit them | Move each layout into a `.Designer.cs` file if the team prefers the designer |
| The password hash uses a fixed salt | Not suitable for production | A per-user random salt and a deliberately slow algorithm such as PBKDF2 or bcrypt |

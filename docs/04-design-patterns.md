# 04 — Design Patterns Specification (20 marks)

The brief requires **at least 2 design patterns**, and explicitly says
*"singleton cannot be the only design pattern."* SCAIS uses **three**:
Singleton, Observer, Strategy.

The rubric awards 20 marks in six pieces. Every pattern below is written against
that structure, because *"the mark should not be awarded simply for including a
pattern"*:

| Rubric criterion | Marks |
|---|---|
| Identification of design problem | 4 |
| Selection and justification of pattern | 4 |
| Correct UML implementation | 4 |
| Correct programming implementation | 4 |
| Explanation of participants and interactions | 2 |
| Evaluation of benefits / limitations | 2 |

---

## 1. Pattern A — Singleton (`DatabaseManager`)

### 1.1 Design problem (4)
SCAIS is a single-user desktop application, but several independent parts of the
system open data at the same time: the login service reads `Users`, the student
dashboard reads `Enrollments`, and the adviser screen reads `CourseRequests`.
If each of those created its **own** connection manager, the application would
hold several open connections to the same LocalDB file, which causes file-locking
errors (`The process cannot access the file because it is being used by another
process`), inconsistent reads, and duplicated configuration strings. The problem
is therefore: **the system needs exactly one object to own the database
connection configuration and hand out connections.**

### 1.2 Selection & justification (4)
Singleton was selected because the constraint is genuinely *"one instance for the
whole application"*, which is exactly what the pattern guarantees. Alternatives
considered and rejected:
- *A static helper class* — cannot hold per-application state cleanly, cannot be
  swapped for a mock in testing, and cannot implement an interface.
- *Dependency injection with a container* — correct in a web application, but it
  adds a framework dependency that is disproportionate for a WinForms course
  project.
Singleton is the smallest solution that satisfies the "exactly one" requirement.

### 1.3 UML implementation (4)
```
        ┌────────────────────────────────────────────┐
        │              DatabaseManager               │
        ├────────────────────────────────────────────┤
        │ - static _instance : DatabaseManager       │
        │ - static _lock     : object                │
        │ - _connectionString : string               │
        ├────────────────────────────────────────────┤
        │ - DatabaseManager()                        │  «private constructor»
        │ + static Instance : DatabaseManager {get}  │
        │ + CreateConnection() : SqlConnection       │
        └────────────────────────────────────────────┘
                          ▲
                          │ «use»
        ┌─────────────────┴──────────────────┐
        │  SqlRepositoryBase<T>              │
        │  CourseRepository, UserRepository, │
        │  EnrollmentRepository, …           │
        └────────────────────────────────────┘
```
Draw this as a separate small class diagram in EA, tagged
`<<Singleton>>`, and add it to the design-pattern package.

### 1.4 Programming implementation (4)
See `03-database-design.md` §5 — `sealed` class, `private static` instance,
`private` constructor, `lock` for thread safety, single `Instance` accessor.
**`sealed` is important**: it prevents subclassing from creating a second instance.

### 1.5 Participants & interactions (2)
| Participant | Role |
|---|---|
| `DatabaseManager` | The single instance; owns the connection string; creates `SqlConnection` objects |
| `SqlRepositoryBase<T>` and its subclasses | Clients — they never build a connection string themselves; they call `DatabaseManager.Instance.CreateConnection()` |
| `frmLogin` / services | Indirect clients through the repositories |

Interaction: repository method → `DatabaseManager.Instance` → `CreateConnection()`
→ `SqlCommand` → `SqlDataReader` → entity list → repository returns to service.

### 1.6 Benefits & limitations (2)
**Benefits:** guarantees one configuration source; removes connection-string
duplication; lazy initialisation (created only when first needed); a single place
to change the database target when the system moves to a web/mobile back end.

**Limitations:** introduces global state, which makes unit testing harder because
the instance cannot easily be replaced with a fake; it hides a dependency (a class
that uses it does not advertise that it needs a database); and it needs locking in
a multi-threaded context, which is a small performance cost. For a large
multi-user system, dependency injection would be the better choice.

---


## 2. Pattern B — Observer (`CourseRequest` → `IUserObserver`)

### 2.1 Design problem (4)
When an adviser approves or rejects a course plan, **several unrelated things must
happen at once**: the student must be notified on their dashboard, an audit entry
must be written, and the enrolment records may need updating. If `CourseRequest`
called each of those directly, the domain class would need to know about UI
notifications and logging — a class that should only model *a request* would be
coupled to the presentation and audit layers, and adding a new reaction later
would mean editing `CourseRequest`. The problem is: **one state change must notify
an unknown, growing set of interested parties without the subject knowing them.**

### 2.2 Selection & justification (4)
Observer was selected because it models exactly this "one-to-many dependency with
automatic notification" situation. Alternatives considered and rejected:
- *Direct method calls from `Approve()`* — tightly couples the domain to the UI
  and audit layers; every new reaction requires editing `CourseRequest`.
- *A polling loop in the student dashboard* — wasteful, and the student would see
  the update only after a delay.
- *Events (`event` keyword)* — C# events are in fact an Observer implementation;
  the interface-based version was chosen so observers can be stored in a list,
  iterated and unit-tested as objects, and so the design maps visibly onto the
  GoF UML for marking.

### 2.3 UML implementation (4)
```
   ┌───────────────────────────┐              ┌─────────────────────────────────┐
   │      «interface»          │              │        CourseRequest            │
   │      IUserObserver        │              ├─────────────────────────────────┤
   ├───────────────────────────┤              │ - _observers : List<IUserObserver>│
   │ + OnRequestStatusChanged( │              │ - _status    : RequestStatus    │
   │     request, newStatus)   │              ├─────────────────────────────────┤
   └───────────▲───────────────┘              │ + Attach(o : IUserObserver)     │
               │ «implements»                 │ + Detach(o : IUserObserver)     │
   ┌───────────┴────────────────┐             │ + Submit()  : void              │
   │ StudentNotificationService │◁────────────│ + Approve(remark) : void        │
   ├────────────────────────────┤  «notifies» │ + Reject(remark)  : void        │
   │ + OnRequestStatusChanged() │  0..*       │ - NotifyObservers() : void      │
   └────────────────────────────┘             └─────────────────────────────────┘
   ┌────────────────────────────┐
   │ AuditLogService            │
   ├────────────────────────────┤
   │ + OnRequestStatusChanged() │
   └────────────────────────────┘
```


### 2.4 Programming implementation (4)
```csharp
/// <summary>Observer contract: anything that wants to react to a request
/// status change implements this.</summary>
public interface IUserObserver
{
    void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus);
}

/// <summary>Subject. Owns the list of observers and notifies them.</summary>
public class CourseRequest
{
    private readonly List<IUserObserver> _observers = new List<IUserObserver>();
    private RequestStatus _status = RequestStatus.Draft;

    public RequestStatus Status { get { return _status; } }

    public void Attach(IUserObserver observer) { _observers.Add(observer); }
    public void Detach(IUserObserver observer) { _observers.Remove(observer); }

    public void Submit()
    {
        _status = RequestStatus.Submitted;
        SubmittedDate = DateTime.Now;
        NotifyObservers();
    }

    public void Approve(string remark)
    {
        _status = RequestStatus.Approved;
        AdviserRemark = remark;
        NotifyObservers();          // <- the whole point of the pattern
    }

    public void Reject(string remark)
    {
        _status = RequestStatus.Rejected;
        AdviserRemark = remark;
        NotifyObservers();
    }

    private void NotifyObservers()
    {
        foreach (IUserObserver observer in _observers)
        {
            observer.OnRequestStatusChanged(this, _status);
        }
    }
}

/// <summary>Concrete observer - writes a row to Notifications.</summary>
public class StudentNotificationService : IUserObserver
{
    public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
    {
        string message = string.Format(
            "Your course plan #{0} was {1}.", request.RequestId, newStatus);
        new NotificationRepository().Add(new Notification(request.StudentId, message));
    }
}

/// <summary>Concrete observer - writes an audit trail entry.</summary>
public class AuditLogService : IUserObserver
{
    public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
    {
        Logger.Write(string.Format("Request {0} -> {1}", request.RequestId, newStatus));
    }
}
```

### 2.5 Participants & interactions (2)
| Participant | Role |
|---|---|
| `IUserObserver` | Observer interface — the contract every listener implements |
| `CourseRequest` | Concrete Subject — holds `List<IUserObserver>`, changes state, calls `NotifyObservers()` |
| `StudentNotificationService` | Concrete Observer — creates a `Notifications` row for the student |
| `AuditLogService` | Concrete Observer — writes an audit line |
| `CourseAdvisingService` | Attaches the observers when a request is loaded for review |

Interaction: adviser clicks **Approve** → `CourseAdvisingService.ApproveRequest()`
→ `CourseRequest.Approve(remark)` → status changes → `NotifyObservers()` →
`StudentNotificationService.OnRequestStatusChanged()` (DB insert) →
`AuditLogService.OnRequestStatusChanged()` (log line) → grid refreshes.

### 2.6 Benefits & limitations (2)
**Benefits:** the subject does not know its observers, so notification logic can
grow without touching `CourseRequest`; observers can be added and removed at
runtime; new behaviour (e.g. emailing the student) is a new class, not an edit to
existing code (open/closed principle).

**Limitations:** observers are notified in an arbitrary order and there is no
built-in guarantee that a failing observer is handled — an exception in one
observer can interrupt the loop, so `NotifyObservers()` should be wrapped in
try/catch; forgetting to `Detach()` creates a memory leak because the subject
keeps a strong reference; and debugging is harder because the call chain is
indirect rather than visible at the point where the state changed.


## 3. Pattern C — Strategy (`IEligibilityRule`)

### 3.1 Design problem (4)
Course eligibility in SCAIS is not one rule. A course may require:
a **passed prerequisite**, a **concurrently-taken corequisite**, a **credit-hour
limit** for the semester, and it must not be a course the student has **already
completed**. If `EligibilityValidator` contained one long method with nested
`if/else` blocks for all of these, then (a) the class would violate single
responsibility, (b) the Admin's ability to *define new rules* would require a code
change and redeployment, and (c) the validation logic could not be tested in
isolation. The problem is: **the eligibility algorithm varies independently of the
class that uses it, and must be selectable/composable at runtime.**

### 3.2 Selection & justification (4)
Strategy was selected because each academic rule is a self-contained algorithm
behind a common interface, and the validator is the context that runs them.
Alternatives considered and rejected:
- *A single method with `switch` on rule type* — every new rule edits the method
  (violates open/closed) and the method grows unmanageably.
- *Inheriting different validators per student type* — the rules differ by
  **course/semester**, not by student type, so inheritance would be the wrong axis
  of variation.
- *Composite pattern* — a natural complement; the validator is in effect a
  composite that runs a list of strategies, which is why the two are combined here
  rather than either alone.

### 3.3 UML implementation (4)
```
   ┌──────────────────────────────┐
   │        «interface»           │
   │        IEligibilityRule      │
   ├──────────────────────────────┤
   │ + RuleName : string {get}    │
   │ + Evaluate(student : Student,│
   │            course  : Course) │
   │            : EligibilityResult│
   └──────────────▲───────────────┘
                  │ «implements»
   ┌──────────────┼──────────────┬──────────────────┬────────────────────┐
   │              │              │                  │                    │
┌──┴────────┐ ┌───┴───────┐ ┌────┴──────┐ ┌─────────┴────┐ ┌─────────────┴──┐
│Prerequisite│ │Corequisite│ │CreditLimit│ │AlreadyCompleted│ │ Specialization │
│Rule        │ │Rule       │ │Rule       │ │Rule            │ │ Rule           │
└────────────┘ └───────────┘ └───────────┘ └────────────────┘ └────────────────┘
                  ▲ «uses» (context holds 1..* rules)
   ┌──────────────┴───────────────────────────────────┐
   │            EligibilityValidator                  │
   ├──────────────────────────────────────────────────┤
   │ - _rules : List<IEligibilityRule>                │
   │ + EligibilityValidator(rules)                    │
   │ + Validate(student, course) : EligibilityResult  │
   └──────────────────────────────────────────────────┘
                          ▲ «uses»
   ┌──────────────────────┴───────────────────────────┐
   │ CourseAdvisingService · frmEligibleCourses        │
   └──────────────────────────────────────────────────┘
```

---


### 3.4 Programming implementation (4)
```csharp
/// <summary>Strategy contract for one eligibility rule.</summary>
public interface IEligibilityRule
{
    string RuleName { get; }
    EligibilityResult Evaluate(Student student, Course course);
}

/// <summary>Result object returned by a rule.</summary>
public class EligibilityResult
{
    public bool IsEligible { get; private set; }
    public string Reason { get; private set; }

    public EligibilityResult(bool isEligible, string reason)
    {
        IsEligible = isEligible;
        Reason = reason;
    }

    public static EligibilityResult Pass(string rule)
    { return new EligibilityResult(true, rule + ": satisfied"); }

    public static EligibilityResult Fail(string rule, string detail)
    { return new EligibilityResult(false, rule + ": " + detail); }
}

/// <summary>Concrete strategy 1 - the prerequisite must be completed.</summary>
public class PrerequisiteRule : IEligibilityRule
{
    private readonly PrerequisiteRepository _repository = new PrerequisiteRepository();

    public string RuleName { get { return "Prerequisite"; } }

    public EligibilityResult Evaluate(Student student, Course course)
    {
        List<Prerequisite> rules = _repository.GetByCourse(course.CourseId);

        foreach (Prerequisite rule in rules)
        {
            if (rule.RuleType != RuleType.Prerequisite) { continue; }

            if (!student.HasCompleted(rule.RequiredCourseId))
            {
                return EligibilityResult.Fail(RuleName,
                    "must complete " + rule.RequiredCourseCode + " first");
            }
        }
        return EligibilityResult.Pass(RuleName);
    }
}

/// <summary>Concrete strategy 3 - semester credit-hour ceiling.</summary>
public class CreditLimitRule : IEligibilityRule
{
    private const int MaxCredits = 18;

    public string RuleName { get { return "CreditLimit"; } }

    public EligibilityResult Evaluate(Student student, Course course)
    {
        int current = student.GetCreditsForCurrentSemester();
        if (current + course.CreditHours > MaxCredits)
        {
            return EligibilityResult.Fail(RuleName,
                "exceeds the " + MaxCredits + " credit limit");
        }
        return EligibilityResult.Pass(RuleName);
    }
}

/// <summary>Context - runs whichever rules it was given, in order.</summary>
public class EligibilityValidator
{
    private readonly List<IEligibilityRule> _rules;

    public EligibilityValidator(List<IEligibilityRule> rules)
    {
        if (rules == null) { throw new ArgumentNullException("rules"); }
        _rules = rules;
    }

    public EligibilityResult Validate(Student student, Course course)
    {
        var failures = new List<string>();

        foreach (IEligibilityRule rule in _rules)
        {
            EligibilityResult result = rule.Evaluate(student, course);
            if (!result.IsEligible) { failures.Add(result.Reason); }
        }

        return failures.Count == 0
            ? new EligibilityResult(true, "Eligible")
            : new EligibilityResult(false, string.Join("; ", failures));
    }
}
```
Wiring (in `CourseAdvisingService`):
```csharp
var validator = new EligibilityValidator(new List<IEligibilityRule>
{
    new AlreadyCompletedRule(),
    new PrerequisiteRule(),
    new CorequisiteRule(),
    new CreditLimitRule()
});
```

### 3.5 Participants & interactions (2)
| Participant | Role |
|---|---|
| `IEligibilityRule` | Strategy interface |
| `PrerequisiteRule`, `CorequisiteRule`, `CreditLimitRule`, `AlreadyCompletedRule`, `SpecializationRule` | Concrete strategies |
| `EligibilityValidator` | Context — holds the strategies and runs them |
| `CourseAdvisingService` | Client — builds the strategy list and asks for a verdict |
| `EligibilityResult` | Value object carrying the verdict and reason text shown in the UI |

Interaction: student opens **Eligible Courses** → `CourseAdvisingService.GetEligibleCourses()`
→ for each course `EligibilityValidator.Validate()` → each rule's `Evaluate()`
→ failures collected → `EligibilityResult` → the grid shows pass/fail with the reason.

### 3.6 Benefits & limitations (2)
**Benefits:** each rule is a small, cohesive, independently testable class; new
academic rules are added by writing a new class and adding it to the list — no
existing code is modified (open/closed); the rules can be reordered, enabled or
disabled per semester; the client (`CourseAdvisingService`) is unaware of the
concrete rules.

**Limitations:** it increases the number of classes, which is over-engineering for
a system with only one rule; the client must know which strategies to construct
and in what order, so some configuration knowledge leaks out of the validator; and
because rules are independent, a cross-rule dependency (e.g. "only check the
credit limit if the prerequisite passes") has to be handled explicitly by the
context rather than inside a rule.

---

## 4. Pattern summary table (put this in the report)

| Pattern | Problem it solves | Key participants | Where in the code |
|---|---|---|---|
| **Singleton** | One object must own the DB connection configuration | `DatabaseManager` | `DataAccess/DatabaseManager.cs` |
| **Observer** | One status change must notify several decoupled parties | `CourseRequest`, `IUserObserver`, `StudentNotificationService`, `AuditLogService` | `Domain/CourseRequest.cs`, `Services/StudentNotificationService.cs` |
| **Strategy** | The eligibility algorithm varies and must be extensible at runtime | `IEligibilityRule`, 5 concrete rules, `EligibilityValidator` | `Business/Eligibility/*.cs` |

---

## 5. Report template for each pattern (matches the 20-mark breakdown)

```
6.x Design Pattern: <Name>

(a) The design problem  .............................................. 4 marks
    What went wrong / what would go wrong without a pattern?
    Describe it as a problem in SCAIS, not a textbook definition.

(b) Why this pattern  ................................................ 4 marks
    Why is this pattern the right fit?
    Name the alternatives you rejected and say why - this is what earns the marks.

(c) UML  ............................................................ 4 marks
    Show the pattern's participants, their relationships and responsibilities.
    Reference the diagram in the EA project.

(d) Implementation  ................................................. 4 marks
    Code snippet showing the pattern's mechanics (private constructor, observer
    list, strategy interface...). Point out the line that makes it the pattern.

(e) Participants & interactions  .................................... 2 marks
    Table: participant | role | what it does.
    Then one sentence: "When X happens, A calls B which notifies C."

(f) Benefits and limitations  ....................................... 2 marks
    Two or three benefits AND at least two real limitations.
```

> **Do not skip (f).** It is the cheapest 2 marks in the whole rubric and the most
> commonly omitted.

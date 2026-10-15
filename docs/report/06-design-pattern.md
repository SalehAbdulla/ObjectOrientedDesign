# 6. Design Pattern

**Rubric: 20 marks — identification of the design problem 4, selection and justification 4,
correct UML implementation 4, correct programming implementation 4, explanation of
participants 2, evaluation of benefits and limitations 2.**

The brief requires at least two design patterns and states that Singleton cannot be the only
one. SCAIS uses **three**: Singleton, Observer and Strategy.

The rubric is explicit that the marks are not awarded for including a pattern:

> *"'We used Singleton in our project' should receive limited credit."*

Each pattern below is therefore written against all six criteria, with the **alternatives
that were rejected** and the **limitations** stated. The limitations are worth two marks per
pattern and are the most commonly omitted part of the rubric.

The structure diagrams are in [`../uml/05-design-patterns.md`](../uml/05-design-patterns.md).

---

## 6.1 Pattern 1 — Singleton: `DatabaseManager`

### (a) The design problem — 4 marks

SCAIS is a single-user desktop application, but several independent parts of it read the
database at the same time:

- the login service reads the `Users` table,
- the student's record screen reads `Enrollments`,
- the adviser's review screen reads `CourseRequests`,
- the reports read across almost every table.

If each of those built its own connection manager, the application would hold several
independent copies of the connection string and would open several concurrent connections to
the same LocalDB file. SQL Server LocalDB is a file-backed instance, and multiple simultaneous
writers produce exactly the failure this design avoids: `The process cannot access the file
because it is being used by another process`, and reads that see a half-written state.

There is also a maintenance problem underneath. The connection string lives in `App.config`
today, but the institute has stated that a web and mobile back end is planned. If the
connection string were read in five places, moving to a different data store would mean
finding and changing all five.

**The problem, stated plainly: the system needs exactly one object that knows the connection
configuration and hands out connections, and no code anywhere should be able to create a
second one.**

### (b) Why this pattern, and what was rejected — 4 marks

Singleton is the right fit because the requirement is *literally* "one instance for the whole
application", which is the guarantee the pattern exists to provide. The implementation
enforces it in the type system rather than by convention: the constructor is `private`, so no
caller can construct one, and the class is `sealed`, so no subclass can be used to create a
second instance through a different route.

Alternatives considered and rejected:

| Alternative | Why it was rejected |
|---|---|
| **A static helper class** (`static class Db`) | Cannot hold per-application state cleanly, cannot implement an interface, and cannot be replaced with a test double. A static class also cannot enforce its own initialisation order, which matters here because `EnsureDatabase()` must run before the first query |
| **A new `DatabaseManager` wherever one is needed** | This is the problem, not a solution. Two instances mean two connection strings and two open connections |
| **Dependency injection with a container** | The correct answer for a web application, and the better long-term choice. It was rejected because it adds a framework dependency and a composition root that are disproportionate for a single-window desktop application, and because the brief fixes the technology stack. The limitation is recorded in (f) |
| **A module-level connection pool** | `SqlConnection` already pools underneath. The problem being solved is the *configuration*, not the connections, so a pool addresses the wrong thing |

### (c) UML implementation — 4 marks

`[DIAGRAM: pattern-singleton — see ../uml/05-design-patterns.md §5.1]`

The UML shows the four things that make it the pattern rather than an ordinary manager:

| Element | How it is drawn |
|---|---|
| The private constructor | `- DatabaseManager() void` — the `-` marker is the whole point |
| The static instance field | `-instance DatabaseManager` with a static marker |
| The static accessor | `+Instance DatabaseManager` with a static marker |
| The clients | `SqlRepositoryBase<T>` with a «uses» dependency to `DatabaseManager` |

### (d) Programming implementation — 4 marks

```csharp
public sealed class DatabaseManager
{
    private static readonly object SyncRoot = new object();
    private static DatabaseManager instance;
    private readonly string connectionString;

    // Private constructor: only Instance may create the manager.
    private DatabaseManager()
    {
        this.connectionString = ReadConnectionString();
    }

    public static DatabaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (SyncRoot)
                {
                    if (instance == null)
                    {
                        instance = new DatabaseManager();
                    }
                }
            }

            return instance;
        }
    }

    public SqlConnection CreateConnection()
    {
        SqlConnection connection = new SqlConnection(this.connectionString);
        connection.Open();
        return connection;
    }

    public void EnsureDatabase() { ... }
}
```

`[CODE: DataAccess/DatabaseManager.cs — the sealed class, the private constructor and the Instance accessor]`

**The three lines that make it a Singleton:**

1. `public sealed class` — a subclass cannot create a second instance.
2. `private DatabaseManager()` — no caller can construct one.
3. `private static DatabaseManager instance;` with the guarded `Instance` accessor — the only
   route to the object.

The double-checked lock is present because `Instance` could be reached from more than one
thread if a later version loads data in the background. Without it, two threads could both
pass the null check and create two managers, which would defeat the entire purpose.

### (e) Participants and interactions — 2 marks

| Participant | Role | What it does |
|---|---|---|
| `DatabaseManager` | Singleton | Owns the connection string, creates connections, and bootstraps the database on first run |
| `SqlRepositoryBase<T>` | Client | Calls `DatabaseManager.Instance.CreateConnection()`; never builds a connection string itself |
| The eight concrete repositories | Indirect clients | Inherit the behaviour from `SqlRepositoryBase<T>` |
| `Program.Main` | Startup client | Calls `Instance.EnsureDatabase()` once, before the login screen opens |

**Interaction, in one sentence:** a repository method calls
`DatabaseManager.Instance.CreateConnection()`, receives an open `SqlConnection`, runs its
parameterised command, disposes the connection and returns domain objects — and at no point
does any repository know where the connection string came from.

### (f) Benefits and limitations — 2 marks

**Benefits**

1. **One configuration source.** The connection string is read once. Moving to a different
   server, or to a web API, is a change to one class.
2. **No connection-string duplication.** The failure mode of five drifting copies is
   eliminated by construction.
3. **Lazy initialisation.** The manager is created on first use, so start-up does not read
   configuration that may never be needed.
4. **Guaranteed ordering.** `Program.Main` can call `EnsureDatabase()` once and know that the
   database exists before any screen queries it.

**Limitations**

1. **It introduces global state.** A class that uses `DatabaseManager.Instance` does not
   advertise that it needs a database. The dependency is hidden inside a method body rather
   than visible in a constructor parameter, which makes the design harder to read and the
   class harder to test in isolation.
2. **It cannot be substituted in a test.** There is no seam to inject a fake manager, so a
   unit test of `CourseRepository` would need a real database. This is the strongest argument
   against the pattern here, and it is why dependency injection is named in the conclusion as
   the change to make if the system grows.
3. **Locking has a cost.** The double-checked lock is cheap once the instance exists, but it is
   not free, and the `lock` is easy to get wrong if the pattern is copied carelessly.
4. **The lifecycle is the process's.** There is no way to close and reopen the manager. That is
   correct for a desktop application and would be wrong for a long-running server process.

---

## 6.2 Pattern 2 — Observer: `CourseRequest` and its listeners

### (a) The design problem — 4 marks

When an adviser approves or rejects a student's course plan, three separate things have to
happen as a consequence of that one change:

1. **The student must be told.** The brief requires the student to see the adviser's decision
   and remark. In SCAIS this is a row in the `Notifications` table that the student's messages
   screen reads.
2. **An audit line must be written.** A record of who decided what and when is needed for
   academic governance, and it is deliberately kept outside the database so that it survives
   even if the database is reset.
3. **The student's record must be updated.** Each eligible course in an approved plan becomes a
   *pending* enrolment.

The obvious implementation is for `CourseRequest.Approve()` to call a notification service and
an audit service directly. That implementation has three defects:

- **It couples the domain to the infrastructure.** `CourseRequest` models *a course plan*. If
  it calls a notification service, it must know that a `Notifications` table exists, and that
  an audit log is a file. A domain class that models a plan should know neither.
- **It breaks the open/closed principle.** If the institute later decides the registrar must
  also be e-mailed, `CourseRequest.Approve()` has to be edited. A change in notification
  requirements would force a change to the class that guards the state machine of a plan.
- **It makes the class untestable in isolation.** Approving a plan in a unit test would try to
  write to a database and to a file.

**The problem, stated plainly: one state change must notify an unknown, growing set of
interested parties, without the object that changed knowing who they are.**

### (b) Why this pattern, and what was rejected — 4 marks

Observer models exactly this situation. The pattern's definition — *"a one-to-many dependency
between objects so that when one object changes state, all its dependents are notified
automatically"* — is a precise description of the requirement, which is why it was selected
rather than adapted.

Alternatives considered and rejected:

| Alternative | Why it was rejected |
|---|---|
| **Direct method calls from `Approve()`** | Couples the domain class to the notification table and to file logging, and requires an edit to `CourseRequest` for every new reaction. This is the design the pattern exists to avoid |
| **A polling loop in the student's dashboard** | The dashboard would have to ask repeatedly whether anything had changed. It wastes work when nothing has changed, and the student sees the update only after a delay. It also does not address the audit requirement at all |
| **C# `event` / `delegate`** | A legitimate Observer implementation, and in a smaller system the pragmatic choice. Rejected for three reasons: the observers could not be stored and iterated as objects, they could not be unit tested individually, and an `event` does not map visibly onto the GoF structure the rubric asks to see in the UML |
| **A message queue** | Correct for a distributed system, and the eventual destination for the planned web front end. Rejected here because it adds infrastructure to a single-process desktop application where the notification is an in-process concern |
| **Returning a list of actions for the caller to perform** | Pushes the responsibility onto every caller. The student screen and the adviser screen would each have to remember to send the notification, and one of them would eventually forget |

### (c) UML implementation — 4 marks

`[DIAGRAM: pattern-observer — see ../uml/05-design-patterns.md §5.2]`

The structure the marker is looking for:

| Pattern role | SCAIS class | How it is drawn |
|---|---|---|
| Observer (interface) | `IUserObserver` | `«interface»` with `OnRequestStatusChanged(request, newStatus)` |
| Concrete Subject | `CourseRequest` | Holds `-observers List<IUserObserver>`, has `+Attach`, `+Detach`, `-NotifyObservers`, and the three transitions that call it |
| Concrete Observer 1 | `StudentNotificationService` | `«realises»` `IUserObserver`; holds a `NotificationRepository` |
| Concrete Observer 2 | `AuditLogService` | `«realises»` `IUserObserver`; writes to `SCAIS-audit.log` |
| Client | `CourseAdvisingService` | Has the private `AttachObservers(request)` method that registers both observers before a transition |

SCAIS uses the concrete subject only, without a separate abstract `Subject` class. That is the
standard simplification when there is exactly one subject, and introducing an abstract subject
would have added a class with no second implementation.

**The detail that proves it is the pattern:** the diagram has an association from
`CourseRequest` to `IUserObserver`, and **no** association from `CourseRequest` to either
concrete observer. That absence is the decoupling the pattern provides, and it is what a
marker checks.

### (d) Programming implementation — 4 marks

```csharp
// The subject holds the list and announces the change. It never names a concrete observer.
public class CourseRequest
{
    private readonly List<IUserObserver> observers = new List<IUserObserver>();

    public void Attach(IUserObserver observer)
    {
        if (observer == null) { throw new ArgumentNullException("observer"); }
        if (!this.observers.Contains(observer)) { this.observers.Add(observer); }
    }

    public bool Detach(IUserObserver observer)
    {
        return this.observers.Remove(observer);
    }

    public void Approve(string remark)
    {
        this.ChangeReviewOutcome(RequestStatus.Approved, remark);
    }

    private void ChangeReviewOutcome(RequestStatus outcome, string remark)
    {
        if (string.IsNullOrEmpty(remark) || remark.Trim().Length == 0)
        {
            throw new ArgumentException("A review must include a remark for the student.", "remark");
        }

        if (this.status != RequestStatus.Submitted)
        {
            throw new InvalidOperationException(
                "Only a submitted plan can be reviewed. The current state is " + this.status + ".");
        }

        this.status = outcome;
        this.adviserRemark = remark.Trim();
        this.NotifyObservers();
    }

    private void NotifyObservers()
    {
        List<IUserObserver> snapshot = new List<IUserObserver>(this.observers);

        for (int index = 0; index < snapshot.Count; index++)
        {
            try
            {
                snapshot[index].OnRequestStatusChanged(this, this.status);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Observer " + snapshot[index].GetType().Name + " failed: " + exception.Message);
            }
        }
    }
}
```

`[CODE: Domain/CourseRequest.cs — Attach, Detach, ChangeReviewOutcome and NotifyObservers]`

Two details in that code are deliberate and worth pointing out:

- **The list is copied before iterating.** An observer that detached itself while being
  notified would otherwise cause an `InvalidOperationException` on the collection being
  modified during enumeration.
- **Each call is wrapped in `try`/`catch`.** The state change has *already happened* by the
  time the observers are told. A failing observer must not be able to undo an approval or to
  stop the other observers from being told. This is the limitation identified in (f), handled
  rather than ignored.

The two observers are independent of each other:

```csharp
public class StudentNotificationService : IUserObserver
{
    public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
    {
        string message = BuildMessage(request, newStatus);
        int userId = new UserRepository().GetUserIdForStudent(request.StudentId);

        if (userId <= 0) { return; }

        this.repository.Add(new Notification(userId, message));
    }
}

public class AuditLogService : IUserObserver
{
    public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
    {
        string line = string.Format(CultureInfo.InvariantCulture,
            "{0:yyyy-MM-dd HH:mm:ss} | request {1} | student {2} | {3} | courses {4} | credits {5}",
            DateTime.Now, request.RequestId, request.StudentId, newStatus,
            request.GetCourseCount(), request.GetTotalCreditHours());

        Trace.WriteLine(line);
        WriteToFile(line);
    }
}
```

`[CODE: Observers/StudentNotificationService.cs and Observers/AuditLogService.cs]`

And the registration happens in one place, in the service:

```csharp
private void AttachObservers(CourseRequest request)
{
    request.Attach(new StudentNotificationService());
    request.Attach(new AuditLogService());
}
```

`[CODE: Business/CourseAdvisingService.cs — AttachObservers]`

**That single method is the entire coupling between the workflow and the notification
mechanism.** Adding a third reaction is one more line here and one new class.

### (e) Participants and interactions — 2 marks

| Participant | Role | What it does |
|---|---|---|
| `IUserObserver` | Observer contract | Declares `OnRequestStatusChanged(request, newStatus)`. Both observers implement it; neither knows the other exists |
| `CourseRequest` | Concrete Subject | Holds `List<IUserObserver>`, guards the state transitions, and calls `NotifyObservers()` on each one |
| `StudentNotificationService` | Concrete Observer | Builds the sentence the student reads, resolves the student's `UserId`, and inserts a `Notifications` row |
| `AuditLogService` | Concrete Observer | Formats one line, appends it to `SCAIS-audit.log`, and emits it to the trace listener |
| `CourseAdvisingService` | Client | Attaches the observers before a transition and creates the pending enrolments after an approval |
| `NotificationRepository` | Collaborator | The only class that knows the `Notifications` table exists |

**Interaction, in one sentence:** the adviser presses Approve, which calls
`CourseAdvisingService.ApproveRequest`, which attaches the observers and then calls
`CourseRequest.Approve(remark)`; the request validates the transition, changes its own state
and calls `NotifyObservers()`, which calls `OnRequestStatusChanged` on each observer in turn,
so the student's message is written and the audit line is appended — and the request has no
idea that either of those things happened.

The full sequence is drawn as **SD-3** in
[`../uml/03-sequence-diagrams.md`](../uml/03-sequence-diagrams.md).

### (f) Benefits and limitations — 2 marks

**Benefits**

1. **The subject does not know its observers.** `CourseRequest` depends on `IUserObserver` and
   on nothing else. It has no reference to the notification table or to file logging, so it can
   be read and reasoned about without either.
2. **New reactions need no change to existing code.** Adding an e-mail observer, or a registrar
   notification, is one new class and one `Attach` line. `CourseRequest`,
   `CourseAdvisingService.ApproveRequest` and the adviser's screen are all untouched. This is
   the open/closed principle demonstrated rather than asserted.
3. **Observers can be added and removed at runtime.** `Attach` and `Detach` are public, and
   `GetObserverCount()` exists so the behaviour can be verified.
4. **Each observer has one job.** `StudentNotificationService` writes a message;
   `AuditLogService` writes a log line. Neither contains any logic about approvals.
5. **It made the audit requirement cheap.** The audit trail was added as a second observer
   without modifying any existing class, which is the clearest evidence that the pattern is
   doing real work here rather than being decorative.

**Limitations**

1. **Observers are notified in an arbitrary order.** There is no defined order between
   `StudentNotificationService` and `AuditLogService`. In SCAIS that does not matter, because
   neither depends on the other having run. It would matter immediately if one observer
   depended on another's side effect, and the pattern offers no mechanism to express that.
2. **A failing observer can interrupt the others.** The pattern itself provides no error
   isolation. This is handled in `NotifyObservers()` with a `try`/`catch` around each call, but
   that is a deliberate addition to the pattern rather than part of it, and a developer who
   omitted it would introduce a defect in which one broken observer silently stops the others.
3. **Failure is silent.** Because the loop swallows exceptions, a persistently failing observer
   produces only a debug message. Nothing tells the user and nothing appears in the audit log.
   A production system would need a real error channel.
4. **Forgetting to `Detach` leaks.** The subject holds a strong reference to every observer
   attached to it. A screen that attaches an observer and does not detach it keeps that
   observer, and everything it references, alive for as long as the subject lives. SCAIS avoids
   this by attaching only inside `CourseAdvisingService`, for the duration of one operation,
   rather than attaching from a long-lived screen.
5. **The call chain is indirect.** Debugging is harder than with a direct call, because the code
   that changes the state does not show what happens next. The sequence diagram exists partly
   to make that chain visible.
6. **It is more machinery than the problem strictly needs.** Two interfaces, two observers and
   an attach method is more code than two direct method calls. The justification is that the
   second observer was genuinely required and a third is anticipated.

---

## 6.3 Pattern 3 — Strategy: the eligibility rules

### (a) The design problem — 4 marks

Course eligibility in SCAIS is not one rule. A student may not take a subject unless **all** of
the following hold:

1. They have already **passed** every prerequisite of that subject.
2. Any **corequisite** of that subject has been passed or is being taken in the same semester.
3. Adding the subject would not take them past the **credit ceiling** for the semester.
4. They have not **already taken or completed** the subject.
5. The subject belongs to **their specialization**, or is a core subject shared by every track.

The straightforward implementation is a single `EligibilityChecker.Validate(student, course)`
containing five blocks of nested `if`/`else`. That implementation has three problems:

- **It violates single responsibility.** One class would contain the prerequisite algorithm,
  the corequisite algorithm, the credit arithmetic, the duplicate check and the track check.
  Five unrelated reasons to change would live in one method.
- **The administrator's requirement becomes a code change.** The business case requires the
  administrator to maintain the rules of the system. With a single method, adding a sixth rule
  — a minimum GPA, say, or a rule that only final-year students may take a subject — would mean
  editing and redeploying the application.
- **It cannot be tested rule by rule.** Testing that the credit ceiling works would require
  constructing a student and a course that also satisfy, or deliberately fail, the other four
  rules. The tests would become coupled to each other.

**The problem, stated plainly: the eligibility algorithm varies independently of the class that
uses it, the set of rules must be extensible without editing the class that runs them, and each
rule must be testable on its own.**

### (b) Why this pattern, and what was rejected — 4 marks

Strategy was selected because each academic rule is a self-contained algorithm behind a common
interface, and the validator is the context that runs them. The pattern's structure matches the
requirement exactly: the context holds a collection of interchangeable algorithms and knows
nothing about which ones it has.

Alternatives considered and rejected:

| Alternative | Why it was rejected |
|---|---|
| **A single method with a `switch` on rule type** | Every new rule edits the method, so the class grows without bound and the open/closed principle is broken. This is the design the pattern replaces |
| **Inheritance — a different validator per student type** | The rules differ by **course and semester**, not by the type of student. A first-year student and a final-year student are checked by the same rules. Inheritance would have been the wrong axis of variation, which is the reason `EligibilityValidator` has no subclasses |
| **The rules implemented as stored procedures in the database** | Moves the rule logic out of the object model, where the brief requires the OO design to be demonstrated, and makes the rules harder to test. It also removes the rule from the reach of the `Student` object's in-memory record |
| **A configuration file of rule names** | Only moves the `switch` out of the class and into a factory. The algorithms would still have to live in one place |
| **The Composite pattern on its own** | A genuine alternative, and the closest competitor. `EligibilityValidator` is in effect a composite that runs a list of strategies. Composite was rejected as the *primary* description because the requirement is not "treat one rule and a group of rules identically" — it is "swap the algorithm". The two are combined here rather than one being chosen |

### (c) UML implementation — 4 marks

`[DIAGRAM: pattern-strategy — see ../uml/05-design-patterns.md §5.3]`

| Pattern role | SCAIS class | How it is drawn |
|---|---|---|
| Strategy (interface) | `IEligibilityRule` | `«interface»` with `RuleName` and `Evaluate(student, course) : EligibilityResult` |
| Concrete Strategy 1 | `PrerequisiteRule` | Realises the interface; holds a `PrerequisiteRepository` |
| Concrete Strategy 2 | `CorequisiteRule` | Realises the interface; holds a `PrerequisiteRepository` |
| Concrete Strategy 3 | `CreditLimitRule` | Realises the interface; holds a configurable `maximumCredits` |
| Concrete Strategy 4 | `AlreadyCompletedRule` | Realises the interface; holds no dependency at all |
| Concrete Strategy 5 | `SpecializationRule` | Realises the interface; holds no dependency at all |
| Context | `EligibilityValidator` | Holds `-rules List<IEligibilityRule>`, with `+Validate` and `+ValidateCatalogue` |
| Return value | `EligibilityResult` | The verdict and its reason |
| Return value | `CourseEligibility` | A course plus its verdict, for the grid |

**The detail that proves it is the pattern:** the context holds an **aggregation of the
interface**, `1 — 1..*`, and the five rules are attached to the interface by dashed realisation
lines. There is no association from `EligibilityValidator` to any concrete rule.

### (d) Programming implementation — 4 marks

```csharp
// The Strategy contract
public interface IEligibilityRule
{
    string RuleName { get; }
    EligibilityResult Evaluate(Student student, Course course);
}

// One concrete strategy - the rule the business case centres on
public class PrerequisiteRule : IEligibilityRule
{
    private readonly PrerequisiteRepository repository = new PrerequisiteRepository();

    public string RuleName { get { return "Prerequisite"; } }

    public EligibilityResult Evaluate(Student student, Course course)
    {
        List<Prerequisite> rules = this.repository.GetByCourse(course.CourseId);
        List<string> missing = new List<string>();

        foreach (Prerequisite rule in rules)
        {
            // A corequisite is CorequisiteRule's concern, not this rule's.
            if (!rule.IsPrerequisite()) { continue; }

            if (!student.HasCompleted(rule.RequiredCourse.CourseId))
            {
                missing.Add(rule.RequiredCourse.Code);
            }
        }

        if (missing.Count == 0) { return EligibilityResult.Pass(this.RuleName); }

        return EligibilityResult.Fail(this.RuleName,
            "must first complete " + string.Join(", ", missing.ToArray()));
    }
}

// The Context
public class EligibilityValidator
{
    private readonly List<IEligibilityRule> rules;

    public EligibilityValidator() : this(CreateDefaultRules()) { }

    public EligibilityValidator(List<IEligibilityRule> rules) { this.rules = rules; }

    // The single place where the composition of the pipeline is decided.
    public static List<IEligibilityRule> CreateDefaultRules()
    {
        return new List<IEligibilityRule>
        {
            new AlreadyCompletedRule(),   // cheapest first: no database access
            new SpecializationRule(),     // cheapest first: no database access
            new CreditLimitRule(),
            new PrerequisiteRule(),
            new CorequisiteRule()
        };
    }

    public EligibilityResult Validate(Student student, Course course)
    {
        List<string> failures = new List<string>();

        for (int index = 0; index < this.rules.Count; index++)
        {
            IEligibilityRule rule = this.rules[index];
            EligibilityResult result = rule.Evaluate(student, course);

            if (result == null || !result.IsEligible)
            {
                failures.Add(result == null ? rule.RuleName + ": returned no verdict" : result.Reason);
            }
        }

        if (failures.Count == 0) { return new EligibilityResult(true, "Eligible"); }

        return new EligibilityResult(false,
            EligibilityResult.BlockedPrefix + ": " + string.Join("; ", failures.ToArray()));
    }
}
```

`[CODE: Interfaces/IEligibilityRule.cs, Business/PrerequisiteRule.cs and Business/EligibilityValidator.cs]`

**The line that makes it the Strategy pattern** is
`public static List<IEligibilityRule> CreateDefaultRules()`. It is the only place where the set
of rules is decided. Adding a sixth rule is one new class and one line in that list; nothing
else in the system changes.

Two decisions inside the context are deliberate:

- **Every rule runs, even after one has refused.** Stopping at the first refusal would be
  marginally faster, but the student would then be told about one problem at a time and would
  have to resubmit repeatedly to discover the rest. Reporting every failure at once is what the
  "block or warn" requirement asks for.
- **A rule that returns `null` is treated as a failure, not as a pass.** A broken rule must not
  be able to silently let a student through.

### (e) Participants and interactions — 2 marks

| Participant | Role | What it does |
|---|---|---|
| `IEligibilityRule` | Strategy contract | Declares `RuleName` and `Evaluate(student, course)`. Five unrelated algorithms implement it |
| `PrerequisiteRule` | Concrete Strategy | Reads the subject's prerequisite rules and refuses it if any required subject has not been passed, naming the missing codes |
| `CorequisiteRule` | Concrete Strategy | The same table, the opposite meaning: refuses unless the partner subject has been passed **or** is being taken |
| `CreditLimitRule` | Concrete Strategy | Adds the subject's credit hours to the student's committed load and refuses if the total exceeds the ceiling |
| `AlreadyCompletedRule` | Concrete Strategy | Refuses a subject the student has already passed or is already taking. Needs no repository |
| `SpecializationRule` | Concrete Strategy | Refuses a specialised subject from another track; passes every core subject |
| `EligibilityValidator` | Context | Holds the list, runs every rule, and combines the verdicts into one result |
| `EligibilityResult` | Return value | Carries the verdict and the reason, so a refusal can name the rule that caused it |
| `CourseEligibility` | Return value | A course plus its verdict, which the eligible-courses grid binds to |
| `CourseAdvisingService` | Client | Constructs the validator and asks it for verdicts |

**Interaction, in one sentence:** the student opens the eligible-courses tab, so
`CourseAdvisingService.GetEligibleCourses` asks `EligibilityValidator.ValidateCatalogue` for a
verdict on each course in the catalogue; the validator loops over its five rules, calling
`Evaluate(student, course)` on each, collecting every refusal into one reason string, and
returning an `EligibilityResult` per course — and the validator never learns which five rules it
is holding.

The full sequence is drawn as **SD-2** in
[`../uml/03-sequence-diagrams.md`](../uml/03-sequence-diagrams.md).

### (f) Benefits and limitations — 2 marks

**Benefits**

1. **Each rule is a small, cohesive, independently testable class.** `AlreadyCompletedRule` and
   `SpecializationRule` need no repository at all, so they can be tested with two constructed
   objects and no database.
2. **New rules need no change to existing code.** A minimum-GPA rule, or a "final-year students
   only" rule, is a new class implementing `IEligibilityRule` plus one line in
   `CreateDefaultRules`. `EligibilityValidator` is not edited. This is what makes the
   administrator's "maintain the rules" requirement satisfiable.
3. **The rules can be reordered.** The order is visible in one list, and the cheaper rules that
   need no database access are placed first. That performance decision is explicit rather than
   buried.
4. **The client is unaware of the rules.** `CourseAdvisingService` and both screens ask for a
   verdict and receive one. None of them knows that there are five rules or what they check.
5. **The set of rules can differ per call.** `EligibilityValidator` has a constructor that takes
   a rule list, so a future version could run a different pipeline for a different semester
   without changing the context.

**Limitations**

1. **It increases the number of classes.** Five rule classes, an interface, a context and two
   result types is nine types where one method might have done. For a system with a single rule
   this would be over-engineering, and the pattern should not be applied by default.
2. **The client must know which strategies to construct and in what order.** Some configuration
   knowledge therefore leaks out of the validator into `CreateDefaultRules`. That knowledge had
   to live somewhere, but it is worth naming as a cost.
3. **Cross-rule dependencies must be handled explicitly by the context.** The rules are
   independent by design, so a rule that should only apply if another rule passed — "check the
   credit limit only if the prerequisite is satisfied" — cannot be expressed inside a rule. The
   context would have to handle it, which would begin to defeat the pattern's simplicity.
4. **Adding a rule still needs a rebuild.** The rules are composed in code, so an administrator
   cannot add a rule at runtime. The pattern makes the change cheap for a developer, not
   possible for a user. A database-driven rule set would be needed for that.
5. **Every rule runs even when the answer is already known.** The implementation does not
   short-circuit, which is deliberate for the reason given in (d), but it is a cost: a refused
   course still runs all five rules and reads the curriculum from the database.

---

## 6.4 Pattern summary

| | Singleton | Observer | Strategy |
|---|---|---|---|
| **Pattern type** | Creational | Behavioural | Behavioural |
| **Design problem** | One object must own the database connection configuration | One state change must notify an unknown, growing set of parties | The eligibility algorithm varies and must be extensible |
| **Key participants** | `DatabaseManager`, `SqlRepositoryBase<T>` | `CourseRequest`, `IUserObserver`, `StudentNotificationService`, `AuditLogService` | `IEligibilityRule`, five rules, `EligibilityValidator` |
| **Where it lives** | `DataAccess/DatabaseManager.cs` | `Domain/CourseRequest.cs`, `Interfaces/IUserObserver.cs`, `Observers/*.cs` | `Interfaces/IEligibilityRule.cs`, `Business/*Rule*.cs`, `Business/EligibilityValidator.cs` |
| **What it made possible** | A single connection configuration, and a database the application creates for itself | An audit trail added as a second observer without editing any existing class | A sixth rule as a new class plus one line, with no change to the validator |
| **Main limitation** | Global state that cannot be substituted in a test | Silent failure if an observer throws, and a leak if `Detach` is forgotten | More classes, and the pipeline is composed in code rather than configured |

### Why three patterns and not one

The brief requires at least two and states that Singleton cannot be the only one. The second
and third were not added to satisfy that rule: each was introduced because a specific problem
appeared, and the table above records what each one made possible. The Observer pattern in
particular earned its place when the audit requirement was added — it was satisfied by one new
class rather than by editing the approval workflow.

---

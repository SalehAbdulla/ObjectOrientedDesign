# 3 — Sequence Diagrams

**Rubric: 4 marks — correct object interaction and messages.**

Four diagrams, one for each operation that has meaningful interaction. Lifeline names are
the **real class names** from the code, which is what the "UML consistency" marks check.

---

## SD-1 — Login and role redirect

The single login screen, the authentication service, the factory that builds the right
account type, and the redirect that follows.

```mermaid
sequenceDiagram
    autonumber
    actor Person as Person signing in
    participant Login as frmLogin
    participant Auth as AuthenticationService
    participant Repo as UserRepository
    participant DB as DatabaseManager
    participant Factory as UserFactory
    participant Session as SessionManager

    Person->>Login: enter username and password
    Person->>Login: click Sign in
    activate Login
    Login->>Auth: Login(username, password)
    activate Auth
    Auth->>Repo: GetByUsername(username)
    activate Repo
    Repo->>DB: Instance.CreateConnection()
    DB-->>Repo: SqlConnection
    Repo->>DB: SELECT account joined to its role table
    DB-->>Repo: one row
    Repo->>Factory: Create(record)
    activate Factory
    Factory-->>Repo: Student or Adviser or Administrator
    deactivate Factory
    Repo-->>Auth: User
    deactivate Repo

    alt credentials are wrong
        Auth-->>Login: LoginResult.Failure
        Login-->>Person: show the message, keep the form open
    else account is deactivated
        Auth-->>Login: LoginResult.Failure
        Login-->>Person: show the message
    else credentials are correct
        Auth->>Auth: user.Authenticate(password)
        Auth->>Session: SignIn(user)
        Auth-->>Login: LoginResult.Success(user)
        Login->>Login: user.GetProfileSummary()
        Login->>Login: user.GetDashboardTitle()
        Login-->>Person: welcome message naming the dashboard
        Login-->>Person: DialogResult.OK
    end
    deactivate Auth
    Login-->>Person: form closes
    deactivate Login

    participant Program
    participant Factory2 as DashboardFactory
    Program->>Factory2: Create(authenticatedUser)
    activate Factory2
    Factory2-->>Program: frmStudentDashboard or frmAdviserDashboard or frmAdminDashboard
    deactivate Factory2
    Program-->>Person: role specific dashboard opens
```

**The polymorphic point.** `GetProfileSummary()` and `GetDashboardTitle()` are called on a
`User` reference. The same two calls produce a student summary, an adviser summary or an
administrator summary depending only on the object that `UserFactory` built.

---

## SD-2 — Student views eligible courses and submits a plan

This diagram carries the central requirement of the business case: eligibility is
validated before a course can be taken, and the student is told which rule refused them.

```mermaid
sequenceDiagram
    autonumber
    actor Student as Student
    participant Record as frmMyAcademicRecord
    participant Advising as CourseAdvisingService
    participant Validator as EligibilityValidator
    participant Rules as IEligibilityRule
    participant EnrolRepo as EnrollmentRepository
    participant CourseRepo as CourseRepository
    participant ReqRepo as CourseRequestRepository

    Student->>Record: open the Eligible courses tab
    activate Record
    Record->>EnrolRepo: GetByStudent(studentId)
    EnrolRepo-->>Record: the full academic record
    Record->>Record: student.LoadAcademicRecord(record)
    Record->>Advising: GetEligibleCourses(student)
    activate Advising
    Advising->>CourseRepo: GetForSpecialization(track)
    CourseRepo-->>Advising: core subjects plus the track catalogue
    Advising->>Validator: ValidateCatalogue(student, courses)
    activate Validator

    loop for each course in the catalogue
        Validator->>Rules: Evaluate(student, course)
        activate Rules
        Rules->>Rules: HasCompleted, HasTakenOrIsTaking, credit total
        Rules-->>Validator: EligibilityResult
        deactivate Rules
    end

    Validator-->>Advising: List of CourseEligibility
    deactivate Validator
    Advising-->>Record: courses with verdicts
    deactivate Advising
    Record-->>Student: grid showing Eligible or Blocked with the reason

    Student->>Record: select a course, click Add selected to plan
    Record->>Advising: AddCourseToRequest(draft, student, course)
    activate Advising
    Advising->>Validator: Validate(student, course)
    Validator-->>Advising: EligibilityResult
    Advising->>Advising: request.AddCourse(course, result.Reason)
    Advising-->>Record: EligibilityResult
    deactivate Advising

    alt the course is blocked
        Record-->>Student: warn, naming the missing subject
    else the course is eligible
        Record-->>Student: added to the plan
    end

    Student->>Record: click Submit plan for review
    Record->>Advising: SubmitRequest(draft)
    activate Advising
    Advising->>Advising: AttachObservers(request)
    Note over Advising: the request now holds a list of IUserObserver
    Advising->>ReqRepo: Add(request)
    activate ReqRepo
    ReqRepo->>ReqRepo: INSERT the plan, then each line, in one transaction
    ReqRepo-->>Advising: saved
    deactivate ReqRepo
    Advising-->>Record: true
    deactivate Advising
    Record-->>Student: plan submitted
    deactivate Record
```

---

## SD-3 — Adviser approves a plan, and the Observer pattern fires

This is the diagram that demonstrates the Observer pattern. The request changes state and
announces it; two observers react, and neither the request nor the approval screen knows
that either of them exists.

```mermaid
sequenceDiagram
    autonumber
    actor Adviser as Adviser
    participant Review as frmCourseRequestReview
    participant Advising as CourseAdvisingService
    participant Request as CourseRequest
    participant Notify as StudentNotificationService
    participant Audit as AuditLogService
    participant NotifyRepo as NotificationRepository
    participant EnrolRepo as EnrollmentRepository
    participant ReqRepo as CourseRequestRepository

    Adviser->>Review: open Review submitted course plans
    activate Review
    Review->>Advising: GetSubmittedRequestsForAdviser(adviserId)
    Advising->>ReqRepo: GetSubmittedForAdviser(adviserId)
    ReqRepo-->>Advising: plans with their lines
    Advising-->>Review: the queue
    Review-->>Adviser: grid of submitted plans

    Adviser->>Review: select a plan, type a remark, click Approve plan
    Review->>Advising: ApproveRequest(request, remark)
    activate Advising

    Advising->>Advising: AttachObservers(request)
    Advising->>Request: Attach(StudentNotificationService)
    Advising->>Request: Attach(AuditLogService)

    Advising->>Request: Approve(remark)
    activate Request
    Request->>Request: guard: a remark is required
    Request->>Request: guard: only a submitted plan can be reviewed
    Request->>Request: status becomes Approved, adviserRemark is stored

    Request->>Request: NotifyObservers()
    Note over Request: the request does not know who is listening

    Request->>Notify: OnRequestStatusChanged(this, Approved)
    activate Notify
    Notify->>NotifyRepo: GetUserIdForStudent(studentId)
    NotifyRepo-->>Notify: userId
    Notify->>NotifyRepo: Add(Notification(userId, message))
    NotifyRepo-->>Notify: saved
    deactivate Notify

    Request->>Audit: OnRequestStatusChanged(this, Approved)
    activate Audit
    Audit->>Audit: append a line to SCAIS-audit.log
    deactivate Audit

    Request-->>Advising: the state has changed
    deactivate Request

    Advising->>ReqRepo: Update(request)
    ReqRepo-->>Advising: saved
    Advising->>Advising: CreateEnrollmentsForApprovedPlan(request)
    Note over Advising: one Pending enrolment per eligible course
    Advising->>EnrolRepo: Add(enrollment)
    EnrolRepo-->>Advising: saved

    Advising-->>Review: true
    deactivate Advising
    Review->>Advising: GetSubmittedRequestsForAdviser(adviserId)
    Advising-->>Review: the refreshed queue
    Review-->>Adviser: the plan has left the queue
    deactivate Review
```

**Why this is the Observer pattern and not just a method call.** `CourseRequest` never
names `StudentNotificationService` or `AuditLogService`. It holds `List<IUserObserver>`
and calls the interface. Adding an e-mail observer would be one new class and one `Attach`
line, with no change to `CourseRequest` and no change to this screen.

---

## SD-4 — Administrator defines a prerequisite rule

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Administrator
    participant Catalog as frmCourseCatalog
    participant CourseRepo as CourseRepository
    participant PrereqRepo as PrerequisiteRepository
    participant Rule as Prerequisite
    participant DB as DatabaseManager

    Admin->>Catalog: open Maintain prerequisite rules
    activate Catalog
    Catalog->>CourseRepo: GetAll()
    activate CourseRepo
    CourseRepo->>DB: Instance.CreateConnection()
    DB-->>CourseRepo: SqlConnection
    CourseRepo-->>Catalog: the catalogue
    deactivate CourseRepo
    Catalog-->>Admin: grids and the course selectors

    Admin->>Catalog: choose IT240, choose IT106, choose Prerequisite, click Add rule
    Catalog->>Catalog: guard: a course cannot require itself
    Catalog->>Rule: new Prerequisite(0, IT240, IT106, Prerequisite)
    activate Rule
    Rule->>Rule: validate both courses are present and are not the same
    Rule-->>Catalog: the rule
    deactivate Rule

    Catalog->>PrereqRepo: Add(rule)
    activate PrereqRepo
    PrereqRepo->>DB: INSERT Prerequisites using parameters
    DB-->>PrereqRepo: the new identity
    PrereqRepo-->>Catalog: true
    deactivate PrereqRepo

    Catalog->>PrereqRepo: GetAll()
    PrereqRepo-->>Catalog: the refreshed rules
    Catalog-->>Admin: rule list refreshed and the rule confirmed
    deactivate Catalog
```

---

## Consistency notes

These diagrams are what the "UML consistency" marks look at, so:

| Consistency requirement | How it is met |
|---|---|
| Lifeline names match the class diagram | Every participant above is a class or an interface from the class diagram |
| Messages match real operations | Every message is a method that exists with that name and those parameters in the code |
| Actors match the use case diagram | The person signing in, the Student, the Adviser and the Administrator are the same actors |
| The sequences match the use cases | SD-1 → UC-01, UC-02, UC-20. SD-2 → UC-09, UC-10, UC-18. SD-3 → UC-15, UC-19. SD-4 → UC-05 |

---

## EA redraw checklist

- [ ] Four sequence diagrams, one per section above
- [ ] Lifelines drawn as named rectangles using the `:ClassName` form
- [ ] **Activation bars** on every lifeline that is executing
- [ ] Messages as **solid** arrows with a filled head, ordered top to bottom
- [ ] Returns as **dashed** arrows with an open head
- [ ] `alt` / `else` fragments on SD-1 (three login outcomes) and SD-2 (blocked or eligible)
- [ ] A `loop` fragment on SD-2 for the rule loop over the catalogue
- [ ] A `Note` on SD-3 where the Observer pattern fires
- [ ] Numbering is optional, but it makes the report easier to follow

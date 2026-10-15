# 01 — Master Plan for Full Marks (SCAIS, IT7006 OOD Project)

> **Purpose of this document.** It converts the brief
> (`IT7006 Project Description.pdf`) and the marking rubric
> (`Marking-Rubric-for-IT7006-OOD-project.pdf`) into an explicit, checkable list
> of *artefacts* and *evidence*. If every box below is ticked with real,
> team-authored work, the project targets 95%+ (A+).

---

## 1. The marking principle you must design against

The rubric is unusually explicit about one thing:

> *"We distinguish between **using** an OO feature and **demonstrating** OO competency."*

Every artefact therefore needs **three** things, not one:

| Using the feature | Demonstrating competency |
|---|---|
| A parent class with two children | Explain **why** the is-a relationship is genuine, what lives in the parent, what is specialised, and show **polymorphic use** |
| "We used Singleton" | Identify the **design problem**, justify the choice, show the UML, show the enforced single instance, and discuss **benefits + limitations** |

**Consequence for the team:** the report must contain a *justification sentence
for every design decision*, and every diagram must be traceable to a requirement.
A "design rationale" paragraph is worth more than an extra class.

---

## 2. Rubric → deliverable → evidence map

| # | Rubric section | Marks | Concrete deliverable | Where the marker finds the evidence |
|---|---|---|---|---|
| 1 | **Project Overview** | **10** | Report §1 (Background, Problem statement, Objectives, Target users, Main functions) | Report pages 1–3; demo video opening |
| 2 | **UML Design** | **25** | EA project: Use Case (4), Class (15), Sequence (4), consistency (2) | `ea/SCAIS.eapx`, exported PNGs in `docs/uml/` |
| 3 | **Object-Oriented Concepts** | **25** | Encapsulation 5 · Abstraction 4 · Inheritance 4 · Polymorphism 5 · Interface 3 · OO quality 4 | Report §3 with **code screenshots** proving each claim |
| 4 | **Design Pattern (ERD)** | **20** | Problem 4 · Selection 4 · UML 4 · Code 4 · Explanation 2 · Benefits/limits 2 | Report §6 + EA pattern diagrams + code |
| 5 | **User Interface & System Implementation** | **10** | UI functionality 3 · usability/consistency 2 · integration with OO classes 3 · overall system 2 | Working app; screenshots; demo video |
| 6 | **Data Layer & Database** | **10** | DB design 3 · data-layer impl 3 · CRUD 2 · integration 2 | ERD + `.mdf` + repository classes |

**Total = 100.**

### 2.1 The "visual marking" warning

> *"For the implementation part marking will be visual. If your project is not
> running a feature, your mark for that feature will be 0."*

So: **a feature that is designed but not clickable scores zero.** Build the
narrow end-to-end vertical slice early (login → role dashboard → one full
course-request approval cycle) and only then add breadth.

---

## 3. Functional scope to implement (traceability matrix)

Every use case below must appear in **all four** artefacts: use-case diagram,
class diagram, sequence diagram (where it has interaction), and the running app.

| ID | Actor | Use case | Priority | Sequence diagram? |
|---|---|---|---|---|
| UC-01 | All | Log in through a **single** login screen | Must | **SD-1** |
| UC-02 | System | Redirect user to role-specific dashboard | Must | SD-1 |
| UC-03 | Admin | Manage user accounts (create/update/deactivate advisers & students) | Must | — |
| UC-04 | Admin | Maintain course catalog (add/update course, credits, specialization) | Must | SD-4 |
| UC-05 | Admin | Define prerequisite / corequisite rules | Must | SD-4 |
| UC-06 | Admin | Assign advisees to advisers | Must | — |
| UC-07 | Admin | Define course offerings per semester | Should | — |
| UC-08 | Student | View academic record + course status (completed / in-progress / pending) | Must | — |
| UC-09 | Student | View eligible courses for the coming semester | Must | SD-2 |
| UC-10 | Student | Submit preferred courses for adviser review | Must | SD-2 |
| UC-11 | Student | View approved courses and adviser remarks | Must | SD-3 |
| UC-12 | Adviser | View assigned advisees | Must | — |
| UC-13 | Adviser | Access advisee academic history + specialization | Must | — |
| UC-14 | Adviser | Recommend courses based on eligibility | Must | SD-2 |
| UC-15 | Adviser | Approve / modify a course plan submitted by an advisee | Must | **SD-3** |
| UC-16 | Adviser | Generate course progression report | Must | — |
| UC-17 | Admin | Generate prerequisite-compliance report | Should | — |
| UC-18 | System | Validate prerequisites & corequisites before enrolment | Must | SD-2 |

## 4. Design decisions that earn the "quality" marks

These are the specific choices that separate a 70% project from a 95% project.

### 4.1 Class diagram (15 marks — the single biggest item)

| Marker looks for | How SCAIS satisfies it |
|---|---|
| Appropriate identification of classes | Domain-driven: `Student`, `Adviser`, `Course`, `Prerequisite`, `Enrollment`, `CourseRequest` — no "Manager" god-classes |
| Appropriate responsibilities | `Student` knows *its own* record; `EligibilityValidator` decides eligibility; `CourseRequest` owns *its own* state transitions |
| Correct attributes & operations | Every operation returns something the UI needs; no `void DoStuff()` |
| Appropriate visibility | `-` private fields, `+` public properties/methods, `#` protected hooks |
| Correct multiplicity | `Student 1..* — * Course` via `Enrollment`; `Adviser 1 — * Student`; `Course * — * Course` via `Prerequisite` |
| Association / Aggregation / Composition | **Composition:** `CourseRequest ◆— CourseRequestLine` (lines cannot outlive the request). **Aggregation:** `Adviser ◇— Student` (student exists without the adviser). **Association:** `Enrollment — Course` |
| Inheritance | `User` (abstract) → `Student`, `Adviser`, `Administrator` |
| Interface implementation | `IRepository<T>`, `IEligibilityRule`, `IUserObserver` |
| Avoid unnecessary relationships | Justify in the report why we did **not** relate every class to every other |

**Mandatory:** state the multiplicity on **every** association. Missing
multiplicities is the most common cause of lost class-diagram marks.

### 4.2 OO concepts (25 marks) — the exact evidence to embed

| Concept | Marks | SCAIS evidence | Report must show |
|---|---|---|---|
| Encapsulation | 5 | `User.PasswordHash` is `private`; login only through `Authenticate()`; `Enrollment.Status` only changes via `MarkCompleted()` / `MarkInProgress()`; `Student.GPA` is a read-only computed property | Code screenshot + explanation of *why* direct field access is prevented |
| Abstraction | 4 | `User` is abstract and exposes `GetDashboardTitle()` without knowing how each role renders it; `EligibilityValidator` hides the rule pipeline behind `Validate()` | Before/after: what is exposed vs hidden |
| Inheritance | 4 | `User` → `Student` / `Adviser` / `Administrator`. Common: `UserId`, `Username`, `FullName`, `IsActive`, `Authenticate()`. Specialised: `Student.SpecializationId`, `Adviser.AssignAdvisee()` | Explicit justification of the **is-a** test: *"an Adviser **is a** User"* |
| Polymorphism | 5 | `User.GetDashboardTitle()` and `User.GetProfileSummary()` overridden by all three children; the login form holds only a `User` reference and calls the same method | Show **one** call site producing **three** behaviours (login redirect) |
| Interface | 3 | `IEligibilityRule` implemented by `PrerequisiteRule`, `CorequisiteRule`, `CreditLimitRule`; `IRepository<T>` implemented by each repository | Explain interface = *contract*; the validator depends on the abstraction, not concretes (DIP) |
| OO design quality | 4 | High cohesion (each rule class validates exactly one thing), low coupling (UI talks to services, services talk to interfaces), reusable repositories | A short "responsibility assignment" table |

### 4.3 Design patterns (20 marks)

Three patterns, because the brief says *"at least 2 … singleton cannot be the only
design pattern."* Full detail in [`04-design-patterns.md`](04-design-patterns.md).

1. **Singleton** — `DatabaseManager`: exactly one object should coordinate the
   LocalDB connection, because the app is single-user desktop and multiple open
   connections caused locking/consistency issues.
2. **Observer** — `CourseRequest` (subject) notifies `IUserObserver` listeners
   (`StudentNotificationService`, `AuditLogService`) when status changes, so
   approval/rejection notifications are decoupled from the approval logic.
3. **Strategy** — `IEligibilityRule` family, selected/composed at runtime by
   `EligibilityValidator`, so new academic rules are added without editing the
   validator.

Each pattern needs **all six** rubric sub-items: problem (4), selection (4),
UML (4), code (4), participants & interaction (2), benefits **and limitations** (2).
Write the *limitations* paragraph — most teams forget it and lose 2 easy marks.

| UC-19 | System | Notify student when a request is approved/rejected | Should | **SD-3** |

**Counts to quote in the report:** 3 actors, 19 use cases, 4+ sequence diagrams,
~18 classes, 13 tables, 3 design patterns, 2 reports.

---



### 4.4 Database (10 marks)

- 3NF, 13 tables, PK/FK on every relationship, documented in an ERD.
- **≥15 mock records in every table** (explicit requirement).
- A real data layer: `IRepository<T>` + concrete repositories using ADO.NET
  (`SqlConnection` / `SqlCommand` / `SqlDataReader`), parameterised queries only.
- CRUD demonstrably exercised from the UI (Add course, Edit user, Delete request, View report).

### 4.5 UI (10 marks)

- **One** login form (`frmLogin`) → factory decides role → redirects.
- Consistent visual language: same fonts, same button order, same message box style,
  data grids for lists, validation before save.
- Every screen's data comes from a repository class — **no hard-coded arrays in the UI.**

---

## 5. Eight-week execution plan

| Week | Milestone | Owner(s) | Exit criteria |
|---|---|---|---|
| 1 | Requirements frozen; UC list + UC diagram | Lead Integrator + all | `docs/01` §3 signed off; UC diagram in EA |
| 2 | Class diagram v1 + ERD v1 | Designer A + Designer B | Every association has multiplicity; ERD in 3NF |
| 3 | LocalDB schema + seed script (≥15 rows/table) | Data owner | DB attaches in VS; seed runs clean |
| 4 | Data layer: `IRepository<T>` + all repositories | Data owner | CRUD verified against seeded DB |
| 5 | Domain layer: `User` hierarchy, `Course`, `Prerequisite`, `EligibilityValidator` + patterns | OO owner | Patterns compile and are wired |
| 6 | UI vertical slice: login → dashboards → course request → approval | UI owner | End-to-end demo works |
| 7 | Reports (≥2) + admin CRUD screens + polish | UI owner + Data owner | 2 reports export/print |
| 8 | Sequence diagrams, consistency pass, report, demo video, package | Recorder + all | Zip `S1G1` produced; §6 checklist complete |

**Progress check (Week 10, optional):** bring the design document, the schedule /
work-distribution plan, and a **working model**.

---

## 6. Final evidence checklist (tick before packaging)

### Documents
- [ ] Report follows `Report Template.pdf` structure exactly (all 7 sections)
- [ ] Work distribution plan included and honest (who did which class/screen)
- [ ] Assessment cover sheet completed and attached

### Enterprise Architect
- [ ] Single `.eapx` containing Use Case, Class, Sequence, ERD, pattern diagrams
- [ ] Use case diagram: 3 actors, all 19 use cases, `<<include>>` for validation & authentication
- [ ] Class diagram: visibility on every member, multiplicity on every association, one composition, one aggregation, one inheritance tree, one interface realisation
- [ ] ≥4 sequence diagrams including login/redirect and request approval
- [ ] UML is **consistent**: class names in sequence diagrams match the class diagram; DB entities match the class diagram's persistent classes
- [ ] ERD: 13 tables, all PK/FK, cardinalities, 3NF noted

### Code
- [ ] Solution builds with **0 errors** on .NET Framework 4.8
- [ ] XML/inline comments on every class and public method
- [ ] Abstract `User` + 3 subclasses actually used polymorphically
- [ ] ≥3 interfaces implemented
- [ ] 3 design patterns visible in code and matching the UML
- [ ] No hard-coded data in forms; all through repositories
- [ ] Parameterised SQL only (no string concatenation)

### Database
- [ ] `.mdf` inside the VS project, attaches via LocalDB
- [ ] ≥15 records in **every** table
- [ ] CRUD demonstrated live

### Demo
- [ ] Video shows: login as each of 3 roles → redirect → eligibility block on a bad prerequisite → request submitted → adviser approves → student sees remark → 2 reports
- [ ] Zip named `S1G1` containing Report + EA file + C# project + video

---

## 7. Risk register

| Risk | Impact | Mitigation |
|---|---|---|
| Feature designed but not running | 0 marks for that feature | Vertical slice by Week 6; freeze scope Week 7 |
| Missing multiplicities / visibility | Up to 15 marks | Peer-review the class diagram against §4.1 |
| Patterns included without justification | Up to 12 marks | Use the template in `04-design-patterns.md` §5 |
| `void` return types that break polymorphism | 5 marks | Design `GetDashboardTitle()`-style contracts first |
| Unequal contribution | Individual marks | Git history per member + work distribution plan |
| LocalDB not attaching on the marker's machine | 10 marks | Ship `.mdf` + `AttachDbFilename` connection string + setup note in README |


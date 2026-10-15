# 7. Conclusion

---

## 7.1 What was developed

**SCAIS — the Smart Course Advising Information System** — a Windows desktop application for
Bahrain Institute of Technology, built as required with C# and Windows Forms on .NET Framework 4.8,
storing its data in SQL Server LocalDB and designed in Sparx Enterprise Architect.

| Deliverable | Quantity |
|---|---|
| Actors | 3 |
| Use cases | 21 |
| Domain classes | 14 |
| Service and data classes | 15 |
| Interfaces | 3 |
| Design patterns | 3 (Singleton, Observer, Strategy) |
| Sequence diagrams | 4 |
| Database tables | 13, in 3NF |
| Seed records | at least 15 in every table; 160 enrolments |
| Screens | 13 |
| Reports | 2 |
| Lines of C# | approximately 10,400 |
| Lines of T-SQL | approximately 770 |

---

## 7.2 How the system meets the requirements

Each requirement from the brief, and where it is satisfied.

| Requirement in the brief | How SCAIS satisfies it |
|---|---|
| Single login screen, role-based redirect | One `frmLogin` for all three roles. `AuthenticationService` returns a `User`; `UserFactory` builds the right subclass from the `Role` column; `DashboardFactory` opens the matching dashboard |
| Role-based access tailored to each role | Each dashboard offers only its role's functions, and the menus are filtered by capability flags rather than by role tests |
| Course validation logic | `EligibilityValidator` runs five `IEligibilityRule` strategies before a subject is accepted |
| Block **or warn** when a prerequisite is missing | The refused course is still added to the plan with the reason recorded against it, so the student is told which subject is missing rather than simply refused |
| Track course status — completed, in progress, pending | `Enrollment` holds one of the three states with no public setter, and `Student` exposes `GetCompletedCourses`, `GetInProgressCourses` and `GetPendingCourses` |
| Adviser: view advisees and their records | `frmAdviseeList` shows the advisees, the selected advisee's record and their eligibility |
| Adviser: recommend courses based on eligibility | The eligibility grid on `frmAdviseeList` is the recommendation |
| Adviser: approve or modify course plans | `frmCourseRequestReview`, with a remark required for either decision |
| Student: view record, eligible courses, submit and view remarks | The three tabs of `frmMyAcademicRecord` |
| Administrator: manage accounts, catalogue, rules, offerings, advisee assignment | `frmUserManagement`, `frmCourseCatalog`, `frmCourseOfferings`, `frmAssignAdvisees` |
| At least two reports | Course progression and prerequisite compliance, both printable and exportable |
| At least two design patterns, not Singleton alone | Singleton, Observer and Strategy |
| ERD in third normal form | 13 tables, with the evidence for 1NF, 2NF and 3NF stated in §5.1 |
| At least 15 records in every table | Verified by query against a live engine; see `docs/07-verification.md` |
| C# with well-commented classes and methods | Every class and public member carries XML documentation explaining **why**, not just what |
| A database file included in the project | The schema and seed scripts are embedded in the assembly and applied on first run; the resulting `.mdf` can be detached into the project, and §5.4 states the reason for the choice |

---

## 7.3 How the OO concepts were applied

| Concept | The one-sentence summary | Marks |
|---|---|---|
| **Encapsulation** | `Enrollment.Status` and `Grade` have no public setter, so an enrolment cannot be completed without a grade; `User` uses `private`, `internal` and `public` deliberately, and the password hash has no public accessor at all | 5 |
| **Abstraction** | `User` is abstract and exposes three role-differing operations instead of a role enum; `EligibilityValidator.Validate(student, course)` hides five rules, their order and their data access behind one signature | 4 |
| **Inheritance** | One genuine is-a hierarchy, `User` ← `Student`, `Adviser`, `Administrator`, where authentication is implemented once; three other candidates were deliberately rejected with reasons | 4 |
| **Polymorphism** | The login form calls `GetProfileSummary()` and `GetDashboardTitle()` on a `User` reference and gets three different answers; `CanApproveCourseRequests` drives whether a menu item exists; the rule loop and the repository loop are the same mechanism at runtime | 5 |
| **Interface** | Three contracts with genuine multiple implementations: `IRepository<T>` for the data layer, `IEligibilityRule` for the rules, `IUserObserver` for notifications | 3 |
| **OO design quality** | Each class has one reason to change; the rules are the most cohesive classes in the project; dependencies point at interfaces; the maintainability consequences of four design choices are stated concretely | 4 |

---

## 7.4 What the design made possible

The clearest evidence that the design is doing work rather than decorating the report is what it
made cheap:

1. **The audit trail was added as one class.** When the requirement to record decisions appeared, it
   was satisfied by a second `IUserObserver` implementation and one `Attach` call. `CourseRequest`
   and the adviser's approval screen were not modified.
2. **Five eligibility rules are independent.** Each can be reasoned about, and two of them —
   `AlreadyCompletedRule` and `SpecializationRule` — need no database at all.
3. **A sixth rule costs one class and one line.** `CreateDefaultRules` is the only place the pipeline
   is defined.
4. **The appearance is defined once.** Twelve screens share `UiTheme`, and three dashboards share
   `DashboardForm`.
5. **Nine repositories share one implementation of the ADO.NET plumbing.** The parameterisation, the
   connection handling, the transaction helpers and the typed readers are written once.

---

## 7.5 Limitations and future work

These are stated plainly rather than hidden. Each is a conscious trade-off, not an oversight.

| Limitation | Consequence | What would fix it |
|---|---|---|
| The password hash uses a fixed salt and a single SHA-256 pass | Not suitable for a production system, where a per-user random salt and a deliberately slow algorithm are required | PBKDF2, bcrypt or Argon2 with a per-user salt, and a password policy |
| `DatabaseManager` is a Singleton | The instance cannot be substituted in a test, so repository tests need a real database | Dependency injection with a composition root, and an `IDatabaseManager` interface |
| `SessionManager` is static | One signed-in user at a time. Correct for a desktop application, wrong for the planned web front end | Hold the session per application instance, or per request on the server |
| Eligibility rules are composed in code | Adding a rule needs a rebuild, so an administrator cannot add one at runtime | Store the active rule set per semester and build the pipeline from it, or make the rule parameters data rather than code |
| The `.mdf` is created by the application rather than shipped | The brief asks for a data file in the project; the trade-off is stated in §5.4 | Detach the created `.mdf` and add it to the project, changing only the connection string |
| Forms are laid out in code | The Visual Studio designer cannot edit them, although the application runs normally | Move each layout into a `.Designer.cs` file |
| Reports print through `PrintDocument` | The layout is functional rather than highly formatted | A reporting component such as ReportViewer with an `.rdlc` definition |
| There is no automated test project | The design is testable — the rules especially — but no tests were written | A test project covering the five rules, the `Enrollment` invariants and the `CourseRequest` state machine. The classes were designed to be testable for exactly this reason |
| No password policy, lockout or audit of sign-in attempts | Authentication is functionally correct but not hardened | Failed-attempt counting, lockout, and sign-in auditing |

### What would be done next, in order

1. **A test project** covering the five eligibility rules and the `Enrollment` invariants. These are
   the classes with the clearest behaviour and the highest consequence if they are wrong, and they
   were written to be testable.
2. **Replace the Singleton with injected dependencies.** This is the change that unlocks the testing
   above, and it is the reason the pattern is named as a limitation rather than a triumph.
3. **Move the rule pipeline into the database**, so the administrator's requirement to maintain the
   rules is fully satisfied without a developer.
4. **The web front end.** The layering already supports it: the services depend on `IRepository<T>`
   and on no user interface type, and the domain model has no reference to Windows Forms. The domain
   classes, the services and the rules could be reused unchanged.
5. **An `.rdlc` reporting component** for the two reports.

---

## 7.6 Closing statement

SCAIS replaces a manual, error-prone advising process with a validated one. The three problems
identified at the start are addressed directly: prerequisite checking is automated and consistent,
ineligible enrolments are blocked before they happen and explained when they do, and a student's
position across completed, in-progress and pending courses is visible in one place.

The system was designed before it was built, and the design was then verified rather than assumed.
The database scripts were executed against a live SQL Server engine and every claim about the data
was checked by query. The application was compiled against the .NET Framework 4.8 reference
assemblies with zero errors and zero warnings, and it was then run: 101 checks were executed against
the compiled application and a real SQL Server LocalDB instance, covering the domain model, the five
eligibility rules, the timetable repository, the seeded logins and the complete submit → review →
approve workflow. That run found two defects that review alone had missed — a student's assigned
adviser was never loaded, so no plan a student submitted ever reached an adviser's queue, and the
dashboard chrome docked in the wrong order so the first menu function was hidden behind the header.
Both are fixed and are recorded in [`../07-verification.md`](../07-verification.md), which also
states plainly what has still not been verified rather than glossing over it.

The object-oriented design is applied because each feature had a reason, not because the rubric
listed the features. Encapsulation protects the invariant that a completed course has a grade.
Inheritance exists because a genuine is-a relationship exists. Polymorphism is what lets one login
screen serve three roles and one validator run five rules. The three design patterns each solved a
problem that appeared in this system, and the Observer pattern in particular earned its place by
making a later requirement cost one class instead of an edit to the workflow.

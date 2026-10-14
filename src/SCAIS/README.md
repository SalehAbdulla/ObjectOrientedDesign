# SCAIS — How to Build and Run

**Smart Course Advising Information System** — the working application.

---

## 1. Prerequisites

| Requirement | Notes |
|---|---|
| Windows | Windows Forms is a Windows-only technology |
| Visual Studio 2019 or 2022 | Any edition, including Community. The **.NET desktop development** workload must be installed |
| .NET Framework 4.8 | Included in Windows 10 1903 and later, and in Visual Studio |
| SQL Server LocalDB | Installed automatically with the .NET desktop development workload in Visual Studio |

---

## 2. Run it

1. Open **`src/SCAIS/SCAIS.sln`** in Visual Studio.
2. Make sure the toolbar shows **Debug** and **Any CPU**.
3. Press **F5**.

On the first run the application creates its own database. You do **not** need to
attach a `.mdf`, run a script, or configure anything: `DatabaseManager.EnsureDatabase()`
runs before the login screen appears and will

1. create the `SCAIS` database if it does not exist,
2. apply the embedded schema (`Database/01_schema.sql`),
3. apply the embedded seed data (`Database/02_seed.sql`).

If the database already exists and has data, nothing is overwritten.

### If the database needs to be reset

In SQL Server Object Explorer, right-click the `SCAIS` database and choose
**Delete**, then run the application again. It will be rebuilt and re-seeded.

---

## 3. Demonstration accounts

All three roles sign in through the same screen.

| Role | Username | Password |
|---|---|---|
| Administrator | `admin` | `Admin@123` |
| Adviser | `adviser01` | `Adviser@123` |
| Student | `student01` | `Student@123` |

Other advisers are `adviser02` … `adviser08` with the same password.
Other students are `student02` … `student30` with the same password.

The credentials are also shown on the login screen, so a marker can sign in
without needing this file.

---

## 4. A demonstration path that shows every feature

1. Sign in as **student02** (`Student@123`). This student is on the **Networking**
   track and has **not** completed Advanced Networking.
2. Open **My academic record and course plans**. The *My record* tab shows courses
   in all three states: completed, in progress and pending.
3. Open the **Eligible courses and new plan** tab. Notice that
   **IT221 Networking and Data Communications 2** is marked *Blocked*, with the
   reason naming the missing prerequisite. This is `PrerequisiteRule` running.
4. Select an eligible course, **Add selected to plan**, then
   **Submit plan for review**. A message is written for the student by the
   notification observer.
5. Sign out, and sign in as **adviser02** (`Adviser@123`).
6. Open **Review submitted course plans**. The plan just submitted is in the queue.
   Type a remark and press **Approve plan**.
7. Check the audit trail: a file named **`SCAIS-audit.log`** appears next to
   `SCAIS.exe` in `bin\Debug\net48\`. It was written by the second observer,
   which the approval screen knows nothing about.
8. Sign out and sign back in as **student02**. The dashboard now reports an unread
   message. Open **View my messages** to read the approval and the remark.
9. Sign out and sign in as **admin** (`Admin@123`) to see the catalogue, the
   prerequisite rules, user accounts, advisee assignment and the two reports.

---

## 5. Where the marks are in the code

| Rubric section | Look at |
|---|---|
| Encapsulation | `Domain/Enrollment.cs` — `Status` and `Grade` have no public setter; `Domain/User.cs` — `internal` password access |
| Abstraction | `Domain/User.cs` (abstract), `Business/EligibilityValidator.cs` |
| Inheritance | `Domain/User.cs` → `Student.cs`, `Adviser.cs`, `Administrator.cs` |
| Polymorphism | `User.GetProfileSummary()` and `GetDashboardTitle()`, called from `UI/frmLogin.cs` and `UI/DashboardForm.cs` |
| Interface | `Interfaces/IRepository.cs`, `IEligibilityRule.cs`, `IUserObserver.cs` |
| Singleton | `DataAccess/DatabaseManager.cs` |
| Observer | `Domain/CourseRequest.cs` (subject), `Interfaces/IUserObserver.cs`, `Observers/*.cs`, wired in `Business/CourseAdvisingService.AttachObservers` |
| Strategy | `Interfaces/IEligibilityRule.cs` and the five rules in `Business/*Rule*.cs` |
| Factory | `Domain/UserFactory.cs`, `UI/DashboardFactory.cs` |
| Data layer | `DataAccess/SqlRepositoryBase.cs` and the eight repositories |
| Database | `Database/01_schema.sql` (13 tables, 3NF), `Database/02_seed.sql` (≥15 rows per table) |
| Reports | `Business/ReportService.cs`, `UI/frmReports.cs` |

---

## 6. Two implementation decisions worth knowing

**The forms are laid out in code, not in designer-generated `.Designer.cs` files.**
Every form builds its controls in a `BuildInterface` method. The reasons are that the
layout is then reviewable and diffable as ordinary C#, and that the whole user
interface can be read without opening a visual designer. The consequence is that
double-clicking a form in Solution Explorer may report that the designer cannot be
shown; the application itself is unaffected. All three dashboards share their chrome
through `UI/DashboardForm.cs` and `UI/UiTheme.cs`, which is what keeps the appearance
consistent.

**The database is created by the application, not by the marker.**
The brief asks for the database to be a local data file included in the project.
Shipping a `.mdf` that only attaches on one machine is fragile, so instead the schema
and seed data are embedded in the assembly and applied automatically. The `.mdf` that
LocalDB creates can be detached and added to the project if a physical file is
required; the connection string in `App.config` is the only thing that would change.

---

## 7. Known limitations

These are stated honestly rather than hidden, and are repeated in the project report:

- The password hash uses a fixed salt. A production system should use a per-user
  random salt and a deliberately slow algorithm such as PBKDF2 or bcrypt.
- Session state is held in a static class, so the application supports one signed-in
  user at a time. This is correct for a desktop application and would have to change
  for the planned web front end.
- Eligibility rules are composed in code rather than configured in the database, so
  adding a rule needs a rebuild. The Strategy pattern means it needs one new class and
  one registration line, not a change to the validator.
- The reports print through `PrintDocument` rather than a reporting component, so the
  layout is functional rather than highly formatted.
- The application has not yet been run. It compiles cleanly against the .NET Framework
  4.8 reference assemblies with zero errors and zero warnings, but Windows Forms cannot
  execute on the machine it was written on, so the first execution happens in Visual
  Studio on Windows.

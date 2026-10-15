# 07 — Verification Evidence

Two independent verification runs were performed while the project was built. Both
are reproducible, and both are worth citing in the report as proof that the
database and the domain model actually work rather than merely looking correct.

---

## 1. Database verification against a real SQL Server engine

`01_schema.sql` and `02_seed.sql` were executed against a real SQL Server engine
(Azure SQL Edge in Docker, T-SQL compatible) and every claim the report makes
about the database was then checked by query.

**Result: ALL CHECKS PASSED.**

```
[1] database SCAIS dropped and recreated from scratch
[2] 01_schema.sql applied (12 batches, no errors)
[2] 02_seed.sql applied (12 batches, no errors)
[3] row counts (the brief requires at least 15 in every table)
    OK    Specializations       15
    OK    Users                 40
    OK    Advisers              15
    OK    Students              30
    OK    Administrators        15
    OK    Courses               30
    OK    Prerequisites         22
    OK    Semesters             15
    OK    CourseOfferings       40
    OK    Enrollments          160
    OK    CourseRequests        20
    OK    CourseRequestLines    45
    OK    Notifications         25
[4] password hash cross-check against an independent SHA-256
    OK    admin
    OK    adviser01
    OK    student01
    OK    all three seeded users returned  3
[5] business rule spot checks
    OK    students blocked from IT211 (IT210 missing)  20
    OK    students eligible for IT211 (IT210 completed)  10
        enrolment status Completed: 100
        enrolment status InProgress: 30
        enrolment status Pending: 30
    OK    all three enrolment statuses present  3
        request status Approved: 6
        request status Draft: 4
        request status Rejected: 4
        request status Submitted: 6
    OK    all four request states present  4
    OK    core courses have a null specialization  10
[6] database constraint checks
        foreign keys: 19, check constraints: 7
    OK    CK_Enrollments_Grade rejects a Completed enrolment with no grade
    OK    CK_Prerequisites_NotSelf rejects a course that requires itself
    OK    deleting a request cascades to its lines (composition)

ALL CHECKS PASSED
```

### What each check proves

| Check | What it demonstrates for the rubric |
|---|---|
| 12 schema batches, 12 seed batches, no errors | The DDL is valid T-SQL and every foreign key resolves in dependency order — the ERD is implementable, not just drawable |
| Every table ≥ 15 rows | The explicit "minimum 15 mock records in every table" requirement is met, and verified rather than assumed |
| Password hash matches an independent SHA-256 | `HASHBYTES('SHA2_256', password + '\|SCAIS')` in the seed script and `PasswordHasher.Hash()` in C# produce the same value, so the seeded accounts can really sign in |
| 20 students blocked / 10 eligible for IT211 | The prerequisite chain in the data actually produces the blocking behaviour the business case requires, and the demo scenario is real |
| All three enrolment statuses present | Completed / In-progress / Pending tracking has data to display |
| All four request states present | The approval workflow has data in every state |
| `CK_Enrollments_Grade` rejects Completed with no grade | The "Completed implies a grade" invariant is enforced by the database, and the same rule is enforced again by `Enrollment.MarkCompleted` in the domain model |
| `CK_Prerequisites_NotSelf` rejects a self-requiring course | Curriculum rules cannot be defined as circular |
| Deleting a request cascades to its lines | The `CourseRequest ◆ CourseRequestLine` **composition** is real in the schema, not only in the diagram |

---

## 2. Compilation and runtime verification

The solution is compiled with the .NET Framework 4.8 reference assemblies, which
means the compiler is checking against the real .NET Framework 4.8 surface area
(the same reference assemblies Visual Studio uses).

- Whole solution, `dotnet build src/SCAIS/SCAIS.sln`: **build succeeded, 0 errors, 0 warnings**

### 2.1 The runtime run

The compiled `SCAIS.exe` was then driven by a harness that lives outside the repository (so the
submitted solution is unchanged) and references the built assembly. **101 checks were run against a
real SQL Server LocalDB 16.0 instance; 0 failed.** The run covered:

| Area | What was exercised |
|---|---|
| `CourseOffering` (UC-07) | the capacity limits, the null guards, `SeatsRemaining`, `HasSpace`, `HasEnrollments` and the display strings |
| `CourseOfferingRepository` | `GetBySemester` (30 offerings in the current term, 10 in the coming one), seats taken counted from real `Enrollments` rows, `Exists`, `Add` / `Update` / `Delete`, and `CopyFrom` including its idempotence |
| Database bootstrap | `DatabaseManager.EnsureDatabase()` created the database from the embedded scripts, and `SplitIntoBatches` produced the 12 + 12 batches §1 records |
| Seeded logins | `student02`, `adviser02` and `admin` all signed in through `AuthenticationService`; a wrong password and an unknown account were refused with the same message |
| The advising workflow | IT221 was refused for `student02` naming IT220; a plan was built, submitted, found in the adviser's queue, approved with a remark, seen by the student, and produced a pending enrolment |
| The Observer pattern | the approval wrote the student's notification **and** the `SCAIS-audit.log` file |
| The user interface | the application was launched and all thirteen screens were captured and inspected: the login screen, the three dashboards, the three tabs of the student's record, the adviser's advisee list and review queue, the catalogue and its rules tab, user management, advisee assignment, both reports, the messages screen and the timetable |

### 2.2 The two defects the run found, and the fixes

| Defect | Symptom | Fix |
|---|---|---|
| A student's assigned adviser was never loaded | `Student.Adviser` was always null, so `CourseAdvisingService.CreateRequest` filed every new plan with `AdviserId = NULL` and **the plan never appeared in any adviser's queue** — the submit → review → approve path could not complete. The student dashboard also always reported "Adviser: not yet assigned" | `UserRepository` now reads the assigned adviser in the same account query (two extra `LEFT JOIN`s) and sets `Student.Adviser` in `MapAccount` |
| The dashboard chrome docked in the wrong order | WinForms resolves docking in reverse z-order, so adding the header first pushed it *down* over the top of the menu: the header appeared below the profile strip and the first menu caption and function were hidden behind it | `DashboardForm.BuildShell` now adds the strips in reverse: menu, status line, profile strip, header |
| The adviser's advisee list opened with its detail grids empty | `ReloadAdvisees` cleared the record and recommendation grids and relied on `SelectionChanged`, which binding a list does not raise, so the adviser's main screen listed four advisees and showed nothing about any of them until a row was clicked | `frmAdviseeList` now selects the first advisee and shows it through the same `ShowDetailsFor` method that the selection handler uses |
| Every refusal read "Blocked: Blocked: ..." | `EligibilityResult.Fail` adds the Blocked prefix and `EligibilityValidator` added it a second time when combining the failures, so the text was doubled on screen and in the eligibility note stored against a requested course | The validator strips the prefix from each part and adds it once, which keeps the `CourseRequestLine.IsEligible` contract intact |

The login screen also clipped the demonstration credentials (a 60-pixel label cap on four lines of
text) and was stretched to the theme's 760x500 minimum although it is a fixed 520x430 dialog; both
are corrected in `frmLogin`.

---

## 3. What is *not* yet verified

Being explicit about the limits of the evidence is better than overclaiming:

| Not verified here | Why | Where it must be verified |
|---|---|---|
| Every screen clicked through by hand | The run exercised sign-in, the student dashboard and the whole service and data workflow; the remaining screens were not clicked through | The Windows virtual machine, by following the demonstration path in [`08-windows-first-run-and-test.md`](08-windows-first-run-and-test.md) |
| The WinForms designer opening each form | Requires Visual Studio | The Windows virtual machine |
| Behaviour on a machine with no SQL Server LocalDB | The run used LocalDB 16.0. The failure path was observed only as `EnsureDatabase` throwing SQL error 52, which is what `Program.PrepareDatabase` reports as *"SCAIS could not prepare its database"* | Any machine without LocalDB |
| The printed output of the two reports | Printing needs a printer or a PDF driver | The Windows virtual machine |

**The procedure for closing these three gaps is
[`08-windows-first-run-and-test.md`](08-windows-first-run-and-test.md)** — the pre-flight
checklist, the build steps, the nine-step demonstration path, and the five most likely
first-run failures with their fixes.

---

## 4. Reproducing the database check

```bash
docker run -d --name scais-sql \
  -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=<a strong password>' \
  -p 14333:1433 mcr.microsoft.com/azure-sql-edge:latest
```

Then apply `src/SCAIS/SCAIS/Database/01_schema.sql` followed by
`src/SCAIS/SCAIS/Database/02_seed.sql`, remembering that `GO` is a batch
separator rather than T-SQL — which is exactly what
`DatabaseManager.SplitIntoBatches` implements in the application.

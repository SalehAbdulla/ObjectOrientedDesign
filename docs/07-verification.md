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

## 2. Compilation verification

The solution is compiled with the .NET Framework 4.8 reference assemblies, which
means the compiler is checking against the real .NET Framework 4.8 surface area
(the same reference assemblies Visual Studio uses).

- Domain and interface layers: **build succeeded, 0 errors, 0 warnings**

This is recorded so that the report can state honestly that the code was
compiled against .NET Framework 4.8 and not merely written.

---

## 3. What is *not* yet verified

Being explicit about the limits of the evidence is better than overclaiming:

| Not verified here | Why | Where it must be verified |
|---|---|---|
| The application running | Windows Forms cannot execute on macOS | The Windows virtual machine, by pressing F5 |
| SQL Server **LocalDB** specifically | LocalDB is a Windows-only feature | The Windows virtual machine; the schema was validated against a T-SQL compatible engine |
| The WinForms designer opening each form | Requires Visual Studio | The Windows virtual machine |

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

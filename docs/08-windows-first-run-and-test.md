# 8 — Windows First Run and Test Guide

**Purpose.** Everything needed to take SCAIS from the repository to a running, screenshotted
application, plus what to do when it fails the first time. Written so that any team member can
follow it without needing the conversation that produced it.

---

## 1. What to pull

```
git pull
```

You want commit **`06e8816`** or later on `main`.

Then open:

```
src/SCAIS/SCAIS.sln
```

---

## 2. Pre-flight checklist

Check these **before** pressing F5. They account for the large majority of first-run failures and
all of them are fixed in the Visual Studio Installer.

| Check | Where to look | If it is missing |
|---|---|---|
| **.NET desktop development** workload | VS Installer → Modify → Workloads | Install it. This is what brings Windows Forms and the .NET Framework 4.8 targeting pack |
| **.NET Framework 4.8 SDK** and **targeting pack** | VS Installer → Individual components | Tick both |
| **SQL Server Express LocalDB** | VS Installer → Individual components | Tick it |
| The LocalDB **instance** exists | Command prompt: `sqllocaldb info` | `sqllocaldb create MSSQLLocalDB` |

If LocalDB is the problem, the application reports it itself with a message box reading
*"SCAIS could not prepare its database"*. That is the application telling you, not a crash.

---

## 3. Build and run

1. Make sure the toolbar shows **Debug** and **Any CPU**.
2. Press **F5**.

On the first run the application creates its own database: `DatabaseManager.EnsureDatabase()`
runs before the login screen appears, creating the `SCAIS` database and applying the embedded
schema and seed data. Nothing has to be attached or configured by hand.

### Sign-in credentials

| Role | Username | Password |
|---|---|---|
| Administrator | `admin` | `Admin@123` |
| Adviser | `adviser01` | `Adviser@123` |
| Student | `student01` | `Student@123` |

Also `adviser02`–`adviser08` and `student02`–`student30`, with the same passwords. The login
screen displays these itself, so a marker never needs this file.

### To reset the database

SQL Server Object Explorer → right-click `SCAIS` → **Delete** → run the application again.

---

## 4. The demonstration path

Run this in order. It exercises every one of the 21 use cases, and it is also what produces the
screenshots the report needs — so **capture as you go** rather than doing a second pass.

| Step | Do this | Screenshot to take |
|---|---|---|
| 1 | Sign in as `student02` / `Student@123` | `frmLogin`, `frmStudentDashboard` |
| 2 | Open **My academic record and course plans** → **My record** tab | `frmMyAcademicRecord` — My record |
| 3 | Switch to the **Eligible courses and new plan** tab. **IT221 Networking and Data Communications 2** should be marked **Blocked**, naming **IT220 Advanced Networking** as the missing prerequisite | `frmMyAcademicRecord` — Eligible courses |
| 4 | Select an eligible course → **Add selected to plan** → **Submit plan for review** | the confirmation dialog |
| 5 | Sign out, sign in as `adviser02` / `Adviser@123` | `frmAdviserDashboard` |
| 6 | Open **Review submitted course plans** — the plan just submitted is in the queue. Type a remark → **Approve plan** | `frmCourseRequestReview` |
| 7 | **Check the audit log.** A file `SCAIS-audit.log` should now exist next to `SCAIS.exe` in `bin\Debug\net48\` | the file in Explorer, and its contents |
| 8 | Sign out, sign back in as `student02`. The dashboard should report an unread message → **View my messages** | `frmMessages` |
| 9 | Sign out, sign in as `admin` / `Admin@123` and visit every menu item, including both reports | `frmAdminDashboard`, `frmUserManagement`, `frmCourseCatalog` (both tabs), `frmCourseOfferings`, `frmAssignAdvisees`, `frmReports` (both reports) |

### Why student02 sees IT221 blocked

Worth being able to explain, because a marker may ask. The chain is in the seed data:

```
student02  completed: IT101, IT102, IT103
IT221 (Networking and Data Communications 2)
  requires IT220 (Advanced Networking)
IT220 is not in student02's completed set
```

So `PrerequisiteRule` reports the failure and the course is shown as **Blocked** rather than
selectable. Nothing about this is hard-coded for student02 — the rule reads the
`Prerequisites` table, which is the point of the pattern.

### Step 7 is the single most valuable piece of evidence in the project

If `SCAIS-audit.log` appears, the **Observer pattern is working end to end**. Nothing on the
adviser's approval screen writes that file: the approval calls
`CourseAdvisingService.ApproveRequest`, which attaches the observers and lets
`CourseRequest.Approve` announce the change, and `AuditLogService` reacts. Capture both the file
and its contents — it is worth a screenshot in §6.2 of the report.

Likewise, the message the student reads in step 8 was written by the **other** observer,
`StudentNotificationService`, which the approval screen also knows nothing about.

---

## 5. Screenshot checklist

The report has a placeholder for each of these.

### Screens

- [ ] `frmLogin`
- [ ] `frmStudentDashboard`
- [ ] `frmAdviserDashboard`
- [ ] `frmAdminDashboard`
- [ ] `frmMyAcademicRecord` — My record tab
- [ ] `frmMyAcademicRecord` — Eligible courses tab
- [ ] `frmMyAcademicRecord` — My requests tab
- [ ] `frmCourseRequestReview`
- [ ] `frmAdviseeList`
- [ ] `frmCourseCatalog` — Courses tab
- [ ] `frmCourseCatalog` — Rules tab
- [ ] `frmCourseOfferings`
- [ ] `frmUserManagement`
- [ ] `frmAssignAdvisees`
- [ ] `frmReports` — progression report
- [ ] `frmReports` — compliance report
- [ ] `frmMessages`
- [ ] `SCAIS-audit.log` (step 7 above)

Capture the **client area only** and resize the window first so nothing is cut off.
`Alt` + `Print Screen` captures the active window on Windows.

### Code screenshots

The report marks each of these `[CODE: ...]`. Each must show the class declaration, the members
the report makes a claim about, and enough context to prove it is real code.

- [ ] `Domain/Enrollment.cs` — the private fields, the get-only properties, `MarkCompleted`
- [ ] `Domain/User.cs` — the private field, the `internal` accessor, `Authenticate`
- [ ] `Domain/Course.cs` — the `CreditHours` setter with its guard
- [ ] `Domain/User.cs` + `Student.cs` + `Adviser.cs` + `Administrator.cs` — the four overrides side by side
- [ ] `UI/DashboardForm.cs` — the single call to `GetProfileSummary`
- [ ] `UI/frmLogin.cs` — the single call producing three different messages
- [ ] `UI/frmAdviserDashboard.cs` — the capability flag driving the menu
- [ ] `Business/EligibilityValidator.cs` — the rule loop and `CreateDefaultRules`
- [ ] `Domain/CourseRequest.cs` — `Attach`, `Detach`, `ChangeReviewOutcome`, `NotifyObservers`
- [ ] `Observers/StudentNotificationService.cs` and `AuditLogService.cs`
- [ ] `Business/CourseAdvisingService.cs` — `AttachObservers`
- [ ] `DataAccess/DatabaseManager.cs` — the sealed class, the private constructor, the accessor
- [ ] `Interfaces/IEligibilityRule.cs`, `IRepository.cs`, `IUserObserver.cs`
- [ ] `Business/PrerequisiteRule.cs`

---

## 6. When it fails — the five most likely failures

| # | Symptom | Cause | Fix |
|---|---|---|---|
| 1 | NuGet restore error on `Microsoft.NETFramework.ReferenceAssemblies` | Offline, or restore blocked | Right-click the project → Manage NuGet Packages → Restore. On Windows this package is **optional** and can be deleted: the real 4.8 reference assemblies are present |
| 2 | `SCAIS could not prepare its database` | LocalDB missing, or the instance has a different name | Check the connection string in `App.config`. Some installs use `(localdb)\v11.0` rather than `(localdb)\MSSQLLocalDB` |
| 3 | Dashboard chrome looks wrong — the menu overlaps the header, or the header is cut off | A WinForms control z-order issue in `UI/DashboardForm.cs`: docking is resolved in reverse z-order, so the control added last is docked first | **Fixed.** The strips are now added in reverse (menu, status, profile, header). The symptom was worse than it looks: the first menu caption and its function were hidden behind the header |
| 4 | Null reference exception when clicking a grid | A `SelectionChanged` handler firing before a row exists | Send the stack trace. It will be a missing null guard |
| 5 | *"The designer could not be shown"* when double-clicking a form | **Expected.** The forms are laid out in code rather than in a `.Designer.cs` file, which is a stated limitation (§7.5 of the report) | Ignore it. It does not affect running the application |

Two further defects were found by the first real run and are also fixed: the login screen clipped
the demonstration credentials (the label was capped at 60 pixels for four lines of text), and it was
stretched to the theme's 760x500 minimum size although it is a fixed 520x430 dialog, which left the
content stranded in one corner.

**One environment note.** SQL Server LocalDB has no ARM64 build, so on Windows on ARM it runs under
x64 emulation, and installing it needs administrator rights. An ordinary x64 Windows machine with
Visual Studio has LocalDB already.

Any other exception: see §7.

---

## 7. What to report back

Send these three things. The exact text matters far more than a screenshot.

1. **The exact exception text** — the message, plus the `at ...` line or lines. Copy and paste it,
   rather than photographing the screen.
2. **Which step** — building, startup, sign-in, or a specific button.
3. **A screenshot**, only if the problem is visual rather than an exception.

---

## 8. What the run unblocks

Once the application runs and the screenshots are captured, the only remaining work is
presentation:

| Remaining task | Where |
|---|---|
| Redraw the 12 diagrams in Enterprise Architect and export them | `docs/uml/README.md` |
| Insert the 13 screenshots and the code screenshots into the report | `docs/report/` — search for `[SCREENSHOT:` and `[CODE:` |
| Complete the cover sheet and the work distribution plan with real names | `docs/05-work-distribution.md` |
| Record the demonstration video | — |

See `docs/report/README.md` for the assembly checklist and the mark each section carries.


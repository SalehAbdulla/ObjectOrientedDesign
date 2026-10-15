# 4. User Interface Design

**Rubric: 10 marks — UI functionality 3, UI usability and consistency 2, integration with OO
classes 3, overall system functionality 2.**

The application has 12 screens. Each screenshot below must be captured from the running
application in Visual Studio; the placeholder marks where it goes.

---

## 4.1 The screens

### Screen 1 — `frmLogin`, the single login screen

`[SCREENSHOT: frmLogin]`

One screen for all three roles, as the brief requires. It shows the demonstration credentials
because SCAIS is a marked project with a seeded database, and a marker who cannot sign in cannot
award the implementation marks.

**Requirements satisfied:** UC-01, UC-20, and the "single login screen" requirement.

**Classes used:** `AuthenticationService`, `LoginResult`, `UserRepository`, `UserFactory`,
`SessionManager`, `User`.

### Screen 2 — `frmAdminDashboard`

`[SCREENSHOT: frmAdminDashboard]`

**Requirements satisfied:** UC-02 and the role-based access requirement. The menu is filtered by
`Administrator.CanManageUsers` and `CanManageCatalogue` rather than by a role test, so a
registrar-level administrator sees a different menu without the screen containing any branching
on the role.

### Screen 3 — `frmStudentDashboard`

`[SCREENSHOT: frmStudentDashboard]`

The profile strip renders `User.GetProfileSummary()`. Signing in as a student, an adviser or an
administrator produces three different summaries from the same code.

**Requirements satisfied:** UC-02, UC-08, UC-09, UC-10, UC-11, UC-21.

### Screen 4 — `frmAdviserDashboard`

`[SCREENSHOT: frmAdviserDashboard]`

The status line reports how many plans are awaiting the adviser's decision before the screen is
opened, which is a small usability decision that saves the adviser a click.

### Screen 5 — `frmMyAcademicRecord`, the student's record and plan

`[SCREENSHOT: frmMyAcademicRecord — the My record tab]`
`[SCREENSHOT: frmMyAcademicRecord — the Eligible courses tab]`
`[SCREENSHOT: frmMyAcademicRecord — the My requests tab]`

Three tabs, covering the four student use cases, because a student works through them in one
sitting.

The **Eligible courses** tab is the screen that demonstrates the central requirement: every course
in the track is listed with the verdict of the eligibility rules, and a blocked course shows the
reason naming the missing prerequisite.

**Requirements satisfied:** UC-08, UC-09, UC-10, UC-11, UC-18.

**Classes used:** `CourseAdvisingService`, `EligibilityValidator`, the five rules, `CourseRequest`,
`CourseRequestLine`, `CourseEligibility`, `EligibilityResult`, `EnrollmentRepository`,
`ReportService`.

### Screen 6 — `frmCourseRequestReview`, the adviser's approval screen

`[SCREENSHOT: frmCourseRequestReview]`

**Requirements satisfied:** UC-15, UC-19.

**Classes used:** `CourseAdvisingService`, `CourseRequest`, `StudentNotificationService`,
`AuditLogService`.

Pressing Approve is the moment the Observer pattern fires, and the screen says so in its
confirmation message, so the marker can connect the visible behaviour to the design.

### Screen 7 — `frmAdviseeList`, the adviser's advisees

`[SCREENSHOT: frmAdviseeList]`

Three grids on one screen: the advisees with their status counts, the selected advisee's academic
record, and the courses that advisee is eligible for. The recommendation is on the same screen as
the record because an adviser makes the recommendation while looking at the record.

**Requirements satisfied:** UC-12, UC-13, UC-14.

### Screen 8 — `frmCourseCatalog`, the catalogue and the curriculum rules

`[SCREENSHOT: frmCourseCatalog — the Courses tab]`
`[SCREENSHOT: frmCourseCatalog — the Rules tab]`

The screen where the curriculum that the eligibility rules enforce is maintained.

**Requirements satisfied:** UC-04, UC-05.

**Classes used:** `CourseRepository`, `PrerequisiteRepository`, `SpecializationRepository`,
`Course`, `Prerequisite`.

### Screen 9 — `frmUserManagement`

`[SCREENSHOT: frmUserManagement]`

**Requirements satisfied:** UC-03. Editing a row and saving calls through to
`UserRepository.Update`, whose polymorphic dispatch decides which role table to write. The screen
contains no branching on the role.

### Screen 10 — `frmAssignAdvisees`

`[SCREENSHOT: frmAssignAdvisees]`

**Requirements satisfied:** UC-06. This is the aggregation `Adviser ◇ Student` made visible: an
assignment can be made, changed and cleared, which is exactly why the relationship is an
aggregation and not a composition.

### Screen 11 — `frmReports`

`[SCREENSHOT: frmReports — the progression report]`
`[SCREENSHOT: frmReports — the compliance report]`

**Requirements satisfied:** UC-16, UC-17, and the brief's requirement to design at least two
reports.

### Screen 12 — `frmMessages`

`[SCREENSHOT: frmMessages]`

**Requirements satisfied:** UC-21. This screen is the visible end of the Observer pattern: it
writes nothing. Every row was produced by `StudentNotificationService` reacting to a plan changing
state.

---

## 4.2 Usability and consistency — 2 marks

Consistency is achieved structurally rather than by discipline. Every screen builds its controls
through `UiTheme`, so the font, the colours, the button size, the grid appearance and the dialog
style are defined once.

| Property | How it is enforced |
|---|---|
| Fonts | `UiTheme.HeadingFont`, `SubHeadingFont`, `BodyFont` — one definition |
| Colours | `UiTheme.HeaderColour`, `BlockedColour`, `EligibleColour` — one definition |
| Buttons | `UiTheme.CreateButton` and `CreateMenuButton` — the same minimum size and padding everywhere |
| Grids | `UiTheme.CreateGrid` — alternating rows, full-row selection, sortable headers, no row headers |
| Dialogs | `UiTheme.ShowError`, `ShowInfo`, `Confirm` — one message box style, with the application name in the title |
| Screen layout | `UiTheme.ApplyForm` sets the same font, background, start position and minimum size |
| Dashboard chrome | `DashboardForm` is the shared base class, so all three dashboards have the same header, profile strip and status line |

Further usability decisions:

- **Validation before saving.** The domain setters validate, so the user is told what is wrong
  with a name, an e-mail address or a credit hour value before the database is touched.
- **Every destructive action is confirmed.** Deleting an account, deleting a course, deleting a
  rule and submitting a plan all ask first.
- **Every screen has a status line** reporting the result of the last action, so the user is never
  left wondering whether something happened.
- **Failure is reported, never silent.** `UiTheme.Guard` wraps every screen action, so an
  unexpected exception produces a message rather than a crash.
- **Selectors are used instead of free text** wherever a key must be chosen: an adviser is picked
  from a list, a track from a list, a rule type from a list. The user cannot type an invalid key.

---

## 4.3 Integration with the OO classes — 3 marks

This is the criterion that asks whether the screens actually use the designed classes, or whether
the design exists only on paper. The evidence is the direction of every dependency: **no form
contains SQL, and no form contains an eligibility rule.**

| Screen | Calls into | Evidence |
|---|---|---|
| `frmLogin` | `AuthenticationService.Login`, `User.Authenticate` | The form never reads the `Users` table |
| `frmMyAcademicRecord` | `CourseAdvisingService`, `ReportService`, `EligibilityValidator` | The form asks for verdicts and renders them; it does not know what a rule is |
| `frmCourseRequestReview` | `CourseAdvisingService.ApproveRequest` | The form does not write a notification or an audit line; the observers do |
| `frmAdviseeList` | `UserRepository`, `ReportService`, `CourseAdvisingService` | Three services, no SQL |
| `frmUserManagement` | `UserRepository.Add`, `Update`, `Delete`, `UpdatePassword` | The polymorphic dispatch is in the repository, not the screen |
| `frmCourseCatalog` | `CourseRepository`, `PrerequisiteRepository` | Domain validation surfaces through the `Course` and `Prerequisite` constructors |
| `frmReports` | `ReportService` | The screen renders rows; it performs no arithmetic |
| All three dashboards | `DashboardForm` base class, `User.GetProfileSummary` | One implementation of the chrome, three polymorphic summaries |

Two specific pieces of evidence worth pointing the marker at:

1. **`frmMyAcademicRecord` shows a blocked course by reading `CourseRequestLine.IsEligible`,**
   which is a computed property on the domain object. The screen does not re-run the rules to
   decide how to colour a row.
2. **`frmAdviserDashboard` decides whether to show the approval function by asking the account,**
   not by testing the role:
   `if (this.adviser.CanApproveCourseRequests) { AddMenuItem(...); }`

---

## 4.4 Overall system functionality — 2 marks

All 21 use cases are implemented and reachable from the running application. The demonstration
path in `src/SCAIS/README.md` §4 exercises every one of them in nine steps, from signing in as a
student with an ineligible course through to an adviser approving the plan, the student reading
the message, and an administrator running both reports.

**Two reports are produced**, as the brief requires:

1. **Student course progression** — every course a student has completed, is taking or has yet to
   take, with the semester and the grade, under a summary line giving the counts and the credit
   hours earned.
2. **Prerequisite compliance** — every student who cannot take a course, the course they are
   blocked from, the exact subject that blocks them, and the rule type.

Both reports can be **printed** through `PrintDocument`, which includes "Microsoft Print to PDF",
and both can be **exported to CSV**.

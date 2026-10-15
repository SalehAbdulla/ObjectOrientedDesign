# 06 — Report Structure & Content Guide

The report must follow `Report Template.pdf`. Below, each template heading is
expanded with **exactly what to write** and **how many marks it is worth**, so
nothing is left thin.

---

## Cover & front matter

- Completed **Assessment Cover Sheet** (Learner ID, names, programme, lecturer, date).
- Table of contents.
- **Work distribution plan** (see `05-work-distribution.md`) — required by the brief.

---

## 1. Project Overview — 10 marks

| Sub-section | Marks | What to write |
|---|---|---|
| 1.1 Project Background | — | BIT is launching SCAIS; advising is currently manual/spreadsheet-based; IT programmes have 4 specialisations with prerequisite chains |
| 1.2 Problem Statement | **3** | Three concrete problems: (a) advisers manually cross-check prerequisites → errors and delays; (b) students enrol in courses they are not eligible for, causing failed semesters; (c) no single view of a student's completed / in-progress / pending courses, so planning is guesswork |
| 1.3 Project Objectives | **2** | 5 numbered objectives: automate eligibility validation; provide a single role-based portal; track course status; give advisers a recommendation tool; produce planning reports |
| 1.4 Target Users | **2** | Table of 3 users with what each does: **Administrator** (catalog, accounts, rules), **Adviser** (advisees, approval), **Student** (record, requests) |
| 1.5 Main System Functions | **3** | Numbered list of the 19 use cases from `01-full-marks-plan.md` §3, grouped by actor. Explicitly mention: single login + role redirect, prerequisite/corequisite validation, status tracking, 2+ reports |

**Scoring tip:** the marker is checking for *specificity*. "Improve efficiency" earns
nothing; "reduce prerequisite-checking time from ~10 minutes per advisee to under
a minute" earns the marks.

---

## 2. UML Design — 25 marks

All diagrams must be produced in **Enterprise Architect** and exported into the report.

### 2.1 Use Case Diagram — 4 marks
- 3 actors (Administrator, Adviser, Student) + System boundary.
- All 19 use cases.
- `<<include>>` relationships: *Validate Eligibility* included by *Submit Course Request* and *Recommend Courses*; *Authenticate User* included by *Log In*.
- `<<extend>>` where genuinely conditional (e.g. *Notify Student* extends *Approve Course Plan*).
- **Do not** invent relationships just to have them.

### 2.2 Class Diagram — 15 marks *(the biggest single item)*
Include and label:
- Visibility (`-` `+` `#`) on every attribute and operation.
- **Multiplicity on every association.**
- Exactly one **composition** (`CourseRequest ◆ CourseRequestLine`), one
  **aggregation** (`Adviser ◇ Student`), the **inheritance** tree
  (`User` ← `Student`/`Adviser`/`Administrator`), and three **interface
  realisations** (`IRepository<T>`, `IEligibilityRule`, `IUserObserver`).
- A short paragraph under the diagram: *"We deliberately did not relate X to Y
  because…"* — the rubric rewards **avoiding unnecessary relationships**.

See `02-architecture-and-class-design.md` §6 for the ready-made relationship table.

### 2.3 Sequence Diagrams — 4 marks
Four diagrams (SD-1 … SD-4 in `02-architecture-and-class-design.md` §7).
Each must show lifelines, activation bars, ordered messages with parameters, and
return messages. Lifeline names must match the class diagram exactly.

### 2.4 UML Consistency — 2 marks
Add a short **consistency statement**: class names in the sequence diagrams are
the class diagram's classes; the persistent classes map 1:1 to the ERD tables;
every use case appears in the class diagram as an operation.

---

## 3. Object-Oriented Design — 25 marks

Use the template's headings exactly. Each sub-section needs **a code screenshot**
and **an explanation of why** — the rubric rewards reasoning, not the feature.

| Sub-section | Marks | Example class to cite | The sentence that earns the marks |
|---|---|---|---|
| 3.1 Encapsulation | 5 | `Enrollment` | "`Status` and `Grade` have no public setter; they change only through `MarkCompleted(grade)`, which validates the grade first — so an enrolment can never be *Completed* without a grade." |
| 3.2 Inheritance | 4 | `User` → `Student`/`Adviser`/`Administrator` | "The is-a test holds: every adviser **is a** user. Identity and authentication are implemented once in `User`; only the dashboard, profile summary and role are specialised." |
| 3.3 Polymorphism | 5 | `User.GetDashboardTitle()` | Show the single login call site and the three resulting dashboards. "The form holds a `User` reference and never branches on role." |
| 3.4 Abstraction | 4 | `User` (abstract), `EligibilityValidator` | "Callers see `Validate(student, course)`; the five rules and their order are hidden behind it." |
| 3.5 Interface | 3 | `IRepository<T>`, `IEligibilityRule`, `IUserObserver` | "The services depend on the contract, not the concrete repository, so the LocalDB data layer could be replaced by a web API without changing business logic — which matters because the brief plans web/mobile expansion." |
| 3.6 OO Design Quality | 4 | whole design | Responsibility-assignment table + note on high cohesion / low coupling / reuse |


---

## 4. User Interface Design — 10 marks

Insert screenshots for at least these screens (label each):

1. `frmLogin` — the single login screen
2. `frmAdminDashboard` / `frmUserManagement` — admin CRUD
3. `frmCourseCatalog` — course + prerequisite maintenance
4. `frmAdviserDashboard` / `frmAdviseeList` — advisees and history
5. `frmRecommendCourses` — eligibility results with reasons
6. `frmCourseRequestReview` — approve / modify / reject
7. `frmStudentDashboard` / `frmMyRecord` — completed / in-progress / pending
8. `frmEligibleCourses` — eligible courses for next semester
9. `frmMyRequests` — approved courses + adviser remarks
10. `frmReports` — the two reports

Under each screenshot, one sentence: **which requirement it satisfies and which
classes it uses.** That is the "integration with OO classes" mark (3 of the 10).

**Usability/consistency evidence:** consistent fonts, consistent button order
(OK/Cancel), input validation before save, meaningful error messages, tab order.

---

## 5. Database Design — 10 marks

- The **ERD** (from EA) with all 13 tables, PK/FK, cardinalities.
- A paragraph on **3NF** (see `03-database-design.md` §1).
- A screenshot of the seeded data proving **≥15 records per table**
  (`SELECT COUNT(*) FROM <table>` for each table).
- A description of the data layer: `IRepository<T>`, `SqlRepositoryBase<T>`,
  `DatabaseManager`, parameterised queries.

**Reports (required by the brief — "design at least two"):**
1. **Student Course Progression Report** — completed / in-progress / pending per
   student with GPA and credit totals.
2. **Prerequisite Compliance Report** — students blocked from a course and the
   exact rule that blocked them.
3. *(optional third)* **Specialisation Progress Report** — credits earned per
   track, showing whether the student is on track.

---

## 6. Design Pattern — 20 marks

Three patterns, each written with the (a)–(f) structure in
`04-design-patterns.md` §5: Singleton, Observer, Strategy.
Include the pattern UML from EA and a code snippet for each.

---

## 7. Conclusion

- What was developed (SCAIS: 3 roles, 21 use cases, 13 tables, 3 patterns, 2 reports).
- How the system meets the requirements (map back to the objectives in §1.3).
- How OO concepts were applied (one sentence per concept).
- **Limitations & future work** — honesty here reads as understanding: e.g. no
  authentication against Active Directory, no web front end yet (the architecture
  anticipates it), the Observer notifications are in-app only.

---

## 8. References

Cite Enterprise Architect documentation, Microsoft ADO.NET/LocalDB documentation,
and any textbook used for the design patterns (GoF). Follow a single consistent
referencing style.

---

## 9. Pre-submission check

- [ ] Every rubric sub-section has a heading in the report
- [ ] Every claim about an OO concept has a code screenshot
- [ ] Every design pattern has benefits **and** limitations
- [ ] Every diagram is exported at readable resolution and referenced by number
- [ ] Work distribution plan and cover sheet are attached
- [ ] Zip named `S1G1` with Report + EA file + C# project + demo video

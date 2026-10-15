# IT7006 Object Oriented Design — Group Project

**System:** SCAIS — *Smart Course Advising Information System*
**Client:** Bahrain Institute of Technology (BIT)
**Course:** IT7006 Object Oriented Design (35% of semester grade)
**Submission date:** 23 December 2026
**Weight:** 100 marks across 6 rubric sections (11 assessed tasks)

---

## 1. What this repository is

This repository is the **single source of truth** for the group project. It holds:

| Folder / file | Contents |
|---|---|
| `Project Folder-20261014/` | The official assessment paperwork issued by the tutor |
| `docs/` | The team's plan, design specifications and report working drafts |
| `src/` | The Visual Studio C# solution (added once implementation starts) |
| `ea/` | The Sparx Enterprise Architect project file (`.eapx`) |
| `deliverables/` | Final zipped submission package + demo video link |

> **Team rule:** nobody commits directly to `main`. Every member works on
> `feature/<area>-<initials>` and opens a pull request so the **Lead Integrator**
> can review before it lands. This is what makes the "work distribution plan"
> defensible at marking time.

---

## 2. The business case in one paragraph

BIT is launching SCAIS to help faculty advisers guide advisees efficiently. The
system must recommend and **validate** course selections against each student's
academic history, specialization, **prerequisites** and **corequisites**, and
block or warn a student who is not eligible. It must support three roles
(Administrator, Adviser, Student) behind a **single login screen**, track each
course as *completed / in-progress / pending*, and produce academic-planning
reports.

---

## 3. Technology stack (fixed by the brief)

| Layer | Technology |
|---|---|
| CASE / UML / MDD | Sparx Enterprise Architect |
| Frontend | C# — Windows Forms (.NET Framework 4.8) |
| Backend / Data | SQL Server LocalDB (ADO.NET) |
| IDE | Visual Studio |

---

## 4. Non-negotiable deliverables

1. **Design document** — one EA project file containing Use Case, Class, Sequence diagrams, the ERD, and the design-pattern diagrams.
2. **C# project** — fully generated classes, well commented, Windows Forms UI, LocalDB database file inside the project.
3. **Report** — following `Project Folder-20261014/Report Template.pdf`.
4. **Demo video**.
5. **Work distribution plan** (submitted with the project).
6. **Minimum 15 mock records in every table.**

Submission zip must be named `S1G1` (Stream 1, Group 1) and contain: Project Report, EA Project File, C# Project File, Demo Video File.

---

## 5. Where to start

### The plan and the design
1. [`docs/01-full-marks-plan.md`](docs/01-full-marks-plan.md) — the master plan and rubric→evidence map.
2. [`docs/05-work-distribution.md`](docs/05-work-distribution.md) — who owns what.
3. Design specs: [`docs/02-architecture-and-class-design.md`](docs/02-architecture-and-class-design.md), [`docs/03-database-design.md`](docs/03-database-design.md), [`docs/04-design-patterns.md`](docs/04-design-patterns.md).

### The working application
4. [`docs/08-windows-first-run-and-test.md`](docs/08-windows-first-run-and-test.md) — **start here to run it.** Pre-flight checklist, the nine-step demonstration path, the screenshot checklist, and the five most likely first-run failures with their fixes.
5. [`src/SCAIS/README.md`](src/SCAIS/README.md) — the application's own README: architecture, credentials and the demonstration script.
6. [`docs/07-verification.md`](docs/07-verification.md) — what was verified and how, including the database checks run against a live engine.

### The diagrams (for redrawing in Enterprise Architect)
7. [`docs/uml/README.md`](docs/uml/README.md) — the diagram index and the EA redraw guide.
8. The twelve diagram sources are in [`docs/uml/`](docs/uml), each with an element checklist. Rendered PNGs are in [`docs/uml/png/`](docs/uml/png).
9. [`docs/uml/07-consistency-checklist.md`](docs/uml/07-consistency-checklist.md) — the two consistency marks.

### The report
10. [`docs/report/README.md`](docs/report/README.md) — the assembly guide, mapping each section to its marks.
11. Sections 1 to 7 follow the official template exactly.

### Earlier drafts
12. [`docs/06-report-structure.md`](docs/06-report-structure.md) — the original report guide, superseded by `docs/report/`.

---

## 6. Academic integrity

The assessment cover sheet states that **the use of generative AI tools is strictly prohibited**.
All UML diagrams, source code and report prose in this repository must be
produced and understood by the team members themselves. Each member must be able
to explain *any* part of the system, not only their own, during the progress check.

`docs/uml/` contains diagram **sources** rather than a finished Enterprise Architect file: the
team must redraw every diagram in EA, which is both a requirement of the brief and a way of
ensuring the model is understood rather than accepted.

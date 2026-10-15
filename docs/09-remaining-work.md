# 09 — What Is Left To Do

Everything in this document needs a person at the machine. The code, the database and the
verification are finished; what remains is producing the artefacts the brief asks for that cannot be
generated from a terminal: screenshots, the Enterprise Architect model, the assembled report, the
demonstration video and the submission package.

The state of the code and the evidence for it are recorded in
[`07-verification.md`](07-verification.md).

---

## 1. Screenshots

**All thirteen screens are captured.** They are stored in [`screenshots/`](screenshots), which carries
the mapping onto the report's `[SCREENSHOT: ...]` placeholders. They were taken from the running
application with the window frame included, exactly as `Alt` + `Print Screen` produces.

What is still outstanding is the **code** screenshots: the ones that prove each OO concept, listed in
[`08-windows-first-run-and-test.md`](08-windows-first-run-and-test.md) §5 and marked `[CODE: ...]`
throughout [`report/03-object-oriented-design.md`](report/03-object-oriented-design.md) and
[`report/06-design-pattern.md`](report/06-design-pattern.md). Each must show the class declaration,
the members the report makes a claim about, and enough context to show it is real code.

One more screenshot is worth taking by hand, because the automated pass could not produce it: open
`frmMyAcademicRecord` as `student02`, add **IT221** to the plan, and capture the plan grid. The Verdict
column then reads *"Blocked: Prerequisite - must first complete IT220"*, which is the clearest single
piece of evidence for the block-or-warn requirement.

---

## 2. Enterprise Architect — the design document

[`uml/`](uml) holds the diagram **sources** and the rendered PNGs; the brief requires the model in
Enterprise Architect. Work through [`uml/README.md`](uml/README.md), which carries the redraw guide
and an element checklist per diagram:

- [ ] Use case diagram — 3 actors, 21 use cases, the four `«include»` and two `«extend»`
- [ ] Class diagram, view 1 — 14 domain classes, visibility on every member, multiplicity on every association
- [ ] Class diagram, view 2 — interfaces, 9 repositories, services, observers, factories
- [ ] Four sequence diagrams
- [ ] ERD — 13 tables, PK/FK, cardinalities, 3NF noted
- [ ] Three design-pattern diagrams (Singleton, Observer, Strategy)
- [ ] State diagram for `CourseRequest`
- [ ] Export each diagram as PNG and paste it at its `[DIAGRAM: ...]` placeholder in the report
- [ ] Work through [`uml/07-consistency-checklist.md`](uml/07-consistency-checklist.md) — it is the two consistency marks

---

## 3. The report

Follow [`report/README.md`](report/README.md), which maps every section to its marks.

- [ ] Copy the cover sheet from `Project Folder-20261014/Report Template.pdf` and complete it
- [ ] Fill in the real names in [`05-work-distribution.md`](05-work-distribution.md) — every `_TBD_` cell
- [ ] Paste the sections in order, then insert every diagram, screenshot and code screenshot
- [ ] Check the counts quoted in §7.1 against the diagrams: 3 actors, 21 use cases, 14 domain classes,
      3 interfaces, 13 tables, 3 patterns, 4 sequence diagrams, 13 screens, 2 reports
- [ ] Export to PDF and check that the class diagram is legible at print size

---

## 4. Demonstration video

Record the nine-step path in [`08-windows-first-run-and-test.md`](08-windows-first-run-and-test.md) §4.
It must show, in one take:

1. signing in as each of the three roles and the redirect that follows,
2. a course **blocked** for a missing prerequisite, naming it,
3. a plan submitted, then approved by the adviser with a remark,
4. the student seeing the approval, the remark and the unread-message count,
5. both reports,
6. the audit log file that the second observer wrote.

---

## 5. Packaging

- [ ] Zip named **`S1G1`** containing: the report PDF, the `.eapx` model, the C# project, the video
- [ ] Confirm the database requirement still holds on a clean machine: the application creates its own
      database from the embedded scripts on first run, so no `.mdf` has to be attached
- [ ] If a physical `.mdf` is wanted, detach the created one, add it to the project and change only the
      connection string in `App.config`

---

## 6. Optional, if there is time

| Idea | Why it would help |
|---|---|
| Re-run the verification harness after any further change | It catches exactly the class of defect that review misses — it found the adviser-loading and dashboard-docking bugs |
| A test project inside the solution | §7.5 of the report names the absence of automated tests as a limitation; the harness proves the design is testable, so moving it into the solution would close the limitation |
| Replace `DatabaseManager` with injected dependencies | The same section names the Singleton as the obstacle to unit testing |
| Store the eligibility rules in the database | Then the administrator can maintain the rule set without a rebuild, which is the one part of UC-05 still held in code |
| An `.rdlc` report definition | The reports print through `PrintDocument` today, so their layout is functional rather than formatted |

---

## 7. Environment notes for whoever picks this up

- The application needs **SQL Server LocalDB**. It comes with Visual Studio's *desktop development*
  workload, so an ordinary Windows machine already has it.
- LocalDB has **no ARM64 build**: on Windows on ARM it installs and runs under x64 emulation, and
  installing it needs administrator rights.
- To reset the database, delete the `SCAIS` database in SQL Server Object Explorer and run the
  application again; it rebuilds itself from `Database/01_schema.sql` and `Database/02_seed.sql`.
- The verification harness behind §2.1 of [`07-verification.md`](07-verification.md) is deliberately
  **not** in this repository, so the submitted solution stays clean. It is a small console project that
  references the built `SCAIS.exe`, and every check it ran is listed in that document.

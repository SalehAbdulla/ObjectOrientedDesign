# Project Report — Assembly Guide

The report follows the structure of `Project Folder-20261014/Report Template.pdf` exactly.
Each section is written as a separate file so that sections can be edited by different team
members and merged at the end.

---

## 1. Files, in the order the template requires

| Template section | File | Marks | Status |
|---|---|---|---|
| Cover sheet | from `Report Template.pdf` | — | to complete by hand |
| 1. Project Overview | [`01-project-overview.md`](01-project-overview.md) | **10** | written |
| 2. UML Design | [`02-uml-design.md`](02-uml-design.md) | **25** | written — insert the EA diagrams |
| 3. Object-Oriented Design | [`03-object-oriented-design.md`](03-object-oriented-design.md) | **25** | written — insert the code screenshots |
| 4. User Interface Design | [`04-user-interface-design.md`](04-user-interface-design.md) | **10** | written — insert the screenshots |
| 5. Database Design | [`05-database-design.md`](05-database-design.md) | **10** | written — insert the ERD |
| 6. Design Pattern | [`06-design-pattern.md`](06-design-pattern.md) | **20** | written — insert the pattern diagrams |
| 7. Conclusion | [`07-conclusion.md`](07-conclusion.md) | — | written |
| Work distribution plan | [`../05-work-distribution.md`](../05-work-distribution.md) | — | to complete by hand |

**Total: 100 marks.**

---

## 2. What still has to be done by hand

The text is written, but four things can only be produced in the Windows environment.
They are listed here so none is forgotten:

| To do | Where it goes |
|---|---|
| Export the EA diagrams as PNG and paste them in | §2, §5, §6 |
| Screenshot the 11 running screens | §4 |
| Screenshot the code that proves each OO concept | §3.1 to §3.6 |
| Fill in the cover sheet and the work distribution plan | front matter |

---

## 3. Assembly order

1. Copy the cover sheet from the template and complete the blue section.
2. Paste the work distribution plan after the cover sheet.
3. Paste each numbered section in order.
4. Insert the diagrams at the placeholders marked `[DIAGRAM: ...]`.
5. Insert the screenshots at the placeholders marked `[SCREENSHOT: ...]`.
6. Insert the code screenshots at the placeholders marked `[CODE: ...]`.
7. Export to PDF and check that every diagram is legible at print size.

---

## 4. How to produce each artefact

### The diagrams
Export from EA with `Publish → Save as Image`, PNG, at a scale that keeps the smallest
text legible. The class diagram will need its own landscape page. If EA's output is not
legible, the rendered PNGs in [`../uml/png/`](../uml/png) are an accurate fallback,
because they are generated from the same model.

### The code screenshots
Use Visual Studio's own screenshot rather than a screen capture of the whole window, so the
class name and the line numbers are visible. Each screenshot must show:

- the class declaration,
- the members that the report is claiming something about,
- enough context that the marker can see it is real code and not a mock-up.

### The screenshots of the screens
Run the application, resize the window so nothing is cut off, and capture the client area
only. `Alt` + `Print Screen` captures the active window on Windows.

**[`../08-windows-first-run-and-test.md`](../08-windows-first-run-and-test.md)** has the exact
nine-step sequence that produces every screenshot in one pass, together with the full
checklist of screens and code files, and what to do when the first run fails.

---

## 5. Honesty notes to keep in the report

These are stated in the relevant sections and must not be edited out. They are what
separate "understanding demonstrated" from "claim made":

- The application compiles against the real .NET Framework 4.8 reference assemblies with
  zero errors and zero warnings, and the database scripts were executed against a live
  T-SQL engine. Both are recorded in [`../07-verification.md`](../07-verification.md).
- The password hash uses a fixed salt and is not suitable for production.
- Session state is static, so one user is signed in at a time.
- Eligibility rules are composed in code rather than configured in the database.
- The `.mdf` is created by the application rather than shipped, and the reason is stated.

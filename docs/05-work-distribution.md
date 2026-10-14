# 05 — Work Distribution Plan

The brief requires *"the submission of the project … along with the work
distribution plan for marking"*, and warns that *"each member of the team is
expected to do an equal amount of work and is expected to understand the entire
application well enough to be examined on how the pieces fit together."*

Fill in the **Name** column with the real team members before submitting.

---

## 1. Named roles (required by the brief)

| Role | Responsibility | Member |
|---|---|---|
| **Team Leader** | Schedules meetings, mediates decisions, breaks ties, holds members accountable | _TBD_ |
| **Recorder** | Minutes every meeting, logs decisions and assigned tasks with due dates, keeps the Moodle group forum thread | _TBD_ |
| **Lead Integrator** | Owns design/code conflict resolution, keeps tasks current and fully functioning, reports participation to the tutor | _TBD_ |
| **Time keeper** | Tracks the timeline, meeting timings and milestone slippage | _TBD_ |

---

## 2. Work packages (equal weight per member)

| # | Work package | Deliverables | Owner | Est. hours |
|---|---|---|---|---|
| WP1 | **Requirements & Use Case** | 19 use cases, use-case diagram, report §1 (Overview, problem, objectives, users, functions) | _TBD_ | 14 |
| WP2 | **Class diagram & OO model** | Class diagram with visibility + multiplicity, relationship justification table, report §3.1–3.2 | _TBD_ | 16 |
| WP3 | **Database & data layer** | ERD, `SCAIS.mdf`, 12 tables, seed script (≥15 rows/table), `IRepository<T>`, all repositories | _TBD_ | 16 |
| WP4 | **Business logic & patterns** | `User` hierarchy, `EligibilityValidator` + 5 rules, `DatabaseManager`, `CourseRequest` Observer, pattern UML + report §6 | _TBD_ | 16 |
| WP5 | **UI implementation** | 14 forms, navigation, validation, data binding, report §4 screenshots | _TBD_ | 18 |
| WP6 | **Reports, sequence diagrams, integration & packaging** | 2 reports, 4 sequence diagrams, consistency pass, report assembly, demo video, `S1G1.zip` | _TBD_ | 16 |

**Pairing rule:** no work package is owned by only one person end-to-end.
Every package has an **owner** and a **reviewer** from another package, so
knowledge of the whole system is spread across the team (which is exactly what the
progress check tests).

| Package | Owner | Reviewer |
|---|---|---|
| WP1 | _TBD_ | _TBD_ |
| WP2 | _TBD_ | _TBD_ |
| WP3 | _TBD_ | _TBD_ |
| WP4 | _TBD_ | _TBD_ |
| WP5 | _TBD_ | _TBD_ |
| WP6 | _TBD_ | _TBD_ |

---

## 3. Contribution evidence (this is what protects individual marks)

The marker can verify contribution objectively from the repository. Keep it honest:

- Every member commits to their **own** branch `feature/<area>-<initials>`.
- Pull requests are reviewed by the **Lead Integrator** before merging to `main`.
- Commit messages follow `WP<n>: <what changed>` so the history maps to the table above.

Useful command for the report appendix:
```bash
git shortlog -sne --all      # commits per author
git log --pretty=format:'%h %an %s' --reverse   # chronological contribution log
```

---

## 4. Meeting record template (Recorder)

```
Meeting #__        Date: ____/____/______        Time: __:__ – __:__ (__ min)
Present: ______________________________________________
Absent:  ______________________________________________

Decisions made
  1. ________________________________________________
  2. ________________________________________________

Tasks assigned
  | Task | Assigned to | Due date | Status |
  |------|-------------|----------|--------|

Blockers / risks
  - ________________________________________________

Next meeting: ____/____/______
```

---

## 5. Progress check preparation (each member, 10 minutes)

Each member must be able to explain **their own work and someone else's**. Prepare
these four answers for the meeting:

1. **What I built** — the package I own, and the class/screen that proves it.
2. **How it connects** — which other packages depend on mine and how (name the interface).
3. **An OO decision I made and why** — e.g. *"I made `User` abstract because…"*.
4. **Someone else's area** — be ready to walk through one other member's diagram
   or class and explain how it fits.

---

## 6. Equal-work declaration

Each member confirms that the work recorded above is an accurate account of their
contribution, and that they understand the whole system sufficiently to be
examined on any part of it.

| Member name | Student ID | Signature |
|---|---|---|
| | | |
| | | |
| | | |
| | | |
| | | |
| | | |

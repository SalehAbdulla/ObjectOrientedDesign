# 2. UML Design

**Rubric: 25 marks — use case diagram 4, class diagram 15, sequence diagrams 4, UML
consistency 2.**

All diagrams were produced in Sparx Enterprise Architect from the models specified in
[`../uml/`](../uml). The diagram sources are plain text and render on GitHub; the exported EA
images are inserted at the placeholders below.

---

## 2.1 Use Case Diagram — 4 marks

`[DIAGRAM: use-case — exported from EA]`

The full specification, the use case catalogue and the relationship justification are in
[`../uml/01-use-case-diagram.md`](../uml/01-use-case-diagram.md).

**Summary.** Three actors — Administrator, Adviser and Student — and 21 use cases inside one
system boundary. Authentication and eligibility validation are modelled as **included** use
cases rather than as secondary actors, because the system performs them on behalf of a primary
actor.

The four inclusions and two extensions are each justified individually in §1.4 of that file.
Nothing else is related, and the reasoning for that is stated: the administrator's maintenance
use cases are independent operations that may be performed in any order, and connecting them
would imply an ordering that does not exist.

---

## 2.2 Class Diagram — 15 marks

The class diagram is the largest single item in the rubric, so it is presented as two views of
one model. Both are in [`../uml/02-class-diagram.md`](../uml/02-class-diagram.md).

### View 1 — the domain model

`[DIAGRAM: class-domain — exported from EA]`

13 domain classes: the abstract `User` with its three subclasses, and the classes that make up
the advising domain — `Specialization`, `Course`, `Prerequisite`, `Enrollment`, `CourseRequest`,
`CourseRequestLine`, `Semester`, `Notification` and `EligibilityResult`, plus the
`IUserObserver` contract.

### View 2 — the services, interfaces and data layer

`[DIAGRAM: class-services — exported from EA]`

The three interfaces, the eight repositories, the abstract `SqlRepositoryBase<T>`, the
`DatabaseManager` Singleton, the four services, the two observers and the two factories.

### What the diagram demonstrates

| Requirement in the rubric | Where it is shown |
|---|---|
| **Appropriate identification of classes** | Domain-driven, with no "Manager" or "Helper" god-classes. Every class is named after something in the business case |
| **Appropriate responsibilities** | §3.6 of this report carries the responsibility table. `Student` answers questions about its own record; `EligibilityValidator` decides eligibility; `CourseRequest` owns its own state transitions |
| **Correct attributes and operations** | Every operation returns something a caller needs. There is no `void DoStuff()` |
| **Appropriate visibility** | `-` private, `#` protected, `+` public, and one `internal` member for persistence |
| **Correct multiplicity** | Stated on **every** association. This is the most commonly lost mark, and §2.4 of the diagram file lists each one |
| **Association** | `Enrollment * — 1 Course`, `Enrollment * — 1 Semester`, and seven others |
| **Aggregation** | `Adviser ◇ Student` (a student exists without an adviser) and `Specialization ◇ Course` (a core course belongs to no single track) |
| **Composition** | `CourseRequest ◆ CourseRequestLine` (a line has no lifetime without its plan, and the database cascades the delete) |
| **Inheritance** | `User` ← `Student`, `Adviser`, `Administrator` |
| **Interface implementation** | `IRepository<T>`, `IEligibilityRule`, `IUserObserver` |
| **Avoidance of unnecessary relationships** | §2.2 of the diagram file states five relationships that were deliberately **not** drawn, with the reason for each |

### The many-to-many self relationship

`Course` appears at both ends of `Prerequisite`. A rule links a dependent course to a course that
must be completed first, so the same table references `Courses` twice. The diagram shows this as
two associations from `Course` to `Prerequisite`, which is the correct way to draw a
self-referencing many-to-many in a class model.

---

## 2.3 Sequence Diagrams — 4 marks

Four diagrams, in [`../uml/03-sequence-diagrams.md`](../uml/03-sequence-diagrams.md):

| Diagram | Scenario | Use cases covered |
|---|---|---|
| **SD-1** | Login and role redirect | UC-01, UC-02, UC-20 |
| **SD-2** | Student views eligible courses and submits a plan | UC-09, UC-10, UC-18 |
| **SD-3** | Adviser approves a plan, and the Observer pattern fires | UC-15, UC-19 |
| **SD-4** | Administrator defines a prerequisite rule | UC-05 |

`[DIAGRAM: sequence-1 — exported from EA]`
`[DIAGRAM: sequence-2 — exported from EA]`
`[DIAGRAM: sequence-3 — exported from EA]`
`[DIAGRAM: sequence-4 — exported from EA]`

Each diagram shows ordered messages with their parameters, activation bars, return messages, and
the fragments the interaction needs — `alt` for the three login outcomes and for the
blocked-or-eligible branch, `loop` for the rule loop over the catalogue, and a `Note` where the
Observer pattern fires.

**The polymorphic point in SD-1** is worth highlighting for the marker: the login form calls
`GetProfileSummary()` and `GetDashboardTitle()` on a `User` reference, and the same two calls
produce three different results depending only on the object that `UserFactory` built.

---

## 2.4 UML Consistency — 2 marks

Consistency is worth two marks and is lost by a name that does not match rather than by a diagram
that is wrong. The full checklist is in
[`../uml/07-consistency-checklist.md`](../uml/07-consistency-checklist.md), and the statement
below summarises it.

**Class names.** Every lifeline in every sequence diagram is a class or interface that appears in
the class diagram. Every message is an operation that exists with that name and those parameters
in the delivered code.

**Use cases.** All 21 use cases are traceable to an operation in the class diagram; the mapping
table is §7.2 of the consistency checklist. Nothing is promised in the use case diagram that the
class model cannot support.

**The class diagram and the ERD agree.** Consistency checklist §7.3 compares each relationship in
both directions. The two places where a name legitimately differs are stated explicitly so that
they do not read as errors:

| Class diagram | Database | Why |
|---|---|---|
| `CourseRequestLine` (singular) | `CourseRequestLines` | The domain model names a type; the schema names a collection |
| `Specialization` (singular) | `Specializations` | The same reason |

**The diagrams and the running application agree.** The final check in the consistency checklist
is behavioural: sign in as each role, confirm that a blocked course names the missing
prerequisite, and confirm that approving a plan writes both a message and an audit line.

---

## 2.5 Design pattern diagrams

Three further class diagrams, one per pattern, are in
[`../uml/05-design-patterns.md`](../uml/05-design-patterns.md) and are discussed in §6 of this
report.

`[DIAGRAM: pattern-singleton — exported from EA]`
`[DIAGRAM: pattern-observer — exported from EA]`
`[DIAGRAM: pattern-strategy — exported from EA]`

---

## 2.6 Supporting diagram

A state diagram of the course plan lifecycle is in
[`../uml/06-state-diagram.md`](../uml/06-state-diagram.md). It is not separately marked, but it is
what justifies the guards in `CourseRequest` and it makes the point at which the Observer pattern
fires visible.

`[DIAGRAM: state — exported from EA]`

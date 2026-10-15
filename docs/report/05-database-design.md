# 5. Database Design

**Rubric: 10 marks — database design 3, data-layer implementation 3, CRUD operations 2,
integration with application 2.**

---

## 5.1 Database design — 3 marks

`[DIAGRAM: erd — exported from EA]`

The full ERD specification is in [`../uml/04-erd.md`](../uml/04-erd.md), and the schema itself is
`src/SCAIS/SCAIS/Database/01_schema.sql`.

**13 tables** covering the entities the brief names, plus the supporting tables the design needs:

| Brief requirement | Table |
|---|---|
| Users | `Users` |
| Students, Advisers | `Students`, `Advisers`, `Administrators` |
| Courses — course details | `Courses` |
| Enrollments — which student enrolled in which course | `Enrollments` |
| Specialization — Programming, Networking, Cybersecurity, Database | `Specializations` |
| Prerequisites — rules linking course dependencies | `Prerequisites` |
| — | `Semesters`, `CourseOfferings`, `CourseRequests`, `CourseRequestLines`, `Notifications` |

### Normalisation

The schema is in **third normal form**, and the evidence is in what the tables do **not** store:

| Normal form | Evidence |
|---|---|
| **1NF** | No repeating groups. A student's courses are rows in `Enrollments`, never a comma separated column. Every column holds a single atomic value |
| **2NF** | Every non-key attribute depends on the whole primary key. Each table uses a surrogate identity key, so there is no partial dependency |
| **3NF** | No transitive dependencies. `Students` stores `SpecializationId`, not the track name — the name lives once in `Specializations`. `Enrollments` stores three foreign keys, never the student name, the course title or the term name. Changing a course title updates one row, not every enrolment of that course |

### Keys and relationships

- Every table has a **primary key**.
- Every relationship has a **foreign key**, 19 in total.
- `Prerequisites` has **two** foreign keys to `Courses`, which is how the many-to-many self
  relationship is expressed.
- `CourseRequestLines.RequestId` carries **`ON DELETE CASCADE`**, which is the composition
  `CourseRequest ◆ CourseRequestLine` enforced by the database.
- `Students.AdviserId` is **nullable**, which is the aggregation `Adviser ◇ Student`: a student
  exists without an adviser.

### Constraints that protect the data

Seven check constraints enforce domain rules at the database level, so the rules hold even if a row
is written by something other than the application:

| Constraint | Rule |
|---|---|
| `CK_Users_Role` | A role must be one of the three |
| `CK_Courses_CreditHours` | Credit hours between 1 and 6 |
| `CK_Prerequisites_RuleType` | A rule is a prerequisite or a corequisite |
| `CK_Prerequisites_NotSelf` | A course cannot require itself |
| `CK_Enrollments_Status` | A status must be one of the three |
| `CK_Enrollments_Grade` | **A completed enrolment must have a grade.** This is the same invariant that `Enrollment.MarkCompleted` enforces in the domain model, enforced twice |
| `CK_CourseRequests_Status` | A plan status must be one of the four |

`UQ_Prerequisites UNIQUE (CourseId, RequiredCourseId, RuleType)` prevents a duplicate rule.

---

## 5.2 Data layer implementation — 3 marks

The data layer is in `src/SCAIS/SCAIS/DataAccess/`.

```
IRepository<T>                        the contract
      ▲
      │ implements
SqlRepositoryBase<T>                  the shared ADO.NET plumbing
      ▲
      ├── UserRepository
      ├── CourseRepository
      ├── PrerequisiteRepository
      ├── EnrollmentRepository
      ├── CourseRequestRepository
      ├── NotificationRepository
      ├── SpecializationRepository
      └── SemesterRepository
              │
              ▼
       DatabaseManager  (Singleton)
```

**`IRepository<T>`** declares `GetById`, `GetAll`, `Add`, `Update` and `Delete`. The business layer
depends on this contract, not on a concrete repository.

**`SqlRepositoryBase<T>`** supplies the shared plumbing so that each repository contains only its
own SQL and its own mapping:

- `CreateConnection()` — obtains a connection from the Singleton
- `Query<TResult>(sql, map, parameters)` — runs a query and maps each row with a delegate
- `Execute(sql, parameters)` and `ExecuteInsert(sql, parameters)` — run a command and return either
  the affected row count or the new identity
- `Param(name, value)` — builds a parameter that tolerates null
- Typed readers: `ToInt`, `ToNullableInt`, `ToText`, `ToBool`, `ToNullableDate`, `IsNull`

**Every command is parameterised.** There is no string concatenation of user input anywhere in the
project, which removes SQL injection as a category of defect rather than mitigating it.

**Transactions are used where a write spans more than one table:**

| Operation | Why a transaction is needed |
|---|---|
| Creating an account | The `Users` row and the role row must both succeed, or the account would exist without being able to sign in to anything |
| Deleting an account | The role row must be deleted before the `Users` row it references |
| Saving a course plan | The `CourseRequests` row and every `CourseRequestLines` row must be written together, or a plan would exist with no courses |
| Approving a plan | The plan status and the new pending enrolments must agree |

**`DatabaseManager`** is the Singleton that owns the connection string, and it also bootstraps the
database: on first run it creates the database and applies the embedded schema and seed scripts, so
the application always has a usable database on any machine. The scripts are embedded in the
assembly and split on `GO` at runtime, because `GO` is an SSMS batch separator rather than valid
T-SQL.

---

## 5.3 CRUD operations — 2 marks

Every table is fully creatable, readable, updatable and deletable through the application, not only
through SQL.

| Operation | Where it is demonstrated in the running application |
|---|---|
| **Create** | Add a course and add a rule on `frmCourseCatalog`; create an account on `frmUserManagement`; submit a plan on `frmMyAcademicRecord` |
| **Read** | Every grid on every screen, including the two reports |
| **Update** | Save changes on `frmUserManagement`; update a course on `frmCourseCatalog`; approve or reject a plan on `frmCourseRequestReview`; assign or clear an adviser on `frmAssignAdvisees`; mark messages read on `frmMessages` |
| **Delete** | Delete an account, a course or a rule; and the cascade from a deleted plan to its lines |

The repositories that carry the writes are `UserRepository` (with `UpdatePassword` and
`SetStudentAdviser`), `CourseRepository` (with `CodeExists`), `PrerequisiteRepository`,
`EnrollmentRepository`, `CourseRequestRepository`, `NotificationRepository` (with `MarkAllAsRead`)
and `SpecializationRepository`.

---

## 5.4 Integration with the application — 2 marks

The database is not a separate artefact bolted on the side: the domain objects are loaded from it
and written back to it, and the rules the application enforces are the rules the schema stores.

**The design maps to the schema:**

| Class diagram | Table |
|---|---|
| `User` and its three subclasses | `Users` plus `Students`, `Advisers`, `Administrators` |
| `Student.Enrollments` | `Enrollments` |
| `Course` | `Courses` |
| `Prerequisite` | `Prerequisites` |
| `Specialization` | `Specializations` |
| `CourseRequest` | `CourseRequests` |
| `CourseRequestLine` | `CourseRequestLines` |
| `Semester` | `Semesters` |
| `Notification` | `Notifications` |

**The invariants are enforced in both places.** `Enrollment.MarkCompleted` refuses a completed
course with no grade, and `CK_Enrollments_Grade` refuses the same row at the database.
`Prerequisite` refuses a course that requires itself, and `CK_Prerequisites_NotSelf` refuses the
same row. This is deliberate: the object protects the in-memory model and the constraint protects
the stored data, and neither relies on the other.

**The seed data makes the application demonstrable.** Every table has at least 15 records
(`Users` 40, `Enrollments` 160, `CourseOfferings` 40), and the data is arranged so that the central
behaviour is visible on the first run: **ten students have completed IT210 Object Oriented Design
and are eligible for IT211, and twenty have not and are blocked.** That difference is what the
demonstration path exercises.

**The database is created by the application rather than shipped as a `.mdf`.** The brief asks for
the database to be a local data file included in the project. Shipping a `.mdf` that only attaches
on one machine is fragile, so instead the schema and seed scripts are embedded in the assembly and
applied automatically on first run. The `.mdf` that LocalDB creates can be detached and added to
the project if a physical file is required, and the connection string in `App.config` is the only
thing that would change.

---

## 5.5 Verification

The schema and the seed script were **executed against a live SQL Server engine** and every claim
above was then checked by query. The result and the full output are recorded in
[`../07-verification.md`](../07-verification.md). The checks included:

- 12 schema batches and 12 seed batches applied with no errors
- every table confirmed to hold at least 15 rows
- the seeded password hashes cross-checked against an independent SHA-256 implementation, proving
  that the seeded accounts can actually sign in
- the blocking scenario confirmed: exactly 20 students blocked from IT211 and 10 eligible
- all three enrolment statuses and all four request states present
- `CK_Enrollments_Grade` and `CK_Prerequisites_NotSelf` confirmed to reject invalid rows
- the `ON DELETE CASCADE` from a plan to its lines confirmed to work

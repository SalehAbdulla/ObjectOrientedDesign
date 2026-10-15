# 7 — Consistency Checklist

**Rubric: 2 marks for "UML consistency".** It is the cheapest two marks in the whole
project to lose, because it is lost by a name that does not match rather than by a diagram
that is wrong.

Work through this list with the exported EA diagrams open side by side before submitting.

---

## 7.1 Names are identical everywhere

| Check | Where to look |
|---|---|
| [ ] Class names in the sequence diagrams are the class names in the class diagram | `UserRepository`, `EligibilityValidator`, `CourseRequest`, `PrerequisiteRepository` appear in both |
| [ ] Operation names in the sequence diagrams are real operations | `Validate`, `Evaluate`, `Attach`, `Approve`, `GetSubmittedForAdviser`, `GetUserIdForStudent` |
| [ ] Parameter names match the code | `student`, `course`, `remark`, `observer`, `eligibilityNote` |
| [ ] Actor names match between the use case diagram and the sequence diagrams | Student, Adviser, Administrator |
| [ ] Table names in the ERD match `01_schema.sql` | `CourseRequestLines`, not `CourseRequestLine`; `Specializations`, not `Specialization` |
| [ ] Class names in the report match the diagrams | Every class named in the report prose also appears in a diagram |

### The two names that most easily drift

| Class diagram | Database | Note |
|---|---|---|
| `CourseRequestLine` | `CourseRequestLines` | The domain object is one line; the table holds many |
| `Specialization` | `Specializations` | Same reason |

This is not an inconsistency — the domain model names a type and the schema names a
collection — but the report must say so explicitly, or a marker may read it as one.

---

## 7.2 Every use case appears in the class diagram

Each use case must be traceable to an operation. Otherwise the class diagram cannot support
the use cases that were promised.

| Use case | Operation that supports it | Where |
|---|---|---|
| UC-01 Log in | `AuthenticationService.Login` | class diagram §2.3 |
| UC-02 Redirect to role dashboard | `DashboardFactory.Create`, `User.GetDashboardTitle` | §2.3, §2.1 |
| UC-03 Manage user accounts | `UserRepository.Add`, `Update`, `Delete`, `UpdatePassword` | §2.3 |
| UC-04 Maintain course catalogue | `CourseRepository.Add`, `Update`, `Delete` | §2.3 |
| UC-05 Define prerequisite rules | `PrerequisiteRepository.Add`, `Delete` | §2.3 |
| UC-06 Assign advisees | `UserRepository.SetStudentAdviser`, `Adviser.AssignAdvisee` | §2.3, §2.1 |
| UC-07 Define offerings | `CourseOfferingRepository.Add`, `Update`, `Delete`, `CourseOffering.Capacity` | §2.3, §2.1 |
| UC-08 View record and status | `Student.GetCompletedCourses`, `GetInProgressCourses`, `GetPendingCourses` | §2.1 |
| UC-09 View eligible courses | `CourseAdvisingService.GetEligibleCourses`, `EligibilityValidator.ValidateCatalogue` | §2.3 |
| UC-10 Submit courses for review | `CourseAdvisingService.SubmitRequest`, `CourseRequest.Submit` | §2.3, §2.1 |
| UC-11 View approved courses and remarks | `CourseAdvisingService.GetRequestsForStudent`, `CourseRequest.AdviserRemark` | §2.3, §2.1 |
| UC-12 View assigned advisees | `UserRepository.GetStudentsByAdviser` | §2.3 |
| UC-13 Access advisee history | `Student.Enrollments`, `ReportService.GetCourseProgression` | §2.1, §2.3 |
| UC-14 Recommend courses | `CourseAdvisingService.GetEligibleCourses` | §2.3 |
| UC-15 Approve or modify a plan | `CourseAdvisingService.ApproveRequest`, `CourseRequest.Approve` | §2.3, §2.1 |
| UC-16 Course progression report | `ReportService.GetCourseProgression` | §2.3 |
| UC-17 Compliance report | `ReportService.GetPrerequisiteCompliance` | §2.3 |
| UC-18 Validate prerequisites | `EligibilityValidator.Validate`, `PrerequisiteRule.Evaluate` | §2.3 |
| UC-19 Notify student | `IUserObserver.OnRequestStatusChanged`, `StudentNotificationService` | §2.1, §5.2 |
| UC-20 Authenticate user | `User.Authenticate` | §2.1 |
| UC-21 View own messages | `NotificationRepository.GetForUser` | §2.3 |

---

## 7.3 The class diagram and the ERD agree

| Class diagram | ERD | Check |
|---|---|---|
| `User` (abstract) | `Users` | [ ] The three subclasses map to `Students`, `Advisers`, `Administrators`, each with a unique `UserId` foreign key |
| `Student.Enrollments` `1 — 0..*` | `Students 1 — 0..* Enrollments` | [ ] Same multiplicity |
| `Enrollment * — 1 Course` | `Courses 1 — 0..* Enrollments` | [ ] The same relationship read from the other end |
| `Adviser ◇ Student` `1 — 0..*` | `Advisers 1 — 0..* Students` | [ ] Same multiplicity; the aggregation is a nullable foreign key |
| `CourseRequest ◆ CourseRequestLine` `1 — 1..*` | `CourseRequests 1 — 1..* CourseRequestLines` | [ ] The composition is expressed as `ON DELETE CASCADE` |
| `Course` ↔ `Prerequisite` both ways | `Courses` referenced twice by `Prerequisites` | [ ] The many-to-many self relationship is visible in both |
| `CourseRequest 1 — 0..* IUserObserver` | no table | [ ] Correct: observers are runtime objects and are not persisted |

---

## 7.4 The report and the diagrams agree

- [ ] Every diagram is referenced by number in the report prose
- [ ] The counts quoted in the report match the diagrams: 3 actors, 21 use cases,
      14 domain classes, 3 interfaces, 13 tables, 3 patterns, 4 sequence diagrams
- [ ] Every OO concept claim in the report names a class that appears in the class diagram
- [ ] Every design pattern section refers to its own diagram
- [ ] The screenshots in the user interface section show the screens named in the use case
      catalogue

---

## 7.5 Final check against the running application

The most convincing consistency check is that the diagrams describe what the application
actually does:

- [ ] Sign in as each of the three roles and confirm the dashboards match UC-02
- [ ] Confirm a blocked course names the missing prerequisite, as SD-2 says
- [ ] Confirm approving a plan writes a message **and** an audit line, as SD-3 says
- [ ] Confirm the two reports produce the columns that §7.2 implies
- [ ] Confirm the 13 tables exist with the names the ERD uses

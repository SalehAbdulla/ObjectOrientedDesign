# 1. Project Overview

**Rubric: 10 marks — problem statement 3, project objectives 2, target users 2, main
system functions 3.**

---

## 1.1 Project Background

Bahrain Institute of Technology (BIT) is introducing the **Smart Course Advising
Information System (SCAIS)** for its IT programmes. The institute offers four flagship
specializations — Programming, Networking, Cybersecurity and Database — and each has a
curriculum in which some subjects may only be taken after others have been passed.

Course advising at BIT is currently carried out by hand. An adviser sits with a student,
looks up the student's transcript, and works through the curriculum on paper or in a
spreadsheet to decide which subjects the student may register for. The institute has
identified this as the weakest point in the student journey, and has commissioned SCAIS to
replace it.

The system is delivered as a Windows desktop application built with C# and Windows Forms
on .NET Framework 4.8, storing its data in SQL Server LocalDB. The institute has stated
that web and mobile front ends are planned for a later phase, which is a design constraint
rather than a future wish: the business logic must not be tied to the desktop user
interface.

---

## 1.2 Problem Statement

Three specific problems were identified.

**Problem 1 — Prerequisite checking is manual, slow and error-prone.**
An adviser must cross-check every requested subject against the curriculum rules. With
chains such as *Database 1 → Database 2 → Database Administration*, and *Advanced
Networking → Networking and Data Communications 2*, a single missed rule lets a student
register for a subject they cannot pass. Because the check is manual it is also
inconsistent: two advisers can reach different conclusions about the same student.

**Problem 2 — Students register for subjects they are not eligible for.**
A student who registers for a subject whose prerequisite has not been passed will usually
fail it, or be removed from it partway through the semester. Both outcomes waste a semester
of the student's time, consume a seat another student could have used, and create
administrative work to undo the registration.

**Problem 3 — There is no single view of a student's academic position.**
A student's record is spread across transcript, registration and curriculum documents.
There is no one place showing which subjects are **completed**, which are **in progress**
and which are **still pending**, so academic planning is guesswork. Advisers cannot see at
a glance how far through a specialization an advisee is, and students cannot see what their
choices are leading towards.

---

## 1.3 Project Objectives

1. **Automate eligibility validation.** Check every requested subject against its
   prerequisites and corequisites, and refuse or warn about any subject the student is not
   eligible to take.
2. **Provide a single role-based portal.** Administrators, advisers and students all sign
   in through one screen and are taken to the functions that belong to their role.
3. **Track course status.** Show each student's courses as completed, in progress or
   pending, so that both the student and the adviser can see the position at a glance.
4. **Give advisers a recommendation tool.** Let an adviser see, for any advisee, which
   subjects that student is currently eligible to take.
5. **Produce planning reports.** Produce at least two reports that support academic
   planning: one for the progression of a student, one for compliance with the prerequisite
   rules.

---

## 1.4 Target Users

| User | Who they are | What they need from SCAIS |
|---|---|---|
| **Administrator** | Registry and academic office staff who maintain the system | Maintain user accounts, the course catalogue and the prerequisite rules; assign students to advisers; define course offerings per semester; produce the compliance report |
| **Adviser** | Faculty members who manage a group of advisees | See their advisees and each advisee's academic history and specialization; see which subjects an advisee is eligible for; approve, modify or reject submitted course plans; produce progression reports |
| **Student** | IT students with a declared specialization | See their own academic record and the status of every course; see which subjects they are eligible for in the coming semester; submit preferred courses for review; see what their adviser approved and any remarks |

---

## 1.5 Main System Functions

The system provides 21 use cases. They are grouped below by the actor that triggers them.

### All roles
1. Sign in through a single login screen (UC-01)
2. Be redirected to the dashboard for their role (UC-02)
3. Have their credentials verified (UC-20)

### Administrator
4. Manage user accounts — create, edit, deactivate (UC-03)
5. Maintain the course catalogue — add, update, delete courses (UC-04)
6. Define prerequisite and corequisite rules (UC-05)
7. Assign advisees to advisers, and reassign them (UC-06)
8. Define course offerings per semester (UC-07)
9. Generate the course progression report (UC-16)
10. Generate the prerequisite compliance report (UC-17)

### Adviser
11. View assigned advisees (UC-12)
12. Access an advisee's academic history and specialization (UC-13)
13. Recommend courses based on eligibility (UC-14)
14. Approve or modify a submitted course plan (UC-15)

### Student
15. View their academic record and the status of each course (UC-08)
16. View the courses they are eligible for (UC-09)
17. Submit preferred courses for review (UC-10)
18. View approved courses and the adviser's remarks (UC-11)
19. View their own messages (UC-21)

### System
20. Validate prerequisites and corequisites before a subject is accepted (UC-18)
21. Notify the student when a decision is made on their plan (UC-19)

### The two headline functions

Two functions carry the business case and are worth naming separately.

**Course validation.** When a student adds a subject to a plan, or views the list of
subjects they may take, SCAIS runs five eligibility rules over it and shows the verdict
with the reason. A blocked subject names the specific course that is missing, so the
student is told what to do rather than simply refused. This is the behaviour the brief asks
for when it says the system should *"block or warn"*.

**Course status tracking.** Every subject a student has any connection with is held as one
of three states — completed with a grade, in progress, or pending — and both the student's
own screen and the adviser's screen are built from that single state.

---

## 1.6 Scope and assumptions

The brief states that requirements may be expanded and assumptions made. The assumptions
below were made, and each is a deliberate decision rather than an oversight.

| Assumption | Reason |
|---|---|
| The four flagship specializations named in the brief are the ones the curriculum uses, and eleven further tracks exist in the data for future expansion | The brief names four tracks but also requires at least fifteen records in every table. The extra tracks satisfy the data requirement without changing the curriculum that the four flagship tracks define |
| A student may request subjects for the coming semester only | Planning further ahead is not part of the brief, and allowing it would have made the eligibility rules considerably more complex |
| The credit ceiling for a semester is 18 hours | A realistic value for the programme. It is a configurable property of the rule rather than a constant, so it can be changed without a code change |
| Approval of a plan creates a *pending* enrolment for each eligible subject in it | This is what makes an approved subject appear in the student's record as "yet to be taken", which is what the status tracking requirement asks for |
| Subjects that failed the eligibility check are not enrolled when a plan is approved | An adviser must not be able to push a student into a subject they cannot legally take |
| Administrators cannot approve course plans | Approval is an academic judgement that belongs to the adviser. The separation is enforced in the domain model, not only in the user interface |
| A student's adviser is assigned by an administrator, and a student may temporarily have none | The brief makes assigning advisees an administrator function. A student with no adviser cannot submit a plan until one is assigned, which the system reports clearly rather than failing |

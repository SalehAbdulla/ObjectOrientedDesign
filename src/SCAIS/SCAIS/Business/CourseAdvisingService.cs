using System;
using System.Collections.Generic;
using SCAIS.DataAccess;
using SCAIS.Domain;
using SCAIS.Interfaces;
using SCAIS.Observers;

namespace SCAIS.Business
{
    /// <summary>
    /// The service behind the advising workflow: it recommends courses, builds a plan,
    /// submits it and records the adviser's decision.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is where the Observer pattern is actually wired up. When a plan is
    /// submitted, approved or rejected, the service attaches the observers to the plan
    /// before the transition, so the transition itself is what triggers the
    /// notifications. The plan never learns who is listening.
    /// </para>
    /// <para>
    /// The service also owns the work that spans more than one table: approving a plan
    /// changes the plan and creates the enrolments for the approved courses, and both
    /// must succeed or neither should.
    /// </para>
    /// </remarks>
    public class CourseAdvisingService
    {
        /// <summary>Reads and writes course plans.</summary>
        private readonly CourseRequestRepository requests;

        /// <summary>Reads and writes the academic record.</summary>
        private readonly EnrollmentRepository enrollments;

        /// <summary>Reads and writes accounts.</summary>
        private readonly UserRepository users;

        /// <summary>Reads the course catalogue.</summary>
        private readonly CourseRepository courses;

        /// <summary>Reads the academic terms.</summary>
        private readonly SemesterRepository semesters;

        /// <summary>Runs the eligibility rules.</summary>
        private readonly EligibilityValidator validator;

        /// <summary>Creates the service with its own collaborators.</summary>
        public CourseAdvisingService()
            : this(new CourseRequestRepository(), new EnrollmentRepository(), new UserRepository(),
                   new CourseRepository(), new SemesterRepository(), new EligibilityValidator())
        {
        }

        /// <summary>Creates the service with supplied collaborators.</summary>
        /// <param name="requests">The plan repository.</param>
        /// <param name="enrollments">The enrolment repository.</param>
        /// <param name="users">The account repository.</param>
        /// <param name="courses">The course repository.</param>
        /// <param name="semesters">The semester repository.</param>
        /// <param name="validator">The eligibility validator.</param>
        public CourseAdvisingService(CourseRequestRepository requests, EnrollmentRepository enrollments,
                                     UserRepository users, CourseRepository courses,
                                     SemesterRepository semesters, EligibilityValidator validator)
        {
            if (requests == null) { throw new ArgumentNullException("requests"); }
            if (enrollments == null) { throw new ArgumentNullException("enrollments"); }
            if (users == null) { throw new ArgumentNullException("users"); }
            if (courses == null) { throw new ArgumentNullException("courses"); }
            if (semesters == null) { throw new ArgumentNullException("semesters"); }
            if (validator == null) { throw new ArgumentNullException("validator"); }

            this.requests = requests;
            this.enrollments = enrollments;
            this.users = users;
            this.courses = courses;
            this.semesters = semesters;
            this.validator = validator;
        }

        /// <summary>
        /// Loads a student together with the academic record that the eligibility rules
        /// need. Loading the record once here is what allows every later check to run in
        /// memory rather than as a database round trip.
        /// </summary>
        /// <param name="studentId">The Students key to load.</param>
        /// <returns>The student, or null when the student does not exist.</returns>
        public Student LoadStudent(int studentId)
        {
            foreach (Student candidate in this.users.GetStudents())
            {
                if (candidate.StudentId == studentId)
                {
                    candidate.LoadAcademicRecord(this.enrollments.GetByStudent(studentId));
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the courses the student is eligible to take, each with its verdict.
        /// The catalogue is the student's track plus the core subjects every student takes.
        /// </summary>
        /// <param name="student">The student to check.</param>
        /// <returns>One entry per catalogue course.</returns>
        public List<CourseEligibility> GetEligibleCourses(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            int specializationId = student.Specialization == null
                ? 0
                : student.Specialization.SpecializationId;

            List<Course> catalogue = specializationId > 0
                ? this.courses.GetForSpecialization(specializationId)
                : this.courses.GetAll();

            return this.validator.ValidateCatalogue(student, catalogue);
        }

        /// <summary>Returns the term a student should be planning for.</summary>
        /// <returns>The coming semester, or the most recent one when the calendar has run out.</returns>
        public Semester GetPlanningSemester()
        {
            return this.semesters.GetNext();
        }

        /// <summary>Creates an empty plan for a student in a semester.</summary>
        /// <param name="student">The student who owns the plan.</param>
        /// <param name="semesterId">The semester being planned.</param>
        /// <returns>A new plan in the Draft state.</returns>
        public CourseRequest CreateRequest(Student student, int semesterId)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            int? adviserId = student.Adviser == null ? (int?)null : student.Adviser.AdviserId;

            return new CourseRequest(0, student.StudentId, semesterId, adviserId);
        }

        /// <summary>
        /// Adds a course to a plan, recording the eligibility verdict against the line.
        /// The course is added even when it is refused, because the student must be able
        /// to see why it was refused - which is the "block or warn" requirement.
        /// </summary>
        /// <param name="request">The plan being built.</param>
        /// <param name="student">The student the plan belongs to.</param>
        /// <param name="course">The course to add.</param>
        /// <returns>The verdict, so the screen can warn the student immediately.</returns>
        public EligibilityResult AddCourseToRequest(CourseRequest request, Student student, Course course)
        {
            if (request == null) { throw new ArgumentNullException("request"); }
            if (student == null) { throw new ArgumentNullException("student"); }
            if (course == null) { throw new ArgumentNullException("course"); }

            EligibilityResult result = this.validator.Validate(student, course);
            request.AddCourse(course, result.Reason);

            return result;
        }

        /// <summary>Submits a plan for review and stores it.</summary>
        /// <param name="request">The plan to submit.</param>
        /// <returns>True when the plan was saved.</returns>
        public bool SubmitRequest(CourseRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            this.AttachObservers(request);

            // The domain object performs the transition and, because the observers are
            // attached, the transition itself is what produces the notification.
            request.Submit();

            return this.requests.Add(request);
        }

        /// <summary>
        /// Approves a plan, stores the decision, and creates the enrolments for the
        /// courses that were approved and are eligible.
        /// </summary>
        /// <param name="request">The plan to approve.</param>
        /// <param name="remark">The adviser's remark for the student.</param>
        /// <returns>True when the decision was saved.</returns>
        public bool ApproveRequest(CourseRequest request, string remark)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            this.AttachObservers(request);
            request.Approve(remark);

            bool saved = this.requests.Update(request);

            if (saved)
            {
                this.CreateEnrollmentsForApprovedPlan(request);
            }

            return saved;
        }

        /// <summary>Rejects a plan and stores the reason.</summary>
        /// <param name="request">The plan to reject.</param>
        /// <param name="remark">The reason for the student.</param>
        /// <returns>True when the decision was saved.</returns>
        public bool RejectRequest(CourseRequest request, string remark)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            this.AttachObservers(request);
            request.Reject(remark);

            return this.requests.Update(request);
        }

        /// <summary>Returns the plans awaiting one adviser's decision.</summary>
        /// <param name="adviserId">The adviser whose queue is wanted.</param>
        /// <returns>The submitted plans with their courses loaded.</returns>
        public List<CourseRequest> GetSubmittedRequestsForAdviser(int adviserId)
        {
            return this.requests.GetSubmittedForAdviser(adviserId);
        }

        /// <summary>Returns every plan submitted by one student.</summary>
        /// <param name="studentId">The student whose plans are wanted.</param>
        /// <returns>The plans, most recent first.</returns>
        public List<CourseRequest> GetRequestsForStudent(int studentId)
        {
            return this.requests.GetByStudent(studentId);
        }

        /// <summary>Returns the account that owns a student record.</summary>
        /// <param name="studentId">The Students key to resolve.</param>
        /// <returns>The owning UserId, or zero.</returns>
        public int GetUserIdForStudent(int studentId)
        {
            return this.users.GetUserIdForStudent(studentId);
        }

        /// <summary>
        /// Registers the observers that must be told when a plan changes state.
        /// </summary>
        /// <remarks>
        /// This is the single method that couples the workflow to the Observer pattern.
        /// If the requirement later grows - for example "the registrar must also be
        /// notified" - it is one more Attach call here and one new class, and neither
        /// <see cref="CourseRequest"/> nor the advisers' screen changes at all.
        /// </remarks>
        /// <param name="request">The plan about to change state.</param>
        private void AttachObservers(CourseRequest request)
        {
            request.Attach(new StudentNotificationService());
            request.Attach(new AuditLogService());
        }

        /// <summary>
        /// Creates a Pending enrolment for every eligible course in an approved plan, so
        /// that the course immediately appears in the student's record as "yet to be
        /// taken".
        /// </summary>
        /// <remarks>
        /// Courses that failed the eligibility check are skipped on purpose: an adviser
        /// approving a plan must not be able to push a student into a course they cannot
        /// legally take.
        /// </remarks>
        /// <param name="request">The approved plan.</param>
        private void CreateEnrollmentsForApprovedPlan(CourseRequest request)
        {
            foreach (CourseRequestLine line in request.Lines)
            {
                if (!line.IsEligible)
                {
                    continue;
                }

                Enrollment enrollment = new Enrollment(
                    0, request.StudentId, line.Course, request.SemesterId,
                    EnrollmentStatus.Pending, null);

                this.enrollments.Add(enrollment);
            }
        }
    }
}

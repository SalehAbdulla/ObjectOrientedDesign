using System;
using System.Collections.Generic;
using SCAIS.Interfaces;

namespace SCAIS.Domain
{
    /// <summary>
    /// A course plan prepared by a student and submitted to an adviser for approval.
    /// </summary>
    /// <remarks>
    /// <para><b>Composition.</b> A request owns its <see cref="CourseRequestLine"/>
    /// objects. The lines are created, exposed read-only, and destroyed with the
    /// request, so this is a composition rather than an aggregation.</para>
    ///
    /// <para><b>Design pattern: Observer (the Subject).</b> When the status of a
    /// request changes, several unrelated parts of the system must react: the student
    /// has to be notified, and an audit entry has to be written. Instead of calling
    /// those parts directly - which would force this domain class to know about
    /// notifications and logging - the request keeps a list of
    /// <see cref="IUserObserver"/> objects and announces the change to all of them.
    /// The request therefore has no idea who is listening, which is what allows a new
    /// reaction to be added later without editing this class.</para>
    ///
    /// <para><b>Encapsulation.</b> <see cref="Status"/>, <see cref="SubmittedDate"/>
    /// and <see cref="AdviserRemark"/> are read-only properties. The only way to change
    /// them is <see cref="Submit"/>, <see cref="Approve"/> or <see cref="Reject"/>, so a
    /// request can never be Approved with no remark, or Submitted without a date.</para>
    /// </remarks>
    public class CourseRequest
    {
        /// <summary>Primary key of the CourseRequests row.</summary>
        private readonly int requestId;

        /// <summary>The student who prepared the plan.</summary>
        private readonly int studentId;

        /// <summary>The semester the plan is for.</summary>
        private readonly int semesterId;

        /// <summary>The courses in the plan.</summary>
        private readonly List<CourseRequestLine> lines = new List<CourseRequestLine>();

        /// <summary>The observers registered for status changes.</summary>
        private readonly List<IUserObserver> observers = new List<IUserObserver>();

        /// <summary>The adviser who reviewed the plan, or null while unreviewed.</summary>
        private int? adviserId;

        /// <summary>Current state of the plan. Private, with no public setter.</summary>
        private RequestStatus status;

        /// <summary>When the plan was sent for review. Private, with no public setter.</summary>
        private DateTime? submittedDate;

        /// <summary>The adviser's comment. Private, with no public setter.</summary>
        private string adviserRemark;

        /// <summary>Creates a course plan in the Draft state.</summary>
        /// <param name="requestId">Primary key, or zero when not yet saved.</param>
        /// <param name="studentId">The student who owns the plan.</param>
        /// <param name="semesterId">The semester the plan is for.</param>
        /// <param name="adviserId">The reviewing adviser, or null.</param>
        public CourseRequest(int requestId, int studentId, int semesterId, int? adviserId)
        {
            this.requestId = requestId;
            this.studentId = studentId;
            this.semesterId = semesterId;
            this.adviserId = adviserId;
            this.status = RequestStatus.Draft;
        }

        /// <summary>Gets the primary key of the request.</summary>
        public int RequestId
        {
            get { return this.requestId; }
        }

        /// <summary>Gets the student who owns the plan.</summary>
        public int StudentId
        {
            get { return this.studentId; }
        }

        /// <summary>Gets the semester the plan is for.</summary>
        public int SemesterId
        {
            get { return this.semesterId; }
        }

        /// <summary>Gets or sets the reviewing adviser.</summary>
        public int? AdviserId
        {
            get { return this.adviserId; }
            set { this.adviserId = value; }
        }

        /// <summary>Gets the current state. Changed only through the transition methods.</summary>
        public RequestStatus Status
        {
            get { return this.status; }
        }

        /// <summary>Gets when the plan was submitted, or null while it is a draft.</summary>
        public DateTime? SubmittedDate
        {
            get { return this.submittedDate; }
        }

        /// <summary>Gets the adviser's comment. Set only by Approve or Reject.</summary>
        public string AdviserRemark
        {
            get { return this.adviserRemark; }
        }

        /// <summary>
        /// Gets the requested courses as a read-only view. Lines are added through
        /// <see cref="AddCourse"/> so they always belong to this request.
        /// </summary>
        public IList<CourseRequestLine> Lines
        {
            get { return this.lines.AsReadOnly(); }
        }

        /// <summary>
        /// Adds a course to the plan. The line is created by the request, which is what
        /// makes the relationship a composition.
        /// </summary>
        /// <param name="course">The course being requested.</param>
        /// <param name="eligibilityNote">The verdict of the eligibility check.</param>
        /// <returns>The created line.</returns>
        public CourseRequestLine AddCourse(Course course, string eligibilityNote)
        {
            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            foreach (CourseRequestLine existing in this.lines)
            {
                if (existing.Course.CourseId == course.CourseId)
                {
                    return existing;
                }
            }

            CourseRequestLine line = new CourseRequestLine(0, this.requestId, course, eligibilityNote);
            this.lines.Add(line);
            return line;
        }

        /// <summary>Returns the number of courses in the plan.</summary>
        public int GetCourseCount()
        {
            return this.lines.Count;
        }

        /// <summary>
        /// Returns the total credit hours requested. Used by the credit limit rule and
        /// shown on the review screen.
        /// </summary>
        public int GetTotalCreditHours()
        {
            int total = 0;

            foreach (CourseRequestLine line in this.lines)
            {
                total += line.Course.CreditHours;
            }

            return total;
        }

        /// <summary>
        /// Returns true when at least one course in the plan failed the eligibility
        /// check, which is what makes the screen warn the student.
        /// </summary>
        public bool ContainsBlockedCourse()
        {
            foreach (CourseRequestLine line in this.lines)
            {
                if (!line.IsEligible)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers an observer to be told when this request changes status.
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        public void Attach(IUserObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }

            if (!this.observers.Contains(observer))
            {
                this.observers.Add(observer);
            }
        }

        /// <summary>
        /// Removes a previously registered observer. Failing to detach an observer keeps
        /// the subject holding a reference to it, so a screen that attaches one must
        /// detach it when it closes.
        /// </summary>
        /// <param name="observer">The observer to remove.</param>
        /// <returns>True when the observer was registered and has been removed.</returns>
        public bool Detach(IUserObserver observer)
        {
            return this.observers.Remove(observer);
        }

        /// <summary>Returns the number of observers currently registered.</summary>
        public int GetObserverCount()
        {
            return this.observers.Count;
        }

        /// <summary>
        /// Moves the plan from Draft to Submitted and announces the change.
        /// A plan with no courses cannot be submitted.
        /// </summary>
        public void Submit()
        {
            if (this.lines.Count == 0)
            {
                throw new InvalidOperationException("A course plan must contain at least one course.");
            }

            if (this.status != RequestStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Only a draft plan can be submitted. The current state is " + this.status + ".");
            }

            this.status = RequestStatus.Submitted;
            this.submittedDate = DateTime.Now;
            this.NotifyObservers();
        }

        /// <summary>
        /// Approves the plan, records the adviser's remark and announces the change.
        /// </summary>
        /// <param name="remark">The adviser's comment, which must not be blank.</param>
        public void Approve(string remark)
        {
            this.ChangeReviewOutcome(RequestStatus.Approved, remark);
        }

        /// <summary>
        /// Rejects the plan, records the adviser's reason and announces the change.
        /// </summary>
        /// <param name="remark">The reason for the rejection, which must not be blank.</param>
        public void Reject(string remark)
        {
            this.ChangeReviewOutcome(RequestStatus.Rejected, remark);
        }

        /// <summary>
        /// Announces the current status to every registered observer.
        /// </summary>
        /// <remarks>
        /// The loop is guarded so that one failing observer cannot stop the others from
        /// being notified, which is one of the known limitations of the pattern.
        /// </remarks>
        private void NotifyObservers()
        {
            List<IUserObserver> snapshot = new List<IUserObserver>(this.observers);

            for (int index = 0; index < snapshot.Count; index++)
            {
                try
                {
                    snapshot[index].OnRequestStatusChanged(this, this.status);
                }
                catch (Exception exception)
                {
                    // An observer must never be able to break the state change that has
                    // already happened, so the failure is recorded and swallowed.
                    System.Diagnostics.Debug.WriteLine(
                        "Observer " + snapshot[index].GetType().Name + " failed: " + exception.Message);
                }
            }
        }

        /// <summary>
        /// Shared implementation of Approve and Reject, so the guard and the
        /// notification happen exactly once and in one place.
        /// </summary>
        private void ChangeReviewOutcome(RequestStatus outcome, string remark)
        {
            if (string.IsNullOrEmpty(remark) || remark.Trim().Length == 0)
            {
                throw new ArgumentException("A review must include a remark for the student.", "remark");
            }

            if (this.status != RequestStatus.Submitted)
            {
                throw new InvalidOperationException(
                    "Only a submitted plan can be reviewed. The current state is " + this.status + ".");
            }

            this.status = outcome;
            this.adviserRemark = remark.Trim();
            this.NotifyObservers();
        }

        /// <summary>Returns the request number, state and course count.</summary>
        public override string ToString()
        {
            return string.Format("Request #{0} - {1} - {2} course(s)",
                this.requestId, this.status, this.lines.Count);
        }
    }
}

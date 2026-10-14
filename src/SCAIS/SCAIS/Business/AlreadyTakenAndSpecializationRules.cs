using System;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Business
{
    /// <summary>
    /// Refuses a course the student has already passed, or is already taking.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (a Concrete Strategy).</b></para>
    /// <para>
    /// This rule needs no repository at all: everything it needs is already in the
    /// student's academic record. It is included because it is the smallest possible
    /// demonstration that the strategies are independent - a rule can be added to the
    /// pipeline without adding a database query, and without the validator changing.
    /// </para>
    /// </remarks>
    public class AlreadyCompletedRule : IEligibilityRule
    {
        /// <summary>Gets the name of the rule.</summary>
        public string RuleName
        {
            get { return "Already taken"; }
        }

        /// <summary>
        /// Returns a refusal when the student has already passed the course or is
        /// currently enrolled in it.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>The verdict.</returns>
        public EligibilityResult Evaluate(Student student, Course course)
        {
            if (student == null || course == null)
            {
                return EligibilityResult.Fail(this.RuleName, "the student or course is missing");
            }

            if (student.HasCompleted(course.CourseId))
            {
                return EligibilityResult.Fail(this.RuleName, "already completed");
            }

            if (student.HasTakenOrIsTaking(course.CourseId))
            {
                return EligibilityResult.Fail(this.RuleName, "already in progress");
            }

            return EligibilityResult.Pass(this.RuleName);
        }
    }

    /// <summary>
    /// Refuses a specialised course that belongs to a different track from the one the
    /// student has declared.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (a Concrete Strategy).</b></para>
    /// <para>
    /// Core subjects are shared by every track, so they pass this rule by definition.
    /// The rule reads the track from the <see cref="Course"/> object rather than from a
    /// join, because a null track already means "core" in the domain model.
    /// </para>
    /// </remarks>
    public class SpecializationRule : IEligibilityRule
    {
        /// <summary>Gets the name of the rule.</summary>
        public string RuleName
        {
            get { return "Specialization"; }
        }

        /// <summary>
        /// Returns a refusal when the course belongs to a track other than the student's.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>The verdict.</returns>
        public EligibilityResult Evaluate(Student student, Course course)
        {
            if (student == null || course == null)
            {
                return EligibilityResult.Fail(this.RuleName, "the student or course is missing");
            }

            // A core subject belongs to every track, so it always passes.
            if (course.IsCoreSubject() || course.Specialization == null)
            {
                return EligibilityResult.Pass(this.RuleName);
            }

            if (student.Specialization == null)
            {
                return EligibilityResult.Fail(this.RuleName, "no track has been declared");
            }

            if (student.Specialization.SpecializationId != course.Specialization.SpecializationId)
            {
                return EligibilityResult.Fail(this.RuleName,
                    "belongs to the " + course.Specialization.Name + " track");
            }

            return EligibilityResult.Pass(this.RuleName);
        }
    }
}

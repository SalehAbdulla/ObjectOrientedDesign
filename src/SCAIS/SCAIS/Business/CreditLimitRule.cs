using System;
using System.Collections.Generic;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Business
{
    /// <summary>
    /// Refuses a course when adding it would take the student past the semester credit
    /// ceiling.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (a Concrete Strategy).</b></para>
    /// <para>
    /// The ceiling is a constructor parameter rather than a constant so that it can be
    /// changed without editing the rule. That is the practical advantage of the Strategy
    /// pattern: the algorithm is a replaceable object, not a hard-coded branch buried in
    /// a validator.
    /// </para>
    /// </remarks>
    public class CreditLimitRule : IEligibilityRule
    {
        /// <summary>The default ceiling for a semester.</summary>
        public const int DefaultMaximumCredits = 18;

        /// <summary>The ceiling this instance enforces.</summary>
        private readonly int maximumCredits;

        /// <summary>Creates the rule with the default ceiling.</summary>
        public CreditLimitRule()
            : this(DefaultMaximumCredits)
        {
        }

        /// <summary>Creates the rule with a supplied ceiling.</summary>
        /// <param name="maximumCredits">The maximum credit hours allowed in a semester.</param>
        public CreditLimitRule(int maximumCredits)
        {
            if (maximumCredits <= 0)
            {
                throw new ArgumentOutOfRangeException("maximumCredits",
                    "The credit ceiling must be greater than zero.");
            }

            this.maximumCredits = maximumCredits;
        }

        /// <summary>Gets the name of the rule.</summary>
        public string RuleName
        {
            get { return "Credit limit"; }
        }

        /// <summary>Gets the ceiling this instance enforces.</summary>
        public int MaximumCredits
        {
            get { return this.maximumCredits; }
        }

        /// <summary>
        /// Returns a refusal when the student's heaviest semester plus this course would
        /// exceed the ceiling.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>The verdict and, when refused, the resulting credit total.</returns>
        public EligibilityResult Evaluate(Student student, Course course)
        {
            if (student == null || course == null)
            {
                return EligibilityResult.Fail(this.RuleName, "the student or course is missing");
            }

            int committed = GetHighestSemesterLoad(student);
            int resulting = committed + course.CreditHours;

            if (resulting > this.maximumCredits)
            {
                return EligibilityResult.Fail(this.RuleName, string.Format(
                    "would total {0} credit hours, above the limit of {1}",
                    resulting, this.maximumCredits));
            }

            return EligibilityResult.Pass(this.RuleName);
        }

        /// <summary>
        /// Returns the largest credit load the student already carries in any one
        /// unfinished semester.
        /// </summary>
        /// <param name="student">The student to measure.</param>
        /// <returns>The highest committed credit total.</returns>
        public static int GetHighestSemesterLoad(Student student)
        {
            if (student == null)
            {
                return 0;
            }

            Dictionary<int, int> loads = new Dictionary<int, int>();

            foreach (Enrollment enrollment in student.Enrollments)
            {
                if (enrollment.Status == EnrollmentStatus.Completed || enrollment.Course == null)
                {
                    continue;
                }

                int current;
                loads.TryGetValue(enrollment.SemesterId, out current);
                loads[enrollment.SemesterId] = current + enrollment.Course.CreditHours;
            }

            int highest = 0;

            foreach (KeyValuePair<int, int> entry in loads)
            {
                if (entry.Value > highest)
                {
                    highest = entry.Value;
                }
            }

            return highest;
        }
    }
}

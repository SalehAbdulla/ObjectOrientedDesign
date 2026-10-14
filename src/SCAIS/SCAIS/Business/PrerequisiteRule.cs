using System;
using System.Collections.Generic;
using SCAIS.DataAccess;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Business
{
    /// <summary>
    /// Refuses a course when the student has not yet passed one of its prerequisites.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (a Concrete Strategy).</b></para>
    /// <para>
    /// This is the rule that implements the central requirement of the business case:
    /// "if a student has not passed the required prerequisite, the system should block
    /// or warn them from enrolling in the dependent course." The two examples given in
    /// the brief - Database 1 before Database 2, and Advanced Networking before
    /// Networking and Data Communications 2 - are both enforced here.
    /// </para>
    /// <para>
    /// The rule holds the whole verdict, not just a boolean. When it refuses a course it
    /// names the exact subject that is missing, which is what turns a refusal into a
    /// warning the student can act on.
    /// </para>
    /// <para>
    /// The rule reads the student's academic record from the <see cref="Student"/>
    /// object rather than querying for it. That is deliberate: the record is loaded once
    /// when the student signs in, so checking twenty courses costs twenty in-memory
    /// scans rather than twenty round trips to the database.
    /// </para>
    /// </remarks>
    public class PrerequisiteRule : IEligibilityRule
    {
        /// <summary>The repository that supplies the curriculum rules.</summary>
        private readonly PrerequisiteRepository repository;

        /// <summary>Creates the rule with its own repository.</summary>
        public PrerequisiteRule()
            : this(new PrerequisiteRepository())
        {
        }

        /// <summary>Creates the rule with a supplied repository.</summary>
        /// <param name="repository">The repository to read curriculum rules from.</param>
        public PrerequisiteRule(PrerequisiteRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
        }

        /// <summary>Gets the name of the rule, shown to the student when it refuses.</summary>
        public string RuleName
        {
            get { return "Prerequisite"; }
        }

        /// <summary>
        /// Returns a refusal naming every missing prerequisite, or a pass when the
        /// student has completed all of them.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>The verdict and, when refused, the missing subject codes.</returns>
        public EligibilityResult Evaluate(Student student, Course course)
        {
            if (student == null || course == null)
            {
                return EligibilityResult.Fail(this.RuleName, "the student or course is missing");
            }

            List<Prerequisite> rules = this.repository.GetByCourse(course.CourseId);
            List<string> missing = new List<string>();

            foreach (Prerequisite rule in rules)
            {
                // A corequisite is the concern of CorequisiteRule, not of this rule.
                if (!rule.IsPrerequisite())
                {
                    continue;
                }

                if (!student.HasCompleted(rule.RequiredCourse.CourseId))
                {
                    missing.Add(rule.RequiredCourse.Code);
                }
            }

            if (missing.Count == 0)
            {
                return EligibilityResult.Pass(this.RuleName);
            }

            return EligibilityResult.Fail(this.RuleName,
                "must first complete " + string.Join(", ", missing.ToArray()));
        }
    }
}

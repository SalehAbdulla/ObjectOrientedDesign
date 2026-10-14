using System;
using System.Collections.Generic;
using SCAIS.DataAccess;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Business
{
    /// <summary>
    /// Refuses a course when a corequisite has not been completed and is not being taken
    /// in the same semester.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (a Concrete Strategy).</b></para>
    /// <para>
    /// A corequisite is the opposite of a prerequisite: the two subjects are designed to
    /// be taken together, so being enrolled in the partner subject is enough. This rule
    /// is a separate class from <see cref="PrerequisiteRule"/> precisely because the two
    /// have different meanings, even though they read the same table.
    /// </para>
    /// </remarks>
    public class CorequisiteRule : IEligibilityRule
    {
        /// <summary>The repository that supplies the curriculum rules.</summary>
        private readonly PrerequisiteRepository repository;

        /// <summary>Creates the rule with its own repository.</summary>
        public CorequisiteRule()
            : this(new PrerequisiteRepository())
        {
        }

        /// <summary>Creates the rule with a supplied repository.</summary>
        /// <param name="repository">The repository to read curriculum rules from.</param>
        public CorequisiteRule(PrerequisiteRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
        }

        /// <summary>Gets the name of the rule.</summary>
        public string RuleName
        {
            get { return "Corequisite"; }
        }

        /// <summary>
        /// Returns a refusal when a partner subject has neither been passed nor is being
        /// taken, otherwise a pass.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>The verdict and, when refused, the missing partner subject codes.</returns>
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
                if (!rule.IsCorequisite())
                {
                    continue;
                }

                // Either already passed, or being taken in the same semester.
                if (!student.HasTakenOrIsTaking(rule.RequiredCourse.CourseId))
                {
                    missing.Add(rule.RequiredCourse.Code);
                }
            }

            if (missing.Count == 0)
            {
                return EligibilityResult.Pass(this.RuleName);
            }

            return EligibilityResult.Fail(this.RuleName,
                "must be taken together with " + string.Join(", ", missing.ToArray()));
        }
    }
}

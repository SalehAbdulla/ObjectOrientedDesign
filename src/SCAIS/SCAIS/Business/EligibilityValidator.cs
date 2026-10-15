using System;
using System.Collections.Generic;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Business
{
    /// <summary>
    /// One course together with the verdict the validator reached about it. This is what
    /// the eligible-courses grid binds to.
    /// </summary>
    public class CourseEligibility
    {
        /// <summary>Gets or sets the course that was checked.</summary>
        public Course Course { get; set; }

        /// <summary>Gets or sets the verdict for that course.</summary>
        public EligibilityResult Result { get; set; }

        /// <summary>Gets the course code, for grid binding.</summary>
        public string Code
        {
            get { return this.Course == null ? string.Empty : this.Course.Code; }
        }

        /// <summary>Gets the course title, for grid binding.</summary>
        public string Title
        {
            get { return this.Course == null ? string.Empty : this.Course.Title; }
        }

        /// <summary>Gets the credit hours, for grid binding.</summary>
        public int CreditHours
        {
            get { return this.Course == null ? 0 : this.Course.CreditHours; }
        }

        /// <summary>Gets the verdict as text, for grid binding.</summary>
        public string Eligibility
        {
            get
            {
                return this.Result == null ? string.Empty : (this.Result.IsEligible ? "Eligible" : "Blocked");
            }
        }

        /// <summary>Gets the explanation, for grid binding.</summary>
        public string Reason
        {
            get { return this.Result == null ? string.Empty : this.Result.Reason; }
        }
    }

    /// <summary>
    /// Runs the eligibility rules and combines their verdicts into one decision.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (the Context).</b></para>
    /// <para>
    /// This class is the whole reason the Strategy pattern is used in SCAIS. It holds a
    /// list of <see cref="IEligibilityRule"/> and knows nothing else about them: not how
    /// many there are, not what they check, and not whether a new one was added last
    /// week. Its only job is to run each rule and collect the refusals.
    /// </para>
    /// <para>
    /// <b>Why this matters for the administrator's requirement.</b> The business case
    /// says the administrator must be able to maintain the rules of the system. Because
    /// the rules are objects behind an interface, that requirement is met by adding a
    /// class and registering it in <see cref="CreateDefaultRules"/> - the validator
    /// itself is never edited, which is the open/closed principle in practice.
    /// </para>
    /// <para>
    /// <b>Every rule runs, even after one has refused.</b> Stopping at the first refusal
    /// would be faster, but the student would then be told about one problem at a time
    /// and would have to resubmit repeatedly to discover the rest. Reporting every
    /// failure at once is the behaviour the "block or warn" requirement asks for.
    /// </para>
    /// </remarks>
    public class EligibilityValidator
    {
        /// <summary>The rules this validator runs, in the order it runs them.</summary>
        private readonly List<IEligibilityRule> rules;

        /// <summary>Creates a validator with the standard rule set for SCAIS.</summary>
        public EligibilityValidator()
            : this(CreateDefaultRules())
        {
        }

        /// <summary>
        /// Creates a validator with a supplied rule set. Supplying the rules from outside
        /// is what makes the strategy replaceable, and it is also how a unit test would
        /// inject a single stub rule.
        /// </summary>
        /// <param name="rules">The rules to run, in order.</param>
        public EligibilityValidator(List<IEligibilityRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            this.rules = rules;
        }

        /// <summary>Gets the rules this validator runs, as a read-only view.</summary>
        public IList<IEligibilityRule> Rules
        {
            get { return this.rules.AsReadOnly(); }
        }

        /// <summary>
        /// Builds the standard rule set. This is the single place where the composition
        /// of the pipeline is decided.
        /// </summary>
        /// <returns>The rules every eligibility check runs.</returns>
        public static List<IEligibilityRule> CreateDefaultRules()
        {
            return new List<IEligibilityRule>
            {
                // Cheapest checks first: these two need no database access at all.
                new AlreadyCompletedRule(),
                new SpecializationRule(),
                new CreditLimitRule(),
                new PrerequisiteRule(),
                new CorequisiteRule()
            };
        }

        /// <summary>
        /// Runs every rule against one course and combines the results.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>
        /// A passing result when every rule passed, otherwise a refusal listing each
        /// failure so the student can see all of them at once.
        /// </returns>
        public EligibilityResult Validate(Student student, Course course)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            List<string> failures = new List<string>();

            for (int index = 0; index < this.rules.Count; index++)
            {
                IEligibilityRule rule = this.rules[index];
                EligibilityResult result = rule.Evaluate(student, course);

                if (result == null || !result.IsEligible)
                {
                    failures.Add(result == null
                        ? rule.RuleName + ": returned no verdict"
                        : result.Reason);
                }
            }

            if (failures.Count == 0)
            {
                return new EligibilityResult(true, "Eligible");
            }

            // Each rule's refusal already begins with the Blocked prefix, so the combined note
            // strips it from the parts and adds it once. Without this the screens and the
            // eligibility note stored against a requested course read "Blocked: Blocked: ...".
            List<string> reasons = new List<string>();
            string prefix = EligibilityResult.BlockedPrefix + ": ";

            for (int index = 0; index < failures.Count; index++)
            {
                string failure = failures[index];

                reasons.Add(failure.StartsWith(prefix, StringComparison.Ordinal)
                    ? failure.Substring(prefix.Length)
                    : failure);
            }

            // The note stored against a requested course must begin with "Blocked" so
            // that CourseRequestLine can report the verdict without another column.
            return new EligibilityResult(false,
                prefix + string.Join("; ", reasons.ToArray()));
        }

        /// <summary>
        /// Checks a whole catalogue and returns each course with its verdict. This is the
        /// single call behind the student's eligible-courses screen.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="courses">The courses to check.</param>
        /// <returns>One entry per course, in the order supplied.</returns>
        public List<CourseEligibility> ValidateCatalogue(Student student, IEnumerable<Course> courses)
        {
            if (courses == null)
            {
                throw new ArgumentNullException("courses");
            }

            List<CourseEligibility> results = new List<CourseEligibility>();

            foreach (Course course in courses)
            {
                results.Add(new CourseEligibility
                {
                    Course = course,
                    Result = this.Validate(student, course)
                });
            }

            return results;
        }
    }
}

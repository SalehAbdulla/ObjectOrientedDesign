using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// One dependency rule between two courses: a dependent course requires another
    /// course to have been completed, or to be taken at the same time.
    /// </summary>
    /// <remarks>
    /// The class models a many-to-many self relationship on <see cref="Course"/>: the
    /// same course can appear as the dependent course in one rule and as the required
    /// course in another. The <see cref="RuleType"/> decides how the eligibility rule
    /// interprets it.
    ///
    /// Both courses are read-only after construction. A rule that could be edited into
    /// pointing at a different course would silently change the curriculum, so the way
    /// to change a rule is to delete it and create a new one.
    /// </remarks>
    public class Prerequisite
    {
        /// <summary>Primary key of the Prerequisites row.</summary>
        private readonly int prerequisiteId;

        /// <summary>The dependent course.</summary>
        private readonly Course course;

        /// <summary>The course that must be completed first.</summary>
        private readonly Course requiredCourse;

        /// <summary>Whether the rule is a prerequisite or a corequisite.</summary>
        private readonly RuleType ruleType;

        /// <summary>Creates a dependency rule.</summary>
        /// <param name="prerequisiteId">Primary key, or zero when not yet saved.</param>
        /// <param name="course">The dependent course.</param>
        /// <param name="requiredCourse">The course that must be completed first.</param>
        /// <param name="ruleType">Whether the rule is a prerequisite or a corequisite.</param>
        public Prerequisite(int prerequisiteId, Course course, Course requiredCourse, RuleType ruleType)
        {
            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            if (requiredCourse == null)
            {
                throw new ArgumentNullException("requiredCourse");
            }

            if (course.CourseId == requiredCourse.CourseId && course.CourseId != 0)
            {
                throw new ArgumentException("A course cannot be its own prerequisite.");
            }

            this.prerequisiteId = prerequisiteId;
            this.course = course;
            this.requiredCourse = requiredCourse;
            this.ruleType = ruleType;
        }

        /// <summary>Gets the primary key of the rule.</summary>
        public int PrerequisiteId
        {
            get { return this.prerequisiteId; }
        }

        /// <summary>Gets the dependent course.</summary>
        public Course Course
        {
            get { return this.course; }
        }

        /// <summary>Gets the course that must be completed first.</summary>
        public Course RequiredCourse
        {
            get { return this.requiredCourse; }
        }

        /// <summary>Gets the type of the rule.</summary>
        public RuleType RuleType
        {
            get { return this.ruleType; }
        }

        /// <summary>Returns true when the required course must already be passed.</summary>
        public bool IsPrerequisite()
        {
            return this.ruleType == RuleType.Prerequisite;
        }

        /// <summary>Returns true when the required course may be taken concurrently.</summary>
        public bool IsCorequisite()
        {
            return this.ruleType == RuleType.Corequisite;
        }

        /// <summary>
        /// Returns the rule as a sentence, for example
        /// "IT240 Database 2 requires IT106 Database 1".
        /// </summary>
        public string GetDescription()
        {
            string relationship = this.ruleType == RuleType.Prerequisite
                ? "requires"
                : "requires (may be taken together with)";

            return this.course.GetDisplayName() + " " + relationship + " " + this.requiredCourse.GetDisplayName();
        }

        /// <summary>Returns the rule as a sentence.</summary>
        public override string ToString()
        {
            return this.GetDescription();
        }
    }
}

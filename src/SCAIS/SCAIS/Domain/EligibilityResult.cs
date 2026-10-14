using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// The verdict produced by one eligibility rule, or by the validator that runs all
    /// of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A result carries both the decision and the explanation. Carrying the reason is
    /// what allows the user interface to satisfy the "block <b>or warn</b>" requirement
    /// of the business case: the student is not simply refused, they are told which
    /// rule refused them.
    /// </para>
    /// <para>
    /// The type is immutable: both properties are read-only and are set once by the
    /// constructor, so a verdict cannot be changed after a rule has produced it.
    /// </para>
    /// </remarks>
    public class EligibilityResult
    {
        /// <summary>
        /// Prefix used on every refusal. It is the marker that
        /// <see cref="CourseRequestLine.IsEligible"/> looks for when a verdict is
        /// persisted to the database.
        /// </summary>
        public const string BlockedPrefix = "Blocked";

        /// <summary>Whether the rule was satisfied.</summary>
        private readonly bool isEligible;

        /// <summary>Human readable explanation of the verdict.</summary>
        private readonly string reason;

        /// <summary>Creates a verdict.</summary>
        /// <param name="isEligible">True when the rule was satisfied.</param>
        /// <param name="reason">Explanation shown to the student.</param>
        public EligibilityResult(bool isEligible, string reason)
        {
            this.isEligible = isEligible;
            this.reason = string.IsNullOrEmpty(reason) ? string.Empty : reason.Trim();
        }

        /// <summary>Gets a value indicating whether the rule was satisfied.</summary>
        public bool IsEligible
        {
            get { return this.isEligible; }
        }

        /// <summary>Gets the explanation of the verdict.</summary>
        public string Reason
        {
            get { return this.reason; }
        }

        /// <summary>
        /// Creates a satisfied verdict for the named rule.
        /// </summary>
        /// <param name="ruleName">The name of the rule that passed.</param>
        /// <returns>A passing result.</returns>
        public static EligibilityResult Pass(string ruleName)
        {
            return new EligibilityResult(true, ruleName + ": satisfied");
        }

        /// <summary>
        /// Creates a refusal for the named rule, formatted so that the note stored
        /// against the requested course starts with <see cref="BlockedPrefix"/>.
        /// </summary>
        /// <param name="ruleName">The name of the rule that refused the course.</param>
        /// <param name="detail">The specific reason, for example the missing course.</param>
        /// <returns>A failing result.</returns>
        public static EligibilityResult Fail(string ruleName, string detail)
        {
            return new EligibilityResult(false,
                BlockedPrefix + ": " + ruleName + " - " + detail);
        }

        /// <summary>Returns the verdict and its explanation.</summary>
        public override string ToString()
        {
            return this.isEligible ? "Eligible - " + this.reason : this.reason;
        }
    }
}

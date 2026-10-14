using SCAIS.Domain;

namespace SCAIS.Interfaces
{
    /// <summary>
    /// The contract that every course eligibility rule must satisfy.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Strategy (the Strategy interface).</b></para>
    /// <para>
    /// Eligibility in SCAIS is not one rule. A course may require a completed
    /// prerequisite, a concurrently taken corequisite, a credit hour ceiling, or it may
    /// already have been passed. Each of those is a separate algorithm, and the set of
    /// algorithms must be extensible because an administrator can define new rules.
    /// </para>
    /// <para>
    /// The interface makes each rule interchangeable: <c>EligibilityValidator</c> holds
    /// a list of <c>IEligibilityRule</c> and runs them in order, without knowing which
    /// concrete rules it was given. Adding a rule is therefore a new class plus one line
    /// of configuration, and no existing class is modified.
    /// </para>
    /// <para>
    /// An interface is used rather than an abstract class because the rules share no
    /// state and no implementation - only the shape of the operation. A rule that does
    /// need shared plumbing could later extend a common base class and still implement
    /// this interface.
    /// </para>
    /// </remarks>
    public interface IEligibilityRule
    {
        /// <summary>
        /// Gets the name of the rule, used in the explanation shown to the student and
        /// in the eligibility report.
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Decides whether the student may enrol in the course.
        /// </summary>
        /// <param name="student">The student whose eligibility is being checked.</param>
        /// <param name="course">The course the student wants to take.</param>
        /// <returns>
        /// A result carrying the verdict and, when the course is refused, the reason.
        /// Implementations must never return null.
        /// </returns>
        EligibilityResult Evaluate(Student student, Course course);
    }
}

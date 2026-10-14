using System.Collections.Generic;

namespace SCAIS.Interfaces
{
    /// <summary>
    /// The single CRUD contract that every repository in SCAIS implements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The business and presentation layers depend on this contract, never on a
    /// concrete repository class. Two things follow from that:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///   A table can gain a specialised repository without any caller changing, because
    ///   the caller was only ever using <see cref="GetById"/>, <see cref="GetAll"/>,
    ///   <see cref="Add"/>, <see cref="Update"/> and <see cref="Delete"/>.
    ///   </description></item>
    ///   <item><description>
    ///   The data store can be replaced. The business case states that web and mobile
    ///   front ends are planned, which will require a different data layer; because the
    ///   services hold an <c>IRepository&lt;T&gt;</c> and not a
    ///   <c>SqlRepositoryBase&lt;T&gt;</c>, that change is confined to the data access
    ///   layer.
    ///   </description></item>
    /// </list>
    /// <para>
    /// An interface is used rather than an abstract base class because the contract is
    /// purely behavioural: there is no shared state or implementation to inherit.
    /// <c>SqlRepositoryBase&lt;T&gt;</c> supplies the shared ADO.NET code separately, so
    /// each class has exactly one reason to change.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The domain type the repository stores.</typeparam>
    public interface IRepository<T>
    {
        /// <summary>
        /// Returns the entity with the supplied primary key, or null when it does not exist.
        /// </summary>
        /// <param name="id">The primary key to look for.</param>
        /// <returns>The entity, or null.</returns>
        T GetById(int id);

        /// <summary>Returns every entity in the table.</summary>
        /// <returns>A list of entities, never null.</returns>
        List<T> GetAll();

        /// <summary>
        /// Inserts the entity.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <returns>True when a row was inserted.</returns>
        bool Add(T entity);

        /// <summary>
        /// Updates the entity, matching on its primary key.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>True when a row was changed.</returns>
        bool Update(T entity);

        /// <summary>
        /// Deletes the entity with the supplied primary key.
        /// </summary>
        /// <param name="id">The primary key to delete.</param>
        /// <returns>True when a row was deleted.</returns>
        bool Delete(int id);
    }
}

using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// An academic track that a student may declare, such as Programming or Networking.
    /// </summary>
    /// <remarks>
    /// The four flagship tracks named in the business case are Programming, Networking,
    /// Cybersecurity and Database. Additional tracks exist in the database so that the
    /// mock data set meets the required minimum number of records per table.
    /// </remarks>
    public class Specialization
    {
        /// <summary>Primary key of the Specializations row.</summary>
        private readonly int specializationId;

        /// <summary>Backing field for <see cref="Name"/>.</summary>
        private string name;

        /// <summary>Backing field for <see cref="Description"/>.</summary>
        private string description;

        /// <summary>Creates a specialization.</summary>
        /// <param name="specializationId">Primary key, or zero when not yet saved.</param>
        /// <param name="name">Name of the track.</param>
        /// <param name="description">Short description of the track.</param>
        public Specialization(int specializationId, string name, string description)
        {
            this.specializationId = specializationId;
            this.Name = name;
            this.description = description;
        }

        /// <summary>Gets the primary key of the track.</summary>
        public int SpecializationId
        {
            get { return this.specializationId; }
        }

        /// <summary>
        /// Gets or sets the track name. A blank name is rejected because the name is
        /// what the student sees on every screen that mentions their track.
        /// </summary>
        public string Name
        {
            get { return this.name; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A specialization must have a name.", "value");
                }

                this.name = value.Trim();
            }
        }

        /// <summary>Gets or sets the description of the track.</summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        /// <summary>Returns the track name.</summary>
        public override string ToString()
        {
            return this.name;
        }
    }
}

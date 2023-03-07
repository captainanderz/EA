using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public abstract class BaseEntity
    {
        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}

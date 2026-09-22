using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.DataAccess.Context
{
    public class DMSContext: DbContext
    {
        public DMSContext(DbContextOptions<DMSContext> options)
            : base(options) { }
        public DbSet<Domain.Model.Document> Documents { get; set; }
    }
}

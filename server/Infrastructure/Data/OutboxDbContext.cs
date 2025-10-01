using System;
using Domain.OutBox;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class OutboxDbContext : DbContext
    {
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<DeadLetterMessage> DeadLetterMessages { get; set; }

        public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboxDbContext).Assembly);
        }
    }
}
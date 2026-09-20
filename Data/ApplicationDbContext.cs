using BauManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Baustelle> Baustellen => Set<Baustelle>();

    public DbSet<WorkAssignment> WorkAssignments => Set<WorkAssignment>();

    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ==========================================
        // COMPANY
        // ==========================================

        builder.Entity<Company>()
            .HasKey(x => x.Id);

        builder.Entity<ApplicationUser>()
            .HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // EMPLOYEE
        // ==========================================

        builder.Entity<Employee>()
            .HasKey(x => x.Id);

        builder.Entity<Employee>()
            .HasOne(x => x.Company)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // BAUSTELLE
        // ==========================================

        builder.Entity<Baustelle>()
            .HasKey(x => x.Id);

        builder.Entity<Baustelle>()
            .HasOne(x => x.Company)
            .WithMany(x => x.Baustellen)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // WORK ASSIGNMENT
        // ==========================================

        builder.Entity<WorkAssignment>()
            .HasKey(x => x.Id);

        builder.Entity<WorkAssignment>()
            .HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Employee>()
            .HasOne(x => x.ApplicationUser)
            .WithOne()
            .HasForeignKey<Employee>(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkAssignment>()
            .HasOne(x => x.Employee)
            .WithMany(x => x.WorkAssignments)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkAssignment>()
            .HasOne(x => x.Baustelle)
            .WithMany(x => x.WorkAssignments)
            .HasForeignKey(x => x.BaustelleId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // WORK TASK
        // ==========================================

        builder.Entity<WorkTask>()
            .HasKey(x => x.Id);

        builder.Entity<WorkTask>()
            .HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkTask>()
            .HasOne(x => x.Baustelle)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.BaustelleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkTask>()
            .HasOne(x => x.Employee)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

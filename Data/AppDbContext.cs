using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuoteExtraCost> QuoteExtraCosts { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }

        public DbSet<ProductionImport> ProductionImports { get; set; }

        public DbSet<Material> Materials { get; set; }
        public DbSet<ImportTransaction> ImportTransactions { get; set; }
        public DbSet<ExportTransaction> ExportTransactions { get; set; }

        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<QuotationDetail> QuotationDetails { get; set; }
        public DbSet<ChiTietPhieuGiaoHang> ChiTietPhieuGiaoHangs { get; set; }
        public DbSet<ChiTietPhieuXuatKho> ChiTietPhieuXuatKhos { get; set; }
        public DbSet<ChiPhiPhatSinhThucTe> ChiPhiPhatSinhThucTe { get; set; }
        public DbSet<PhieuGiaoHang> PhieuGiaoHangs { get; set; }
        public DbSet<PhieuXuatKho> PhieuXuatKhos { get; set; }
        public DbSet<CongNoKhachHang> CongNoKhachHangs { get; set; }
        public DbSet<PhanQuyen> PhanQuyen { get; set; }
        public DbSet<TaiKhoanKeToan> TaiKhoanKeToans { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<AccountingJournalBatch> AccountingJournalBatches { get; set; }
        public DbSet<AccountingJournalLine> AccountingJournalLines { get; set; }
        public DbSet<SoCai> SoCais { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = "Server=localhost;Database=PhanMemInAnDB;User Id=sa;Password=123;TrustServerCertificate=True;";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}

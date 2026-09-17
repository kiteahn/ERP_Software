using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemInAnERP.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingAndAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "DeliveryTime",
                table: "SalesOrders",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleType",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerTaxCode",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PlatePricePerColorLargeMachine",
                table: "Quotations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlatePricePerColorSmallMachine",
                table: "Quotations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PrintProofFee",
                table: "Quotations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "QuoteNo",
                table: "Quotations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Material",
                table: "ProductionOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Dimensions",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "BuHao",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColorCount",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasButtoning",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDieCutting",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGluing",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasLamination",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPackaging",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPlateMaking",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPrinting",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasStringing",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLargeMachine",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "LaminationPrice",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "LaminationSides",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LaminationType",
                table: "ProductionOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PaperPricePerTon",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlatePricePerColorLarge",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlatePricePerColorSmall",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PrintLength",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PrintWidth",
                table: "ProductionOrders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SoCon",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "ExportTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxCode",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaKH",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySua",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "Customers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXoa",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiLienHe",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NguoiSua",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NguoiTao",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NguoiXoa",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AccountingJournalBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TriggerDocument = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Posted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingJournalBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingJournalBatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenBang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdBanGhi = table.Column<int>(type: "int", nullable: false),
                    DuLieuCu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuLieuMoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiaChiIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChiPhiPhatSinhThucTe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    TenKhoanMuc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NguoiNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiPhiPhatSinhThucTe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiPhiPhatSinhThucTe_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiPhiPhatSinhThucTe_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongNoKhachHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    SoTienPhaiThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienDaThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienConLai = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayHoaDon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDenHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoNgayQuaHan = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongNoKhachHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongNoKhachHangs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongNoKhachHangs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordReset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaSuDung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordReset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordReset_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhanQuyen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenModule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CoXem = table.Column<bool>(type: "bit", nullable: false),
                    CoTao = table.Column<bool>(type: "bit", nullable: false),
                    CoSua = table.Column<bool>(type: "bit", nullable: false),
                    CoXoa = table.Column<bool>(type: "bit", nullable: false),
                    CoDuyet = table.Column<bool>(type: "bit", nullable: false),
                    CoXuatBC = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanQuyen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhieuGiaoHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPGH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    NgayGiao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiGiao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChiGiao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SoDienThoaiNhanHang = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayGiaoThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiNhanHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChuGiao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HinhAnhXacNhan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuGiaoHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuGiaoHangs_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuXuatKhos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuXuat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayXuat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MucDich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TongGiaTri = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuXuatKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuXuatKhos_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhieuXuatKhos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TienGiay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienMuc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienKem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienCanMang = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienMetalize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienUV = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienBe = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienKhuonBe = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienDan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienNut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienThung = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienXeGiao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienProof = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongGiaThanhSanXuat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiaMoiCai = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiaBaoKhach = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongGiaBaoKhach = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaNucMucChinh = table.Column<bool>(type: "bit", nullable: false),
                    SoLuongThucTe = table.Column<int>(type: "int", nullable: true),
                    PhiGiaCongThucTe = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LoiNhuanThucTe = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GhiChuGiaCong = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationDetails_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanKeToans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenTK = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoaiTK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaTKCha = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LoTang = table.Column<bool>(type: "bit", nullable: false),
                    CoSoDu = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanKeToans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThongBaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBaos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountingJournalLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Debit = table.Column<double>(type: "float", nullable: false),
                    Credit = table.Column<double>(type: "float", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingJournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingJournalLines_AccountingJournalBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "AccountingJournalBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoCais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayHachToan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoChungTu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DienGiai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SoTienNo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienCo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JournalBatchId = table.Column<int>(type: "int", nullable: true),
                    DoiTuong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoCais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoCais_AccountingJournalBatches_JournalBatchId",
                        column: x => x.JournalBatchId,
                        principalTable: "AccountingJournalBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuGiaoHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuGiaoHangId = table.Column<int>(type: "int", nullable: false),
                    TenSanPham = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuGiaoHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuGiaoHangs_PhieuGiaoHangs_PhieuGiaoHangId",
                        column: x => x.PhieuGiaoHangId,
                        principalTable: "PhieuGiaoHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuXuatKhos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuXuatKhoId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuXuatKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuatKhos_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuatKhos_PhieuXuatKhos_PhieuXuatKhoId",
                        column: x => x.PhieuXuatKhoId,
                        principalTable: "PhieuXuatKhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportTransactions_QuotationId",
                table: "ExportTransactions",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingJournalBatches_UserId",
                table: "AccountingJournalBatches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingJournalLines_BatchId",
                table: "AccountingJournalLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiPhiPhatSinhThucTe_ProductionOrderId",
                table: "ChiPhiPhatSinhThucTe",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiPhiPhatSinhThucTe_QuotationId",
                table: "ChiPhiPhatSinhThucTe",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuGiaoHangs_PhieuGiaoHangId",
                table: "ChiTietPhieuGiaoHangs",
                column: "PhieuGiaoHangId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuatKhos_MaterialId",
                table: "ChiTietPhieuXuatKhos",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuatKhos_PhieuXuatKhoId",
                table: "ChiTietPhieuXuatKhos",
                column: "PhieuXuatKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_CongNoKhachHangs_CustomerId",
                table: "CongNoKhachHangs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CongNoKhachHangs_InvoiceId",
                table: "CongNoKhachHangs",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordReset_UserId",
                table: "PasswordReset",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuGiaoHangs_SalesOrderId",
                table: "PhieuGiaoHangs",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuatKhos_ProductionOrderId",
                table: "PhieuXuatKhos",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuatKhos_UserId",
                table: "PhieuXuatKhos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationDetails_QuotationId",
                table: "QuotationDetails",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SoCais_JournalBatchId",
                table: "SoCais",
                column: "JournalBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_UserId",
                table: "ThongBaos",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportTransactions_Quotations_QuotationId",
                table: "ExportTransactions",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportTransactions_Quotations_QuotationId",
                table: "ExportTransactions");

            migrationBuilder.DropTable(
                name: "AccountingJournalLines");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ChiPhiPhatSinhThucTe");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuGiaoHangs");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuXuatKhos");

            migrationBuilder.DropTable(
                name: "CongNoKhachHangs");

            migrationBuilder.DropTable(
                name: "PasswordReset");

            migrationBuilder.DropTable(
                name: "PhanQuyen");

            migrationBuilder.DropTable(
                name: "QuotationDetails");

            migrationBuilder.DropTable(
                name: "SoCais");

            migrationBuilder.DropTable(
                name: "TaiKhoanKeToans");

            migrationBuilder.DropTable(
                name: "ThongBaos");

            migrationBuilder.DropTable(
                name: "PhieuGiaoHangs");

            migrationBuilder.DropTable(
                name: "PhieuXuatKhos");

            migrationBuilder.DropTable(
                name: "AccountingJournalBatches");

            migrationBuilder.DropIndex(
                name: "IX_ExportTransactions_QuotationId",
                table: "ExportTransactions");

            migrationBuilder.DropColumn(
                name: "DeliveryTime",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "SaleType",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CustomerTaxCode",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PlatePricePerColorLargeMachine",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PlatePricePerColorSmallMachine",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PrintProofFee",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "QuoteNo",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "BuHao",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ColorCount",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasButtoning",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasDieCutting",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasGluing",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasLamination",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasPackaging",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasPlateMaking",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasPrinting",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasStringing",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "IsLargeMachine",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "LaminationPrice",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "LaminationSides",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "LaminationType",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PaperPricePerTon",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PlatePricePerColorLarge",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PlatePricePerColorSmall",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PrintLength",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PrintWidth",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SoCon",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "ExportTransactions");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MaKH",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NgaySua",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NgayXoa",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NguoiLienHe",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NguoiSua",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NguoiTao",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NguoiXoa",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Material",
                table: "ProductionOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dimensions",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxCode",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}

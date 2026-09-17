using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service trung tâm xử lý hạch toán kế toán tự động.
    /// Map nghiệp vụ kinh tế → bút toán kế toán (Nợ/Có).
    /// </summary>
    public class AccountingService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Dictionary map nghiệp vụ → danh sách bút toán (TK Nợ, TK Có, Diễn giải).
        /// </summary>
        private readonly Dictionary<string, List<(string TKNo, string TKCo, string DienGiai)>> _map;

        public AccountingService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _map = new Dictionary<string, List<(string TKNo, string TKCo, string DienGiai)>>
            {
                // MUA HANG & NHAP KHO
                ["NHAP_KHO_NVL"] = new List<(string, string, string)>
                {
                    ("152", "331", "Nhập kho NVL — Nợ TK152 / Có TK331")
                },
                ["TRA_TIEN_NCC_TM"] = new List<(string, string, string)>
                {
                    ("331", "111", "Trả NCC tiền mặt — Nợ TK331 / Có TK111")
                },
                ["TRA_TIEN_NCC_CK"] = new List<(string, string, string)>
                {
                    ("331", "112", "Trả NCC CK — Nợ TK331 / Có TK112")
                },

                // SAN XUAT
                ["XUAT_KHO_SX"] = new List<(string, string, string)>
                {
                    ("154", "152", "Xuất NVL SX — Nợ TK154 / Có TK152")
                },
                ["GHI_NHAN_NHAN_CONG"] = new List<(string, string, string)>
                {
                    ("154", "334", "Nhân công trực tiếp — Nợ TK154 / Có TK334")
                },
                ["PHAN_BO_MAY"] = new List<(string, string, string)>
                {
                    ("154", "214", "Khấu hao máy — Nợ TK154 / Có TK214"),
                    ("154", "331", "CP điện/nước — Nợ TK154 / Có TK331")
                },
                ["NHAP_KHO_TP"] = new List<(string, string, string)>
                {
                    ("155", "154", "Nhập kho TP — Nợ TK155 / Có TK154")
                },

                // BAN HANG
                ["GIAO_HANG_XUAT_HOA_DON"] = new List<(string, string, string)>
                {
                    ("131", "511", "Ghi nhận DT — Nợ TK131 / Có TK511"),
                    ("131", "3331", "Thuế GTGT ĐR — Nợ TK131 / Có TK3331")
                },
                ["GHI_NHAN_GIA_VON"] = new List<(string, string, string)>
                {
                    ("632", "155", "Giá vốn — Nợ TK632 / Có TK155")
                },
                ["THU_TIEN_KHACH_TM"] = new List<(string, string, string)>
                {
                    ("111", "131", "Thu TM — Nợ TK111 / Có TK131")
                },
                ["THU_TIEN_KHACH_CK"] = new List<(string, string, string)>
                {
                    ("112", "131", "Thu CK — Nợ TK112 / Có TK131")
                },

                // DAC THU NGANH IN
                ["HANG_HONG_BTHG"] = new List<(string, string, string)>
                {
                    ("138", "154", "Hàng hỏng có BTHG — Nợ TK138 / Có TK154")
                },
                ["HANG_HONG_CHIPHI"] = new List<(string, string, string)>
                {
                    ("632", "154", "Hàng hỏng tính CP — Nợ TK632 / Có TK154")
                },
                ["HAO_HUT_VUOT"] = new List<(string, string, string)>
                {
                    ("632", "154", "Hao hụt vượt ĐM — Nợ TK632 / Có TK154")
                },
                ["HANG_TRA_LAI_DT"] = new List<(string, string, string)>
                {
                    ("511", "131", "Trả hàng giảm DT — Nợ TK511 / Có TK131"),
                    ("3331", "131", "Thuế GTGT ĐC — Nợ TK3331 / Có TK131")
                },
                ["HANG_TRA_LAI_KHO"] = new List<(string, string, string)>
                {
                    ("155", "632", "Nhập lại TP — Nợ TK155 / Có TK632")
                },
                ["CHIET_KHAU_TT"] = new List<(string, string, string)>
                {
                    ("635", "131", "CK TT sớm — Nợ TK635 / Có TK131")
                },
                ["CHIET_KHAU_TM"] = new List<(string, string, string)>
                {
                    ("511", "131", "CK TM sản lượng — Nợ TK511 / Có TK131")
                },

                // CUOI KY
                ["KC_DOANH_THU"] = new List<(string, string, string)>
                {
                    ("511", "911", "KC DT — Nợ TK511 / Có TK911")
                },
                ["KC_DOANH_THU_TAI_CHINH"] = new List<(string, string, string)>
                {
                    ("515", "911", "KC DT tài chính — Nợ TK515 / Có TK911")
                },
                ["KC_GIAM_TRU"] = new List<(string, string, string)>
                {
                    ("911", "521", "KC giảm trừ — Nợ TK911 / Có TK521")
                },
                ["KC_GIA_VON"] = new List<(string, string, string)>
                {
                    ("911", "632", "KC GV — Nợ TK911 / Có TK632")
                },
                ["KC_CP_TAI_CHINH"] = new List<(string, string, string)>
                {
                    ("911", "635", "KC CP tài chính — Nợ TK911 / Có TK635")
                },
                ["KC_CP_BH"] = new List<(string, string, string)>
                {
                    ("911", "641", "KC CPBH — Nợ TK911 / Có TK641")
                },
                ["KC_CP_QLDN"] = new List<(string, string, string)>
                {
                    ("911", "642", "KC CPQLDN — Nợ TK911 / Có TK642")
                },
                ["XAC_DINH_LAI"] = new List<(string, string, string)>
                {
                    ("911", "421", "Xác định lãi — Nợ TK911 / Có TK421")
                },
                ["XAC_DINH_LO"] = new List<(string, string, string)>
                {
                    ("421", "911", "Xác định lỗ — Nợ TK421 / Có TK911")
                }
            };
        }

        /// <summary>
        /// Lấy địa chỉ IP của máy hiện tại.
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "unknown";
        }

        /// <summary>
        /// Trigger hạch toán một nghiệp vụ kinh tế.
        /// Tạo AccountingJournalBatch + AccountingJournalLines + SoCai.
        /// </summary>
        /// <param name="loaiNghiepVu">Mã nghiệp vụ (key trong _map)</param>
        /// <param name="refId">ID chứng từ gốc</param>
        /// <param name="soTien">Số tiền hạch toán</param>
        /// <param name="refTable">Tên bảng gốc</param>
        /// <param name="doiTuong">Đối tượng (mã KH, mã NCC...)</param>
        /// <param name="soChungTu">Số chứng từ</param>
        /// <param name="tuDongDuyet">Tự động duyệt hay để Nháp</param>
        /// <returns>Lô bút toán đã tạo</returns>
        public async Task<AccountingJournalBatch> TriggerKeToanAsync(
            string loaiNghiepVu,
            int refId,
            decimal soTien,
            string refTable,
            string? doiTuong = null,
            string? soChungTu = null,
            bool tuDongDuyet = true)
        {
            if (!_map.ContainsKey(loaiNghiepVu))
            {
                throw new ArgumentException($"Không tìm thấy nghiệp vụ: {loaiNghiepVu}");
            }

            if (soTien <= 0)
            {
                throw new ArgumentException("Số tiền phải lớn hơn 0");
            }

            var butToans = _map[loaiNghiepVu];
            var userName = Helpers.AppSession.CurrentUser?.FullName ?? "System";
            var now = DateTime.Now;

            // Tạo batch
            var batch = new AccountingJournalBatch
            {
                EventType = loaiNghiepVu,
                ReferenceType = refTable,
                ReferenceId = refId,
                TriggerDocument = soChungTu,
                CreatedAt = now,
                Posted = tuDongDuyet,
                PostedAt = tuDongDuyet ? now : null,
                UserId = Helpers.AppSession.CurrentUser?.Id
            };

            _context.AccountingJournalBatches.Add(batch);
            await _context.SaveChangesAsync();

            int lineNo = 1;
            foreach (var (tkNo, tkCo, dienGiai) in butToans)
            {
                // Dòng Nợ
                var lineNoDong = new AccountingJournalLine
                {
                    BatchId = batch.Id,
                    LineNo = lineNo++,
                    AccountCode = tkNo,
                    AccountName = await GetTenTKAsync(tkNo),
                    Debit = (double)soTien,
                    Credit = 0,
                    Explanation = dienGiai
                };
                _context.AccountingJournalLines.Add(lineNoDong);

                // Dòng Có
                var lineCoDong = new AccountingJournalLine
                {
                    BatchId = batch.Id,
                    LineNo = lineNo++,
                    AccountCode = tkCo,
                    AccountName = await GetTenTKAsync(tkCo),
                    Debit = 0,
                    Credit = (double)soTien,
                    Explanation = dienGiai
                };
                _context.AccountingJournalLines.Add(lineCoDong);

                // Ghi sổ cái cho dòng Nợ
                var soCaiNo = new SoCai
                {
                    MaTK = tkNo,
                    NgayHachToan = DateTime.Today,
                    SoChungTu = soChungTu,
                    DienGiai = dienGiai,
                    SoTienNo = soTien,
                    SoTienCo = 0,
                    JournalBatchId = batch.Id,
                    DoiTuong = doiTuong,
                    NguoiTao = userName,
                    NgayTao = now
                };
                _context.SoCais.Add(soCaiNo);

                // Ghi sổ cái cho dòng Có
                var soCaiCo = new SoCai
                {
                    MaTK = tkCo,
                    NgayHachToan = DateTime.Today,
                    SoChungTu = soChungTu,
                    DienGiai = dienGiai,
                    SoTienNo = 0,
                    SoTienCo = soTien,
                    JournalBatchId = batch.Id,
                    DoiTuong = doiTuong,
                    NguoiTao = userName,
                    NgayTao = now
                };
                _context.SoCais.Add(soCaiCo);
            }

            // Cập nhật tổng tiền batch
            batch.Lines = _context.AccountingJournalLines
                .Where(l => l.BatchId == batch.Id)
                .ToList();

            await _context.SaveChangesAsync();

            // Ghi audit log
            try
            {
                using var auditCtx = new AppDbContext();
                auditCtx.AuditLogs.Add(new AuditLog
                {
                    ThoiGian = now,
                    NguoiThucHien = userName,
                    UserId = Helpers.AppSession.CurrentUser?.Id,
                    HanhDong = "KETCUYEN",
                    TenBang = "AccountingJournalBatch",
                    IdBanGhi = batch.Id,
                    DuLieuMoi = $"Hạch toán {loaiNghiepVu} — {soTien:N0}đ — {doiTuong ?? ""}",
                    DiaChiIP = GetLocalIPAddress()
                });
                await auditCtx.SaveChangesAsync();
            }
            catch { /* Không throw nếu audit lỗi */ }

            return batch;
        }

        /// <summary>
        /// Xem trước bút toán mà không lưu DB.
        /// </summary>
        public async Task<List<XemTruocButToanDto>> XemTruocButToanAsync(string loaiNghiepVu, decimal soTien)
        {
            var result = new List<XemTruocButToanDto>();

            if (!_map.ContainsKey(loaiNghiepVu))
            {
                throw new ArgumentException($"Không tìm thấy nghiệp vụ: {loaiNghiepVu}");
            }

            var butToans = _map[loaiNghiepVu];
            foreach (var (tkNo, tkCo, dienGiai) in butToans)
            {
                result.Add(new XemTruocButToanDto
                {
                    TKNo = tkNo,
                    TenTKNo = await GetTenTKAsync(tkNo),
                    TKCo = tkCo,
                    TenTKCo = await GetTenTKAsync(tkCo),
                    SoTien = soTien,
                    DienGiai = dienGiai
                });
            }

            return result;
        }

        /// <summary>
        /// Lấy tên tài khoản theo mã.
        /// </summary>
        private async Task<string> GetTenTKAsync(string maTk)
        {
            var tk = await _context.TaiKhoanKeToans
                .FirstOrDefaultAsync(t => t.MaTK == maTk);
            return tk?.TenTK ?? maTk;
        }

        /// <summary>
        /// Lấy danh sách mã nghiệp vụ được hỗ trợ.
        /// </summary>
        public List<string> GetDanhSachNghiepVu()
        {
            return _map.Keys.ToList();
        }
    }

    /// <summary>
    /// DTO xem trước bút toán.
    /// </summary>
    public class XemTruocButToanDto
    {
        public string TKNo { get; set; } = "";
        public string TenTKNo { get; set; } = "";
        public string TKCo { get; set; } = "";
        public string TenTKCo { get; set; } = "";
        public decimal SoTien { get; set; }
        public string DienGiai { get; set; } = "";
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Views
{
    public partial class JournalPreviewWindow : Window
    {
        public sealed class LineVm
        {
            public int LineNo { get; init; }
            public string AccountCode { get; init; } = "";
            public string AccountName { get; init; } = "";
            public double Debit { get; init; }
            public double Credit { get; init; }
            public string DebitText => Debit > 0 ? AccountingJournalMapper.FormatMoney(Debit) : "";
            public string CreditText => Credit > 0 ? AccountingJournalMapper.FormatMoney(Credit) : "";
            public string Explanation { get; init; } = "";
        }

        private readonly AccountingTrigger _trigger;
        private readonly IReadOnlyList<JournalPreviewLine> _lines;
        private readonly string? _triggerDocument;
        private readonly string? _referenceType;
        private readonly int? _referenceId;

        public bool Posted { get; private set; }

        public JournalPreviewWindow(
            AccountingTrigger trigger,
            IReadOnlyList<JournalPreviewLine> lines,
            string? triggerDocument = null,
            string? referenceType = null,
            int? referenceId = null)
        {
            InitializeComponent();
            _trigger = trigger;
            _lines = lines;
            _triggerDocument = triggerDocument;
            _referenceType = referenceType;
            _referenceId = referenceId;

            txtTitle.Text = AccountingJournalMapper.TriggerDisplayName(trigger);
            if (lines == null || lines.Count == 0)
            {
                txtHint.Text = "Không phát sinh bút toán (nghiệp vụ chỉ ghi nhận hệ thống / chưa hạch toán).";
                gridLines.ItemsSource = new ObservableCollection<LineVm>();
                btnConfirm.IsEnabled = false;
                txtBalance.Text = "";
                return;
            }

            txtHint.Text = "Kiểm tra tổng Nợ = tổng Có trước khi xác nhận. Chứng từ nguồn: " + (triggerDocument ?? "—");
            var vms = lines.Select(l => new LineVm
            {
                LineNo = l.LineNo,
                AccountCode = l.AccountCode,
                AccountName = l.AccountName,
                Debit = l.Debit,
                Credit = l.Credit,
                Explanation = l.Explanation
            }).ToList();
            gridLines.ItemsSource = new ObservableCollection<LineVm>(vms);

            var sumDr = lines.Sum(x => x.Debit);
            var sumCr = lines.Sum(x => x.Credit);
            var ok = Math.Abs(sumDr - sumCr) < 0.01 && sumDr > 0;
            txtBalance.Text = $"Tổng Nợ: {AccountingJournalMapper.FormatMoney(sumDr)}   |   Tổng Có: {AccountingJournalMapper.FormatMoney(sumCr)}" +
                              (ok ? "   ✓ Cân đối" : "   ⚠ Chưa cân đối hoặc số tiền = 0");
            btnConfirm.IsEnabled = ok;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_lines == null || _lines.Count == 0)
            {
                DialogResult = false;
                Close();
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var batch = new AccountingJournalBatch
                {
                    EventType = _trigger.ToString(),
                    TriggerDocument = _triggerDocument,
                    ReferenceType = _referenceType,
                    ReferenceId = _referenceId,
                    Posted = true,
                    PostedAt = DateTime.Now,
                    UserId = AppSession.CurrentUser?.Id
                };
                db.Set<AccountingJournalBatch>().Add(batch);
                db.SaveChanges();

                int i = 0;
                foreach (var l in _lines.OrderBy(x => x.LineNo))
                {
                    db.Set<AccountingJournalLine>().Add(new AccountingJournalLine
                    {
                        BatchId = batch.Id,
                        LineNo = ++i,
                        AccountCode = l.AccountCode,
                        AccountName = l.AccountName,
                        Debit = l.Debit,
                        Credit = l.Credit,
                        Explanation = l.Explanation
                    });
                }
                db.SaveChanges();
                Posted = true;
                MessageBox.Show("Đã ghi sổ bút toán tự động.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không ghi được CSDL: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

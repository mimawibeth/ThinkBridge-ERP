using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

/// <summary>
/// Server-side structured PDF report generation using QuestPDF.
/// Layout: Header → Summary → Detailed Data Table → Footer
/// </summary>
public class PdfReportService
{
    private readonly IReportService _reportService;
    private readonly ISuperAdminService _superAdminService;
    private readonly ILogger<PdfReportService> _logger;

    // Brand palette
    private static readonly Color Primary = Color.FromHex("#0B4F6C");
    private static readonly Color PrimaryLight = Color.FromHex("#E8F4F8");
    private static readonly Color Success = Color.FromHex("#16A34A");
    private static readonly Color Warning = Color.FromHex("#D97706");
    private static readonly Color Danger = Color.FromHex("#DC2626");
    private static readonly Color Muted = Color.FromHex("#6B7280");
    private static readonly Color LightBg = Color.FromHex("#F8FAFC");
    private static readonly Color BorderLight = Color.FromHex("#E5E7EB");
    private static readonly Color Purple = Color.FromHex("#7C3AED");
    private static readonly Color HeaderText = Color.FromHex("#1E293B");

    public PdfReportService(
        IReportService reportService,
        ISuperAdminService superAdminService,
        ILogger<PdfReportService> logger)
    {
        _reportService = reportService;
        _superAdminService = superAdminService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    //  PROJECT MANAGER PDF
    // ═══════════════════════════════════════════════════

    public async Task<byte[]> GenerateProjectManagerPdfAsync(
        int companyId, int userId, string userRole,
        string fullName, string companyName, string period = "month")
    {
        var now = DateTime.UtcNow;
        DateTime dateFrom = period switch
        {
            "week" => now.AddDays(-7),
            "quarter" => now.AddMonths(-3),
            "year" => now.AddYears(-1),
            _ => now.AddMonths(-1)
        };
        DateTime dateTo = now;

        var dashboard = await _reportService.GetReportDashboardAsync(companyId, userId, userRole, dateFrom, dateTo);
        var projects = await _reportService.GetProjectProgressAsync(companyId, userId, userRole, dateFrom, dateTo);
        var tasks = await _reportService.GetTaskDistributionAsync(companyId, userId, userRole, dateFrom, dateTo);
        var team = await _reportService.GetTeamPerformanceAsync(companyId, userId, userRole, dateFrom, dateTo);

        var periodLabel = period switch
        {
            "week" => "Last Week",
            "quarter" => "Last Quarter",
            "year" => "Last Year",
            _ => "Last Month"
        };

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                // ── HEADER ──
                page.Header().Element(h =>
                    ComposeHeader(h, companyName, $"{companyName} \u2013 Official Report", fullName, "Project Manager"));

                // ── CONTENT ──
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);

                    // ─ SUMMARY SECTION ─
                    if (dashboard.Success)
                    {
                        col.Item().Element(c => SectionHeading(c, "Summary"));
                        col.Item().Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => MetricCard(c, "Tasks Completed",
                                $"{dashboard.TasksCompleted} / {dashboard.TotalTasks}",
                                dashboard.TotalTasks > 0 ? $"{Math.Round((decimal)dashboard.TasksCompleted / dashboard.TotalTasks * 100)}% done" : "\u2014"));
                            row.RelativeItem().Element(c => MetricCard(c, "On-Time Delivery",
                                $"{dashboard.OnTimeDeliveryPercent}%", "of completed tasks", Success));
                            row.RelativeItem().Element(c => MetricCard(c, "Team Utilization",
                                $"{dashboard.TeamUtilizationPercent}%", "members with active tasks", Primary));
                            row.RelativeItem().Element(c => MetricCard(c, "Blockers",
                                $"{dashboard.BlockersResolved} / {dashboard.TotalBlockers}",
                                $"{dashboard.TotalBlockers - dashboard.BlockersResolved} unresolved",
                                dashboard.TotalBlockers - dashboard.BlockersResolved > 0 ? Danger : Success));
                        });
                    }

                    // Task breakdown row
                    if (tasks.Success)
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => SmallStat(c, "Completed", tasks.Completed.ToString(), Success));
                            row.RelativeItem().Element(c => SmallStat(c, "In Progress", tasks.InProgress.ToString(), Warning));
                            row.RelativeItem().Element(c => SmallStat(c, "In Review", tasks.InReview.ToString(), Primary));
                            row.RelativeItem().Element(c => SmallStat(c, "Not Started", tasks.NotStarted.ToString(), Muted));
                        });
                    }

                    // ─ PROJECT PROGRESS TABLE ─
                    if (projects.Success && projects.Projects.Count > 0)
                    {
                        col.Item().Element(c => SectionHeading(c, "Project Progress"));
                        col.Item().Element(c => ComposeTable(c,
                            new[] { "#", "Project Name", "Status", "Progress" },
                            new[] { 30f, 0f, 80f, 70f },
                            projects.Projects.Select((p, i) => new[]
                            {
                                (i + 1).ToString(),
                                p.ProjectName,
                                p.Status,
                                $"{p.Progress:F0}%"
                            }).ToList()));
                    }

                    // ─ TEAM PERFORMANCE TABLE ─
                    if (team.Success && team.Members.Count > 0)
                    {
                        col.Item().Element(c => SectionHeading(c, "Team Performance"));
                        col.Item().Element(c => ComposeTable(c,
                            new[] { "#", "Member", "Assigned", "Completed", "Completion %", "On-Time %" },
                            new[] { 25f, 0f, 60f, 65f, 75f, 65f },
                            team.Members.Select((m, i) => new[]
                            {
                                (i + 1).ToString(),
                                m.FullName,
                                m.TasksAssigned.ToString(),
                                m.TasksCompleted.ToString(),
                                $"{m.CompletionRate:F0}%",
                                $"{m.OnTimePercent:F0}%"
                            }).ToList()));
                    }
                });

                // ── FOOTER ──
                page.Footer().Element(f => ComposeFooter(f));
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  COMPANY ADMIN PDF
    // ═══════════════════════════════════════════════════

    public async Task<byte[]> GenerateCompanyAdminPdfAsync(
        int companyId, string fullName, string companyName)
    {
        var data = await _reportService.GetCompanyReportAsync(companyId);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                // ── HEADER ──
                page.Header().Element(h =>
                    ComposeHeader(h, companyName, $"{companyName} \u2013 Official Report", fullName, "Company Admin"));

                // ── CONTENT ──
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);

                    if (data.Success)
                    {
                        // ─ SUMMARY SECTION ─
                        col.Item().Element(c => SectionHeading(c, "Summary"));

                        var usagePct = data.MaxUsers.HasValue && data.MaxUsers > 0
                            ? $"{Math.Round((decimal)data.ActiveUsers / data.MaxUsers.Value * 100)}% capacity"
                            : "Unlimited seats";

                        col.Item().Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => MetricCard(c, "Total Users",
                                data.TotalUsers.ToString(),
                                $"{data.ActiveUsers} active \u00b7 {data.InactiveUsers} inactive"));
                            row.RelativeItem().Element(c => MetricCard(c, "Active Logins (30d)",
                                data.RecentLoginsLast30Days.ToString(),
                                data.TotalUsers > 0
                                    ? $"{Math.Round((decimal)data.RecentLoginsLast30Days / data.TotalUsers * 100)}% of total"
                                    : "\u2014", Primary));
                            row.RelativeItem().Element(c => MetricCard(c, "Current Plan",
                                data.PlanName, data.SubscriptionStatus, Purple));
                            row.RelativeItem().Element(c => MetricCard(c, "Seat Usage",
                                $"{data.ActiveUsers} / {(data.MaxUsers?.ToString() ?? "\u221e")}",
                                usagePct, Success));
                        });

                        // ─ SUBSCRIPTION DETAILS TABLE ─
                        col.Item().Element(c => SectionHeading(c, "Subscription Details"));
                        col.Item().Element(c =>
                        {
                            c.Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(2.5f);
                                });

                                InfoRow(table, "Plan", data.PlanName);
                                InfoRow(table, "Status", data.SubscriptionStatus);
                                InfoRow(table, "Price", $"\u20b1{data.PlanPrice:N2} / {data.BillingCycle}");
                                InfoRow(table, "Start Date", data.SubscriptionStart?.ToString("MMM dd, yyyy") ?? "\u2014");
                                InfoRow(table, "End Date", data.SubscriptionEnd?.ToString("MMM dd, yyyy") ?? "\u2014");
                                InfoRow(table, "Max Users", data.MaxUsers?.ToString() ?? "Unlimited");
                                InfoRow(table, "Max Projects", data.MaxProjects?.ToString() ?? "Unlimited");
                            });
                        });

                        // ─ USERS BY ROLE TABLE ─
                        if (data.UsersByRole.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Users by Role"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "Role", "Count" },
                                new[] { 0f, 80f },
                                data.UsersByRole.Select(r => new[]
                                {
                                    r.RoleName,
                                    r.Count.ToString()
                                }).ToList()));
                        }

                        // ─ TEAM MEMBERS TABLE ─
                        if (data.RecentUsers.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Team Members"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "#", "Name", "Email", "Role", "Status", "Last Login" },
                                new[] { 25f, 0f, 0f, 80f, 55f, 80f },
                                data.RecentUsers.Select((u, i) => new[]
                                {
                                    (i + 1).ToString(),
                                    u.FullName,
                                    u.Email,
                                    u.RoleName,
                                    u.Status,
                                    u.LastLoginAt?.ToString("MMM dd, yyyy") ?? "Never"
                                }).ToList()));
                        }
                    }
                });

                // ── FOOTER ──
                page.Footer().Element(f => ComposeFooter(f));
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  SUPER ADMIN PDF
    // ═══════════════════════════════════════════════════

    public async Task<byte[]> GenerateSuperAdminPdfAsync(
        string fullName, DateTime? dateFrom, DateTime? dateTo)
    {
        var data = await _superAdminService.GetPlatformReportAsync(new PlatformReportRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        });

        const string sysCompany = "ThinkBridge ERP";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                // ── HEADER ──
                page.Header().Element(h =>
                    ComposeHeader(h, sysCompany,
                        $"{sysCompany} \u2013 System Financial Overview",
                        fullName, "Super Admin"));

                // ── CONTENT ──
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);

                    if (data.Success)
                    {
                        // ─ SUMMARY SECTION ─
                        col.Item().Element(c => SectionHeading(c, "Financial Summary"));
                        col.Item().Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => MetricCard(c, "Total Companies",
                                data.TotalCompanies.ToString(),
                                $"{data.ActiveCompanies} active \u00b7 {data.InactiveCompanies} inactive"));
                            row.RelativeItem().Element(c => MetricCard(c, "Total Revenue",
                                $"\u20b1{data.TotalRevenue:N2}",
                                $"{data.CompletedPayments} completed", Success));
                            row.RelativeItem().Element(c => MetricCard(c, "Monthly Recurring",
                                $"\u20b1{data.MonthlyRecurringRevenue:N2}", "MRR", Primary));
                            row.RelativeItem().Element(c => MetricCard(c, "Pending Amount",
                                $"\u20b1{data.PendingAmount:N2}",
                                $"{data.PendingPayments} pending", Warning));
                        });

                        // Subscription breakdown
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => SmallStat(c, "Active", data.ActiveSubscriptions.ToString(), Success));
                            row.RelativeItem().Element(c => SmallStat(c, "Trial", data.TrialSubscriptions.ToString(), Purple));
                            row.RelativeItem().Element(c => SmallStat(c, "Expired", data.ExpiredSubscriptions.ToString(), Danger));
                            row.RelativeItem().Element(c => SmallStat(c, "Cancelled", data.CancelledSubscriptions.ToString(), Warning));
                        });

                        // ─ PLAN DISTRIBUTION TABLE ─
                        if (data.PlanDistribution.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Plan Distribution"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "#", "Plan", "Subscribers", "Revenue" },
                                new[] { 25f, 0f, 80f, 100f },
                                data.PlanDistribution.Select((p, i) => new[]
                                {
                                    (i + 1).ToString(),
                                    p.PlanName,
                                    p.Count.ToString(),
                                    $"\u20b1{p.Revenue:N2}"
                                }).ToList()));
                        }

                        // ─ MONTHLY REVENUE TABLE ─
                        if (data.MonthlyRevenue.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Monthly Revenue Breakdown"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "#", "Month", "Revenue", "Payments" },
                                new[] { 25f, 0f, 100f, 70f },
                                data.MonthlyRevenue.Select((m, i) => new[]
                                {
                                    (i + 1).ToString(),
                                    m.Label,
                                    $"\u20b1{m.Revenue:N2}",
                                    m.PaymentCount.ToString()
                                }).ToList()));
                        }

                        // ─ PAYMENT SUMMARY ─
                        col.Item().Element(c => SectionHeading(c, "Payment Summary"));
                        col.Item().Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => MetricCard(c, "Completed",
                                data.CompletedPayments.ToString(),
                                $"\u20b1{data.TotalRevenue:N2} collected", Success));
                            row.RelativeItem().Element(c => MetricCard(c, "Pending",
                                data.PendingPayments.ToString(),
                                $"\u20b1{data.PendingAmount:N2} outstanding", Warning));
                            row.RelativeItem().Element(c => MetricCard(c, "Failed",
                                data.FailedPayments.ToString(), "", Danger));
                        });

                        // ─ TOP COMPANIES BY REVENUE TABLE ─
                        if (data.TopCompaniesByRevenue.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Top Companies by Revenue"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "#", "Company", "Plan", "Revenue" },
                                new[] { 25f, 0f, 80f, 100f },
                                data.TopCompaniesByRevenue.Select((x, i) => new[]
                                {
                                    (i + 1).ToString(),
                                    x.CompanyName,
                                    x.PlanName,
                                    $"\u20b1{x.TotalRevenue:N2}"
                                }).ToList()));
                        }

                        // ─ TOP COMPANIES BY USERS TABLE ─
                        if (data.TopCompaniesByUsers.Count > 0)
                        {
                            col.Item().Element(c => SectionHeading(c, "Top Companies by Users"));
                            col.Item().Element(c => ComposeTable(c,
                                new[] { "#", "Company", "Industry", "Plan", "Users" },
                                new[] { 25f, 0f, 0f, 80f, 50f },
                                data.TopCompaniesByUsers.Select((x, i) => new[]
                                {
                                    (i + 1).ToString(),
                                    x.CompanyName,
                                    x.Industry,
                                    x.PlanName,
                                    x.UserCount.ToString()
                                }).ToList()));
                        }
                    }
                });

                // ── FOOTER ──
                page.Footer().Element(f => ComposeFooter(f));
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  PAYMENT REPORT PDF
    // ═══════════════════════════════════════════════════

    public async Task<byte[]> GeneratePaymentReportPdfAsync(
        string fullName, DateTime? dateFrom, DateTime? dateTo)
    {
        var from = dateFrom ?? DateTime.UtcNow.AddMonths(-1);
        var to = dateTo ?? DateTime.UtcNow;
        var pht = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        var stats = await _superAdminService.GetPaymentStatsAsync(null, null, from, to);
        var payments = await _superAdminService.GetPaymentsAsync(new PaymentFilterRequest
        {
            DateFrom = from,
            DateTo = to,
            Page = 1,
            PageSize = 500
        });

        const string sysCompany = "ThinkBridge ERP";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                page.Header().Element(h =>
                    ComposeHeader(h, sysCompany,
                        $"{sysCompany} \u2013 Payment Report",
                        fullName, "Super Admin"));

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);

                    // Period info
                    col.Item().Text($"Period: {from:MMM dd, yyyy} \u2014 {to:MMM dd, yyyy}  \u2022  Generated: {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pht):MMM dd, yyyy hh:mm tt} PHT")
                        .FontSize(8).FontColor(Muted);

                    if (stats.Success)
                    {
                        col.Item().Element(c => SectionHeading(c, "Payment Summary"));
                        col.Item().Row(row =>
                        {
                            row.Spacing(6);
                            row.RelativeItem().Element(c => MetricCard(c, "Total Revenue",
                                $"\u20b1{stats.TotalRevenue:N2}",
                                $"{stats.CompletedCount} completed", Success));
                            row.RelativeItem().Element(c => MetricCard(c, "Pending",
                                $"\u20b1{stats.PendingAmount:N2}",
                                $"{stats.PendingCount} invoices", Warning));
                            row.RelativeItem().Element(c => MetricCard(c, "Overdue",
                                $"\u20b1{stats.OverdueAmount:N2}",
                                $"{stats.OverdueCount} invoices", Danger));
                            row.RelativeItem().Element(c => MetricCard(c, "Failed",
                                stats.FailedCount.ToString(),
                                "transactions", Danger));
                        });
                    }

                    if (payments.Success && payments.Payments.Count > 0)
                    {
                        col.Item().Element(c => SectionHeading(c, $"Payment Transactions ({payments.TotalCount})"));
                        col.Item().Element(c => ComposeTable(c,
                            new[] { "#", "Company", "Invoice", "Amount", "Method", "Status", "Date (PHT)" },
                            new[] { 20f, 0f, 70f, 65f, 60f, 55f, 90f },
                            payments.Payments.Select((p, i) => new[]
                            {
                                (i + 1).ToString(),
                                p.CompanyName,
                                p.InvoiceNumber ?? "N/A",
                                $"\u20b1{p.Amount:N2}",
                                p.PaymentMethod ?? p.Provider,
                                p.Status,
                                p.PaidAt.HasValue
                                    ? TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt.Value, pht).ToString("MMM dd, yyyy hh:mm tt")
                                    : TimeZoneInfo.ConvertTimeFromUtc(p.CreatedAt, pht).ToString("MMM dd, yyyy hh:mm tt")
                            }).ToList()));
                    }
                });

                page.Footer().Element(f => ComposeFooter(f));
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  SHARED BUILDING BLOCKS
    // ═══════════════════════════════════════════════════

    /// <summary>Page size, margins, default text style.</summary>
    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(40);
        page.MarginVertical(30);
        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));
    }

    // ── HEADER ────────────────────────────────────────
    /// <summary>
    /// Dynamic PDF Header:
    ///   Left  — Logo placeholder + Company Name + Report Title
    ///   Right — Date Generated, Generated By (Full Name + Role)
    /// Followed by a coloured divider line.
    /// </summary>
    private static void ComposeHeader(IContainer container,
        string companyName, string reportTitle,
        string generatedBy, string role)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(6).Row(row =>
            {
                // Left: logo placeholder + company name + title
                row.ConstantItem(36).Height(36)
                    .Background(PrimaryLight)
                    .Border(1).BorderColor(Primary)
                    .AlignCenter().AlignMiddle()
                    .Text(companyName.Length > 0 ? companyName[..1] : "T")
                    .FontSize(16).Bold().FontColor(Primary);

                row.ConstantItem(10); // spacer

                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(companyName)
                        .FontSize(15).Bold().FontColor(HeaderText);
                    left.Item().PaddingTop(1).Text(reportTitle)
                        .FontSize(9).FontColor(Muted);
                });

                row.ConstantItem(170).AlignRight().AlignBottom().Column(right =>
                {
                    right.Item().AlignRight().Text($"Date: {DateTime.UtcNow:MMMM dd, yyyy}")
                        .FontSize(8).FontColor(Muted);
                    right.Item().AlignRight().PaddingTop(1)
                        .Text($"Generated by: {generatedBy} ({role})")
                        .FontSize(8).FontColor(Muted);
                });
            });

            // Divider
            col.Item().LineHorizontal(1.5f).LineColor(Primary);
        });
    }

    // ── SECTION HEADING ───────────────────────────────
    private static void SectionHeading(IContainer container, string title)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Item().Text(title).FontSize(11).Bold().FontColor(Primary);
            col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(BorderLight);
        });
    }

    // ── METRIC CARD ───────────────────────────────────
    private static void MetricCard(IContainer container,
        string label, string value, string sub, Color? accent = null)
    {
        var color = accent ?? Primary;
        container
            .Border(1).BorderColor(BorderLight)
            .Background(Colors.White)
            .Padding(10)
            .Column(col =>
            {
                col.Item().Text(label).FontSize(7).Bold().FontColor(Muted);
                col.Item().PaddingTop(3).Text(value).FontSize(15).Bold().FontColor(color);
                if (!string.IsNullOrEmpty(sub))
                    col.Item().PaddingTop(1).Text(sub).FontSize(7).FontColor(Muted);
            });
    }

    // ── SMALL STAT PILL ───────────────────────────────
    private static void SmallStat(IContainer container,
        string label, string value, Color accent)
    {
        container
            .Border(1).BorderColor(BorderLight)
            .Background(Colors.White)
            .Padding(6)
            .Row(row =>
            {
                row.AutoItem().AlignMiddle()
                    .Width(4).Height(18)
                    .Background(accent);
                row.ConstantItem(6);
                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().Text(value).FontSize(12).Bold().FontColor(accent);
                    c.Item().Text(label).FontSize(7).FontColor(Muted);
                });
            });
    }

    // ── GENERIC TABLE ─────────────────────────────────
    /// <summary>
    /// Builds a clean data table.
    /// widths: positive = ConstantColumn(px), 0 = RelativeColumn(fill).
    /// </summary>
    private static void ComposeTable(IContainer container,
        string[] headers, float[] widths, List<string[]> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var w in widths)
                {
                    if (w > 0) cols.ConstantColumn(w);
                    else cols.RelativeColumn();
                }
            });

            // Header row
            table.Header(header =>
            {
                foreach (var h in headers)
                {
                    header.Cell()
                        .Background(Primary)
                        .Padding(6)
                        .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                }
            });

            // Data rows
            for (var r = 0; r < rows.Count; r++)
            {
                var bg = r % 2 == 0 ? Colors.White : Color.FromHex("#F3F4F6");
                foreach (var cell in rows[r])
                {
                    table.Cell()
                        .Background(bg)
                        .BorderBottom(0.5f).BorderColor(BorderLight)
                        .Padding(5)
                        .Text(cell ?? "").FontSize(8);
                }
            }
        });
    }

    // ── INFO ROW (label → value) ──────────────────────
    private static void InfoRow(TableDescriptor table, string label, string value)
    {
        table.Cell()
            .BorderBottom(0.5f).BorderColor(BorderLight)
            .Background(LightBg)
            .Padding(6)
            .Text(label).FontSize(8).Bold().FontColor(Muted);

        table.Cell()
            .BorderBottom(0.5f).BorderColor(BorderLight)
            .Padding(6)
            .Text(value ?? "").FontSize(8);
    }

    // ── FOOTER ────────────────────────────────────────
    /// <summary>
    /// Page footer: system note on left, page X / Y on right.
    /// </summary>
    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(BorderLight);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().AlignBottom().Text(txt =>
                {
                    txt.Span("ThinkBridge ERP").FontSize(7).Bold().FontColor(Muted);
                    txt.Span("  \u2022  ").FontSize(7).FontColor(BorderLight);
                    txt.Span("System-generated report \u2013 unauthorized distribution is prohibited.")
                        .FontSize(7).FontColor(Muted).Italic();
                });
                row.ConstantItem(50).AlignRight().AlignBottom().Text(txt =>
                {
                    txt.CurrentPageNumber().FontSize(7).FontColor(Muted);
                    txt.Span(" / ").FontSize(7).FontColor(Muted);
                    txt.TotalPages().FontSize(7).FontColor(Muted);
                });
            });
        });
    }
}

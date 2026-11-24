using ClosedXML.Excel;
using Cutter.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cutter.Services
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportStoreStatisticsAsync(Guid storeId, DateTime startDate, DateTime endDate,
            List<int>? departmentIds = null, List<int>? subdepartmentIds = null, bool groupByDepartment = false);
    }

    public class ExcelExportService : IExcelExportService
    {
        private readonly ApplicationDbContext _db;

        public ExcelExportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<byte[]> ExportStoreStatisticsAsync(Guid storeId, DateTime startDate, DateTime endDate,
                    List<int>? departmentIds = null, List<int>? subdepartmentIds = null, bool groupByDepartment = false)
        {
            var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            string storeName = store?.Name ?? "Название магазина";

            // 1. Кэш товаров (SKU/BarCode -> DeptId, SubdeptId)
            var storeItemsList = await _db.StoreItems
                .Include(si => si.Item)
                .Include(si => si.StoreSubdepartment)
                .Where(si => si.StoreId == storeId)
                .ToListAsync();

            var itemDeptCache = new Dictionary<string, (int? DeptId, int? SubdeptId)>();
            foreach (var si in storeItemsList)
            {
                var deptId = si.StoreSubdepartment?.StoreDepartmentId;
                var subdeptId = si.StoreSubdepartment?.Id;
                var value = (deptId, subdeptId);

                if (!string.IsNullOrEmpty(si.Item?.SKU) && !itemDeptCache.ContainsKey(si.Item.SKU))
                    itemDeptCache[si.Item.SKU] = value;

                if (!string.IsNullOrEmpty(si.Item?.BarCode) && !itemDeptCache.ContainsKey(si.Item.BarCode))
                    itemDeptCache[si.Item.BarCode] = value;
            }

            // 2. Кэш названий отделов и подотделов (СНАЧАЛА ToList, чтобы избежать ошибок EF Core)
            var deptsList = await _db.StoreDepartments.Where(d => d.StoreId == storeId).ToListAsync();
            var deptNames = deptsList.ToDictionary(d => d.Id, d => d.Name);
            var validDeptIds = deptsList.Select(d => d.Id).ToList();

            var subdeptsList = await _db.StoreSubdepartments.Where(sd => validDeptIds.Contains(sd.StoreDepartmentId)).ToListAsync();
            var subdeptNames = subdeptsList.ToDictionary(sd => sd.Id, sd => sd.Name);

            // 3. Загрузка планов раскроя
            var plans = await _db.CutPlan
                .Where(p => p.User.StoreId == storeId
                    && p.CreateDate >= startDate
                    && p.CreateDate <= endDate.AddDays(1).AddTicks(-1))
                .OrderBy(p => p.CreateDate)
                .ToListAsync();

            var rawData = new List<SheetStatRow>();
            bool isDeptFilterActive = departmentIds != null;
            bool isSubdeptFilterActive = subdepartmentIds != null;

            foreach (var plan in plans)
            {
                var cuttingPlans = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CuttingPlan>>(plan.CuttingPlan);
                if (cuttingPlans == null) continue;

                foreach (var cp in cuttingPlans)
                {
                    foreach (var layout in cp.SheetLayouts)
                    {
                        if (layout.Sheet == null) continue;

                        var itemKey = layout.Sheet.SKU ?? layout.Sheet.BarCode;

                        // Фильтрация
                        if (!string.IsNullOrEmpty(itemKey) && itemDeptCache.TryGetValue(itemKey, out var deptInfo))
                        {
                            bool deptMatch = !isDeptFilterActive || (deptInfo.DeptId.HasValue && departmentIds.Contains(deptInfo.DeptId.Value));
                            bool subdeptMatch = !isSubdeptFilterActive || (deptInfo.SubdeptId.HasValue && subdepartmentIds.Contains(deptInfo.SubdeptId.Value));

                            if (!deptMatch || !subdeptMatch) continue;
                        }
                        else
                        {
                            if (isDeptFilterActive || isSubdeptFilterActive) continue;
                        }

                        decimal layoutSum = 0;
                        if (layout.Services != null)
                        {
                            layoutSum = layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                        }

                        // Получение имен
                        string deptName = "Без отдела";
                        string subdeptName = "Без подотдела";

                        if (!string.IsNullOrEmpty(itemKey) && itemDeptCache.TryGetValue(itemKey, out var infoForName))
                        {
                            if (infoForName.DeptId.HasValue && deptNames.TryGetValue(infoForName.DeptId.Value, out var dName))
                                deptName = dName;
                            if (infoForName.SubdeptId.HasValue && subdeptNames.TryGetValue(infoForName.SubdeptId.Value, out var sdName))
                                subdeptName = sdName;
                        }

                        rawData.Add(new SheetStatRow
                        {
                            Date = plan.CreateDate.Date,
                            Invoice = string.IsNullOrWhiteSpace(cp.Invoice) ? "Б/Н" : cp.Invoice,
                            SheetName = layout.Sheet.Name ?? "Неизвестно",
                            SKU = layout.Sheet.SKU ?? "N/A",
                            DepartmentName = deptName,
                            SubdepartmentName = subdeptName,
                            Price = (decimal)layout.Sheet.Price,
                            Quantity = layout.Sheet.Quantity,
                            Cuts = layout.CutCount,
                            Sum = layoutSum
                        });
                    }
                }
            }

            // 4. Группировка данных
            var groupedData = rawData
                .GroupBy(x => new { x.Date, x.Invoice, x.SheetName, x.SKU, x.Price, x.DepartmentName, x.SubdepartmentName })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Invoice = g.Key.Invoice,
                    SheetName = g.Key.SheetName,
                    SKU = g.Key.SKU,
                    DepartmentName = g.Key.DepartmentName,
                    SubdepartmentName = g.Key.SubdepartmentName,
                    Price = g.Key.Price,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalCuts = g.Sum(x => x.Cuts),
                    TotalSum = g.Sum(x => x.Sum)
                })
                .OrderBy(x => x.DepartmentName)
                .ThenBy(x => x.SubdepartmentName)
                .ThenBy(x => x.Date)
                .ThenBy(x => x.Invoice)
                .ThenByDescending(x => x.TotalSum)
                .ToList();

            // 5. Генерация Excel
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Статистика");

            ws.Style.Fill.BackgroundColor = XLColor.White;
            ws.Style.Font.FontColor = XLColor.Black;
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 11;

            int totalCols = groupByDepartment ? 10 : 8;

            // Шапка
            var storeCell = ws.Cell(1, 1);
            storeCell.Value = storeName;
            storeCell.Style.Font.Bold = true;
            storeCell.Style.Font.FontSize = 16;
            storeCell.Style.Font.FontColor = XLColor.FromHtml("#0055A4");
            storeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, totalCols).Merge();

            var periodCell = ws.Cell(2, 1);
            periodCell.Value = $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
            periodCell.Style.Font.Bold = true;
            periodCell.Style.Font.FontSize = 12;
            periodCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, 2, totalCols).Merge();

            // Заголовки
            var headers = groupByDepartment
                ? new[] { "Отдел", "Подотдел", "Дата", "Договор", "Название материала", "Артикул", "Цена (₽)", "Кол-во (шт)", "Всего резов", "Сумма (₽)" }
                : new[] { "Дата", "Договор", "Название материала", "Артикул", "Цена (₽)", "Кол-во (шт)", "Всего резов", "Сумма (₽)" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0055A4");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#003366");
            }

            int row = 4;
            decimal grandTotalSum = 0;
            int grandTotalQuantity = 0;
            int grandTotalCuts = 0;

            if (groupByDepartment)
            {
                string currentDept = "";
                string currentSubdept = "";
                decimal deptTotalSum = 0;
                int deptTotalQuantity = 0;
                int deptTotalCuts = 0;

                foreach (var item in groupedData)
                {
                    bool subdeptChanged = item.SubdepartmentName != currentSubdept;
                    bool deptChanged = item.DepartmentName != currentDept;

                    if (subdeptChanged && row > 4)
                    {
                        ws.Cell(row, 1).Value = "";
                        ws.Cell(row, 2).Value = $"Итого по подотделу: {currentSubdept}";
                        ws.Cell(row, 2).Style.Font.Bold = true;
                        ws.Cell(row, 2).Style.Font.Italic = true;
                        ws.Range(row, 2, row, 5).Merge();

                        ws.Cell(row, 8).Value = deptTotalQuantity;
                        ws.Cell(row, 8).Style.Font.Bold = true;
                        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Cell(row, 9).Value = deptTotalCuts;
                        ws.Cell(row, 9).Style.Font.Bold = true;
                        ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Cell(row, 10).Value = deptTotalSum;
                        ws.Cell(row, 10).Style.Font.Bold = true;
                        ws.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#F58220");
                        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        for (int c = 1; c <= 10; c++)
                        {
                            ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Medium;
                            ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.FromHtml("#0055A4");
                            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F4F8");
                        }
                        row++;
                        deptTotalSum = 0; deptTotalQuantity = 0; deptTotalCuts = 0;
                    }

                    if (deptChanged)
                    {
                        ws.Cell(row, 1).Value = item.DepartmentName;
                        ws.Cell(row, 1).Style.Font.Bold = true;
                        ws.Cell(row, 1).Style.Font.FontSize = 12;
                        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0055A4");
                        ws.Range(row, 1, row, 10).Merge();

                        for (int c = 1; c <= 10; c++)
                        {
                            ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thick;
                            ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.Black;
                        }
                        row++;
                        currentDept = item.DepartmentName;
                    }
                    currentSubdept = item.SubdepartmentName;

                    // Строка данных
                    ws.Cell(row, 3).Value = item.Date.ToString("dd.MM.yyyy");
                    ws.Cell(row, 4).Value = item.Invoice;
                    ws.Cell(row, 5).Value = item.SheetName;
                    ws.Cell(row, 6).Value = item.SKU;

                    var priceCell = ws.Cell(row, 7); priceCell.Value = item.Price; priceCell.Style.NumberFormat.Format = "#,##0.00"; priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    var qtyCell = ws.Cell(row, 8); qtyCell.Value = item.TotalQuantity; qtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cutsCell = ws.Cell(row, 9); cutsCell.Value = item.TotalCuts; cutsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var sumCell = ws.Cell(row, 10); sumCell.Value = item.TotalSum; sumCell.Style.NumberFormat.Format = "#,##0.00"; sumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    for (int c = 1; c <= 10; c++)
                    {
                        ws.Cell(row, c).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, c).Style.Border.BottomBorderColor = XLColor.LightGray;
                    }

                    grandTotalSum += item.TotalSum; grandTotalQuantity += item.TotalQuantity; grandTotalCuts += item.TotalCuts;
                    deptTotalSum += item.TotalSum; deptTotalQuantity += item.TotalQuantity; deptTotalCuts += item.TotalCuts;
                    row++;
                }

                // Итог по последнему подотделу
                if (row > 4)
                {
                    ws.Cell(row, 2).Value = $"Итого по подотделу: {currentSubdept}";
                    ws.Cell(row, 2).Style.Font.Bold = true; ws.Cell(row, 2).Style.Font.Italic = true;
                    ws.Range(row, 2, row, 5).Merge();
                    ws.Cell(row, 8).Value = deptTotalQuantity; ws.Cell(row, 8).Style.Font.Bold = true; ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 9).Value = deptTotalCuts; ws.Cell(row, 9).Style.Font.Bold = true; ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 10).Value = deptTotalSum; ws.Cell(row, 10).Style.Font.Bold = true; ws.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#F58220"); ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00"; ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    for (int c = 1; c <= 10; c++) { ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Medium; ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.FromHtml("#0055A4"); ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F4F8"); }
                    row++;
                }
            }
            else
            {
                // Плоский список
                foreach (var item in groupedData)
                {
                    ws.Cell(row, 1).Value = item.Date.ToString("dd.MM.yyyy");
                    ws.Cell(row, 2).Value = item.Invoice;
                    ws.Cell(row, 3).Value = item.SheetName;
                    ws.Cell(row, 4).Value = item.SKU;

                    var priceCell = ws.Cell(row, 5);
                    priceCell.Value = item.Price;
                    priceCell.Style.NumberFormat.Format = "#,##0.00";
                    priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    var qtyCell = ws.Cell(row, 6);
                    qtyCell.Value = item.TotalQuantity;
                    qtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var cutsCell = ws.Cell(row, 7);
                    cutsCell.Value = item.TotalCuts;
                    cutsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var sumCell = ws.Cell(row, 8);
                    sumCell.Value = item.TotalSum;
                    sumCell.Style.NumberFormat.Format = "#,##0.00";
                    sumCell.Style.Font.Bold = true;
                    sumCell.Style.Font.FontColor = XLColor.FromHtml("#0055A4");
                    sumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    for (int c = 1; c <= 8; c++)
                    {
                        ws.Cell(row, c).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, c).Style.Border.BottomBorderColor = XLColor.LightGray;
                    }

                    grandTotalSum += item.TotalSum;
                    grandTotalQuantity += item.TotalQuantity;
                    grandTotalCuts += item.TotalCuts;
                    row++;
                }
            }

            // Общая итоговая строка
            int mergeCols = groupByDepartment ? 7 : 4;
            int qtyCol = groupByDepartment ? 8 : 6;
            int cutsCol = groupByDepartment ? 9 : 7;
            int sumCol = groupByDepartment ? 10 : 8;

            ws.Cell(row, 1).Value = "ИТОГО за период:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F58220");
            ws.Range(row, 1, row, mergeCols).Merge();

            ws.Cell(row, qtyCol).Value = grandTotalQuantity;
            ws.Cell(row, qtyCol).Style.Font.Bold = true;
            ws.Cell(row, qtyCol).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, qtyCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#F58220");
            ws.Cell(row, qtyCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, cutsCol).Value = grandTotalCuts;
            ws.Cell(row, cutsCol).Style.Font.Bold = true;
            ws.Cell(row, cutsCol).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, cutsCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#F58220");
            ws.Cell(row, cutsCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var totalSumCell = ws.Cell(row, sumCol);
            totalSumCell.Value = grandTotalSum;
            totalSumCell.Style.Font.Bold = true;
            totalSumCell.Style.Font.FontColor = XLColor.White;
            totalSumCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F58220");
            totalSumCell.Style.NumberFormat.Format = "#,##0.00";
            totalSumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            for (int c = 1; c <= totalCols; c++)
            {
                ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thick;
                ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.Black;
            }

            ws.Columns().AdjustToContents();
            if (groupByDepartment) ws.Column(5).Width = 40; else ws.Column(3).Width = 40;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }

    internal class SheetStatRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string SheetName { get; set; }
        public string SKU { get; set; }
        public string DepartmentName { get; set; } = "Без отдела";
        public string SubdepartmentName { get; set; } = "Без подотдела";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Cuts { get; set; }
        public decimal Sum { get; set; }
    }
}
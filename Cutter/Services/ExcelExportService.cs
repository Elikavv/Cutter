using ClosedXML.Excel;
using Cutter.Data; // Убедись, что пространство имен правильное для ApplicationDbContext и Store
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
        Task<byte[]> ExportStoreStatisticsAsync(Guid storeId, DateTime startDate, DateTime endDate);
    }

    public class ExcelExportService : IExcelExportService
    {
        private readonly ApplicationDbContext _db;

        public ExcelExportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<byte[]> ExportStoreStatisticsAsync(Guid storeId, DateTime startDate, DateTime endDate)
        {
            // 1. Получаем название магазина для шапки
            var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            string storeName = store?.Name ?? "Название магазина";

            // 2. Загружаем данные за период
            var plans = await _db.CutPlan
                .Where(p => p.User.StoreId == storeId
                    && p.CreateDate >= startDate
                    && p.CreateDate <= endDate.AddDays(1).AddTicks(-1))
                .OrderBy(p => p.CreateDate)
                .ToListAsync();

            // 3. Собираем плоские данные
            var rawData = new List<SheetStatRow>();

            foreach (var plan in plans)
            {
                var cuttingPlans = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CuttingPlan>>(plan.CuttingPlan);
                if (cuttingPlans == null) continue;

                foreach (var cp in cuttingPlans)
                {
                    foreach (var layout in cp.SheetLayouts)
                    {
                        if (layout.Sheet == null) continue;

                        decimal layoutSum = 0;
                        if (layout.Services != null)
                        {
                            layoutSum = layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                        }

                        rawData.Add(new SheetStatRow
                        {
                            Date = plan.CreateDate.Date, // Только дата, без времени
                            Invoice = string.IsNullOrWhiteSpace(cp.Invoice) ? "Б/Н" : cp.Invoice, // Договор
                            SheetName = layout.Sheet.Name ?? "Неизвестно",
                            SKU = layout.Sheet.SKU ?? "N/A",
                            Price = (decimal)layout.Sheet.Price,
                            Quantity = layout.Sheet.Quantity,
                            Cuts = layout.CutCount,
                            Sum = layoutSum
                        });
                    }
                }
            }

            // 4. ГРУППИРУЕМ: Дата -> Договор -> Материал
            var groupedData = rawData
                .GroupBy(x => new { x.Date, x.Invoice, x.SheetName, x.SKU, x.Price })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Invoice = g.Key.Invoice,
                    SheetName = g.Key.SheetName,
                    SKU = g.Key.SKU,
                    Price = g.Key.Price,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalCuts = g.Sum(x => x.Cuts),
                    TotalSum = g.Sum(x => x.Sum)
                })
                .OrderBy(x => x.Date)          // Сначала сортируем по дате
                .ThenBy(x => x.Invoice)        // Потом по договору
                .ThenByDescending(x => x.TotalSum) // Внутри группы по сумме
                .ToList();

            // 5. Генерация Excel
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Статистика");

            // Базовые стили листа
            ws.Style.Fill.BackgroundColor = XLColor.White;
            ws.Style.Font.FontColor = XLColor.Black;
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 11;

            // --- ШАПКА ---
            // Строка 1: Название магазина
            var storeCell = ws.Cell(1, 1);
            storeCell.Value = storeName;
            storeCell.Style.Font.Bold = true;
            storeCell.Style.Font.FontSize = 16;
            storeCell.Style.Font.FontColor = XLColor.FromHtml("#0055A4");
            storeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, 8).Merge();

            // Строка 2: Период
            var periodCell = ws.Cell(2, 1);
            periodCell.Value = $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
            periodCell.Style.Font.Bold = true;
            periodCell.Style.Font.FontSize = 12;
            periodCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, 2, 8).Merge();

            // Строка 3: Заголовки таблицы (8 колонок)
            var headers = new[]
            {
                "Дата", "Договор", "Название материала", "Артикул",
                "Цена (₽)", "Кол-во (шт)", "Всего резов", "Сумма (₽)"
            };

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

            // 6. Запись данных
            int row = 4;
            decimal grandTotalSum = 0;
            int grandTotalQuantity = 0;
            int grandTotalCuts = 0;

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

                // Тонкие границы между строками
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

            // 7. Итоговая строка
            ws.Cell(row, 1).Value = "ИТОГО за период:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#F58220");
            ws.Range(row, 1, row, 4).Merge(); // Объединяем первые 4 ячейки для красоты

            ws.Cell(row, 6).Value = grandTotalQuantity;
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = grandTotalCuts;
            ws.Cell(row, 7).Style.Font.Bold = true;
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var totalSumCell = ws.Cell(row, 8);
            totalSumCell.Value = grandTotalSum;
            totalSumCell.Style.Font.Bold = true;
            totalSumCell.Style.Font.FontColor = XLColor.FromHtml("#F58220");
            totalSumCell.Style.NumberFormat.Format = "#,##0.00";
            totalSumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Жирная черная линия над итогами
            for (int c = 1; c <= 8; c++)
            {
                ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thick;
                ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.Black;
            }

            // 8. Автоширина колонок
            ws.Columns().AdjustToContents();

            // Немного расширим колонку с названием материала, чтобы текст не сплющивался
            ws.Column(3).Width = 40;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }

    // Вспомогательный класс для сбора данных
    internal class SheetStatRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string SheetName { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Cuts { get; set; }
        public decimal Sum { get; set; }
    }
}
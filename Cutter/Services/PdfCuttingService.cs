using Cutter.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;

namespace Cutter.Services
{
    public class PdfCuttingService
    {

        private readonly SvgLayoutService _svgService;

        public enum PdfPrintMode
        {
            Full = 0,          // Бланк + Раскрой
            BlankOnly = 1,     // Только бланк
            LayoutsOnly = 2    // Только раскрой
        }

        public PdfCuttingService(SvgLayoutService svgService)
        {
            _svgService = svgService;
        }
        public byte[] GeneratePdf(UserCuttingPlan plan, PdfPrintMode mode = PdfPrintMode.Full)
        {
            if (plan == null || plan.CuttingPlan == null)
                throw new ArgumentException("Нет планов раскроя для генерации PDF");

            var allLayouts = plan.CuttingPlan
                .SelectMany(p => p.SheetLayouts)
                .Where(layout => layout.CutCount > 0 || layout.DetailsCount > 0 || !layout.IsEmpty)
                .ToList();

            /*if (allLayouts.Count == 0)
                throw new ArgumentException("Нет данных для генерации PDF");*/

            var createdAt = DateTime.Now;

            return Document.Create(container =>
            {
                if (mode == PdfPrintMode.Full || mode == PdfPrintMode.BlankOnly)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().ShowOnce().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        page.Footer().Element(ComposeFooter);
                    });
                }

                // ==========================================================
                // СТРАНИЦЫ 2+: РАСКРОЙ ЛИСТОВ (Если они есть)
                // ==========================================================
                if (mode == PdfPrintMode.Full || mode == PdfPrintMode.LayoutsOnly)
                {
                    var layoutsToPrint = allLayouts.Where(x => x.Details.Any() && x.CutCount > 0).ToList();


                    if (layoutsToPrint.Any())
                {
                    int totalLayouts = layoutsToPrint.Count;
                    int layoutIndex = 1;

                    foreach (var layout in layoutsToPrint.Where(x => x.Details.Any() && x.CutCount > 0))
                    {
                        container.Page(page =>
                        {
                            // Раскрой лучше смотрится в альбомной ориентации
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(1, Unit.Centimetre);
                            page.DefaultTextStyle(x => x.FontSize(10));

                            // Тот же самый футер для страниц раскроя
                            page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                            {

                                row.RelativeItem().Text($"Бланк № {plan.NumCut}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken2);

                                row.RelativeItem().AlignRight().Text(x =>
                                {
                                    x.Span("Страница ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            page.Content().Column(col =>
                            {
                                // Заголовок страницы раскроя
                                col.Item().Text($"Раскрой листа {layoutIndex} из {totalLayouts}")
                                         .FontSize(14).Bold().AlignCenter();

                                // Информация о листе
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Материал: {layout.Sheet.Name} ({layout.Sheet.Length} × {layout.Sheet.Width} мм)").Bold();
                                    row.RelativeItem().AlignRight().Text($"Артикул: {layout.Sheet.SKU}");
                                });

                                // SVG схема раскроя
                                // ВАЖНО: Замени GetLayoutSvg(layout) на свой метод, который возвращает string с SVG
                                col.Item().Border(1).BorderColor(Colors.Black).Padding(10).Width(500).AlignCenter()
                                                    .Svg(_svgService.GenerateSheetSvgForPdf(layout));

                                // Таблица деталей для этого конкретного листа
                                col.Item().PaddingTop(10).Element(c => ComposeLayoutDetailsTable(c, layout));
                            });
                        });
                        layoutIndex++;
                    }
                }
                }

            }).GeneratePdf();


            // НОВЫЙ МЕТОД: Таблица деталей для конкретного листа раскроя
            void ComposeLayoutDetailsTable(IContainer container, SheetLayout layout)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1); // №
                        columns.RelativeColumn(3); // Название
                        columns.RelativeColumn(2); // Длина
                        columns.RelativeColumn(2); // Ширина
                        columns.RelativeColumn(2); // Кол-во
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("№");
                        header.Cell().Element(HeaderStyle).Text("Название");
                        header.Cell().Element(HeaderStyle).Text("Длина (мм)");
                        header.Cell().Element(HeaderStyle).Text("Ширина (мм)");
                        header.Cell().Element(HeaderStyle).Text("Кол-во");
                    });

                    int idx = 1;
                    // Группируем одинаковые детали, чтобы не дублировать строки
                    var groupedDetails = layout.Details
                        .GroupBy(d => new { d.Name, d.Length, d.Width })
                        .Select(g => new { g.Key.Name, g.Key.Length, g.Key.Width, Count = g.Count() });

                    foreach (var detail in groupedDetails)
                    {
                        table.Cell().Element(CellStyle).Text(idx.ToString());
                        table.Cell().Element(CellStyle).Text(detail.Name);
                        table.Cell().Element(CellStyle).Text(detail.Length.ToString());
                        table.Cell().Element(CellStyle).Text(detail.Width.ToString());
                        table.Cell().Element(CellStyle).Text(detail.Count.ToString());
                        idx++;
                    }
                });
            }

            void ComposeHeader(IContainer container)
            {
                var svgContent = File.ReadAllText("./wwwroot/imgs/lp.svg");

                container.Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(40).Svg(svgContent);
                            row.RelativeItem()
                               .PaddingLeft(120)
                               .Text("БЛАНК РЕЗКА").FontSize(24).Bold().AlignLeft();
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            // Было:
                            // row.ConstantItem(200).Text($"Бланк №{plan.NumCut}").Bold().AlignLeft();

                            // Стало:
                            row.ConstantItem(200).Column(col => {
                                col.Item().Text($"Бланк №{plan.NumCut}").Bold().AlignLeft();

                                // Если номер в смене больше 0, показываем его серым цветом под основным номером
                                if (plan.ShiftNumber > 0)
                                {
                                    col.Item().Text($"(№{plan.ShiftNumber} в смене)")
                                             .FontSize(10)
                                             .FontColor(Colors.Grey.Darken2)
                                             .AlignLeft();
                                }
                            });
                            row.RelativeItem().Text($"Дата создания бланка: {createdAt:dd.MM.yyyy HH:mm}").AlignRight();
                        });

                        if (!string.IsNullOrEmpty(plan.Invoice))
                            col.Item().PaddingTop(15).Text($"Договор №{plan.Invoice}").Bold();

                        col.Item().PaddingTop(15).Text(plan.StoreName).FontSize(12).Bold();
                        col.Item().PaddingTop(10).Text(plan.StoreAddress).FontSize(10);
                        col.Item().PaddingTop(10).Text($"Телефон: {plan.Phone}").FontSize(10);
                        col.Item().PaddingTop(5).Text("Для оплаты в течение дня").Bold().AlignCenter();
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                double TotalCost = 0;

                container.Column(col =>
                {
                    // ==========================================================
                    // 1. ТАБЛИЦА МАТЕРИАЛОВ (Сгруппирована, чтобы не дублировать одинаковые листы)
                    // ==========================================================
                    var groupedSheets = plan.CuttingPlan
                        .SelectMany(cp => cp.SheetLayouts)
                        .Where(sl => sl.Sheet != null && sl.Sheet.BarCode != "OWN")
                        .GroupBy(sl => new { sl.Sheet.SKU, sl.Sheet.Name, sl.Sheet.Price, sl.Sheet.BarCode })
                        .Select(g => new
                        {
                            Sheet = g.First().Sheet,
                            TotalQuantity = g.Sum(sl => sl.Sheet.Quantity)
                        }).ToList();

                    if (groupedSheets.Any())
                    {
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Код
                                columns.RelativeColumn(2); // Артикул
                                columns.RelativeColumn(3); // Наименование
                                columns.RelativeColumn(2); // Штрихкод
                                columns.RelativeColumn(1); // Кол-во
                                columns.RelativeColumn(1); // Цена
                                columns.RelativeColumn(1); // Сумма
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Код товара");
                                header.Cell().Element(HeaderStyle).Text("Артикул");
                                header.Cell().Element(HeaderStyle).Text("Наименование");
                                header.Cell().Element(HeaderStyle).Text("Штрихкод");
                                header.Cell().Element(HeaderStyle).Text("Кол-во");
                                header.Cell().Element(HeaderStyle).Text("Цена (руб)");
                                header.Cell().Element(HeaderStyle).Text("Сумма (руб)");
                            });

                            foreach (var gs in groupedSheets)
                            {
                                var sheet = gs.Sheet;
                                var qty = gs.TotalQuantity;
                                var sum = sheet.Price * qty;
                                TotalCost += (float)sum;

                                table.Cell().Element(CellStyle).Text(sheet.BarCode ?? "Без кода");
                                table.Cell().Element(CellStyle).Text(sheet.SKU ?? "N/A");
                                table.Cell().Element(CellStyleNoRight).Text(sheet.Name ?? "N/A");
                                table.Cell().Element(CellStyleBarCode).AlignCenter().Height(40)
                                    .BarcodeCode128(sheet.SKU ?? "N/A", true, false, new QuestPDF.Drawing.BarcodeRenderOptions { CustomMargin = 1, IncludeContentAsText = false, StrokeWidthCorrection1D = 0 });
                                table.Cell().Element(CellStyleNoLeft).Text(qty.ToString());
                                table.Cell().Element(CellStyle).Text($"{sheet.Price:F2}");
                                table.Cell().Element(CellStyle).Text($"{sum:F2}");
                            }
                        });
                    }
                    else
                    {
                        // Проверка на "Свой материал"
                        bool hasOwn = plan.CuttingPlan.Any(cp => cp.SheetLayouts.Any(sl => sl.Sheet?.BarCode == "OWN"));
                        if (hasOwn)
                        {
                            col.Item().PaddingTop(15).Text($"Товар оплачен (Свой материал)").FontSize(14).Bold();
                        }
                    }

                    // ==========================================================
                    // 2. ТАБЛИЦА УСЛУГ (СГРУППИРОВАННАЯ + ОБЩИЕ ТАРОМЕСТЫ)
                    // ==========================================================
                    col.Item().PaddingTop(10).PaddingBottom(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f); // Код услуги
                            columns.RelativeColumn(3);    // Наименование
                            columns.RelativeColumn(1);    // Цена ед.
                            columns.RelativeColumn(1);    // Кол-во
                            columns.RelativeColumn(1);    // Сумма (руб)
                            columns.RelativeColumn(1);    // Таромест
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Код услуги");
                            header.Cell().Element(HeaderStyle).Text("Наименование");
                            header.Cell().Element(HeaderStyle).Text("Цена ед.");
                            header.Cell().Element(HeaderStyle).Text("Кол-во");
                            header.Cell().Element(HeaderStyle).Text("Сумма (руб)");
                            header.Cell().Element(HeaderStyle).Text("Таромест");
                        });

                        // --- АГРЕГАЦИЯ ДАННЫХ ---
                        var groupedServices = new Dictionary<int, SheetServiceItem>();
                        int totalTaromest = 0;

                        foreach (var cp in plan.CuttingPlan)
                        {
                            foreach (var sl in cp.SheetLayouts)
                            {
                                // 1. Считаем общие тароместы (детали) по всем листам
                                totalTaromest += sl.DetailsCount > 0 ? sl.DetailsCount : sl.Details.Count;

                                // 2. Группируем услуги по ServiceId
                                if (sl.Services != null)
                                {
                                    foreach (var svc in sl.Services)
                                    {
                                        if (!groupedServices.ContainsKey(svc.ServiceId))
                                        {
                                            groupedServices[svc.ServiceId] = new SheetServiceItem
                                            {
                                                ServiceId = svc.ServiceId,
                                                ServiceName = svc.ServiceName,
                                                ServiceCode = svc.ServiceCode,
                                                Price = svc.Price,
                                                Quantity = 0, // Будем суммировать
                                                IsDefaultCuttingService = svc.IsDefaultCuttingService
                                            };
                                        }
                                        // Складываем количество услуг
                                        groupedServices[svc.ServiceId].Quantity += svc.Quantity;
                                    }
                                }
                            }
                        }

                        // --- ОТРИСОВКА СГРУППИРОВАННЫХ УСЛУГ ---
                        if (groupedServices.Any())
                        {
                            bool firstRow = true;
                            foreach (var svc in groupedServices.Values)
                            {
                                var svcTotal = svc.Price * svc.Quantity;
                                TotalCost += (float)svcTotal;

                                table.Cell().Element(CellStyle).Text(!string.IsNullOrEmpty(svc.ServiceCode) ? svc.ServiceCode : "-");
                                table.Cell().Element(CellStyle).Text(svc.ServiceName ?? "Услуга");
                                table.Cell().Element(CellStyle).Text($"{svc.Price:F2}");
                                table.Cell().Element(CellStyle).Text(svc.Quantity.ToString()); // Здесь теперь общее кол-во (например, 2)
                                table.Cell().Element(CellStyle).Text($"{svcTotal:F2}");

                                // Таромест показываем ТОЛЬКО в первой строке, чтобы не дублировать
                                if (firstRow)
                                {
                                    table.Cell().Element(CellStyle).Text(totalTaromest.ToString()); // Здесь будет 4
                                    firstRow = false;
                                }
                                else
                                {
                                    table.Cell().Element(CellStyle).Text("");
                                }
                            }
                        }
                        else
                        {
                            // Fallback на случай очень старых данных без массива Services
                            int totalCuts = plan.CuttingPlan.Sum(cp => cp.SheetLayouts.Sum(sl => sl.CutCount));
                            var firstSheet = plan.CuttingPlan.FirstOrDefault()?.SheetLayouts?.FirstOrDefault()?.Sheet;
                            var pricePerUnit = firstSheet?.CutPrice ?? 0;
                            var total = pricePerUnit * totalCuts;
                            TotalCost += (float)total;

                            table.Cell().Element(CellStyle).Text("-");
                            table.Cell().Element(CellStyle).Text("Резка листового материала, 1 рез");
                            table.Cell().Element(CellStyle).Text($"{pricePerUnit:F2}");
                            table.Cell().Element(CellStyle).Text(totalCuts.ToString());
                            table.Cell().Element(CellStyle).Text($"{total:F2}");
                            table.Cell().Element(CellStyle).Text(totalTaromest.ToString());
                        }
                    });

                    // ==========================================================
                    // 3. ИТОГОВАЯ ТАБЛИЦА
                    // ==========================================================
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn((float)1.5);
                            columns.RelativeColumn(1);
                        });

                        table.Cell().Element(CellStyle).Text("Итого за все услуги");
                        table.Cell().Element(CellStyleNoRight).Text("");
                        table.Cell().Element(CellStyleBarCode).AlignCenter().Height(40)
                            .BarcodeCode128(plan.CutBarCode ?? "N/A", true, false, new QuestPDF.Drawing.BarcodeRenderOptions { CustomMargin = 1, IncludeContentAsText = false, StrokeWidthCorrection1D = 0 });
                        table.Cell().Element(CellStyleNoLeft).Text($"{TotalCost:F2} руб.");

                        table.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("ИТОГО");
                        table.Cell().Element(CellStyle).Text($"{TotalCost:F2} руб.");
                    });

                    // ==========================================================
                    // 4. ПОДПИСИ
                    // ==========================================================
                    col.Item().PaddingTop(30).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Cell().Element(CellStyleSign).Text("Услуга оказана в полном объеме, претензий не имею:").Bold().Italic();
                        table.Cell().AlignBottom().Element(CellStyleSign).Column(c => { c.Item().LineHorizontal(1); c.Item().Text("подпись").FontSize(8).AlignCenter(); });
                        table.Cell().AlignBottom().Element(CellStyleSign).Column(c => { c.Item().LineHorizontal(1); c.Item().Text("ФИО клиента").FontSize(8).AlignCenter(); });

                        table.Cell().Element(CellStyleSign).Text("Подпись сотрудника распила:").Bold().Italic();
                        table.Cell().AlignBottom().Element(CellStyleSign).Column(c => { c.Item().PaddingTop(10).LineHorizontal(1); c.Item().Text("подпись").FontSize(8).AlignCenter(); });
                        table.Cell().Element(CellStyleSign).Column(c => { c.Item().PaddingTop(10).LineHorizontal(1); c.Item().Text("ФИО сотрудника").FontSize(8).AlignCenter(); });
                    });
                });
            }

            void ComposeFooter(IContainer container)
            {
                container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Бланк № {plan.NumCut}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            row.RelativeItem().Text(x =>
                            {
                                x.Span("Страница ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2); // QuestPDF сам посчитает сквозную нумерацию
                            });//.FontSize(8).Color(Colors.Grey.Darken2);

                            /*row.RelativeItem()
                               .PaddingLeft(120)
                               .Text("БЛАНК РЕЗКА").FontSize(24).Bold().AlignLeft();*/
                        });
                    });
                });
            }

            static IContainer HeaderStyle(IContainer container) => container
                .Border(1)
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .DefaultTextStyle(x => x.Bold());

            static IContainer CellStyle(IContainer container) => container
                .Border(1)
                .BorderColor(Colors.Black)
                .AlignMiddle()
                .Padding(5);

            static IContainer CellStyleSign(IContainer container) => container
                .Border(0)
                .BorderColor(Colors.Black)
                .Padding(5);

            static IContainer CellStyleBarCode(IContainer container) => container
                .Border(0)
                .BorderHorizontal(1)
                .BorderColor(Colors.Black)
                .Padding(5);

            static IContainer CellStyleNoRight(IContainer container) => container
                .Border(1)
                .BorderRight(0)
                .AlignMiddle()
                .BorderColor(Colors.Black)
                .Padding(5);

            static IContainer CellStyleNoLeft(IContainer container) => container
                .Border(1)
                .BorderLeft(0)
                .AlignMiddle()
                .BorderColor(Colors.Black)
                .Padding(5);
        }

    }
}
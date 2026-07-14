using Cutter.Components.Account.Pages.Manage;
using Cutter.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Cutter.Services
{
    public class PdfCuttingService
    {
        public byte[] GeneratePdf(UserCuttingPlan plan)
        {
            if (plan == null)
                throw new ArgumentException("Нет планов раскроя для генерации PDF");

            // Собираем все НЕПУСТЫЕ листы из всех планов
            var allLayouts = plan.CuttingPlan
                .SelectMany(plan => plan.SheetLayouts)
                .Where(layout => !layout.IsEmpty)
                .ToList();


            if (allLayouts.Count == 0)
                throw new ArgumentException("Нет непустых листов для генерации PDF");

            // Берём дату первого плана или текущую
            var createdAt = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().ShowOnce().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    //page.Footer().Element(ComposeFooter);
                });

            }).GeneratePdf();

            void ComposeHeader(IContainer container)
            {
                //var pageNumber = container.PageNumber(); // ← номер текущей страницы (начинается с 1)

                var svgContent = File.ReadAllText("./wwwroot/imgs/lp.svg");

                container.Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {

                        col.Item().Row(row =>
                        {

                            row.ConstantItem(40).Svg(svgContent);
                                

                            row.RelativeItem()
                               //.Background(Colors.Grey.Lighten1)
                               .PaddingLeft(120)
                               .Text("БЛАНК РЕЗКА").FontSize(24).Bold().AlignLeft();
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {

                            row.ConstantItem(200)
                                //.Background(Colors.Grey.Medium)
                                //.Padding(10)
                                .Text($"Бланк №{plan.NumCut}").Bold().AlignLeft();

                            row.RelativeItem()
                               //.Background(Colors.Grey.Lighten1)
                               //.Padding(10)
                               .Text($"Дата создания бланка: {createdAt:dd.MM.yyyy HH:mm}").AlignRight();
                        });
                        if(!string.IsNullOrEmpty(plan.Invoice))
                        col.Item().PaddingTop(15).Text($"Договор №{plan.Invoice}").Bold();

                        col.Item().PaddingTop(15).Text(plan.StoreName).FontSize(12).Bold();
                        col.Item().PaddingTop(10).Text(plan.StoreAddress).FontSize(10);
                        col.Item().PaddingTop(10).Text($"Телефон: {plan.Phone}").FontSize(10);
                        col.Item().PaddingTop(5).Text("Для оплаты в течение дня").Bold().AlignCenter();
                        // Можно добавить общий номер заказа, если он есть
                        //col.Item().Text($"Дата: {createdAt:dd.MM.yyyy HH:mm}");
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                //bool isFirst = true;
                double TotalCost = 0;



                container.Column(col =>
                    {
                        foreach (var layout in plan.CuttingPlan)
                        {
                            /*if (!isFirst)
                            {
                                container.PageBreak(); // Новая страница для каждого листа, кроме первого
                            }
                            isFirst = false;*/
                            col.Item().PaddingTop(15).Text($"Товар №{layout.SheetLayouts.FirstOrDefault().Sheet.BarCode}").FontSize(14).Bold();

                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Код товара
                                    columns.RelativeColumn(2); // Артикул
                                    columns.RelativeColumn(2); // Наименование
                                    columns.RelativeColumn(2); // Штрихкод
                                    columns.RelativeColumn(1); // Кол-во
                                    columns.RelativeColumn(1); // Кол-во
                                    columns.RelativeColumn(1); // Кол-во
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

                                table.Cell().Element(CellStyle).Text(layout.SheetLayouts?.FirstOrDefault()?.Sheet.BarCode ?? "Без названия");
                                table.Cell().Element(CellStyle).Text(layout.SheetLayouts?.FirstOrDefault()?.Sheet?.SKU ?? "N/A");
                                table.Cell().Element(CellStyle).Text($"{layout.SheetLayouts?.FirstOrDefault()?.Sheet?.Name ?? "N/A"}");
                                table.Cell().Element(CellStyle).AlignCenter()
                                    .Height(40)
                                    .BarcodeCode128(layout.SheetLayouts?.FirstOrDefault()?.Sheet?.SKU ?? "N/A", true, false, new QuestPDF.Drawing.BarcodeRenderOptions { CustomMargin = 1, IncludeContentAsText = false, StrokeWidthCorrection1D = 0 });
                                table.Cell().Element(CellStyle).Text(layout.SheetLayouts?.Sum(x => x.Sheet.Quantity).ToString() ?? "N/A"); // или detail.Quantity, если есть
                                table.Cell().Element(CellStyle).Text($"{layout.SheetLayouts?.FirstOrDefault()?.Sheet?.Price:F2}");
                                table.Cell().Element(CellStyle).Text($"{(layout.SheetLayouts?.FirstOrDefault()?.Sheet?.Price * layout.SheetLayouts?.Sum(x => x.Sheet.Quantity)):F2}");


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
                            });

                            // Услуга резки для листа
                            col.Item().PaddingTop(10).PaddingBottom(40).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Код услуги");
                                    header.Cell().Element(HeaderStyle).Text("Наименование");
                                    header.Cell().Element(HeaderStyle).Text("Цена ед.");
                                    header.Cell().Element(HeaderStyle).Text("Кол-во резов");
                                    header.Cell().Element(HeaderStyle).Text("Сумма (руб)");
                                    header.Cell().Element(HeaderStyle).Text("Таромест");
                                });

                                //var cutsCount = layout.CutLines?.Count(c => c.IsVisible) ?? 0;
                                var cutsCount = layout.SheetLayouts.Sum(s => s.CutCount);
                                var pricePerUnit = layout.SheetLayouts?.FirstOrDefault()?.Sheet?.CutPrice;
                                var total = pricePerUnit * cutsCount;
                                TotalCost += total.Value;
                                //var taromest = layout.Details?.Count ?? 0;
                                var taromest = layout.SheetLayouts.Sum(c => c.Details.Count());

                                table.Cell().Element(CellStyle).Text(plan.CutBarCode);
                                table.Cell().Element(CellStyle).Text("Резка листового материала, 1 рез");
                                table.Cell().Element(CellStyle).Text($"{pricePerUnit:F2}");
                                table.Cell().Element(CellStyle).Text(cutsCount.ToString());
                                table.Cell().Element(CellStyle).Text($"{total:F2}");
                                table.Cell().Element(CellStyle).Text(taromest.ToString());

                                static IContainer HeaderStyle(IContainer container) => container
                                    .Background(Colors.Grey.Lighten3)
                                    .Border(1)
                                    .Padding(5)
                                    .DefaultTextStyle(x => x.Bold());

                                static IContainer CellStyle(IContainer container) => container
                                    .Border(1)
                                    .BorderColor(Colors.Black)
                                    .Padding(5)
                                    .AlignMiddle();
                            });

                        }

                        // Итого резка
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });


                            table.Cell().Element(CellStyle).Text("Итого за услугу резка по коду");
                            table.Cell().Element(CellStyle).Text(plan.CutBarCode);
                            table.Cell().Element(CellStyle).AlignCenter()
                                                            .Height(40)
                                                            .BarcodeCode128(plan.CutBarCode, true, false, new QuestPDF.Drawing.BarcodeRenderOptions { CustomMargin = 1, IncludeContentAsText = false, StrokeWidthCorrection1D = 0 });
                            table.Cell().Element(CellStyle).Text($"{TotalCost:F2} руб.");


                            table.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("ИТОГО");
                            table.Cell().Element(CellStyle).Text($"{TotalCost:F2} руб.");


                            static IContainer CellStyle(IContainer container) => container
                                .Border(1)
                                .BorderColor(Colors.Black)
                                .Padding(5)
                                .AlignMiddle();
                        });

                        // Подписи
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });


                            table.Cell().Element(CellStyle).Text("Услуга оказана в полном объеме, претензий не имею:").Bold().Italic();
                            table.Cell().AlignBottom().Element(CellStyle).Column(c =>
                            {
                                c.Item().LineHorizontal(1);
                                c.Item().Text("подпись").FontSize(8).AlignCenter();
                            });
                            table.Cell().AlignBottom().Element(CellStyle).Column(c =>
                            {
                                c.Item().LineHorizontal(1);
                                c.Item().Text("ФИО клиента").FontSize(8).AlignCenter();
                            });

                            table.Cell().Element(CellStyle).Text("Подпись сотрудника распила:").Bold().Italic();
                            table.Cell().AlignBottom().Element(CellStyle).Column(c =>
                            {
                                c.Item().PaddingTop(10).LineHorizontal(1);
                                c.Item().Text("подпись").FontSize(8).AlignCenter();
                            });
                            table.Cell().Element(CellStyle).Column(c =>
                            {
                                c.Item().PaddingTop(10).LineHorizontal(1);
                                c.Item().Text("ФИО сотрудника").FontSize(8).AlignCenter();
                            });


                            static IContainer CellStyle(IContainer container) => container
                                .Border(0)
                                .BorderColor(Colors.Black)
                                .Padding(5);
                        });

                    });
                

                // Итого
            }

            void ComposeFooter(IContainer container)
            {
                container
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Сформировано автоматически. ");
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(8);
                    }); //.FontSize(8);
            }
        }
    }
}
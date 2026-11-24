using ClosedXML.Excel;
using Cutter.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Cutter.Data.DBModels;

namespace Cutter.Services
{
    // DTO для настроек (галочки в UI)
    public class ExcelImportSettings
    {
        // === ЧТО ИМПОРТИРОВАТЬ (поля) ===
        public bool ImportName { get; set; } = true;
        public bool ImportSizes { get; set; } = true;
        public bool ImportPrice { get; set; } = true;

        // === СПРАВОЧНИКИ ===
        public bool CreateNewDepartments { get; set; } = true;
        public bool CreateNewSubdepartments { get; set; } = true;

        // === НОВЫЕ ТОВАРЫ (которых нет нигде) ===
        public bool CreateNewItems { get; set; } = true; // Создать и в справочнике, и в магазине

        // === ТОВАР ЕСТЬ В СПРАВОЧНИКЕ, НО НЕТ В МАГАЗИНЕ ===
        public bool AddToStore { get; set; } = true; // Добавить привязку к магазину

        // === ОБНОВЛЕНИЕ СУЩЕСТВУЮЩИХ ===
        public bool UpdateCatalog { get; set; } = false; // Обновлять справочник Items
        public bool UpdateStore { get; set; } = true;    // Обновлять StoreItems (цену, подотдел)

        // === НУЛЕВАЯ ЦЕНА ===
        public ZeroPriceAction ZeroPriceAction { get; set; } = ZeroPriceAction.Skip;
    }

    public enum ExistingItemAction
    {
        Skip,           // Не трогать
        Overwrite,      // Перезаписать полностью
        UpdateFields    // Обновить только выбранные поля (Name, Sizes, Price)
    }

    public enum ZeroPriceAction
    {
        Skip,           // Не трогать (оставить старую цену)
        Overwrite       // Перезаписать (обнулить)
    }

    // DTO для результата
    public class ExcelImportResult
    {
        public int TotalRows { get; set; }
        public int CreatedItems { get; set; }
        public int UpdatedItems { get; set; }
        public int CreatedDepartments { get; set; }
        public int CreatedSubdepartments { get; set; }
        public int SkippedRows { get; set; }
        public int ErrorsCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public interface IExcelImportService
    {
        Task<ExcelImportResult> ImportFromFileAsync(Stream fileStream, string fileName, Guid storeId, string userId, ExcelImportSettings settings);
    }

    public class ExcelImportService : IExcelImportService
    {
        private readonly ApplicationDbContext _db;

        public ExcelImportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ExcelImportResult> ImportFromFileAsync(Stream fileStream, string fileName, Guid storeId, string userId, ExcelImportSettings settings)
        {
            var result = new ExcelImportResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Копируем поток в MemoryStream (Blazor InputFile не поддерживает синхронное чтение)
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var workbook = new XLWorkbook(memoryStream);
                var worksheet = workbook.Worksheets.Worksheet(1);

                var rows = worksheet.RowsUsed().Skip(1);
                result.TotalRows = worksheet.RowsUsed().Count() - 1;

                // Кэши для быстрого поиска
                var deptsCache = await _db.StoreDepartments
                    .Where(d => d.StoreId == storeId)
                    .ToDictionaryAsync(d => d.Code);

                var subdeptsCache = await _db.StoreSubdepartments
                    .Where(s => deptsCache.Values.Select(d => d.Id).Contains(s.StoreDepartmentId))
                    .ToDictionaryAsync(s => $"{s.StoreDepartmentId}_{s.Code}");

                var itemsCache = await _db.Set<Items>().ToDictionaryAsync(i => i.BarCode);
                var storeItemsCache = await _db.StoreItems
                    .Where(si => si.StoreId == storeId)
                    .ToDictionaryAsync(si => si.ItemId);

                var newStoreItems = new List<StoreItem>();
                var updatedStoreItems = new List<StoreItem>();
                var updatedItems = new List<Items>();

                foreach (var row in rows)
                {
                    try
                    {
                        if (row.IsHidden) continue;

                        var article = row.Cell(1).GetString()?.Trim();
                        var name = row.Cell(2).GetString()?.Trim();
                        var deptCode = NormalizeDepartmentCode(row.Cell(3).GetString()?.Trim() ?? "");
                        var subdeptRaw = row.Cell(4).GetString()?.Trim() ?? "";
                        var barCode = row.Cell(5).GetString()?.Trim();

                        row.Cell(7).TryGetValue<float>(out float price);

                        // Валидация обязательных полей
                        if (string.IsNullOrEmpty(barCode) || string.IsNullOrEmpty(deptCode) || string.IsNullOrEmpty(subdeptRaw))
                        {
                            result.SkippedRows++;
                            result.Errors.Add($"Строка {row.RowNumber()}: Пропущено (нет штрихкода, отдела или подотдела)");
                            continue;
                        }

                        // Парсинг подотдела
                        var subdeptParts = subdeptRaw.Split(new[] { ' ' }, 2);
                        var subdeptCode = NormalizeDepartmentCode(subdeptParts[0]);
                        var subdeptName = subdeptParts.Length > 1 ? subdeptParts[1] : subdeptCode;

                        // === ОТДЕЛ ===
                        if (!deptsCache.TryGetValue(deptCode, out var department))
                        {
                            if (!settings.CreateNewDepartments)
                            {
                                result.SkippedRows++;
                                result.Errors.Add($"Строка {row.RowNumber()}: Отдел '{deptCode}' не найден и создание запрещено");
                                continue;
                            }

                            department = new StoreDepartment
                            {
                                StoreId = storeId,
                                Code = deptCode,
                                Name = deptCode,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.StoreDepartments.Add(department);
                            await _db.SaveChangesAsync();
                            deptsCache[deptCode] = department;
                            result.CreatedDepartments++;
                        }

                        // === ПОДОТДЕЛ ===
                        var subdeptKey = $"{department.Id}_{subdeptCode}";
                        if (!subdeptsCache.TryGetValue(subdeptKey, out var subdepartment))
                        {
                            if (!settings.CreateNewSubdepartments)
                            {
                                result.SkippedRows++;
                                result.Errors.Add($"Строка {row.RowNumber()}: Подотдел '{subdeptCode}' не найден и создание запрещено");
                                continue;
                            }

                            subdepartment = new StoreSubdepartment
                            {
                                StoreDepartmentId = department.Id,
                                Code = subdeptCode,
                                Name = subdeptName,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.StoreSubdepartments.Add(subdepartment);
                            await _db.SaveChangesAsync();
                            subdeptsCache[subdeptKey] = subdepartment;
                            result.CreatedSubdepartments++;
                        }

                        // === ТОВАР ===
                        Items item = null;
                        StoreItem storeItem = null;

                        var itemExists = itemsCache.TryGetValue(barCode, out item);
                        var storeItemExists = itemExists && storeItemsCache.TryGetValue(item.Id, out storeItem);

                        if (!itemExists)
                        {
                            // СЦЕНАРИЙ А: Товара нет нигде
                            if (!settings.CreateNewItems)
                            {
                                result.SkippedRows++;
                                continue;
                            }

                            var sizeResult = SizeParser.Parse(name ?? "");

                            if (!sizeResult.HasSize && settings.ImportSizes)
                            {
                                result.Warnings.Add($"Строка {row.RowNumber()}: Не удалось распарсить размеры из '{name}'");
                            }

                            item = new Items
                            {
                                BarCode = barCode,
                                SKU = article ?? barCode,
                                Name = name ?? "Без названия",
                                Price = price,
                                IsActive = true,
                                Length = sizeResult.Length,
                                Width = sizeResult.Width,
                                Depth = sizeResult.Depth,
                                MaterialTypeId = 1
                            };
                            _db.Set<Items>().Add(item);
                            await _db.SaveChangesAsync();
                            itemsCache[barCode] = item;

                            // Сразу создаем привязку к магазину
                            newStoreItems.Add(new StoreItem
                            {
                                StoreId = storeId,
                                ItemId = item.Id,
                                Price = price,
                                StoreSubdepartmentId = subdepartment.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            result.CreatedItems++;
                        }
                        else if (!storeItemExists)
                        {
                            // СЦЕНАРИЙ Б: Товар есть в справочнике, но нет в магазине
                            if (!settings.AddToStore)
                            {
                                result.SkippedRows++;
                                continue;
                            }

                            // Создаем привязку к магазину
                            newStoreItems.Add(new StoreItem
                            {
                                StoreId = storeId,
                                ItemId = item.Id,
                                Price = price,
                                StoreSubdepartmentId = subdepartment.Id,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            result.CreatedItems++;

                            // Опционально обновляем справочник
                            if (settings.UpdateCatalog)
                            {
                                bool updated = false;

                                if (settings.ImportName && name != item.Name)
                                {
                                    item.Name = name;
                                    updated = true;
                                }

                                if (settings.ImportSizes)
                                {
                                    var sizeResult = SizeParser.Parse(name ?? "");
                                    if (sizeResult.Length != item.Length ||
                                        sizeResult.Width != item.Width ||
                                        sizeResult.Depth != item.Depth)
                                    {
                                        item.Length = sizeResult.Length;
                                        item.Width = sizeResult.Width;
                                        item.Depth = sizeResult.Depth;
                                        updated = true;
                                    }
                                }

                                if (updated)
                                {
                                    updatedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            // СЦЕНАРИЙ В: Товар есть и там, и там
                            bool catalogUpdated = false;
                            bool storeUpdated = false;

                            // Обновляем справочник
                            if (settings.UpdateCatalog)
                            {
                                if (settings.ImportName && name != item.Name)
                                {
                                    item.Name = name;
                                    catalogUpdated = true;
                                }

                                if (settings.ImportSizes)
                                {
                                    var sizeResult = SizeParser.Parse(name ?? "");
                                    if (sizeResult.Length != item.Length ||
                                        sizeResult.Width != item.Width ||
                                        sizeResult.Depth != item.Depth)
                                    {
                                        item.Length = sizeResult.Length;
                                        item.Width = sizeResult.Width;
                                        item.Depth = sizeResult.Depth;
                                        catalogUpdated = true;
                                    }
                                }
                            }

                            // Обновляем магазин
                            if (settings.UpdateStore)
                            {
                                if (settings.ImportPrice)
                                {
                                    if (price > 0 || settings.ZeroPriceAction == ZeroPriceAction.Overwrite)
                                    {
                                        if (price != storeItem.Price)
                                        {
                                            storeItem.Price = price;
                                            item.Price = price;
                                            storeUpdated = true;
                                        }
                                    }
                                }

                                if (storeItem.StoreSubdepartmentId != subdepartment.Id)
                                {
                                    storeItem.StoreSubdepartmentId = subdepartment.Id;
                                    storeUpdated = true;
                                }
                            }

                            if (catalogUpdated)
                            {
                                updatedItems.Add(item);
                            }

                            if (storeUpdated)
                            {
                                storeItem.UpdatedAt = DateTime.UtcNow;
                                updatedStoreItems.Add(storeItem);
                            }

                            if (catalogUpdated || storeUpdated)
                            {
                                result.UpdatedItems++;
                            }
                            else
                            {
                                result.SkippedRows++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorsCount++;
                        result.Errors.Add($"Строка {row.RowNumber()}: {ex.Message}");
                    }
                }

                // Сохраняем все изменения в конце
                if (newStoreItems.Any())
                {
                    _db.StoreItems.AddRange(newStoreItems);
                }

                if (updatedItems.Any())
                {
                    _db.Set<Items>().UpdateRange(updatedItems);
                }

                if (updatedStoreItems.Any())
                {
                    _db.StoreItems.UpdateRange(updatedStoreItems);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Критическая ошибка: {ex.Message}";
            }

            await LogImportAsync(fileName, storeId, userId, settings, result, startTime);

            return result;
        }

        private async Task LogImportAsync(string fileName, Guid storeId, string userId, ExcelImportSettings settings, ExcelImportResult result, DateTime startTime)
        {
            var log = new ImportLog
            {
                StoreId = storeId,
                UserId = userId,
                Source = "Manual",
                FileName = fileName,
                ImportDate = DateTime.UtcNow,
                ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                SettingsJson = JsonSerializer.Serialize(settings),
                TotalRows = result.TotalRows,
                CreatedItems = result.CreatedItems,
                UpdatedItems = result.UpdatedItems,
                CreatedDepartments = result.CreatedDepartments,
                CreatedSubdepartments = result.CreatedSubdepartments,
                SkippedRows = result.SkippedRows,
                ErrorsCount = result.ErrorsCount,
                ErrorMessage = result.ErrorMessage ?? (result.Errors.Any() ? string.Join("\n", result.Errors.Take(10)) : null),
                Status = result.ErrorMessage != null ? "Failed" : (result.ErrorsCount > 0 ? "Partial" : "Completed")
            };

            _db.ImportLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        private string NormalizeDepartmentCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;

            // Если код короче 2 символов, добавляем ведущий ноль
            if (code.Length < 2) code = "0" + code;

            return code;
        }
    }
}
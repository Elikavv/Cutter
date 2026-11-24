using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cutter.Services
{
    public class SizeParserResult
    {
        public int Length { get; set; }
        public int Width { get; set; }
        public int Depth { get; set; }
        public bool HasSize { get; set; }
    }

    public static class SizeParser
    {
        public static SizeParserResult Parse(string name)
        {
            var result = new SizeParserResult();
            if (string.IsNullOrWhiteSpace(name)) return result;

            var normalized = name.Trim();

            // Убираем количество штук в конце (10ШТ, 5шт)
            normalized = Regex.Replace(normalized, @"\s*\d+\s*(шт|штук|ШТ|ШТУК)\.?$", "", RegexOptions.IgnoreCase);

            // Убираем площадь и объем в конце (2,89М2, 1.5м3)
            normalized = Regex.Replace(normalized, @"\s*\d+[.,]?\d*\s*(м2|м3|м²|м³|m2|m3|m²|m³)\.?$", "", RegexOptions.IgnoreCase);

            // Ищем размеры с префиксами (L=, W=, H=, Д=, Ш=, В=)
            var prefixPattern = @"(?:L|W|H|Д|Ш|В)\s*=\s*(\d+[.,]?\d*)\s*(мм|см|м|mm|cm|m)?";
            var prefixMatches = Regex.Matches(normalized, prefixPattern, RegexOptions.IgnoreCase);

            if (prefixMatches.Count > 0)
            {
                var prefixDimensions = new List<double>();
                string? prefixUnit = null;

                foreach (Match prefixMatch in prefixMatches)
                {
                    if (double.TryParse(prefixMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        prefixDimensions.Add(val);
                        if (!string.IsNullOrEmpty(prefixMatch.Groups[2].Value))
                        {
                            prefixUnit = prefixMatch.Groups[2].Value.ToLower();
                        }
                    }
                }

                if (prefixDimensions.Count > 0)
                {
                    ApplyDimensions(result, prefixDimensions.ToArray(), prefixUnit);
                    return result;
                }
            }

            // Основной паттерн: числа, разделенные x/х/*
            //var pattern = @"(?<nums>\d+[.,]?\d*(?:\s*[xх*]\s*\d+[.,]?\d*){0,2})\s*(?<unit>мм|см|м|mm|cm|m)?";
            // Захватываем любое количество чисел, разделенных x/х/*
            var pattern = @"(?<nums>(?:\d+[.,]?\d*\s*[xх*]\s*)*\d+[.,]?\d*)\s*(?<unit>мм|см|м|mm|cm|m)?";
            var matches = Regex.Matches(normalized, pattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
            {
                result.HasSize = false;
                return result;
            }

            // Берём последнее совпадение
            var match = matches[matches.Count - 1];
            var numsString = match.Groups["nums"].Value;
            var unit = match.Groups["unit"].Value.ToLower();

            // Разделяем числа по x/х/*
            var parts = Regex.Split(numsString, @"\s*[xх*]\s*");
            var dimensions = new double[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (double.TryParse(parts[i].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    dimensions[i] = val;
                }
            }

            // Если единицы нет - считаем мм
            if (string.IsNullOrEmpty(unit))
            {
                unit = "мм";
            }

            ApplyDimensions(result, dimensions, unit);
            return result;
        }

        private static void ApplyDimensions(SizeParserResult result, double[] dimensions, string? unit)
        {
            Array.Sort(dimensions);

            double multiplier = unit?.ToLower() switch
            {
                "м" or "m" => 1000,
                "см" or "cm" or "с" => 10,
                "мм" or "mm" or "" => 1,
                _ => 1
            };

            if (dimensions.Length == 1)
            {
                result.Length = (int)(dimensions[0] * multiplier);
            }
            else if (dimensions.Length == 2)
            {
                result.Length = (int)(dimensions[1] * multiplier);
                result.Width = (int)(dimensions[0] * multiplier);
            }
            else if (dimensions.Length >= 3)
            {
                result.Length = (int)(dimensions[2] * multiplier);
                result.Width = (int)(dimensions[1] * multiplier);
                result.Depth = (int)(dimensions[0] * multiplier);
            }

            result.HasSize = true;
        }
    }
}
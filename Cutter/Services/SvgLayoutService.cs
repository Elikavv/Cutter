using Cutter.Data;
using System.Text;

namespace Cutter.Services
{
    public class SvgLayoutService
    {

        // Константы для отображения
        private const float MAX_DISPLAY_WIDTH = 750f;   // Максимальная ширина для отображения
        private const float MAX_DISPLAY_HEIGHT = 350f;  // Максимальная высота для отображения
        private const float MIN_FONT_SIZE = 6f;
        private const float MAX_FONT_SIZE = 14f;

        // ВСТАВЬ СЮДА СВОЮ РЕАЛЬНУЮ ЛОГИКУ ГЕНЕРАЦИИ SVG ИЗ Cut.razor
        public string GenerateSheetSvg(SheetLayout sheetLayout)
        {
            var sb = new StringBuilder();

            var maxDim = Math.Max(sheetLayout.Sheet.Width, sheetLayout.Sheet.Length);
            var scale = 400.0 / maxDim;

            var normWidth = sheetLayout.Sheet.Width * scale;
            var normLength = sheetLayout.Sheet.Length * scale;

            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' height='100%' width='100%' viewBox='0 0 {normWidth:F0} {normLength:F0}' preserveAspectRatio='xMidYMid meet' style='display: block; max-width: 100%;'>");

            // Фон
            sb.Append($"<rect x='0' y='0' width='{normWidth:F0}' height='{normLength:F0}' fill='white' stroke='#333' stroke-width='2' />");

            // Штриховка
            // Штриховка ТОЛЬКО в пределах листа
            var hatchSpacing = 10;
            for (double y = 0; y < normLength; y += hatchSpacing)
            {
                for (double x = 0; x < normWidth; x += hatchSpacing)
                {
                    var x1 = x.ToString("F1").Replace(",", ".");
                    var y1 = y.ToString("F1").Replace(",", ".");
                    var x2 = (x + hatchSpacing).ToString("F1").Replace(",", ".");
                    var y2 = (y + hatchSpacing).ToString("F1").Replace(",", ".");

                    sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#999' stroke-width='1' opacity='0.3' />");
                }
            }

            // Детали с УНИКАЛЬНЫМ индексом
            if (sheetLayout.Details != null)
            {
                for (int i = 0; i < sheetLayout.Details.Count; i++)
                {
                    var detail = sheetLayout.Details[i];
                    var x = detail.X * scale;
                    var y = (sheetLayout.Sheet.Length - detail.Y - detail.Length) * scale;
                    var w = detail.Width * scale;
                    var h = detail.Length * scale;
                    var centerX = x + w / 2;
                    var centerY = y + h / 2;

                    var xStr = x.ToString("F1").Replace(",", ".");
                    var yStr = y.ToString("F1").Replace(",", ".");
                    var wStr = w.ToString("F1").Replace(",", ".");
                    var hStr = h.ToString("F1").Replace(",", ".");
                    var centerXStr = centerX.ToString("F1").Replace(",", ".");
                    var centerYStr = centerY.ToString("F1").Replace(",", ".");

                    var fontSize = Math.Max(10, 18 * scale).ToString("F0");
                    var rectId = $"rect_{i}";

                    // Прямоугольник с УНИКАЛЬНЫМ data-idx
                    sb.Append($"<rect id='{rectId}' data-idx='{i}' x='{xStr}' y='{yStr}' width='{wStr}' height='{hStr}' ");
                    sb.Append($"fill='white' stroke='#2E7D32' stroke-width='2' style='cursor: pointer;' ");
                    sb.Append($"onmouseover=\"this.style.fill='#E8F5E9'\" ");
                    sb.Append($"onmouseout=\"if(!this.classList.contains('sel')){{this.style.fill='white'}}\" ");
                    sb.Append($"onclick=\"highlightByIndex({i})\" />");

                    // Название детали ПО ЦЕНТРУ
                    sb.Append($"<text x='{centerXStr}' y='{centerYStr}' text-anchor='middle' dominant-baseline='middle' font-size='{fontSize}' fill='#2E7D32' font-weight='bold' style='pointer-events: none;'>{detail.Name}</text>");

                    // Горизонтальный размер (ширина) - ВНУТРИ, над нижней линией, по центру
                    var widthText = detail.Width.ToString();
                    var widthY = (y + h - 2).ToString("F1").Replace(",", ".");
                    sb.Append($"<text x='{centerXStr}' y='{widthY}' text-anchor='middle' font-size='{Math.Max(8, 11 * scale):F0}' fill='#666' style='pointer-events: none;'>{widthText}</text>");

                    // Вертикальный размер (длина) - ВНУТРИ, справа от вертикальной линии, по центру
                    var lengthText = detail.Length.ToString();
                    var lengthX = (x + w - 5).ToString("F1").Replace(",", ".");
                    sb.Append($"<text x='{lengthX}' y='{centerYStr}' text-anchor='middle' dominant-baseline='middle' font-size='{Math.Max(8, 11 * scale):F0}' fill='#666' transform='rotate(-90, {lengthX}, {centerYStr})' style='pointer-events: none;'>{lengthText}</text>");
                }
            }

            // Линии реза
            if (sheetLayout.CutLines != null)
            {
                foreach (var cutLine in sheetLayout.CutLines.Where(cl => cl.IsVisible))
                {
                    var color = cutLine.Type == "vertical" ? "#d32f2f" : "#1976d2";
                    var dash = cutLine.Type == "vertical" ? "6,4" : "4,6";
                    var x1 = (cutLine.X1 * scale).ToString("F1").Replace(",", ".");
                    var y1 = ((sheetLayout.Sheet.Length - cutLine.Y1) * scale).ToString("F1").Replace(",", ".");
                    var x2 = (cutLine.X2 * scale).ToString("F1").Replace(",", ".");
                    var y2 = ((sheetLayout.Sheet.Length - cutLine.Y2) * scale).ToString("F1").Replace(",", ".");

                    sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='2' stroke-dasharray='{dash}' opacity='0.7' stroke-linecap='round' />");
                }
            }

            sb.Append("</svg>");

            string result = sb.ToString();

            return sb.ToString();
        }

        public string GenerateSheetSvgForPdf(SheetLayout sheetLayout)
        {
            var sb = new StringBuilder();

            // Реальные размеры листа
            var realWidth = (float)sheetLayout.Sheet.Width;
            var realLength = (float)sheetLayout.Sheet.Length;

            // Вычисляем оптимальный масштаб
            var scaleX = MAX_DISPLAY_WIDTH / realWidth;
            var scaleY = MAX_DISPLAY_HEIGHT / realLength;
            var scale = (float)(Math.Min(scaleX, scaleY) * 0.95); // 5% отступ

            // Итоговые размеры для отображения
            var displayWidth = realWidth * scale;
            var displayHeight = realLength * scale;

            // Формируем SVG
            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' ");
            sb.Append($"width='{displayWidth:F0}' height='{displayHeight:F0}' ");
            sb.Append($"viewBox='0 0 {displayWidth:F0} {displayHeight:F0}' ");
            sb.Append($"preserveAspectRatio='xMidYMid meet'>");

            // Фон листа
            sb.Append($"<rect x='0' y='0' width='{displayWidth:F0}' height='{displayHeight:F0}' ");
            sb.Append($"fill='white' stroke='#333' stroke-width='0' />");

            // Штриховка (адаптивная)
            AddHatchPattern(sb, displayWidth, displayHeight, scale);

            // Детали
            if (sheetLayout.Details != null)
            {
                //AddDetails(sb, sheetLayout.Details, sheetLayout.Sheet.Length, scale);
                for (int i = 0; i < sheetLayout.Details.Count; i++)
                {
                    var detail = sheetLayout.Details[i];
                    var x = detail.X * scale;
                    var y = (sheetLayout.Sheet.Length - detail.Y - detail.Length) * scale;
                    var w = detail.Width * scale;
                    var h = detail.Length * scale;
                    var centerX = x + w / 2;
                    var centerY = y + h / 2;

                    var xStr = x.ToString("F1").Replace(",", ".");
                    var yStr = y.ToString("F1").Replace(",", ".");
                    var wStr = w.ToString("F1").Replace(",", ".");
                    var hStr = h.ToString("F1").Replace(",", ".");
                    var centerXStr = centerX.ToString("F1").Replace(",", ".");
                    var centerYStr = centerY.ToString("F1").Replace(",", ".");

                    var fontSize = Math.Max(10, 18 * scale).ToString("F0");
                    var rectId = $"rect_{i}";

                    // Прямоугольник с УНИКАЛЬНЫМ data-idx
                    sb.Append($"<rect id='{rectId}' data-idx='{i}' x='{xStr}' y='{yStr}' width='{wStr}' height='{hStr}' ");
                    sb.Append($"fill='white' stroke='#2E7D32' stroke-width='2' style='cursor: pointer;' />");

                    // Название детали ПО ЦЕНТРУ
                    sb.Append($"<text x='{centerXStr}' y='{centerYStr}' text-anchor='middle' dominant-baseline='middle' font-size='{fontSize}' fill='#2E7D32' font-weight='bold' style='pointer-events: none;'>{detail.Name}</text>");

                    // Горизонтальный размер (ширина) - ВНУТРИ, над нижней линией, по центру
                    var widthText = detail.Width.ToString();
                    var widthY = (y + h - 2).ToString("F1").Replace(",", ".");
                    sb.Append($"<text x='{centerXStr}' y='{widthY}' text-anchor='middle' font-size='{Math.Max(8, 11 * scale):F0}' fill='#666' style='pointer-events: none;'>{widthText}</text>");

                    // Вертикальный размер (длина) - ВНУТРИ, справа от вертикальной линии, по центру
                    var lengthText = detail.Length.ToString();
                    var lengthX = (x + w - 5).ToString("F1").Replace(",", ".");
                    sb.Append($"<text x='{lengthX}' y='{centerYStr}' text-anchor='middle' dominant-baseline='middle' font-size='{Math.Max(8, 11 * scale):F0}' fill='#666' transform='rotate(-90, {lengthX}, {centerYStr})' style='pointer-events: none;'>{lengthText}</text>");
                }
            }

            // Линии реза
            if (sheetLayout.CutLines != null)
            {
                //AddCutLines(sb, sheetLayout.CutLines, sheetLayout.Sheet.Length, scale);
                foreach (var cutLine in sheetLayout.CutLines.Where(cl => cl.IsVisible))
                {
                    var color = cutLine.Type == "vertical" ? "#d32f2f" : "#1976d2";
                    var dash = cutLine.Type == "vertical" ? "6,4" : "4,6";
                    var x1 = (cutLine.X1 * scale).ToString("F1").Replace(",", ".");
                    var y1 = ((sheetLayout.Sheet.Length - cutLine.Y1) * scale).ToString("F1").Replace(",", ".");
                    var x2 = (cutLine.X2 * scale).ToString("F1").Replace(",", ".");
                    var y2 = ((sheetLayout.Sheet.Length - cutLine.Y2) * scale).ToString("F1").Replace(",", ".");

                    sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='2' stroke-dasharray='{dash}' opacity='0.7' stroke-linecap='round' />");
                }
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private void AddHatchPattern(StringBuilder sb, float width, float height, float scale)
        {
            var spacing = Math.Max(5f, Math.Min(15f, 10f * scale));

            for (float y = 0; y < height; y += spacing)
            {
                for (float x = 0; x < width; x += spacing)
                {
                    sb.Append($"<line x1='{x:F1}' y1='{y:F1}' ");
                    sb.Append($"x2='{x + spacing:F1}' y2='{y + spacing:F1}' ");
                    sb.Append($"stroke='#999' stroke-width='1' opacity='0.3' />");
                }
            }
        }

        private void AddDetails(StringBuilder sb, List<CutDetail> details, double sheetLength, float scale)
        {
            for (int i = 0; i < details.Count; i++)
            {
                var detail = details[i];
                var x = (float)detail.X * scale;
                var y = (float)(sheetLength - detail.Y - detail.Length) * scale;
                var w = (float)detail.Width * scale;
                var h = (float)detail.Length * scale;
                var centerX = x + w / 2;
                var centerY = y + h / 2;

                // Размер шрифта зависит от размера детали и масштаба
                var fontSize = Math.Clamp(18f * scale, MIN_FONT_SIZE, MAX_FONT_SIZE);
                var sizeFontSize = Math.Clamp(11f * scale, MIN_FONT_SIZE - 2, MAX_FONT_SIZE - 2);

                // Прямоугольник (БЕЗ JAVASCRIPT!)
                sb.Append($"<rect x='{x:F1}' y='{y:F1}' width='{w:F1}' height='{h:F1}' ");
                sb.Append($"fill='white' stroke='#2E7D32' stroke-width='2' />");

                // Название по центру
                sb.Append($"<text x='{centerX:F1}' y='{centerY:F1}' ");
                sb.Append($"text-anchor='middle' dominant-baseline='middle' ");
                sb.Append($"font-size='{fontSize:F0}' fill='#2E7D32' font-weight='bold' ");
                sb.Append($">{detail.Name}</text>");

                // Размеры (если достаточно места)
                if (w > 30 && h > 30)
                {
                    // Ширина - внизу
                    sb.Append($"<text x='{centerX:F1}' y='{y + h - 2:F1}' ");
                    sb.Append($"text-anchor='middle' font-size='{sizeFontSize:F0}' ");
                    sb.Append($"fill='#666'>{detail.Width:F0}</text>");

                    // Длина - справа, вертикально
                    sb.Append($"<text x='{x + w - 5:F1}' y='{centerY:F1}' ");
                    sb.Append($"text-anchor='middle' dominant-baseline='middle' ");
                    sb.Append($"font-size='{sizeFontSize:F0}' fill='#666' ");
                    sb.Append($"transform='rotate(-90, {x + w - 5:F1}, {centerY:F1})' ");
                    sb.Append($">{detail.Length:F0}</text>");
                }
            }
        }

        private void AddCutLines(StringBuilder sb, List<CutLine> cutLines, double sheetLength, float scale)
        {
            foreach (var cutLine in cutLines.Where(cl => cl.IsVisible))
            {
                var color = cutLine.Type == "vertical" ? "#d32f2f" : "#1976d2";
                var dash = cutLine.Type == "vertical" ? "6,4" : "4,6";

                var x1 = (float)cutLine.X1 * scale;
                var y1 = (float)(sheetLength - cutLine.Y1) * scale;
                var x2 = (float)cutLine.X2 * scale;
                var y2 = (float)(sheetLength - cutLine.Y2) * scale;

                sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' ");
                sb.Append($"stroke='{color}' stroke-width='2' stroke-dasharray='{dash}' ");
                sb.Append($"opacity='0.7' stroke-linecap='round' />");
            }
        }
    }
}
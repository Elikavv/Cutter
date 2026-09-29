// cutInteractive.js
window.initInteractiveSVG = function () {
    // Hover эффекты
    window.highlightDetail = function (elementId, isHover) {
        var rect = document.getElementById(elementId);
        var text = document.getElementById('text_' + elementId);
        if (rect && text) {
            if (isHover) {
                rect.style.fill = '#E8F5E9';
                rect.style.stroke = '#1B5E20';
                rect.style.strokeWidth = '3';
                text.style.fill = '#1B5E20';
            } else {
                if (rect.getAttribute('data-selected') !== 'true') {
                    rect.style.fill = 'white';
                    rect.style.stroke = '#2E7D32';
                    rect.style.strokeWidth = '2';
                    text.style.fill = '#2E7D32';
                }
            }
        }
    };

    // Клик по детали
    window.selectDetail = function (name, width, length, count, elementId) {
        var allRects = document.querySelectorAll('svg rect[id^="detail_"]');
        allRects.forEach(function (rect) {
            rect.setAttribute('data-selected', 'false');
            rect.style.fill = 'white';
            rect.style.stroke = '#2E7D32';
            rect.style.strokeWidth = '2';
            var text = document.getElementById('text_' + rect.id);
            if (text) text.style.fill = '#2E7D32';
        });

        var rect = document.getElementById(elementId);
        if (rect) {
            rect.setAttribute('data-selected', 'true');
            rect.style.fill = '#BBDEFB';
            rect.style.stroke = '#1565C0';
            rect.style.strokeWidth = '3';
            var text = document.getElementById('text_' + elementId);
            if (text) text.style.fill = '#1565C0';
        }

        window.showTooltip(name, width, length, count);
    };

    // Показ tooltip
    window.showTooltip = function (name, width, length, count) {
        var tooltip = document.getElementById('detailTooltip');
        var tooltipName = document.getElementById('tooltipName');
        var tooltipSize = document.getElementById('tooltipSize');

        if (tooltip && tooltipName && tooltipSize) {
            tooltipName.textContent = name;
            tooltipSize.textContent = width + ' × ' + length + ' мм • Всего: ' + count + ' шт.';
            tooltip.style.display = 'block';

            document.addEventListener('mousemove', window.moveTooltip);
        }
    };

    // Движение tooltip
    window.moveTooltip = function (e) {
        var tooltip = document.getElementById('detailTooltip');
        if (!tooltip) return;

        var rect = tooltip.getBoundingClientRect();
        var x = e.pageX + 15;
        var y = e.pageY + 15;

        if (x + rect.width > window.innerWidth) {
            x = e.pageX - rect.width - 15;
        }
        if (y + rect.height > window.innerHeight) {
            y = e.pageY - rect.height - 15;
        }

        tooltip.style.left = x + 'px';
        tooltip.style.top = y + 'px';
    };

    // Скрытие tooltip
    document.addEventListener('mouseleave', function () {
        var tooltip = document.getElementById('detailTooltip');
        if (tooltip) {
            tooltip.style.display = 'none';
        }
        document.removeEventListener('mousemove', window.moveTooltip);
    });
};
// Функция для точного рендеринга листа с масштабированием
function renderSheet(container, sheetWidth, sheetHeight, details) {
    const containerWidth = container.clientWidth;
    const containerHeight = container.clientHeight;

    // Рассчитываем масштаб
    const scaleX = containerWidth / sheetWidth;
    const scaleY = containerHeight / sheetHeight;
    const scale = Math.min(scaleX, scaleY) * 0.95; // 5% отступ

    // Очищаем контейнер
    container.innerHTML = '';

    // Создаем SVG элемент
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute('width', '100%');
    svg.setAttribute('height', '100%');
    svg.setAttribute('viewBox', `0 0 ${sheetWidth} ${sheetHeight}`);
    svg.style.backgroundColor = '#f8f9fa';
    svg.style.border = '1px solid #333';

    // Добавляем детали
    details.forEach(detail => {
        const svgY = sheetHeight - detail.y - detail.length;
        const part = document.createElementNS("http://www.w3.org/2000/svg", "rect");
        part.setAttribute('x', detail.x);
        //part.setAttribute('y', detail.y);
        part.setAttribute('y', svgY); // ← Исправлено!
        part.setAttribute('width', detail.width);
        part.setAttribute('height', detail.length);
        part.setAttribute('fill', detail.rotated ? 'rgba(220, 53, 69, 0.3)' : 'rgba(0, 123, 255, 0.3)');
        part.setAttribute('stroke', detail.rotated ? '#dc3545' : '#007bff');
        part.setAttribute('stroke-width', '1');

        // Добавляем текст
        const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
        text.setAttribute('x', detail.x + detail.width / 2);
        //text.setAttribute('y', detail.y + detail.length / 2);
        text.setAttribute('y', svgY + detail.length / 2); // ← Исправлено!
        text.setAttribute('text-anchor', 'middle');
        text.setAttribute('dominant-baseline', 'middle');
        text.setAttribute('font-size', '38');
        text.textContent = `${detail.name} (${detail.width}x${detail.length})`;

        svg.appendChild(part);
        svg.appendChild(text);
    });

    container.appendChild(svg);
}

// Инициализация 3D просмотра с Three.js
function init3DView(container, sheetWidth, sheetHeight, details) {
    // Реализация 3D визуализации с использованием Three.js
    // (требуется подключение three.js в проекте)
}
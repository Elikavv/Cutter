window.initManagerChart = function (dailyStats) {
    const canvas = document.getElementById('managerTrendChart');
    if (!canvas) {
        //console.error('Canvas #managerTrendChart не найден');
        return;
    }

    const ctx = canvas.getContext('2d');

    // Уничтожаем старый график
    if (window.managerChartInstance) {
        window.managerChartInstance.destroy();
    }

    const labels = dailyStats.map(d => d.dateLabel);
    const dataPlans = dailyStats.map(d => d.plansCount);
    const dataAmount = dailyStats.map(d => d.totalAmount);

    window.managerChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Бланков',
                    data: dataPlans,
                    borderColor: '#28A745',
                    backgroundColor: 'rgba(40, 167, 69, 0.0)',
                    yAxisID: 'y',
                    tension: 0.3,
                    borderDash: [5, 5]
                },
                {
                    label: 'Сумма (₽)',
                    data: dataAmount,
                    borderColor: '#0055A4',
                    backgroundColor: 'rgba(0, 85, 164, 0.1)',
                    yAxisID: 'y1',
                    tension: 0.3,
                    fill: true,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: { display: true, text: 'Кол-во бланков' },
                    beginAtZero: true
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    grid: { drawOnChartArea: false },
                    title: { display: true, text: 'Сумма (₽)' },
                    beginAtZero: true
                }
            }
        }
    });
};

window.initStatsTrendChart = function (canvasId, trendData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    if (window.statsTrendChart) window.statsTrendChart.destroy();

    const labels = trendData.map(d => d.dateLabel);
    const actualPlans = trendData.map(d => d.actualPlans);
    const actualCuts = trendData.map(d => d.actualCuts);
    const actualAmount = trendData.map(d => d.actualAmount);
    const forecastPlans = trendData.map(d => d.forecastPlans);
    const forecastCuts = trendData.map(d => d.forecastCuts);
    const forecastAmount = trendData.map(d => d.forecastAmount);

    window.statsTrendChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Факт бланков',
                    data: actualPlans,
                    borderColor: '#0055A4',
                    backgroundColor: 'rgba(0, 85, 164, 0.1)',
                    tension: 0.3,
                    fill: true,
                    yAxisID: 'y'
                },
                {
                    label: 'Факт резов',
                    data: actualCuts,
                    borderColor: '#28A745',
                    backgroundColor: 'rgba(40, 167, 69, 0.1)',
                    tension: 0.3,
                    fill: true,
                    yAxisID: 'y'
                },
                {
                    label: 'Факт суммы (₽)',
                    data: actualAmount,
                    borderColor: '#17A2B8',
                    backgroundColor: 'rgba(23, 162, 184, 0.1)',
                    tension: 0.3,
                    fill: true,
                    yAxisID: 'y1'
                },
                {
                    label: 'Прогноз бланков',
                    data: forecastPlans,
                    borderColor: '#FFC107',
                    borderDash: [5, 5],
                    tension: 0.3,
                    pointRadius: 4,
                    pointBackgroundColor: '#FFC107',
                    yAxisID: 'y'
                },
                {
                    label: 'Прогноз резов',
                    data: forecastCuts,
                    borderColor: '#FF6B6B',
                    borderDash: [5, 5],
                    tension: 0.3,
                    pointRadius: 4,
                    pointBackgroundColor: '#FF6B6B',
                    yAxisID: 'y'
                },
                {
                    label: 'Прогноз суммы (₽)',
                    data: forecastAmount,
                    borderColor: '#9C27B0',
                    borderDash: [5, 5],
                    tension: 0.3,
                    pointRadius: 4,
                    pointBackgroundColor: '#9C27B0',
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: { position: 'top' }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: { display: true, text: 'Количество' }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    grid: { drawOnChartArea: false },
                    title: { display: true, text: 'Сумма (₽)' }
                }
            }
        }
    });
};

window.initStatsEmployeeChart = function (canvasId, empData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    if (window.statsEmpChart) window.statsEmpChart.destroy();

    window.statsEmpChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: empData.map(e => e.userName),
            datasets: [{
                label: 'Сумма услуг (₽)',
                data: empData.map(e => e.totalAmount),
                backgroundColor: '#28A745',
                borderRadius: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'y', // Горизонтальный барчарт
            plugins: { legend: { display: false } }
        }
    });
};
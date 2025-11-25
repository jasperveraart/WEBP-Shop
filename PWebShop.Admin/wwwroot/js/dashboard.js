window.dashboardCharts = window.dashboardCharts || {
    instances: {},
    renderWeeklyOrdersChart: function (canvasId, labels, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        // Dispose existing chart instance when re-rendering
        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
        }

        const gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 280);
        gradient.addColorStop(0, 'rgba(59, 130, 246, 0.9)');
        gradient.addColorStop(1, 'rgba(31, 61, 115, 0.85)');

        this.instances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [
                    {
                        label: 'Orders',
                        data,
                        tension: 0.4,
                        fill: true,
                        backgroundColor: gradient,
                        borderColor: '#1f3d73',
                        borderWidth: 3,
                        pointRadius: 5,
                        pointBackgroundColor: '#ffffff',
                        pointBorderColor: '#1f3d73',
                        pointBorderWidth: 2,
                        pointHoverRadius: 7,
                        pointHoverBorderWidth: 3
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            title: (ctx) => `Orders on ${ctx[0].label}`,
                            label: (ctx) => `${ctx.raw} orders`
                        },
                        backgroundColor: '#1f3d73',
                        padding: 12,
                        displayColors: false
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#4b5563',
                            font: {
                                weight: '600'
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(31, 61, 115, 0.08)'
                        },
                        ticks: {
                            color: '#4b5563',
                            stepSize: 5
                        }
                    }
                }
            }
        });
    }
};

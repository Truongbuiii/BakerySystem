/**
 * BakerySystem Dashboard ApexCharts Integration
 */

window.initBakeryDashboardCharts = function (categorySeries, categoryLabels) {
    if (typeof ApexCharts === 'undefined') {
        setTimeout(function() {
            window.initBakeryDashboardCharts(categorySeries, categoryLabels);
        }, 120);
        return;
    }

    // 1. Revenue Updates Stacked Bar Chart
    const revenueEl = document.querySelector("#revenue-updates");
    if (revenueEl) {
        revenueEl.innerHTML = "";
        const revenueChartOptions = {
            series: [
                {
                    name: 'Doanh thu bánh (triệu)',
                    data: [15, 27, 22, 30, 18, 25, 24, 32, 28, 35, 30, 38],
                    color: '#1e40af',
                },
                {
                    name: 'Chi phí nguyên liệu (triệu)',
                    data: [-8, -11, -9, -12, -7, -10, -9, -13, -11, -14, -12, -15],
                    color: '#94a3b8',
                },
            ],
            chart: {
                type: 'bar',
                fontFamily: "inherit",
                foreColor: '#adb0bb',
                toolbar: { show: false },
                height: 320,
                stacked: true,
            },
            plotOptions: {
                bar: {
                    horizontal: false,
                    barHeight: "60%",
                    columnWidth: "36%",
                    borderRadius: 5,
                    borderRadiusApplication: 'end',
                    borderRadiusWhenStacked: 'all',
                },
            },
            stroke: { show: false },
            dataLabels: { enabled: false },
            legend: { show: false },
            grid: {
                borderColor: 'rgba(0,0,0,0.06)',
                strokeDashArray: 3,
                xaxis: { lines: { show: false } },
            },
            yaxis: {
                min: -20,
                max: 45,
                tickAmount: 5,
                labels: {
                    formatter: function (val) {
                        return val + "Tr";
                    }
                }
            },
            xaxis: {
                categories: ["T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12"],
                axisBorder: { show: false },
            },
            tooltip: {
                theme: document.documentElement.classList.contains('dark') ? "dark" : "light",
                y: {
                    formatter: function (val) {
                        return Math.abs(val) + " triệu VNĐ";
                    }
                }
            },
        };
        const revChart = new ApexCharts(revenueEl, revenueChartOptions);
        revChart.render();
    }

    // 2. Category Breakup Donut Chart
    const breakupEl = document.querySelector("#breakup");
    if (breakupEl && typeof ApexCharts !== 'undefined') {
        breakupEl.innerHTML = "";
        const seriesData = (categorySeries && categorySeries.length > 0) ? categorySeries : [45, 35, 20];
        const labelsData = (categoryLabels && categoryLabels.length > 0) ? categoryLabels : ["Bánh kem", "Bánh ngọt", "Bánh mì"];
        const breakupOptions = {
            series: seriesData,
            labels: labelsData,
            chart: {
                height: 175,
                type: "donut",
                fontFamily: "inherit",
                foreColor: "#adb0bb",
            },
            colors: ["#1e40af", "#3b82f6", "#10b981", "#f59e0b", "#8b5cf6"],
            plotOptions: {
                pie: {
                    startAngle: 0,
                    endAngle: 360,
                    donut: {
                        size: '72%',
                    },
                },
            },
            stroke: { show: false },
            dataLabels: { enabled: false },
            legend: { show: false },
            tooltip: {
                theme: document.documentElement.classList.contains('dark') ? "dark" : "light",
                fillSeriesColor: false,
                y: {
                    formatter: function (val) {
                        return val + "% tỷ trọng";
                    }
                }
            },
        };
        const breakupChart = new ApexCharts(breakupEl, breakupOptions);
        breakupChart.render();
    }

    // 3. Monthly Earning Sparkline Chart
    const earningEl = document.querySelector("#earning");
    if (earningEl) {
        earningEl.innerHTML = "";
        const earningOptions = {
            chart: {
                id: "monthly-sparkline",
                type: "area",
                height: 60,
                sparkline: { enabled: true },
                group: 'sparklines',
                fontFamily: "inherit",
                foreColor: "#adb0bb",
            },
            series: [
                {
                    name: 'Doanh số tuần',
                    color: "#1e40af",
                    data: [25, 45, 30, 60, 48, 70, 62, 85],
                },
            ],
            stroke: {
                curve: "smooth",
                width: 2,
            },
            fill: {
                type: "gradient",
                gradient: {
                    shadeIntensity: 0,
                    inverseColors: false,
                    opacityFrom: 0.45,
                    opacityTo: 0,
                    stops: [20, 180],
                },
            },
            markers: { size: 0 },
            tooltip: {
                theme: document.documentElement.classList.contains('dark') ? "dark" : "light",
                x: { show: false },
            },
        };
        const earningChart = new ApexCharts(earningEl, earningOptions);
        earningChart.render();
    }
};

// Auto-run when DOM is loaded or after Blazor navigation
document.addEventListener("DOMContentLoaded", function () {
    setTimeout(window.initBakeryDashboardCharts, 200);
});

window.__bakeryReportsFinChart = null;
window.__bakeryReportsDonutChart = null;

/**
 * BakerySystem Reports Charts
 * Renders Revenue vs COGS vs Gross Profit and Category Net Revenue Donut
 */
window.initBakeryReportsCharts = function (labels, revSeries, costSeries, profitSeries, donutSeries, donutLabels, donutColors) {
    if (typeof ApexCharts === 'undefined') {
        setTimeout(function () {
            window.initBakeryReportsCharts(labels, revSeries, costSeries, profitSeries, donutSeries, donutLabels, donutColors);
        }, 120);
        return;
    }

    const isDark = document.documentElement.classList.contains('dark');
    const axisLabelColor = isDark ? '#94a3b8' : '#64748b';

    // 1. Comparison Chart: Doanh thu thực thu vs Giá vốn (COGS) vs Lợi nhuận gộp
    const finChartEl = document.querySelector("#reports-financial-chart");
    if (finChartEl) {
        if (window.__bakeryReportsFinChart) {
            try { window.__bakeryReportsFinChart.destroy(); } catch { }
            window.__bakeryReportsFinChart = null;
        }
        finChartEl.innerHTML = "";

        const options = {
            series: [
                { name: 'Doanh thu thực thu', type: 'column', data: revSeries || [] },
                { name: 'Giá vốn (COGS)', type: 'column', data: costSeries || [] },
                { name: 'Lợi nhuận gộp', type: 'line', data: profitSeries || [] }
            ],
            chart: {
                height: 260,
                type: 'line',
                stacked: false,
                toolbar: { show: false },
                fontFamily: 'inherit',
                animations: { speed: 350 }
            },
            colors: ['#2563eb', '#f59e0b', '#10b981'],
            stroke: {
                width: [0, 0, 3],
                curve: 'smooth'
            },
            plotOptions: {
                bar: {
                    horizontal: false,
                    columnWidth: '48%',
                    borderRadius: 5,
                    borderRadiusApplication: 'end'
                }
            },
            markers: {
                size: [0, 0, 4],
                strokeWidth: 2,
                hover: { size: 6 }
            },
            dataLabels: { enabled: false },
            xaxis: {
                categories: labels || [],
                labels: {
                    style: { colors: axisLabelColor, fontSize: '11px', fontWeight: 500 }
                },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: {
                labels: {
                    style: { colors: axisLabelColor, fontSize: '11px' },
                    formatter: function (val) {
                        const abs = Math.abs(val);
                        const sign = val < 0 ? '-' : '';
                        if (abs >= 1000000) return sign + (abs / 1000000).toFixed(1) + ' tr';
                        if (abs >= 1000) return sign + (abs / 1000).toFixed(0) + ' k';
                        return val;
                    }
                }
            },
            grid: {
                borderColor: isDark ? '#334155' : '#e2e8f0',
                strokeDashArray: 3,
                xaxis: { lines: { show: false } }
            },
            fill: { opacity: [0.95, 0.9, 1] },
            tooltip: {
                shared: true,
                intersect: false,
                theme: isDark ? 'dark' : 'light',
                y: {
                    formatter: function (val) {
                        if (val === null || val === undefined) return '0 đ';
                        return new Intl.NumberFormat('vi-VN').format(Math.round(val)) + ' đ';
                    }
                }
            },
            legend: {
                position: 'top',
                horizontalAlign: 'right',
                fontSize: '12px',
                fontWeight: 500,
                labels: { colors: isDark ? '#cbd5e1' : '#334155' },
                markers: { radius: 4 }
            }
        };

        window.__bakeryReportsFinChart = new ApexCharts(finChartEl, options);
        window.__bakeryReportsFinChart.render();
    }

    // 2. Category Donut Chart (Net Revenue after proportional voucher allocation)
    const donutEl = document.querySelector("#reports-category-donut");
    if (donutEl) {
        if (window.__bakeryReportsDonutChart) {
            try { window.__bakeryReportsDonutChart.destroy(); } catch { }
            window.__bakeryReportsDonutChart = null;
        }
        donutEl.innerHTML = "";

        const hasData = Array.isArray(donutSeries) && donutSeries.some(v => v > 0);
        const palette = (Array.isArray(donutColors) && donutColors.length > 0)
            ? donutColors
            : ['#2563eb', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4'];

        const donutOptions = {
            series: hasData ? donutSeries : [1],
            labels: hasData ? donutLabels : ['Chưa có doanh thu'],
            chart: {
                type: 'donut',
                height: 200,
                fontFamily: 'inherit'
            },
            colors: hasData ? palette : [isDark ? '#334155' : '#e2e8f0'],
            dataLabels: { enabled: false },
            legend: { show: false },
            plotOptions: {
                pie: {
                    donut: {
                        size: '70%',
                        labels: {
                            show: true,
                            name: {
                                show: true,
                                fontSize: '11px',
                                color: axisLabelColor
                            },
                            value: {
                                show: true,
                                fontSize: '14px',
                                fontWeight: 700,
                                color: isDark ? '#f8fafc' : '#0f172a',
                                formatter: function (val) {
                                    if (!hasData) return '0 đ';
                                    return new Intl.NumberFormat('vi-VN').format(Math.round(val)) + ' đ';
                                }
                            },
                            total: {
                                show: true,
                                label: 'Thực thu',
                                fontSize: '11px',
                                color: axisLabelColor,
                                formatter: function (w) {
                                    if (!hasData) return '0 đ';
                                    const total = w.globals.seriesTotals.reduce((a, b) => a + b, 0);
                                    return new Intl.NumberFormat('vi-VN').format(Math.round(total)) + ' đ';
                                }
                            }
                        }
                    }
                }
            },
            stroke: {
                colors: [isDark ? '#1e293b' : '#ffffff'],
                width: 2
            },
            tooltip: {
                enabled: hasData,
                theme: isDark ? 'dark' : 'light',
                y: {
                    formatter: function (val) {
                        return new Intl.NumberFormat('vi-VN').format(Math.round(val)) + ' đ';
                    }
                }
            }
        };

        window.__bakeryReportsDonutChart = new ApexCharts(donutEl, donutOptions);
        window.__bakeryReportsDonutChart.render();
    }
};

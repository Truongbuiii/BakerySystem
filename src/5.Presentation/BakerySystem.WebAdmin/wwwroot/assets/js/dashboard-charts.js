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

/**
 * BakerySystem Reports Charts (Module 7)
 */
window.initBakeryReportsCharts = function (categories, revenueSeries, costSeries, catDonutSeries, catDonutLabels) {
    if (typeof ApexCharts === 'undefined') {
        setTimeout(function() {
            window.initBakeryReportsCharts(categories, revenueSeries, costSeries, catDonutSeries, catDonutLabels);
        }, 120);
        return;
    }

    const isDark = document.documentElement.classList.contains('dark');

    // 1. Financial Comparison Chart (Doanh thu vs Chi phí vốn)
    const finEl = document.querySelector("#reports-financial-chart");
    if (finEl) {
        finEl.innerHTML = "";
        const finOptions = {
            series: [
                {
                    name: 'Doanh thu thực thu',
                    data: revenueSeries || []
                },
                {
                    name: 'Chi phí vốn nhập hàng',
                    data: costSeries || []
                }
            ],
            chart: {
                type: 'bar',
                height: 320,
                fontFamily: "inherit",
                toolbar: { show: false }
            },
            colors: ['#2563eb', '#f59e0b'],
            plotOptions: {
                bar: {
                    horizontal: false,
                    columnWidth: '42%',
                    borderRadius: 6,
                    borderRadiusApplication: 'end'
                }
            },
            dataLabels: { enabled: false },
            stroke: {
                show: true,
                width: 2,
                colors: ['transparent']
            },
            xaxis: {
                categories: categories || [],
                labels: {
                    style: {
                        colors: isDark ? '#94a3b8' : '#64748b',
                        fontSize: '11px',
                        fontWeight: 500
                    }
                },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: {
                labels: {
                    formatter: function (val) {
                        if (val >= 1000000) return (val / 1000000).toFixed(1) + ' tr';
                        if (val >= 1000) return (val / 1000).toFixed(0) + ' k';
                        return val;
                    },
                    style: {
                        colors: isDark ? '#94a3b8' : '#64748b',
                        fontSize: '11px'
                    }
                }
            },
            grid: {
                borderColor: isDark ? '#334155' : '#e2e8f0',
                strokeDashArray: 3,
                xaxis: { lines: { show: false } }
            },
            fill: { opacity: 1 },
            tooltip: {
                theme: isDark ? 'dark' : 'light',
                y: {
                    formatter: function (val) {
                        return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
                    }
                }
            },
            legend: {
                position: 'top',
                horizontalAlign: 'right',
                labels: {
                    colors: isDark ? '#cbd5e1' : '#475569'
                }
            }
        };
        const finChart = new ApexCharts(finEl, finOptions);
        finChart.render();
    }

    // 2. Category Share Donut Chart (Cơ cấu doanh thu theo Loại bánh)
    const donutEl = document.querySelector("#reports-category-donut");
    if (donutEl) {
        donutEl.innerHTML = "";
        const donutOptions = {
            series: catDonutSeries && catDonutSeries.length > 0 ? catDonutSeries : [1],
            labels: catDonutLabels && catDonutLabels.length > 0 ? catDonutLabels : ['Chưa có dữ liệu'],
            chart: {
                type: 'donut',
                height: 280,
                fontFamily: "inherit"
            },
            colors: ['#2563eb', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4'],
            dataLabels: { enabled: false },
            legend: {
                position: 'bottom',
                fontSize: '11px',
                labels: {
                    colors: isDark ? '#cbd5e1' : '#475569'
                }
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: '72%',
                        labels: {
                            show: true,
                            name: { fontSize: '12px', color: isDark ? '#cbd5e1' : '#64748b' },
                            value: {
                                fontSize: '15px',
                                fontWeight: 700,
                                color: isDark ? '#f8fafc' : '#0f172a',
                                formatter: function (val) {
                                    return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
                                }
                            },
                            total: {
                                show: true,
                                label: 'Tổng doanh thu',
                                color: isDark ? '#cbd5e1' : '#64748b',
                                formatter: function (w) {
                                    const total = w.globals.seriesTotals.reduce((a, b) => a + b, 0);
                                    return new Intl.NumberFormat('vi-VN').format(total) + ' đ';
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
                theme: isDark ? 'dark' : 'light',
                y: {
                    formatter: function (val) {
                        return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
                    }
                }
            }
        };
        const donutChart = new ApexCharts(donutEl, donutOptions);
        donutChart.render();
    }
};

// Auto-run when DOM is loaded or after Blazor navigation
document.addEventListener("DOMContentLoaded", function () {
    setTimeout(window.initBakeryDashboardCharts, 200);
});

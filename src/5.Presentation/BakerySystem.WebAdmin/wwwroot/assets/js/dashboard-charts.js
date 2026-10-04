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

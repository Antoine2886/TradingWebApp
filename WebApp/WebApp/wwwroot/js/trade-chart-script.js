let symbol = null;
let interval = null;
let chartType = null;
let timeZone = null;
let theme = null;
let lineColor = null;
let upColor = null;
let downColor = null;
document.addEventListener("DOMContentLoaded", function () {




    let series = null;
    let overlayCanvas = null;
    const chartContainer = document.getElementById('chartContainer');
    const chartCanvas = document.getElementById('chartCanvas');
    const overlayCanvasElement = document.getElementById('overlayCanvas');
    const drawingModeButton = document.getElementById('toggleDrawingMode');
    const chart = LightweightCharts.createChart(chartCanvas, {
        width: chartContainer.clientWidth,
        height: 500,
        priceScale: {
            position: 'right',
            mode: LightweightCharts.PriceScaleMode.Normal,
            borderColor: '#e0e0e0',
            autoScale: true,
            priceFormat: {
                type: 'price',
                precision: 5,  // Number of decimal places
                minMove: 0.00001,  // Minimum price movement
            },
        },

        handleScroll: {
            mouseWheel: true,
            pressedMouseMove: true,
            horzTouchDrag: true,
            vertTouchDrag: true,
        },
        handleScale: {
            axisPressedMouseMove: true,
            mouseWheel: true,
            pinch: true,
        },
        timeScale: {
            timeVisible: true,
            secondsVisible: true,

        },
    });
    series = chart.addLineSeries();
    series.applyOptions({
        priceFormat: {
            type: 'price',
            precision: 5,  // Number of decimal places
            minMove: 0.00001,  // Minimum price movement
        },
    });
    overlayCanvas = new fabric.Canvas('overlayCanvas', {
        selection: false,
    });
    let currentTool = null;

    // Function to clear canvas events
    const clearCanvasEvents = () => {
        overlayCanvas.isDrawingMode = false;
        overlayCanvas.off('mouse:down');
        overlayCanvas.off('mouse:move');
        overlayCanvas.off('mouse:up');
    };



    // Function to get timezone offset in seconds
    const getOffsetInSeconds2 = (date, timeZone) => {
        const tzDate = new Intl.DateTimeFormat('en-US', {
            timeZone,
            hour12: false,
            timeZoneName: 'longOffset',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
        }).formatToParts(date);
        let offsetParts2 = tzDate.find(part => part.type === 'timeZoneName').value;
        let match2 = offsetParts2.match(/GMT([+-]\d{1,2})/);
        if (!match2) return 0;

        const hours = parseInt(match2[1], 10);
        const minutes = match2[2] ? parseInt(match2[2], 10) : 0;
        return (hours * 3600)
    };
    // Sync overlay canvas size with chart container
    const syncOverlayCanvasSize = () => {
        overlayCanvasElement.width = chartContainer.clientWidth;
        overlayCanvasElement.height = chartContainer.clientHeight;
        overlayCanvas.setWidth(chartContainer.clientWidth);
        overlayCanvas.setHeight(chartContainer.clientHeight);
        overlayCanvas.renderAll();
    };


    syncOverlayCanvasSize();


    overlayCanvas.renderAll();

    // Convert time to the adjusted timezone
    window.timeToTzAndAdjust = (originalTime, timeZone) => {
        // Get the current New York time
        const interval = document.getElementById('intervalSelect').value;

        const newYorkTime = new Date(new Date().toLocaleString('en-US', { timeZone: 'America/New_York' }));

        // Handle intraday intervals
        const date = new Date(new Date(originalTime).toLocaleString('en-US', { timeZone }));
        const adjustedDate = new Date(date.getTime() - 4 * 60 * 60 * 1000); // Subtract 4 hours
        return Math.floor(adjustedDate.getTime() / 1000); // Return the adjusted date in seconds

    };




    // Fetch data for the chart
    const fetchData = () => {
        fetch(`/api/Trade/historical?symbol=${symbol}&interval=${interval}`, {
        })
            .then(response => response.json())
            .then(data => {
                console.log('API Data:', data); // Log the raw data

                let formattedData = [];

                if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns' || chartType === 'baseline') {
                    formattedData = data.map(item => ({
                        time: timeToTzAndAdjust(item.timeStamp, timeZone),
                        value: item.close,
                    })).filter(item => item.time !== null && item.time !== undefined && item.value !== null && item.value !== undefined);
                } else if (chartType === 'candlestick' || chartType === 'bar' || chartType === 'hollowCandle' || chartType === 'highLow') {
                    formattedData = data.map(item => ({
                        time: timeToTzAndAdjust(item.timeStamp, timeZone),
                        open: item.open,
                        high: item.high,
                        low: item.low,
                        close: item.close,
                    })).filter(item => item.time !== null && item.time !== undefined && item.open !== null && item.open !== undefined && item.high !== null && item.high !== undefined && item.low !== null && item.low !== undefined && item.close !== null && item.close !== undefined);
                }

                console.log('Formatted Data:', formattedData);

                series.setData(formattedData);
                series.applyOptions({
                    priceFormat: {
                        type: 'price',
                        precision: 5,  // Number of decimal places
                        minMove: 0.00001,  // Minimum price movement
                    },
                })
                // Ensure the visible range includes the latest data
                if (formattedData.length > 0) {
                    const lastDataPoint = formattedData[formattedData.length - 1].time;
                    chart.timeScale().setVisibleRange({
                        from: formattedData[0].time,
                        to: lastDataPoint,
                    });
                }
                updateChartTimeScale();
            })
            .catch(error => console.error('Error fetching data:', error));

    };



        window.timeToTzAndAdjust = (originalTime, timeZone) => {
            const date = new Date(new Date(originalTime).toLocaleString('en-US', { timeZone }));
            const adjustedDate = new Date(date.getTime() - 4 * 60 * 60 * 1000);
            return Math.floor(adjustedDate.getTime() / 1000);
    };


    // Load chart settings by ID
        const loadChartSettingsById = (id) => {
            fetch(`/api/Trade/load-settings-by-id?id=${id}`)
                .then(response => response.json())
                .then(settings => {
                    if (settings) {
                        symbol = settings.symbol;
                        interval = settings.interval;
                        chartType = settings.chartType;
                        timeZone = getUserLocalTimeZone();
                        theme = settings.theme;
                        lineColor = settings.lineColor;
                        upColor = settings.upColor;
                        downColor = settings.downColor;

                        document.getElementById('Title').innerText = settings.symbol;

                        overlayCanvas.clear();

                        updateChartTheme();
                        updateChartType();
                        updateChartTimeScale()

                        updateSeriesColor(settings.chartType);
                        fetchData();

                        settings.drawings.forEach(drawing => {
                            const coordinates = JSON.parse(drawing.coordinates);
                            addDrawingToCanvas(drawing.type, coordinates); // Function to add drawings to the canvas
                        });
                    } else {
                        console.warn('No settings found for the specified id.');
                    }
                })
                .catch(error => console.error('Error loading chart settings:', error));
        };
    const chartSettingsId = document.getElementById('chartSettingsId').value;
    loadChartSettingsById(chartSettingsId);


    // Update the chart time scale based on the interval
        const updateChartTimeScale = () => {

            if (['1D', '1W', '1M'].includes(interval)) {
                chart.applyOptions({
                    timeScale: {
                        timeVisible: false, // Hide time for daily and higher intervals
                    },
                });
            } else {
                chart.applyOptions({
                    timeScale: {
                        timeVisible: true,
                        secondsVisible: true,

                    },
                });
            }
        };
    // Adjust the time to the nearest interval
        function floorToNearestInterval(time, interval) {
            const date = new Date(time * 1000);

            if (interval === '1min') {
                date.setUTCSeconds(0, 0);
            } else if (interval === '3min') {
                const minutes = date.getUTCMinutes();
                const adjustedMinutes = Math.floor(minutes / 3) * 3;
                date.setUTCMinutes(adjustedMinutes, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '5min') {
                const minutes = date.getUTCMinutes();
                const adjustedMinutes = Math.floor(minutes / 5) * 5;
                date.setUTCMinutes(adjustedMinutes, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '15min') {
                const minutes = date.getUTCMinutes();
                const adjustedMinutes = Math.floor(minutes / 15) * 15;
                date.setUTCMinutes(adjustedMinutes, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '30min') {
                const minutes = date.getUTCMinutes();
                const adjustedMinutes = Math.floor(minutes / 30) * 30;
                date.setUTCMinutes(adjustedMinutes, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '1h') {
                date.setUTCMinutes(0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '2h') {
                const hours = date.getUTCHours();
                const adjustedHours = Math.floor(hours / 2) * 2;
                date.setUTCHours(adjustedHours, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '4h') {
                const hours = date.getUTCHours();
                const adjustedHours = Math.floor(hours / 4) * 4;
                date.setUTCHours(adjustedHours, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '8h') {
                const hours = date.getUTCHours();
                const adjustedHours = Math.floor(hours / 8) * 8;
                date.setUTCHours(adjustedHours, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '1D') {
                date.setUTCHours(0, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '1W') {
                const day = date.getUTCDay();
                const diff = date.getUTCDate() - day + (day === 0 ? -6 : 1); // adjust when day is Sunday
                date.setUTCDate(diff);
                date.setUTCHours(0, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            } else if (interval === '1M') {
                date.setUTCDate(1);
                date.setUTCHours(0, 0, 0, 0);
                date.setUTCSeconds(0, 0);
            }

            return Math.floor(date.getTime() / 1000);
        }

    // Adjust from UTC to the specified timezone
        const adjustFromUTC2 = (utcTime, timeZone, interval) => {
            // Floor the time to the nearest interval
            const flooredUtcTime = floorToNearestInterval(utcTime, interval);

            // Create a date object from the floored UTC time
            const date = new Date(flooredUtcTime * 1000);

            // Calculate the offset for the given time zone
            const tzOffset = getOffsetInSeconds2(date, timeZone);

            // Add the offset to the floored time to get the adjusted time in the specified time zone
            const adjustedTime = flooredUtcTime + tzOffset;

            // Convert back to seconds for consistency
            return adjustedTime;
        };

    // Update the chart theme
        const updateChartTheme = () => {
            const chartOptions = theme === 'dark' ? {
                layout: {
                    background: { color: '#000000' },
                    textColor: '#FFFFFF',
                },
                grid: {
                    vertLines: { color: '#404040' },
                    horzLines: { color: '#404040' },
                },
            } : {
                layout: {
                    background: { color: '#FFFFFF' },
                    textColor: '#000000',
                },
                grid: {
                    vertLines: { color: '#e0e0e0' },
                    horzLines: { color: '#e0e0e0' },
                },
            };
            chart.applyOptions(chartOptions);

            // Update series color based on chart type and theme
            const chartType = document.getElementById('chartTypeSelect').value;
            updateSeriesColor(chartType);
        };

    // Update the series color based on chart type and theme
        const updateSeriesColor = (chartType) => {
            if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns') {
                series.applyOptions({
                    color: lineColor,
                });
            } else if (chartType === 'candlestick' || chartType === 'bar' || chartType === 'hollowCandle' || chartType === 'highLow') {
                series.applyOptions({
                    upColor: upColor,
                    downColor: downColor,
                    borderUpColor: upColor,
                    borderDownColor: downColor,
                    wickUpColor: upColor,
                    wickDownColor: downColor,
                });
            } else if (chartType === 'columns') {
                series.applyOptions({
                    color: upColor,
                    base: 0, // you can customize the base value
                    borderColor: downColor,
                });
            } else if (chartType === 'baseline') {
                series.applyOptions({
                    baseValue: { type: 'price', price: 0 },
                    topFillColor1: upColor,
                    topFillColor2: upColor,
                    bottomFillColor1: downColor,
                    bottomFillColor2: downColor,
                });
            }
            series.applyOptions({
                priceFormat: {
                    type: 'price',
                    precision: 5,  // Number of decimal places
                    minMove: 0.00001,  // Minimum price movement
                }
            });
        };

    // Update the chart type
        const updateChartType = () => {

            chart.removeSeries(series);

            if (chartType === 'line') {
                series = chart.addLineSeries({
                    color: lineColor,
                });
            } else if (chartType === 'area') {
                series = chart.addAreaSeries({
                    color: lineColor,
                });
            } else if (chartType === 'candlestick') {
                series = chart.addCandlestickSeries({
                    upColor: upColor,
                    downColor: downColor,
                    borderUpColor: upColor,
                    borderDownColor: downColor,
                    wickUpColor: upColor,
                    wickDownColor: downColor,
                });
            } else if (chartType === 'bar') {
                series = chart.addBarSeries({
                    upColor: upColor,
                    downColor: downColor,
                    borderUpColor: upColor,
                    borderDownColor: downColor,
                    wickUpColor: upColor,
                    wickDownColor: downColor,
                });
            } else if (chartType === 'histogram') {
                series = chart.addHistogramSeries({
                    color: upColor,
                    base: 0,
                    borderColor: downColor,
                });
            } else if (chartType === 'baseline') {
                series = chart.addBaselineSeries({
                    baseValue: { type: 'price', price: 0 },
                    topFillColor1: upColor,
                    topFillColor2: downColor,
                    bottomFillColor1: downColor,
                    bottomFillColor2: upColor,
                });
            } else if (chartType === 'hollowCandle') {
                series = chart.addCandlestickSeries({
                    upColor: upColor,
                    downColor: downColor,
                    borderUpColor: upColor,
                    borderDownColor: downColor,
                    wickUpColor: upColor,
                    wickDownColor: downColor,
                    borderVisible: true,
                    wickVisible: true,
                    fillOpacity: 0,
                });
            } else if (chartType === 'stepLine') {
                series = chart.addLineSeries({
                    color: lineColor,
                    lineType: LightweightCharts.LineType.WithSteps,
                });
            } else if (chartType === 'columns') {
                series = chart.addHistogramSeries({
                    color: upColor,
                    base: 0,
                    borderColor: downColor,
                });
            } else if (chartType === 'highLow') {
                series = chart.addCandlestickSeries({
                    upColor: '#0000FF',
                    downColor: '#0000FF',
                    borderUpColor: '#0000FF',
                    borderDownColor: '#0000FF',
                    wickUpColor: '#0000FF',
                    wickDownColor: '#0000FF',
                });
            }
        };








    // Load shape coordinates onto the overlay canvas
        const loadShapeCoordinates = () => {

            overlayCanvas.getObjects().forEach(obj => {
                const shapeData = obj.get('data');
                if (shapeData) {
                    const { startTime, startPrice, endTime, endPrice } = shapeData;


                    const adjustedStartTime = adjustFromUTC(startTime, timeZone);
                    const adjustedEndTime = adjustFromUTC(endTime, timeZone);

                    console.log('Loading shape data:', { startTime, startPrice, endTime, endPrice });
                    console.log('Adjusted times:', { adjustedStartTime, adjustedEndTime });

                    const startCoords = chartToCanvasCoords(adjustedStartTime, startPrice, interval);
                    const endCoords = chartToCanvasCoords(adjustedEndTime, endPrice, interval);

                    console.log('Converted coordinates:', { startCoords, endCoords });

                    if (startCoords && endCoords) {
                        if (obj.type === 'rect') {
                            obj.set({
                                left: startCoords.x,
                                top: startCoords.y,
                                width: Math.abs(endCoords.x - startCoords.x),
                                height: Math.abs(endCoords.y - startCoords.y),
                            });
                        } else if (obj.type === 'line') {
                            obj.set({
                                x1: startCoords.x,
                                y1: startCoords.y,
                                x2: endCoords.x,
                                y2: endCoords.y,
                            });
                        }
                        obj.setCoords();
                        overlayCanvas.renderAll();
                    }
                }
            });
        };


        // Function to add drawings to the canvas using the complex logic
        const addDrawingToCanvas = (type, coordinates) => {
            const shapeData = {
                startTime: coordinates.startTime,
                startPrice: coordinates.startPrice,
                endTime: coordinates.endTime,
                endPrice: coordinates.endPrice
            };

            if (type === 'rect') {
                const rect = new fabric.Rect({
                    left: coordinates.startTime,  // Use the actual coordinates directly
                    top: coordinates.startPrice,
                    width: coordinates.endTime - coordinates.startTime,
                    height: coordinates.endPrice - coordinates.startPrice,
                    fill: 'rgba(255,0,0,0.3)', // Customize the fill color as needed
                    selectable: false,
                    data: shapeData
                });
                overlayCanvas.add(rect);
            } else if (type === 'line') {
                const line = new fabric.Line([coordinates.startTime, coordinates.startPrice, coordinates.endTime, coordinates.endPrice], {
                    stroke: 'red', // Customize the stroke color as needed
                    strokeWidth: 5,
                    selectable: false,
                    data: shapeData
                });
                overlayCanvas.add(line);
            }
            overlayCanvas.renderAll();
        };

    // Adjust from UTC to the specified timezone
        const adjustFromUTC = (utcTime, timeZone) => {
            const date = new Date(utcTime * 1000); // Create a date object from the provided UTC time
            const tzOffset = getOffsetInSeconds(date, timeZone); // Calculate the offset for the given time zone
            return utcTime + tzOffset; // Add the offset to get the time in the specified time zone
        };

    // Get the timezone offset in seconds
        const getOffsetInSeconds = (date, timeZone) => {
            const tzDate = new Intl.DateTimeFormat('en-US', {
                timeZone,
                hour12: false,
                timeZoneName: 'longOffset',
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
            }).formatToParts(date);

            const offsetParts = tzDate.find(part => part.type === 'timeZoneName').value;
            const match = offsetParts.match(/GMT([+-]\d{1,2})(\d{2})?/);
            if (!match) return 0;

            const hours = parseInt(match[1], 10);
            const minutes = parseInt(match[2], 10);
            return (hours * 3600)
        };






    // Update the positions of the shapes
        const updateShapePositions = () => {
            loadShapeCoordinates();
        };


    // Get the user's local timezone
        const getUserLocalTimeZone = () => {
            const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
            return timeZone;
    };

    // Convert chart time and price to canvas coordinates
        const chartToCanvasCoords = (time, price, interval) => {
            const timeScale = chart.timeScale();
            const priceScale = series.priceScale();
            // Ensure the timeScale and series are ready
            if (!timeScale || !priceScale || time === null || price === null) {
                console.warn('Time scale or price scale not ready or invalid time/price values');
                return null;
            }

            // Find the nearest valid time for the given interval

            const adjustedTime = floorToNearestInterval(time, interval);



            let x = timeScale.timeToCoordinate(adjustedTime);
            let y = series.priceToCoordinate(price);

            console.log('Converting time and price to canvas coordinates:', { time, price, x, y });

            // Check if x and y are valid numbers before returning
            if (isNaN(x) || isNaN(y) || y === null || y === undefined || y == 0) {
                console.warn('Invalid coordinates:', { x, y });
                return null;
            }

            // Ensure y is within the canvas bounds
            const canvasHeight = overlayCanvas.getHeight();
            if (y < 0 || y > canvasHeight) {
                y = Math.max(0, Math.min(y, canvasHeight));
            }

            return { x, y };
        };




        chart.timeScale().subscribeVisibleTimeRangeChange(updateShapePositions);
        window.addEventListener('mousemove', updateShapePositions);

        window.addEventListener('resize', syncOverlayCanvasSize);




        // Ensure normal mode is set by default
        document.body.classList.add('normal-mode');
        overlayCanvas.isDrawingMode = false;
        overlayCanvasElement.style.pointerEvents = 'none';



    });

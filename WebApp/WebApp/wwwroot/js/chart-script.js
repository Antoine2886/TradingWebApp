let series = null;
let overlayCanvas = null;

// The name of the function is a good description of that it does. This is by far the ugliest code in this project but it is what it is.


document.addEventListener("DOMContentLoaded", function () {

    const chartContainer = document.getElementById('chartContainer');
    const chartCanvas = document.getElementById('chartCanvas');
    const overlayCanvasElement = document.getElementById('overlayCanvas');
    const drawingModeButton = document.getElementById('toggleDrawingMode');

    // Initialize LightweightCharts
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
    // Initialize series
    series = chart.addLineSeries();
    series.applyOptions({
        priceFormat: {
            type: 'price',
            precision: 5,  // Number of decimal places
            minMove: 0.00001,  // Minimum price movement
        },
    });
    // Initialize Fabric.js overlay canvas for drawing
    overlayCanvas = new fabric.Canvas('overlayCanvas', {
        selection: false,
    });
    let currentTool = null;

    // Function to clear drawing events from the canvas
    const clearCanvasEvents = () => {
        overlayCanvas.isDrawingMode = false;
        overlayCanvas.off('mouse:down');
        overlayCanvas.off('mouse:move');
        overlayCanvas.off('mouse:up');
    };
    // Function to start drawing based on selected tool
    const startDrawing = (tool) => {
        clearCanvasEvents();
        overlayCanvas.isDrawingMode = false;

        if (tool === 'rectangle') {
            let rect, isDrawing;
            overlayCanvas.on('mouse:down', function (o) {
                if (currentTool !== 'rectangle') return;
                isDrawing = true;
                let pointer = overlayCanvas.getPointer(o.e);
                let origX = pointer.x;
                let origY = pointer.y;

                rect = new fabric.Rect({
                    left: origX,
                    top: origY,
                    originX: 'left',
                    originY: 'top',
                    width: pointer.x - origX,
                    height: pointer.y - origY,
                    angle: 0,
                    fill: 'rgba(255,0,0,0.5)',
                    transparentCorners: false
                });
                overlayCanvas.add(rect);
            });

            overlayCanvas.on('mouse:move', function (o) {
                if (!isDrawing || currentTool !== 'rectangle') return;
                let pointer = overlayCanvas.getPointer(o.e);
                if (origX > pointer.x) {
                    rect.set({ left: Math.abs(pointer.x) });
                }
                if (origY > pointer.y) {
                    rect.set({ top: Math.abs(pointer.y) });
                }

                rect.set({ width: Math.abs(origX - pointer.x) });
                rect.set({ height: Math.abs(origY - pointer.y) });

                overlayCanvas.renderAll();
            });

            overlayCanvas.on('mouse:up', function (o) {
                if (!isDrawing || currentTool !== 'rectangle') return;
                isDrawing = false;
                rect.setCoords();
                if (!saveShapeCoordinates(rect)) {
                    overlayCanvas.remove(rect);
                }
            });
        } else if (tool === 'line') {
            let line, isDrawing;
            overlayCanvas.on('mouse:down', function (o) {
                if (currentTool !== 'line') return;
                isDrawing = true;
                let pointer = overlayCanvas.getPointer(o.e);
                let points = [pointer.x, pointer.y, pointer.x, pointer.y];

                line = new fabric.Line(points, {
                    strokeWidth: 5,
                    fill: 'red',
                    stroke: 'red',
                    originX: 'center',
                    originY: 'center',
                    selectable: false
                });
                overlayCanvas.add(line);
            });

            overlayCanvas.on('mouse:move', function (o) {
                if (!isDrawing || currentTool !== 'line') return;
                let pointer = overlayCanvas.getPointer(o.e);
                line.set({ x2: pointer.x, y2: pointer.y });
                overlayCanvas.renderAll();
            });

            overlayCanvas.on('mouse:up', function () {
                if (!isDrawing || currentTool !== 'line') return;
                isDrawing = false;
                line.setCoords();
                if (!saveShapeCoordinates(line)) {
                    overlayCanvas.remove(line);
                }
            });
        } else if (tool === 'select') {
            overlayCanvas.isDrawingMode = false;
            clearCanvasEvents();
            overlayCanvas.selection = true;
            overlayCanvas.forEachObject(obj => obj.selectable = true);
        } else {
            overlayCanvas.selection = false;
            overlayCanvas.forEachObject(obj => obj.selectable = false);
        }
    };

    // Event listener for drawing tool selection
    document.getElementById('drawingToolSelect').addEventListener('change', (event) => {
        const tool = event.target.value;
        currentTool = tool;
        startDrawing(tool);
    });

    // Event listener for deleting selected objects with backspace
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Backspace' || event.key === 'Delete') {
            const activeObject = overlayCanvas.getActiveObject();
            if (activeObject) {
                overlayCanvas.remove(activeObject);
                overlayCanvas.discardActiveObject();
                overlayCanvas.renderAll();
            }
        }
    });


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
    // Function to sync overlay canvas size with chart container
    const syncOverlayCanvasSize = () => {
        overlayCanvasElement.width = chartContainer.clientWidth;
        overlayCanvasElement.height = chartContainer.clientHeight;
        overlayCanvas.setWidth(chartContainer.clientWidth);
        overlayCanvas.setHeight(chartContainer.clientHeight);
        overlayCanvas.renderAll();
    };
    // Fetch symbols from API and populate the symbol select dropdown
    const fetchSymbols = async () => {
        try {
            const response = await fetch('/api/Trade/symbols');
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            const symbols = await response.json();

            const symbolSelect = document.getElementById('symbolSelect');
            symbolSelect.innerHTML = ''; // Clear existing options

            symbols.forEach(symbol => {
                const option = document.createElement('option');
                option.value = symbol;
                option.textContent = symbol;
                symbolSelect.appendChild(option);
            });

            // Optionally, trigger a change event to load data for the first symbol
            symbolSelect.dispatchEvent(new Event('change'));
        } catch (error) {
            console.error('Error fetching symbols:', error);
        }
        const symbol = document.getElementById('symbolSelect').value;
        const interval = document.getElementById('intervalSelect').value;
        fetchBatchData(symbol, interval);
        fetchAndDisplaySettings();

    };

    syncOverlayCanvasSize();


    overlayCanvas.renderAll();


    const token = '@ViewBag.Token';
    const userId = '@ViewBag.UserId';

    window.timeToTzAndAdjust = (originalTime, timeZone) => {
        // Get the current New York time
        const interval = document.getElementById('intervalSelect').value;

        const newYorkTime = new Date(new Date().toLocaleString('en-US', { timeZone: 'America/New_York' }));

            // Handle intraday intervals
            const date = new Date(new Date(originalTime).toLocaleString('en-US', { timeZone }));
            const adjustedDate = new Date(date.getTime() - 4 * 60 * 60 * 1000); // Subtract 4 hours
            return Math.floor(adjustedDate.getTime() / 1000); // Return the adjusted date in seconds

    };

    const timeToTzAndAdjust2 = (originalTime, timeZone) => {
        const date = new Date(originalTime * 1000); // originalTime is in seconds
        const tzDate = new Intl.DateTimeFormat('en-US', {
            timeZone,
            hour12: false,
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
        }).formatToParts(date);

        let year, month, day, hour, minute, second;
        tzDate.forEach(({ type, value }) => {
            if (type === 'year') year = value;
            if (type === 'month') month = value;
            if (type === 'day') day = value;
            if (type === 'hour') hour = value;
            if (type === 'minute') minute = value;
            if (type === 'second') second = value;
        });

        const adjustedDate = new Date(`${year}-${month}-${day}T${hour}:${minute}:${second}Z`);
        return Math.floor(adjustedDate.getTime() / 1000); // Return time in seconds
    };



    const fetchData = () => {
        const symbol = document.getElementById('symbolSelect').value;
        const interval = document.getElementById('intervalSelect').value;
        const chartType = document.getElementById('chartTypeSelect').value;
        const timeZone = document.getElementById('timeZoneSelect').value;

        fetch(`/api/Trade/historical?symbol=${symbol}&interval=${interval}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'X-UserId': userId
            }
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
    const fetchLatestData = () => {
        //const symbol = document.getElementById('symbolSelect').value;
        //const interval = document.getElementById('intervalSelect').value;
        //const chartType = document.getElementById('chartTypeSelect').value;
        //const timeZone = document.getElementById('timeZoneSelect').value;

        //fetch(`/api/Trade/latest?symbol=${symbol}&interval=${interval}`, {
        //    headers: {
        //        'Authorization': `Bearer ${token}`,
        //        'X-UserId': userId
        //    }
        //})
        //    .then(response => response.json())
        //    .then(data => {
        //        let formattedData;

        //        if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns' || chartType === 'baseline') {
        //            formattedData = {
        //                time: timeToTzAndAdjust(data.adjustedTimestampUnix, timeZone),
        //                value: data.close,
        //            };
        //        } else if (chartType === 'candlestick' || chartType === 'bar' || chartType === 'hollowCandle' || chartType === 'highLow') {
        //            formattedData = {
        //                time: timeToTzAndAdjust(data.adjustedTimestampUnix, timeZone),
        //                open: data.open,
        //                high: data.high,
        //                low: data.low,
        //                close: data.close,
        //            };
        //        }

        //        const seriesData = series.data();
        //        if (seriesData.length > 0) {
        //            const lastDataPoint = seriesData[seriesData.length - 1];

        //            // Check if the latest data point is newer than the last data point
        //            if (formattedData.time <= lastDataPoint.time) {
        //                // If the latest data point is not newer, do nothing
        //                return;
        //            }

        //            // Check if the latest data point matches the last data point's time
        //            if (lastDataPoint.time === formattedData.time) {
        //                if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns' || chartType === 'baseline') {
        //                    lastDataPoint.value = formattedData.value;
        //                } else {
        //                    lastDataPoint.open = formattedData.open;
        //                    lastDataPoint.high = formattedData.high;
        //                    lastDataPoint.low = formattedData.low;
        //                    lastDataPoint.close = formattedData.close;
        //                }
        //                series.update(lastDataPoint);
        //            } else {
        //                // Add the new data point
        //                seriesData.push(formattedData);
        //                series.setData(seriesData);
        //            }
        //        } else {
        //            // Add the new data point if no previous data
        //            series.setData([formattedData]);
        //        }
        //        fetchBidAsk(symbol);

        //    })
        //    .catch(error => console.error('Error fetching latest data:', error));
    };
    const loadChartSettingsById = (id = null) => {
        const url = id ? `/api/Trade/load-settings?id=${id}` : `/api/Trade/load-settings`;
        return fetch(url)
            .then(response => response.json())
            .then(settings => {
                if (settings) {
                    document.getElementById('intervalSelect').value = settings.interval;
                    document.getElementById('chartTypeSelect').value = settings.chartType;
                    document.getElementById('timeZoneSelect').value = settings.timeZone;
                    document.getElementById('themeSelect').value = settings.theme;
                    document.getElementById('lineColor').value = settings.lineColor;
                    document.getElementById('upColor').value = settings.upColor;
                    document.getElementById('downColor').value = settings.downColor;

                    overlayCanvas.clear();



                    // Fetch data based on the loaded settings

                    updateChartTheme();
                    updateChartType();
                    
                    updateSeriesColor(settings.chartType);
                    fetchData();



                  //  fetchLatestData();

                    // Load drawings
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

    const fetchAndDisplaySettings = () => {
        fetch('/api/Trade/all-settings')
            .then(response => response.json())
            .then(settings => {
                const settingsList = document.getElementById('settingsList');
                settingsList.innerHTML = '';

                if (settings.length === 0) {
                    settingsList.innerHTML = '<p>No saved settings found.</p>';
                    return;
                }

                settings.forEach((setting, index) => {
                    const settingDiv = document.createElement('div');
                    settingDiv.innerHTML = `
                    <input type="radio" id="setting${index}" name="selectedSetting" value="${setting.id}">
                    <label for="setting${index}">${setting.symbol} - ${setting.interval}</label>
                `;
                    settingsList.appendChild(settingDiv);
                });

                const loadButton = document.createElement('button');
                loadButton.textContent = 'Load Selected Setting';
                loadButton.addEventListener('click', () => {
                    const selectedSetting = document.querySelector('input[name="selectedSetting"]:checked');
                    if (selectedSetting) {
                        loadChartSettingsById(selectedSetting.value);
                    } else {
                        alert('Please select a setting to load.');
                    }
                });
                settingsList.appendChild(loadButton);
                fetchAndDisplaySettingsDefault();

            })
            .catch(error => console.error('Error fetching settings:', error));

    };



    const fetchAndDisplaySettingsDefault = () => {
        // Automatically load the first setting if available
        const settingsList = document.getElementById('settingsList');

        if (settingsList.children.length > 0) {
            loadChartSettingsById();
        }
    }


    const updateChartTimeScale = () => {
        const interval = document.getElementById('intervalSelect').value;
        const timeZone = document.getElementById('timeZoneSelect').value; // Get the selected time zone

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

    // SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/tradeHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveTradeUpdate", (message) => {
        console.log("Received trade update:", message); // Log the received message for debugging
        const data = JSON.parse(message);
        const { symbol, price, bid, ask, adjustedTimestampUnix } = data;
        const currentSymbol = document.getElementById('symbolSelect').value;
        if (symbol !== currentSymbol) {
            return; // Ignore updates for different symbols
        }

        const timeZone = document.getElementById('timeZoneSelect').value;
        const interval = document.getElementById('intervalSelect').value;

        let date = new Date().toISOString()
        let realTimeStamp = Date.parse(date);
        realTimeStamp = realTimeStamp / 1000;
        realTimeStamp = parseInt(realTimeStamp);
        const candleTime = adjustFromUTC2(realTimeStamp, timeZone, interval);

        // Floor the time to the nearest interval
       // const candleTime = floorToNearestInterval(adjustedTime, interval);



        const seriesData = series.data();
        const lastCandle = seriesData[seriesData.length - 1];



        if (lastCandle && lastCandle.time === candleTime) {
            if (series.seriesType() === 'Candlestick' || series.seriesType() === 'Bar') {
                lastCandle.close = price;
                lastCandle.high = Math.max(lastCandle.high, price);
                lastCandle.low = Math.min(lastCandle.low, price);
                lastCandle.open = lastCandle.open; // Assuming open remains the same

                series.update(lastCandle); // Update the last candle
                console.warn(lastCandle);
            } else {
                lastCandle.value = price;
                series.update(lastCandle); // Update the last point for other series types
                console.warn(lastCandle);

            }
        } else {
            if (series.seriesType() === 'Candlestick' || series.seriesType() === 'Bar') {
                const newCandle = {
                    time: candleTime,
                    open: price,
                    high: price,
                    low: price,
                    close: price
                };
                if (['1D', '1W', '1M'].includes(interval)) {
                    if (lastCandle) {
                        lastCandle.close = price;
                        lastCandle.high = Math.max(lastCandle.high, price);
                        lastCandle.low = Math.min(lastCandle.low, price);
                        series.update(lastCandle); // Update the last candle
                    }
                }
                else {
                    if (newCandle.time != null && newCandle.open != null && newCandle.high != null && newCandle.low != null && newCandle.close != null) {
                        seriesData.push(newCandle);
                        series.setData(seriesData);
                        console.warn(newCandle);

                    } else {
                        console.error("Invalid new candle data:", newCandle);
                    }
                }

            } else {
                if (price != null && candleTime != null) {
                    const newPoint = {
                        value: price,
                        time: candleTime
                    };
                    if (['1D', '1W', '1M'].includes(interval)) {
                        if (lastCandle) {
                            lastCandle.value = price;
                            series.update(lastCandle); // Update the last point
                        }
                    } else {
                        seriesData.push(newPoint);
                        console.warn(newPoint);
                        series.setData(seriesData);
                    }
                } else {
                    console.error("Invalid new point data:", { value: price, time: candleTime });
                }

            }
            series.applyOptions({
                priceFormat: {
                    type: 'price',
                    precision: 5,  // Number of decimal places
                    minMove: 0.00001,  // Minimum price movement
                },
            })
        }

        // Update bid and ask prices if valid
        if (data.bid > 0) {
            document.getElementById('bidPrice').textContent = `Bid: ${data.bid}`;
        }
        if (data.ask > 0) {
            document.getElementById('askPrice').textContent = `Ask: ${data.ask}`;
        }
    });
    connection.on("ReceiveOrderUpdate", (orders) => {
        updateOrders(orders);
    });

    const intervalToSeconds = (interval) => {
        switch (interval) {
            case '1min':
                return 60;
            case '5min':
                return 300;
            case '15min':
                return 900;
            case '30min':
                return 1800;
            case '1h':
                return 3600;
            default:
                return 60;
        }
    };
    
    connection.on("ReceiveBalanceUpdate", (balanceData) => {
        document.getElementById('totalBalance').textContent = `Total Balance: ${balanceData.totalBalance}`;
        document.getElementById('availableBalance').textContent = `Available Balance: ${balanceData.availableBalance}`;
    });

    connection.on("NewCandle", (message) => {
        // A verifier
        const data = JSON.parse(message);

        const symbol = data.Symbol;
        const timestamp = data.TimeStamp;

        const currentSymbol = document.getElementById('symbolSelect').value;
        const currentInterval = document.getElementById('intervalSelect').value;

        // Check if the symbol and interval match the current selection
        if (symbol !== currentSymbol || data.Interval !== currentInterval) {
            return; // Ignore updates for different symbols or intervals
        }

        const timeZone = document.getElementById('timeZoneSelect').value;


        let adjustedTime = adjustFromUTC2(timestamp, timeZone, currentInterval);




        const newCandle = {
            time: adjustedTime,
            open: data.Open,
            high: data.High,
            low: data.Low,
            close: data.Close
        };

        const chartType = document.getElementById('chartTypeSelect').value;
        const seriesData = series.data();
        const lastCandle = seriesData[seriesData.length - 1];

        if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns') {
            // For line, area, histogram, stepLine, and columns charts
            const newPoint = {
                value: newCandle.close,
                time: newCandle.time
            };

            if (lastCandle && lastCandle.time === newCandle.time) {
                lastCandle.value = newCandle.close;
                series.update(lastCandle);
            } else {
                if (newPoint.time != null && newPoint.value != null) {
                    seriesData.push(newPoint);
                    series.setData(seriesData);
                } else {
                    console.error("Invalid new point data:", newPoint);
                }
            }
        } else if (chartType === 'candlestick' || chartType === 'bar' || chartType === 'hollowCandle' || chartType === 'highLow') {
            // For candlestick, bar, hollowCandle, and highLow charts
            if (lastCandle && lastCandle.time === newCandle.time) {
                lastCandle.open = newCandle.open;
                lastCandle.high = newCandle.high;
                lastCandle.low = newCandle.low;
                lastCandle.close = newCandle.close;
                series.update(lastCandle);
            } else {
                if (newCandle.time != null && newCandle.open != null && newCandle.high != null && newCandle.low != null && newCandle.close != null) {
                    seriesData.push(newCandle);
                    series.setData(seriesData);
                } else {
                    console.error("Invalid new candle data:", newCandle);
                }
            }
        }

    });

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    }

    connection.onclose(async () => {
        await start();
    });
   
    const updateChartTheme = () => {
        const theme = document.getElementById('themeSelect').value;
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

    const updateSeriesColor = (chartType) => {
        if (chartType === 'line' || chartType === 'area' || chartType === 'histogram' || chartType === 'stepLine' || chartType === 'columns') {
            const lineColor = document.getElementById('lineColor').value;
            series.applyOptions({
                color: lineColor,
            });
        } else if (chartType === 'candlestick' || chartType === 'bar' || chartType === 'hollowCandle' || chartType === 'highLow') {
            const upColor = document.getElementById('upColor').value;
            const downColor = document.getElementById('downColor').value;
            series.applyOptions({
                upColor: upColor,
                downColor: downColor,
                borderUpColor: upColor,
                borderDownColor: downColor,
                wickUpColor: upColor,
                wickDownColor: downColor,
            });
        } else if (chartType === 'columns') {
            const upColor = document.getElementById('upColor').value;
            const downColor = document.getElementById('downColor').value;
            series.applyOptions({
                color: upColor,
                base: 0, // you can customize the base value
                borderColor: downColor,
            });
        } else if (chartType === 'baseline') {
            const upColor = document.getElementById('upColor').value;
            const downColor = document.getElementById('downColor').value;
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


    document.getElementById('loadSettingsButton').addEventListener('click', fetchAndDisplaySettings);

    document.getElementById('symbolSelect').addEventListener('change', () => {
        fetchData();
        fetchLatestData();
        updateChartTimeScale();


    });
    const updateChartType = () => {
        const chartType = document.getElementById('chartTypeSelect').value;
        const lineColor = document.getElementById('lineColor').value;
        const upColor = document.getElementById('upColor').value;
        const downColor = document.getElementById('downColor').value;

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

    document.getElementById('intervalSelect').addEventListener('change', () => {
        fetchData();
        fetchLatestData();

    });
        document.getElementById('chartTypeSelect').addEventListener('change', () => {
            updateChartType();
            fetchData();
            fetchLatestData();
        });
    document.getElementById('themeSelect').addEventListener('change', updateChartTheme);
    document.getElementById('lineColor').addEventListener('input', () => updateSeriesColor(document.getElementById('chartTypeSelect').value));
    document.getElementById('upColor').addEventListener('input', () => updateSeriesColor(document.getElementById('chartTypeSelect').value));
    document.getElementById('downColor').addEventListener('input', () => updateSeriesColor(document.getElementById('chartTypeSelect').value));
    document.getElementById('timeZoneSelect').addEventListener('change', () => {
        fetchData();
        fetchLatestData();
        updateShapePositions();
    });




    //const loadRectangleCoordinates = () => {
    //    if (!initialTopLeftChartCoords || !initialBottomRightChartCoords) {
    //        console.warn('Rectangle coordinates not set');
    //        return;
    //    }

    //    const topLeftTime = Date.parse(initialTopLeftChartCoords.time) / 1000;
    //    const bottomRightTime = Date.parse(initialBottomRightChartCoords.time) / 1000;
    //    const interval = document.getElementById('intervalSelect').value;

    //    console.log('Loading Rectangle Coordinates:', {
    //        topLeft: initialTopLeftChartCoords,
    //        bottomRight: initialBottomRightChartCoords,
    //        topLeftTime,
    //        bottomRightTime
    //    });

    //    const topLeftCanvasCoords = chartToCanvasCoords(topLeftTime, initialTopLeftChartCoords.price, interval);
    //    const bottomRightCanvasCoords = chartToCanvasCoords(bottomRightTime, initialBottomRightChartCoords.price, interval);

    //    console.log('Converted Canvas Coordinates:', {
    //        topLeft: topLeftCanvasCoords,
    //        bottomRight: bottomRightCanvasCoords
    //    });

    //    if (!topLeftCanvasCoords || !bottomRightCanvasCoords) {
    //        console.warn('Invalid canvas coordinates:', { topLeftCanvasCoords, bottomRightCanvasCoords });
    //        return;
    //    }

    //    rect.set({
    //        left: topLeftCanvasCoords.x,
    //        top: topLeftCanvasCoords.y,
    //        width: Math.abs(bottomRightCanvasCoords.x - topLeftCanvasCoords.x),
    //        height: Math.abs(bottomRightCanvasCoords.y - topLeftCanvasCoords.y)
    //    });

    //    overlayCanvas.renderAll();
    //    console.log('Rectangle position updated on canvas');
    //};

    const findNearestValidTime = (time, interval) => {
        // Convert time to a Date object
        const date = new Date(time * 1000);

        // Adjust date to the nearest valid time for the given interval
        if (interval === '1min') {
            // No adjustment needed for 1 minute interval
        } else if (interval === '3min') {
            const minutes = date.getUTCMinutes();
            const adjustedMinutes = Math.floor(minutes / 3) * 3;
            date.setUTCMinutes(adjustedMinutes, 0, 0);
        } else if (interval === '5min') {
            const minutes = date.getUTCMinutes();
            const adjustedMinutes = Math.floor(minutes / 5) * 5;
            date.setUTCMinutes(adjustedMinutes, 0, 0);
        } else if (interval === '15min') {
            const minutes = date.getUTCMinutes();
            const adjustedMinutes = Math.floor(minutes / 15) * 15;
            date.setUTCMinutes(adjustedMinutes, 0, 0);
        } else if (interval === '30min') {
            const minutes = date.getUTCMinutes();
            const adjustedMinutes = Math.floor(minutes / 30) * 30;
            date.setUTCMinutes(adjustedMinutes, 0, 0);
        } else if (interval === '1h') {
            date.setUTCMinutes(0, 0, 0);
        } else if (interval === '2h') {
            const hours = date.getUTCHours();
            const adjustedHours = Math.floor(hours / 2) * 2;
            date.setUTCHours(adjustedHours, 0, 0, 0);
        } else if (interval === '4h') {
            const hours = date.getUTCHours();
            const adjustedHours = Math.floor(hours / 4) * 4;
            date.setUTCHours(adjustedHours, 0, 0, 0);
        } else if (interval === '8h') {
            const hours = date.getUTCHours();
            const adjustedHours = Math.floor(hours / 8) * 8;
            date.setUTCHours(adjustedHours, 0, 0, 0);
        } else if (interval === '1D') {
            date.setUTCHours(0, 0, 0, 0);
        } else if (interval === '1W') {
            const day = date.getUTCDay();
            const diff = date.getUTCDate() - day + (day === 0 ? -6 : 1); // adjust when day is sunday
            date.setUTCDate(diff);
            date.setUTCHours(0, 0, 0, 0);
        } else if (interval === '1M') {
            date.setUTCDate(1);
            date.setUTCHours(0, 0, 0, 0);
        }

        return date.getTime() / 1000;
    };



    const loadShapeCoordinates = () => {
        const timeZone = document.getElementById('timeZoneSelect').value;

        overlayCanvas.getObjects().forEach(obj => {
            const shapeData = obj.get('data');
            if (shapeData) {
                const { startTime, startPrice, endTime, endPrice } = shapeData;
                const interval = document.getElementById('intervalSelect').value;


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
    document.getElementById('saveButton').addEventListener('click', () => {
        saveChartSettings(false);
        setTimeout(fetchAndDisplaySettings, 50);
    });
    // Save New button click handler
    document.getElementById('saveNewButton').addEventListener('click', () => {
        saveNewChartSettings(true);
        setTimeout(fetchAndDisplaySettings, 50);

    });

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


    const adjustFromUTC = (utcTime, timeZone) => {
        const date = new Date(utcTime * 1000); // Create a date object from the provided UTC time
        const tzOffset = getOffsetInSeconds(date, timeZone); // Calculate the offset for the given time zone
        return utcTime + tzOffset; // Add the offset to get the time in the specified time zone
    };



    const saveShapeCoordinates = (shape) => {
        const shapeType = shape.get('type');
        let shapeData;

        const currentTimeZone = document.getElementById('timeZoneSelect').value;

        if (shapeType === 'rect') {
            const topLeftCoords = canvasToChartCoords(shape.left, shape.top);
            const bottomRightCoords = canvasToChartCoords(shape.left + shape.width, shape.top + shape.height);

            if (!topLeftCoords.time || !topLeftCoords.price || !bottomRightCoords.time || !bottomRightCoords.price) {
                return false; // Coordinates are invalid, return false to indicate failure
            }

            shapeData = {
                startTime: convertToUTC(topLeftCoords.time, currentTimeZone),
                startPrice: topLeftCoords.price,
                endTime: convertToUTC(bottomRightCoords.time, currentTimeZone),
                endPrice: bottomRightCoords.price
            };

            console.log('Saving rectangle coordinates:', shapeData);
        } else if (shapeType === 'line') {
            const startCoords = canvasToChartCoords(shape.x1, shape.y1);
            const endCoords = canvasToChartCoords(shape.x2, shape.y2);
            if (!startCoords.time || !startCoords.price || !endCoords.time || !endCoords.price) {
                return false; // Coordinates are invalid, return false to indicate failure
            }
            shapeData = {
                startTime: convertToUTC(startCoords.time, currentTimeZone),
                startPrice: startCoords.price,
                endTime: convertToUTC(endCoords.time, currentTimeZone),
                endPrice: endCoords.price
            };

            console.log('Saving line coordinates:', shapeData);
        }

        shape.set('data', shapeData);
        return true;
    };


    const convertToUTC = (time, timeZone) => {
        const date = new Date(time * 1000); // Create a date object from the provided time
        const tzOffset = getOffsetInSeconds(date, timeZone); // Calculate the offset for the given time zone
        return time - tzOffset; // Subtract the offset to get UTC time
    };

    const convertToUTC2 = (time, timeZone) => {
        const date = new Date(time * 1000); // Create a date object from the provided time
        const tzOffset = getOffsetInSeconds2(date, timeZone); // Calculate the offset for the given time zone
        return time - tzOffset; // Subtract the offset to get UTC time
    };

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







    const updateShapePositions = () => {
        loadShapeCoordinates();
    };

    const canvasToChartCoords = (x, y) => {
        const timeScale = chart.timeScale();

        const time = timeScale.coordinateToTime(x);
        const price = series.coordinateToPrice(y);

        return { time, price };
    };


    const chartToCanvasCoords = (time, price, interval) => {
        const timeScale = chart.timeScale();
        const priceScale = series.priceScale();
        const timeZone = document.getElementById('timeZoneSelect').value;

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














    let initialTopLeftChartCoords = null;
    let initialBottomRightChartCoords = null;

    //const saveRectangleCoordinates = () => {
    //    const topLeftCoords = canvasToChartCoords(rect.left, rect.top);
    //    const bottomRightCoords = canvasToChartCoords(rect.left + rect.width, rect.top + rect.height);

    //    if (!topLeftCoords.time || !topLeftCoords.price || !bottomRightCoords.time || !bottomRightCoords.price) {
    //        console.warn('Invalid rectangle coordinates:', { topLeftCoords, bottomRightCoords });
    //        return;
    //    }

    //    initialTopLeftChartCoords = {
    //        time: new Date(topLeftCoords.time * 1000).toISOString(),
    //        price: topLeftCoords.price
    //    };
    //    initialBottomRightChartCoords = {
    //        time: new Date(bottomRightCoords.time * 1000).toISOString(),
    //        price: bottomRightCoords.price
    //    };

    //    console.log('Saved Rectangle Coordinates:', {
    //        topLeft: initialTopLeftChartCoords,
    //        bottomRight: initialBottomRightChartCoords
    //    });
    //};




    // Function to initialize and store the rectangle's time and price coordinates
    //const initializeRectangleCoordinates = () => {
    //    saveRectangleCoordinates();

    //    console.log('Initial Rectangle Chart Coordinates:', {
    //        topLeft: initialTopLeftChartCoords,
    //        bottomRight: initialBottomRightChartCoords
    //    });
    //};


    //const updateRectanglePosition = () => {
    //    if (!initialTopLeftChartCoords || !initialBottomRightChartCoords) {
    //        console.warn('Initial chart coordinates not set');
    //        return;
    //    }

    //    loadRectangleCoordinates();
    //};

    // Retry initialization if coordinates are null
    //const retryInitialization = () => {
    //    if (!initialTopLeftChartCoords || !initialBottomRightChartCoords) {
    //        console.warn('Retrying initialization of rectangle coordinates');
    //        initializeRectangleCoordinates();
    //        if (!initialTopLeftChartCoords || !initialBottomRightChartCoords) {
    //            setTimeout(retryInitialization, 1000); // Retry after 1 second
    //        }
    //    }
    //};

    // Subscribe to time scale changes to update rectangle position
    //chart.timeScale().subscribeVisibleTimeRangeChange(updateRectanglePosition);
   // window.addEventListener('mousemove', updateRectanglePosition);

    chart.timeScale().subscribeVisibleTimeRangeChange(updateShapePositions);
    window.addEventListener('mousemove', updateShapePositions);

    window.addEventListener('resize', syncOverlayCanvasSize);
    document.getElementById('timeZoneSelect').addEventListener('change', () => {
        loadShapeCoordinates();
    });

    const toggleDrawingMode = () => {
        document.body.classList.toggle('drawing-mode');
        if (document.body.classList.contains('drawing-mode')) {
            overlayCanvas.isDrawingMode = true;
            chartCanvas.style.pointerEvents = 'none';
            drawingModeButton.classList.add('drawing-mode-button');
            if (currentTool) startDrawing(currentTool);
        } else {
            overlayCanvas.isDrawingMode = false;
            chartCanvas.style.pointerEvents = 'auto';
            drawingModeButton.classList.remove('drawing-mode-button');
            clearCanvasEvents();
            startDrawing(currentTool); // Restart the current tool to restore functionality
        }
    };

    // Add event listener to the toggle button
    document.getElementById('toggleDrawingMode').addEventListener('click', toggleDrawingMode);

    // Ensure normal mode is set by default
    // Ensure normal mode is set by default
    document.body.classList.add('normal-mode');
    overlayCanvas.isDrawingMode = false;
    overlayCanvasElement.style.pointerEvents = 'none';
    fetchSymbols();
    fetchData();
    fetchLatestData();
    updateChartTheme();
    syncOverlayCanvasSize();

    start();
    //retryInitialization();

});
// Function to fetch batch data
const fetchBatchData = (symbol, interval) => {
    const url = `/api/Batch/batch?symbol=${encodeURIComponent(symbol)}&interval=${encodeURIComponent(interval)}`;

    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {

            // Handle data structure from API
            if  (data.bidAsk && data.orders) {


                // Update Bid and Ask prices
                if (data.bidAsk.bid > 0) {
                    document.getElementById('bidPrice').textContent = `Bid: ${data.bidAsk.bid}`;
                }
                if (data.bidAsk.ask > 0) {
                    document.getElementById('askPrice').textContent = `Ask: ${data.bidAsk.ask}`;
                }

                // Update Orders
                updateOrders(data.orders);

                // Update Balance
                document.getElementById('totalBalance').textContent = `Total Balance: ${data.balance.totalBalance}`;
                document.getElementById('availableBalance').textContent = `Available Balance: ${data.balance.availableBalance}`;
            } else {
                console.warn('Data structure from API does not match expected format');
            }
        })
        .catch(error => console.error('Error fetching batch data:', error));
};
document.addEventListener('DOMContentLoaded', () => {

    toggleOrderMode();

});


const fetchOrders = () => {
    fetch('/api/Trade/orders')
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                updateOrders(result.data);
            } else {
                console.error('No orders found:', result.message);
                alert('No orders found.');
            }
        })
        .catch(error => console.error('Error fetching orders data:', error));
};

const updateOrders = (orders) => {
    const pendingOrdersContainer = document.getElementById('pendingOrdersContainer');
    const openedPositionsContainer = document.getElementById('openedPositionsContainer');
    const completedOrdersContainer = document.getElementById('completedOrdersContainer');

    pendingOrdersContainer.innerHTML = '';
    openedPositionsContainer.innerHTML = '';
    completedOrdersContainer.innerHTML = '';

    // Sort orders by date
    orders.sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate));

    orders.forEach(order => {
        const orderElement = document.createElement('div');
        orderElement.className = 'order-item';
        orderElement.innerHTML = `
            <div>Date: ${order.orderDate}</div>
            <div>Symbol: ${order.symbol}</div>
            <div>Quantity: ${order.quantity}</div>
            <div>Price: ${order.price}</div>
            <div>Type: ${getOrderTypeString(order.orderType)}</div>
            <div>State: ${getOrderStateString(order.orderState)}</div>
            <div>Stop Loss: ${order.stopLoss}</div>
            <div>Take Profit: ${order.takeProfit}</div>
            ${order.orderState === 3 || order.orderState === 1 ? `<div>Profit/Loss: <span style="color: ${order.profitorLoss >= 0 ? 'green' : 'red'};">${order.profitorLoss}</span></div>` : ''}
            ${order.orderState === 1 ? `<div>Closed Price: ${order.closedPrice}</div>` : ''}
            ${order.orderState === 3 ? '<button onclick="closeOrder(this)" data-order-id="' + order.id + '">Close Order</button>' : ''}
        `;

        if (order.orderState === 0) { // Pending
            pendingOrdersContainer.appendChild(orderElement);
        } else if (order.orderState === 3) { // Opened
            openedPositionsContainer.appendChild(orderElement);
        } else if (order.orderState === 1) { // Completed
            completedOrdersContainer.appendChild(orderElement);
        }
    });

   // showOrders('pending'); // Show pending orders by default
};
const showOrders = (orderType) => {
    document.getElementById('pendingOrdersContainer').style.display = orderType === 'pending' ? 'block' : 'none';
    document.getElementById('openedPositionsContainer').style.display = orderType === 'opened' ? 'block' : 'none';
    document.getElementById('completedOrdersContainer').style.display = orderType === 'completed' ? 'block' : 'none';
};


const OrderType = {
    0: 'Buy',
    1: 'Short',
    2: 'BuyLimit',
    3: 'BuyStop',
    4: 'SellLimit',
    5: 'SellStop'
};

const OrderState = {
    0: 'Pending',
    1: 'Completed',
    2: 'Cancelled',
    3: 'Opened'
};

const getOrderTypeString = (type) => {
    return OrderType[type] || 'Unknown';
};

const getOrderStateString = (state) => {
    return OrderState[state] || 'Unknown';
};

const closeOrder = (button) => {
    const orderId = button.getAttribute('data-order-id');
    fetch('/api/Trade/execute-order', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ id: orderId })
    })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                alert('Order closed successfully.');
                fetchOrders(); // Refresh the orders list
            } else {
                alert('Failed to close order: ' + result.message);
            }
        })
        .catch(error => console.error('Error closing order:', error));
};





function toggleOrderMode() {
    const orderMode = document.getElementById('orderModeSelect').value;
    const marketOrderFields = document.getElementById('marketOrderFields');
    const pendingOrderFields = document.getElementById('pendingOrderFields');

    if (orderMode === 'market') {
        marketOrderFields.style.display = 'block';
        pendingOrderFields.style.display = 'none';
    } else {
        marketOrderFields.style.display = 'none';
        pendingOrderFields.style.display = 'block';
    }
    updateOrderFieldsWithCurrentPrice();
}
async function placeNewOrder() {
    const orderMode = document.getElementById('orderModeSelect').value;
    let orderData = {};

    if (orderMode === 'market') {
        const orderType = document.getElementById('marketOrderType').value;
        const quantity = parseFloat(document.getElementById('marketQuantity').value);
        const stopLoss = parseFloat(document.getElementById('marketStopLoss').value);
        const takeProfit = parseFloat(document.getElementById('marketTakeProfit').value);
        const symbol = document.getElementById('symbolSelect').value;
        orderData = {
            symbol,
            orderType,
            quantityInLots: quantity,
            stopLoss: isNaN(stopLoss) ? null : stopLoss,
            takeProfit: isNaN(takeProfit) ? null : takeProfit
        };
    } else {
        const orderType = document.getElementById('pendingOrderType').value;
        const symbol = document.getElementById('symbolSelect').value;
        const quantity = parseFloat(document.getElementById('pendingQuantity').value);
        const price = parseFloat(document.getElementById('pendingPrice').value);
        const stopLoss = parseFloat(document.getElementById('pendingStopLoss').value);
        const takeProfit = parseFloat(document.getElementById('pendingTakeProfit').value);

        orderData = {
            symbol,
            orderType,
            quantityInLots: quantity,
            price,
            stopLoss: isNaN(stopLoss) ? null : stopLoss,
            takeProfit: isNaN(takeProfit) ? null : takeProfit
        };
    }

    console.log("Order Data:", orderData); // Log the order data to debug
    try {
        const response = await fetch(`/api/Trade/place-order`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderData)
        });

        if (response.ok) {
            const result = await response.json();
            console.log('Order placed successfully');
            fetchOrders(); // Update the orders list
        } else {
            const result = await response.json();
            console.error('Failed to place order:', result);
            alert(`Failed to place order: ${result.title}\n${JSON.stringify(result.errors)}`);
        }
    } catch (error) {
        console.error('Error placing order:', error);
        alert('An error occurred while placing the order.');
    }
}

async function updateOrderFieldsWithCurrentPrice() {
    const { bidPrice, askPrice } = fetchCurrentBidAskPrices();

    if (!isNaN(bidPrice)) {
        document.getElementById('pendingPrice').value = bidPrice;
    }
    if (!isNaN(bidPrice)) {
        document.getElementById('marketStopLoss').value = bidPrice;
        document.getElementById('pendingStopLoss').value = bidPrice;
    }
    if (!isNaN(askPrice)) {
        document.getElementById('marketTakeProfit').value = bidPrice;
        document.getElementById('pendingTakeProfit').value = bidPrice;
    }
}



const getDrawingsFromCanvas = () => {
    const drawings = [];
    overlayCanvas.getObjects().forEach(obj => {
        const shapeData = obj.get('data');
        if (shapeData) {
            const drawing = {
                type: obj.type,
                coordinates: JSON.stringify(shapeData)
            };
            drawings.push(drawing);
        }
    });
    return drawings;
};

function fetchCurrentBidAskPrices() {
    const bidPriceText = document.getElementById('bidPrice').textContent;
    const askPriceText = document.getElementById('askPrice').textContent;

    const bidPrice = parseFloat(bidPriceText.replace('Bid: ', ''));
    const askPrice = parseFloat(askPriceText.replace('Ask: ', ''));

    return { bidPrice, askPrice };
}
const saveNewChartSettings = () => {
    const settings = {
        symbol: document.getElementById('symbolSelect').value,
        interval: document.getElementById('intervalSelect').value,
        chartType: document.getElementById('chartTypeSelect').value,
        timeZone: document.getElementById('timeZoneSelect').value,
        theme: document.getElementById('themeSelect').value,
        lineColor: document.getElementById('lineColor').value,
        upColor: document.getElementById('upColor').value,
        downColor: document.getElementById('downColor').value,
        drawings: getDrawingsFromCanvas() // Function to retrieve current drawings from the canvas
    };

    const formData = new FormData();
    formData.append('Settings', JSON.stringify(settings));
    formData.append('SaveNew', true);

    fetch('/api/Trade/save-new-settings', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(error => { throw new Error(error.message); });
            }
            console.log('Chart settings saved successfully.');
        })
        .catch(error => alert('Error saving chart settings: ' + error.message));
};

const saveChartSettings = () => {
    const selectedSetting = document.querySelector('input[name="selectedSetting"]:checked');
    const settings = {
        id: selectedSetting.value,
        symbol: document.getElementById('symbolSelect').value,
        interval: document.getElementById('intervalSelect').value,
        chartType: document.getElementById('chartTypeSelect').value,
        timeZone: document.getElementById('timeZoneSelect').value,
        theme: document.getElementById('themeSelect').value,
        lineColor: document.getElementById('lineColor').value,
        upColor: document.getElementById('upColor').value,
        downColor: document.getElementById('downColor').value,
        drawings: getDrawingsFromCanvas() // Function to retrieve current drawings from the canvas
    };

    const formData = new FormData();
    formData.append('Settings', JSON.stringify(settings));
    formData.append('SaveNew', false);

    fetch('/api/Trade/save-settings', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(error => { throw new Error(error.message); });
            }
            console.log('Chart settings saved successfully.');
        })
        .catch(error => alert('Error saving chart settings: ' + error.message));
};






// Save button click handler





document.getElementById('saveChartAsPostButton').addEventListener('click', function () {
    // Display the modal
    var modal = document.getElementById('postModal');
    modal.style.display = "block";

    // Close the modal when the user clicks the close button
    var span = document.getElementsByClassName('close')[0];
    span.onclick = function () {
        modal.style.display = "none";
    }

    // Close the modal when the user clicks anywhere outside of the modal
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
        }
    }
});

// Ensure the event listener is only added once
document.getElementById('createPostButton').addEventListener('click', function () {
    const title = document.getElementById('postTitle').value;
    const content = document.getElementById('postContent').value;
    const postType = document.getElementById('postType').value;

    const settings = {
        symbol: document.getElementById('symbolSelect').value,
        interval: document.getElementById('intervalSelect').value,
        chartType: document.getElementById('chartTypeSelect').value,
        timeZone: document.getElementById('timeZoneSelect').value,
        theme: document.getElementById('themeSelect').value,
        lineColor: document.getElementById('lineColor').value,
        upColor: document.getElementById('upColor').value,
        downColor: document.getElementById('downColor').value,
        drawings: getDrawingsFromCanvas() // Function to retrieve current drawings from the canvas
    };

    const postData = {
        Title: title,
        Content: content,
        IncludeChart: true,
        ChartSettingsJson: JSON.stringify(settings),
        PostType: postType
    };

    fetch('/api/PostApi/Create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(postData)
    })
        .then(response => {
        })
        .then(data => {
            console.log('Success:', data);
            alert('Post created successfully!'); // Display a success message
        })
        .catch(error => {
            console.error('Error:', error.message);
            alert('An error occurred while creating the post.'); // Display an error message
        });
});








async function executeOrder(order) {
    try {
        const response = await fetch('/api/trade/execute-order', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(order)
        });
        const result = await response.json();
        if (result.success) {
            console.log('Order executed successfully');
            //fetchOrders(); // Refresh orders list
        } else {
            console.error('Failed to execute order:', result.message);
            alert(result.message);
        }
    } catch (error) {
        console.error('Error executing order:', error);
        alert('An error occurred while executing the order.');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    toggleOrderMode();
});
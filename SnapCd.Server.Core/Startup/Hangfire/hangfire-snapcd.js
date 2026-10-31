// Hangfire draws its graphs with Chart.js and keeps the palette in a variable closed over by its
// own module, so the colours cannot be set from configuration or from CSS. The datasets are
// recoloured on the live chart instances instead, matched by the label Hangfire gives each one.
(function () {
    var INK = '#12171D';

    var FILL = {
        succeeded: { background: 'rgba(0, 179, 104, 0.35)', border: '#00B368' },
        failed: { background: 'rgba(219, 74, 63, 0.35)', border: '#DB4A3F' },
        deleted: { background: 'rgba(138, 150, 160, 0.35)', border: '#8B96A0' }
    };

    // Hangfire labels the datasets from its own localised strings, so the kind is read from the
    // label's own text rather than from an index that could change with the dataset order.
    function kindOf(label) {
        var text = (label || '').toLowerCase();
        if (text.indexOf('succeed') > -1) return 'succeeded';
        if (text.indexOf('fail') > -1) return 'failed';
        if (text.indexOf('delet') > -1) return 'deleted';
        return null;
    }

    function recolour() {
        if (!window.Chart || !Chart.instances) return false;

        var touched = false;

        Object.keys(Chart.instances).forEach(function (key) {
            var chart = Chart.instances[key];
            if (!chart || !chart.data || !chart.data.datasets) return;

            chart.data.datasets.forEach(function (dataset) {
                var kind = kindOf(dataset.label);
                if (!kind) return;

                dataset.backgroundColor = FILL[kind].background;
                dataset.borderColor = FILL[kind].border;
                touched = true;
            });

            if (touched) chart.update();
        });

        return touched;
    }

    // The charts are built after this script runs, and the realtime one is rebuilt as it polls,
    // so the colours are reapplied for a short while rather than once.
    function start() {
        var attempts = 0;

        var timer = setInterval(function () {
            attempts++;
            if (recolour() || attempts > 40) clearInterval(timer);
        }, 250);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();

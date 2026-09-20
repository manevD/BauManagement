window.chefCalendar = window.chefCalendar || {};

window.chefCalendar = {

    getDropMinute: function(clientX, clientY, timelineElement) {
        let timeline = timelineElement;

        // Доколку не е пронајдена референцата, го наоѓаме `.timeline` елементот директно под глувчето
        if (!timeline || typeof timeline.getBoundingClientRect !== "function") {
            const el = document.elementFromPoint(clientX, clientY);
            timeline = el ? el.closest(".timeline") : document.querySelector(".timeline");
        }

        if (!timeline) return 0;

        const rect = timeline.getBoundingClientRect();
        if (rect.width <= 0) return 0;

        let x = clientX - rect.left;
        x = Math.max(0, Math.min(x, rect.width));

        let minute = Math.round((x / rect.width) * 1440);
        minute = Math.round(minute / 15) * 15;

        return Math.max(0, Math.min(minute, 1425));
    },

    startResize: function(key, side, startMinute, endMinute, dotNet) {
        const element = document.querySelector(`[data-assignment="${key}"]`);
        if (!element) {
            if (dotNet) dotNet.dispose();
            return;
        }

        const timeline = element.closest(".timeline");
        if (!timeline) {
            if (dotNet) dotNet.dispose();
            return;
        }

        let currentStart = startMinute;
        let currentEnd = endMinute;

        function getMinute(clientX) {
            const rect = timeline.getBoundingClientRect();
            if (rect.width <= 0) return 0;
            let x = clientX - rect.left;
            x = Math.max(0, Math.min(x, rect.width));
            let minute = Math.round((x / rect.width) * 1440);
            return Math.round(minute / 15) * 15;
        }

        function formatTime(minutes) {
            if (minutes >= 1440) return "24:00";
            const hours = Math.floor(minutes / 60);
            const mins = minutes % 60;
            return String(hours).padStart(2, "0") + ":" + String(mins).padStart(2, "0");
        }

        function formatDuration(minutes) {
            const hours = Math.floor(minutes / 60);
            const mins = minutes % 60;
            if (hours === 0) return `${mins} Min.`;
            if (mins === 0) return `${hours} Std.`;
            return `${hours} Std. ${mins} Min.`;
        }

        function updateVisual() {
            const left = (currentStart / 1440) * 100;
            const width = Math.max(((currentEnd - currentStart) / 1440) * 100, 1.2);

            element.style.left = `${left.toFixed(2)}%`;
            element.style.width = `${width.toFixed(2)}%`;

            const timeElement = element.querySelector(".assignment-time");
            if (timeElement) {
                timeElement.textContent = `${formatTime(currentStart)} – ${formatTime(currentEnd)}`;
            }

            const durationElement = element.querySelector(".assignment-duration");
            if (durationElement) {
                durationElement.textContent = formatDuration(currentEnd - currentStart);
            }
        }

        function onMove(event) {
            const minute = getMinute(event.clientX);
            if (side === "left") {
                currentStart = Math.min(minute, currentEnd - 15);
            } else {
                currentEnd = Math.max(minute, currentStart + 15);
            }
            updateVisual();
        }

        async function onUp(event) {
            document.removeEventListener("pointermove", onMove);
            document.removeEventListener("pointerup", onUp);
            document.removeEventListener("pointercancel", onUp);

            const minute = getMinute(event.clientX);

            try {
                if (dotNet) {
                    await dotNet.invokeMethodAsync("ResizeAssignment", key, side, minute);
                }
            } catch (error) {
                console.error("ResizeAssignment error:", error);
            } finally {
                if (dotNet) {
                    dotNet.dispose();
                }
            }
        }

        document.addEventListener("pointermove", onMove);
        document.addEventListener("pointerup", onUp);
        document.addEventListener("pointercancel", onUp);

        updateVisual();
    }
};
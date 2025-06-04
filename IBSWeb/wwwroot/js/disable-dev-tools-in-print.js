document.addEventListener("keydown", function (event) {
    // Disable Ctrl+P (Print)
    if (event.ctrlKey && event.key.toLowerCase() === "p") {
        event.preventDefault();
    }

    // Disable F12
    if (event.key === "F12" || event.keyCode === 123) {
        event.preventDefault();
    }

    // Disable Ctrl+Shift+I, Ctrl+Shift+J, Ctrl+Shift+C
    if (event.ctrlKey && event.shiftKey) {
        const key = event.key.toUpperCase();
        if (["I", "J", "C"].includes(key)) {
            event.preventDefault();
        }
    }

    // Disable Ctrl+U (View Source)
    if (event.ctrlKey && event.key.toLowerCase() === "u") {
        event.preventDefault();
    }
});

document.addEventListener("contextmenu", function (e) {
    e.preventDefault();
});

$(document).ready(function () {
    const threshold = 160;
    let devtoolsOpen = false;
    const originalContent = document.body.innerHTML;

    function checkDevTools() {
        const widthDiff = window.outerWidth - window.innerWidth;
        const heightDiff = window.outerHeight - window.innerHeight;
        const isOpen = widthDiff > threshold || heightDiff > threshold;

        if (isOpen && !devtoolsOpen) {
            devtoolsOpen = true;
            document.body.innerHTML = '';
        } else if (!isOpen && devtoolsOpen) {
            devtoolsOpen = false;
            document.body.innerHTML = originalContent;
        }
    }

    setInterval(checkDevTools, 250);
});
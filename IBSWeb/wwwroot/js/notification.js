var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub", {
        transport: signalR.HttpTransportType.LongPolling // ⬅ IMPORTANT
    })
    .withAutomaticReconnect([10000, 30000, 60000]) // less aggressive
    .configureLogging(signalR.LogLevel.Error)
    .build();

let isConnected = false;
let reconnectTimer = null;

// ==========================
// Start connection safely
// ==========================
async function startConnection() {
    if (connection.state !== signalR.HubConnectionState.Disconnected) return;

    try {
        await connection.start();
        isConnected = true;
        await onConnected();
        console.log("✅ Notification channel connected");
    } catch (err) {
        isConnected = false;
        console.error("❌ SignalR start failed:", err.toString());
        scheduleReconnect();
    }
}

// ==========================
// Controlled reconnect
// ==========================
function scheduleReconnect() {
    if (reconnectTimer) return;

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        startConnection();
    }, 30000); // reconnect every 30s max
}

// ==========================
// SignalR lifecycle handlers
// ==========================
connection.onreconnecting(() => {
    isConnected = false;
    console.warn("🔄 Reconnecting notification channel...");
});

connection.onreconnected(async () => {
    isConnected = true;
    await onConnected();
    console.log("✅ Notification channel reconnected");
});

connection.onclose(() => {
    isConnected = false;
    console.warn("❌ Notification channel closed");
    scheduleReconnect();
});

// ==========================
// Register user once per session
// ==========================
async function onConnected() {
    const username = $('#hfUsername').val();
    if (!username) return;

    try {
        await connection.invoke("SaveUserConnection", username);
    } catch (err) {
        console.error("❌ Failed to register user:", err.toString());
    }
}

// ==========================
// Receive notification count
// ==========================
connection.on("ReceiveNotificationCount", function (count) {
    $('#notificationCount').text(count);

    if (count > 0) {
        $('#notificationCount')
            .addClass('badge-pulse')
            .delay(1500)
            .queue(function (next) {
                $(this).removeClass('badge-pulse');
                next();
            });
    }
});

// ==========================
// Receive notification message
// ==========================
connection.on("ReceivedNotification", function (message) {
    Swal.fire({
        title: 'New Notification',
        text: message,
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: 'View Notifications',
        cancelButtonText: 'Dismiss',
        timer: 8000,
        timerProgressBar: true
    }).then(result => {
        if (result.isConfirmed) {
            window.location.href = '/User/Notification/Index';
        }
    });
});

// ==========================
// Initial count fetch (ONCE)
// ==========================
function fetchInitialNotificationCount() {
    $.get('/User/Notification/GetNotificationCount')
        .done(count => $('#notificationCount').text(count))
        .fail(() => $('#notificationCount').text('0'));
}

// ==========================
// Page lifecycle
// ==========================
$(document).ready(function () {
    const username = $('#hfUsername').val();
    if (!username) return;

    fetchInitialNotificationCount();
    startConnection();
});

// ==========================
// Cleanup on tab close
// ==========================
window.addEventListener("beforeunload", function () {
    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.stop();
    }
});

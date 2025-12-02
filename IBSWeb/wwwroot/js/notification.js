var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

// Track connection state
var isConnected = false;

// Start connection
connection.start()
    .then(function () {
        //console.log('✅ SignalR Connected - Real-time notifications active');
        isConnected = true;
        OnConnected();
    })
    .catch(function (err) {
        console.error('❌ SignalR Connection Error:', err.toString());
        isConnected = false;
    });

// Handle reconnection
connection.onreconnecting(function() {
    console.log('🔄 SignalR Reconnecting...');
    isConnected = false;
});

connection.onreconnected(function() {
    console.log('✅ SignalR Reconnected');
    isConnected = true;
    OnConnected();
    updateNotificationCount();
});

connection.onclose(function() {
    console.log('❌ SignalR Disconnected');
    isConnected = false;
});

// Save user connection when connected
function OnConnected() {
    var username = $('#hfUsername').val();
    if (username && username !== "") {
        connection.invoke("SaveUserConnection", username)
            .then(function() {
                //console.log('✅ User connection saved:', username);
            })
            .catch(function (err) {
                console.error('❌ Error saving connection:', err.toString());
            });
    }
}

// Listen for real-time notification count updates
connection.on("ReceiveNotificationCount", function (count) {
    console.log('📬 Notification count updated:', count);
    $('#notificationCount').text(count);

    // Optional: Add pulse animation when count increases
    if (count > 0) {
        $('#notificationCount').addClass('badge-pulse');
        setTimeout(function() {
            $('#notificationCount').removeClass('badge-pulse');
        }, 2000);
    }
});

// Listen for notification messages (with SweetAlert)
connection.on("ReceivedNotification", function (message) {
    console.log('📨 New notification:', message);

    Swal.fire({
        title: 'New Notification',
        text: message,
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: 'View Notifications',
        cancelButtonText: 'Dismiss',
        allowOutsideClick: true,
        timer: 10000,
        timerProgressBar: true
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = '/User/Notification/Index';
        }
    });
});

// Fetch initial notification count (called once on page load)
function updateNotificationCount() {
    $.ajax({
        url: '/User/Notification/GetNotificationCount',
        type: 'GET',
        success: function (count) {
            $('#notificationCount').text(count);
            //console.log('📊 Initial notification count:', count);
        },
        error: function (xhr, status, error) {
            console.error('❌ Error fetching notification count:', error);
            // Set to 0 on error
            $('#notificationCount').text('0');
        }
    });
}

// Initialize on page load
$(document).ready(function () {
    var username = $('#hfUsername').val();

    if (username && username !== "") {
        //console.log('👤 User logged in:', username);
        updateNotificationCount();

    } else {
        console.log('👤 No user logged in - notifications disabled');
    }
});

// Cleanup on page unload
$(window).on('beforeunload', function() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.stop();
    }
});
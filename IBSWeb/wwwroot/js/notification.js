var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.None) // Suppress SignalR logs
    .build();

connection.start()
    .then(function () {
        console.log('Connected to hub'); // Optional custom log
    })
    .catch(function (err) {
        return console.error(err.toString());
    });

connection.on("OnConnected", function () {
    OnConnected();
});

function OnConnected() {
    var username = $('#hfUsername').val();
    if (username != "") {
        connection.invoke("SaveUserConnection", username)
            .catch(function (err) {
                return console.error(err.toString());
            });
    }
}

connection.on("ReceivedNotification", function (message) {
    Swal.fire({
        title: message,
        allowOutsideClick: false,
        icon: 'info',
        confirmButtonText: 'Go to Notifications'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = '/User/Notification/Index';
        }
    });
});

function updateNotificationCount() {
    $.ajax({
        url: '/User/Notification/GetNotificationCount',
        type: 'GET',
        success: function (count) {
            $('#notificationCount').text(count);
        },
        error: function () {
            console.error("Error fetching notification count");
        }
    });
}

$(document).ready(function () {
    updateNotificationCount();
});
var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

connection.start().then(function () {
    console.log('connected to hub');
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("OnConnected", function () {
    OnConnected();
});

function OnConnected() {
    var username = $('#hfUsername').val();
    if (username != "") {
        connection.invoke("SaveUserConnection", username).catch(function (err) {
            return console.error(err.toString());
        })
    }
}

connection.on("ReceivedNotification", function (message) {
    Swal.fire({
        title: message,
        allowOutsideClick: false,
        icon: 'info',
        confirmButtonText: 'Go to Inbox'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = '/User/Notification/Index';
        }
    });
});
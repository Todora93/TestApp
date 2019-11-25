function bla() {
    keyInput.value = prompt('Enter your name:', '');
}

bla();

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/myHub?userId=" + keyInput.value)
    .withAutomaticReconnect([0, 0, 10000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.start().then(function () {
    console.log("started");
    connection.invoke('GetConnectionId').then(function (connectionId) {
        console.log('connectionId is :' + connectionId);
        //document.getElementById('signalRConnectionId').innerHTML = connectionId;
    })

}).catch(function (err) {
    return console.error(err.toString());
});

async function start() {
    try {
        await connection.start();
        console.log("connected");
    } catch (err) {
        console.log(err);
        setTimeout(() => start(), 5000);
    }
};

connection.onreconnected((connectionId) => {
    console.assert(connection.state === signalR.HubConnectionState.Connected);
    console.log("Reconnected with connectionId " + connectionId);
    self = this;
    self.connectionId = connectionId;
});
connection.onclose((error) => {
    console.assert(connection.state === signalR.HubConnectionState.Disconnected);
    console.log("Connection closed ");
});


connection.on("ReceiveGameState", message => {
    const li = document.createElement("li");
    li.textContent = message;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("GameFinished", message => {
    const li = document.createElement("li");
    li.textContent = message;
    document.getElementById("messagesList").appendChild(li);
});
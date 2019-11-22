const connection = new signalR.HubConnectionBuilder()
    .withUrl("/myHub")
    .withAutomaticReconnect([0, 0, 10000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.start().then(function () {
    console.log("started");
}).catch(function (err) {
    return console.error(err.toString());
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

connection.onclose(async () => {
    await start();
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

    document.getElementById("messageInput").disabled = false;

    const li = document.createElement("li");
    li.textContent = `Connection reestablished. Connected with connectionId "${connectionId}".`;
    document.getElementById("messagesList").appendChild(li);
});

//connection.onclose(async () => {
//    console.assert(connection.state === signalR.HubConnectionState.Disconnected);

//    document.getElementById("messageInput").disabled = true;

//    const li = document.createElement("li");
//    li.textContent = `Connection closed due to error "${error}". Try refreshing this page to restart the connection.`;
//    document.getElementById("messagesList").appendChild(li);
//});


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

//connection.onclose(async () => {
//    console.assert(connection.state === signalR.HubConnectionState.Disconnected);

//    document.getElementById("messageInput").disabled = true;

//    const li = document.createElement("li");
//    li.textContent = `Connection closed due to error "${error}". Try refreshing this page to restart the connection.`;
//    document.getElementById("messagesList").appendChild(li);
//});

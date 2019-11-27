
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

    connection.invoke('GetConnectionId').then(function (connectionId) {
        console.log('connectionId is :' + connectionId);
        //document.getElementById('signalRConnectionId').innerHTML = connectionId;
    })
});
connection.onclose((error) => {
    console.assert(connection.state === signalR.HubConnectionState.Disconnected);
    console.log("Connection closed ");
});

connection.on("GameStarted", (mLong, playerId) => {
    controller.actorId = mLong;
    const li = document.createElement("li");
    li.textContent = "[MATCH STARTED] ActorId long " + mLong + " playerId: " + playerId;
    document.getElementById("messagesList").appendChild(li);
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

var keys = {
    RIGHT: 39,
    LEFT: 37,
    UP: 38,
    DOWN: 40,
};

function subscribe() {
    console.log("added listener");
    document.addEventListener('keydown', function (e) {

        console.log('keydown, code: ' + e.keyCode + ' userId: ' + keyInput.value + ' actorId: ' + controller.actorId);

        //connection.invoke('SendMessage', keyInput.value, "nesto");

        var code = -1;

        if (e.keyCode === keys.RIGHT) {
            code = 0;
        }
        else if (e.keyCode === keys.LEFT) {
            code = 1;
        }
        else if (e.keyCode === keys.UP) {
            code = 2;
        }
        else if (e.keyCode === keys.DOWN) {
            code = 3;
        }

        connection.invoke('SendInput', keyInput.value, controller.actorId, code);

    }, false);
}

subscribe();
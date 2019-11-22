/* This function calls the RequestsController's HTTP PUT method to insert a new UserRequest in the reliable queue in the RequestsService*/
function addRequestValue() {
    var data = {
        UserId: keyInput.value,
    };

    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                keyInput.value = '';
                valueInput.value = '';
                keyInput.focus();
                updateFooter(http, (end - start));
            } else {
                keyInput.focus();
                updateFooter(http, (end - start));
            }
        }
    };
    start = new Date().getTime();
    http.open("PUT", "/api/Requests/?c=" + start);
    http.setRequestHeader("content-type", "application/json");
    http.send(JSON.stringify(data));
}


/* This function calls the Requests's HTTP GET method to get a collection of request GUIDs from the reliable queue in the RequestsService */
function getAllRequests() {
    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                returnData = JSON.parse(http.responseText);
                if (returnData) {
                    renderRequestsQueue(returnData);
                    updateFooter(http, (end - start));
                    //postMessage("Got all KeyValuePairs in  " + (end - start).toString() + "ms.", "success", true);
                }
            } else {
                updateFooter(http, (end - start));
                //postMessage(http.statusText, "danger", true);
            }
        }
    };
    start = new Date().getTime();
    http.open("GET", "/api/Requests/?c=" + start);
    http.send();
}

/* This function calls the Requests's HTTP GET method to get a collection of request GUIDs from the reliable queue in the RequestsService */
function deleteAllRequests() {
    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                returnData = JSON.parse(http.responseText);
                if (returnData) {
                    renderRequestsQueue(null);
                    updateFooter(http, (end - start));
                    //postMessage("Got all KeyValuePairs in  " + (end - start).toString() + "ms.", "success", true);
                }
            } else {
                updateFooter(http, (end - start));
                //postMessage(http.statusText, "danger", true);
            }
        }
    };
    start = new Date().getTime();
    http.open("DELETE", "/api/Requests/?c=" + start);
    http.send();
}

/* This function calls the StatelessBackendController's HTTP GET method to get the current count from the StatelessBackendService */
function matchmake() {
    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                returnData = JSON.parse(http.responseText);
                if (returnData) {
                    countDisplay.innerHTML = returnData.count;
                    updateFooter(http, (end - start));
                }
            } else {
                updateFooter(http, (end - start));
            }
        }
    };
    start = new Date().getTime();
    http.open("GET", "/api/Matchmaker/?c=" + start);
    http.send();
}

/* This function calls the ActorBackendController's HTTP GET method to get the number of actors in the ActorBackendService */
function getActorCount() {
    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                returnData = JSON.parse(http.responseText);
                if (returnData) {
                    countDisplay.innerHTML = returnData.count;
                    updateFooter(http, (end - start));
                }
            } else {
                updateFooter(http, (end - start));
            }
        }
    };
    start = new Date().getTime();
    http.open("GET", "/api/ActorBackendService/?c=" + start);
    http.send();
}

/* This function calls the ActorBackendController's HTTP POST method to create a new actor in the ActorBackendService */
function newActor() {
    var http = new XMLHttpRequest();
    http.onreadystatechange = function () {
        if (http.readyState === 4) {
            end = new Date().getTime();
            if (http.status < 400) {
                returnData = JSON.parse(http.responseText);
                if (returnData) {
                    updateFooter(http, (end - start));
                }
            } else {
                updateFooter(http, (end - start));
            }
        }
    };
    start = new Date().getTime();
    http.open("POST", "/api/ActorBackendService/?c=" + start);
    http.send();
}

/* UI Helper fuctions */

/* This function renders the output of the call to the Requests Service in a table */
function renderRequestsQueue(list) {
    var table = document.getElementById('statefulBackendServiceTable').childNodes[1];

    while (table.childElementCount > 1) {
        table.removeChild(table.lastChild);
    }

    if (list == null) return;

    for (var i = 0; i < list.length; i++) {
        var tr = document.createElement('tr');
        var tdKey = document.createElement('td');
        tdKey.appendChild(document.createTextNode(list[i].userId));
        tr.appendChild(tdKey);
        var tdValue = document.createElement('td');
        tdValue.appendChild(document.createTextNode(list[i].userId));
        tr.appendChild(tdValue);
        table.appendChild(tr);
    }
}

/* This function highlights the current nav tab */
function navTab() {
    toggleFooter(0);
    var pathName = document.location.pathname.substring(6);
    switch (pathName) {
        case "":
            document.getElementById('navHome').className = "active";
            break;
        case "Matchmaker":
            document.getElementById('navMatchmaker').className = "active";
            break;
        case "Requests":
            document.getElementById('navRequests').className = "active";
            break;
        case "Simulation":
            document.getElementById('navSimulation').className = "active";
            break;
        case "Actor":
            document.getElementById('navActor').className = "active";
            break;
    }
}

/*This function hides the footer*/
function toggleFooter(option) {
    var footer = document.getElementById('footer');
    switch (option) {
        case 0:
            footer.hidden = true;
            break;
        case 1:
            footer.hidden = false;
            break;
    }
}

/*This function puts HTTP result in the footer */
function updateFooter(http, timeTaken) {
    toggleFooter(1);
    if (http.status < 299) {
        statusPanel.className = 'panel panel-success';
        statusPanelHeading.innerHTML = http.status + ' ' + http.statusText;
        statusPanelBody.innerHTML = 'Result returned in ' + timeTaken.toString() + ' ms from ' + http.responseURL;
    }
    else {
        statusPanel.className = 'panel panel-danger';
        statusPanelHeading.innerHTML = http.status + ' ' + http.statusText;
        statusPanelBody.innerHTML = http.responseText;
    }
}

function handleEnter() {
    var pathName = document.location.pathname.substring(6);
    switch (pathName) {
        case "Matchmaker":
            onkeyup = function (e) {
                if (e.keyCode == 13) {
                    matchmake();
                    return false;
                }
            };
            break;
        case "Stateful":
            keyInput.onkeyup = function (e) {
                if (e.keyCode == 13) {
                    addRequestValue();
                    return false;
                }
            };
            valueInput.onkeyup = function (e) {
                if (e.keyCode == 13) {
                    addRequestValue();
                    return false;
                }
            };
            break;
    }
}

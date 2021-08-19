const {
    app,
    BrowserWindow,
    protocol
} = require('electron');
const {
    spawn
} = require('child_process');
const portscanner = require('portscanner');
const path = require('path');
const {
    dosDateTimeToDate
} = require('yauzl');
require('./bufferExtension')(Buffer)
const {
    NodePack,
    NodePackType
} = require("./nodePack");
const {
    resolve
} = require('path');

let nextPackId = 1;
let cbMap = {};

let mainWin;
let csProcess;

app.on('ready', async () => {
    let {
        webPort,
        wsPort
    } = await startServer();
    let win = new BrowserWindow({
        width: 640,
        height: 320,
        center: true,
        show: false,
        webPreferences: {
            nodeIntegration: false
        }
    });
    mainWin = win;
    win.loadURL('http://localhost:' + webPort + '/wwwroot/index.html');

    win.webContents.on('did-finish-load', function () {
        win.webContents.executeJavaScript("client.connect('localhost', " + wsPort + ");");
    });

    win.once('ready-to-show', () => {
        win.show();
    });
});

async function startServer() {
    var srvPath = path.join(__dirname, "../ElectronFlex/bin/Debug/net5.0/ElectronFlex.exe");
    if (path.basename(app.getAppPath()) == 'app.asar') {
        srvPath = path.join(path.dirname(app.getPath('exe')), "bin/ElectronFlex.exe");
    }

    // Added default port as configurable for port restricted environments.
    let defaultElectronPort = 8000;
    // hostname needs to be localhost, otherwise Windows Firewall will be triggered.
    let webPort = await portscanner.findAPortNotInUse(defaultElectronPort, 65535, 'localhost');
    console.log('Web Port: ' + webPort);
    let wsPort = await portscanner.findAPortNotInUse(webPort + 1, 65535, 'localhost');
    console.log('WS Port: ' + wsPort);

    let child = spawn(srvPath, [
        "--electron",
        "--webport=" + webPort,
        "--wsport=" + wsPort
    ]);
    csProcess = child;

    let buff = Buffer.alloc(128 * 1024); //128k

    // child.stdout.setEncoding('utf8');
    child.stdout.on('data', data => {
        // console.log('data', data, data.toString('utf8'));
        buff = buff.writeBytes(data);
        // console.log('buff', buff);

        var pack = NodePack.Decode(buff);
        // console.log("pack", pack);
        while (pack) {
            switch (pack.Type) {
                case NodePackType.ConsoleOutput:
                    console.log("CS WriteLine: " + pack.Content);
                    break;
                case NodePackType.InvokeCode:
                    console.log("CS Invoke: " + pack.Content);
                    var result = eval(pack.Content);
                    if (isPromise(result)) {
                        var packId = pack.Id;
                        result.then(val => {
                            var json = JSON.stringify(val);
                            json = json === undefined ? 'null' : json;
                            console.log("Invoke Result: " + json);
                            var retPack = new NodePack(packId, NodePackType.InvokeResult, json);

                            child.stdin.cork();
                            child.stdin.write(retPack.Encode());
                            child.stdin.uncork();
                        });
                    } else {
                        var json = JSON.stringify(result);
                        json = json === undefined ? 'null' : json;
                        console.log("Invoke Result: " + json);
                        var retPack = new NodePack(pack.Id, NodePackType.InvokeResult, json);

                        child.stdin.cork();
                        child.stdin.write(retPack.Encode());
                        child.stdin.uncork();
                    }
                    break;
                case NodePackType.InvokeResult:
                    console.log("CS Result: " + pack.Content);
                    var result = Function('"use strict";return (' + pack.Content + ')')();
                    if (cbMap[pack.Id]) {
                        let {
                            resolve,
                            reject
                        } = cbMap[pack.Id];
                        resolve(result);
                    }

                    break;
            }

            pack = NodePack.Decode(buff);
            // console.log("pack", pack);
        }
    });

    return {
        "webPort": webPort,
        "wsPort": wsPort
    };
}

function invokeCsharp(cls, method) {
    var args = [];
    if (arguments.length > 2) args = [].slice.call(arguments, 2);

    var id = nextPackId++;
    if (nextPackId > 255) nextPackId = 1;

    var content = JSON.stringify({
        "Class": cls,
        "Method": method,
        "Arguments": args
    });

    var pack = new NodePack(id, NodePackType.InvokeCode, content);

    return new Promise((resolve, reject) => {
        cbMap[id] = {
            "resolve": resolve,
            "reject": reject
        };

        csProcess.stdin.cork();
        csProcess.stdin.write(pack.Encode());
        csProcess.stdin.uncork();
    });
}

function isPromise(v) {
    return typeof v === 'object' && typeof v.then === 'function';
}
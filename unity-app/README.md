# A.R.K. Unity frontend

The Unity client for A.R.K. Built by Christian Malan ([chris-malan](https://github.com/chris-malan)).

It connects to the Python server in `../server` over a WebSocket. The scripts are in `Assets/Script`, including `ArkWebSocketClient.cs`, `ArkEditIntentRouter.cs` and `MicrophoneRecorder.cs`.

## Run it

1. Open this folder in Unity `6000.3.8f1`.
2. Start the server so it's available at `ws://localhost:8000/ws`. You can change that address on the `websocketUrl` field of `ArkWebSocketClient`.
3. Open `Assets/Scenes/EditScene.unity` and press Play.

## Third-party code

- `Assets/OBJImport`: Dummiesman's OBJ Importer, MIT licence.
- `Assets/TextMesh Pro`: Unity's TextMesh Pro resources.
- Ultimate Procedural Primitives FREE was in the original project. It's a Unity Asset Store package and isn't included here. `EditScene` doesn't reference it.

## Known limitations

This is a prototype, and it has known bugs.

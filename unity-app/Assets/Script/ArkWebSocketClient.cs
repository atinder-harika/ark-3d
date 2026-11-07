using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TMPro; 
using Dummiesman; 
using System.IO;

public class ArkWebSocketClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string websocketUrl = "ws://localhost:8000/ws";
    
    [Tooltip("The absolute path to your Python server folder")]
    public string serverBasePath = @"C:\Users\atind\GitHub\A.R.K.-2\server";

    [Header("UI & Routing")]
    public TMP_InputField commandInput;
    public TMP_Text statusText;
    public ArkEditIntentRouter editRouter; // Link to your new script!

    private ClientWebSocket ws;
    private CancellationTokenSource cts;

    private Queue<Action> mainThreadActions = new Queue<Action>();
    private GameObject currentModel = null;

    [Serializable]
    private class ArkMessage
    {
        public string type;
        public string obj_path;
        public string job_id;
        public string status;
        public string message;
    }

    async void Start()
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();
        await ConnectToServer();
    }

    async Task ConnectToServer()
    {
        try
        {
            UpdateStatusText("Connecting...");
            Debug.Log($"[ARK] Connecting to {websocketUrl}...");
            
            await ws.ConnectAsync(new Uri(websocketUrl), cts.Token);
            
            Debug.Log("[ARK] Connected to Server successfully!");
            UpdateStatusText("Connected");
            
            _ = ReceiveMessages(); 
        }
        catch (Exception e)
        {
            UpdateStatusText("Socket Error");
            Debug.LogError($"[ARK] WebSocket Connection Error: {e.Message}");
        }
    }

    async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 4];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"[ARK Server Message]: {json}");
                mainThreadActions.Enqueue(() => ProcessIncomingJson(json));
            }
        }
    }

    void ProcessIncomingJson(string json)
    {
        if (json.Contains("\"type\":\"generation_complete\"") || json.Contains("\"type\":\"image_job_accepted\""))
        {
            UpdateStatusText("Processing Job...");
        }

        // HAND-OFF TO THE ROUTER
        if (json.Contains("\"intent\":\"edit_unity\"") || json.Contains("\"type\":\"edit_unity\""))
        {
            try
            {
                var intent = JsonUtility.FromJson<EditUnityIntent>(json);
                if (editRouter != null)
                {
                    // Pass the intent AND the currently spawned model to the router
                    editRouter.ApplyEditIntent(intent, currentModel);
                }
                else
                {
                    Debug.LogWarning("[ARK] Received an edit intent, but ArkEditIntentRouter is not assigned in the Inspector!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARK] Error parsing Edit Intent: {ex.Message}");
            }
            return;
        }

        try
        {
            ArkMessage msg = JsonUtility.FromJson<ArkMessage>(json);
            HandleServerMessage(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ARK] Error parsing JSON: {e.Message}");
        }
    }

    void HandleServerMessage(ArkMessage msg)
    {
        if (msg.type == "generation_started" || msg.type == "webcam_job_started")
        {
            Debug.Log($"[ARK] Server started: {msg.type}...");
            UpdateStatusText("Generating...");
        }
        else if ((msg.type == "generation_complete" || msg.type == "webcam_job_complete") && !string.IsNullOrEmpty(msg.obj_path))
        {
            Debug.Log($"[ARK] {msg.type} complete! Queuing model swap...");
            UpdateStatusText("Loading Model...");
            LoadNewModel(msg.obj_path);
        }
        else if (msg.type == "generation_failed")
        {
            Debug.LogError($"[ARK] Generation failed: {msg.message}");
            UpdateStatusText("Failed");
        }
    }

    void LoadNewModel(string relativePath)
    {
        string absolutePath = Path.Combine(serverBasePath, relativePath);
        absolutePath = absolutePath.Replace("/", @"\"); 

        if (!File.Exists(absolutePath))
        {
            Debug.LogError($"[ARK] OBJ file not found at: {absolutePath}");
            return;
        }

        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        try
        {
            currentModel = new OBJLoader().Load(absolutePath);
            
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r.material != null)
                {
                    Texture oldTex = r.material.HasProperty("_MainTex") ? r.material.GetTexture("_MainTex") : null;
                    Color oldColor = r.material.HasProperty("_Color") ? r.material.color : Color.white;

                    r.material.shader = urpShader;

                    if (oldTex != null && r.material.HasProperty("_BaseMap"))
                        r.material.SetTexture("_BaseMap", oldTex);
                    
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", oldColor);
                }

                if (r.gameObject.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                    mc.convex = false;
                }
            }
            
            currentModel.transform.SetParent(this.transform);
            currentModel.transform.localPosition = Vector3.zero;
            
            ObjectBehavior behavior = GetComponent<ObjectBehavior>();
            if (behavior != null)
            {
                behavior.SetupModel(currentModel.transform);
            }

            MouseOrbit orbit = GetComponentInParent<MouseOrbit>();
            if (orbit != null)
            {
                orbit.RecenterPivot(currentModel);
            }

            UpdateStatusText("");
            Debug.Log("[ARK] Successfully swapped model!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ARK] Failed to load OBJ: {e.Message}");
        }
    }

    // ==========================================
    // PAYLOADS 
    // ==========================================

    public async void SendGenerateCommand()
    {
        if (ws == null || ws.State != WebSocketState.Open || commandInput == null) return;
        string payload = "{\"type\":\"generate\",\"prompt\":\"" + EscapeJson(commandInput.text) + "\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
    }

    public async Task SendVoiceTextAsync(string text)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        string payload = "{\"type\":\"voice_analyze\",\"text\":\"" + EscapeJson(text) + "\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
    }

    public async Task SendVoiceAudioAsync(byte[] wavBytes, string audioName = "voice.wav")
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        string payload = "{\"type\":\"voice_audio\",\"audio_name\":\"" + EscapeJson(audioName) + "\",\"audio_base64\":\"data:audio/wav;base64," + Convert.ToBase64String(wavBytes) + "\",\"auto_process\":true}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
    }

    public async Task SendImageTo3DAsync(byte[] imageBytes, string imageName, string prompt, string filename)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        string payload = "{\"type\":\"image_to_3d\",\"prompt\":\"" + EscapeJson(prompt) + "\",\"filename\":\"" + EscapeJson(filename) + "\",\"image_name\":\"" + EscapeJson(imageName) + "\",\"image_base64\":\"data:image/png;base64," + Convert.ToBase64String(imageBytes) + "\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
    }

    private string EscapeJson(string input)
    {
        return (input ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private void UpdateStatusText(string text)
    {
        mainThreadActions.Enqueue(() =>
        {
            if (statusText != null) statusText.text = text;
        });
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            Action action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (ws != null)
        {
            cts?.Cancel();
            ws.Dispose();
        }
    }
}
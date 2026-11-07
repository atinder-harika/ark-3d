using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ImagePicker : MonoBehaviour
{
    public ArkWebSocketClient wsClient;
    public TMP_Text statusText;
    public string defaultFilename = "image-model";

    public void PickImage()
    {
        NativeFilePicker.PickFile((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                if (statusText != null) statusText.text = "Cancelled";
                return;
            }

            if (statusText != null) statusText.text = "Uploading Image...";
            _ = SendPickedImageAsync(path);
        }, new[] { "image/png", "image/jpeg" });
    }

    private async Task SendPickedImageAsync(string path)
    {
        if (!File.Exists(path) || wsClient == null)
            return;

        byte[] bytes = File.ReadAllBytes(path);
        string filename = string.IsNullOrWhiteSpace(defaultFilename) ? Path.GetFileNameWithoutExtension(path) : defaultFilename;
        string imageName = Path.GetFileName(path);
        
        await wsClient.SendImageTo3DAsync(bytes, imageName, "", filename);
    }
}
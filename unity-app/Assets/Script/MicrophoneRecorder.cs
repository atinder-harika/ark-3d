using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class MicrophoneRecorder : MonoBehaviour
{
    public ArkWebSocketClient wsClient;
    public TMP_Text statusText;
    public int sampleRate = 16000;
    public int maxRecordSeconds = 30;

    private AudioClip _clip;
    private bool _recording;

    public void ToggleRecording()
    {
        if (!_recording)
        {
            StartRecording();
        }
        else
        {
            _ = StopRecordingAndSendAsync();
        }
    }

    private void StartRecording()
    {
        if (!Microphone.devices.Length.Equals(0))
        {
            _clip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
            _recording = true;
            if (statusText != null) statusText.text = "Recording...";
        }
    }

    private async Task StopRecordingAndSendAsync()
    {
        if (!_recording)
            return;

        int position = Microphone.GetPosition(null);
        Microphone.End(null);
        _recording = false;

        if (_clip == null || position <= 0)
            return;

        byte[] wavBytes = WavUtility.FromAudioClip(_clip, position);
        
        if (statusText != null) statusText.text = "Sending voice...";
        
        if (wsClient != null)
            await wsClient.SendVoiceAudioAsync(wavBytes, "voice.wav");
    }
}

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip, int samplesRecorded)
    {
        float[] samples = new float[samplesRecorded * clip.channels];
        clip.GetData(samples, 0);

        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            WriteWavHeader(writer, clip.channels, clip.frequency, samples.Length * sizeof(short));
            foreach (var sample in samples)
            {
                short value = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                writer.Write(value);
            }
            writer.Flush();
            return memoryStream.ToArray();
        }
    }

    private static void WriteWavHeader(BinaryWriter writer, int channels, int sampleRate, int dataLength)
    {
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(dataLength);
    }
}
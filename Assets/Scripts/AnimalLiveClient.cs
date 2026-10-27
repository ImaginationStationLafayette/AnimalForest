using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TMPro;

using UnityEngine;


public class AnimalLiveClient : MonoBehaviour
{
    // =========================================================
    // BACKEND
    // =========================================================

    [Header("Live Backend")]

    [SerializeField]
    private string liveServerUrl =
        "ws://127.0.0.1:8765";


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private ExhibitHUD exhibitHUD;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private TMP_Text microphoneButtonText;

    [SerializeField]
    private AudioSource audioSource;


    // =========================================================
    // MICROPHONE
    // =========================================================

    [Header("Microphone")]

    [SerializeField]
    private int microphoneSampleRate =
        16000;

    [SerializeField]
    private int microphoneBufferSeconds =
        10;

    [SerializeField]
    private float sendInterval =
        0.05f;

    [Range(0.5f, 3f)]
    [SerializeField]
    private float microphoneGain =
        1.25f;


    // =========================================================
    // OUTPUT
    // =========================================================

    [Header("Gemini Output")]

    [SerializeField]
    private int outputSampleRate =
        24000;

    [SerializeField]
    private float prebufferSeconds =
        0.20f;


    // =========================================================
    // SOCKET
    // =========================================================

    private ClientWebSocket socket;

    private CancellationTokenSource cancellation;

    private readonly SemaphoreSlim sendLock =
        new SemaphoreSlim(
            1,
            1
        );


    // =========================================================
    // QUEUES
    // =========================================================

    private readonly ConcurrentQueue<string>
        receivedMessages =
            new ConcurrentQueue<string>();

    private readonly ConcurrentQueue<float>
        incomingAudio =
            new ConcurrentQueue<float>();


    // =========================================================
    // STATE
    // =========================================================

    private bool liveSessionActive;

    private bool geminiReady;

    private bool pushToTalkHeld;

    private bool microphoneActive;

    private bool streamingPlaybackStarted;


    private AudioClip microphoneClip;

    private string microphoneDevice;

    private int lastMicrophonePosition;

    private float sendTimer;


    private string animalTranscript =
        "";

    private bool newAnimalTurn =
        true;


    // =========================================================
    // JSON
    // =========================================================

    [Serializable]
    private class StartMessage
    {
        public string type =
            "start";

        public string animal;
        public string species;
        public string diet;
        public string habitat;
        public string funFact;
    }


    [Serializable]
    private class AudioMessage
    {
        public string type =
            "audio";

        public string data;
    }


    [Serializable]
    private class AudioEndMessage
    {
        public string type =
            "audio_end";
    }


    [Serializable]
    private class StopMessage
    {
        public string type =
            "stop";
    }


    [Serializable]
    private class ServerMessage
    {
        public string type;

        public string data;

        public string text;

        public string message;
    }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (exhibitHUD == null)
        {
            exhibitHUD =
                GetComponent<ExhibitHUD>();
        }


        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource =
                    gameObject
                        .AddComponent<AudioSource>();
            }
        }


        audioSource.playOnAwake =
            false;

        audioSource.loop =
            true;

        audioSource.spatialBlend =
            0f;


        CreateStreamingAudioClip();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        ProcessServerMessages();


        if (
            liveSessionActive &&
            geminiReady &&
            pushToTalkHeld &&
            microphoneActive
        )
        {
            sendTimer +=
                Time.deltaTime;


            if (
                sendTimer >=
                sendInterval
            )
            {
                sendTimer =
                    0f;

                SendNewMicrophoneAudio();
            }
        }


        CheckStreamingPlayback();
    }


    // =========================================================
    // PREPARE SESSION
    // =========================================================

    public async void PrepareSession(
        AnimalExhibit animal
    )
    {
        if (animal == null)
            return;


        if (liveSessionActive)
            return;


        liveSessionActive =
            true;

        geminiReady =
            false;

        pushToTalkHeld =
            false;

        animalTranscript =
            "";

        newAnimalTurn =
            true;


        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Connecting";
        }


        socket =
            new ClientWebSocket();


        cancellation =
            new CancellationTokenSource();


        try
        {
            await socket.ConnectAsync(

                new Uri(
                    liveServerUrl
                ),

                cancellation.Token
            );


            StartMessage start =
                new StartMessage
                {
                    animal =
                        animal.AnimalTypeName,

                    species =
                        animal.Species,

                    diet =
                        animal.Diet,

                    habitat =
                        animal.Habitat,

                    funFact =
                        animal.FunFact
                };


            await SendJson(
                JsonUtility.ToJson(
                    start
                )
            );


            _ = ReceiveLoop();


            Debug.Log(
                "Animal Live session connected."
            );
        }

        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Live connection failed: " +
                exception.Message
            );


            if (dialogueText != null)
            {
                dialogueText.text =
                    "I couldn't start talking right now.";
            }


            CleanUpLiveState();
        }
    }


    // =========================================================
    // PRESS DOWN
    // =========================================================

    public void BeginPushToTalk()
    {
        if (
            !liveSessionActive ||
            !geminiReady
        )
        {
            return;
        }


        if (pushToTalkHeld)
            return;


        pushToTalkHeld =
            true;


        // -----------------------------------------------------
        // IMPORTANT:
        // Child intentionally interrupted the animal.
        //
        // Immediately stop whatever Gemini audio Unity
        // is currently playing.
        // -----------------------------------------------------

        ClearIncomingAudio();


        animalTranscript =
            "";

        newAnimalTurn =
            true;


        if (dialogueText != null)
        {
            dialogueText.text =
                "I'm listening...";
        }


        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Listening...";
        }


        StartMicrophone();
    }


    // =========================================================
    // RELEASE
    // =========================================================

    public void EndPushToTalk()
    {
        if (!pushToTalkHeld)
            return;


        pushToTalkHeld =
            false;


        StopMicrophone();


        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Hold to Talk";
        }


        if (dialogueText != null)
        {
            dialogueText.text =
                "Hmm...";
        }


        // Explicitly tell Gemini that this microphone
        // stream has ended.
        _ = SendAudioEnd();
    }


    // =========================================================
    // RECEIVE LOOP
    // =========================================================

    private async Task ReceiveLoop()
    {
        byte[] buffer =
            new byte[
                1024 *
                256
            ];


        try
        {
            while (
                socket != null &&
                socket.State ==
                WebSocketState.Open
            )
            {
                int totalBytes =
                    0;


                WebSocketReceiveResult result;


                do
                {
                    result =
                        await socket
                            .ReceiveAsync(

                                new ArraySegment<byte>(
                                    buffer,
                                    totalBytes,
                                    buffer.Length -
                                    totalBytes
                                ),

                                cancellation.Token
                            );


                    totalBytes +=
                        result.Count;


                    if (
                        totalBytes >=
                        buffer.Length
                    )
                    {
                        throw new Exception(
                            "Live message exceeded buffer."
                        );
                    }

                }
                while (
                    !result.EndOfMessage
                );


                if (
                    result.MessageType ==
                    WebSocketMessageType.Close
                )
                {
                    break;
                }


                string json =
                    Encoding.UTF8
                        .GetString(
                            buffer,
                            0,
                            totalBytes
                        );


                receivedMessages.Enqueue(
                    json
                );
            }
        }

        catch (
            OperationCanceledException
        )
        {
        }

        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Live receive error: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // PROCESS SERVER DATA
    // =========================================================

    private void ProcessServerMessages()
    {
        while (
            receivedMessages
                .TryDequeue(
                    out string json
                )
        )
        {
            ServerMessage message;


            try
            {
                message =
                    JsonUtility
                        .FromJson<ServerMessage>(
                            json
                        );
            }

            catch
            {
                continue;
            }


            if (message == null)
                continue;


            // -------------------------------------------------
            // READY
            // -------------------------------------------------

            if (
                message.type ==
                "ready"
            )
            {
                geminiReady =
                    true;


                if (microphoneButtonText != null)
                {
                    microphoneButtonText.text =
                        "Hold to Talk";
                }


                if (dialogueText != null)
                {
                    dialogueText.text =
                        "Hold the microphone button and ask me something!";
                }


                continue;
            }


            // -------------------------------------------------
            // AUDIO
            // -------------------------------------------------

            if (
                message.type ==
                "audio"
                &&
                !string.IsNullOrEmpty(
                    message.data
                )
            )
            {
                QueueGeminiAudio(
                    message.data
                );

                continue;
            }


            // -------------------------------------------------
            // CHILD TRANSCRIPT
            // -------------------------------------------------

            if (
                message.type ==
                "user_text"
            )
            {
                Debug.Log(
                    "Child: " +
                    message.text
                );

                continue;
            }


            // -------------------------------------------------
            // ANIMAL TEXT
            // -------------------------------------------------

            if (
                message.type ==
                "animal_text"
            )
            {
                if (
                    string.IsNullOrWhiteSpace(
                        message.text
                    )
                )
                {
                    continue;
                }


                if (newAnimalTurn)
                {
                    animalTranscript =
                        "";

                    newAnimalTurn =
                        false;
                }


                animalTranscript +=
                    message.text;


                if (dialogueText != null)
                {
                    dialogueText.text =
                        animalTranscript;
                }


                continue;
            }


            // -------------------------------------------------
            // TURN COMPLETE
            // -------------------------------------------------

            if (
                message.type ==
                "turn_complete"
            )
            {
                newAnimalTurn =
                    true;


                if (
                    microphoneButtonText !=
                    null
                )
                {
                    microphoneButtonText.text =
                        "Hold to Talk";
                }


                continue;
            }


            // -------------------------------------------------
            // INTERRUPTED
            // -------------------------------------------------

            if (
                message.type ==
                "interrupted"
            )
            {
                ClearIncomingAudio();

                animalTranscript =
                    "";

                newAnimalTurn =
                    true;

                continue;
            }


            // -------------------------------------------------
            // ERROR
            // -------------------------------------------------

            if (
                message.type ==
                "error"
            )
            {
                Debug.LogError(
                    "Live server: " +
                    message.message
                );
            }
        }
    }


    // =========================================================
    // START MICROPHONE
    // =========================================================

    private void StartMicrophone()
    {
        if (microphoneActive)
            return;


        if (
            Microphone.devices ==
            null
            ||
            Microphone.devices.Length ==
            0
        )
        {
            Debug.LogError(
                "No microphone found."
            );

            return;
        }


        microphoneDevice =
            Microphone.devices[0];


        microphoneClip =
            Microphone.Start(

                microphoneDevice,

                true,

                microphoneBufferSeconds,

                microphoneSampleRate
            );


        if (microphoneClip == null)
        {
            Debug.LogError(
                "Microphone.Start failed."
            );

            return;
        }


        microphoneActive =
            true;


        lastMicrophonePosition =
            Microphone.GetPosition(
                microphoneDevice
            );


        sendTimer =
            0f;
    }


    // =========================================================
    // STOP MICROPHONE
    // =========================================================

    private void StopMicrophone()
    {
        if (!microphoneActive)
            return;


        // Send anything recorded since our last Update
        // before closing the microphone.
        SendNewMicrophoneAudio();


        microphoneActive =
            false;


        if (
            !string.IsNullOrEmpty(
                microphoneDevice
            )
            &&
            Microphone.IsRecording(
                microphoneDevice
            )
        )
        {
            Microphone.End(
                microphoneDevice
            );
        }


        microphoneClip =
            null;

        microphoneDevice =
            null;
    }


    // =========================================================
    // MICROPHONE CHUNKS
    // =========================================================

    private void SendNewMicrophoneAudio()
    {
        if (
            microphoneClip == null
            ||
            string.IsNullOrEmpty(
                microphoneDevice
            )
        )
        {
            return;
        }


        int currentPosition =
            Microphone.GetPosition(
                microphoneDevice
            );


        if (
            currentPosition < 0
            ||
            currentPosition ==
            lastMicrophonePosition
        )
        {
            return;
        }


        int clipSamples =
            microphoneClip.samples;


        int channels =
            microphoneClip.channels;


        int sampleFrames;


        if (
            currentPosition >
            lastMicrophonePosition
        )
        {
            sampleFrames =
                currentPosition -
                lastMicrophonePosition;
        }
        else
        {
            sampleFrames =
                clipSamples -
                lastMicrophonePosition +
                currentPosition;
        }


        if (sampleFrames <= 0)
            return;


        float[] samples =
            new float[
                sampleFrames *
                channels
            ];


        if (
            currentPosition >
            lastMicrophonePosition
        )
        {
            microphoneClip.GetData(
                samples,
                lastMicrophonePosition
            );
        }

        else
        {
            int firstFrames =
                clipSamples -
                lastMicrophonePosition;


            float[] first =
                new float[
                    firstFrames *
                    channels
                ];


            float[] second =
                new float[
                    currentPosition *
                    channels
                ];


            microphoneClip.GetData(
                first,
                lastMicrophonePosition
            );


            if (currentPosition > 0)
            {
                microphoneClip.GetData(
                    second,
                    0
                );
            }


            Array.Copy(
                first,
                0,
                samples,
                0,
                first.Length
            );


            Array.Copy(
                second,
                0,
                samples,
                first.Length,
                second.Length
            );
        }


        lastMicrophonePosition =
            currentPosition;


        // -----------------------------------------------------
        // MONO
        // -----------------------------------------------------

        float[] mono;


        if (channels == 1)
        {
            mono =
                samples;
        }

        else
        {
            mono =
                new float[
                    sampleFrames
                ];


            for (
                int frame = 0;
                frame < sampleFrames;
                frame++
            )
            {
                float sum =
                    0f;


                for (
                    int channel = 0;
                    channel < channels;
                    channel++
                )
                {
                    sum +=
                        samples[
                            frame *
                            channels +
                            channel
                        ];
                }


                mono[frame] =
                    sum /
                    channels;
            }
        }


        // -----------------------------------------------------
        // GAIN
        // -----------------------------------------------------

        for (
            int i = 0;
            i < mono.Length;
            i++
        )
        {
            mono[i] =
                Mathf.Clamp(
                    mono[i] *
                    microphoneGain,
                    -1f,
                    1f
                );
        }


        byte[] pcm =
            FloatSamplesToPcm16(
                mono
            );


        _ = SendAudioChunk(
            pcm
        );
    }


    // =========================================================
    // PCM CONVERSION
    // =========================================================

    private byte[] FloatSamplesToPcm16(
        float[] samples
    )
    {
        byte[] bytes =
            new byte[
                samples.Length *
                2
            ];


        for (
            int i = 0;
            i < samples.Length;
            i++
        )
        {
            float clamped =
                Mathf.Clamp(
                    samples[i],
                    -1f,
                    1f
                );


            short value =
                (short)(
                    clamped *
                    32767f
                );


            bytes[i * 2] =
                (byte)(
                    value &
                    0xff
                );


            bytes[
                i * 2 + 1
            ] =
                (byte)(
                    (
                        value >>
                        8
                    )
                    &
                    0xff
                );
        }


        return bytes;
    }


    // =========================================================
    // SEND AUDIO
    // =========================================================

    private async Task SendAudioChunk(
        byte[] pcm
    )
    {
        if (
            pcm == null
            ||
            pcm.Length == 0
        )
        {
            return;
        }


        AudioMessage message =
            new AudioMessage
            {
                data =
                    Convert.ToBase64String(
                        pcm
                    )
            };


        await SendJson(
            JsonUtility.ToJson(
                message
            )
        );
    }


    // =========================================================
    // END USER AUDIO TURN
    // =========================================================

    private async Task SendAudioEnd()
    {
        AudioEndMessage message =
            new AudioEndMessage();


        await SendJson(
            JsonUtility.ToJson(
                message
            )
        );
    }


    // =========================================================
    // SEND JSON
    // =========================================================

    private async Task SendJson(
        string json
    )
    {
        if (
            socket == null
            ||
            socket.State !=
            WebSocketState.Open
        )
        {
            return;
        }


        byte[] bytes =
            Encoding.UTF8
                .GetBytes(
                    json
                );


        await sendLock.WaitAsync();


        try
        {
            await socket.SendAsync(

                new ArraySegment<byte>(
                    bytes
                ),

                WebSocketMessageType.Text,

                true,

                cancellation.Token
            );
        }

        catch (
            OperationCanceledException
        )
        {
        }

        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Live send failed: " +
                exception.Message
            );
        }

        finally
        {
            sendLock.Release();
        }
    }


    // =========================================================
    // GEMINI AUDIO
    // =========================================================

    private void QueueGeminiAudio(
        string base64Audio
    )
    {
        try
        {
            byte[] pcm =
                Convert.FromBase64String(
                    base64Audio
                );


            for (
                int i = 0;
                i + 1 < pcm.Length;
                i += 2
            )
            {
                short sample =
                    (short)(
                        pcm[i]
                        |
                        (
                            pcm[
                                i + 1
                            ]
                            << 8
                        )
                    );


                incomingAudio.Enqueue(
                    sample /
                    32768f
                );
            }
        }

        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Could not decode Live audio: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // PLAYBACK
    // =========================================================

    private void CreateStreamingAudioClip()
    {
        AudioClip streamClip =
            AudioClip.Create(

                "GeminiLiveAudio",

                outputSampleRate *
                60,

                1,

                outputSampleRate,

                true,

                OnAudioRead
            );


        audioSource.clip =
            streamClip;
    }


    private void CheckStreamingPlayback()
    {
        if (
            streamingPlaybackStarted
            ||
            audioSource == null
        )
        {
            return;
        }


        int requiredSamples =
            Mathf.RoundToInt(
                outputSampleRate *
                prebufferSeconds
            );


        if (
            incomingAudio.Count >=
            requiredSamples
        )
        {
            streamingPlaybackStarted =
                true;


            if (
                !audioSource.isPlaying
            )
            {
                audioSource.Play();
            }
        }
    }


    private void OnAudioRead(
        float[] data
    )
    {
        for (
            int i = 0;
            i < data.Length;
            i++
        )
        {
            if (
                incomingAudio.TryDequeue(
                    out float sample
                )
            )
            {
                data[i] =
                    sample;
            }

            else
            {
                data[i] =
                    0f;
            }
        }
    }


    private void ClearIncomingAudio()
    {
        while (
            incomingAudio.TryDequeue(
                out _
            )
        )
        {
        }


        streamingPlaybackStarted =
            false;


        if (
            audioSource != null
            &&
            audioSource.isPlaying
        )
        {
            audioSource.Stop();
        }
    }


    // =========================================================
    // STOP WHOLE SESSION
    // =========================================================

    public async void StopLiveConversation()
    {
        if (!liveSessionActive)
            return;


        pushToTalkHeld =
            false;


        StopMicrophone();


        liveSessionActive =
            false;

        geminiReady =
            false;


        ClearIncomingAudio();


        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Hold to Talk";
        }


        try
        {
            if (
                socket != null
                &&
                socket.State ==
                WebSocketState.Open
            )
            {
                StopMessage stop =
                    new StopMessage();


                await SendJson(
                    JsonUtility.ToJson(
                        stop
                    )
                );


                await socket.CloseAsync(

                    WebSocketCloseStatus
                        .NormalClosure,

                    "Conversation ended",

                    CancellationToken.None
                );
            }
        }

        catch
        {
        }


        if (cancellation != null)
        {
            cancellation.Cancel();

            cancellation.Dispose();

            cancellation =
                null;
        }


        if (socket != null)
        {
            socket.Dispose();

            socket =
                null;
        }
    }


    private void CleanUpLiveState()
    {
        pushToTalkHeld =
            false;

        StopMicrophone();

        liveSessionActive =
            false;

        geminiReady =
            false;

        ClearIncomingAudio();


        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Hold to Talk";
        }


        if (cancellation != null)
        {
            cancellation.Cancel();

            cancellation.Dispose();

            cancellation =
                null;
        }


        if (socket != null)
        {
            socket.Dispose();

            socket =
                null;
        }
    }


    private void OnDestroy()
    {
        if (liveSessionActive)
        {
            StopLiveConversation();
        }
    }
}
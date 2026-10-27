using System;
using System.Collections;
using System.IO;
using System.Text;

using TMPro;

using UnityEngine;
using UnityEngine.Networking;


public class AnimalAIClient : MonoBehaviour
{
    // =========================================================
    // BACKEND
    // =========================================================

    [Header("Backend")]

    [SerializeField]
    private string chatUrl =
        "http://127.0.0.1:5000/animal-chat";

    [SerializeField]
    private string voiceUrl =
        "http://127.0.0.1:5000/animal-voice";

    [SerializeField]
    private string speakUrl =
        "http://127.0.0.1:5000/animal-speak";


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private ExhibitHUD exhibitHUD;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private AudioSource audioSource;

    [Tooltip("Optional TMP text inside the mic button.")]
    [SerializeField]
    private TMP_Text microphoneButtonText;


    // =========================================================
    // MICROPHONE
    // =========================================================

    [Header("Microphone")]

    [SerializeField]
    private int microphoneSampleRate =
        16000;

    [SerializeField]
    private int maximumRecordingSeconds =
        10;


    // =========================================================
    // STATUS
    // =========================================================

    [Header("Status Messages")]

    [SerializeField]
    private string listeningMessage =
        "I'm listening...";

    [SerializeField]
    private string thinkingMessage =
        "Hmm... let me think!";

    [SerializeField]
    private string errorMessage =
        "Oops! I couldn't answer right now.";


    // =========================================================
    // STATE
    // =========================================================

    private bool requestInProgress;
    private bool isRecording;

    private string microphoneDevice;

    private AudioClip microphoneClip;


    // =========================================================
    // JSON CLASSES
    // =========================================================

    [Serializable]
    private class AnimalChatRequest
    {
        public string animal;
        public string species;
        public string diet;
        public string habitat;
        public string funFact;
        public string question;
    }


    [Serializable]
    private class AnimalChatResponse
    {
        public string answer;
        public string audio;
    }


    [Serializable]
    private class AnimalVoiceRequest
    {
        public string animal;
        public string species;
        public string diet;
        public string habitat;
        public string funFact;

        public string audio;
    }


    [Serializable]
    private class AnimalVoiceResponse
    {
        public string transcript;
        public string answer;
        public string audio;
    }


    [Serializable]
    private class SpeakRequest
    {
        public string text;
    }


    [Serializable]
    private class SpeakResponse
    {
        public string audio;
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
                    gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            false;

        // Voice plays like UI audio,
        // not from a position in 3D space.
        audioSource.spatialBlend =
            0f;
    }


    // =========================================================
    // CURRENT ANIMAL
    // =========================================================

    private AnimalExhibit GetAnimal()
    {
        if (exhibitHUD == null)
            return null;

        return
            exhibitHUD.ActiveConversationExhibit;
    }


    // =========================================================
    // PRESET BUTTON: DIET
    // =========================================================

    public void AskDiet()
    {
        AnimalExhibit animal =
            GetAnimal();

        if (animal == null)
            return;

        string answer =
            $"I eat {animal.Diet}.";

        ShowAndSpeakTrustedAnswer(
            answer
        );
    }


    // =========================================================
    // PRESET BUTTON: HABITAT
    // =========================================================

    public void AskHabitat()
    {
        AnimalExhibit animal =
            GetAnimal();

        if (animal == null)
            return;

        string answer =
            $"I live in {animal.Habitat}.";

        ShowAndSpeakTrustedAnswer(
            answer
        );
    }


    // =========================================================
    // PRESET BUTTON: FUN FACT
    // =========================================================

    public void AskFunFact()
    {
        AnimalExhibit animal =
            GetAnimal();

        if (animal == null)
            return;

        string answer =
            animal.FunFact;

        ShowAndSpeakTrustedAnswer(
            answer
        );
    }


    // =========================================================
    // TRUSTED ANSWER
    // =========================================================

    private void ShowAndSpeakTrustedAnswer(
        string answer
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                answer
            )
        )
        {
            return;
        }

        // Display immediately.
        if (dialogueText != null)
        {
            dialogueText.text =
                answer;
        }

        // Speak asynchronously.
        StartCoroutine(
            SpeakText(
                answer
            )
        );
    }


    // =========================================================
    // MICROPHONE BUTTON
    // =========================================================

    public void ToggleMicrophone()
    {
        if (requestInProgress)
            return;

        if (!isRecording)
        {
            StartMicrophoneRecording();
        }
        else
        {
            StopMicrophoneAndSend();
        }
    }


    // =========================================================
    // START MICROPHONE
    // =========================================================

    private void StartMicrophoneRecording()
    {
        if (
            Microphone.devices == null ||
            Microphone.devices.Length == 0
        )
        {
            if (dialogueText != null)
            {
                dialogueText.text =
                    "I can't find a microphone.";
            }

            Debug.LogWarning(
                "No microphone detected."
            );

            return;
        }

        microphoneDevice =
            Microphone.devices[0];

        microphoneClip =
            Microphone.Start(
                microphoneDevice,
                false,
                maximumRecordingSeconds,
                microphoneSampleRate
            );

        if (microphoneClip == null)
        {
            Debug.LogError(
                "Could not start microphone."
            );

            return;
        }

        isRecording =
            true;

        if (dialogueText != null)
        {
            dialogueText.text =
                listeningMessage;
        }

        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Stop";
        }

        Debug.Log(
            "Microphone recording started: " +
            microphoneDevice
        );

        // Automatically stop when maximum time is reached.
        StartCoroutine(
            AutoStopRecording()
        );
    }


    // =========================================================
    // AUTO STOP
    // =========================================================

    private IEnumerator AutoStopRecording()
    {
        float timer =
            0f;

        while (
            isRecording &&
            timer < maximumRecordingSeconds
        )
        {
            timer +=
                Time.deltaTime;

            yield return null;
        }

        if (isRecording)
        {
            StopMicrophoneAndSend();
        }
    }


    // =========================================================
    // STOP + SEND MICROPHONE
    // =========================================================

    private void StopMicrophoneAndSend()
    {
        if (
            !isRecording ||
            microphoneClip == null
        )
        {
            return;
        }

        int recordedSamples =
            Microphone.GetPosition(
                microphoneDevice
            );

        Microphone.End(
            microphoneDevice
        );

        isRecording =
            false;

        if (microphoneButtonText != null)
        {
            microphoneButtonText.text =
                "Talk";
        }

        if (recordedSamples <= 0)
        {
            if (dialogueText != null)
            {
                dialogueText.text =
                    "I didn't hear anything. Try again!";
            }

            return;
        }

        byte[] wav =
            AudioClipToWav(
                microphoneClip,
                recordedSamples
            );

        microphoneClip =
            null;

        if (
            wav == null ||
            wav.Length == 0
        )
        {
            if (dialogueText != null)
            {
                dialogueText.text =
                    errorMessage;
            }

            return;
        }

        StartCoroutine(
            SendVoiceQuestion(
                wav
            )
        );
    }


    // =========================================================
    // SEND VOICE QUESTION
    // =========================================================

    private IEnumerator SendVoiceQuestion(
        byte[] wavBytes
    )
    {
        AnimalExhibit animal =
            GetAnimal();

        if (animal == null)
            yield break;

        requestInProgress =
            true;

        if (dialogueText != null)
        {
            dialogueText.text =
                thinkingMessage;
        }

        AnimalVoiceRequest payload =
            new AnimalVoiceRequest
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
                    animal.FunFact,

                audio =
                    Convert.ToBase64String(
                        wavBytes
                    )
            };

        string json =
            JsonUtility.ToJson(
                payload
            );

        byte[] body =
            Encoding.UTF8.GetBytes(
                json
            );

        using UnityWebRequest request =
            new UnityWebRequest(
                voiceUrl,
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                body
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return
            request.SendWebRequest();

        if (
            request.result !=
            UnityWebRequest.Result.Success
        )
        {
            Debug.LogError(
                "Voice request failed: " +
                request.error
            );

            if (dialogueText != null)
            {
                dialogueText.text =
                    errorMessage;
            }

            requestInProgress =
                false;

            yield break;
        }

        AnimalVoiceResponse response =
            null;

        try
        {
            response =
                JsonUtility
                    .FromJson<AnimalVoiceResponse>(
                        request.downloadHandler.text
                    );
        }
        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Could not parse voice response: " +
                exception.Message
            );
        }

        if (response == null)
        {
            if (dialogueText != null)
            {
                dialogueText.text =
                    errorMessage;
            }

            requestInProgress =
                false;

            yield break;
        }

        Debug.Log(
            "Child said: " +
            response.transcript
        );

        if (
            dialogueText != null &&
            !string.IsNullOrWhiteSpace(
                response.answer
            )
        )
        {
            dialogueText.text =
                response.answer;
        }

        PlayBase64Audio(
            response.audio
        );

        requestInProgress =
            false;
    }


    // =========================================================
    // OPTIONAL TEXT QUESTION
    // =========================================================

    public void AskQuestion(
        string question
    )
    {
        if (
            requestInProgress ||
            string.IsNullOrWhiteSpace(
                question
            )
        )
        {
            return;
        }

        AnimalExhibit animal =
            GetAnimal();

        if (animal == null)
            return;

        StartCoroutine(
            SendTextQuestion(
                animal,
                question
            )
        );
    }


    private IEnumerator SendTextQuestion(
        AnimalExhibit animal,
        string question
    )
    {
        requestInProgress =
            true;

        if (dialogueText != null)
        {
            dialogueText.text =
                thinkingMessage;
        }

        AnimalChatRequest payload =
            new AnimalChatRequest
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
                    animal.FunFact,

                question =
                    question
            };

        string json =
            JsonUtility.ToJson(
                payload
            );

        byte[] body =
            Encoding.UTF8.GetBytes(
                json
            );

        using UnityWebRequest request =
            new UnityWebRequest(
                chatUrl,
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                body
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return
            request.SendWebRequest();

        if (
            request.result !=
            UnityWebRequest.Result.Success
        )
        {
            if (dialogueText != null)
            {
                dialogueText.text =
                    errorMessage;
            }

            requestInProgress =
                false;

            yield break;
        }

        AnimalChatResponse response =
            JsonUtility.FromJson<AnimalChatResponse>(
                request.downloadHandler.text
            );

        if (
            response != null &&
            !string.IsNullOrWhiteSpace(
                response.answer
            )
        )
        {
            dialogueText.text =
                response.answer;
        }

        if (response != null)
        {
            PlayBase64Audio(
                response.audio
            );
        }

        requestInProgress =
            false;
    }


    // =========================================================
    // SPEAK TRUSTED TEXT
    // =========================================================

    private IEnumerator SpeakText(
        string text
    )
    {
        SpeakRequest payload =
            new SpeakRequest
            {
                text = text
            };

        string json =
            JsonUtility.ToJson(
                payload
            );

        byte[] body =
            Encoding.UTF8.GetBytes(
                json
            );

        using UnityWebRequest request =
            new UnityWebRequest(
                speakUrl,
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                body
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return
            request.SendWebRequest();

        if (
            request.result !=
            UnityWebRequest.Result.Success
        )
        {
            Debug.LogWarning(
                "TTS request failed: " +
                request.error
            );

            yield break;
        }

        SpeakResponse response =
            JsonUtility.FromJson<SpeakResponse>(
                request.downloadHandler.text
            );

        if (response != null)
        {
            PlayBase64Audio(
                response.audio
            );
        }
    }


    // =========================================================
    // BASE64 AUDIO
    // =========================================================

    private void PlayBase64Audio(
        string audioBase64
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                audioBase64
            )
        )
        {
            return;
        }

        try
        {
            byte[] wavBytes =
                Convert.FromBase64String(
                    audioBase64
                );

            AudioClip clip =
                CreateAudioClipFromWav(
                    wavBytes,
                    "AnimalVoice"
                );

            if (
                clip != null &&
                audioSource != null
            )
            {
                audioSource.Stop();

                audioSource.clip =
                    clip;

                audioSource.Play();
            }
        }
        catch (
            Exception exception
        )
        {
            Debug.LogError(
                "Could not play voice: " +
                exception.Message
            );
        }
    }


    // =========================================================
    // UNITY AUDIOCLIP -> WAV
    // =========================================================

    private byte[] AudioClipToWav(
        AudioClip clip,
        int recordedSamples
    )
    {
        if (
            clip == null ||
            recordedSamples <= 0
        )
        {
            return null;
        }

        int channels =
            clip.channels;

        int sampleCount =
            recordedSamples *
            channels;

        float[] allSamples =
            new float[
                clip.samples *
                channels
            ];

        clip.GetData(
            allSamples,
            0
        );

        float[] recordedData =
            new float[
                sampleCount
            ];

        Array.Copy(
            allSamples,
            recordedData,
            sampleCount
        );

        const int bitsPerSample =
            16;

        int byteRate =
            clip.frequency *
            channels *
            bitsPerSample /
            8;

        int blockAlign =
            channels *
            bitsPerSample /
            8;

        int dataSize =
            recordedData.Length *
            2;

        using MemoryStream stream =
            new MemoryStream();

        using BinaryWriter writer =
            new BinaryWriter(
                stream
            );

        writer.Write(
            Encoding.ASCII.GetBytes(
                "RIFF"
            )
        );

        writer.Write(
            36 +
            dataSize
        );

        writer.Write(
            Encoding.ASCII.GetBytes(
                "WAVE"
            )
        );

        writer.Write(
            Encoding.ASCII.GetBytes(
                "fmt "
            )
        );

        writer.Write(
            16
        );

        writer.Write(
            (short)1
        );

        writer.Write(
            (short)channels
        );

        writer.Write(
            clip.frequency
        );

        writer.Write(
            byteRate
        );

        writer.Write(
            (short)blockAlign
        );

        writer.Write(
            (short)bitsPerSample
        );

        writer.Write(
            Encoding.ASCII.GetBytes(
                "data"
            )
        );

        writer.Write(
            dataSize
        );

        foreach (
            float sample
            in recordedData
        )
        {
            float clamped =
                Mathf.Clamp(
                    sample,
                    -1f,
                    1f
                );

            short pcm =
                (short)(
                    clamped *
                    32767f
                );

            writer.Write(
                pcm
            );
        }

        writer.Flush();

        return
            stream.ToArray();
    }


    // =========================================================
    // WAV -> UNITY AUDIOCLIP
    // =========================================================

    private AudioClip CreateAudioClipFromWav(
        byte[] wavBytes,
        string clipName
    )
    {
        if (
            wavBytes == null ||
            wavBytes.Length < 44
        )
        {
            Debug.LogError(
                "Invalid WAV data."
            );

            return null;
        }

        using MemoryStream stream =
            new MemoryStream(
                wavBytes
            );

        using BinaryReader reader =
            new BinaryReader(
                stream
            );

        string riff =
            new string(
                reader.ReadChars(4)
            );

        reader.ReadInt32();

        string wave =
            new string(
                reader.ReadChars(4)
            );

        if (
            riff != "RIFF" ||
            wave != "WAVE"
        )
        {
            Debug.LogError(
                "Audio is not WAV."
            );

            return null;
        }

        ushort audioFormat =
            1;

        ushort channels =
            1;

        int sampleRate =
            24000;

        ushort bitsPerSample =
            16;

        byte[] pcmData =
            null;

        while (
            reader.BaseStream.Position <
            reader.BaseStream.Length
        )
        {
            if (
                reader.BaseStream.Length -
                reader.BaseStream.Position <
                8
            )
            {
                break;
            }

            string chunkId =
                new string(
                    reader.ReadChars(4)
                );

            int chunkSize =
                reader.ReadInt32();

            if (
                chunkSize < 0 ||
                reader.BaseStream.Position +
                chunkSize >
                reader.BaseStream.Length
            )
            {
                break;
            }

            if (
                chunkId ==
                "fmt "
            )
            {
                audioFormat =
                    reader.ReadUInt16();

                channels =
                    reader.ReadUInt16();

                sampleRate =
                    reader.ReadInt32();

                reader.ReadInt32();

                reader.ReadUInt16();

                bitsPerSample =
                    reader.ReadUInt16();

                int remaining =
                    chunkSize -
                    16;

                if (remaining > 0)
                {
                    reader.ReadBytes(
                        remaining
                    );
                }
            }
            else if (
                chunkId ==
                "data"
            )
            {
                pcmData =
                    reader.ReadBytes(
                        chunkSize
                    );
            }
            else
            {
                reader.ReadBytes(
                    chunkSize
                );
            }

            if (
                chunkSize % 2 != 0 &&
                reader.BaseStream.Position <
                reader.BaseStream.Length
            )
            {
                reader.ReadByte();
            }
        }

        if (
            pcmData == null ||
            pcmData.Length == 0
        )
        {
            return null;
        }

        if (
            audioFormat != 1 ||
            bitsPerSample != 16
        )
        {
            Debug.LogError(
                "Expected 16-bit PCM WAV."
            );

            return null;
        }

        int sampleCount =
            pcmData.Length /
            2;

        float[] samples =
            new float[
                sampleCount
            ];

        for (
            int i = 0;
            i < sampleCount;
            i++
        )
        {
            short sample =
                BitConverter.ToInt16(
                    pcmData,
                    i * 2
                );

            samples[i] =
                sample /
                32768f;
        }

        int frameCount =
            sampleCount /
            channels;

        AudioClip clip =
            AudioClip.Create(
                clipName,
                frameCount,
                channels,
                sampleRate,
                false
            );

        clip.SetData(
            samples,
            0
        );

        return clip;
    }
}
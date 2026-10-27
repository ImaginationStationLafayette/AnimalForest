import os
import json
import base64
import asyncio

import websockets

from dotenv import load_dotenv
from google import genai
from google.genai import types


# ============================================================
# ENVIRONMENT
# ============================================================

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

load_dotenv(
    os.path.join(BASE_DIR, ".env")
)

API_KEY = os.getenv("GEMINI_API_KEY")

if not API_KEY:
    raise RuntimeError(
        "GEMINI_API_KEY was not found."
    )


# ============================================================
# GEMINI
# ============================================================

MODEL = "gemini-3.1-flash-live-preview"

client = genai.Client(
    api_key=API_KEY
)


# ============================================================
# SYSTEM PROMPT
# ============================================================

def build_system_prompt(data):
    animal = str(
        data.get("animal", "animal")
    )

    species = str(
        data.get("species", "")
    )

    diet = str(
        data.get("diet", "")
    )

    habitat = str(
        data.get("habitat", "")
    )

    fun_fact = str(
        data.get("funFact", "")
    )

    return f"""
You are a friendly educational {animal}
inside an interactive children's animal sanctuary.

The child is between 5 and 10 years old.

TRUSTED INFORMATION

Animal: {animal}
Species: {species}
Diet: {diet}
Habitat: {habitat}
Fun fact: {fun_fact}


RESPONSE STYLE

- Answer the child's question immediately.
- Default to ONE short sentence.
- Use at most TWO short sentences when necessary.
- Never give long speeches.
- Do not repeat the question.
- Do not introduce yourself repeatedly.
- Be cheerful, warm, playful, and easy to understand.
- Stay in character as the {animal}.


ALLOWED TOPICS

- animals
- nature
- habitats
- animal food
- animal behavior
- conservation
- simple child-friendly science


SAFETY

Never:
- request identifying information
- ask where the child lives
- ask what school they attend
- ask for phone numbers or email
- discuss sexual content
- discuss graphic violence
- discuss drugs
- discuss self-harm
- provide medical advice
- discuss politics
- encourage unsafe contact with wildlife


If the child asks something unrelated, say:

"I'm here to talk about animals and nature! What would you
like to know about me?"


Use the trusted facts above whenever relevant.

Do not invent animal facts when you are unsure.
"""


# ============================================================
# LIVE SESSION
# ============================================================

async def animal_live_handler(websocket):
    print("Unity connected.")

    stop_event = asyncio.Event()

    try:
        # ----------------------------------------------------
        # START MESSAGE
        # ----------------------------------------------------

        raw_start = await websocket.recv()
        start_data = json.loads(raw_start)

        if start_data.get("type") != "start":
            await websocket.send(
                json.dumps(
                    {
                        "type": "error",
                        "message": "Expected start message."
                    }
                )
            )
            return

        animal = str(
            start_data.get(
                "animal",
                "Animal"
            )
        )

        voice_name = str(
            start_data.get(
                "voice",
                "Kore"
            )
        ).strip()

        if not voice_name:
            voice_name = "Kore"

        print(
            "Starting Live session for:",
            animal
        )

        print(
            "Using voice:",
            voice_name
        )

        system_prompt = build_system_prompt(
            start_data
        )

        # ----------------------------------------------------
        # GEMINI LIVE CONFIG
        # ----------------------------------------------------

        config = types.LiveConnectConfig(
            response_modalities=[
                types.Modality.AUDIO
            ],

            system_instruction=
                system_prompt,

            speech_config=
                types.SpeechConfig(
                    voice_config=
                        types.VoiceConfig(
                            prebuilt_voice_config=
                                types.PrebuiltVoiceConfig(
                                    voice_name=voice_name
                                )
                        )
                ),

            input_audio_transcription=
                types.AudioTranscriptionConfig(),

            output_audio_transcription=
                types.AudioTranscriptionConfig(),

            realtime_input_config=
                types.RealtimeInputConfig(
                    automatic_activity_detection=
                        types.AutomaticActivityDetection(
                            disabled=False,

                            start_of_speech_sensitivity=
                                types.StartSensitivity
                                .START_SENSITIVITY_HIGH,

                            prefix_padding_ms=150,

                            end_of_speech_sensitivity=
                                types.EndSensitivity
                                .END_SENSITIVITY_HIGH,

                            silence_duration_ms=300,
                        ),

                    activity_handling=
                        types.ActivityHandling
                        .START_OF_ACTIVITY_INTERRUPTS,
                ),
        )

        # ----------------------------------------------------
        # CONNECT TO GEMINI LIVE
        # ----------------------------------------------------

        async with client.aio.live.connect(
            model=MODEL,
            config=config
        ) as session:

            await websocket.send(
                json.dumps(
                    {
                        "type": "ready"
                    }
                )
            )

            print(
                "Gemini Live ready."
            )

            # =================================================
            # UNITY -> GEMINI
            # =================================================

            async def receive_from_unity():
                try:
                    async for raw_message in websocket:
                        message = json.loads(
                            raw_message
                        )

                        message_type = message.get(
                            "type"
                        )

                        # -------------------------------------
                        # MICROPHONE AUDIO
                        # -------------------------------------

                        if message_type == "audio":
                            encoded = message.get(
                                "data",
                                ""
                            )

                            if not encoded:
                                continue

                            pcm_bytes = (
                                base64.b64decode(
                                    encoded
                                )
                            )

                            await session.send_realtime_input(
                                audio=
                                    types.Blob(
                                        data=pcm_bytes,
                                        mime_type=
                                            "audio/pcm;rate=16000"
                                    )
                            )

                        # -------------------------------------
                        # PUSH-TO-TALK RELEASE
                        # -------------------------------------

                        elif message_type == "audio_end":
                            print(
                                "Push-to-talk released."
                            )

                            await session.send_realtime_input(
                                audio_stream_end=True
                            )

                        # -------------------------------------
                        # CLOSE ENTIRE SESSION
                        # -------------------------------------

                        elif message_type == "stop":
                            print(
                                "Unity requested Live stop."
                            )

                            stop_event.set()
                            return

                except websockets.ConnectionClosed:
                    stop_event.set()

                except asyncio.CancelledError:
                    pass

                except Exception as exc:
                    print(
                        "Unity receive error:",
                        repr(exc)
                    )

                    stop_event.set()

            # =================================================
            # GEMINI -> UNITY
            # =================================================

            async def receive_from_gemini():
                try:
                    while not stop_event.is_set():

                        # session.receive() may finish after
                        # one model turn. Re-open it so future
                        # turns continue working.
                        async for message in session.receive():

                            if stop_event.is_set():
                                return

                            server_content = (
                                message.server_content
                            )

                            if server_content is None:
                                continue

                            # ---------------------------------
                            # STREAMED AUDIO
                            # ---------------------------------

                            if (
                                server_content.model_turn
                                is not None
                            ):
                                parts = (
                                    server_content
                                    .model_turn
                                    .parts
                                    or []
                                )

                                for part in parts:
                                    inline_data = (
                                        part.inline_data
                                    )

                                    if inline_data is None:
                                        continue

                                    audio_bytes = (
                                        inline_data.data
                                    )

                                    if not audio_bytes:
                                        continue

                                    encoded = (
                                        base64
                                        .b64encode(
                                            audio_bytes
                                        )
                                        .decode(
                                            "utf-8"
                                        )
                                    )

                                    await websocket.send(
                                        json.dumps(
                                            {
                                                "type":
                                                    "audio",
                                                "data":
                                                    encoded
                                            }
                                        )
                                    )

                            # ---------------------------------
                            # CHILD TRANSCRIPT
                            # ---------------------------------

                            transcription = (
                                server_content
                                .input_transcription
                            )

                            if (
                                transcription is not None
                                and transcription.text
                            ):
                                print(
                                    "Child:",
                                    transcription.text
                                )

                                await websocket.send(
                                    json.dumps(
                                        {
                                            "type":
                                                "user_text",
                                            "text":
                                                transcription.text
                                        }
                                    )
                                )

                            # ---------------------------------
                            # ANIMAL TRANSCRIPT
                            # ---------------------------------

                            output_transcription = (
                                server_content
                                .output_transcription
                            )

                            if (
                                output_transcription is not None
                                and output_transcription.text
                            ):
                                print(
                                    "Animal:",
                                    output_transcription.text
                                )

                                await websocket.send(
                                    json.dumps(
                                        {
                                            "type":
                                                "animal_text",
                                            "text":
                                                output_transcription.text
                                        }
                                    )
                                )

                            # ---------------------------------
                            # INTERRUPTION
                            # ---------------------------------

                            if server_content.interrupted:
                                print(
                                    "Animal response interrupted."
                                )

                                await websocket.send(
                                    json.dumps(
                                        {
                                            "type":
                                                "interrupted"
                                        }
                                    )
                                )

                            # ---------------------------------
                            # TURN COMPLETE
                            # ---------------------------------

                            if server_content.turn_complete:
                                print(
                                    "Gemini turn complete."
                                )

                                await websocket.send(
                                    json.dumps(
                                        {
                                            "type":
                                                "turn_complete"
                                        }
                                    )
                                )

                except asyncio.CancelledError:
                    pass

                except Exception as exc:
                    if not stop_event.is_set():
                        print(
                            "Gemini receive error:",
                            repr(exc)
                        )

                    stop_event.set()

            # =================================================
            # RUN BOTH DIRECTIONS
            # =================================================

            unity_task = asyncio.create_task(
                receive_from_unity()
            )

            gemini_task = asyncio.create_task(
                receive_from_gemini()
            )

            await stop_event.wait()

            unity_task.cancel()
            gemini_task.cancel()

            await asyncio.gather(
                unity_task,
                gemini_task,
                return_exceptions=True
            )

    except websockets.ConnectionClosed:
        print(
            "Unity disconnected."
        )

    except Exception as exc:
        print(
            "Live session error:",
            repr(exc)
        )

        try:
            await websocket.send(
                json.dumps(
                    {
                        "type": "error",
                        "message":
                            "Live conversation failed."
                    }
                )
            )
        except Exception:
            pass

    finally:
        print(
            "Animal Live session ended."
        )


# ============================================================
# SERVER
# ============================================================

async def main():
    print(
        "Animal Live server:"
    )

    print(
        "ws://127.0.0.1:8765"
    )

    async with websockets.serve(
        animal_live_handler,
        "127.0.0.1",
        8765,
        max_size=None
    ):
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(
        main()
    )
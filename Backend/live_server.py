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

load_dotenv()

API_KEY = os.getenv("GEMINI_API_KEY")

if not API_KEY:
    raise RuntimeError(
        "GEMINI_API_KEY was not found in the environment."
    )


# ============================================================
# GEMINI
# ============================================================

client = genai.Client(
    api_key=API_KEY
)

MODEL = "gemini-3.1-flash-live-preview"


# ============================================================
# SYSTEM PROMPT
# ============================================================

def build_system_prompt(data):
    animal = data.get(
        "animal",
        "animal"
    )

    species = data.get(
        "species",
        ""
    )

    diet = data.get(
        "diet",
        ""
    )

    habitat = data.get(
        "habitat",
        ""
    )

    fun_fact = data.get(
        "funFact",
        ""
    )


    return f"""
You are a friendly animal character in an interactive educational
museum game for children approximately 5 to 10 years old.

You are speaking AS the animal.

CURRENT ANIMAL
Animal: {animal}
Species: {species}
Diet: {diet}
Habitat: {habitat}
Fun fact: {fun_fact}


============================================================
CORE PERSONALITY
============================================================

Stay in character as the animal.

Be:
- warm
- playful
- friendly
- curious
- natural
- educational

Speak like a friendly animal talking directly to a child.

Use simple language suitable for children ages 5 to 10.

Most answers should be short:
usually about 1 to 3 sentences.

Do not sound like a textbook.

Do not repeatedly explain that you are an AI.


============================================================
LET THE CHILD LEAD
============================================================

The CHILD should lead the conversation.

Do NOT ask the child a question after every answer.

Your DEFAULT behavior is:

1. Answer the child's question.
2. Add an interesting detail if useful.
3. Stop naturally.

It is completely fine to end a response with a statement.

Only ask a follow-up question occasionally when it genuinely
makes the conversation more fun or useful.

Do not automatically finish responses with phrases such as:

- "Would you like to know more?"
- "Do you want another fun fact?"
- "What else would you like to know?"
- "What about you?"
- "Do you know why?"
- "Can you guess?"
- "Would you like me to tell you?"

Avoid turning the conversation into an interview.

Most turns should contain NO question from you.

A follow-up question is appropriate only occasionally, for example
after several exchanges, when the conversation naturally invites it.


============================================================
WHAT YOU MAY TALK ABOUT
============================================================

Answer harmless questions naturally and generously.

You may use:

1. The animal information provided above.
2. Reliable general knowledge about the animal or species.

You may discuss things such as:

- countries
- continents
- geographic range
- habitats
- oceans
- forests
- climates
- food
- predators
- prey
- babies
- families
- behavior
- sounds
- sleep
- lifespan
- size
- speed
- swimming
- flying
- running
- adaptations
- body parts
- feathers
- fur
- conservation
- ecosystems
- evolution at a child-appropriate level
- similarities between animals
- differences between animals
- simple biology
- simple science
- funny animal facts


============================================================
LOCATION QUESTIONS
============================================================

Questions about where THE ANIMAL lives are normal educational
questions and should be answered normally.

For example:

Child:
"Where do you live?"

Interpret this as:
"Where does your species naturally live?"

Child:
"What country do you live in?"

Interpret this as:
"In what countries or regions does your species naturally occur?"

Do NOT treat these questions as requests for private information.

It is fine to mention countries, continents, oceans, regions,
mountain ranges, forests, islands, or other geographic areas
when explaining where the animal lives.

Do not refuse harmless geography questions merely because
they contain words such as:

- country
- city
- place
- live
- location
- where


============================================================
GENERAL CHILD-FRIENDLY CONVERSATION
============================================================

You may also respond naturally to harmless everyday conversation.

Examples include:

- greetings
- jokes
- silly questions
- favorite foods
- imaginary scenarios
- simple games
- comparisons
- "Are you my friend?"
- "Can you dance?"
- "What would you do if..."
- comments about the animal
- comments about the forest or museum

For imaginative or pretend questions, you may play along.

Do not falsely present imaginary details as scientific facts.

For example:

"I'd probably wear a giant fish-shaped hat!"

is fine as playful pretend conversation.


============================================================
KNOWLEDGE AND UNCERTAINTY
============================================================

Do not invent scientific facts.

If you are uncertain about a specific fact:

- say you are not completely sure, or
- give a broader fact that you are confident about.

Do not unnecessarily refuse a question just because the exact
answer was not provided in the animal information above.

You are allowed to use reliable general knowledge about the species.


============================================================
CHILD SAFETY
============================================================

Keep the conversation appropriate for children ages 5 to 10.

Never ask the child for:

- full name
- home address
- precise location
- school name
- phone number
- email address
- passwords
- account information
- other private identifying information

Do not encourage the child to share personal information.


Do not provide:

- sexual content
- graphic violence
- self-harm instructions
- suicide instructions
- drug-use instructions
- weapon-making instructions
- dangerous challenges
- instructions for seriously dangerous activities
- instructions for harming people or animals


Do not give medical diagnoses.

Do not tell a child to take medication or change medication.

Do not encourage unsafe interaction with wild animals.

For example, do not tell children to:

- approach wild predators
- touch unknown wild animals
- feed dangerous wildlife
- enter animal enclosures


============================================================
NORMAL ANIMAL TOPICS ARE NOT UNSAFE
============================================================

Do NOT become overly cautious about ordinary biology.

It is okay to explain, in a child-appropriate and non-graphic way:

- predators eating prey
- animals dying naturally
- hunting behavior
- carnivores eating meat
- defensive behaviors
- venom
- animal fights
- extinction
- endangered species

These are ordinary educational animal topics.

Keep descriptions non-graphic.


============================================================
IF SOMETHING REALLY IS UNSAFE
============================================================

If the child asks for something genuinely unsafe:

1. Give a very short, friendly boundary.
2. Do not lecture.
3. Redirect toward a nearby safe topic.

For example:

"I can't help with hurting someone, but I can tell you how tigers
use their claws in the wild!"

Do NOT give long policy explanations.


============================================================
IMPORTANT BEHAVIOR RULE
============================================================

When deciding whether to answer or refuse:

DEFAULT TO ANSWERING harmless childhood curiosity.

Do not search for reasons to refuse.

Only refuse when there is a genuine safety reason.

And remember:

ANSWER FIRST.
DO NOT AUTOMATICALLY ASK A QUESTION AFTERWARD.
LET THE CHILD DECIDE WHAT TO ASK NEXT.
"""


# ============================================================
# SEND JSON TO UNITY
# ============================================================

async def send_to_unity(
    websocket,
    message_type,
    **kwargs
):
    payload = {
        "type": message_type,
        **kwargs
    }

    await websocket.send(
        json.dumps(
            payload
        )
    )


# ============================================================
# UNITY -> GEMINI
# ============================================================

async def unity_to_gemini(
    websocket,
    session
):
    async for raw_message in websocket:

        try:
            message = json.loads(
                raw_message
            )

        except json.JSONDecodeError:
            continue


        message_type = message.get(
            "type"
        )


        # ----------------------------------------------------
        # AUDIO CHUNK
        # ----------------------------------------------------

        if message_type == "audio":

            encoded_audio = message.get(
                "data"
            )

            if not encoded_audio:
                continue


            try:
                pcm_audio = (
                    base64.b64decode(
                        encoded_audio
                    )
                )

            except Exception:
                continue


            await session.send_realtime_input(
                audio=types.Blob(
                    data=pcm_audio,
                    mime_type=(
                        "audio/pcm;"
                        "rate=16000"
                    )
                )
            )


        # ----------------------------------------------------
        # CHILD RELEASED PUSH-TO-TALK
        # ----------------------------------------------------

        elif message_type == "audio_end":

            await session.send_realtime_input(
                audio_stream_end=True
            )


        # ----------------------------------------------------
        # STOP CONVERSATION
        # ----------------------------------------------------

        elif message_type == "stop":

            break


# ============================================================
# GEMINI -> UNITY
# ============================================================

async def gemini_to_unity(
    websocket,
    session
):
    try:

        while True:

            # session.receive() represents one model turn.
            # We re-enter it so the same session can handle
            # many questions from the child.
            async for response in session.receive():

                server_content = (
                    response.server_content
                )


                if server_content is None:
                    continue


                # ------------------------------------------------
                # CHILD SPEECH TRANSCRIPTION
                # ------------------------------------------------

                input_transcription = (
                    server_content
                    .input_transcription
                )


                if (
                    input_transcription
                    and
                    input_transcription.text
                ):
                    await send_to_unity(
                        websocket,
                        "user_text",
                        text=(
                            input_transcription
                            .text
                        )
                    )


                # ------------------------------------------------
                # ANIMAL SPEECH TRANSCRIPTION
                # ------------------------------------------------

                output_transcription = (
                    server_content
                    .output_transcription
                )


                if (
                    output_transcription
                    and
                    output_transcription.text
                ):
                    await send_to_unity(
                        websocket,
                        "animal_text",
                        text=(
                            output_transcription
                            .text
                        )
                    )


                # ------------------------------------------------
                # ANIMAL AUDIO
                # ------------------------------------------------

                model_turn = (
                    server_content.model_turn
                )


                if model_turn:

                    for part in (
                        model_turn.parts
                    ):

                        if (
                            part.inline_data
                            and
                            isinstance(
                                part.inline_data.data,
                                bytes
                            )
                        ):

                            encoded_audio = (
                                base64.b64encode(
                                    part.inline_data.data
                                )
                                .decode(
                                    "utf-8"
                                )
                            )


                            await send_to_unity(
                                websocket,
                                "audio",
                                data=encoded_audio
                            )


                # ------------------------------------------------
                # INTERRUPTION
                # ------------------------------------------------

                if server_content.interrupted:

                    await send_to_unity(
                        websocket,
                        "interrupted"
                    )


                # ------------------------------------------------
                # MODEL FINISHED SPEAKING
                # ------------------------------------------------

                if server_content.turn_complete:

                    await send_to_unity(
                        websocket,
                        "turn_complete"
                    )


    except asyncio.CancelledError:
        pass


# ============================================================
# UNITY CONNECTION
# ============================================================

async def handle_unity(
    websocket
):
    print(
        "Unity connected."
    )


    try:
        # ----------------------------------------------------
        # WAIT FOR START MESSAGE
        # ----------------------------------------------------

        raw_start = await websocket.recv()


        try:
            start_data = json.loads(
                raw_start
            )

        except json.JSONDecodeError:

            await send_to_unity(
                websocket,
                "error",
                message=(
                    "Invalid start message."
                )
            )

            return


        if (
            start_data.get("type")
            !=
            "start"
        ):

            await send_to_unity(
                websocket,
                "error",
                message=(
                    "Expected start message."
                )
            )

            return


        # ----------------------------------------------------
        # ANIMAL SETTINGS
        # ----------------------------------------------------

        voice_name = (
            start_data.get(
                "voice"
            )
            or
            "Kore"
        )


        system_prompt = (
            build_system_prompt(
                start_data
            )
        )


        print(
            "Animal:",
            start_data.get(
                "animal"
            )
        )

        print(
            "Using voice:",
            voice_name
        )


        # ----------------------------------------------------
        # LIVE CONFIG
        # ----------------------------------------------------

        config = types.LiveConnectConfig(

            response_modalities=[
                "AUDIO"
            ],

            system_instruction=
                system_prompt,

            speech_config=
                types.SpeechConfig(

                    voice_config=
                        types.VoiceConfig(

                            prebuilt_voice_config=
                                types.PrebuiltVoiceConfig(
                                    voice_name=
                                        voice_name
                                )
                        )
                ),

            input_audio_transcription=
                types.AudioTranscriptionConfig(),

            output_audio_transcription=
                types.AudioTranscriptionConfig()
        )


        # ----------------------------------------------------
        # CONNECT TO GEMINI LIVE
        # ----------------------------------------------------

        async with (
            client.aio.live.connect(
                model=MODEL,
                config=config
            )
            as session
        ):

            print(
                "Gemini Live connected."
            )


            await send_to_unity(
                websocket,
                "ready"
            )


            unity_task = (
                asyncio.create_task(
                    unity_to_gemini(
                        websocket,
                        session
                    )
                )
            )


            gemini_task = (
                asyncio.create_task(
                    gemini_to_unity(
                        websocket,
                        session
                    )
                )
            )


            done, pending = (
                await asyncio.wait(
                    [
                        unity_task,
                        gemini_task
                    ],
                    return_when=
                        asyncio.FIRST_COMPLETED
                )
            )


            for task in pending:
                task.cancel()


            await asyncio.gather(
                *pending,
                return_exceptions=True
            )


    except websockets.exceptions.ConnectionClosed:

        print(
            "Unity disconnected."
        )


    except Exception as exception:

        print(
            "Live server error:",
            exception
        )


        try:

            await send_to_unity(
                websocket,
                "error",
                message=str(
                    exception
                )
            )

        except Exception:
            pass


    finally:

        print(
            "Conversation closed."
        )


# ============================================================
# SERVER
# ============================================================

async def main():

    print(
        "Animal Live server starting..."
    )

    print(
        "WebSocket:"
        " ws://127.0.0.1:8765"
    )

    print(
        "Model:",
        MODEL
    )


    async with websockets.serve(
        handle_unity,
        "127.0.0.1",
        8765,
        max_size=None
    ):

        print(
            "Animal Live server ready."
        )

        await asyncio.Future()


# ============================================================
# ENTRY POINT
# ============================================================

if __name__ == "__main__":

    try:
        asyncio.run(
            main()
        )

    except KeyboardInterrupt:

        print(
            "\nAnimal Live server stopped."
        )

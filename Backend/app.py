import os
import base64

from flask import Flask, request, jsonify
from dotenv import load_dotenv
from google import genai
from google.genai import types


# ---------------------------------------------------------
# ENVIRONMENT
# ---------------------------------------------------------

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

load_dotenv(
    os.path.join(BASE_DIR, ".env")
)

api_key = os.getenv("GEMINI_API_KEY")

if not api_key:
    raise RuntimeError(
        "GEMINI_API_KEY was not found. "
        "Make sure Backend/.env exists."
    )


# ---------------------------------------------------------
# APP / GEMINI
# ---------------------------------------------------------

app = Flask(__name__)

client = genai.Client(
    api_key=api_key
)

TEXT_MODEL = "gemini-3.5-flash-lite"
TTS_MODEL = "gemini-3.8-flash-tts"

SAFE_FALLBACK = (
    "I'm here to talk about animals and nature! "
    "Ask me something about animals."
)


# ---------------------------------------------------------
# ANIMAL ANSWER
# ---------------------------------------------------------

def generate_animal_answer(
    animal,
    species,
    diet,
    habitat,
    fun_fact,
    question
):
    system_prompt = f"""
You are a friendly educational {animal} in a children's
learning game for children ages 5 to 10.

Trusted information about you:
Animal: {animal}
Species: {species}
Diet: {diet}
Habitat: {habitat}
Fun fact: {fun_fact}

Rules:
- Speak as the animal.
- Answer only questions about animals, nature, habitats,
  food, behavior, or simple science.
- Keep answers to 1 to 3 short sentences.
- Use simple language suitable for ages 5 to 10.
- Be warm, playful, and encouraging.
- Use the trusted information above whenever relevant.
- Never ask for the child's name, age, school, address,
  location, phone number, email, or personal information.
- Do not discuss sexual content, graphic violence, drugs,
  self-harm, politics, or medical advice.
- Never encourage dangerous interaction with wild animals.
- If the question is unrelated or inappropriate, reply:
  "I'm here to talk about animals and nature! Ask me something about animals."
- If you are unsure of an animal fact, do not invent it.
"""

    response = client.models.generate_content(
        model=TEXT_MODEL,
        contents=question,
        config=types.GenerateContentConfig(
            system_instruction=system_prompt,
            max_output_tokens=120,
        ),
    )

    answer = (
        response.text or ""
    ).strip()

    if not answer:
        answer = SAFE_FALLBACK

    return answer


# ---------------------------------------------------------
# AUDIO -> QUESTION
# ---------------------------------------------------------

def transcribe_question(
    audio_bytes
):
    response = client.models.generate_content(
        model=TEXT_MODEL,
        contents=[
            (
                "Listen to this child's audio and transcribe "
                "the spoken question. Return ONLY the words "
                "the child said. Do not answer the question. "
                "Do not add quotation marks or commentary."
            ),
            types.Part.from_bytes(
                data=audio_bytes,
                mime_type="audio/wav",
            ),
        ],
        config=types.GenerateContentConfig(
            max_output_tokens=100
        ),
    )

    transcript = (
        response.text or ""
    ).strip()

    return transcript


# ---------------------------------------------------------
# TEXT -> SPEECH
# ---------------------------------------------------------

def generate_speech(
    text
):
    response = client.models.generate_content(
        model=TTS_MODEL,

        contents=[
            {
                "role": "user",
                "parts": [
                    {
                        "text": text,

                        "speech_metadata": {
                            "style":
                                "Warm, cheerful, friendly, playful, "
                                "clear, expressive, and suitable "
                                "for a child ages 5 to 10."
                        },
                    }
                ],
            }
        ],

        config={
            "response_modalities": [
                "AUDIO"
            ],

            "speech_config": {
                "voice_config": {
                    "voice": "Kore"
                }
            },
        },
    )

    audio_data = (
        response
        .candidates[0]
        .content
        .parts[0]
        .inline_data
        .data
    )

    return audio_data


# ---------------------------------------------------------
# TEXT CHAT ENDPOINT
# ---------------------------------------------------------

@app.post("/animal-chat")
def animal_chat():
    data = (
        request.get_json(
            silent=True
        )
        or {}
    )

    animal = str(
        data.get("animal", "")
    ).strip()

    species = str(
        data.get("species", "")
    ).strip()

    diet = str(
        data.get("diet", "")
    ).strip()

    habitat = str(
        data.get("habitat", "")
    ).strip()

    fun_fact = str(
        data.get("funFact", "")
    ).strip()

    question = str(
        data.get("question", "")
    ).strip()

    if not animal or not question:
        return jsonify(
            {
                "answer": SAFE_FALLBACK,
                "audio": None
            }
        ), 400

    question = question[:300]

    try:
        answer = generate_animal_answer(
            animal,
            species,
            diet,
            habitat,
            fun_fact,
            question
        )

        audio_bytes = generate_speech(
            answer
        )

        audio_base64 = (
            base64.b64encode(
                audio_bytes
            )
            .decode("utf-8")
        )

        return jsonify(
            {
                "answer": answer,
                "audio": audio_base64
            }
        )

    except Exception as exc:
        print(
            "animal-chat error:",
            repr(exc)
        )

        return jsonify(
            {
                "answer": SAFE_FALLBACK,
                "audio": None
            }
        ), 200


# ---------------------------------------------------------
# MICROPHONE ENDPOINT
# ---------------------------------------------------------

@app.post("/animal-voice")
def animal_voice():
    data = (
        request.get_json(
            silent=True
        )
        or {}
    )

    animal = str(
        data.get("animal", "")
    ).strip()

    species = str(
        data.get("species", "")
    ).strip()

    diet = str(
        data.get("diet", "")
    ).strip()

    habitat = str(
        data.get("habitat", "")
    ).strip()

    fun_fact = str(
        data.get("funFact", "")
    ).strip()

    audio_base64 = str(
        data.get("audio", "")
    ).strip()

    if not animal or not audio_base64:
        return jsonify(
            {
                "transcript": "",
                "answer": SAFE_FALLBACK,
                "audio": None
            }
        ), 400

    try:
        # Decode Unity microphone WAV.
        audio_bytes = (
            base64.b64decode(
                audio_base64
            )
        )

        # 1. Understand what the child said.
        transcript = transcribe_question(
            audio_bytes
        )

        print(
            "Child said:",
            transcript
        )

        if not transcript:
            return jsonify(
                {
                    "transcript": "",
                    "answer":
                        "I didn't quite hear that. "
                        "Would you like to try again?",
                    "audio": None
                }
            )

        # Limit text passed onward.
        transcript = transcript[:300]

        # 2. Generate safe animal answer.
        answer = generate_animal_answer(
            animal,
            species,
            diet,
            habitat,
            fun_fact,
            transcript
        )

        # 3. Speak it.
        speech_bytes = generate_speech(
            answer
        )

        speech_base64 = (
            base64.b64encode(
                speech_bytes
            )
            .decode("utf-8")
        )

        return jsonify(
            {
                "transcript": transcript,
                "answer": answer,
                "audio": speech_base64
            }
        )

    except Exception as exc:
        print(
            "animal-voice error:",
            repr(exc)
        )

        return jsonify(
            {
                "transcript": "",
                "answer": SAFE_FALLBACK,
                "audio": None
            }
        ), 200


# ---------------------------------------------------------
# STATIC ANSWER -> SPEECH
# ---------------------------------------------------------

@app.post("/animal-speak")
def animal_speak():
    data = (
        request.get_json(
            silent=True
        )
        or {}
    )

    text = str(
        data.get("text", "")
    ).strip()

    if not text:
        return jsonify(
            {
                "audio": None
            }
        ), 400

    # Prevent huge TTS requests.
    text = text[:500]

    try:
        audio_bytes = generate_speech(
            text
        )

        audio_base64 = (
            base64.b64encode(
                audio_bytes
            )
            .decode("utf-8")
        )

        return jsonify(
            {
                "audio": audio_base64
            }
        )

    except Exception as exc:
        print(
            "animal-speak error:",
            repr(exc)
        )

        return jsonify(
            {
                "audio": None
            }
        ), 200


# ---------------------------------------------------------
# START SERVER
# ---------------------------------------------------------

if __name__ == "__main__":
    app.run(
        host="127.0.0.1",
        port=5000,
        debug=True
    )
using System.Collections;
using UnityEngine;


public class ForestAmbienceController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource ambienceSource;


    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField]
    private float normalVolume = 0.12f;


    [Range(0f, 1f)]
    [SerializeField]
    private float listeningVolume = 0.025f;


    [Header("Fade")]
    [SerializeField]
    private float fadeDuration = 0.35f;


    private Coroutine fadeCoroutine;


    private void Awake()
    {
        if (ambienceSource == null)
        {
            ambienceSource =
                GetComponent<AudioSource>();
        }


        if (ambienceSource != null)
        {
            ambienceSource.volume =
                normalVolume;
        }
    }


    public void BeginListening()
    {
        FadeTo(
            listeningVolume
        );
    }


    public void EndListening()
    {
        FadeTo(
            normalVolume
        );
    }


    private void FadeTo(
        float targetVolume
    )
    {
        if (ambienceSource == null)
            return;


        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }


        fadeCoroutine =
            StartCoroutine(
                FadeVolume(
                    targetVolume
                )
            );
    }


    private IEnumerator FadeVolume(
        float targetVolume
    )
    {
        float startVolume =
            ambienceSource.volume;

        float elapsed = 0f;


        while (
            elapsed <
            fadeDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );


            ambienceSource.volume =
                Mathf.Lerp(
                    startVolume,
                    targetVolume,
                    t
                );


            yield return null;
        }


        ambienceSource.volume =
            targetVolume;

        fadeCoroutine =
            null;
    }
}
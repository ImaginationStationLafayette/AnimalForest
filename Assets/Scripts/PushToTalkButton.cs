using UnityEngine;
using UnityEngine.EventSystems;


public class PushToTalkButton :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField]
    private AnimalLiveClient liveClient;


    private void Awake()
    {
        if (liveClient == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (player != null)
            {
                liveClient =
                    player.GetComponent<
                        AnimalLiveClient
                    >();
            }
        }
    }


    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (liveClient != null)
        {
            liveClient
                .BeginPushToTalk();
        }
    }


    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (liveClient != null)
        {
            liveClient
                .EndPushToTalk();
        }
    }


    // If the child's finger slides off the button,
    // treat that as releasing the microphone.
    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        if (liveClient != null)
        {
            liveClient
                .EndPushToTalk();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using SimpleJSON;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EventManager : MonoBehaviour {
    public GameObject textResult;
    public GameObject textInput;
    private string query;
    private int urlOffset = 0;

    public Transform buttonElm;
    public Transform content;
    public Transform MetaInfo;
    public Transform DetailPanel;

    private const float scrollViewHeight = 500.0f;

    private bool isRequesting = false;
    private int numberOfButtonsInScrollView = 0;
    private int numberOfHits = 0;

    string YOUR_APP_ID = "";
    string YOUR_APP_KEY = "";
    const string url_listing = "https://external.api.yle.fi/v1/programs/items.json";
    const string url_listing_item = "https://external.api.yle.fi/v1/programs/items/";

    // Use this for initialization
    void Start () {
        APIConfidentialData data = new APIConfidentialData();
        YOUR_APP_ID = data.getAppId();
        YOUR_APP_KEY = data.getAppKey();
	}

    public void CallInputEnd()
    {
        Debug.Log("input end");
        query = textInput.GetComponent<Text>().text;
        Refresh();
        StartCoroutine(RequestData(null));
    }

    string GenerateUrl(bool isList, string id)
    {
        string url = (isList) ?
            url_listing + "?q=" + query + "&offset=" + urlOffset + "&limit=10&app_id=" + YOUR_APP_ID + "&app_key=" + YOUR_APP_KEY :
            url_listing_item + id + ".json?app_id=" + YOUR_APP_ID + "&app_key=" + YOUR_APP_KEY;
        return url;
    }

    IEnumerator RequestData(string id)
    {
        isRequesting = true;
        using (UnityWebRequest www = (id == null) ? UnityWebRequest.Get(GenerateUrl(true, null)) : UnityWebRequest.Get(GenerateUrl(false, id)))
        {
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                var N = JSON.Parse(www.downloadHandler.text);
                if (id == null)
                {
                    AppUpdate(N);
                }
                else
                {
                    DisplayDetails(N);
                }
                isRequesting = false;
            }
        }
    }

    void AppUpdate(SimpleJSON.JSONNode N)
    {
        numberOfHits = N["meta"]["program"].AsInt;
        string results = numberOfHits.ToString();
        UpdateMetaInfoView(results);
        int listLength = (numberOfHits - urlOffset < 10) ? numberOfHits - urlOffset : 10;
        string[] list = new string[listLength];
        list = GetList(N, listLength, true);
        string[] idList = new string[listLength];
        idList = GetList(N, listLength, false);
        UpdateScrollView(list, idList);
        numberOfButtonsInScrollView += listLength;
    }

    void Refresh()
    {
        urlOffset = 0;
        numberOfButtonsInScrollView = 0;
        numberOfHits = 0;
        content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1200);
        foreach (Transform child in content.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    string GetFinnishTitle(int n, SimpleJSON.JSONNode Node)
    {
        return Node["data"][n]["itemTitle"]["fi"].Value;
    }

    string GetProgramID(int n, SimpleJSON.JSONNode Node)
    {
        return Node["data"][n]["id"].Value;
    }

    string[] GetList(SimpleJSON.JSONNode Node, int length, bool isTitle)
    {
        string[] list = new string[length];
        for (int i = 0; i < length; i++)
        {
            list[i] = (isTitle) ? GetFinnishTitle(i, Node) : GetProgramID(i, Node);
        }
        return list;
    }

    void UpdateScrollView(string[] list, string[] idList)
    {
        if (urlOffset != 0)
        {
            Vector2 currentSizeDelta = content.GetComponent<RectTransform>().sizeDelta;
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(currentSizeDelta.x, currentSizeDelta.y + 1000);
        }

        for (int i = 0; i < list.Length; i++)
        {
            Transform temp = Instantiate(buttonElm, content);
            //TODO: seem to set 0.0f to pos.x not working. then get "currentPos.x" from the "localPosition" for the sake of it.
            Vector3 currentPos = temp.transform.localPosition;
            temp.GetComponent<RectTransform>().transform.localPosition = new Vector3(currentPos.x, (i + numberOfButtonsInScrollView) * -100, 0.0f);
            temp.GetComponentInChildren<Text>().text = list[i];
            temp.name = idList[i];
            temp.GetComponent<Button>().onClick.AddListener(ShowDetails);
        }
    }

    void UpdateMetaInfoView(string results)
    {
        MetaInfo.GetComponent<Text>().text = results + " programs found.";
    }

    void DisplayDetails(SimpleJSON.JSONNode N)
    {
        string title, description, typeMedia, event_service, event_publisher, event_starttime;
        title =         "title: " +         N["data"]["title"]["fi"].Value;
        description =   "description: " +   N["data"]["description"]["fi"].Value;
        typeMedia =     "type media: " +    N["data"]["video"]["typeMedia"].Value;
        event_service = "event service: " + N["data"]["publicationEvent"][0]["service"]["id"].Value;
        event_publisher = "event publisher: " +  N["data"]["publicationEvent"][0]["publisher"][0]["id"].Value;
        event_starttime = "event start time: " + N["data"]["publicationEvent"][0]["startTime"].Value;
        string output = title + "\n" + description + "\n" + typeMedia + "\n" + event_service + "\n" + event_publisher + "\n" + event_starttime;
        Debug.Log(output);
        DetailPanel.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        textResult.GetComponent<Text>().text = output;
    }

    public void CheckIfCallUpdate()
    {
        if (isRequesting)
        {
            Debug.Log("requesting...");
            return;
        }
        float yPos = content.transform.localPosition.y;
        if (content.GetComponent<RectTransform>().rect.height - yPos < scrollViewHeight)
        {
            // check if buttons could be appended more, otherwise no request called
            if ((numberOfHits - numberOfButtonsInScrollView) > 0)
            {
                urlOffset += 10;
                StartCoroutine(RequestData(null));
            }
        }
    }

    public void ShowDetails()
    {
        string id = EventSystem.current.currentSelectedGameObject.name;
        StartCoroutine(RequestData(id));
    }

    public void ClosePanel()
    {
        DetailPanel.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
        textResult.GetComponent<Text>().text = "";
    }
}

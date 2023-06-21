using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Linq;
using System;

public class SimpleCloudRecoEventHandler : MonoBehaviour, IObjectRecoEventHandler
{
    CloudRecoBehaviour mCloudRecoBehaviour;
    public ImageTargetBehaviour imageTargetBehaviour;
    string mTargetMetadata = "";
    public Text ErrorTxt;
    string errorTitle, errorMsg;
    private ObjectTracker mObjectTracker;
    public GameObject rescanBtn;
    GameObject mainPlayer;
    GameObject newImageTarget;

    //GameObject MainPlayer;

    public void OnInitialized(TargetFinder cloudRecoBehaviour)
    {
        Debug.Log("Cloud Reco initialized");
        mObjectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
    }

    public void OnInitError(TargetFinder.InitState initError)
    {
        Debug.Log("On Init Error");
        Global.is_error = true;
        Global.is_marker_detected = false;
        Global.curMarkerName = "";
        switch (initError)
        {
            case TargetFinder.InitState.INIT_ERROR_NO_NETWORK_CONNECTION:
                errorTitle = "Network Unavailble";
                errorMsg = "Check internet connection and try again";
                break;
            case TargetFinder.InitState.INIT_ERROR_SERVICE_NOT_AVAILABLE:
                errorTitle = "Service not availble";
                errorMsg = "Failed to initialize beacause service is unavailble";
                break;
        }
        errorMsg = "<color=red>" + initError.ToString().Replace("_", " ") + "</color>\n\n" + errorMsg;
        ErrorTxt.text = "Cloud Reco - Update Error: " + initError + "\n\n" + errorMsg;
    }

    public void NoResult()
    {
        Destroy(mainPlayer);
        mainPlayer = null;
    }

    IEnumerator playVideo()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("start play video : video_url = " + Global.video_url);
        while(Global.video_url == "")
        {
            yield return new WaitForEndOfFrame();
        }
        Hide(mainPlayer, true);
        mainPlayer.GetComponent<VideoPlayer>().isLooping = true;
        mainPlayer.GetComponent<VideoPlayer>().url = Global.video_url.Trim();
        mainPlayer.GetComponent<VideoPlayer>().Prepare();
        while (mainPlayer && !mainPlayer.GetComponent<VideoPlayer>().isPrepared)
        {
            yield return new WaitForEndOfFrame();
        }
        mainPlayer.GetComponent<VideoPlayer>().Play();
        //mainPlayer.GetComponent<MediaPlayerCtrl>().m_strFileName = Global.video_url.Trim();
    }

    public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult)
    {
        Debug.Log("CloudReco: new search result available: " + targetSearchResult.TargetName);
        TargetFinder.CloudRecoSearchResult cloudRecoSearchResult = (TargetFinder.CloudRecoSearchResult)targetSearchResult;
        newImageTarget = Instantiate(imageTargetBehaviour.gameObject) as GameObject;
        mainPlayer = newImageTarget.transform.GetChild(0).gameObject;
        //MainPlayer = newImageTarget.transform.GetChild(0).gameObject;
        GameObject augmentation = null;
        Global.curMarkerName = targetSearchResult.TargetName;
        Debug.Log("curMarkerName = " + Global.curMarkerName);
        Global.is_marker_detected = true;
        if (augmentation != null)
        {
            augmentation.transform.SetParent(newImageTarget.transform);
        }

        if (imageTargetBehaviour)
        {
            List<TargetFinder> convertedTargetFinders = mObjectTracker.GetTargetFinders().ToList();
            if (convertedTargetFinders.Count() > 0)
            {
                ImageTargetBehaviour ImageTargetBehaviour = (ImageTargetBehaviour)convertedTargetFinders[0].EnableTracking(targetSearchResult, newImageTarget);
            }
            //string URL = cloudRecoSearchResult.MetaData;
            //MainPlayer.GetComponent<VideoPlayer>().url = URL.Trim();
        }
        StartCoroutine(playVideo());
        mCloudRecoBehaviour.CloudRecoEnabled = false;
    }

    public void OnStateChanged(bool scanning)
    {
        Debug.Log("marker detect state changed : " + scanning);
        try
        {
            if (scanning)
            {
                rescanBtn.SetActive(false);
                List<TargetFinder> convertedTargetFinders = mObjectTracker.GetTargetFinders().ToList();
                for (int i = 0; i < convertedTargetFinders.Count(); i++)
                {
                    convertedTargetFinders[i].ClearTrackables(false);
                }
            }
            else
            {
                rescanBtn.SetActive(true);
            }
        }
        catch(Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void OnUpdateError(TargetFinder.UpdateState updateError)
    {
        Global.is_error = true;
        Global.is_marker_detected = false;
        Global.curMarkerName = "";
        switch (updateError)
        {
            case TargetFinder.UpdateState.UPDATE_ERROR_AUTHORIZATION_FAILED:
                errorTitle = "Authorization Error";
                errorMsg = "The cloud server access keys are invalid";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_NO_NETWORK_CONNECTION:
                errorTitle = "Network Error";
                errorMsg = "Check Internet Connection";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_PROJECT_SUSPENDED:
                errorTitle = "Authorization Error";
                errorMsg = "The project has been suspended";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_REQUEST_TIMEOUT:
                errorTitle = "Request Timeout";
                errorMsg = "The request has timed out";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_SERVICE_NOT_AVAILABLE:
                errorTitle = "Service Unavailble";
                errorMsg = "The service is unavailble";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_TIMESTAMP_OUT_OF_RANGE:
                errorTitle = "Clock Sync Error";
                errorMsg = "Update date and time";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_UPDATE_SDK:
                errorTitle = "Unsupported version";
                errorMsg = "Update SDK";
                break;
            case TargetFinder.UpdateState.UPDATE_ERROR_BAD_FRAME_QUALITY:
                errorTitle = "Bad Frame";
                errorMsg = "Low Frame Quality";
                break;

        }
        errorMsg = "<color=red>" + updateError.ToString().Replace("_", " ") + "</color>\n\n" + errorMsg;
        ErrorTxt.text = "Cloud Reco - Update Error: " + updateError + "\n\n" + errorMsg;
    }

    void Update()
    {
        if(newImageTarget != null)
        {
            if (Global.is_shared)
            {
                newImageTarget.SetActive(false);
            }
            else
            {
                newImageTarget.SetActive(true);
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        Debug.Log("Start simple cloud reco event handler");
        mCloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();
        if (mCloudRecoBehaviour)
        {
            mCloudRecoBehaviour.RegisterEventHandler(this);
        }
        mainPlayer = GameObject.Find("videoPlayer");
    }

    public void Rescan()
    {
        Destroy(newImageTarget);
        newImageTarget = null;
        mCloudRecoBehaviour.CloudRecoEnabled = true;
        Global.is_marker_detected = false;
        Global.curMarkerName = "";
    }

    void Hide(GameObject ob, bool status = false)
    {
        Renderer[] rends = ob.GetComponentsInChildren<Renderer>();
        Collider[] cols = ob.GetComponentsInChildren<Collider>();

        foreach (var item in rends)
        {
            item.enabled = status;
        }
        foreach (var item in cols)
            item.enabled = status;
    }
}

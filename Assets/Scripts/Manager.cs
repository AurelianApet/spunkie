using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.UI;
using static InAppBrowser;
using System.IO;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Android;
using UnityEngine.Video;
using Vuforia;

public class Manager : MonoBehaviour
{
    public GameObject mainObj;
    public GameObject menuObj;

    //main scene
    public GameObject artParent;
    public GameObject artItem;
    public GameObject artSlider;
    public Text authorInfoTxt;
    public Text artistNameTxt;
    public Text artNoTxt;
    public GameObject artInfoObj;
    public GameObject ErrorTxtObj;
    public GameObject InitPopup;
    public GameObject photoCaptureBtn;
    public GameObject videoCaptureBtn;
    public GameObject menuBtn;
    public GameObject linkBtn;
    public GameObject recordBtn;
    public GameObject sharePanel;
    public GameObject capturedImageObj;
    public GameObject capturedVideoObj;
    //public GameObject topBackMenu;

    public GameObject gra_home_top;
    public GameObject gra_home_bottom;
    public GameObject gra_col_top;
    public GameObject gra_col_bottom;

    //menu scene
    public GameObject collectionItemPrefab;
    public GameObject collectionParent;
    public ScrollRect contentRect;

    //error
    public GameObject errorPopup;
    public GameObject errorPopup1;
    public Text errmsg;
    public Text errmsg1;

    GameObject[] artobj_list;
    List<GameObject> collection_list = new List<GameObject>();
    ArtInfo curArtInfo = new ArtInfo();
    int curSelectedIndex = -1;

    //video, image capture
    public GameObject videoRecordPanel;
    public ReplayCam replayCam;
    public GameObject alertObj;
    public Text alertContentTxt;
    public GameObject titleObj;
    public Text timeObj;

    public UnityEngine.UI.Image recanImage;

    private float easing = 0.2f;
    private float delay_gradient_delay_time = 0.1f;

    AudioSource audio;
    Vector3 initMenuObjPos;
    string share_file_path = "";
    bool share_type = false;//image, true-video
    string[] errorTxt = {
        "서버에 연결할 수 없습니다.\n일시적인 현상이니 잠시 후 다시 시도해주세요.",
        "일시적인 오류입니다.\n재시도를 눌러 다시 전송을 시도할 수 있습니다."
    };
    int scene_type = 0;//0-main, 1-collection
    bool detect_start = false;
    bool marker_lost_start = false;
    bool onCheck = false;

    //for camera flash
    public UnityEngine.UI.Image backImg;
    float flashTimelength = 0.2f;
    private float startFlashTime;

    Vector3 initCollectionParentPosition;
    bool is_art_changing = false;

    void Awake()
    {
#if UNITY_IPHONE
		Global.prePath = @"file://";
#elif UNITY_ANDROID
        Global.prePath = @"file:///";
        if (onCheck == false)
        {
            StartCoroutine("PermissionCheckCoroutine");
        }
#else
		Global.prePath = @"file://";
#endif
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.fullScreen = true;
#if UNITY_ANDROID
        Global.setStatusBarValue(2048);
#endif
        audio = GetComponent<AudioSource>();
        InitPopup.SetActive(true);
        videoRecordPanel.SetActive(false);
        alertObj.SetActive(false);
        Global.is_shared = false;
        sharePanel.SetActive(false);
        Global.imgPath = System.IO.Path.Combine(Application.persistentDataPath, "spunkie");
    }

    IEnumerator PermissionCheckCoroutine()
    {
        onCheck = true;

        yield return new WaitForEndOfFrame();
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) == false)
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);

            yield return new WaitForSeconds(0.2f); // 0.2초의 딜레이 후 focus를 체크하자.
            yield return new WaitUntil(() => Application.isFocused == true);

            if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) == false)
            {
                // 다이얼로그를 위해 별도의 플러그인을 사용했기 때문에 주석처리. 그냥 별도의 UI를 만들어주면 됨.
                //AGAlertDialog.ShowMessageDialog("권한 필요", "스크린샷을 저장하기 위해 저장소 권한이 필요합니다.",
                //"Ok", () => OpenAppSetting(),
                //"No!", () => AGUIMisc.ShowToast("저장소 요청 거절됨"));

                OpenAppSetting(); // 원래는 다이얼로그 선택에서 Yes를 누르면 호출됨.

                onCheck = false;
                yield break;
            }
        }

        // 권한을 사용해서 처리하는 부분. 스크린샷이나, 파일 저장 등등.

        onCheck = false;
    }

    private void OpenAppSetting()
    {
        try
        {
#if UNITY_ANDROID
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string packageName = currentActivityObject.Call<string>("getPackageName");

                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null))
                using (var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject))
                {
                    intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                    intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
                    currentActivityObject.Call("startActivity", intentObject);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    void HideUI(bool status)
    {
        titleObj.SetActive(status);
        artInfoObj.SetActive(status);
        artSlider.SetActive(status);
        photoCaptureBtn.SetActive(status);
        videoCaptureBtn.SetActive(status);
        menuBtn.SetActive(status);
        linkBtn.SetActive(status);
        InitPopup.SetActive(status);
    }

    // Start is called before the first frame update
    void Start()
    {
        initMenuObjPos = menuObj.transform.position;
        initCollectionParentPosition = collectionParent.transform.position;
        GetArtlist();
        StartCoroutine(showHomeGradient());
    }

    IEnumerator showHomeGradient()
    {
        yield return new WaitForSeconds(delay_gradient_delay_time);
        gra_home_top.SetActive(true);
        gra_home_bottom.SetActive(true);
        InitPopup.SetActive(true);
    }

    void MoveScrollView(ScrollRect origin, GameObject target, int type = 1/*1-x, 0-y*/)
    {
        RectTransform contentRT = origin.content.GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();
        Vector2 offset = (Vector2)origin.transform.InverseTransformPoint(contentRT.position)
            - (Vector2)origin.transform.InverseTransformPoint(target.transform.position);
        Vector2 anchor = contentRT.anchoredPosition;
        if(type == 1)
        {
            //x
            float width = target.transform.GetComponent<RectTransform>().rect.width / 2;
            anchor.x = offset.x - width;
        }
        else
        {
            //y
            float height = target.transform.GetComponent<RectTransform>().rect.height / 2;
            anchor.y = offset.y - height;
        }
        contentRT.anchoredPosition = anchor;
    }

    void GetArtlist()
    {
        Debug.Log("get marker list");
        Debug.Log(Global.api_url + Global.get_markerlist_api);
        WWWForm form = new WWWForm();
        WWW www = new WWW(Global.api_url + Global.get_markerlist_api, form);
        StartCoroutine(LoadArtlist(www));
    }

    IEnumerator LoadArtlist(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            //Debug.Log("result = " + jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode art_list = JSON.Parse(jsonNode["markerlist"].ToString()/*.Replace("\"", "")*/);
                Global.artInfoList = new ArtInfo[art_list.Count];
                for (int i = 0; i < art_list.Count; i++)
                {
                    ArtInfo artInfo = new ArtInfo();
                    artInfo.id = art_list[i]["id"].AsInt;
                    //Debug.Log(i + "," + artInfo.id);
                    artInfo.name = art_list[i]["name"];
                    artInfo.inapp_marker_cnt = art_list[i]["inapp_marker_cnt"].AsInt;
                    artInfo.no = art_list[i]["no"];
                    artInfo.contentTitle = art_list[i]["content_title"];
                    artInfo.artist1 = art_list[i]["artist1"];
                    artInfo.artist2 = art_list[i]["artist2"];
                    artInfo.inapp_content_cnt = art_list[i]["inapp_content_cnt"].AsInt;
                    if(art_list[i]["video_url"] != "")
                    {
                        artInfo.video_url = Global.api_url + art_list[i]["video_url"];
                    }
                    else
                    {
                        artInfo.video_url = "";
                    }
                    artInfo.content_url = art_list[i]["content_url"];
                    artInfo.thumbnail = Global.api_url + art_list[i]["thumbnail"];
                    artInfo.gif_url = Global.api_url + art_list[i]["gif_url"];
                    artInfo.show_video_cnt = art_list[i]["show_video_cnt"].AsInt;
                    Global.artInfoList[i] = artInfo;
                }
                StartCoroutine(DrawArtList());
            }
        }
        else
        {
            errmsg.text = errorTxt[0];
            errorPopup.SetActive(true);
        }
    }

    IEnumerator DrawArtList()
    {
        while (artParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(artParent.transform.GetChild(0).gameObject));
        }
        while (artParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        int cnt = Global.artInfoList.Length;
        artobj_list = new GameObject[cnt];
        for (int i = 0; i < cnt; i++)
        {
            artobj_list[i] = Instantiate(artItem);
            artobj_list[i].transform.SetParent(artParent.transform);
            artobj_list[i].transform.localScale = Vector3.one;
            //float h = artParent.transform.parent.transform.parent.GetComponent<RectTransform>().rect.height;
            ////Debug.Log("art height = " + h);
            //artobj_list[i].transform.GetComponent<RectTransform>().sizeDelta = new Vector2(h, h);

            string path = Path.Combine(Global.imgPath, Path.GetFileName(Global.artInfoList[i].thumbnail));
            StartCoroutine(downloadImage(Global.artInfoList[i].thumbnail, path, artobj_list[i].transform.Find("image").gameObject));
            if(i == curSelectedIndex)
            {
                artobj_list[i].transform.localScale = Vector3.one;
            }
            else
            {
                artobj_list[i].transform.localScale = new Vector3(48/78f, 48/78f, 1);
            }
            //m_artObj.transform.GetComponent<Button>().onClick.RemoveAllListeners();
            //int _i = i;
            //m_artObj.transform.GetComponent<Button>().onClick.AddListener(delegate () { SelectArt(_i); });
        }
        int random_index = UnityEngine.Random.Range(0, cnt - 1);
        Debug.Log("Rand:" + random_index);
        SelectArt(random_index);
    }

    private static void Rotate<T>(ref T[] array, int shiftCount)
    {
        T[] backupArray = new T[array.Length];
        for (int index = 0; index < array.Length; index++)
        {
            backupArray[(index + array.Length + shiftCount % array.Length) % array.Length] =
              array[index];
        }
        array = backupArray;
    }

    void Reposition()
    {
        int cnt = Global.artInfoList.Length;
        int center = (cnt - 1) / 2;
        int off = center - curSelectedIndex;
        Rotate(ref Global.artInfoList, off);
        curSelectedIndex += off;
        StartCoroutine(DrawArtList());
    }

    public void SelectArt(int index)
    {
        try
        {
            //ScrollRect scrollRect = artSlider.GetComponent<ScrollRect>();
            //MoveScrollView(scrollRect, artobj_list[index], 1);
            curArtInfo = Global.artInfoList[index];
            curSelectedIndex = index;
            Debug.Log("curSel:" + curSelectedIndex);
            //artNoTxt.text = Global.GetNoFormat(curArtInfo.no);
            authorInfoTxt.text = /*"," + */curArtInfo.contentTitle;
            if(curArtInfo.artist2 != "" && curArtInfo.artist2 != "")
            {
                artistNameTxt.text = curArtInfo.artist1 + " x " + curArtInfo.artist2;
            }
            else
            {
                artistNameTxt.text = curArtInfo.artist1;
            }
            //Reposition();
            RectTransform targetObj = artParent.transform.GetChild(curSelectedIndex).GetComponent<RectTransform>();
            artSlider.GetComponent<ScrollRect>().FocusOnItem(targetObj);
            changeArtType();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator SetEffect()
    {
        Color topLeftColor = artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_topLeftColor;
        Color topRightColor = artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_topRightColor;
        Color bottomLeftColor = artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_bottomLeftColor;
        Color bottomRightColor = artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_bottomRightColor;
        bool topLeftRDirection = false;//down
        bool topLeftGDirection = true;//up
        bool topRightGDirection = false;//down
        bool topRightBDirection = true;//up
        bool bottomRightRDirection = true;//up
        bool bottomLeftBDirection = false;//down

        float step = Time.deltaTime * 3;
        while (Global.is_marker_detected)
        {
            if(topLeftRDirection)
            {
                topLeftColor.r += step;
            }
            else
            {
                topLeftColor.r -= step;
            }
            if(topLeftGDirection)
            {
                topLeftColor.g += step;
            }
            else
            {
                topLeftColor.g -= step;
            }

            if (topRightGDirection)
            {
                topRightColor.g += step;
            }
            else
            {
                topRightColor.g -= step;
            }
            if (topRightBDirection)
            {
                topRightColor.b += step;
            }
            else
            {
                topRightColor.b -= step;
            }

            if (bottomRightRDirection)
            {
                bottomRightColor.r += step;
            }
            else
            {
                bottomRightColor.r -= step;
            }

            if (bottomLeftBDirection)
            {
                bottomLeftColor.b += step;
            }
            else
            {
                bottomLeftColor.b -= step;
            }

            if(topLeftRDirection && topLeftColor.r >= 1f)
            {
                topLeftRDirection = false;//down
            }
            if (!topLeftRDirection && topLeftColor.r <= 0f)
            {
                topLeftRDirection = true;//up
            }

            if (topLeftGDirection && topLeftColor.g >= 1f)
            {
                topLeftGDirection = false;//down
            }
            if (!topLeftGDirection && topLeftColor.g <= 0f)
            {
                topLeftGDirection = true;//up
            }

            if (topRightGDirection && topRightColor.g >= 1f)
            {
                topRightGDirection = false;//down
            }
            if (!topRightGDirection && topRightColor.g <= 0f)
            {
                topRightGDirection = true;//up
            }

            if (topRightBDirection && topRightColor.b >= 1f)
            {
                topRightBDirection = false;//down
            }
            if (!topRightBDirection && topRightColor.b <= 0f)
            {
                topRightBDirection = true;//up
            }

            if (bottomRightRDirection && bottomRightColor.r >= 1f)
            {
                bottomRightRDirection = false;//down
            }
            if (!bottomRightRDirection && bottomRightColor.r <= 0f)
            {
                bottomRightRDirection = true;//up
            }

            if (bottomLeftBDirection && bottomLeftColor.b >= 1f)
            {
                bottomLeftBDirection = false;//down
            }
            if (!bottomLeftBDirection && bottomLeftColor.b <= 0f)
            {
                bottomLeftBDirection = true;//up
            }

            artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_topLeftColor = topLeftColor;
            artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_topRightColor = topRightColor;
            artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_bottomLeftColor = bottomLeftColor;
            artInfoObj.transform.Find("effect").GetComponent<UICornersGradient>().m_bottomRightColor = bottomRightColor;
            yield return new WaitForSeconds(0.05f);
        }
        if (!Global.is_marker_detected)
        {

        }
    }

    public void CaptureCamera()
    {
        try
        {
            HideUI(false);

            Color r = recanImage.color;
            r.a = 0f;
            recanImage.color = r;

            //SwitchFlashTorch(true);
            StartCoroutine(TakeScreenshotAndSave());
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void CaptureVideo()
    {
        HideUI(false);

        Color r = recanImage.color;
        r.a = 0f;
        recanImage.color = r;

        videoRecordPanel.SetActive(true);
        replayCam.CaptureStart();
        is_capturing_video = true;
        StartCoroutine(CountTime());
    }

    bool is_capturing_video = false;
    int video_capture_time = 0;

    string showTime(int time)
    {
        string stime = "";
        int min = time / 60;
        if(min > 60)
        {
            min = min % 60;
        }
        int sec = time % 60;
        stime = string.Format("{0:D2}", min) + ":" + string.Format("{0:D2}", sec);
        if(time == 180)
        {
            Btn_Capture_Video_Save();
        }
        else
        {
            recordBtn.GetComponent<UnityEngine.UI.Image>().fillAmount = (time % 180) / 180.0f;
        }
        return stime;
    }

    IEnumerator CountTime()
    {
        video_capture_time = 0;
        while (is_capturing_video)
        {
            timeObj.text = showTime(video_capture_time);
            yield return new WaitForSeconds(1.0f);
            video_capture_time++;
        }
    }

    public void Btn_Capture_Video_Save()
    {
        replayCam.SetSave(true);
        is_capturing_video = false;
        replayCam.CaptureStop();
        videoRecordPanel.SetActive(false);
    }

    public void Btn_Close_VideoCapture()
    {
        replayCam.SetSave(false);
        is_capturing_video = false;
        replayCam.CaptureStop();
        videoRecordPanel.SetActive(false);
    }

    void ShowAlert(string content)
    {
        alertContentTxt.text = content;
        alertObj.SetActive(true);
        StartCoroutine(stopAlert());
    }

    IEnumerator stopAlert()
    {
        yield return new WaitForSeconds(5f);
        alertObj.SetActive(false);
    }

    void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
    }

    Texture2D ss = null;
    private IEnumerator TakeScreenshotAndSave()
    {
        audio.Play();
        yield return new WaitForEndOfFrame();
        Color col = backImg.color;
        startFlashTime = Time.time;
        col.a = 1.0f;
        backImg.color = col;
        bool done = false;
        while (!done)
        {
            float perc;
            perc = Time.time - startFlashTime;
            perc = perc / flashTimelength;

            if (perc > 1.0f)
            {
                perc = 1.0f;
                done = true;
            }
            col.a = Mathf.Lerp(1.0f, 0.0f, perc);
            backImg.color = col;
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();

        ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        share_type = false;
        var sshotName = string.Format("screenshot_{0}.png", System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff"));
        // Save the screenshot to Gallery/Photo
        string spath = "";
        NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(ss, "spunkie", sshotName, (success, path)
            =>
        {
            spath = path;
            Debug.Log("saved image to " + spath);
            ShowAlert("사진이 저장되었습니다.");
            //SwitchFlashTorch(false);
            Global.is_shared = true;
            artInfoObj.SetActive(false);
            artSlider.SetActive(false);
            sharePanel.SetActive(true);
            capturedImageObj.SetActive(true);
            capturedVideoObj.SetActive(false);
            capturedImageObj.GetComponent<RawImage>().texture = ss;
        });
    }

    void DestroyTempFiles()
    {
        try
        {
            if (ss != null)
            {
                Destroy(ss);
                ss = null;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void share()
    {
        if (!share_type)
        {
            //image share
            new NativeShare().AddFile(ss)
                .SetSubject("From SpunkieAr").SetText("Share Screenshot")
                .SetUrl("https://github.com/yasirkula/UnityNativeShare")
                .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + ", selected app: " + shareTarget))
                .Share();
            //Share on WhatsApp only, if installed(Android only)
//#if UNITY_ANDROID
//            if (NativeShare.TargetExists("com.whatsapp"))
//                    new NativeShare().AddFile(share_file_path).AddTarget("com.whatsapp").Share();
//#endif
        }
        else
        {
            //video share
            Debug.Log("share path = " + share_file_path);
            new NativeShare().AddFile(share_file_path)
                .SetSubject("From SpunkieAr").SetText("Share Recorded Video")
                .SetUrl("https://github.com/yasirkula/UnityNativeShare")
                .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + ", selected app: " + shareTarget))
                .Share();
            //Share on WhatsApp only, if installed(Android only)
//#if UNITY_ANDROID
//            if (NativeShare.TargetExists("com.whatsapp"))
//                new NativeShare().AddFile(share_file_path).AddTarget("com.whatsapp").Share();
//#endif
        }
    }

    public void SaveCaptureVideo(string recordingPath)
    {
        var sshotName = string.Format("record_{0}.mp4", System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff"));
        NativeGallery.Permission permission = NativeGallery.SaveVideoToGallery(recordingPath, "spunkie", sshotName, (success, path) =>
        {
            share_file_path = path;
            ShowAlert("동영상이 저장되었습니다.");
            artSlider.SetActive(false);
            sharePanel.SetActive(true);
            capturedImageObj.SetActive(false);
            capturedVideoObj.SetActive(true);
            capturedVideoObj.transform.localScale = Vector3.one;
            Debug.Log("captured video to " + share_file_path);
            share_type = true;
            Global.is_shared = true;
            string sPath = Global.prePath + share_file_path;
            sPath = sPath.Replace(@"\\\\", @"\\\");
            Debug.Log("video path:" + sPath);
            capturedVideoObj.GetComponent<MediaPlayerCtrl>().m_strFileName = sPath;
            capturedVideoObj.GetComponent<MediaPlayerCtrl>().Play();
            if (recordingPath != "")
            {
                File.Delete(recordingPath);
            }
            artInfoObj.SetActive(false);
        });
    }

    public void DiscardCaptureVideo(string recordingPath)
    {
        File.Delete(recordingPath);
        videoRecordPanel.SetActive(false);
    }

    public void GotoUrl()
    {
        Debug.Log("I clicked");
        if (curArtInfo.content_url != null && curArtInfo.content_url != "")
        {
            Debug.Log("url:" + curArtInfo.content_url);
            OpenInAppBrowser(curArtInfo.content_url);
            WWWForm form = new WWWForm();
            form.AddField("marker_id", curArtInfo.id);
            form.AddField("cnt", curArtInfo.inapp_content_cnt);
            WWW www = new WWW(Global.api_url + Global.set_contentCnt_api, form);
            StartCoroutine(SetViewCnt(www));
        }
    }

    IEnumerator SetViewCnt(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            curArtInfo.inapp_content_cnt++;
            Global.artInfoList[curSelectedIndex] = curArtInfo;
        }
        else
        {
            //errmsg.text = errorTxt[0];
            //errorPopup.SetActive(true);
        }
    }

    IEnumerator SetMarkerViewCnt(WWW www, int i)
    {
        yield return www;
        if (www.error == null)
        {
            ArtInfo ainfo = Global.artInfoList[i];
            ainfo.inapp_marker_cnt++;
            Global.artInfoList[i] = ainfo;
        }
        else
        {
            //errmsg.text = errorTxt[0];
            //errorPopup.SetActive(true);
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator playVideo(VideoPlayer videoPlayer, RawImage rawImage)
    {
        if (videoPlayer == null || rawImage == null || string.IsNullOrEmpty(videoPlayer.url))
            yield break;
        Debug.Log("video play:" + videoPlayer.url);
        //videoPlayer.url = url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.Prepare();
        while (videoPlayer && !videoPlayer.isPrepared)
        {
            yield return new WaitForSeconds(1);
        }
        try
        {
            rawImage.texture = videoPlayer.texture;
            videoPlayer.Play();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator clearMenu()
    {
        collectionParent.transform.position = initCollectionParentPosition;
        while (collectionParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(collectionParent.transform.GetChild(0).gameObject));
        }
        while (collectionParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        StartCoroutine(SmoothMove(menuObj.transform.position, mainObj.transform.position, easing));
        collection_list.Clear();
        Global.currentPage = -1;
        Global.curSelCollectionIndex = -1;
        try
        {
            collectionParent.AddComponent<Swipe>();
            //collectionParent.GetComponent<Swipe>().topBackObj = topBackMenu;
            for (int i = Global.artInfoList.Length - 1; i >= 0; i--)
            {
                if (Global.artInfoList[i].video_url != "")
                {
                    GameObject newCol = Instantiate(collectionItemPrefab);
                    newCol.transform.SetParent(collectionParent.transform);
                    //GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTranform.Axis.Vertical, myHeight);
                    float w = collectionParent.transform.parent.transform.parent.GetComponent<RectTransform>().rect.width;
                    float h = collectionParent.transform.parent.transform.parent.GetComponent<RectTransform>().rect.height;
                    newCol.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
                    newCol.transform.localScale = Vector3.one;
                    //newCol.transform.Find("VideoManager").GetComponent<MediaPlayerCtrl>().m_strFileName = Global.artInfoList[i].video_url;
                    newCol.transform.Find("VideoManager").GetComponent<VideoPlayer>().url = Global.artInfoList[i].video_url.Trim();
                    if (Global.artInfoList[i].artist2 != null && Global.artInfoList[i].artist2 != "")
                    {
                        newCol.transform.Find("author_name").GetComponent<Text>().text = Global.artInfoList[i].artist1 + " x " + Global.artInfoList[i].artist2;
                    }
                    else
                    {
                        newCol.transform.Find("author_name").GetComponent<Text>().text = Global.artInfoList[i].artist1;
                    }
                    newCol.transform.Find("art_name").GetComponent<Text>().text = Global.artInfoList[i].contentTitle;
                    newCol.transform.Find("artNo").GetComponent<Text>().text = Global.GetNoFormat(Global.artInfoList[i].no);
                    newCol.transform.Find("viewCnt").GetComponent<Text>().text = Global.artInfoList[i].inapp_content_cnt.ToString();
                    newCol.transform.Find("art_name").transform.GetComponent<Button>().onClick.RemoveAllListeners();
                    newCol.transform.Find("id").GetComponent<Text>().text = Global.artInfoList[i].id.ToString();
                    int _i = i;
                    newCol.transform.Find("click").transform.GetComponent<Button>().onClick.AddListener(delegate () { onInappUrl(_i); });
                    collection_list.Add(newCol);
                }
            }
            if (collection_list.Count < 1)
            {
                StartCoroutine(showColGradient());
            }
        }
        catch (Exception ex)
        {

        }
        try
        {
            int col_sel_index = -1;
            //Debug.Log("col count = " + collection_list.Count);
            //for(int i = 0; i < collection_list.Count; i++)
            //{
            //    if(collection_list[i].transform.Find("id").GetComponent<Text>().text == Global.artInfoList[curSelectedIndex].id.ToString())
            //    {
            //        col_sel_index = i;break;
            //    }
            //}
            col_sel_index = 0;
            if(col_sel_index > -1)
            {
                MoveScrollView(contentRect, collection_list[col_sel_index], 0);
            }
            Global.totalPages = collection_list.Count;
            Debug.Log("total Pages:" + Global.totalPages);
            //for(int i = 0; i < collection_list.Count; i ++)
            //{
            //    if (collection_list[i].transform.Find("id").GetComponent<Text>().text == Global.artInfoList[curSelectedIndex].id.ToString())
            //    {
            //        Debug.Log("cur page = " + (i + 1));
            //        Global.currentPage = i + 1;
            //        Global.curSelCollectionIndex = i;
            //        collection_list[i].transform.Find("VideoManager").gameObject.SetActive(true);
            //        collectionParent.GetComponent<Swipe>().setVideoCnt();
            //        break;
            //    }
            //}
            Global.currentPage = 1;
            Global.curSelCollectionIndex = 0;
            if (collection_list.Count > 0)
            {
                collection_list[0].transform.Find("VideoManager").gameObject.SetActive(true);
                StartCoroutine(playVideo(collection_list[0].transform.Find("VideoManager").GetComponent<VideoPlayer>(), collection_list[0].transform.Find("VideoManager").GetComponent<RawImage>()));
            }
            collectionParent.GetComponent<Swipe>().setVideoCnt();
            Global.is_setted_swipe = true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        scene_type = 1;
    }

    IEnumerator showColGradient()
    {
        yield return new WaitForSeconds(delay_gradient_delay_time);
        gra_col_bottom.SetActive(true);
        gra_col_top.SetActive(true);
    }

    public void onMenu()
    {
        StartCoroutine(clearMenu());
    }

    public void closeAlert()
    {
        Global.is_shared = false;
        share_file_path = "";
        capturedVideoObj.GetComponent<MediaPlayerCtrl>().Stop();
        capturedVideoObj.GetComponent<MediaPlayerCtrl>().m_strFileName = "";
        capturedVideoObj.SetActive(false);
        capturedImageObj.SetActive(true);
        sharePanel.SetActive(false);
        artSlider.SetActive(true);
        alertObj.SetActive(false);
        HideUI(true);

        Color r = recanImage.color;
        r.a = 1f;
        recanImage.color = r;

        DestroyTempFiles();
    }

    IEnumerator PlayerGif(string gifUrl)
    {
        yield return new WaitForFixedUpdate();
        string path = Path.Combine(Application.persistentDataPath, Path.GetFileName(gifUrl));

        if (File.Exists(path))
        {
            Debug.Log(path + " exists");
            //gifPlayer.GetComponent<SingleGifPlayer>().gifPath = Path.GetFileName(path);
        }
        else
        {
            Debug.Log(path + " downloading--");
            WWW www = new WWW(gifUrl);
            yield return www;
            Debug.Log("savepath : " + path);
            //Check if we failed to send
            if (string.IsNullOrEmpty(www.error))
            {
                try
                {
                    //Create Directory if it does not exist
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    File.WriteAllBytes(path, www.bytes);
                    //gifPlayer.GetComponent<SingleGifPlayer>().gifPath = Path.GetFileName(path);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed To Save Data");
                    Debug.LogWarning("Error: " + e.Message);
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error: " + www.error);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Global.is_marker_detected && !detect_start)
        {
            Debug.Log("marker detection:" + Global.curMarkerName);
            bool is_found_detectedMarker = false;
            if(Global.artInfoList != null)
            {
                for (int i = 0; i < Global.artInfoList.Length; i++)
                {
                    if (Global.artInfoList[i].name == Global.curMarkerName)
                    {
                        is_found_detectedMarker = true;
                        InitPopup.SetActive(false);
                        Global.video_url = Global.artInfoList[i].gif_url;
                        Debug.Log("video url set = " + Global.video_url);
                        WWWForm form = new WWWForm();
                        form.AddField("marker_id", Global.artInfoList[i].id);
                        form.AddField("cnt", Global.artInfoList[i].inapp_marker_cnt);
                        WWW www = new WWW(Global.api_url + Global.set_markerCnt_api, form);
                        int _i = i;
                        StartCoroutine(SetMarkerViewCnt(www, _i));
                        SelectArt(i);
                        artInfoObj.transform.Find("effect").gameObject.SetActive(true);
                        StartCoroutine(SetEffect());
                        break;
                    }
                }
            }
            if (!is_found_detectedMarker)
            {
                GameObject.Find("Cloud Recognition").GetComponent<SimpleCloudRecoEventHandler>().NoResult();
            }
            detect_start = true;
            marker_lost_start = false;
            artSlider.GetComponent<ScrollRect>().enabled = false;
            //artSlider.GetComponent<ScrollRect>().StopMovement();
        }
        else if(Global.is_marker_detected && detect_start)
        {
        }
        else if(!Global.is_marker_detected && !marker_lost_start)
        {
            Debug.Log("marker detection lost.");
            marker_lost_start = true;
            detect_start = false;
            //StopCoroutine("PlayerGif");
            StopCoroutine(SetEffect());
            artInfoObj.transform.Find("effect").gameObject.SetActive(false);
            InitPopup.SetActive(true);
            artSlider.GetComponent<ScrollRect>().enabled = true;
        }
        //if (Global.is_error)
        //{
        //    ErrorTxtObj.SetActive(true);
        //}
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                if (sharePanel.activeSelf)
                {
                    closeAlert();
                }else if (scene_type == 1)
                {
                    onBacktoMain();
                }else if(scene_type == 0)
                {
                    Application.Quit();
                }
            }
        }
    }

    public void onBacktoMain()
    {
        StartCoroutine(SmoothMove(mainObj.transform.position, initMenuObjPos, easing, 1));
        StartCoroutine(clearCollection());
        scene_type = 0;
    }

    IEnumerator clearCollection()
    {
        while (collectionParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(collectionParent.transform.GetChild(0).gameObject));
        }
        while (collectionParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        collection_list.Clear();
        Global.curSelCollectionIndex = -1;
        Global.currentPage = -1;
        Vector3 tmp = collectionParent.transform.position;
        tmp.y = initCollectionParentPosition.y;
        collectionParent.transform.position = tmp;
        Destroy(collectionParent.GetComponent<Swipe>());
        Global.is_setted_swipe = false;
    }

    float offset_y(ScrollRect origin, Vector3 position)
    {
        RectTransform contentRT = origin.content.GetComponent<RectTransform>();
        Vector2 offset = (Vector2)origin.transform.InverseTransformPoint(contentRT.position)
            - (Vector2)origin.transform.InverseTransformPoint(position);
        return offset.y;
    }

    public void onChangeColScrollView(Vector2 value)
    {
        Debug.Log("on value changed");
        try
        {
            float miny = offset_y(contentRect, collection_list[0].transform.position);
            int index = -1;
            for (int i = 1; i < collection_list.Count; i++)
            {
                float offy = offset_y(contentRect, collection_list[i].transform.position);
                if (offy < miny)
                {
                    miny = offy;
                    index = i;
                }
            }
            if(index != -1)
            {
                Debug.Log("miny = " + miny + ", index = " + index);
                RectTransform contentRT = contentRect.content.GetComponent<RectTransform>();
                Vector2 anchor = contentRT.anchoredPosition;
                float height = collection_list[index].transform.GetComponent<RectTransform>().rect.height / 2;
                anchor.y = miny - height;
                contentRT.anchoredPosition = anchor;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    void OpenInAppBrowser(string url)
    {
        OpenURL(url);
        //DisplayOptions displayOptions = new DisplayOptions();
        //displayOptions.displayURLAsPageTitle = false;
        //displayOptions.backButtonText = "Back";
        //displayOptions.pageTitle = "InAppBrowser";
        //displayOptions.barBackgroundColor = "#FF0000";
        //displayOptions.textColor = "#00FF00";
        //displayOptions.browserBackgroundColor = "#00FF00";
        //displayOptions.loadingIndicatorColor = "#FF0000";
        //displayOptions.androidBackButtonCustomBehaviour = true;
        //OpenURL(url, displayOptions);
    }

    void onInappUrl(int index)
    {
        Debug.Log("inappbrowser");
        for (int i = 0; i < Global.artInfoList.Length; i++)
        {
            if(Global.artInfoList[i].id.ToString() == collection_list[index].transform.Find("id").GetComponent<Text>().text)
            {
                Debug.Log("id:" + Global.artInfoList[i].content_url);
                OpenInAppBrowser(Global.artInfoList[i].content_url);
                WWWForm form = new WWWForm();
                form.AddField("marker_id", Global.artInfoList[i].id);
                form.AddField("cnt", Global.artInfoList[i].inapp_content_cnt);
                WWW www = new WWW(Global.api_url + Global.set_contentCnt_api, form);
                StartCoroutine(SetViewCnt(www));
                break;
            }
        }
    }

    public void closeErrorTxt()
    {
        ErrorTxtObj.SetActive(false);
        Global.is_error = false;
    }

    public void closeEror()
    {
        errorPopup.SetActive(false);
    }

    public void closeError1()
    {
        errorPopup1.SetActive(false);
    }

    public void Retry()
    {
        //retry process
        errorPopup1.SetActive(false);
    }

    IEnumerator downloadImage(string url, string pathToSaveImage, GameObject imgObj)
    {
        //Debug.Log("image url = " + url);
        yield return new WaitForFixedUpdate();
        RawImage img = imgObj.GetComponent<RawImage>();
        if (File.Exists(pathToSaveImage))
        {
            StartCoroutine(LoadPictureToTexture(pathToSaveImage, img));
        }
        else
        {
            WWW www = new WWW(url);
            StartCoroutine(_downloadImage(www, pathToSaveImage, img));
        }
    }

    IEnumerator LoadPictureToTexture(string name, RawImage img)
    {
        //Debug.Log("load image = " + name);
        WWW pictureWWW = new WWW(Global.prePath + name);
        yield return pictureWWW;

        try
        {
            if (img != null)
            {
                //float width = pictureWWW.texture.width * 1.0f;
                //float height = pictureWWW.texture.height * 1.0f;
                //if(width > height)
                //{
                //    float ratio = height / width;
                //    img.transform.localScale = new Vector3(1, ratio, 1);
                //}
                //else
                //{
                //    float ratio = width / height;
                //    img.transform.localScale = new Vector3(ratio, 1, 1);
                //}
                img.texture = pictureWWW.texture;
                //img.sprite = Sprite.Create(pictureWWW.texture, new Rect(0, 0, pictureWWW.texture.width, pictureWWW.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
            }
        }
        catch (Exception ex)
        {
            //errmsg.text = errorTxt[0];
            //errorPopup.SetActive(true);
            Debug.Log(ex);
        }
    }

    private IEnumerator _downloadImage(WWW www, string savePath, RawImage img)
    {
        yield return www;
        //Debug.Log("savepath : " + savePath);
        //Check if we failed to send
        if (string.IsNullOrEmpty(www.error))
        {
            saveImage(savePath, www.bytes, img);
        }
        else
        {
            UnityEngine.Debug.Log("Error: " + www.error);
        }
    }

    void saveImage(string path, byte[] imageBytes, RawImage img)
    {
        try
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, imageBytes);
            //Debug.Log("Download Image: " + path.Replace("/", "\\"));
            StartCoroutine(LoadPictureToTexture(path, img));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
        }
    }

    IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos, float seconds, int type = 0)
    {
        if (type == 0)
        {
            menuObj.SetActive(true);
            gra_home_bottom.SetActive(false);
            gra_home_top.SetActive(false);
            InitPopup.SetActive(false);
        }
        else
        {
            mainObj.SetActive(true);
            gra_col_bottom.SetActive(false);
            gra_col_top.SetActive(false);
        }
        float t = 0f;
        while (t <= 1)
        {
            t += (Time.deltaTime / seconds);
            menuObj.transform.position = Vector3.Lerp(startpos, endpos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        if(type == 0)
        {
            mainObj.SetActive(false);
            StartCoroutine(showGradient(1));
        }
        else
        {
            menuObj.SetActive(false);
            StartCoroutine(showGradient(2));
        }
    }

    IEnumerator showGradient(int type)
    {
        yield return new WaitForSeconds(delay_gradient_delay_time);
        if(type == 1)
        {
            gra_col_bottom.SetActive(true);
            gra_col_top.SetActive(true);
        }
        else
        {
            gra_home_bottom.SetActive(true);
            gra_home_top.SetActive(true);
            InitPopup.SetActive(true);
        }
    }

    void SwitchFlashTorch(bool ON)
    {
        if (CameraDevice.Instance.SetFlashTorchMode(ON))
        {
            Debug.Log("Successfully turned flash " + ON);
        }
        else
        {
            Debug.Log("Failed to set the flash torch " + ON);
        }
    }

    public void slideValueChanged(Vector2 value)
    {
        try
        {
            if (is_art_changing || Global.is_marker_detected)
                return;
            is_art_changing = true;
            int cnt = artParent.transform.childCount;
            int newEndIndex = Convert.ToInt16(Math.Ceiling(value.x * 100 * (cnt - 1)) / 100);
            Debug.Log("newIndex:" + newEndIndex + ", cnt" + cnt);
            curSelectedIndex = newEndIndex;
            curArtInfo = Global.artInfoList[curSelectedIndex];
            RectTransform targetObj = artParent.transform.GetChild(newEndIndex).GetComponent<RectTransform>();
            artSlider.GetComponent<ScrollRect>().FocusOnItem(targetObj);
            changeArtType();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    void changeArtType()
    {
        for (int i = 0; i < artParent.transform.childCount; i++)
        {
            if (i == curSelectedIndex)
            {
                artParent.transform.GetChild(i).transform.localScale = Vector3.one;
                curSelectedIndex = i;
                curArtInfo = Global.artInfoList[i];
                string title = curArtInfo.contentTitle;
                if(title.Length > 5)
                {
                    title = title.Substring(0, 5) + "...";
                }
                //artNoTxt.text = Global.GetNoFormat(curArtInfo.no);
                authorInfoTxt.text = /*"," + */title;
                string artist = curArtInfo.artist1;
                if(curArtInfo.artist2 != null && curArtInfo.artist2 != "")
                {
                    artist = curArtInfo.artist1 + " x " + curArtInfo.artist2; ;
                }
                if(artist.Length > 18)
                {
                    artist = artist.Substring(0, 18) + "...";
                }
                artistNameTxt.text = artist;

            }
            else
            {
                artParent.transform.GetChild(i).transform.localScale = new Vector3(48 / 78f, 48 / 78f, 1);
            }
        }
        is_art_changing = false;
    }

    public void onPreviousArt()
    {
        if(curSelectedIndex > 0)
        {
            Debug.Log("curSelectedIndex:" + curSelectedIndex);
            RectTransform targetObj = artParent.transform.GetChild(curSelectedIndex - 1).GetComponent<RectTransform>();
            artSlider.GetComponent<ScrollRect>().FocusOnItem(targetObj);
            curSelectedIndex--;
            curArtInfo = Global.artInfoList[curSelectedIndex];
            changeArtType();
        }
    }

    public void onNextArt()
    {
        if (curSelectedIndex < artParent.transform.childCount - 1)
        {
            RectTransform targetObj = artParent.transform.GetChild(curSelectedIndex + 1).GetComponent<RectTransform>();
            artSlider.GetComponent<ScrollRect>().FocusOnItem(targetObj);
            curSelectedIndex++;
            curArtInfo = Global.artInfoList[curSelectedIndex];
            changeArtType();
        }
    }
}

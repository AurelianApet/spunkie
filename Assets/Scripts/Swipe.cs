using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class Swipe : MonoBehaviour, IDragHandler, IEndDragHandler
{
    //for swipe
    private Vector3 panelLocation;
    //public GameObject topBackObj;
    public float percentThreshold = 0.2f;
    float easing = 0.2f;
    float delay_gradient_delay_time = 0.1f;
    //private float offy = 0f;

    Vector3 InitPos;
    bool isset = false;
    private void Start()
	{
        StartCoroutine(WaitForInitSettingSwipe());
    }

    IEnumerator WaitForInitSettingSwipe()
    {
        while (!Global.is_setted_swipe)
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(1f);
        InitPos = transform.position;
        panelLocation = InitPos;
        isset = true;
        //offy = topBackObj.GetComponent<RectTransform>().rect.height;
        //Debug.Log("off y = " + offy);
    }

    IEnumerator WaitForSettingSwipe()
    {
        while (!Global.is_setted_swipe)
        {
            isset = false;
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(1f);
        panelLocation = InitPos;
        isset = true;
    }

    public void OnDrag(PointerEventData data)
    {
        if (!isset)
        {
            StartCoroutine(WaitForSettingSwipe());
        }
        else
        {
            float difference = data.pressPosition.y - data.position.y;
            transform.position = panelLocation - new Vector3(0, difference, 0);
        }
    }

    public void setVideoCnt()
    {
        try
        {
            Debug.Log("cur id:" + this.gameObject.transform.GetChild(Global.curSelCollectionIndex).Find("id").GetComponent<Text>().text);
            WWWForm form = new WWWForm();
            for(int i = 0; i < Global.artInfoList.Length; i++)
            {
                if(Global.artInfoList[i].id.ToString() == this.gameObject.transform.GetChild(Global.curSelCollectionIndex).Find("id").GetComponent<Text>().text)
                {
                    Debug.Log("set view cnt:" + Global.artInfoList[i].show_video_cnt);
                    form.AddField("marker_id", Global.artInfoList[i].id);
                    form.AddField("cnt", Global.artInfoList[i].show_video_cnt);
                    WWW www = new WWW(Global.api_url + Global.set_videoCnt_api, form);
                    int _i = i;
                    StartCoroutine(SetViewCnt(www, i));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator SetViewCnt(WWW www, int i)
    {
        yield return www;
        if (www.error == null)
        {
            ArtInfo ainfo = Global.artInfoList[i];
            ainfo.show_video_cnt++;
            Global.artInfoList[i] = ainfo;
            Debug.Log(ainfo.id + ", cnt:" + ainfo.show_video_cnt);
            this.gameObject.transform.GetChild(Global.curSelCollectionIndex).Find("viewCnt").GetComponent<Text>().text = ainfo.show_video_cnt.ToString();
        }
    }

    int old_index = Global.curSelCollectionIndex;
    public void OnEndDrag(PointerEventData data)
    {
        if (!isset)
            return;
        float percentage = (data.pressPosition.y - data.position.y) / Screen.height;
        if (Mathf.Abs(percentage) >= percentThreshold)
        {
            Vector3 newLocation = panelLocation;
            if (percentage < 0 && Global.currentPage < Global.totalPages)
            {
                old_index = Global.curSelCollectionIndex;
                Global.curSelCollectionIndex++;
                Debug.Log(Global.curSelCollectionIndex);
                Global.currentPage++;
                newLocation += new Vector3(0, Screen.height/* - offy*/, 0);
                setVideoCnt();
            }
            else if (percentage > 0 && Global.currentPage > 1)
            {
                old_index = Global.curSelCollectionIndex;
                Global.curSelCollectionIndex--; ;
                Debug.Log(Global.curSelCollectionIndex);
                Global.currentPage--;
                newLocation += new Vector3(0, -(Screen.height/* - offy*/), 0);
                setVideoCnt();
            }
            StartCoroutine(SmoothMove(transform.position, newLocation, easing));
            panelLocation = newLocation;
        }
        else
        {
            StartCoroutine(SmoothMove(transform.position, panelLocation, easing));
        }
    }

    IEnumerator playVideo(VideoPlayer videoPlayer, RawImage rawImage)
    {
        if (videoPlayer == null || rawImage == null || string.IsNullOrEmpty(videoPlayer.url))
            yield break;

        //videoPlayer.url = url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.Prepare();
        while (videoPlayer && !videoPlayer.isPrepared)
        {
            yield return new WaitForSeconds(1);
        }

        rawImage.texture = videoPlayer.texture;
        videoPlayer.Play();
    }

    IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos, float seconds)
    {
        float t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / seconds;
            transform.position = Vector3.Lerp(startpos, endpos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        try
        {
            Debug.Log("old:" + old_index + ", new:" + Global.curSelCollectionIndex);
            gameObject.transform.GetChild(old_index).transform.Find("top_gradient").gameObject.SetActive(false);
            gameObject.transform.GetChild(old_index).transform.Find("bottom_gradient").gameObject.SetActive(false);
            gameObject.transform.GetChild(old_index).transform.Find("VideoManager").GetComponent<VideoPlayer>().Stop();
            gameObject.transform.GetChild(old_index).transform.Find("VideoManager").gameObject.SetActive(false);
            gameObject.transform.GetChild(Global.curSelCollectionIndex).transform.Find("VideoManager").gameObject.SetActive(true);
            StartCoroutine(playVideo(gameObject.transform.GetChild(Global.curSelCollectionIndex).transform.Find("VideoManager").GetComponent<VideoPlayer>(), gameObject.transform.GetChild(Global.curSelCollectionIndex).transform.Find("VideoManager").GetComponent<RawImage>()));
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        yield return new WaitForSeconds(delay_gradient_delay_time);
        try
        {
            this.gameObject.transform.GetChild(Global.curSelCollectionIndex).transform.Find("top_gradient").gameObject.SetActive(true);
            this.gameObject.transform.GetChild(Global.curSelCollectionIndex).transform.Find("bottom_gradient").gameObject.SetActive(true);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }
}

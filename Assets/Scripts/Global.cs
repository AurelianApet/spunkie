using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public struct ArtInfo
{
    public int id;
    public int inapp_marker_cnt;
    public string name;//marker name
    public int no;//작품번호
    public string contentTitle;
    public string artist1;//작품명
    public string artist2;
    public int inapp_content_cnt;
    public string video_url;
    public string content_url;
    public string thumbnail;
    public string gif_url;
    public int show_video_cnt;
}

public class Global
{
    public static string api_url = "http://localhost:1990";
    //public static string api_url = "http://ec2-13-125-199-217.ap-northeast-2.compute.amazonaws.com:1990";
    public static string imgPath = "";
    public static string prePath = "";

    static string api_prefix = "/api/";
    public static string get_markerlist_api = api_prefix + "getmarkerlist";
    public static string set_markerCnt_api = api_prefix + "setmarkerCnt";
    public static string set_contentCnt_api = api_prefix + "setcontentCnt";
    public static string set_videoCnt_api = api_prefix + "setvideoCnt";
    public static ArtInfo[] artInfoList;
    public static bool is_marker_detected = false;
    public static string curMarkerName = "";
    public static int totalPages = 1;//for swipe
    public static int currentPage = 1;//for swipe
    public static int curSelCollectionIndex = 0;
    public static bool is_setted_swipe = false;//for swipe
    public static bool is_error = false;
    public static string video_url = "";
    public static bool is_shared = false;
    public static int newStatusBarValue;


    public static string GetNoFormat(int ono)
    {
        return string.Format("{0:D4}", ono);
    }

    public static void setStatusBarValue(int value)
    {
        newStatusBarValue = value;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                try
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(setStatusBarValueInThread));
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
    }

    private static void setStatusBarValueInThread()
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                {
                    window.Call("setFlags", newStatusBarValue, -1);
                }
            }
        }
#endif
    }
}

public static class ScrollViewFocusFunctions
{
    public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollView, Vector2 focusPoint)
    {
        Vector2 contentSize = scrollView.content.rect.size;
        Vector2 viewportSize = ((RectTransform)scrollView.content.parent).rect.size;
        Vector2 contentScale = scrollView.content.localScale;

        contentSize.Scale(contentScale);
        focusPoint.Scale(contentScale);

        Vector2 scrollPosition = scrollView.normalizedPosition;
        if (scrollView.horizontal && contentSize.x > viewportSize.x)
            scrollPosition.x = Mathf.Clamp01((focusPoint.x - viewportSize.x * 0.5f) / (contentSize.x - viewportSize.x));
        if (scrollView.vertical && contentSize.y > viewportSize.y)
            scrollPosition.y = Mathf.Clamp01((focusPoint.y - viewportSize.y * 0.5f) / (contentSize.y - viewportSize.y));

        return scrollPosition;
    }

    public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollView, RectTransform item)
    {
        Vector2 itemCenterPoint = scrollView.content.InverseTransformPoint(item.transform.TransformPoint(item.rect.center));

        Vector2 contentSizeOffset = scrollView.content.rect.size;
        contentSizeOffset.Scale(scrollView.content.pivot);

        return scrollView.CalculateFocusedScrollPosition(itemCenterPoint + contentSizeOffset);
    }

    public static void FocusAtPoint(this ScrollRect scrollView, Vector2 focusPoint)
    {
        scrollView.normalizedPosition = scrollView.CalculateFocusedScrollPosition(focusPoint);
    }

    public static void FocusOnItem(this ScrollRect scrollView, RectTransform item)
    {
        scrollView.normalizedPosition = scrollView.CalculateFocusedScrollPosition(item);
    }

    private static IEnumerator LerpToScrollPositionCoroutine(this ScrollRect scrollView, Vector2 targetNormalizedPos, float speed)
    {
        Vector2 initialNormalizedPos = scrollView.normalizedPosition;

        float t = 0f;
        while (t < 1f)
        {
            scrollView.normalizedPosition = Vector2.LerpUnclamped(initialNormalizedPos, targetNormalizedPos, 1f - (1f - t) * (1f - t));

            yield return null;
            t += speed * Time.unscaledDeltaTime;
        }

        scrollView.normalizedPosition = targetNormalizedPos;
    }

    public static IEnumerator FocusAtPointCoroutine(this ScrollRect scrollView, Vector2 focusPoint, float speed)
    {
        yield return scrollView.LerpToScrollPositionCoroutine(scrollView.CalculateFocusedScrollPosition(focusPoint), speed);
    }

    public static IEnumerator FocusOnItemCoroutine(this ScrollRect scrollView, RectTransform item, float speed)
    {
        yield return scrollView.LerpToScrollPositionCoroutine(scrollView.CalculateFocusedScrollPosition(item), speed);
    }
}
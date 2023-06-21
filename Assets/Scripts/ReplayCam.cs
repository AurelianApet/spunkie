using NatCorder;
using NatCorder.Clocks;
using NatCorder.Inputs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using NativeShareNamespace;
using System;

public class ReplayCam : MonoBehaviour
{
    public Manager sceneManager;

    [Header("Recording")]
    public int videoWidth = 1280;
    public int videoHeight = 720;

    private MP4Recorder videoRecorder;
    private CameraInput cameraInput;
    private AudioInput audioInput;
    private AudioSource microphoneSource;
    private RealtimeClock recordingClock;

    private bool isRecording;
    private bool canSave;

    private bool mReplayCamInitialized = false;

    #region Unity Life Cycle

    // Start is called before the first frame update
    void Start()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartCoroutine(InitializeReplayCam());
        }
        else
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!mReplayCamInitialized && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartCoroutine(InitializeReplayCam());
        }
    }

    private IEnumerator InitializeReplayCam()
    {
        Debug.Log("Start replay cam");

        mReplayCamInitialized = true;
        isRecording = false;
        canSave = false;

        // Start microphone
        try
        {
            microphoneSource = gameObject.AddComponent<AudioSource>();
            microphoneSource.mute =
            microphoneSource.loop = true;
            microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
            microphoneSource.clip = Microphone.Start(null, true, 1000, AudioSettings.outputSampleRate);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
        microphoneSource.Play();
    }

    private void OnDestroy()
    {
        Debug.Log("Destroy replay cam");
        // Stop microphone
        microphoneSource.Stop();
        Microphone.End(null);
    }

    #endregion

    #region Event

    public void SetSave(bool b)
    {
        canSave = b;
    }

    public void CaptureStart()
    {
        if (isRecording)
        {
            StopRecording();
        }
        StartRecording();
    }

    public void CaptureStop()
    {
        StopRecording();
    }

    #endregion

    #region Recording

    public void StartRecording()
    {
        // Start recording
        var frameRate = 30;
        var sampleRate = AudioSettings.outputSampleRate;
        var channelCount = (int)AudioSettings.speakerMode;
        recordingClock = new RealtimeClock();
        videoRecorder = new MP4Recorder(
            videoWidth,
            videoHeight,
            frameRate,
            sampleRate,
            channelCount,
            recordingPath =>
            {
                Debug.Log($"Saved recording to: {recordingPath}");

                if (canSave)
                {
                    sceneManager.SaveCaptureVideo(recordingPath);
                }
                else
                {
                    sceneManager.DiscardCaptureVideo(recordingPath);
                }
                //var prefix = Application.platform == RuntimePlatform.IPhonePlayer ? "file://" : "";
                //Handheld.PlayFullScreenMovie($"{prefix}{recordingPath}");
            }
        );
        // Create recording inputs
        GameObject cameraObj = GameObject.Find("ARCamera");
        if (cameraObj == null)
            cameraObj = GameObject.Find("Camera");
        Camera arCam = cameraObj.GetComponent<Camera>();

        cameraInput = new CameraInput(videoRecorder, recordingClock, arCam);
        audioInput = new AudioInput(videoRecorder, recordingClock, microphoneSource, true);
        //// Unmute microphone
        microphoneSource.mute = audioInput == null;
    }

    public void StopRecording()
    {
        // Stop recording
        audioInput?.Dispose();
        cameraInput.Dispose();
        videoRecorder.Dispose();
        videoRecorder = null;
        
        // Mute microphone
        microphoneSource.mute = true;

        //share
    }
    
    #endregion
}

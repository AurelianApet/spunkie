/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatCorder.Inputs {

    using UnityEngine;
    using System;
    using System.Collections;
    using Clocks;
    using Internal;

    /// <summary>
    /// INCOMPLETE. Recorder input for recording video frames from the screen
    /// </summary>
    [Doc(@"ScreenInput")]
    public sealed class ScreenInput : IDisposable { // INCOMPLETE // Do not use as this requires Unity 2019.1

        #region --Client API--
        /// <summary>
        /// Control number of successive camera frames to skip while recording.
        /// This is very useful for GIF recording, which typically has a lower framerate appearance
        /// </summary>
        [Doc(@"CameraInputFrameSkip", @"CameraInputFrameSkipDiscussion")]
        public int frameSkip;

        /// <summary>
        /// Create a video recording input from the screen
        /// </summary>
        /// <param name="mediaRecorder">Media recorder to receive committed frames</param>
        /// <param name="clock">Clock for generating timestamps</param>
        /// <param name="cameras">Game cameras to record</param>
        [Doc(@"ScreenInputCtor")]
        public ScreenInput (IMediaRecorder mediaRecorder, IClock clock) {
            // Save state
            this.mediaRecorder = mediaRecorder;
            this.clock = clock;
            // Create framebuffer
            var frameDescriptor = new RenderTextureDescriptor(mediaRecorder.frameSize.width, mediaRecorder.frameSize.height, RenderTextureFormat.ARGB32, 24);
            frameDescriptor.sRGB = true;
            this.framebuffer = RenderTexture.GetTemporary(frameDescriptor);
            // Start recording
            this.attachment = new GameObject("NatCorder ScreenInput").AddComponent<ScreenInputAttachment>();
            attachment.StartCoroutine(OnFrame());
        }

        /// <summary>
        /// Stop recorder input and teardown resources
        /// </summary>
        [Doc(@"AudioInputDispose")]
        public void Dispose () {
            ScreenInputAttachment.Destroy(attachment);
            GameObject.Destroy(attachment.gameObject);
            RenderTexture.ReleaseTemporary(framebuffer);
        }
        #endregion


        #region --Operations--

        private readonly IMediaRecorder mediaRecorder;
        private readonly IClock clock;
        private readonly RenderTexture framebuffer;
        private readonly ScreenInputAttachment attachment;
        private int frameCount;

        private IEnumerator OnFrame () {
            var yielder = new WaitForEndOfFrame();
            for (;;) {
                // Check frame index
                yield return yielder;
                if (frameCount++ % (frameSkip + 1) != 0)
                    continue;
                // Commit frame
                //ScreenCapture.CaptureScreenshotIntoRenderTexture(framebuffer);
                //var timestamp = clock.Timestamp;
                //framebuffer.Readback(pixelBuffer => mediaRecorder.CommitFrame(pixelBuffer, timestamp));
            }
        }

        private sealed class ScreenInputAttachment : MonoBehaviour { }
        #endregion
    }
}
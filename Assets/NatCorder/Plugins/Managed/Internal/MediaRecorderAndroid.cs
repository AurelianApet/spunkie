/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatCorder.Internal {

    using UnityEngine;
    using UnityEngine.Scripting;
    using System;
    using System.Runtime.InteropServices;

    public sealed class MediaRecorderAndroid : AndroidJavaProxy, IMediaRecorder {
        
        #region --IMediaRecorder--

        public (int width, int height) frameSize {
            get; private set;
        }

        public MediaRecorderAndroid (AndroidJavaObject recorder, int width, int height, string recordingPath, Action<string> callback) : base(@"api.natsuite.natcorder.MediaRecorder$Callback") {
            this.recorder = recorder;
            this.frameSize = (width, height);
            this.callback = callback;
            // Start recording
            recorder.Call(@"startRecording", recordingPath, this);
        }

        public void Dispose () {
            recorder.Call(@"stopRecording");
            recorder.Dispose();
        }
        
        public void CommitFrame<T> (T[] pixelBuffer, long timestamp) where T : struct {
            var handle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            CommitFrame(handle.AddrOfPinnedObject(), timestamp);
            handle.Free();
        }

        public void CommitFrame (IntPtr pixelBuffer, long timestamp) => recorder.Call(@"encodeFrame", (long)pixelBuffer, timestamp);

        public void CommitSamples (float[] sampleBuffer, long timestamp) => recorder.Call(@"encodeSamples", sampleBuffer, timestamp);
        #endregion


        #region --Operations--
        private readonly AndroidJavaObject recorder;
        private readonly Action<string> callback;
        [Preserve] private void onRecording (string path) => callback(path);
        #endregion
    }
}
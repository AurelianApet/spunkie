/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatCorder.Internal {

    using AOT;
    using System;
    using System.Runtime.InteropServices;

    public sealed class MediaRecorderiOS : IMediaRecorder {
        
        #region --IMediaRecorder--

        public (int width, int height) frameSize {
            get; private set;
        }

        public MediaRecorderiOS (IntPtr recorder, int width, int height, string recordingPath, Action<string> callback) {
            this.recorder = recorder;
            this.frameSize = (width, height);
            this.callback = callback;
            this.callbackDispatcher = new MainDispatcher();
            // Start recording
            recorder.StartRecording(recordingPath, OnRecording, (IntPtr)GCHandle.Alloc(this, GCHandleType.Normal));
        }

        public void Dispose () => recorder.StopRecording();

        public void CommitFrame<T> (T[] pixelBuffer, long timestamp) where T : struct {
            var handle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            CommitFrame(handle.AddrOfPinnedObject(), timestamp);
            handle.Free();
        }

        public void CommitFrame (IntPtr nativeBuffer, long timestamp) => recorder.EncodeFrame(nativeBuffer, timestamp);

        public void CommitSamples (float[] sampleBuffer, long timestamp) => recorder.EncodeSamples(sampleBuffer, sampleBuffer.Length, timestamp);
        #endregion


        #region --Operations--

        public readonly IntPtr recorder;
        private readonly Action<string> callback;
        private readonly MainDispatcher callbackDispatcher;

        [MonoPInvokeCallback(typeof(Action<IntPtr, IntPtr>))]
        private static void OnRecording (IntPtr context, IntPtr path) {
            var pathStr = Marshal.PtrToStringAuto(path);
            var instanceHandle = (GCHandle)context;
            var instance = instanceHandle.Target as MediaRecorderiOS;
            instanceHandle.Free();
            instance.callbackDispatcher.Dispatch(() => {
                instance.callback(pathStr);
                instance.callbackDispatcher.Dispose();
            });
        }
        #endregion
    }
}
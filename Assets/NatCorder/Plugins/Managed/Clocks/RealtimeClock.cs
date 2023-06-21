/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatCorder.Clocks {

    using System;
    using System.Runtime.CompilerServices;
    using Internal;
    using Stopwatch = System.Diagnostics.Stopwatch;

    /// <summary>
    /// Realtime clock for generating timestamps
    /// </summary>
    [Doc(@"RealtimeClock")]
    public sealed class RealtimeClock : IClock {

        /// <summary>
        /// Current timestamp in nanoseconds.
        /// The very first value reported by this property will always be zero.
        /// </summary>
        [Doc(@"Timestamp")]
        public long Timestamp {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                var time = stopwatch.Elapsed.Ticks * 100L;
                if (!stopwatch.IsRunning)
                    stopwatch.Start();
                return time;
            }
        }

        /// <summary>
        /// Is the clock paused?
        /// </summary>
        [Doc(@"RealtimeClockPaused")]
        public bool Paused {
            [MethodImpl(MethodImplOptions.Synchronized)] get => !stopwatch.IsRunning;
            [MethodImpl(MethodImplOptions.Synchronized)] set => (value ? (Action)stopwatch.Stop : stopwatch.Start)();
        }

        public RealtimeClock () => this.stopwatch = new Stopwatch();

        private readonly Stopwatch stopwatch;
    }
}
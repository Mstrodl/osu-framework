﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Timing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// A GameHost which doesn't require a graphical or sound device.
    /// </summary>
    public class HeadlessGameHost : DesktopGameHost
    {
        private readonly IFrameBasedClock customClock;

        protected override IFrameBasedClock SceneGraphClock => customClock ?? base.SceneGraphClock;

        public override void OpenFileExternally(string filename) => Logger.Log($"Application has requested file \"{filename}\" to be opened.");

        public override void OpenUrlExternally(string url) => Logger.Log($"Application has requested URL \"{url}\" to be opened.");

        protected override Storage GetStorage(string baseName) => new DesktopStorage($"headless-{baseName}", this);

        public HeadlessGameHost(string gameName = @"", bool bindIPC = false, bool realtime = true)
            : base(gameName, bindIPC)
        {
            if (!realtime) customClock = new FramedClock(new FastClock(1000.0 / 30));

            UpdateThread.Scheduler.Update();
        }

        protected override void UpdateInitialize()
        {
        }

        protected override void DrawInitialize()
        {
        }

        protected override void DrawFrame()
        {
            //we can't draw.
        }

        protected override void UpdateFrame()
        {
            customClock?.ProcessFrame();

            base.UpdateFrame();
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() => new InputHandler[] { };

        private class FastClock : IClock
        {
            private readonly double increment;
            private double time;

            /// <summary>
            /// A clock which increments each time <see cref="CurrentTime"/> is requested.
            /// Run fast. Run consistent.
            /// </summary>
            /// <param name="increment">Milliseconds we should increment the clock by each time the time is requested.</param>
            public FastClock(double increment)
            {
                this.increment = increment;
            }

            public double CurrentTime => time += increment;
            public double Rate => 1;
            public bool IsRunning => true;
        }
    }
}

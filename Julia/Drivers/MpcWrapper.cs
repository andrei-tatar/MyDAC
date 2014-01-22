using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Julia.Utils;

namespace Julia.Drivers
{
    delegate void MpcEventHandler<in T>(T arg);

    enum PlayStatus
    {
        Play, Pause, Stopped
    }

    [Flags]
    enum PlayFlags
    {
        None = 0x00,
        Repeat = 0x01,
        Shuffle = 0x02,
        Single = 0x04,
        Consume = 0x08
    }

    class MpcWrapper : IDisposable
    {
        private readonly Thread _notificationWorker;
        private bool _ignoreNext;
        private PlayFlags _playFlags = PlayFlags.None;
        private PlayStatus _playStatus = PlayStatus.Stopped;

        public event MpcEventHandler<string> OnSongNameChanged;
        public event MpcEventHandler<PlayStatus> OnPlayStatusChanged;
        public event MpcEventHandler<PlayFlags> OnPlayFlagsChanged;

        public PlayFlags PlayFlags
        {
            get { return _playFlags; }
            set
            {
                if (_playFlags == value) return;
                _playFlags = value;
                OnPlayFlagsChanged(_playFlags);
            }
        }

        public PlayStatus PlayStatus
        {
            get { return _playStatus; }
            set
            {
                if (_playStatus == value) return;
                _playStatus = value;
                OnPlayStatusChanged(_playStatus);
            }
        }

        public MpcWrapper()
        {
            ConsoleUtils.Execute("mpc").CheckForExceptionOrError("MPC is not installed");
            UpdateProperties(ConsoleUtils.Execute("mpc").Output);
            _notificationWorker = new Thread(MpcNotificationWorker);
            _notificationWorker.Start();
        }

        private void MpcNotificationWorker()
        {
            try
            {
                while (true)
                {
                    var result = ConsoleUtils.Execute("mpd", "idle");
                    if (_ignoreNext)
                    {
                        _ignoreNext = false;
                        continue;
                    }
                    var resultMessage = result.Output.Trim('\r', '\n', ' ').ToLower();

                    if (resultMessage == "player")
                        UpdateProperties(ConsoleUtils.Execute("mpc").Output);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        public void Play()
        {
            _ignoreNext = true;
            UpdateProperties(ConsoleUtils.Execute("mpc", "play").CheckForExceptionOrError("Could not change play state").Output);
        }

        public void Pause()
        {
            _ignoreNext = true;
            UpdateProperties(ConsoleUtils.Execute("mpc", "pause").CheckForExceptionOrError("Could not change play state").Output);
        }

        public void PlayPause()
        {
            _ignoreNext = true;
            UpdateProperties(ConsoleUtils.Execute("mpc", "toggle").CheckForExceptionOrError("Could not change play state").Output);
        }

        public void Stop()
        {
            _ignoreNext = true;
            UpdateProperties(ConsoleUtils.Execute("mpc", "stop").CheckForExceptionOrError("Could not change play state").Output);
        }

        public void UpdateProperties(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToList();
            var st = 0;
            foreach (var line in lines)
            {
                string[] parts;
                switch (st)
                {
                    case 0:
                        if (!line.StartsWith("volume:")) break;

                        parts = line.Split(new[] { ':', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var dict = new Dictionary<string, string>();
                        for (var i = 0; i < parts.Length; i += 2)
                            dict[parts[i]] = parts[i + 1];

                        var flags = PlayFlags.None;
                        if (dict["repeat"] != "off") flags |= PlayFlags.Repeat;
                        if (dict["random"] != "off") flags |= PlayFlags.Shuffle;
                        if (dict["single"] != "off") flags |= PlayFlags.Single;
                        if (dict["consume"] != "off") flags |= PlayFlags.Consume;
                        PlayFlags = flags;

                        st = 1;
                        break;
                    case 1:
                        if (!line.StartsWith("[")) break;
                        parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        if (parts[0] == "[playing]")
                            PlayStatus = PlayStatus.Play;
                        else if (parts[1] == "[paused]")
                            PlayStatus = PlayStatus.Pause;
                        else PlayStatus = PlayStatus.Stopped;

                        st = 2;
                        break;
                    case 2:
                        
                        break;
                }
            }
        }

        public void Dispose()
        {
            _notificationWorker.Abort();
        }
    }
}

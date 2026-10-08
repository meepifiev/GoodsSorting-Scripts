using System;
using System.Globalization;
using _Project.Core.Lives;
using PrimeGames.SDK;
using VContainer.Unity;

namespace _Project.Infrastructure.Lives
{
    public class LivesService : ILivesService, ITickable
    {
        private const int MaxLives = 5;
        private const int StartingLives = 5;
        private const double RegenSeconds = 1800d;

        private const string CurrentKey = "lives_current";
        private const string NextAtKey = "lives_next_at";
        private const string InfiniteKey = "lives_infinite";
        private const string LegacyRegenKey = "lives_regen_seconds";

        private int _current = StartingLives;
        private bool _infinite;
        private bool _loaded;
        private bool _ready;
        private DateTime _nextLifeAt;

        public LivesService()
        {
            PrimeSDK.WaitForProviders(() => _ready = true);
        }

        public event Action Changed;

        public int Current
        {
            get
            {
                EnsureLoaded();
                Refresh();
                return _infinite ? MaxLives : _current;
            }
        }

        public int Max => MaxLives;

        public bool IsInfinite
        {
            get
            {
                EnsureLoaded();
                return _infinite;
            }
        }

        public float SecondsToNextLife
        {
            get
            {
                EnsureLoaded();
                Refresh();

                if (_infinite || _current >= MaxLives)
                {
                    return 0f;
                }

                double remaining = (_nextLifeAt - Now()).TotalSeconds;
                return remaining > 0d ? (float)remaining : 0f;
            }
        }

        public void Tick()
        {
            EnsureLoaded();
            Refresh();
        }

        public bool TrySpendLife()
        {
            EnsureLoaded();

            if (_infinite)
            {
                return true;
            }

            Refresh();

            if (_current <= 0)
            {
                return false;
            }

            bool wasFull = _current >= MaxLives;
            _current -= 1;

            if (wasFull)
            {
                _nextLifeAt = Now().AddSeconds(RegenSeconds);
            }

            Persist();
            OnChanged();
            return true;
        }

        public void AddLife()
        {
            EnsureLoaded();

            if (_current >= MaxLives)
            {
                return;
            }

            _current += 1;

            if (_current < MaxLives)
            {
                _nextLifeAt = Now().AddSeconds(RegenSeconds);
            }

            Persist();
            OnChanged();
        }

        public void Fill()
        {
            EnsureLoaded();

            if (_infinite || _current >= MaxLives)
            {
                return;
            }

            _current = MaxLives;
            Persist();
            OnChanged();
        }

        public void SetInfinite(bool value)
        {
            EnsureLoaded();

            if (_infinite == value)
            {
                return;
            }

            _infinite = value;
            PrimeSDK.Data.SetBool(InfiniteKey, value, important: true);
            PrimeSDK.Data.Save();
            OnChanged();
        }

        private void EnsureLoaded()
        {
            if (_loaded || _ready == false)
            {
                return;
            }

            _loaded = true;
            _current = PrimeSDK.Data.GetInt(CurrentKey, StartingLives);
            _infinite = PrimeSDK.Data.GetBool(InfiniteKey, false);
            _nextLifeAt = ReadNextAt();

            if (_current < MaxLives && _nextLifeAt == default(DateTime))
            {
                double remaining = PrimeSDK.Data.GetFloat(LegacyRegenKey, (float)RegenSeconds);

                if (remaining < 0d)
                {
                    remaining = 0d;
                }
                else if (remaining > RegenSeconds)
                {
                    remaining = RegenSeconds;
                }

                _nextLifeAt = Now().AddSeconds(remaining);
                Persist();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_loaded == false || _infinite || _current >= MaxLives)
            {
                return;
            }

            DateTime now = Now();
            bool changed = false;

            while (_current < MaxLives && now >= _nextLifeAt)
            {
                _current += 1;
                _nextLifeAt = _nextLifeAt.AddSeconds(RegenSeconds);
                changed = true;
            }

            if (changed)
            {
                Persist();
                OnChanged();
            }
        }

        private DateTime Now()
        {
            return PrimeSDK.Time.CurrentDate;
        }

        private DateTime ReadNextAt()
        {
            string raw = PrimeSDK.Data.GetString(NextAtKey, string.Empty);

            if (string.IsNullOrEmpty(raw) == false
                && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
            {
                return new DateTime(ticks);
            }

            return default(DateTime);
        }

        private void Persist()
        {
            PrimeSDK.Data.SetInt(CurrentKey, _current, important: false);
            PrimeSDK.Data.SetString(NextAtKey, _nextLifeAt.Ticks.ToString(CultureInfo.InvariantCulture), important: false);
            PrimeSDK.Data.Save();
        }

        private void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}

﻿// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using ClassicUO.IO.Audio;
using ClassicUO.Assets;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.Game.Managers
{
    internal sealed class AudioManager
    {
        const float SOUND_DELTA = 250;

        private bool _canReproduceAudio = true;
        private readonly LinkedList<UOSound> _currentSounds = new LinkedList<UOSound>();
        private readonly UOMusic[] _currentMusic = { null, null };
        private readonly int[] _currentMusicIndices = { 0, 0 };
        public int LoginMusicIndex { get; private set; }
        public int DeathMusicIndex { get; } = 42;

        public void Initialize()
        {
            try
            {
                new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose();
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn(ex.ToString());
                _canReproduceAudio = false;
            }

            LoginMusicIndex = Client.Game.UO.Version switch
            {
                >= ClientVersion.CV_7000 => 78, // LoginLoop
                > ClientVersion.CV_308Z => 0,
                _ => 8 // stones2
            };

            Client.Game.Activated += OnWindowActivated;
            Client.Game.Deactivated += OnWindowDeactivated;
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 0;
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 1;
        }

        public void PlaySound(int index)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!_canReproduceAudio || currentProfile == null)
            {
                return;
            }

            float volume = currentProfile.SoundVolume / SOUND_DELTA;

            if (Client.Game.IsActive)
            {
                if (!currentProfile.ReproduceSoundsInBackground)
                {
                    volume = currentProfile.SoundVolume / SOUND_DELTA;
                }
            }
            else if (!currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            UOSound sound = (UOSound) Client.Game.UO.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume))
            {
                sound.X = -1;
                sound.Y = -1;
                sound.CalculateByDistance = false;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlaySoundWithDistance(World world, int index, int x, int y)
        {
            if (!_canReproduceAudio || !world.InGame)
            {
                return;
            }

            int distX = Math.Abs(x - world.Player.X);
            int distY = Math.Abs(y - world.Player.Y);
            int distance = Math.Max(distX, distY);

            Profile currentProfile = ProfileManager.CurrentProfile;
            float volume = currentProfile.SoundVolume / SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (world.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > world.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (currentProfile == null || !currentProfile.EnableSound || !Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            UOSound sound = (UOSound)Client.Game.UO.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume, distanceFactor))
            {
                sound.X = x;
                sound.Y = y;
                sound.CalculateByDistance = true;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            {
                return;
            }

            float volume;

            if (is_login)
            {
                volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
            }
            else
            {
                Profile currentProfile = ProfileManager.CurrentProfile;

                if (currentProfile == null || !currentProfile.EnableMusic)
                {
                    volume = 0;
                }
                else
                {
                    volume = currentProfile.MusicVolume / SOUND_DELTA;
                }

                if (currentProfile != null && !currentProfile.EnableCombatMusic && iswarmode)
                {
                    return;
                }
            }


            if (volume < -1 || volume > 1f)
            {
                return;
            }

            Sound m = Client.Game.UO.Sounds.GetMusic(music);

            if (m == null && _currentMusic[0] != null)
            {
                StopMusic();
            }
            else if (m != null && (m != _currentMusic[0] || iswarmode))
            {
                StopMusic();

                int idx = iswarmode ? 1 : 0;
                _currentMusicIndices[idx] = music;
                _currentMusic[idx] = (UOMusic) m;

                _currentMusic[idx].Play(Time.Ticks, volume);
            }
        }

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    float volume;

                    if (isLogin)
                    {
                        volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
                    }
                    else
                    {
                        Profile currentProfile = ProfileManager.CurrentProfile;

                        volume = currentProfile == null || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                    }


                    if (volume < -1 || volume > 1f)
                    {
                        return;
                    }

                    _currentMusic[i].Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                }
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            float volume = currentProfile == null || !currentProfile.EnableSound ? 0 : currentProfile.SoundVolume / SOUND_DELTA;

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            for (LinkedListNode<UOSound> soundNode = _currentSounds.First; soundNode != null; soundNode = soundNode.Next)
            {
                soundNode.Value.Volume = volume;
            }
        }

        public void StopMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    _currentMusic[i].Stop();
                    _currentMusic[i].Dispose();
                    _currentMusic[i] = null;
                }
            }

            //MobileUO: NOTE: Also discard sounds
            foreach (UOSound sound in _currentSounds)
            {
                if (sound != null)
                {
                    sound.Stop();
                    sound.Dispose();
                }
            }
            _currentSounds.Clear();

            DynamicSoundEffectInstance.DisposePool();
        }

        public void StopWarMusic()
        {
            PlayMusic(_currentMusicIndices[0]);
        }

        public void StopSounds()
        {
            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                first.Value.Stop();

                _currentSounds.Remove(first);

                first = next;
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            bool runninWarMusic = _currentMusic[1] != null;
            Profile currentProfile = ProfileManager.CurrentProfile;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && currentProfile != null)
                {
                    if (Client.Game.IsActive)
                    {
                        if (!currentProfile.ReproduceSoundsInBackground)
                        {
                            _currentMusic[i].Volume = i == 0 && runninWarMusic || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                        }
                    }
                    else if (!currentProfile.ReproduceSoundsInBackground && _currentMusic[i].Volume != 0.0f)
                    {
                        _currentMusic[i].Volume = 0;
                    }
                }

                _currentMusic[i]?.Update();
            }


            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                if (!first.Value.IsPlaying(Time.Ticks))
                {
                    first.Value.Stop();
                    _currentSounds.Remove(first);
                }

                first = next;
            }
        }

        public UOMusic GetCurrentMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && _currentMusic[i].IsPlaying(Time.Ticks))
                {
                    return _currentMusic[i];
                }
            }
            return null;
        }
    }
}
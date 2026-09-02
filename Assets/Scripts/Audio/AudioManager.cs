using SlotMachine.Utils;
using UnityEngine;

namespace SlotMachine.Audio
{
    /// <summary>
    /// Central place for every sound effect the game plays. Clips are
    /// generated procedurally at startup (see ProceduralTone) since no
    /// audio assets were supplied; this can be swapped for designer-authored
    /// AudioClips later without touching any calling code, since callers
    /// only ever ask for "PlaySpin", "PlayWin", etc.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        private AudioSource _source;

        private AudioClip _clickClip;
        private AudioClip _spinClip;
        private AudioClip _reelStopClip;
        private AudioClip _winClip;
        private AudioClip _bigWinClip;
        private AudioClip _loseClip;

        protected override void Awake()
        {
            base.Awake();
            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }
            _source.playOnAwake = false;

            _clickClip = ProceduralTone.GenerateBlip("sfx_click", 440f);
            _spinClip = ProceduralTone.GenerateBlip("sfx_spin", 260f);
            _reelStopClip = ProceduralTone.Generate("sfx_reel_stop", 200f, 0.08f, 0.3f);
            _winClip = ProceduralTone.Generate("sfx_win", 660f, 0.35f, 0.35f);
            _bigWinClip = ProceduralTone.Generate("sfx_big_win", 880f, 0.6f, 0.4f);
            _loseClip = ProceduralTone.Generate("sfx_lose", 150f, 0.3f, 0.3f);
        }

        public void PlayClick() => PlayOneShot(_clickClip);
        public void PlaySpinStart() => PlayOneShot(_spinClip);
        public void PlayReelStop() => PlayOneShot(_reelStopClip);
        public void PlayWin(bool isBigWin)
        {
            PlayOneShot(isBigWin ? _bigWinClip : _winClip);
        }
        public void PlayLose() => PlayOneShot(_loseClip);

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip);
        }
    }
}

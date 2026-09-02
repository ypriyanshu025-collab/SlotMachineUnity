using UnityEngine;

namespace SlotMachine.Audio
{
    /// <summary>
    /// The supplied art pack does not include any sound effects. Rather
    /// than ship the game silent, this generates a handful of very small
    /// procedural sine/square-wave "beep" AudioClips at runtime, so button
    /// clicks, spins and wins still get audible feedback out of the box.
    /// Swapping these for real SFX later is a one-line change in
    /// AudioManager (just assign real AudioClips instead of calling these
    /// generators).
    /// </summary>
    public static class ProceduralTone
    {
        private const int SampleRate = 44100;

        public static AudioClip Generate(string name, float frequencyHz, float durationSeconds, float volume = 0.35f, bool fadeOut = true)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = fadeOut ? Mathf.Clamp01(1f - (i / (float)sampleCount)) : 1f;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * volume * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A short two-note upward "blip" used for spin start / button presses.</summary>
        public static AudioClip GenerateBlip(string name, float baseFrequency)
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.12f);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float freq = baseFrequency * (1f + 2f * (i / (float)sampleCount));
                float envelope = Mathf.Clamp01(1f - (i / (float)sampleCount));
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * envelope;
            }
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

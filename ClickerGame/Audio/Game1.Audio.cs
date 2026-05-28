using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using ClickerGame;

namespace ClickerGame
{
    // Audio generation methods live here to keep Game1.cs focused on game logic.
    public partial class Game1
    {
        // ═══════════ SFX Generation ═══════════

        SoundEffect GenerateHitSfx()
        {
            int sr = 44100; int len = (int)(0.08f * sr);
            byte[] pcm = new byte[len * 2];
            for (int i = 0; i < len; i++)
            { float t = (float)i / sr; float v = ((float)Math.Sin(2 * Math.PI * 800 * t) * (float)Math.Exp(-t * 60) * 0.5f + (float)Math.Sin(2 * Math.PI * 250 * t) * (float)Math.Exp(-t * 30) * 0.5f) * 0.7f;
              short s = (short)(Math.Clamp(v, -1f, 1f) * short.MaxValue); pcm[i * 2] = (byte)(s & 0xFF); pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF); }
            return new SoundEffect(pcm, sr, AudioChannels.Mono);
        }

        SoundEffect GenerateMissSfx()
        {
            int sr = 44100; int len = (int)(0.1f * sr);
            byte[] pcm = new byte[len * 2]; Random mr = new(99);
            for (int i = 0; i < len; i++)
            { float t = (float)i / sr; float v = ((float)Math.Sin(2 * Math.PI * 80 * t) * (float)Math.Exp(-t * 20) * 0.4f + ((float)mr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 30) * 0.2f) * 0.4f;
              short s = (short)(Math.Clamp(v, -1f, 1f) * short.MaxValue); pcm[i * 2] = (byte)(s & 0xFF); pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF); }
            return new SoundEffect(pcm, sr, AudioChannels.Mono);
        }

        // ═══════════ Music Generation ═══════════

        void GenerateMusicalWav(string path, float dur, float bpm = 120f, double bassNote = 110.0)
        {
            int sr = 44100; int n = (int)(sr * dur);
            float[] mix = new float[n]; float bs = 60f / bpm;
            Random wr = new((int)(bpm * 100 + dur * 10));

            // Kick on every beat
            for (float bt = 0; bt < dur; bt += bs)
            { int st = (int)(bt * sr); int ln = Math.Min((int)(0.18f * sr), n - st); float ph = 0;
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; float fr = 150f * (float)Math.Exp(-t * 35) + 42f; ph += fr / sr;
                mix[st + i] += (float)Math.Sin(2 * Math.PI * ph) * (float)Math.Exp(-t * 7) * 0.45f; } }

            // Snare on beats 2,4
            for (float bt = bs; bt < dur; bt += bs * 2)
            { int st = (int)(bt * sr); int ln = Math.Min((int)(0.12f * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; float bd = (float)Math.Sin(2 * Math.PI * 200 * t) * (float)Math.Exp(-t * 22);
                float ns = ((float)wr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 16);
                mix[st + i] += bd * 0.3f + ns * 0.25f; } }

            // Hi-hat on 8ths
            for (float ht = 0; ht < dur; ht += bs / 2)
            { int st = (int)(ht * sr); int ln = Math.Min((int)(0.035f * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; mix[st + i] += ((float)wr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 90) * 0.13f; } }

            // Bass synth
            double[] bassNotes = { bassNote, bassNote * 1.333, bassNote * 1.5, bassNote * 1.25 };
            float md = bs * 4;
            for (float ms = 0; ms < dur; ms += md)
            { int ni = ((int)(ms / md)) % bassNotes.Length; double fr = bassNotes[ni];
              int st = (int)(ms * sr); int ln = Math.Min((int)(md * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; float ev = Math.Min(t * 20, 1f) * Math.Max(1f - t / md, 0f);
                float ph = (float)((fr * t) % 1.0); mix[st + i] += (ph * 2 - 1) * ev * 0.14f; } }

            NormalizeAndWriteWav(path, mix, n, sr);
        }

        void GenerateMenuMusicWav(string path, float dur, float bpm, double bassNote)
        {
            int sr = 44100; int n = (int)(sr * dur);
            float[] mix = new float[n]; float bs = 60f / bpm;
            Random wr = new(42);

            for (float bt = 0; bt < dur; bt += bs)
            { int st = (int)(bt * sr); int ln = Math.Min((int)(0.15f * sr), n - st); float ph = 0;
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; ph += (100f * (float)Math.Exp(-t * 25) + 35f) / sr;
                mix[st + i] += (float)Math.Sin(2 * Math.PI * ph) * (float)Math.Exp(-t * 9) * 0.25f; } }

            for (float ht = bs / 2; ht < dur; ht += bs)
            { int st = (int)(ht * sr); int ln = Math.Min((int)(0.025f * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; mix[st + i] += ((float)wr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 120) * 0.06f; } }

            double[] cn = { bassNote * 2, bassNote * 2.5, bassNote * 3, bassNote * 2.67 }; float cd = bs * 8;
            for (float cs = 0; cs < dur; cs += cd)
            { int ni = ((int)(cs / cd)) % cn.Length; double[] tr = { cn[ni], cn[ni] * 1.25, cn[ni] * 1.5 };
              int st = (int)(cs * sr); int ln = Math.Min((int)(cd * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; float ev = Math.Min(t * 4, 1f) * Math.Max(1f - t / cd * 0.3f, 0.5f);
                float v = 0; foreach (var f in tr) v += (float)Math.Sin(2 * Math.PI * f * t);
                mix[st + i] += v / 3f * ev * 0.1f; } }

            double[] bn = { bassNote, bassNote * 1.333, bassNote * 1.5, bassNote * 1.25 }; float md = bs * 4;
            for (float ms = 0; ms < dur; ms += md)
            { int ni = ((int)(ms / md)) % bn.Length; double fr = bn[ni];
              int st = (int)(ms * sr); int ln = Math.Min((int)(md * sr), n - st);
              for (int i = 0; i < ln && st + i < n; i++)
              { float t = (float)i / sr; mix[st + i] += (float)Math.Sin(2 * Math.PI * fr * t) * Math.Min(t * 10, 1f) * Math.Max(1f - t / md, 0f) * 0.12f; } }

            NormalizeAndWriteWav(path, mix, n, sr);
        }

        // Blue Archive style: bright piano+bell tone, upbeat pop drums, synth arpeggio, chord pads
        void GenerateBaStyleWav(string path, float dur, float bpm, double rootNote, int variation)
        {
            int sr = 44100; int n = (int)(sr * dur);
            float[] mix = new float[n]; float bs = 60f / bpm;
            Random wr = new(variation * 1000 + (int)(bpm * 10));

            // Major scale intervals: root, 2nd, 3rd, 5th, 6th, octave
            double[] scale = { 1.0, 9.0/8, 5.0/4, 3.0/2, 5.0/3, 2.0 };

            // Piano-bell lead melody (bright sine+harmonics with fast decay)
            double[] melody;
            switch (variation)
            {
                case 0: melody = new double[] { 1,5.0/4,3.0/2,2, 5.0/3,3.0/2,5.0/4,1, 9.0/8,5.0/4,3.0/2,5.0/3, 2,5.0/3,3.0/2,5.0/4 }; break;
                case 1: melody = new double[] { 2,5.0/3,3.0/2,5.0/4, 1,9.0/8,5.0/4,3.0/2, 5.0/3,2,5.0/3,3.0/2, 5.0/4,9.0/8,1,9.0/8 }; break;
                default: melody = new double[] { 3.0/2,2,5.0/3,3.0/2, 5.0/4,3.0/2,2,5.0/3, 1,5.0/4,3.0/2,5.0/4, 9.0/8,1,5.0/3,2 }; break;
            }

            // Lead melody - piano-like bell tones
            float noteLen = bs;
            for (int mi = 0; mi < (int)(dur / noteLen); mi++)
            {
                int idx = mi % melody.Length;
                double freq = rootNote * melody[idx];
                int st = (int)(mi * noteLen * sr);
                int ln = Math.Min((int)(noteLen * 0.9f * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                {
                    float t = (float)i / sr;
                    float env = (float)Math.Exp(-t * 4.5) * Math.Min(t * 200, 1f);
                    // Fundamental + octave + 3rd harmonic for bell-like timbre
                    float v = (float)Math.Sin(2 * Math.PI * freq * t) * 0.5f
                            + (float)Math.Sin(2 * Math.PI * freq * 2 * t) * 0.25f
                            + (float)Math.Sin(2 * Math.PI * freq * 3 * t) * 0.08f;
                    mix[st + i] += v * env * 0.22f;
                }
            }

            // Synth arpeggio (fast 16th note arpeggios on chord tones)
            double[][] chords = {
                new[] { 1.0, 5.0/4, 3.0/2 },
                new[] { 5.0/3/2, 1.0, 5.0/4 },
                new[] { 9.0/8, 3.0/2/1.2, 3.0/2 },
                new[] { 3.0/2, 15.0/8/1.0, 2.0 },
            };
            float arpLen = bs / 4;
            for (float at = 0; at < dur; at += arpLen)
            {
                int ci = ((int)(at / (bs * 4))) % chords.Length;
                int ai = ((int)(at / arpLen)) % chords[ci].Length;
                double freq = rootNote * 2 * chords[ci][ai];
                int st = (int)(at * sr);
                int ln = Math.Min((int)(arpLen * 0.7f * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                {
                    float t = (float)i / sr;
                    float env = (float)Math.Exp(-t * 12) * Math.Min(t * 400, 1f);
                    mix[st + i] += (float)Math.Sin(2 * Math.PI * freq * t) * env * 0.08f;
                }
            }

            // Upbeat kick (4-on-the-floor, punchy)
            for (float bt = 0; bt < dur; bt += bs)
            {
                int st = (int)(bt * sr); int ln = Math.Min((int)(0.12f * sr), n - st); float ph = 0;
                for (int i = 0; i < ln && st + i < n; i++)
                { float t = (float)i / sr; float fr = 180f * (float)Math.Exp(-t * 40) + 50f; ph += fr / sr;
                  mix[st + i] += (float)Math.Sin(2 * Math.PI * ph) * (float)Math.Exp(-t * 8) * 0.38f; }
            }

            // Snappy snare on 2 and 4
            for (float bt = bs; bt < dur; bt += bs * 2)
            {
                int st = (int)(bt * sr); int ln = Math.Min((int)(0.08f * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                { float t = (float)i / sr;
                  float body = (float)Math.Sin(2 * Math.PI * 240 * t) * (float)Math.Exp(-t * 28);
                  float noise = ((float)wr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 22);
                  mix[st + i] += (body * 0.2f + noise * 0.22f); }
            }

            // Bright hi-hat on 8ths with accented offbeats
            for (float ht = 0; ht < dur; ht += bs / 2)
            {
                bool offbeat = ((int)(ht / (bs / 2))) % 2 == 1;
                float vol = offbeat ? 0.14f : 0.08f;
                int st = (int)(ht * sr); int ln = Math.Min((int)(0.02f * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                { float t = (float)i / sr;
                  mix[st + i] += ((float)wr.NextDouble() * 2 - 1) * (float)Math.Exp(-t * 100) * vol; }
            }

            // Warm bass synth (sub + saw)
            double[] bassPattern = { 1.0, 1.0, 5.0/3/2, 3.0/2/2 };
            float bassLen = bs * 2;
            for (float bt = 0; bt < dur; bt += bassLen)
            {
                int bi = ((int)(bt / bassLen)) % bassPattern.Length;
                double freq = rootNote / 2 * bassPattern[bi];
                int st = (int)(bt * sr); int ln = Math.Min((int)(bassLen * 0.9f * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                { float t = (float)i / sr;
                  float env = Math.Min(t * 30, 1f) * Math.Max(1f - t / (bassLen * 0.9f), 0f);
                  float sub = (float)Math.Sin(2 * Math.PI * freq * t);
                  float saw = (float)((freq * t) % 1.0) * 2 - 1;
                  mix[st + i] += (sub * 0.7f + saw * 0.3f) * env * 0.14f; }
            }

            // Chord pad (warm synth pad on chord changes)
            float chordLen = bs * 4;
            for (float ct = 0; ct < dur; ct += chordLen)
            {
                int ci2 = ((int)(ct / chordLen)) % chords.Length;
                int st = (int)(ct * sr); int ln = Math.Min((int)(chordLen * sr), n - st);
                for (int i = 0; i < ln && st + i < n; i++)
                { float t = (float)i / sr;
                  float env = Math.Min(t * 3, 1f) * Math.Max(1f - t / chordLen * 0.4f, 0.3f);
                  float v = 0;
                  foreach (var c in chords[ci2])
                      v += (float)Math.Sin(2 * Math.PI * rootNote * c * t);
                  mix[st + i] += v / 3f * env * 0.06f; }
            }

            NormalizeAndWriteWav(path, mix, n, sr);
        }

        void NormalizeAndWriteWav(string path, float[] mix, int n, int sr)
        {
            float mx = 0; for (int i = 0; i < n; i++) mx = Math.Max(mx, Math.Abs(mix[i]));
            if (mx > 0.85f) { float sc = 0.8f / mx; for (int i = 0; i < n; i++) mix[i] *= sc; }

            using var fs = new FileStream(path, FileMode.Create);
            using var bw = new BinaryWriter(fs);
            int br = sr * 2; int sc2 = n * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); bw.Write(36 + sc2);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); bw.Write(16);
            bw.Write((short)1); bw.Write((short)1); bw.Write(sr); bw.Write(br);
            bw.Write((short)2); bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data")); bw.Write(sc2);
            for (int i = 0; i < n; i++)
            { short s = (short)(Math.Clamp(mix[i], -1f, 1f) * short.MaxValue); bw.Write(s); }
        }

        // ═══════════ Beatmap Generation ═══════════

        Beatmap GenerateBeatmapObject(float duration, float bpm, string diff)
        {
            float bs = 60f / bpm;
            float interval; double dc, tc;
            switch (diff)
            {
                case "very_difficulty": interval = bs / 4; dc = 0.45; tc = 0.15; break;
                case "difficulty": interval = bs / 3; dc = 0.30; tc = 0.05; break;
                case "hard": interval = bs / 2; dc = 0.25; tc = 0.0; break;
                default: interval = bs; dc = 0.0; tc = 0.0; break;
            }
            var nl = new List<Note>();
            var r = new Random((int)(bpm * 100 + duration * 7 + diff.GetHashCode()));
            for (float t = bs; t < duration - 0.5f; t += interval)
            {
                int col = r.Next(4);
                nl.Add(new Note { Time = (float)Math.Round(t, 3), Column = col });
                if (r.NextDouble() < dc) nl.Add(new Note { Time = (float)Math.Round(t, 3), Column = (col + 1 + r.Next(3)) % 4 });
                if (r.NextDouble() < tc) nl.Add(new Note { Time = (float)Math.Round(t, 3), Column = (col + 2) % 4 });
            }
            return new Beatmap { Notes = nl };
        }
    }
}

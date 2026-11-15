using System;
using System.Collections.Generic;
using UnityEngine;

// Sons générés par code (aucun fichier audio requis) : ambiance, clics, flashs de lumière, rugissement...
public static class ProceduralAudio
{
	private const int Rate = 22050;
	private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
	private static AudioSource uiSource;

	private delegate float Gen(float t);

	private static AudioClip Make(string name, float seconds, Gen gen)
	{
		AudioClip existing;
		if (cache.TryGetValue(name, out existing) && existing != null)
		{
			return existing;
		}
		int count = Mathf.Max(1, (int)(seconds * Rate));
		float[] data = new float[count];
		for (int i = 0; i < count; i++)
		{
			data[i] = Mathf.Clamp(gen(i / (float)Rate), -1f, 1f);
		}
		AudioClip clip = AudioClip.Create(name, count, 1, Rate, false);
		clip.SetData(data, 0);
		cache[name] = clip;
		return clip;
	}

	private static readonly System.Random rng = new System.Random(1234);

	private static float Noise()
	{
		return (float)(rng.NextDouble() * 2.0 - 1.0);
	}

	private static float Sine(float freq, float t)
	{
		return Mathf.Sin(2f * Mathf.PI * freq * t);
	}

	public static AudioClip Hum
	{
		get
		{
			return Make("pp_hum", 3f, t => 0.10f * Sine(50f, t) + 0.05f * Sine(100f, t) + 0.025f * Sine(150f, t) + 0.015f * Noise());
		}
	}

	public static AudioClip Buzz
	{
		get
		{
			return Make("pp_buzz", 0.6f, t =>
			{
				float env = Mathf.Exp(-t * 4f);
				float sq = Sine(120f, t) > 0f ? 1f : -1f;
				return (0.25f * sq + 0.35f * Noise()) * env;
			});
		}
	}

	public static AudioClip Click
	{
		get { return Make("pp_click", 0.06f, t => 0.35f * Sine(1800f, t) * Mathf.Exp(-t * 70f)); }
	}

	public static AudioClip Hover
	{
		get { return Make("pp_hover", 0.05f, t => 0.18f * Sine(1100f, t) * Mathf.Exp(-t * 80f)); }
	}

	public static AudioClip Thud
	{
		get { return Make("pp_thud", 0.6f, t => 0.9f * Sine(Mathf.Lerp(70f, 32f, t * 2f), t) * Mathf.Exp(-t * 7f)); }
	}

	public static AudioClip Heartbeat
	{
		get
		{
			return Make("pp_heart", 1.1f, t =>
			{
				float a = 0.8f * Sine(55f, t) * Mathf.Exp(-t * 14f);
				float t2 = t - 0.26f;
				float b = t2 > 0f ? 0.6f * Sine(48f, t2) * Mathf.Exp(-t2 * 14f) : 0f;
				return a + b;
			});
		}
	}

	public static AudioClip Roar
	{
		get
		{
			return Make("pp_roar", 1.5f, t =>
			{
				float env = Mathf.Min(1f, t * 30f) * Mathf.Exp(-t * 1.8f);
				float f = Mathf.Lerp(110f, 55f, t / 1.5f);
				float saw = ((t * f) % 1f) * 2f - 1f;
				float trem = 0.7f + 0.3f * Sine(38f, t);
				return (0.55f * saw + 0.5f * Noise()) * env * trem;
			});
		}
	}

	public static AudioClip Coin
	{
		get { return Make("pp_coin", 0.3f, t => (0.25f * Sine(988f, t) + 0.25f * Sine(1319f, Mathf.Max(0f, t - 0.07f))) * Mathf.Exp(-t * 9f)); }
	}

	public static AudioClip Alarm
	{
		get
		{
			return Make("pp_alarm", 1.2f, t =>
			{
				float f = ((int)(t * 4f)) % 2 == 0 ? 640f : 880f;
				float sq = Sine(f, t) > 0f ? 1f : -1f;
				return 0.18f * sq * (1f - (t / 1.2f) * 0.3f);
			});
		}
	}

	public static AudioClip Static
	{
		get
		{
			return Make("pp_static", 1.6f, t =>
			{
				float gate = Sine(11f, t) > -0.2f ? 1f : 0.2f;
				return 0.3f * Noise() * gate * Mathf.Exp(-t * 0.8f);
			});
		}
	}

	public static AudioClip Whoosh
	{
		get
		{
			return Make("pp_whoosh", 0.45f, t =>
			{
				float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.45f));
				return 0.4f * Noise() * env * (0.4f + 0.6f * Sine(Mathf.Lerp(900f, 200f, t / 0.45f), t));
			});
		}
	}

	public static AudioClip Spark
	{
		get { return Make("pp_spark", 0.35f, t => 0.35f * Noise() * Mathf.Exp(-t * 18f) * (Sine(40f, t) > 0f ? 1f : 0.3f)); }
	}

	public static AudioClip Success
	{
		get { return Make("pp_success", 0.7f, t => 0.22f * (Sine(523f, t) * Mathf.Exp(-t * 4f) + Sine(659f, Mathf.Max(0f, t - 0.12f)) * Mathf.Exp(-t * 4f) + Sine(784f, Mathf.Max(0f, t - 0.24f)) * Mathf.Exp(-t * 3f))); }
	}

	public static AudioSource Ui
	{
		get
		{
			if (uiSource == null)
			{
				GameObject go = new GameObject("PlaytimeUiAudio");
				UnityEngine.Object.DontDestroyOnLoad(go);
				uiSource = go.AddComponent<AudioSource>();
				uiSource.spatialBlend = 0f;
				uiSource.playOnAwake = false;
			}
			return uiSource;
		}
	}

	public static void Play2D(AudioClip clip, float volume)
	{
		if (clip == null)
		{
			return;
		}
		Ui.PlayOneShot(clip, volume);
	}

	public static void PlayAt(AudioClip clip, Vector3 position, float volume)
	{
		if (clip == null)
		{
			return;
		}
		AudioSource.PlayClipAtPoint(clip, position, volume);
	}

	// Crée une source en boucle attachée à un objet.
	public static AudioSource Loop(GameObject host, AudioClip clip, float volume, float spatial, float maxDistance)
	{
		AudioSource src = host.AddComponent<AudioSource>();
		src.clip = clip;
		src.loop = true;
		src.volume = volume;
		src.spatialBlend = spatial;
		src.maxDistance = maxDistance;
		src.rolloffMode = AudioRolloffMode.Linear;
		src.Play();
		return src;
	}
}

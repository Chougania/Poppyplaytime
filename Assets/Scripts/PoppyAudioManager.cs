using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Musiques du jeu : menu, salon, manche, thème du monstre, alerte quand Huggy te repère.
/// - vrais fondus enchaînés entre les pistes (plus de silence entre deux musiques) ;
/// - la musique de manche baisse quand Huggy te poursuit (ducking) puis revient ;
/// - battements de coeur qui s'accélèrent quand Huggy est proche ou te poursuit ;
/// - thème du monstre : 2D pour le joueur monstre, 3D sur Huggy pour les survivants (si monster_theme est fourni).
/// Pistes dans Assets/Resources/PoppyAudio/ : menu, lobby (optionnel), gameplay, monster_spotted, monster_theme.
/// </summary>
[DisallowMultipleComponent]
public class PoppyAudioManager : MonoBehaviour
{
    public static PoppyAudioManager Instance { get; private set; }

    [Header("Pistes (laisser vide = chargées depuis Resources/PoppyAudio)")]
    public AudioClip menuMusic;
    public AudioClip lobbyMusic;
    public AudioClip gameplayMusic;
    public AudioClip monsterMusic;
    public AudioClip monsterSpotted;

    [Header("Volumes")]
    [Range(0f, 1f)] public float menuVolume = 0.55f;
    [Range(0f, 1f)] public float lobbyVolume = 0.35f;
    [Range(0f, 1f)] public float gameplayVolume = 0.5f;
    [Range(0f, 1f)] public float monsterVolume = 0.55f;
    [Range(0f, 1f)] public float alertVolume = 1f;

    [Header("Transitions")]
    [Min(0.05f)] public float musicFadeSeconds = 1.6f;

    [Header("Poursuite")]
    [Tooltip("Volume de la musique de manche pendant que Huggy te poursuit.")]
    [Range(0f, 1f)] public float chaseDuckVolume = 0.25f;
    [Min(0f)] public float chaseHoldSeconds = 4f;

    [Header("Battements de coeur (généré par code)")]
    public bool heartbeat = true;
    [Min(1f)] public float heartbeatDistance = 16f;
    [Range(0f, 1f)] public float heartbeatVolume = 0.8f;

    [Header("Thème du monstre en 3D (survivants)")]
    [Min(1f)] public float monsterThemeMaxDistance = 28f;

    private class Layer
    {
        public AudioSource source;
        public float target;
    }

    private Layer layerA;
    private Layer layerB;
    private Layer current;
    private AudioSource alertSource;
    private AudioSource heartSource;
    private string activeTrack = "";
    private float duck = 1f;
    private float chaseUntil;
    private float nextScan;
    private HuggyHunterAI cachedAi;
    private AudioSource aiEmitter;
    private readonly HashSet<string> warned = new HashSet<string>();

    // ------------------------------------------------------------------ API
    public static PoppyAudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        PoppyAudioManager existing = FindObjectOfType<PoppyAudioManager>();
        if (existing != null) return existing;
        return new GameObject("PoppyAudioManager").AddComponent<PoppyAudioManager>();
    }

    public static void PlayMenuMusic()
    {
        PoppyAudioManager m = EnsureInstance();
        m.PlayTrack("menu", m.menuMusic, m.menuVolume);
    }

    // Salon / fin de manche : piste "lobby" si elle existe, sinon la musique du menu en plus discret.
    public static void PlayLobbyMusic()
    {
        PoppyAudioManager m = EnsureInstance();
        if (m.lobbyMusic != null) m.PlayTrack("lobby", m.lobbyMusic, m.lobbyVolume);
        else m.PlayTrack("lobbyMenu", m.menuMusic, m.lobbyVolume);
    }

    public static void PlayGameplayMusic()
    {
        PoppyAudioManager m = EnsureInstance();
        m.PlayTrack("gameplay", m.gameplayMusic, m.gameplayVolume);
    }

    // Joueur monstre : son thème, ou à défaut la musique de manche (jamais de silence).
    public static void PlayMonsterMusic()
    {
        PoppyAudioManager m = EnsureInstance();
        if (m.monsterMusic != null) m.PlayTrack("monster", m.monsterMusic, m.monsterVolume);
        else m.PlayTrack("gameplay", m.gameplayMusic, m.gameplayVolume);
    }

    public static void PlayMonsterSpotted()
    {
        PoppyAudioManager m = EnsureInstance();
        m.PlayAlert();
    }

    // Alerte ciblée : seul le joueur repéré l'entend à plein volume, les autres seulement s'ils sont proches de Huggy.
    public static void PlayMonsterSpotted(ulong spottedClientId)
    {
        PoppyAudioManager m = EnsureInstance();
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || spottedClientId == nm.LocalClientId)
        {
            m.PlayAlert();
            m.chaseUntil = Time.unscaledTime + m.chaseHoldSeconds;
        }
    }

    public static void StopMusic()
    {
        if (Instance == null) return;
        Instance.activeTrack = "";
        if (Instance.layerA != null) Instance.layerA.target = 0f;
        if (Instance.layerB != null) Instance.layerB.target = 0f;
    }

    // ------------------------------------------------------------------ cycle de vie
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        layerA = NewLayer();
        layerB = NewLayer();
        current = layerA;

        alertSource = gameObject.AddComponent<AudioSource>();
        alertSource.playOnAwake = false;
        alertSource.loop = false;
        alertSource.spatialBlend = 0f;

        heartSource = gameObject.AddComponent<AudioSource>();
        heartSource.playOnAwake = false;
        heartSource.loop = true;
        heartSource.spatialBlend = 0f;
        heartSource.volume = 0f;

        LoadMissingClips();
    }

    private Layer NewLayer()
    {
        Layer l = new Layer();
        l.source = gameObject.AddComponent<AudioSource>();
        l.source.playOnAwake = false;
        l.source.loop = true;
        l.source.spatialBlend = 0f;
        l.source.volume = 0f;
        return l;
    }

    private void LoadMissingClips()
    {
        if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("PoppyAudio/menu");
        if (lobbyMusic == null) lobbyMusic = Resources.Load<AudioClip>("PoppyAudio/lobby");
        if (gameplayMusic == null) gameplayMusic = Resources.Load<AudioClip>("PoppyAudio/gameplay");
        if (monsterSpotted == null) monsterSpotted = Resources.Load<AudioClip>("PoppyAudio/monster_spotted");
        if (monsterMusic == null) monsterMusic = Resources.Load<AudioClip>("PoppyAudio/monster_theme");
    }

    private void PlayAlert()
    {
        AudioClip clip = monsterSpotted;
        if (clip == null)
        {
            HuggyHunterAI ai = FindObjectOfType<HuggyHunterAI>();
            if (ai != null) clip = ai.alertSound; // ancien son d'alerte conservé en repli
        }
        if (clip != null) alertSource.PlayOneShot(clip, alertVolume);
    }

    // ------------------------------------------------------------------ fondu enchaîné
    private void PlayTrack(string id, AudioClip clip, float volume)
    {
        if (clip == null)
        {
            if (warned.Add(id))
                Debug.LogWarning("PoppyAudioManager : piste '" + id + "' absente (Assets/Resources/PoppyAudio/).", this);
            return;
        }
        if (activeTrack == id && current.source.clip == clip && current.target > 0f) return;

        activeTrack = id;
        Layer old = current;
        current = current == layerA ? layerB : layerA;
        old.target = 0f;

        current.source.clip = clip;
        current.source.volume = 0f;
        current.target = volume;
        current.source.Play();
    }

    private void UpdateLayer(Layer l, float rate)
    {
        float goal = l.target * (l == current ? duck : 1f);
        l.source.volume = Mathf.MoveTowards(l.source.volume, goal, rate);
        if (l.target <= 0f && l.source.volume <= 0.001f && l.source.isPlaying)
        {
            l.source.Stop();
            l.source.clip = null;
        }
    }

    // ------------------------------------------------------------------ ambiance dynamique
    private static DbUnityMovment LocalPlayer()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null) return null;
        return nm.LocalClient.PlayerObject.GetComponent<DbUnityMovment>();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float rate = dt / Mathf.Max(0.05f, musicFadeSeconds);
        UpdateLayer(layerA, rate);
        UpdateLayer(layerB, rate);

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 1f;
            cachedAi = FindObjectOfType<HuggyHunterAI>();
        }

        NetworkManager nm = NetworkManager.Singleton;
        bool session = nm != null && nm.IsListening;
        DbUnityMovment me = session ? LocalPlayer() : null;
        bool inRoundMusic = activeTrack == "gameplay" || activeTrack == "monster";
        bool iAmMonster = me != null && me.monsterModelContainer != null && me.monsterModelContainer.activeSelf;

        // poursuite : Huggy IA t'a pris pour cible
        bool chased = inRoundMusic && !iAmMonster && cachedAi != null && cachedAi.IsChasing && nm != null &&
                      cachedAi.ChaseTargetClientId == nm.LocalClientId;
        if (chased) chaseUntil = Time.unscaledTime + chaseHoldSeconds;
        float duckGoal = Time.unscaledTime < chaseUntil && inRoundMusic ? chaseDuckVolume : 1f;
        duck = Mathf.MoveTowards(duck, duckGoal, dt / 0.6f);

        UpdateHeartbeat(me, inRoundMusic && !iAmMonster, chased, dt);
        UpdateMonsterEmitter(inRoundMusic && !iAmMonster);
    }

    private void UpdateHeartbeat(DbUnityMovment me, bool active, bool chased, float dt)
    {
        float goal = 0f;
        float pitch = 1f;
        if (heartbeat && active && me != null && cachedAi != null)
        {
            float dist = Vector3.Distance(me.transform.position, cachedAi.transform.position);
            float proximity = Mathf.Clamp01(1f - dist / heartbeatDistance);
            float intensity = chased ? Mathf.Max(proximity, 0.8f) : proximity;
            goal = intensity * heartbeatVolume;
            pitch = 1f + intensity * 0.45f;
        }
        if (heartSource.clip == null) heartSource.clip = ProceduralAudio.Heartbeat;
        heartSource.pitch = Mathf.MoveTowards(heartSource.pitch, pitch, dt * 0.5f);
        heartSource.volume = Mathf.MoveTowards(heartSource.volume, goal, dt * 1.2f);
        if (goal > 0.02f && !heartSource.isPlaying) heartSource.Play();
        else if (goal <= 0.02f && heartSource.volume <= 0.01f && heartSource.isPlaying) heartSource.Stop();
    }

    // Thème du monstre en 3D porté par Huggy IA : on l'entend de plus en plus fort quand il approche.
    private void UpdateMonsterEmitter(bool active)
    {
        if (!active || monsterMusic == null || cachedAi == null)
        {
            if (aiEmitter != null) aiEmitter.volume = Mathf.MoveTowards(aiEmitter.volume, 0f, Time.unscaledDeltaTime);
            return;
        }
        if (aiEmitter == null || aiEmitter.transform.parent != cachedAi.transform)
        {
            GameObject go = new GameObject("MonsterThemeEmitter");
            go.transform.SetParent(cachedAi.transform, false);
            aiEmitter = go.AddComponent<AudioSource>();
            aiEmitter.clip = monsterMusic;
            aiEmitter.loop = true;
            aiEmitter.spatialBlend = 1f;
            aiEmitter.rolloffMode = AudioRolloffMode.Linear;
            aiEmitter.minDistance = 3f;
            aiEmitter.maxDistance = monsterThemeMaxDistance;
            aiEmitter.volume = 0f;
            aiEmitter.Play();
        }
        aiEmitter.volume = Mathf.MoveTowards(aiEmitter.volume, monsterVolume, Time.unscaledDeltaTime * 0.5f);
    }
}

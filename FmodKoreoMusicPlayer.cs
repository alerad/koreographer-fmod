using System;
using Archon.SwissArmyLib.Utils.Editor;
using MyBox;
using SonicBloom.Koreo;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Scripts.Audio.Music {
    [RequireComponent(typeof(Koreographer))]
    public class FmodKoreoMusicPlayer : MonoBehaviour, IKoreographedPlayer {

        public bool playOnAwake;
        public float startTime = 0;

        public bool loopSong;
        //[NonSerialized]

        [SerializeField]
        private bool manualConfig = false;

        [ConditionalField("manualConfig")]
        [ReadOnly]
        public string ___warning___  = "config MUST be null if current Scene is a playable level";
    
        [ConditionalField("manualConfig")]
        public MusicPlayerConfig config;
    
        private FMODAudioVisor visor;
        private FMODMusicPlayer musicPlayer;
        private Koreographer koreographer;
    
        private PauseMenu menu;
    
        private bool startedPlaying;
    
        private void Awake() {
            koreographer = Koreographer.Instance;
            koreographer.musicPlaybackController = this;
            menu = FindObjectOfType<PauseMenu>();
        
            SceneManager.sceneUnloaded += x => Stop();
            if (!manualConfig) {
                config = ConfigService.GetConfig<MusicPlayerConfig>();
            }

            SceneManager.sceneLoaded += (x,y) => {
                if (!manualConfig) {
                    config = ConfigService.GetConfig<MusicPlayerConfig>();
                }
                InstantiateAudio();
            };
        }

        private void Start() {
            if (playOnAwake)
                SetTimeAndPlay(startTime);
        
            if (menu != null && !menu.pauseMenuNavigation.IsMainMenu()) {
                menu.menuShown.Subscribe(x => {
                    if (x)
                        musicPlayer.Pause();
                    else
                        musicPlayer.Resume();
                });
            }

            if (loopSong)
                CreateLoopWatcher();
        }

        private void CreateLoopWatcher() {
            Observable
                .Interval(TimeSpan.FromMilliseconds(1000))
                .Where(_ => musicPlayer.GetSamples(config.clip.frequency) > config.clip.samples)
                .Where(_ => menu.pauseMenuNavigation.IsMainMenu() && !FMODMusicPlayer.awaitingCallback)
                .Subscribe(_ => {
                    RestartSong();
                    FMODMusicPlayer.awaitingCallback = true;
                });
        }

        private void Update() {
            if (!startedPlaying)
                return;
        
            if (GetIsPlaying())       
                visor.Update();
        }

        public void Stop() => musicPlayer.Stop();
  
        public void SetTimeAndPlay(float time) => SetSamplesAndPlay(SampleForSecond(time));

        public void SetSamplesAndPlay(int samples) {
            if (!startedPlaying) {
                LoadSong(samples);
                startedPlaying = true;
            }
        }
    
    
        private void InstantiateAudio() {
            koreographer.UnloadKoreography(config.koreo);
            koreographer.LoadKoreography(config.koreo);
            musicPlayer = new FMODMusicPlayer(config.clip);
            visor = new FMODAudioVisor(musicPlayer.fmodInstance, koreographer, config.clip);
        }


        private void LoadSong(int startSampleTime = 0) {
            musicPlayer.Start(startSampleTime);
            SeekToSample(startSampleTime);
        }

        private void RestartSong() {
            musicPlayer.JumpToSample(0);
            SeekToSample(0);
        }
    
        private void SeekToSample(int sampleTime) => visor.ResyncTimings(sampleTime);

        public int GetSampleTimeForClip(string clipName) => (int) visor.estimatedSamplePosition;

        public int GetTotalSampleTimeForClip(string clipName) => visor.totalSamples();

        public bool GetIsPlaying(string _ = null) => musicPlayer.GetIsPlaying();

        public float GetPitch(string clipName) => musicPlayer.GetPitch(clipName);

        public string GetCurrentClipName() => config.clip.name;
    
        private int SampleForSecond(float time) => Mathf.RoundToInt(time * config.clip.samples / config.clip.length) ;

    }
}

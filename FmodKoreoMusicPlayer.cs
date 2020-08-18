using System;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Koreographer))]
public class FmodKoreoMusicPlayer : MonoBehaviour, IKoreographedPlayer {
    public AudioClip clip;
    public bool playOnAwake;
    public float startTime = 0;
    private FMODAudioVisor visor;
    private FMODMusicPlayer musicPlayer;
    private Koreographer koreographer;


    private bool startedPlaying;

    private void Awake() {
        koreographer = Koreographer.Instance;
        koreographer.musicPlaybackController = this;
        InstantiateAudio();
    }

    private void Start() {
        if (playOnAwake)
            SetTimeAndPlay(startTime);
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
        koreographer.UnloadKoreography(koreographer.GetKoreographyAtIndex(0));
        koreographer.LoadKoreography(koreographer.GetKoreographyAtIndex(0));
        musicPlayer = new FMODMusicPlayer(clip);
        visor = new FMODAudioVisor(musicPlayer.fmodInstance, koreographer, clip);
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

    public string GetCurrentClipName() => clip.name;

    private int SampleForSecond(float time) => Mathf.RoundToInt(time * clip.samples / clip.length) ;

}

using System;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Code.Scripts.Audio.Music {
    /// <summary>
    /// Music player that loads an audio-clip on runtime.
    /// </summary>
    public class FMODMusicPlayer {
        private static string programmerSoundPath = "event:/Music/MusicPlayer";
        private static Sound audioClipSound;
        private AudioClip clip;
        public EventInstance fmodInstance;

        /// <summary>
        /// Only messy part in this code. When you make a timeline change, this sets to true untill the sound starts playing again.
        /// </summary>
        public static bool awaitingCallback;

        public FMODMusicPlayer(AudioClip clip) {
            this.clip = clip;
            audioClipSound = FMODSoundclipCreator.CreateSoundFromAudioClip(clip);
            CreateFmodInstance(programmerSoundPath);
        }

        /// <summary>
        /// Call this method to start playing the song
        /// </summary>
        /// <param name="sample">Sample at which to start playing the song</param>
        public void Start(int sample = 0) {
            fmodInstance.setTimelinePosition(RythmHelpers.SampleToMillis(sample));
            fmodInstance.start();
            RuntimeManager.StudioSystem.update();
        }
        
        public void Pause() => fmodInstance.setPaused(true);
        
        
        public float GetPitch(string clipName) {
            fmodInstance.getPitch(out float pitch);
            return pitch;
        }
          
        public void Resume() => fmodInstance.setPaused(false);
        
        public bool IsPaused() {
            fmodInstance.getPaused(out bool p);
            return p;
        }
        
        public void Stop() => fmodInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        public void JumpToSample(int sample) {
            awaitingCallback = true;
            fmodInstance.setTimelinePosition(RythmHelpers.SampleToMillis(sample));
        } 

        public bool GetIsPlaying(string _ = null) {
            fmodInstance.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING && !IsPaused();
        }

        public int GetSamples(int frequency) {
            fmodInstance.getTimelinePosition(out int timeMillis);
            var songSeconds = timeMillis / 1000;
            return songSeconds * frequency;
        }

        /// <summary>
        /// Creates a programmer sound for an event bank with a programmer sound
        /// </summary>
        private void CreateFmodInstance(string bankPath) {
            DSP dsp;
            RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out dsp);
            dsp.setParameterInt((int)DSP_FFT.WINDOWTYPE, (int)DSP_FFT_WINDOW.HANNING);
            dsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, 512);
    
            fmodInstance = RuntimeManager.CreateInstance(bankPath);
            fmodInstance.getChannelGroup(out ChannelGroup group);
            group.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, dsp);
            fmodInstance.setCallback(MusicEventCallback);
        }
        
        static RESULT MusicEventCallback(EVENT_CALLBACK_TYPE type, EventInstance instance, IntPtr parameterPtr) {

            switch (type) {
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND: {
                    MODE soundMode = MODE.LOOP_NORMAL | MODE.CREATECOMPRESSEDSAMPLE | MODE.NONBLOCKING;
                    var parameter = (PROGRAMMER_SOUND_PROPERTIES) Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));    
                    parameter.sound = audioClipSound.handle;
                    parameter.subsoundIndex = -1;
                    Marshal.StructureToPtr(parameter, parameterPtr, false);
                    break;
                }
                case EVENT_CALLBACK_TYPE.SOUND_PLAYED: {
                    awaitingCallback = false;
                    break;
                }
            }
            return RESULT.OK;
        }
        
        private int SampleToMillis(int samples) => samples / clip.frequency * 1000;
        
    }
}
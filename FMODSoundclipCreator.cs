using System;
using System.IO;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace Code.Scripts.Audio.Music {
    public class FMODSoundclipCreator {
        
        public static Sound CreateSoundFromAudioClip(AudioClip audioClip) {
            var samplesSize = audioClip.samples * audioClip.channels;
            var samples = new float[samplesSize];
            audioClip.GetData(samples, 0);

            var bytesLength = (uint)(samplesSize * sizeof(float));

            var soundInfo = new CREATESOUNDEXINFO();
            soundInfo.length = bytesLength;
            soundInfo.format = SOUND_FORMAT.PCMFLOAT;
            soundInfo.defaultfrequency = audioClip.frequency;
            soundInfo.numchannels = audioClip.channels;
            soundInfo.cbsize = Marshal.SizeOf(soundInfo);

            Sound sound;
            var result = RuntimeManager.CoreSystem.createSound("", MODE.OPENUSER, ref soundInfo, out sound);
            if (result == RESULT.OK) {
                IntPtr ptr1, ptr2;
                uint len1, len2;
                result = sound.@lock(0, bytesLength, out ptr1, out ptr2, out len1, out len2);
                if (result == FMOD.RESULT.OK) {
                    var samplesLength = (int) (len1 / sizeof(float));
                    Marshal.Copy(samples, 0, ptr1, samplesLength);
                    if (len2 > 0) {
                        Marshal.Copy(samples, samplesLength, ptr2, (int) (len2 / sizeof(float)));
                    }

                    result = sound.unlock(ptr1, ptr2, len1, len2);
                    if (result == RESULT.OK) {
                        result = sound.setMode(MODE.LOOP_NORMAL);
                        if (result == RESULT.OK) {
                            return sound;
                        }
                    }
                }
            }


            throw new InvalidDataException("There was an error processing this audioclip");
        }
    }
    
}
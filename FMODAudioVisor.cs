using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using SonicBloom.Koreo;
using SonicBloom.Koreo.Players;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class FMODAudioVisor : VisorBase
{
    protected EventInstance audioCom;
    protected EventDescription evtDescription;
    protected AudioClipID clipID;
    protected double dspPlayStartTime;
    public double estimatedSamplePosition => timeSamples();

    protected FrameStats lastFrameStats;
    protected bool bDidLoop;
    protected int dspBufferSize;
    protected int outputSampleRate;

    protected AudioClip clip;

    /// <summary>
    /// Private default constructor means we require a different constructor.
    /// </summary>
    private FMODAudioVisor() { }
    
    public FMODAudioVisor(EventInstance fmodInstance, Koreographer targetKoreographer, AudioClip clip) {
        koreographerCom = targetKoreographer;
        if (koreographerCom == null)
            koreographerCom = Koreographer.Instance;
        
        this.clip = clip;
        audioCom = fmodInstance;
        
        AudioSettings.OnAudioConfigurationChanged += UpdateAudioConfiguration;
        UpdateAudioConfiguration(false);
        
        lastFrameStats = GetFrameStats();
        audioCom.getDescription(out evtDescription);
        sourceSampleTime = GetAudioSampleTime();
    }
    
    public static double Clamp(double value, double min, double max)
    {
      if (value < min)
        return min;
      if (value > max)
        return max;
      return value;
    }

    public override void Update()
    {
      if (!GetIsAudioPlaying())
        return;
      ProcessUpdate();
      base.Update();
    }

    private void ProcessUpdate()
    {
      FrameStats frameStats = GetFrameStats();
      bDidLoop = false;
      int num1 = frameStats.dspBufferCount - lastFrameStats.dspBufferCount;
      double bufferSampleLength1 = GetBufferSampleLength(frameStats.pitch);
      double bufferSampleLength2 = GetBufferSampleLength(lastFrameStats.pitch);
      int samplesRemaining = GetAudioEndSampleExtent() - GetAudioStartSampleExtent() + 1;
      double num3 = (lastFrameStats.sourceSampleTime +  num1 * Math.Max(bufferSampleLength1, bufferSampleLength2)) % samplesRemaining;
      double num4 = Math.Max(bufferSampleLength1, bufferSampleLength2) * 2.0;
      double num5 = num3 - num4;
      double num6 = num3 + num4;
      if ((num6 <= samplesRemaining && (frameStats.sourceSampleTime > num6 || frameStats.sourceSampleTime < num5) || num6 > samplesRemaining && frameStats.sourceSampleTime < lastFrameStats.sourceSampleTime && ((frameStats.sourceSampleTime + samplesRemaining) > num6 || (frameStats.sourceSampleTime + samplesRemaining) < num5)) && Time.frameCount != 1)
      {
//        estimatedSamplePosition = frameStats.sourceSampleTime;
      }
      else
      {
        double num7 = 0.0;
        double num8 = bufferSampleLength1 * num7;
        double num9 = bufferSampleLength1 * (1.0 - num7);
        double sourceSampleTime = frameStats.sourceSampleTime;
        double num10 = estimatedSamplePosition + GetRawFrameTime() * (double) outputSampleRate * frameStats.pitch;
        double num11;
        if (sourceSampleTime < estimatedSamplePosition)
        {
          if (sourceSampleTime + bufferSampleLength2 * num7 >= estimatedSamplePosition && frameStats.sourceSampleTime == lastFrameStats.sourceSampleTime)
          {
            num11 = Clamp(num10, sourceSampleTime - num9, sourceSampleTime + num8);
          }
          else
          {
            double num12 = sourceSampleTime + samplesRemaining;
            num11 = Clamp(num10, num12 - num9, num12 + num8);
          }
        }
        else
          num11 = sourceSampleTime <= estimatedSamplePosition ? Clamp(num10, sourceSampleTime - num9, sourceSampleTime + num8) : (sourceSampleTime + bufferSampleLength2 * num7 < estimatedSamplePosition + (double) samplesRemaining || frameStats.sourceSampleTime != this.lastFrameStats.sourceSampleTime ? Clamp(num10, sourceSampleTime - num9, sourceSampleTime + num8) : Clamp(num10 + (double) samplesRemaining, sourceSampleTime - num9, sourceSampleTime + num8) - (double) samplesRemaining);
        if (num11 >= samplesRemaining)
        {
          num11 -= samplesRemaining;
          bDidLoop = true;
        }
//        estimatedSamplePosition = num11;
      }
      lastFrameStats = frameStats;
    }
    
    private double GetBufferSampleLength(float pitch)
    {
        return dspBufferSize * (clip.frequency / (double) outputSampleRate) *pitch;
    }
    
    public void ResyncTimings()
    {
        this.ResyncTimings(GetAudioSampleTime());
    }
    
    public void ResyncTimings(int targetSampleTime)
    {
        Mathf.Clamp(targetSampleTime, 0, this.GetAudioEndSampleExtent());
        sourceSampleTime = targetSampleTime;
        sampleTime = sourceSampleTime - 1;
//        estimatedSamplePosition = targetSampleTime;
    }
    
    public int GetCurrentTimeInSamples()
    {
        return Mathf.Max(0, sampleTime);
    }
    
    private FrameStats GetFrameStats()
    {
        FrameStats frameStats;
        audioCom.getPitch(out float pitch);
        frameStats.pitch = pitch;
        frameStats.sourceSampleTime = timeSamples();
        frameStats.dspBufferCount = GetTotalDSPBufferCount();
        
        return frameStats;
    }
    
    private int GetTotalDSPBufferCount(double atTime = 0.0)
    {
        atTime = atTime != 0.0 ? atTime : dspTime;
        return Convert.ToInt32(outputSampleRate * atTime / dspBufferSize);
    }

    
    private void UpdateAudioConfiguration(bool deviceChanged)
    {
        RuntimeManager.CoreSystem.getDSPBufferSize(out uint length, out int numbuffers);
        dspBufferSize = numbuffers;
        outputSampleRate = Settings.Instance.SampleRateSettings[0].Value;
        var a = AudioSettings.GetConfiguration();
        a.sampleRate = outputSampleRate;
        a.dspBufferSize = dspBufferSize;
    }

    public int timeSamples() {
        audioCom.getTimelinePosition(out int posMs);
        
        return posMs * (outputSampleRate / 1000);
    }

    public float dspTime => (float) timeSamples() / outputSampleRate;
    
    

    protected override string GetAudioName() {
        int instanceId = clip.GetInstanceID();
        if (instanceId != clipID.instanceID)
        {
            clipID.instanceID = instanceId;
            clipID.name = clip.name;
        }
        return clipID.name;
    }

    protected override bool GetIsAudioPlaying() {
        audioCom.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.PLAYING)
            return dspPlayStartTime <= dspTime;
        return false;
    }

    protected override bool GetIsAudioLooping() {
        return false;
    }

    protected override bool GetDidAudioLoop() {
        return bDidLoop;
    }

    protected override int GetAudioSampleTime() {
        return Convert.ToInt32(estimatedSamplePosition);
    }

    protected override int GetAudioStartSampleExtent() {
        return 0;
    }

    protected override int GetAudioEndSampleExtent() {
        return totalSamples() - 1;
    }

    public int sampleEnd => GetAudioEndSampleExtent();

    public int totalSamples() {
        evtDescription.getLength(out int length);
        var totSamples = length * (outputSampleRate / 1000);
        return totSamples;
    }

    protected override int GetDeltaTimeInSamples() {
        audioCom.getPitch(out float pitch);
        return (int) ((double) clip.frequency * pitch * GetRawFrameTime());
    }
    
    
    protected struct FrameStats
    {
        public int dspBufferCount;
        public int sourceSampleTime;
        public float pitch;
    }
}

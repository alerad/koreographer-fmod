# Background

After buying koreographer, and having my sound guy convince me of adding FMOD, I've spent at least two days of headaches trying to get them to work together.
Koreographer doesn't support FMOD integration by itself, so I had to make my own since it was kinda core for our game.

This MonoBehaviour lets you load songs from an audioclip and play them as if it were a normal unity music player.

Hope this saves headaches if someone reaches this repo!

PRS are welcome

# How to use
Create a programmer sound in your FMOD EventBank, make it really long. Change the const string "programmerSoundPath" in FMODMusicPlayer.cs. 

Add the "FmodKoreoMusicPlayer.cs" to any gameobject.

Enjoy.


# Explanation of each class

## FmodKoreoMusicPlayer

It's the main music player. Links loading the audio clip to FMOD while mantaining sync with koreographer.

## FMODMusicPlayer

Handles loading the AudioClip into FMOD and has helper functions.

## FMODAudioVisor

Syncs the FMOD timeline with the koreographer timeline. You will have a hard time and will cry reading this class. I had it too, and i'm still not sure how it works.



using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

/// Exposes
/// - an audio source list that is set by a user
/// - The SoundUI's state
public class SoundUiAudioSourceStore : UdonSharpBehaviour {
    public GameObject audioSourceList;
    public Text playingAudioName;

    private readonly string NOT_PLAYING_NOW = "<Not playing now>";

    private AudioSource[] audioSources;
    private int paused = -1;

    /// Returns audio sources that is audioSourceList's children.
    /// Or return the empty array if audioSourceList has not set.
    ///
    /// This doesn't return null.
    public AudioSource[] GetAudioSources() {
        if (this.audioSources == null) {
            Debug.Log("SoundUiAudioSourceStore: Gotten audio sources is null. Return the empty array instead.");
            return new AudioSource[0];
        }
        Debug.Log($"SoundUiAudioSourceStore: Gotten audio sources size is ${this.audioSources.Length}");

        return this.audioSources;
    }

    /// Returns a paused audio source's index (non negative number) if it is existent.
    /// Or returns -1.
    public int GetPaused() {
        return this.paused;
    }

    public void UnsetPaused() {
        this.playingAudioName.text = NOT_PLAYING_NOW;
        this.paused = -1;
    }

    /// Pauses the i-th audio source,
    /// and saves the index i.
    /// Also show the audio source name on the text.
    ///
    /// <see cref="UnPauseLatest"/>
    public void Pause(int i) {
        var audioSource = this.audioSources[i];
        audioSource.Pause();

        this.playingAudioName.text = audioSource.name;
        this.paused = i;
    }

    /// Unpauses the latestly paused audio source, and returns true.
    /// Or return false if no one is paused or the one already has been unpaused.
    ///
    /// Also show the audio source name on the text.
    ///
    /// <see cref="Pause"/>
    public bool UnPauseLatest() {
        if (this.paused == -1) {
            Debug.Log("SoundUiAudioSourceStore: UnPauseLatest(): no one has been paused.");
            return false;
        }

        var audioSource = this.audioSources[this.paused];
        audioSource.UnPause();
        this.playingAudioName.text = audioSource.name;

        this.paused = -1;
        return true;
    }

    /// Play the i-th audio source if it is existent.
    /// Also show the audio source name on the text.
    public void Play(int i) {
        if (i < 0 || this.audioSources.Length <= i) {
            Debug.Log($"SoundUiAudioSourceStore: Play(): A got index is out of bounds: ${i}");
            return;
        }

        var audioSource = this.audioSources[i];
        audioSource.Play();

        this.playingAudioName.text = audioSource.name;
    }

    /// Stop the i-th audio source if it is existent.
    /// Also clear the text.
    public void Stop(int i) {
        if (i < 0 || this.audioSources.Length <= i) {
            Debug.Log($"SoundUiAudioSourceStore: Stop(): A got index is out of bounds: ${i}");
            return;
        }

        this.audioSources[i].Stop();
        this.playingAudioName.text = NOT_PLAYING_NOW;
    }

    public void StopAll() {
        foreach (var audioSource in this.audioSources) {
            audioSource.Stop();
        }
    }

    void Start() {
        if (this.audioSourceList == null) {
            Debug.Log("SoundUiAudioSourceList: The audio source list has not set.");
            return;
        }

        var components = this.audioSourceList.GetComponentsInChildren(typeof(AudioSource));
        if (components == null) {
            Debug.Log("SoundUiAudioSourceList: Gotten components is null.");
            return;
        }
        Debug.Log($"SoundUiAudioSourceList: Gotten components size is ${components.Length}");

        this.audioSources = new AudioSource[components.Length];
        for (var i = 0; i < components.Length; i++) {
            this.audioSources[i] = this.getAudioSource(components[i].gameObject);
        }
    }

    private AudioSource getAudioSource(GameObject x) {
        var audioSource = (AudioSource)x.GetComponent(typeof(AudioSource));

        if (audioSource == null) {
            Debug.Log("SoundUiAudioSourceList: [warning] a null audio source is found.");
            return null;
        }

        return audioSource;
    }
}

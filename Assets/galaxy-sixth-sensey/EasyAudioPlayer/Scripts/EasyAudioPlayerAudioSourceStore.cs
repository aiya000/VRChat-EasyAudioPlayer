using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Exposes
/// - an audio source list that is set by a user
/// - The EasyAudioPlayer's state
/// - Operations for the audio sources (EasyAudioPlayer scripts should use this instead of AudioSource methods.)
/// </summary>
public class EasyAudioPlayerAudioSourceStore : UdonSharpBehaviour {
    public GameObject audioSourceList;
    public Text playingAudioName;

    private readonly string NOT_PLAYING_NOW = "<Not playing now>";

    private AudioSource[] audioSources;
    private int paused = -1;
    private int playing = -1;  // Now playing AudioSource's index

    public void Start() {
        if (this.audioSourceList == null) {
            Debug.Log("EasyAudioPlayerAudioSourceStore: The audio source list has not set.");
            return;
        }

        var components = this.audioSourceList.GetComponentsInChildren(typeof(AudioSource));
        if (components == null) {
            Debug.Log("EasyAudioPlayerAudioSourceStore: Gotten components is null.");
            return;
        }
        Debug.Log($"EasyAudioPlayerAudioSourceStore: Gotten components size is {components.Length}");

        this.audioSources = new AudioSource[components.Length];
        for (var i = 0; i < components.Length; i++) {
            this.audioSources[i] = this.getAudioSource(components[i].gameObject);

            // Set this.playing to the first one which is playing
            if (this.playing == -1 && this.audioSources[i].isPlaying) {
                Debug.Log($"EasyAudioPlayerAudioSourceStore: Start(): Set this.playing to {i}");
                this.playing = i;
            }
        }

        // Stop all excluding the one which is the playing
        this.StopAll();

        if (this.playing != -1) {
            this.Play(this.playing);
        }
    }

    /// <summary>
    /// Returns audio sources that is audioSourceList's children.
    /// Or return the empty array if audioSourceList has not set.
    ///
    /// This doesn't return null.
    /// </summary>
    public AudioSource[] GetAudioSources() {
        if (this.audioSources == null) {
            Debug.Log("EasyAudioPlayerAudioSourceStore: Gotten audio sources is null. Return the empty array instead.");
            return new AudioSource[0];
        }
        Debug.Log($"EasyAudioPlayerAudioSourceStore: Gotten audio sources size is {this.audioSources.Length}");

        return this.audioSources;
    }

    public bool IsNotOutOfBoundsOnAudioSources(int index) {
        return 0 <= index && index < this.audioSources.Length;
    }

    /// <summary>
    /// Pauses the i-th audio source,
    /// and saves the index i.
    /// Also show the audio source name on the text.
    ///
    /// <see cref="UnPauseLatest"/>
    /// </summary>
    public void Pause(int i) {
        var audioSource = this.audioSources[i];
        audioSource.Pause();
        this.playingAudioName.text = audioSource.name;

        this.paused = i;
        Debug.Log($"EasyAudioPlayerAudioSourceStore: Pause(): {i}-th is paused.");
    }

    /// <summary>
    /// Pauses a currently playing audio source.
    /// Return false if no one is playing now.
    /// </summary>
    public bool PausePlaying() {
        if (!IsNotOutOfBoundsOnAudioSources(this.playing)) {
            Debug.Log($"EasyAudioPlayerAudioSourceStore: PausePlaying(): {this.playing} is out of bounds. Skip.");
            return false;
        }

        var result = this.audioSources[this.playing].isPlaying;
        this.Pause(this.playing);
        Debug.Log($"EasyAudioPlayerAudioSourceStore: PausePlaying(): {this.playing} is paused.");
        return result;
    }

    /// <summary>
    /// Unpauses the latestly paused audio source, and returns true.
    /// Or return false if no one is paused or the one already has been unpaused.
    ///
    /// Also show the audio source name on the text.
    ///
    /// <see cref="Pause"/>
    /// </summary>
    public bool UnPauseLatest() {
        if (this.paused == -1) {
            Debug.Log("EasyAudioPlayerAudioSourceStore: UnPauseLatest(): no one was playing. Skip.");
            return false;
        }

        var audioSource = this.audioSources[this.paused];
        audioSource.UnPause();
        this.playingAudioName.text = audioSource.name;

        Debug.Log($"EasyAudioPlayerAudioSourceStore: UnPauseLatest(): {this.paused} is unpaused.");
        this.paused = -1;
        return true;
    }

    /// <summary>
    /// Play the i-th audio source if it is existent.
    /// Also show the audio source name on the text.
    /// </summary>
    public void Play(int i) {
        if (i < 0 || this.audioSources.Length <= i) {
            Debug.Log($"EasyAudioPlayerAudioSourceStore: Play(): A got index is out of bounds: {i}");
            return;
        }

        var audioSource = this.audioSources[i];
        audioSource.Play();
        this.playing = i;
        Debug.Log($"EasyAudioPlayerAudioSourceStore: Play(): {i} started to play.");

        this.playingAudioName.text = audioSource.name;
    }

    public int StopLatest() {
        if (this.playing < 0 || this.audioSources.Length <= this.playing) {
            Debug.Log($"EasyAudioPlayerAudioSourceStore: Stop(): A got index is out of bounds: {this.playing}. Skip.");
            return -1;
        }

        this.audioSources[this.playing].Stop();
        this.playingAudioName.text = NOT_PLAYING_NOW;

        var playing = this.playing;
        this.playing = -1;
        return playing;
    }

    public void StopAll() {
        foreach (var audioSource in this.audioSources) {
            audioSource.Stop();
        }

        this.playingAudioName.text = NOT_PLAYING_NOW;
        this.paused = -1;
        this.playing = -1;
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

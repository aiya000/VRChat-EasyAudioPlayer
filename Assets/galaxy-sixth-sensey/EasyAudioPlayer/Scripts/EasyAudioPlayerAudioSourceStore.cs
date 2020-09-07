using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Exposes
/// - an audio source list that is set by a user
/// - The EasyAudioPlayer's state
/// - Operations for the audio sources (EasyAudioPlayer scripts should use this instead of AudioSource methods.)
///
/// To synchronize on the network of users, operations are splited to setter methods and Apply().
/// The setter methods should be called only by the owner.
/// Also Apply() should be called everyone after a setter is called.
///
/// <see cref="Apply"/>
/// </summary>
public class EasyAudioPlayerAudioSourceStore : UdonSharpBehaviour {
    // Do update manually.
    private readonly bool DEBUG = true;

    public GameObject audioSourceList;
    public Text playingAudioName;

    /// <summary>
    /// To be referenced by other classes.
    /// </summary>
    public bool isWorkingOnlyOnLocal = false;

    /// <summary>
    /// Please see <see cref="Apply"/>.
    /// </summary>
    public BoolSync paused;
    public BoolSync unPaused;
    public IntSync playing;

    private readonly string NOT_PLAYING_NOW = "<Not playing now>";

    private AudioSource[] audioSources;

    public void Start() {
        this.initializeFields();

        if (this.audioSourceList == null) {
            this.log("Start(): Error! The audio source list has not set.");
            return;
        }

        var components = this.audioSourceList.GetComponentsInChildren(typeof(AudioSource));
        if (components == null) {
            this.log("Start(): Error! Gotten components is null.");
            return;
        }
        this.log($"Start(): Gotten components size is {components.Length}");

        this.audioSources = new AudioSource[components.Length];
        for (var i = 0; i < components.Length; i++) {
            this.audioSources[i] = this.getAudioSource(components[i].gameObject);

            // Set this.playing to the first one which is playing
            if (this.playing.Get() == -1 && this.audioSources[i].isPlaying) {
                this.log($"Start(): Set this.playing to {i}");
                this.playing.Set(i);
            }
        }
    }

    public void Update() {
        // TODO
    }

    /// <summary>
    /// Transforms the state.
    /// </summary>
    public void PrepareToPlayFirstOrPauseOrUnpause() {
        // if now stopping
        if (this.paused.Get() && this.unPaused.Get()) {
            this.prepareToPlayFirst();
            return;
        }

        // if already paused
        if (this.paused.Get()) {
            this.prepareToUnPause();
            return;
        }

        // if some one is playing
        if (this.playing.Get() != -1) {
            this.prepareToPause();
            return;
        }

        // if no one is playing
        this.prepareToPlayFirst();
    }

    public void PrepareToStop() {
        this.log("PrepareToStop()");

        this.playing.Set(-1);
        this.paused.Set(true);
        this.unPaused.Set(true);
    }

    public void PrepareToPlayNext() {
        this.log("PrepareToPlayNext()");

        if (this.playing.Get() == this.audioSources.Length - 1) {
            this.log("PrepareToPlayNext(): this.playing arrived at the ending of the audio sources.");
            this.PrepareToStop();
            return;
        }

        this.paused.Set(false);
        this.unPaused.Set(false);
        this.playing.Set(this.playing.Get() + 1);
    }

    public void PrepareToPlayPrevious() {
        this.log("PrepareToPlayPrevious()");

        if (this.playing.Get() <= 0) {
            this.log("PrepareToPlayPrevious(): this.playing arrived at the beginning of the audio sources.");
            this.PrepareToStop();
            return;
        }

        this.paused.Set(false);
        this.unPaused.Set(false);
        this.playing.Set(this.playing.Get() - 1);
    }

    // TODO: What is a more better way?
    /// <summary>
    /// Waits little secs to synchronize UdonSynced variables.
    /// </summary>
    public void WaitToSync() {
        const int VERY_VERY_RANDOM_VALUE = 100000;
        for (var i = 0; i < VERY_VERY_RANDOM_VALUE; i++) {}
    }

    /// <summary>
    /// To synchronize the state with network users.
    ///
    /// This does
    /// - stop all audio sources (if `paused and unPaused`)
    /// - pause the current playing audio source (if `paused`)
    /// - unpause the latestly paused audio source (if `unPaused`)
    /// - play the specified (== this.playing) audio source (else if)
    ///
    /// NOTE: `paused and unPaused` may not make feeling, but it maybe right.
    /// </summary>
    public void Apply() {
        if (this.audioSources == null) {
            this.log("Apply(): Gotten audio sources is null. Skip.");
            return;
        }

        if (this.paused.Get() && this.unPaused.Get()) {
            this.stop();
            return;
        }

        if (this.paused.Get()) {
            this.pause();
            return;
        }

        if (this.unPaused.Get()) {
            this.unPause();
            return;
        }

        if (!this.isNotOutOfBoundsOnAudioSources(this.playing.Get())) {
            this.log($"Apply(): Error! this.playing is out of bounds: {this.playing.Get()} of {this.audioSources.Length}");
            return;
        }

        this.play();
    }

    private void initializeFields() {
        this.paused.Set(false);
        this.paused.SetDebugMode(DEBUG);

        this.unPaused.Set(false);
        this.unPaused.SetDebugMode(DEBUG);

        this.playing.Set(-1);
        this.playing.SetDebugMode(DEBUG);
    }

    private bool isNotOutOfBoundsOnAudioSources(int index) {
        return (0 <= index) && (index < this.audioSources.Length);
    }

    private void stopAll() {
        foreach (var audioSource in this.audioSources) {
            audioSource.Stop();
        }
    }

    private string getStateInfo() {
        return $@"{{
            audioSourceLength: {this.audioSources.Length},
            paused: {this.paused.Get()},
            unPaused: {this.unPaused.Get()},
            playing: {this.playing.Get()}
        }}";
    }

    private void logStateInfo() {
        if (DEBUG) {
            this.log(this.getStateInfo());
        }
    }

    private void log(string message) {
        if (DEBUG) {
            Debug.Log($"EasyAudioPlayerAudioSourceStore: {message}");
        }
    }

    private AudioSource getAudioSource(GameObject x) {
        var audioSource = (AudioSource)x.GetComponent(typeof(AudioSource));

        if (audioSource == null) {
            this.log("getAudioSource(): Warning. a null audio source is found.");
            return null;
        }

        return audioSource;
    }

    private void prepareToUnPause() {
        this.log("prepareToUnPause()");

        this.unPaused.Set(true);
        this.paused.Set(false);
    }

    private void prepareToPause() {
        this.log("prepareToPause()");

        this.unPaused.Set(false);
        this.paused.Set(true);
    }

    private void prepareToPlayFirst() {
        this.log("prepareToPlayFirst()");

        this.playing.Set(0);
        this.paused.Set(false);
        this.unPaused.Set(false);
    }

    private void stop() {
        this.log("stop(): Start.");
        this.logStateInfo();

        this.stopAll();
        this.playingAudioName.text = NOT_PLAYING_NOW;
        this.log("stop(): End.");
    }

    private void pause() {
        this.log("pause(): Start.");
        this.logStateInfo();

        if (!this.isNotOutOfBoundsOnAudioSources(this.playing.Get())) {
            this.log($"pause(): Error! this.playing is out of bounds: {this.playing.Get()} of {this.audioSources.Length}");
            return;
        }
        var current = this.audioSources[this.playing.Get()];

        current.Pause();
        this.playingAudioName.text = current.name;

        this.log("pause(): End.");
    }

    private void unPause() {
        this.log("unPause(): Start.");
        this.logStateInfo();

        if (!this.isNotOutOfBoundsOnAudioSources(this.playing.Get())) {
            this.log($"unPause(): Error! this.playing is out of bounds: {this.playing.Get()} of {this.audioSources.Length}");
            return;
        }
        var current = this.audioSources[this.playing.Get()];

        current.UnPause();
        this.playingAudioName.text = current.name;

        this.log("unPause(): End.");
    }

    private void play() {
        this.log("play(): Start.");
        this.logStateInfo();

        if (!this.isNotOutOfBoundsOnAudioSources(this.playing.Get())) {
            this.log($"play(): Error! this.playing is out of bounds: {this.playing.Get()} of {this.audioSources.Length}");
            return;
        }
        var next = this.audioSources[this.playing.Get()];

        this.stopAll();
        next.Play();
        this.playingAudioName.text = next.name;

        this.log("play(): End.");
    }

    private void playFirst() {
        if (this.audioSources.Length == 0) {
            this.log($"playFirst(): this.audioSources is empty. Skip.");
            return;
        }
        var first = this.audioSources[0];

        first.Play();
        this.playingAudioName.text = first.name;
    }
}

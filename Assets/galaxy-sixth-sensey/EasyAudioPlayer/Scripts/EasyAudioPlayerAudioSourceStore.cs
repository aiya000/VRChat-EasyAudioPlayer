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
    public GameObject audioSourceList;
    public Text playingAudioName;

    /// <summary>
    /// To be referenced by other classes.
    /// </summary>
    public bool isWorkingOnlyOnLocal = false;

    private readonly string NOT_PLAYING_NOW = "<Not playing now>";

    private AudioSource[] audioSources;

    // Please see Apply().
    [UdonSynced(UdonSyncMode.None)]
    private bool paused = false;

    // Please see Apply().
    [UdonSynced(UdonSyncMode.None)]
    private bool unPaused = false;

    // Please see Apply().
    [UdonSynced(UdonSyncMode.None)]
    private int playing = -1;

    // Do update manually.
    private readonly bool DEBUG = true;

    public void Start() {
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
            if (this.playing == -1 && this.audioSources[i].isPlaying) {
                this.log($"Start(): Set this.playing to {i}");
                this.setPlaying(i);
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
        // if already paused
        if (this.paused) {
            this.prepareToUnpause();
            return;
        }

        // if some one is playing
        if (this.playing != -1) {
            this.prepareToPause();
            return;
        }

        // if no one is playing
        this.prepareToPlayFirst();
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

        if (this.paused && this.unPaused) {
            this.log("Apply(): stop() will start.");
            this.logStateInfo();
            this.stop();
            this.log("Apply(): stop() is end.");
            return;
        }

        if (this.paused) {
            this.log("Apply(): pause() will start.");
            this.logStateInfo();
            this.pause();
            this.log("Apply(): pause() is end.");
            return;
        }

        if (this.unPaused) {
            this.log("Apply(): unPause() will start.");
            this.logStateInfo();
            this.unPause();
            this.log("Apply(): unPause() is end.");
            return;
        }

        if (!this.isNotOutOfBoundsOnAudioSources(this.playing)) {
            this.log($"Apply(): Error! this.playing is out of bounds: {this.playing} of {this.audioSources.Length}");
            return;
        }

        this.log("Apply(): play() will start.");
        this.logStateInfo();
        this.play();
        this.log("Apply(): play() is end.");
    }

    private bool isNotOutOfBoundsOnAudioSources(int index) {
        return (0 <= index) && (index < this.audioSources.Length);
    }

    private void stopAll() {
        foreach (var audioSource in this.audioSources) {
            audioSource.Stop();
        }
    }

    private void setPaused(bool val) {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        this.log($"setPaused(): from {this.paused} to {val}");
        this.paused = val;
    }

    private void setUnPaused(bool val) {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        this.log($"setUnPaused(): from {this.unPaused} to {val}");
        this.unPaused = val;
    }

    private void setPlaying(int val) {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        this.log($"setPlaying(): from {this.playing} to {val}");
        this.playing = val;
    }

    private string getStateInfo() {
        return $@"{{
            audioSourceLength: {this.audioSources.Length},
            paused: {this.paused},
            unPaused: {this.unPaused},
            playing: {this.playing}
        }}";
    }

    private void logStateInfo() {
        if (this.DEBUG) {
            this.log(this.getStateInfo());
        }
    }

    private void log(string message) {
        if (this.DEBUG) {
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

    private void prepareToUnpause() {
        this.setUnPaused(true);
        this.setPaused(false);
    }

    private void prepareToPause() {
        this.setUnPaused(false);
        this.setPaused(true);
    }

    private void prepareToPlayFirst() {
        this.setPlaying(0);
        this.setPaused(false);
        this.setUnPaused(false);
    }

    private void stop() {
        this.stopAll();
        this.playingAudioName.text = NOT_PLAYING_NOW;
    }

    private void pause() {
        if (!this.isNotOutOfBoundsOnAudioSources(this.playing)) {
            this.log($"pause(): Error! this.playing is out of bounds: {this.playing} of {this.audioSources.Length}");
            return;
        }
        var current = this.audioSources[this.playing];

        current.Pause();
        this.playingAudioName.text = current.name;
    }

    private void unPause() {
        if (!this.isNotOutOfBoundsOnAudioSources(this.playing)) {
            this.log($"unPause(): Error! this.playing is out of bounds: {this.playing} of {this.audioSources.Length}");
            return;
        }
        var current = this.audioSources[this.playing];

        current.UnPause();
        this.playingAudioName.text = current.name;
    }

    private void play() {
        if (!this.isNotOutOfBoundsOnAudioSources(this.playing)) {
            this.log($"play(): Error! this.playing is out of bounds: {this.playing} of {this.audioSources.Length}");
            return;
        }
        var next = this.audioSources[this.playing];

        next.Play();
        this.playingAudioName.text = next.name;
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

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

public class EasyAudioPlayerPlayOrPause : UdonSharpBehaviour {
    public EasyAudioPlayerAudioSourceStore core;

    /// <summary>
    /// Plays (Unpauses) an audio source that has been paused.
    /// Pauses an audio source that is playing now.
    /// Or plays the first audio source if no one is playing now.
    /// </summary>
    public override void Interact() {
        if (this.core == null) {
            Debug.Log("EasyAudioPlayerPlayOrPause: The core has not set.");
            return;
        }

        if (this.core.isWorkingOnlyOnLocal) {
            this.PlayOrPause();
        } else {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "PlayOrPause");
        }
    }

    public void PlayOrPause() {
        if (this.core.UnPauseLatest()) {
            return;
        }

        if (this.core.PausePlaying()) {
            return;
        }

        this.core.Play(0);
    }
}

using System.Collections;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

// TODO: Rename to EasyAudioPlayerPlayOrPauseOrUnpause
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

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) {
            this.core.PrepareToPlayFirstOrPauseOrUnpause();
        }

        for (var i = 0; i < 100000; i++) {}  // Wait to sync variables
        this.SendCustomNetworkEvent(NetworkEventTarget.All, "Apply");
    }

    public void Apply() {
        this.core.Apply();
    }
}

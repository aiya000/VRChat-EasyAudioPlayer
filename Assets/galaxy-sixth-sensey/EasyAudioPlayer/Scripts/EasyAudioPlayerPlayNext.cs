using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

public class EasyAudioPlayerPlayNext : UdonSharpBehaviour {
    public EasyAudioPlayerAudioSourceStore core;

    /// <summary>
    /// Do nothing if core is null or core size is 0.
    /// Play the first if no one is playing now or the last is playing now.
    /// Or play the next of the one that is playing now or the one has been paused.
    /// </summary>
    public override void Interact() {
        if (this.core == null) {
            Debug.Log("EasyAudioPlayerPlayNext: The core has not set.");
            return;
        }

        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) {
            this.core.PrepareToPlayNext();
        }

        for (var i = 0; i < 100000; i++) {}  // Wait to sync variables
        this.SendCustomNetworkEvent(NetworkEventTarget.All, "Apply");
    }

    public void Apply() {
        this.core.Apply();
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

public class EasyAudioPlayerStop : UdonSharpBehaviour {
    public EasyAudioPlayerAudioSourceStore core;

    public override void Interact() {
        if (this.core == null) {
            Debug.Log("EasyAudioPlayerStop: The core has not set.");
            return;
        }

        this.core.PrepareToStop();
        this.core.WaitToSync();
        this.SendCustomNetworkEvent(NetworkEventTarget.All, "Apply");
    }

    public void Apply() {
        this.core.Apply();
    }
}

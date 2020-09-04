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

        if (this.core.isWorkingOnlyOnLocal) {
            this.Stop();
        } else {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "Stop");
        }
    }

    public void Stop() {
        this.core.StopAll();
    }
}

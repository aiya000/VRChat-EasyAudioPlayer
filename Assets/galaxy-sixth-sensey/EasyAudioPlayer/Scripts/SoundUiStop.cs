using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiStop : UdonSharpBehaviour {
    public SoundUiAudioSourceStore core;

    public override void Interact() {
        if (this.core == null) {
            Debug.Log("SoundUiStop: The core has not set.");
            return;
        }
        this.core.StopAll();
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiStop : UdonSharpBehaviour {
    public SoundUiAudioSourceStore soundUiAudioSourceStore;

    public override void Interact() {
        if (this.soundUiAudioSourceStore == null) {
            Debug.Log("SoundUiStop: The SoundUiAudioSourceStore has not set.");
            return;
        }

        var audioSources = this.soundUiAudioSourceStore.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiStop: exit for the empty audio sources.");
            return;
        }

        var paused = this.soundUiAudioSourceStore.GetPaused();
        if (paused != -1) {
            this.soundUiAudioSourceStore.Stop(paused);
            return;
        }

        for (var i = 0; i < audioSources.Length; i++) {
            if (audioSources[i].isPlaying) {
                this.soundUiAudioSourceStore.Stop(i);
                return;
            }
        }
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiPlayOrPause : UdonSharpBehaviour {
    public SoundUiAudioSourceStore soundUiAudioSourceStore;

    /// Stop an audio source that is playing now.
    /// Play an audio source that has been paused.
    /// Or play the first audio source if no one is playing now.
    public override void Interact() {
        if (this.soundUiAudioSourceStore == null) {
            Debug.Log("SoundUiPlayOrPause: The SoundUiAudioSourceStore has not set.");
            return;
        }

        var audioSources = this.soundUiAudioSourceStore.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiPlayOrPause: exit for the empty audio sources.");
            return;
        }

        if (this.soundUiAudioSourceStore.UnPauseLatest()) {
            return;
        }

        for (var i = 0; i < audioSources.Length; i++) {
            var audioSource = audioSources[i];
            if (audioSource.isPlaying) {
                this.soundUiAudioSourceStore.Pause(i);
                return;
            }
        }

        this.soundUiAudioSourceStore.Play(0);
    }
}

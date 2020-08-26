using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiPlayNext : UdonSharpBehaviour {
    public SoundUiAudioSourceStore soundUiAudioSourceStore;

    /// Do nothing if soundUiAudioSourceStore is null or soundUiAudioSourceStore size is 0.
    /// Play the first if no one is playing now or the last is playing now.
    /// Or play the next of the one that is playing now or the one has been paused.
    public override void Interact() {
        if (this.soundUiAudioSourceStore == null) {
            Debug.Log("SoundUiPlayNext: The SoundUiAudioSourceStore has not set.");
            return;
        }

        var audioSources = this.soundUiAudioSourceStore.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiPlayNext: exit for the empty audio sources.");
            return;
        }

        var paused = this.soundUiAudioSourceStore.GetPaused();
        if (paused != -1 && paused == audioSources.Length - 1) {  // if someone has been paused && paused is the last index
            this.soundUiAudioSourceStore.UnsetPaused();
            this.soundUiAudioSourceStore.Stop(paused);
            this.soundUiAudioSourceStore.Play(0);
            return;
        } else if (paused != -1) {  // if '' && paused is less than the last index
            this.soundUiAudioSourceStore.UnsetPaused();
            this.soundUiAudioSourceStore.Stop(paused);
            this.soundUiAudioSourceStore.Play(paused + 1);
            return;
        }

        int playingNow = -1;
        for (var i = 0; i < audioSources.Length; i++) {
            var audioSource = audioSources[i];

            if (audioSource.isPlaying) {
                playingNow = i;
                break;
            }
        }
        this.soundUiAudioSourceStore.StopAll();

        if (playingNow == -1 || playingNow == audioSources.Length - 1) {
            Debug.Log("SoundUiPlayNext: Play the first.");
            this.soundUiAudioSourceStore.Play(0);
            return;
        }

        Debug.Log($"SoundUiPlayNext: Play the ${playingNow + 1}th.");
        this.soundUiAudioSourceStore.Play(playingNow + 1);
    }
}

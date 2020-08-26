using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiPlayPrevious : UdonSharpBehaviour {
    public SoundUiAudioSourceStore soundUiAudioSourceStore;

    public override void Interact() {
        if (this.soundUiAudioSourceStore == null) {
            Debug.Log("SoundUiPlayPrevious: The SoundUiAudioSourceStore has not set.");
            return;
        }

        var audioSources = this.soundUiAudioSourceStore.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiPlayPrevious: exit for the empty audio sources.");
            return;
        }

        var paused = this.soundUiAudioSourceStore.GetPaused();
        if (paused != -1 && paused == 0) {
            this.soundUiAudioSourceStore.UnsetPaused();
            this.soundUiAudioSourceStore.Stop(paused);
            this.soundUiAudioSourceStore.Play(audioSources.Length - 1);
            return;
        } else if (paused != -1) {
            this.soundUiAudioSourceStore.UnsetPaused();
            this.soundUiAudioSourceStore.Stop(paused);
            this.soundUiAudioSourceStore.Play(paused - 1);
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

        if (playingNow == -1) {
            Debug.Log("SoundUiPlayPrevious: Play the first.");
            this.soundUiAudioSourceStore.Play(0);
            return;
        } else if (playingNow == 0) {
            Debug.Log("SoundUiPlayPrevious: Play the last.");
            this.soundUiAudioSourceStore.Play(audioSources.Length - 1);
            return;
        }

        Debug.Log($"SoundUiPlayPrevious: Play the ${playingNow - 1}th.");
        this.soundUiAudioSourceStore.Play(playingNow - 1);
    }
}

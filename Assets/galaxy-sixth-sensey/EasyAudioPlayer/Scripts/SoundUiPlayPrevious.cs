using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiPlayPrevious : UdonSharpBehaviour {
    public SoundUiAudioSourceStore core;

    public override void Interact() {
        if (this.core == null) {
            Debug.Log("SoundUiPlayPrevious: The core has not set.");
            return;
        }

        var audioSources = this.core.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiPlayPrevious: exit for the empty audio sources.");
            return;
        }

        var stoppedIndex = this.core.StopLatest();
        if (!this.core.IsNotOutOfBoundsOnAudioSources(stoppedIndex)) {
            Debug.Log($"SoundUiPlayPrevious: Illegal stopped index '{stoppedIndex}. Exit.");
            return;
        }

        var previousIndex = this.getPreviousIndex(stoppedIndex, audioSources.Length);
        if (previousIndex == -1) {
            Debug.Log($"SoundUiPlayPrevious: The previous index of '{stoppedIndex}' couldn't be gotten. Exit.");
        }

        this.core.Play(previousIndex);
    }

    private int getPreviousIndex(int baseIndex, int length) {
        if (baseIndex <= -2 || length <= baseIndex) {
            return -1;
        }

        return (baseIndex == 0) ? (length - 1) : (baseIndex - 1);
    }
}

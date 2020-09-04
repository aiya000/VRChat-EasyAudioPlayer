using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundUiPlayNext : UdonSharpBehaviour {
    public SoundUiAudioSourceStore core;

    /// Do nothing if core is null or core size is 0.
    /// Play the first if no one is playing now or the last is playing now.
    /// Or play the next of the one that is playing now or the one has been paused.
    public override void Interact() {
        if (this.core == null) {
            Debug.Log("SoundUiPlayNext: The core has not set.");
            return;
        }

        var audioSources = this.core.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("SoundUiPlayNext: exit for the empty audio sources.");
            return;
        }

        var stoppedIndex = this.core.StopLatest();
        if (!this.core.IsNotOutOfBoundsOnAudioSources(stoppedIndex)) {
            Debug.Log("SoundUiPlayNext: Illegal State. Exit.");
            return;
        }

        var nextIndex = (stoppedIndex + 1) % audioSources.Length;
        this.core.Play(nextIndex);
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

public class EasyAudioPlayerPlayPrevious : UdonSharpBehaviour {
    public EasyAudioPlayerAudioSourceStore core;

    public override void Interact() {
        if (this.core == null) {
            Debug.Log("EasyAudioPlayerPlayPrevious: The core has not set.");
            return;
        }

        if (this.core.isWorkingOnlyOnLocal) {
            this.PlayPrevious();
        } else {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "PlayPrevious");
        }
    }

    public void PlayPrevious() {
        var audioSources = this.core.GetAudioSources();
        if (audioSources.Length == 0) {
            Debug.Log("EasyAudioPlayerPlayPrevious: exit for the empty audio sources.");
            return;
        }

        var stoppedIndex = this.core.StopLatest();
        if (!this.core.IsNotOutOfBoundsOnAudioSources(stoppedIndex)) {
            Debug.Log($"EasyAudioPlayerPlayPrevious: Illegal stopped index '{stoppedIndex}. Exit.");
            return;
        }

        var previousIndex = this.getPreviousIndex(stoppedIndex, audioSources.Length);
        if (previousIndex == -1) {
            Debug.Log($"EasyAudioPlayerPlayPrevious: The previous index of '{stoppedIndex}' couldn't be gotten. Exit.");
            return;
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

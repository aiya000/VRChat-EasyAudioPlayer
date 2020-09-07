using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

/// <summary>
/// Sets a value with network users.
///
/// NOTE:
/// Don't use DoSetValueTo*() directly.
/// Please use Set() instead.
/// </summary>
public class BoolSync : UdonSharpBehaviour {
    private bool val = false;
    private bool debugMode = false;

    public void SetDebugMode(bool debugMode) {
        this.debugMode = debugMode;
    }

    /// <summary>
    /// SendCustomNetworkEvent
    /// </summary>
    public void Set(bool val) {
        this.SendCustomNetworkEvent(NetworkEventTarget.All, "DoSetValueTo" + val);
    }

    public void DoSetValueToTrue() {
        this.log($"Set from {this.val} to True");
        this.val = true;
    }

    public void DoSetValueToFalse() {
        this.log($"Set from {this.val} to False");
        this.val = false;
    }

    public bool Get() => this.val;

    private void log(string message) {
        if (this.debugMode) {
            Debug.Log($"IntSync: {message}");
        }
    }
}

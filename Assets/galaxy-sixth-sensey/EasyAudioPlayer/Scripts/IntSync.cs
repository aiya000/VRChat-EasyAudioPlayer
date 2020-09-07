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
public class IntSync : UdonSharpBehaviour {
    private int val = 0;
    private bool debugMode = false;

    public void SetDebugMode(bool debugMode) {
        this.debugMode = debugMode;
    }

    /// <summary>
    /// The argument must be -1~64
    /// </summary>
    public void Set(int val) {
        if (val > -1 && 64 < val) {
            this.log($"Illegal argument (the argument must be -1~64): {val}");
            return;
        }

        if (val == -1) {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "DoSetValueToNothing");
        } else {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "DoSetValueTo" + val);
        }
    }

    public int Get() => this.val;

    private void log(string message) {
        if (this.debugMode) {
            Debug.Log($"IntSync: {message}");
        }
    }

    /* - - vvvvvvvvvvvvvvv - - */
    /* - - v UNDERGROUND v - - */
    /* - - vvvvvvvvvvvvvvv - - */

    public void DoSetValueToNothing() {
        this.log($"Set from {this.val} to -1");
        this.val = -1;
    }

    public void DoSetValueTo0() {
        this.log($"Set from {this.val} to 0");
        this.val = 0;
    }

    public void DoSetValueTo1() {
        this.log($"Set from {this.val} to 1");
        this.val = 1;
    }

    public void DoSetValueTo2() {
        this.log($"Set from {this.val} to 2");
        this.val = 2;
    }

    public void DoSetValueTo3() {
        this.log($"Set from {this.val} to 3");
        this.val = 3;
    }

    public void DoSetValueTo4() {
        this.log($"Set from {this.val} to 4");
        this.val = 4;
    }

    public void DoSetValueTo5() {
        this.log($"Set from {this.val} to 5");
        this.val = 5;
    }

    public void DoSetValueTo6() {
        this.log($"Set from {this.val} to 6");
        this.val = 6;
    }

    public void DoSetValueTo7() {
        this.log($"Set from {this.val} to 7");
        this.val = 7;
    }

    public void DoSetValueTo8() {
        this.log($"Set from {this.val} to 8");
        this.val = 8;
    }

    public void DoSetValueTo9() {
        this.log($"Set from {this.val} to 9");
        this.val = 9;
    }

    public void DoSetValueTo10() {
        this.log($"Set from {this.val} to 10");
        this.val = 10;
    }

    public void DoSetValueTo11() {
        this.log($"Set from {this.val} to 11");
        this.val = 11;
    }

    public void DoSetValueTo12() {
        this.log($"Set from {this.val} to 12");
        this.val = 12;
    }

    public void DoSetValueTo13() {
        this.log($"Set from {this.val} to 13");
        this.val = 13;
    }

    public void DoSetValueTo14() {
        this.log($"Set from {this.val} to 14");
        this.val = 14;
    }

    public void DoSetValueTo15() {
        this.log($"Set from {this.val} to 15");
        this.val = 15;
    }

    public void DoSetValueTo16() {
        this.log($"Set from {this.val} to 16");
        this.val = 16;
    }

    public void DoSetValueTo17() {
        this.log($"Set from {this.val} to 17");
        this.val = 17;
    }

    public void DoSetValueTo18() {
        this.log($"Set from {this.val} to 18");
        this.val = 18;
    }

    public void DoSetValueTo19() {
        this.log($"Set from {this.val} to 19");
        this.val = 19;
    }

    public void DoSetValueTo20() {
        this.log($"Set from {this.val} to 20");
        this.val = 20;
    }

    public void DoSetValueTo21() {
        this.log($"Set from {this.val} to 21");
        this.val = 21;
    }

    public void DoSetValueTo22() {
        this.log($"Set from {this.val} to 22");
        this.val = 22;
    }

    public void DoSetValueTo23() {
        this.log($"Set from {this.val} to 23");
        this.val = 23;
    }

    public void DoSetValueTo24() {
        this.log($"Set from {this.val} to 24");
        this.val = 24;
    }

    public void DoSetValueTo25() {
        this.log($"Set from {this.val} to 25");
        this.val = 25;
    }

    public void DoSetValueTo26() {
        this.log($"Set from {this.val} to 26");
        this.val = 26;
    }

    public void DoSetValueTo27() {
        this.log($"Set from {this.val} to 27");
        this.val = 27;
    }

    public void DoSetValueTo28() {
        this.log($"Set from {this.val} to 28");
        this.val = 28;
    }

    public void DoSetValueTo29() {
        this.log($"Set from {this.val} to 29");
        this.val = 29;
    }

    public void DoSetValueTo30() {
        this.log($"Set from {this.val} to 30");
        this.val = 30;
    }

    public void DoSetValueTo31() {
        this.log($"Set from {this.val} to 31");
        this.val = 31;
    }

    public void DoSetValueTo32() {
        this.log($"Set from {this.val} to 32");
        this.val = 32;
    }

    public void DoSetValueTo33() {
        this.log($"Set from {this.val} to 33");
        this.val = 33;
    }

    public void DoSetValueTo34() {
        this.log($"Set from {this.val} to 34");
        this.val = 34;
    }

    public void DoSetValueTo35() {
        this.log($"Set from {this.val} to 35");
        this.val = 35;
    }

    public void DoSetValueTo36() {
        this.log($"Set from {this.val} to 36");
        this.val = 36;
    }

    public void DoSetValueTo37() {
        this.log($"Set from {this.val} to 37");
        this.val = 37;
    }

    public void DoSetValueTo38() {
        this.log($"Set from {this.val} to 38");
        this.val = 38;
    }

    public void DoSetValueTo39() {
        this.log($"Set from {this.val} to 39");
        this.val = 39;
    }

    public void DoSetValueTo40() {
        this.log($"Set from {this.val} to 40");
        this.val = 40;
    }

    public void DoSetValueTo41() {
        this.log($"Set from {this.val} to 41");
        this.val = 41;
    }

    public void DoSetValueTo42() {
        this.log($"Set from {this.val} to 42");
        this.val = 42;
    }

    public void DoSetValueTo43() {
        this.log($"Set from {this.val} to 43");
        this.val = 43;
    }

    public void DoSetValueTo44() {
        this.log($"Set from {this.val} to 44");
        this.val = 44;
    }

    public void DoSetValueTo45() {
        this.log($"Set from {this.val} to 45");
        this.val = 45;
    }

    public void DoSetValueTo46() {
        this.log($"Set from {this.val} to 46");
        this.val = 46;
    }

    public void DoSetValueTo47() {
        this.log($"Set from {this.val} to 47");
        this.val = 47;
    }

    public void DoSetValueTo48() {
        this.log($"Set from {this.val} to 48");
        this.val = 48;
    }

    public void DoSetValueTo49() {
        this.log($"Set from {this.val} to 49");
        this.val = 49;
    }

    public void DoSetValueTo50() {
        this.log($"Set from {this.val} to 50");
        this.val = 50;
    }

    public void DoSetValueTo51() {
        this.log($"Set from {this.val} to 51");
        this.val = 51;
    }

    public void DoSetValueTo52() {
        this.log($"Set from {this.val} to 52");
        this.val = 52;
    }

    public void DoSetValueTo53() {
        this.log($"Set from {this.val} to 53");
        this.val = 53;
    }

    public void DoSetValueTo54() {
        this.log($"Set from {this.val} to 54");
        this.val = 54;
    }

    public void DoSetValueTo55() {
        this.log($"Set from {this.val} to 55");
        this.val = 55;
    }

    public void DoSetValueTo56() {
        this.log($"Set from {this.val} to 56");
        this.val = 56;
    }

    public void DoSetValueTo57() {
        this.log($"Set from {this.val} to 57");
        this.val = 57;
    }

    public void DoSetValueTo58() {
        this.log($"Set from {this.val} to 58");
        this.val = 58;
    }

    public void DoSetValueTo59() {
        this.log($"Set from {this.val} to 59");
        this.val = 59;
    }

    public void DoSetValueTo60() {
        this.log($"Set from {this.val} to 60");
        this.val = 60;
    }

    public void DoSetValueTo61() {
        this.log($"Set from {this.val} to 61");
        this.val = 61;
    }

    public void DoSetValueTo62() {
        this.log($"Set from {this.val} to 62");
        this.val = 62;
    }

    public void DoSetValueTo63() {
        this.log($"Set from {this.val} to 63");
        this.val = 63;
    }

    public void DoSetValueTo64() {
        this.log($"Set from {this.val} to 64");
        this.val = 64;
    }
}

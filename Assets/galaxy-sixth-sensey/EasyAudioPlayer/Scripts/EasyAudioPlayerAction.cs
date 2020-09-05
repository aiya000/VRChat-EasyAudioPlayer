namespace EasyAudioPlayer {
    /// <summary>
    /// Which is now playing index.
    /// Or no one is playing now.
    /// </summary>
    public interface PlayingState {
        int getIndex();
    }

    public class PlayingStateStopping : PlayingState {
        public int getIndex() {
            Debug.Log($"PlayingState: PlayingStateStopping: This shouldn't be called.");
            return -1;
        }
    }

    public class PlayingStatePlaying : PlayingState {
        private int index;

        public PlayingStatePlaying(int index) {
            this.index = index;
        }

        public int getIndex() => this.index;
    }

    public class PlayingStatePausing : PlayingState {
        private int index;

        public PlayingStatePausing(int index) {
            this.index = index;
        }

        public int getIndex() => this.index;
    }

    public class PlayingStateUnpausing : PlayingState {
        private int index;

        public PlayingStateUnpausing(int index) {
            this.index = index;
        }

        public int getIndex() => this.index;
    }
}

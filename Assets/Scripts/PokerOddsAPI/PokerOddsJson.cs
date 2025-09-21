namespace PokerOddsJson
{
    [System.Serializable]
    public class HandRequest {
        public string[] hand;
        public string[] board;
    }

    [System.Serializable]
    public class HandResponse {
        public string[] hand;
        public int simulations;
        public HandChance[] distribution;
        public float strengthScore;
        public float normalizedStrength;
        public float visualStrength;
    }

    [System.Serializable]
    public class HandChance {
        public string name;
        public float probability;
    }
}

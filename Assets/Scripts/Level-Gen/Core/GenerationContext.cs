using System;

namespace ScaryGame.LevelGen
{
    public class GenerationContext
    {
        public LevelGrid grid;
        public LevelConfig config;
        public System.Random rng;
        public uint seed;
    }
}

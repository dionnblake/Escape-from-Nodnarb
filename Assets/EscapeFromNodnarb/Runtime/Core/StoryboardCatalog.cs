using System;
using System.Collections.Generic;

namespace EscapeFromNodnarb
{
    [Serializable]
    public sealed class StoryboardBeat
    {
        public int Level;
        public string FrameOne;
        public string FrameTwo;
        public string VisualDirection;
        public string SoundDirection;

        public StoryboardBeat(int level, string frameOne, string frameTwo, string visualDirection, string soundDirection)
        {
            Level = level;
            FrameOne = frameOne;
            FrameTwo = frameTwo;
            VisualDirection = visualDirection;
            SoundDirection = soundDirection;
        }
    }

    public static class StoryboardCatalog
    {
        private static readonly StoryboardBeat EndlessBeat = new StoryboardBeat(
            0,
            "THE SIGNAL NEVER STOPS",
            "NO EXTRACTION VECTOR",
            "The dead signal loops across the night shelf while the squad chooses how long to keep moving.",
            "Sparse radio hiss, cold wind, and a low repeating pulse with no rescue swell.");

        private static readonly StoryboardBeat[] Beats =
        {
            new StoryboardBeat(1, "THE HULL SPLITS", "THE BEACON ANSWERS", "Captain turns from the burning ship interior toward a breach filled with alien light.", "Metal groan, low alarm, first distant creature cry."),
            new StoryboardBeat(2, "ASH BEHIND US", "THE BASIN MOVES", "The crew exits the wreck while shapes move through dust between the canyon walls.", "Wind rises, debris skitters, radio cuts in and out."),
            new StoryboardBeat(3, "THE GROUND BREATHES", "EVERY SHOT CALLS THEM", "Spore growth opens around the squad and a hidden swarm wakes under the soil.", "Wet pulse, spores crackle, swarm chitter under the music."),
            new StoryboardBeat(4, "WHITEGLASS WEATHER", "THE ARCH IS ALIVE", "Snow blows sideways across a natural ice pass as something follows above the ridge.", "Thin wind, ice strain, distant howl."),
            new StoryboardBeat(5, "A DEAD RELAY", "SEND THE PING", "The crew finds a broken alien relay half-buried in a field of bones and rock.", "Electrical hum, short radio burst, armored impact."),
            new StoryboardBeat(6, "THE PLANET ANSWERS", "THE FAULT OPENS", "Purple crystal shelves split the ground while a giant carrier wakes below them.", "Crystal resonance, sub-bass rumble, carrier roar."),
            new StoryboardBeat(7, "THOUSANDS BELOW", "RUN THE BROOD CHANNEL", "The squad crosses a living trench as the walls pulse with nests and moving eyes.", "Layered heartbeats, wet clicks, rapid warning pulses."),
            new StoryboardBeat(8, "NIGHT FALLS FAST", "THE SIGNAL FADES", "Blue-black night drops over a sloped shelf; the rescue signal becomes a thin line.", "Cold wind, sparse radio, restrained low drone."),
            new StoryboardBeat(9, "LIGHT THE SKY", "THE PLAIN OPENS", "The beacon appears across open ground while every creature in the valley turns toward it.", "Signal flare, distant mass movement, music opens wide."),
            new StoryboardBeat(10, "NINETY SECONDS", "LEAVE TOGETHER", "The extraction ring burns against the alien plain as the last carrier closes in.", "Rescue engines, full combat layer, final extraction hit."),
        };

        public static IReadOnlyList<StoryboardBeat> All
        {
            get { return Beats; }
        }

        public static StoryboardBeat Endless
        {
            get { return EndlessBeat; }
        }

        public static StoryboardBeat Get(int level)
        {
            int index = Math.Max(1, Math.Min(Beats.Length, level)) - 1;
            return Beats[index];
        }
    }
}

using System;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes;

public class AIPacket
{
    public struct Packet
    {
        public struct FleeingParams
        {
            public int NPCHPBelow; //percentage of NPC hit points below which NPC will flee
            public int PeopleAround; //number of people besides PC beyond which NPC will flee
            public int LevelAbove; //number of levels above NPC beyond which NPC will flee
            public int PCHPBelow; //percentage of PC hit points below which NPC will never flee
            public int FleeRange; //how far to flee, in tiles
        }

        public struct FollowerParams
        {
            public int ReactionLevel; //the reaction level at which the NPC will not follow the PC
            public int PCAlignAboveNPC; //how far PC alignment is above NPC align before NPC wont follow
            public int PCAlignBelowNPC; //how far PC align is below NPC align before NPC wont follow
            public int NPCLevelsAbovePC; //how many levels the NPC can be above the PC and still join
            public int NPCAbuseReaction; //how much a non-accidental hit will lower a follower's reaction
        }

        public struct KillOnSightParams
        {
            public int NPCReactionLevelBelow; //NPC will attack if his reaction is below this
            public int NPCAlignDiffToPC; //how different alignments can be before non-follower NPC attacks
            public int TargetAlignGoodNoAtt; //alignment of target at (or above) which the good-aligned follower NPC will not attack
        }

        public struct SpellParams
        {
            public int DefensiveSpellChance; //chance of throwing defensive spell (as opposed to offensive)
            public int HealingSpellInCmbChance; //chance of throwing a healing spell in combat
        }

        public struct CombatParams
        {
            public int CombatMinDistance; //minimum distance in combat
        }

        public struct DoorAndWindowsParams
        {
            public int CanOpenPortals; //the NPC can open portals if this is nonzero and cannot if it is zero
        }
    }

    public List<Tuple<int, Packet>> Entries;
}
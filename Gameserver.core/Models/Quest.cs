    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Gameserver.core.Models
    {
        public class Quest
        {
            [Key]
            public int QuestID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int MinLevel     { get; set; }
            public int GoldReward {  get; set; }
            public int ExpReward { get; set; }
            public int StartNPCID { get; set; }
            public int EndNPCID { get; set; }

        }

        public class Questprogress
        {
            [Key]
            public int QuestProgressID { get; set; }
            public int CharacterID { get; set; }
            public int QuestID { get; set; }
            public string Status { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime CompletionTime { get; set; }

        }
        public class Questprogressobjective
        {
            [Key]
            public int ProgressObjectiveID { get; set; }
            public int QuestProgressID { get; set; }
            public int Type { get; set; }
            public int TargetID { get; set; }
            public int CurrentAmount { get; set; }
            public int RequiredAmount { get; set; }
        }

    public class QuestTemplateObjective
    {
        [Key]
        public int TemplateObjectiveID { get; set; }
        public int QuestID { get; set; }
        public int Type { get; set; }
        public int TargetID { get; set; }
        public int RequiredAmount { get; set; }
    }
    // Enum cho quest status
    public enum QuestStatus
        {
            NotStarted,
            InProgress,
            Completed,
        }

        // Enum cho objective type
        public enum ObjectiveType
        {
            KillMonster = 1,
            CollectItem = 2,
            TalkToNPC = 3,
            ReachLocation = 4
        }
    }

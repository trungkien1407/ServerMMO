using System;
using Gameserver.core.Models;
using Microsoft.EntityFrameworkCore;
namespace Gameserver.core.Repo.Context
{
  
        public class AppDbContext : DbContext
        {
            public DbSet<User> Accounts { get; set; }
            public DbSet<Characters> Characters { get; set; }
             public DbSet<Monsters> Monsters { get; set; }
             public DbSet<Monsterspawn> Monsterspawns { get; set; }
            public DbSet<Characterskill> CharacterSkills { get; set; }
            public DbSet<Quest> Quest { get; set; }
            public DbSet<Questprogress> Questprogress { get; set; }
            public DbSet<Questprogressobjective> Questprogressobjective { get; set; }
            public DbSet<QuestTemplateObjective> QuestTemplateObjective { get; set; }
              public DbSet<Item> Item { get; set; }
               public DbSet<Inventory> Inventorie { get; set; }
        public DbSet<InventoryItem> InventoryItem { get; set; }
        public DbSet<CharacterEquipment> CharacterEquipment { get; set; }
        public DbSet<EquipmentData> EquipmentData { get; set; }


        public DbSet<NPC> NPC  { get; set; }
        // Constructor cần thiết để nhận DbContextOptions
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           base.OnConfiguring(optionsBuilder);

        }
    }
}


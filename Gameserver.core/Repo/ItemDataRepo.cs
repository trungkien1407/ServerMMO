using Gameserver.core.Manager;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public class ItemDataRepo : IItemDataRepo
    {
        private readonly AppDbContext _context;

        public ItemDataRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CachedItemData>> GetItemDataAsync()
        {
            var data = await (from item in _context.Item
                              join equip in _context.EquipmentData on item.ItemID equals equip.ItemID into equipJoin
                              from equip in equipJoin.DefaultIfEmpty()
                              select new CachedItemData
                              {
                                  ItemID = item.ItemID,
                                  Name = item.Name,
                                  Type = item.Type,
                                  RequiredLevel = item.RequiredLevel,
                                  Description = item.Description,
                                  BuyPrice = item.BuyPrice,
                                  MaxStackSize = item.MaxStackSize,
                                  SlotType = equip != null ? equip.SlotType : null,
                                  ClassRestriction = equip != null ? equip.ClassRestriction : 0,
                                  GenderRestriction = equip.GenderRestriction,
                                  AttackBonus = equip.AttackBonus,
                                  HPBonus = equip.HPBonus
                              }).ToListAsync();

            return data;
        }
    }

}

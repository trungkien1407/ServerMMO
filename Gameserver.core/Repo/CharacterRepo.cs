using Gameserver.core.Dto;
using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public class CharacterRepo : ICharacterRepo
    {
        private readonly AppDbContext _context;
        public CharacterRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Characters> Create(Characters character)
        {
            _context.Add(character);
            var resuit = await _context.SaveChangesAsync();
            if (resuit > 0)
            {
                return character;
            }
            return null;
        }

        //public Task<Characters> GetById(int id)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<Characters?> GetCharactersByUserID(int userid)
        {
            return await _context.Characters.FirstOrDefaultAsync(c => c.UserID == userid);
        }
        public async Task<bool> Update(Characters character)
        {
            _context.Characters.Update(character);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
        public async Task<bool> CheckName(string charname)
        {
            var result = await _context.Characters.AnyAsync(c => c.Name == charname);
            return result;
        }

        public async Task LoadCharacterDataIntoSession(Session session,int Class)
        {
            if (session.Character == null)
            {
                throw new InvalidOperationException("Character must be selected before loading data.");
            }

            int characterId = session.Character.CharacterID;

            // 1. Load Inventory và InventoryItems
            var inventoryData = await _context.Inventorie
                                          .Include(inv => inv.InventoryItems)
                                          .FirstOrDefaultAsync(inv => inv.CharacterID == characterId);

            if (inventoryData != null)
            {
                // Logic cũ không đổi: tải dữ liệu đã có
                session.InventoryCache.InventoryId = inventoryData.InventoryID;
                session.InventoryCache.MaxSlots = inventoryData.MaxSlots;
                foreach (var item in inventoryData.InventoryItems)
                {
                    session.InventoryCache.Items[item.InventoryItemID] = item;
                    Console.WriteLine($"Load item {item.InventoryItemID} with {item.ItemID} quantity {item.Quantity}");
                }
            }
            else
            {
                
              
                // Nhân vật chưa có túi đồ -> Tạo mới và thêm vật phẩm khởi đầu
                Console.WriteLine($"No inventory found for CharacterID: {characterId}. Creating new inventory with starting items.");

                // Bước 1 & 2: Tạo túi đồ mới và các vật phẩm khởi đầu
                var newInventory = new Inventory { CharacterID = characterId, MaxSlots = 30 };
                var startingItem1 = new InventoryItem
                {
                    ItemID = 103,
                    Quantity = 50,
                    SlotNumber = 0, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };

                var startingItem2 = new InventoryItem
                {
                    ItemID = 104,
                    Quantity = 50,
                    SlotNumber = 1, // Gán vào ô thứ hai
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };
                var startingItem3 = new InventoryItem
                {
                    ItemID = Class == 1 ? 105 : 106,
                    Quantity = 1,
                    SlotNumber = 2, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };
                var startingItem4 = new InventoryItem
                {
                    ItemID = 110,
                    Quantity = 1,
                    SlotNumber = 3, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };
                var startingItem5 = new InventoryItem
                {
                    ItemID = 112,
                    Quantity = 1,
                    SlotNumber = 4, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                }; var startingItem6 = new InventoryItem
                {
                    ItemID = 113,
                    Quantity = 1,
                    SlotNumber = 5, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                }; var startingItem7 = new InventoryItem
                {
                    ItemID = 115,
                    Quantity = 1,
                    SlotNumber = 6, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                }; var startingItem8 = new InventoryItem
                {
                    ItemID = 118,
                    Quantity = 1,
                    SlotNumber = 7, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };
                var startingItem9 = new InventoryItem
                {
                    ItemID = 119,
                    Quantity = 1,
                    SlotNumber = 8, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };
                var startingItem10 = new InventoryItem
                {
                    ItemID = 120,
                    Quantity = 1,
                    SlotNumber = 9, // Gán vào ô đầu tiên
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };


                // Bước 3: Liên kết các vật phẩm vào túi đồ mới
                newInventory.InventoryItems.Add(startingItem1);
                newInventory.InventoryItems.Add(startingItem2);
                newInventory.InventoryItems.Add(startingItem3);
                newInventory.InventoryItems.Add(startingItem4);
                newInventory.InventoryItems.Add(startingItem5);
                newInventory.InventoryItems.Add(startingItem6);
                newInventory.InventoryItems.Add(startingItem7);
                newInventory.InventoryItems.Add(startingItem8);
                newInventory.InventoryItems.Add(startingItem9);
                newInventory.InventoryItems.Add(startingItem10);

                // Bước 4: Thêm túi đồ (và các vật phẩm con của nó) vào context và lưu vào DB
                _context.Inventorie.Add(newInventory);
                await _context.SaveChangesAsync();
                // Sau lệnh này, newInventory, startingItem1, và startingItem2 đều đã có ID do DB cấp

                // Bước 5: Cập nhật đầy đủ thông tin vào session cache
                session.InventoryCache.InventoryId = newInventory.InventoryID;
                session.InventoryCache.MaxSlots = newInventory.MaxSlots;

                // Thêm các vật phẩm vừa tạo vào cache
                foreach (var item in newInventory.InventoryItems)
                {
                    session.InventoryCache.Items[item.InventoryItemID] = item;
                    Console.WriteLine($"Added starting item ID {item.InventoryItemID} (ItemID: {item.ItemID}) to cache.");
                }
                // === KẾT THÚC PHẦN THAY ĐỔI ===
            }

            // 2. Load or Create Equipment (giữ nguyên như cũ)
            var equipmentData = await _context.CharacterEquipment
                                           .FirstOrDefaultAsync(eq => eq.CharacterID == characterId);
            if (equipmentData == null)
            {
                Console.WriteLine($"No equipment record found for CharacterID: {characterId}. Creating a new one.");
                equipmentData = new CharacterEquipment
                {
                    CharacterID = characterId
                };
                _context.CharacterEquipment.Add(equipmentData);
                await _context.SaveChangesAsync();
            }

            session.EquipmentCache.WeaponSlot_InventoryItemID = equipmentData.WeaponSlot;
            session.EquipmentCache.AoSlot_InventoryItemID = equipmentData.SlotAo;
            session.EquipmentCache.QuanSlot_InventoryItemID = equipmentData.SlotQuan;
            session.EquipmentCache.GiaySlot_InventoryItemID = equipmentData.SlotGiay;
            session.EquipmentCache.GangTaySlot_InventoryItemID = equipmentData.SlotGangTay;
            session.EquipmentCache.BuaSlot_InventoryItemID = equipmentData.SlotBua;
        }
    }
}


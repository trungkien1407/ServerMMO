using System;
using System.ComponentModel.DataAnnotations;

namespace Gameserver.core.Models
{
    public class Characters
    {
        [Key]
        public int CharacterID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }  // giới tính 0 là nam, 1 là nữ;
        public int HeadID { get; set; } = 1; // id đầu
        public int BodyID { get; set; } = 1; // id thân
        public int WeaponID { get; set; } = 0; // id vũ khí mạc định là chưa có
        public int PantID { get; set; } = 1; // id giày 
        public int Class { get; set; } = 0; // mặc định là chưa vào class, sau khi chơi sẽ chọn
        public int Level { get; set; } = 1;  // Mặc định level 1 khi tạo nhân vật
        public int Exp { get; set; } = 0;  // Mặc định Exp là 0 khi tạo nhân vật
        public int Health { get; set; } = 100;  // Mặc định 100 máu
        public int CurrentHealth { get; set; } = 100;
        public int CurrentMana { get; set; } = 100;
        public int Mana { get; set; } = 100;  // Mặc định 100 mana
        public int Strength { get; set; } = 10;  // Mặc định chỉ số sức mạnh
        public int Intelligence { get; set; } = 10;  // Mặc định chỉ số trí tuệ
        public int Dexterity { get; set; } = 10;  // Mặc định chỉ số nhanh nhẹn
        public float X { get; set; } = -9;  // Mặc định vị trí X
        public float Y { get; set; } = -5;  // Mặc định vị trí Y
        public int MapID { get; set; } = 1;  // Mặc định bản đồ là MapID 1
        public int Gold { get; set; } = 1000;  // Mặc định vàng của nhân vật là 1000
        public DateTime CreationDate { get; set; } = DateTime.Now;  // Ngày tạo nhân vật
        public DateTime LastPlayTime { get; set; } = DateTime.Now;  // Lần chơi cuối cùng
        public int SkillPoints { get; set; } = 0;
        public ICollection<Characterskill> CharacterSkills { get; set; }

        // Navigation Property
        //public User user { get; set; }

        // Constructor để tạo nhân vật mới
        public Characters(string name, int userID,int Gender,int Class)
        {
            Name = name;
            UserID = userID;
            this.Gender = Gender;
            this.Class = Class;
            this.WeaponID = Class;
        }
        public Characters() { }
    }
}

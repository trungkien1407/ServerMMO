using Gameserver.core.Models;

public class MonsterRuntime
{
    // Properties từ Monsters entity
    public int SpawnID { get; set; }
    public int MonsterID { get; set; }
    public string MonsterName { get; set; }
    public string MonsterImg { get; set; }
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int Strength { get; set; }
    public int Level { get; set; }
    public string DropItem { get; set; }

    // Position và movement
    public float X { get; set; }
    public float Y { get; set; }
    private float _originX;
    private float _originY;
    private float _moveRange = 1f; // Di chuyển trong phạm vi 3 ô
    private bool _movingRight = true;

    // Combat
    public int AttackRange { get; set; } = 4; // Tầm đánh
    public int AttackCooldown { get; set; } = 3; // Cooldown 3 giây
    private DateTime _lastAttackTime = DateTime.MinValue;

    // Respawn
    public int RespawnTime { get; set; }
    private DateTime? _timeOfDeath;

    public bool IsAlive => CurrentHP > 0;
    public bool ReadyToRespawn => !IsAlive && _timeOfDeath.HasValue &&
                                  (DateTime.UtcNow - _timeOfDeath.Value).TotalSeconds >= RespawnTime;

    public MonsterRuntime() { }

    public MonsterRuntime(Monsters monster, Monsterspawn spawn, int index = 0)
    {
        SpawnID = spawn.SpawnID;
        MonsterID = monster.MonsterId;
        MonsterName = monster.MonsterName?.Trim(); // Trim để loại bỏ khoảng trắng
        MonsterImg = monster.MonsterImg;
        MaxHP = monster.HP;
        CurrentHP = MaxHP;
        Strength = monster.Strength;
        Level = monster.Level;
        DropItem = monster.DropItems;

        // Position với offset nhỏ nếu có nhiều monster cùng spawn point
        _originX = spawn.X + (index % 2); // Offset đơn giản
        _originY = spawn.Y + (index / 2);
        X = _originX;
        Y = _originY;

        RespawnTime = spawn.RespawnTime;

        // Stats dựa trên level
        AttackRange = Math.Max(1, Level / 5 + 1);
        AttackCooldown = Math.Max(1, 4 - Level / 10);

        // DEBUG: In thông tin monster được tạo
        Console.WriteLine($"[Constructor] Created monster: '{MonsterName}' at ({X},{Y}) Origin: ({_originX},{_originY}) IsScarecrow: {IsScarecrow}");
    }

    // Di chuyển qua lại đơn giản - CHỈ THEO TRỤC X
    public bool CanMove { get; set; } = true;

    // Kiểm tra có phải bù nhìn không
    public bool IsScarecrow => MonsterID == 1;

    public void Move()
    {
        // DEBUG: In ra để kiểm tra
    //    Console.WriteLine($"[Move Debug] Monster: '{MonsterName}' IsScarecrow: {IsScarecrow} CanMove: {CanMove} IsAlive: {IsAlive}");

        // BÙ NHÌN KHÔNG DI CHUYỂN
        if (IsScarecrow || !CanMove || !IsAlive)
        {
            Console.WriteLine($"[Move] {MonsterName} skipped movement - IsScarecrow: {IsScarecrow}");
            return;
        }

    //    Console.WriteLine($"[Move] Before: ({X},{Y})");

        if (_movingRight)
        {
            X += 1;
            if (X >= _originX + _moveRange)
                _movingRight = false;
        }
        else
        {
            X -= 1;
            if (X <= _originX - _moveRange)
                _movingRight = true;
        }

        // Y LUÔN GIỮ NGUYÊN THEO ORIGIN
        Y = _originY;

    //    Console.WriteLine($"[Move] After: ({X},{Y})");
    }

    // Kiểm tra có thể tấn công không
    public bool CanAttack()
    {
        return IsAlive && (DateTime.UtcNow - _lastAttackTime).TotalSeconds >= AttackCooldown;
    }

    // Tấn công
    public int Attack()
    {
        _lastAttackTime = DateTime.UtcNow;
        if (MonsterID ==1) return 0;

        var random = new Random();
        int damage = Strength + random.Next(0, Level + 1);
        return damage;
    }

    // Nhận damage
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        CurrentHP -= damage;
        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Die();
        }

       
    }

    private void Die()
    {
        _timeOfDeath = DateTime.UtcNow;
      

        // TODO: Drop items, give EXP to players, etc.
    }

    public void Respawn()
    {
        CurrentHP = MaxHP;
        X = _originX;
        Y = _originY;
        _movingRight = true;
        _timeOfDeath = null;
        _lastAttackTime = DateTime.MinValue;

      
    }
}
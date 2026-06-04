using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using JungleAdventure.Source.Core;

namespace JungleAdventure
{
    // =====================================================================
    //  ENUMS
    // =====================================================================
    public enum GameState  { Welcome, LevelSelect, Playing, Shop, FullMap, StageClear, Win, Lose, Paused, Help, Credits }
    public enum EnemyType  { Zombie, Goblin, Skeleton, Shielded, Boss }
    public enum WeaponType { Fists, Sword, Axe, MagicStaff, Pistol }
    public enum Weather    { Clear, Rain, Storm }

    // =====================================================================
    //  SOUND MANAGER
    // =====================================================================
    public class SoundManager
    {
        private static readonly Random _sharedRandom = new Random();

        public SoundEffect SwordSwing;
        public SoundEffect PlayerHit;
        public SoundEffect EnemyHit;
        public SoundEffect EnemyDeath;
        public SoundEffect ZombieGroan;
        public SoundEffect CoinPickup;
        public SoundEffect LevelUp;
        public SoundEffect WinJingle;
        public SoundEffect LoseStinger;
        public SoundEffect ShopBuy;
        public SoundEffect BossRoar;
        public SoundEffect RainLoop;

        public void Play(SoundEffect sfx, float volume = 1f, float pitch = 0f)
        {
            if (sfx == null) return;
            try { sfx.Play(volume, pitch, 0f); } catch { }
        }
        public void PlayRandom(SoundEffect sfx, float volMin = 0.7f, float volMax = 1f, float pitchRange = 0.2f)
        {
            if (sfx == null) return;
            float v = volMin + (float)_sharedRandom.NextDouble() * (volMax - volMin);
            float p = (float)(_sharedRandom.NextDouble() * 2 - 1) * pitchRange;
            try { sfx.Play(v, p, 0f); } catch { }
        }
    }

    // =====================================================================
    //  DAMAGE NUMBER
    // =====================================================================
    public class DamageNumber
    {
        public Vector2 Position;
        public float   Value;
        public float   Life   = 1.2f;
        public float   MaxLife = 1.2f;
        public bool    IsCrit;
        public bool    IsHeal;
    }

    // =====================================================================
    //  FLOATING COIN
    // =====================================================================
    public class FloatingCoin
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float   Life = 1.5f;
        public int     Amount;
    }

    // =====================================================================
    //  FRUIT PICKUP
    // =====================================================================
    public class FruitPickup
    {
        public Vector2 Position;
        public int     Value;
        public string  Kind;
        public float   Spin;
    }

    // =====================================================================
    //  PROJECTILE
    // =====================================================================
    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float   Damage;
        public float   Life = 2f;
        public bool    Friendly = true;
        public Color   Color;
    }

    // =====================================================================
    //  PARTICLE
    // =====================================================================
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float   Life;
        public float   MaxLife;
        public float   Size;
        public float   Rotation;
        public float   RotVel;
        public Color   Color;
        public bool    IsSpark;
    }

    // =====================================================================
    //  ENEMY (standalone - not inheriting from abstract Entity)
    // =====================================================================
    public class Enemy
    {
        public EnemyType Type;
        public Vector2   Position;
        public bool      IsActive  = true;
        public float     Health;
        public float     MaxHealth;
        public float     Speed;
        public float     Damage;
        public float     AttackRange;
        public float     AttackCooldown;
        public float     AttackTimer;
        public float     HitFlash;
        public float     KnockbackX;
        public bool      FacingRight;
        public bool      IsDead;
        public bool      DeathAnimDone;
        public float     DeathTimer;
        public int       GoldDrop;
        public int       XPDrop;
        public float     WalkAnim;
        public int       Wave;
        public bool      IsBoss;
        public float     BossPhase;
        public float     ShieldHealth;
        public float     WidthScale = 1f;
        public float     HeightScale = 1f;
        public int       Variant = 0;

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            Health -= dmg;
            HitFlash = 0.18f;
            if (Health <= 0) { Health = 0; IsDead = true; IsActive = false; }
        }
    }

    // =====================================================================
    //  PLAYER (standalone - not inheriting from abstract Entity)
    // =====================================================================
    public class Player
    {
        public Vector2  Position;
        public bool     IsActive  = true;
        public float    Health    = 100f;
        public float    MaxHealth = 100f;
        public float    Stamina   = 100f;
        public float    MaxStamina = 100f;
        public float    BaseDamage  = 18f;
        public float    CritChance  = 0.15f;
        public float    Defense     = 0f;
        public float    MoveSpeed   = 220f;
        public float    JumpPower   = -520f;
        public float    VelocityY   = 0f;
        public bool     OnGround    = true;
        public bool     FacingRight = true;
        public bool     IsDead      = false;
        public float    DeathTimer  = 0f;
        public int      Gold       = 80;
        public int      FruitCount = 0;
        public int      WoodCount  = 0;
        public int      StoneCount = 0;
        public int      XP         = 0;
        public int      Level      = 1;
        public int      XPToNext   = 100;
        public WeaponType Weapon       = WeaponType.Fists;
        public bool      HasShield     = false;
        public bool      HasBoots      = false;
        public int       ArmorLevel    = 0;
        public int       HealthPotions = 0;
        public int       Bombs         = 0;
        public bool      IsAttacking   = false;
        public float     AttackTimer   = 0f;
        public float     AttackDuration = 0.35f;
        public float     AttackCooldown = 0f;
        public float     IFrameTimer   = 0f;
        public float     ComboCount    = 0;
        public float     ComboWindow   = 0f;
        public int       ComboHits     = 0;
        public float     WalkAnim      = 0f;
        public float     SwordArcAngle = 0f;
        public float     SwordArcProgress = 0f;

        public bool IsInvincible => IFrameTimer > 0f;
        public bool CanAttack => AttackCooldown <= 0f && !IsDead;

        public void AddXP(int amount, out bool leveledUp)
        {
            XP += amount;
            leveledUp = false;
            while (XP >= XPToNext)
            {
                XP -= XPToNext;
                Level++;
                XPToNext = (int)(XPToNext * 1.45f);
                MaxHealth += 15f;
                Health = MaxHealth;
                BaseDamage += 3f;
                Defense += 1f;
                leveledUp = true;
            }
        }
    }

    // =====================================================================
    //  SHOP ITEM
    // =====================================================================
    public class ShopItem
    {
        public string Name;
        public string Description;
        public int    Cost;
        public string Icon;
        public bool   IsUnlocked = true;
        public bool   Purchased  = false;
        public Color  TintColor;
    }

    // =====================================================================
    //  ACHIEVEMENT
    // =====================================================================
    public class Achievement
    {
        public string Title;
        public string Desc;
        public float  ShowTimer = 3.5f;
        public bool   Shown     = false;
    }

    // =====================================================================
    //  QUEST
    // =====================================================================
    public class Quest
    {
        public string Title;
        public string Desc;
        public int    Progress;
        public int    Goal;
        public bool   Complete;
        public int    RewardGold;
    }

    // =====================================================================
    //  SWORD SLASH EFFECT
    // =====================================================================
    public class SwordSlash
    {
        public Vector2 Origin;
        public float   Angle;
        public float   Life   = 0.28f;
        public float   MaxLife = 0.28f;
        public float   ArcSpan;
        public float   Radius;
        public bool    FacingRight;
        public List<Particle> Sparks = new List<Particle>();
    }

    // =====================================================================
    //  MAIN GAME CLASS
    // =====================================================================
    public class Game1 : Game
    {
        private GraphicsDeviceManager _gfx;
        private SpriteBatch           _sb;
        private SoundManager          _snd = new SoundManager();
        private Random                _rng = new Random();

        public GameState  State     = GameState.Welcome;
        private GameState _pauseReturn;
        private GameState _shopReturnState;
        private int       _shopSelected = 0;
        private int       _settingsSelected = 0;

        private const int BASE_W = 1280;
        private const int BASE_H = 720;
        private RenderTarget2D _rt;
        private Rectangle      _rtRect;
        private bool           _fullscreen;

        private Texture2D _pixel;
        private Texture2D _circleTex;
        private SpriteFont _font;

        private Player      _player = new Player();
        private List<Enemy> _enemies = new List<Enemy>();

        private List<Vector2> _trees      = new List<Vector2>();
        private List<float>   _treeSizes  = new List<float>();
        private List<Vector2> _grasses    = new List<Vector2>();
        private List<Vector2> _rocks      = new List<Vector2>();
        private List<Rectangle> _platRects = new List<Rectangle>();

        private Vector2 _cam;
        private float   _shakeTimer;
        private float   _shakeIntensity;

        private List<Particle>      _particles     = new List<Particle>();
        private List<Particle>      _rainDrops     = new List<Particle>();
        private List<DamageNumber>  _dmgNums       = new List<DamageNumber>();
        private List<FloatingCoin>  _coins         = new List<FloatingCoin>();
        private List<FruitPickup>   _fruits        = new List<FruitPickup>();
        private List<Projectile>    _projectiles   = new List<Projectile>();
        private List<SwordSlash>    _slashes       = new List<SwordSlash>();
        private List<Achievement>   _achQueue      = new List<Achievement>();
        private Achievement         _currentAch;

        private List<Quest> _quests = new List<Quest>();
        private List<ShopItem> _shopItems = new List<ShopItem>();

        private int   _wave         = 1;
        private int   _waveKills    = 0;
        private int   _waveGoal     = 5;
        private float _waveCooldown = 0f;
        [Obsolete] private bool  _bossSpawned  = false;
        [Obsolete] private bool  _bossDefeated = false;
        private int   _totalBossesDefeated = 0;
        private const int BOSSES_TO_WIN = 3;

        private float _dayTime      = 0f;
        private float _daySpeed     = 0.003f;
        private Weather _weather    = Weather.Clear;
        private float   _weatherTimer = 0f;

        private float _welcomeTimer   = 0f;
        private float _lightningTimer = 0f;
        private bool  _showLightning  = false;
        private float _lightningAlpha = 0f;
        private List<Particle> _welcomeParticles = new List<Particle>();
        private float _titleY         = -120f;
        private float _menuItemsAlpha = 0f;
        private int   _welcomeMenuSel = 0;
        private float _orbAngle       = 0f;
        private float _levelTipTimer   = 0f;
        private string _levelTipText   = "";
        private bool   _musicEnabled    = true;
        private SoundEffectInstance _musicInstance;
        private int    _highestUnlockedStage = 1;
        private int    _selectedLevel   = 1;
        private float  _levelSelectTimer = 0f;
        private string _levelSelectMessage = "";
        private bool  _rotateScreen    = false;
        private bool  _mobileMode      = true;
        private bool  _touchMoveLeft, _touchMoveRight, _touchJump, _touchAttack;
        private string _shopMessage    = "";
        private float  _shopMessageTimer = 0f;
        private int    _stageIndex     = 1;
        private int    _stageTheme     = 0;
        private bool   _stageAdvancePending = false;
        private float  _stageClearTimer = 0f;
        private const int MAX_STAGE = 100;

        private float _endScreenTimer = 0f;
        private bool  _endSoundPlayed = false;

        private KeyboardState _kPrev, _kCurr;
        private MouseState    _mPrev, _mCurr;

        private float _globalTimer  = 0f;
        private float _blinkTimer   = 0f;
        private bool  _blinkOn      = true;
        // ---- Cave spawner (house that continuously spawns zombies) ----
        private Vector2 _cavePos = Vector2.Zero;
        private int _caveHealth = 0;
        private float _caveSpawnTimer = 0f;

        private float _levelUpTimer = 0f;
        private float _comboDisplayTimer = 0f;
        private int   _lastCombo = 0;

        public Game1()
        {
            _gfx = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _gfx.PreferredBackBufferWidth  = BASE_W;
            _gfx.PreferredBackBufferHeight = BASE_H;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (s, e) => RecalcRTRect();
        }

        protected override void Initialize()
        {
            _player.Position = new Vector2(300, Globals.GroundY);
            SetupShop();
            SetupQuests();
            GenerateWorld();
            SpawnWave(_wave);
            SpawnWelcomeParticles();
            base.Initialize();
        }

        private void SetupShop()
        {
            _shopItems = new List<ShopItem>
            {
                new ShopItem { Name="Iron Sword",    Description="+20 DMG  One-handed blade",   Cost=50,  Icon="[SWORD]",  TintColor=new Color(180,220,255) },
                new ShopItem { Name="Battle Axe",    Description="+35 DMG  Slow but devastating",Cost=100, Icon="[AXE]",    TintColor=new Color(255,140,80)  },
                new ShopItem { Name="Magic Staff",   Description="+28 DMG  AoE burst on hit",   Cost=130, Icon="[STAFF]",  TintColor=new Color(180,80,255)  },
                new ShopItem { Name="Hunter Pistol", Description="Rapid shots  long range",      Cost=140, Icon="[PISTL]",  TintColor=new Color(220,220,220) },
                new ShopItem { Name="Health Potion", Description="Restore 40 HP  (stackable)",  Cost=25,  Icon="[POTN]",   TintColor=new Color(255,80,80), Purchased=false  },
                new ShopItem { Name="Iron Shield",   Description="-30% incoming damage",        Cost=80,  Icon="[SHLD]",   TintColor=new Color(160,160,200) },
                new ShopItem { Name="Swift Boots",   Description="+40 move speed  +jump height",Cost=70,  Icon="[BOOT]",   TintColor=new Color(255,220,80)  },
                new ShopItem { Name="Bomb x3",       Description="Throw bomb (B key)  big AoE", Cost=45,  Icon="[BOMB]",   TintColor=new Color(80,255,80)   },
                new ShopItem { Name="Max HP Up",     Description="+50 Max HP  permanent",       Cost=90,  Icon="[MXHP]",   TintColor=new Color(255,120,180) },
                new ShopItem { Name="Armor Upgrade", Description="+10 defense  stronger body",  Cost=120, Icon="[ARMR]",   TintColor=new Color(180,220,160) },
                new ShopItem { Name="VIP Gold Pack", Description="+250 gold  instant boost",    Cost=0,   Icon="[VIP++]",  TintColor=new Color(255,215,0)   },
            };
        }

        private void SetupQuests()
        {
            _quests = new List<Quest>
            {
                new Quest { Title="First Blood",   Desc="Kill 3 enemies",           Goal=3,  RewardGold=30  },
                new Quest { Title="Wave Warrior",  Desc="Survive 3 waves",          Goal=3,  RewardGold=60  },
                new Quest { Title="Rich Adventurer",Desc="Collect 200 coins",       Goal=200,RewardGold=50  },
                new Quest { Title="Level 5",       Desc="Reach player level 5",     Goal=5,  RewardGold=100 },
                new Quest { Title="Boss Slayer",   Desc="Defeat the first boss",    Goal=1,  RewardGold=150 },
            };
        }

        private void GenerateWorld()
        {
            _stageTheme = Math.Min(5, (_stageIndex - 1) / 20);
            _trees.Clear();
            _treeSizes.Clear();
            _grasses.Clear();
            _rocks.Clear();
            _platRects.Clear();
            _fruits.Clear();

            int treeCount = 36 + (_stageIndex - 1) * 5;
            int grassCount = 58 + (_stageIndex - 1) * 9;
            int rockCount = 18 + (_stageIndex - 1) * 3;
            int platCount = 12 + (_stageIndex - 1) * 2;

            for (int i = 0; i < treeCount; i++)
            {
                float x = _rng.Next(200, (int)Globals.WorldWidth - 200);
                _trees.Add(new Vector2(x, Globals.GroundY));
                _treeSizes.Add(0.75f + (float)_rng.NextDouble() * 0.6f);
            }
            for (int i = 0; i < grassCount; i++)
                _grasses.Add(new Vector2(_rng.Next(100, (int)Globals.WorldWidth - 100), Globals.GroundY));
            for (int i = 0; i < rockCount; i++)
                _rocks.Add(new Vector2(_rng.Next(200, (int)Globals.WorldWidth - 200), Globals.GroundY - 6));
            for (int i = 0; i < platCount; i++)
            {
                int pw = _rng.Next(120, 260);
                int px = _rng.Next(300, (int)Globals.WorldWidth - 400);
                int py = (int)Globals.GroundY - _rng.Next(80, 220);
                _platRects.Add(new Rectangle(px, py, pw, 18));
            }
            for (int i = 0; i < 10; i++)
            {
                int pw = _rng.Next(160, 260);
                int px = _rng.Next(200, (int)Globals.WorldWidth - 500);
                int py = (int)Globals.GroundY - _rng.Next(70, 150);
                _platRects.Add(new Rectangle(px, py, pw, 18));
                _fruits.Add(new FruitPickup { Position = new Vector2(px + pw / 2f, py - 16), Value = 6 + _rng.Next(0, 4), Kind = "Leaf Fruit", Spin = (float)_rng.NextDouble() * MathHelper.TwoPi });
            }
            if (_stageIndex >= 15)
            {
                int houseX = 900 + (_stageIndex % 5) * 180;
                int houseY = (int)Globals.GroundY - 96;
                _platRects.Add(new Rectangle(houseX - 18, houseY, 210, 18));
                _platRects.Add(new Rectangle(houseX + 34, houseY - 60, 92, 14));
                _fruits.Add(new FruitPickup { Position = new Vector2(houseX + 88, houseY - 78), Value = 10 + _stageIndex / 10, Kind = "House Fruit", Spin = 0f });
            }
            if (_stageIndex >= 35)
            {
                int nestX = (int)Globals.WorldWidth - 920;
                int nestY = (int)Globals.GroundY - 188;
                _platRects.Add(new Rectangle(nestX, nestY, 180, 18));
                _platRects.Add(new Rectangle(nestX + 36, nestY - 54, 104, 12));
                _fruits.Add(new FruitPickup { Position = new Vector2(nestX + 78, nestY - 76), Value = 16 + _stageIndex / 8, Kind = "Nest Fruit", Spin = 0f });
            }
            // ---- Add a hidden cave that spawns zombies continuously (stage 45+)
            if (_stageIndex >= 45)
            {
                // Position the cave near the right side of the world, slightly above ground
                int caveX = (int)Globals.WorldWidth - 800;
                int caveY = (int)Globals.GroundY - 120;
                _cavePos = new Vector2(caveX, caveY);
                _caveHealth = 250 + (_stageIndex - 45) * 30; // scale health with stage
                // Add a simple platform for the cave entrance
                _platRects.Add(new Rectangle(caveX - 30, caveY, 60, 12));
                // Add a decorative fruit (apple) on the cave roof for collection
                _fruits.Add(new FruitPickup { Position = new Vector2(caveX, caveY - 30), Value = 12, Kind = "Apple", Spin = 0f });
            }
            _platRects.Add(new Rectangle((int)Globals.WorldWidth - 620, (int)Globals.GroundY - 120, 220, 18));
            _platRects.Add(new Rectangle((int)Globals.WorldWidth - 420, (int)Globals.GroundY - 150, 160, 18));
            _fruits.Add(new FruitPickup { Position = new Vector2(Globals.WorldWidth - 520, Globals.GroundY - 94), Value = 15, Kind = "Temple Fruit", Spin = 0f });
            _stageAdvancePending = true;
        }

        private void SpawnWave(int waveNum)
        {
            _waveCooldown = 0f;
            _bossSpawned  = false;
            int count = 4 + waveNum * 2;
            _waveGoal = count + (waveNum % 3 == 0 ? 1 : 0);
            bool isBossWave = (waveNum % 3 == 0);

            for (int i = 0; i < count; i++)
            {
                EnemyType type = EnemyType.Zombie;
                if (_stageIndex >= 8 && _rng.NextDouble() < 0.38f) type = EnemyType.Goblin;
                if (_stageIndex >= 18 && _rng.NextDouble() < 0.28f) type = EnemyType.Skeleton;
                if (_stageIndex >= 28 && _rng.NextDouble() < 0.2f) type = EnemyType.Shielded;
                if (_stageIndex >= 45 && _rng.NextDouble() < 0.2f) type = EnemyType.Zombie;

                float spawnX = _player.Position.X + (float)(_rng.NextDouble() < 0.5 ? -1 : 1) * (_rng.Next(600, 1400));
                spawnX = MathHelper.Clamp(spawnX, 100, Globals.WorldWidth - 100);
                _enemies.Add(CreateEnemy(type, new Vector2(spawnX, Globals.GroundY), waveNum));
            }

            if (isBossWave)
            {
                float bx = _player.Position.X + (_rng.NextDouble() < 0.5 ? -1200 : 1200);
                bx = MathHelper.Clamp(bx, 200, Globals.WorldWidth - 200);
                var boss = CreateEnemy(EnemyType.Boss, new Vector2(bx, Globals.GroundY), waveNum);
                boss.IsBoss = true;
                _enemies.Add(boss);
                _bossSpawned = true;
                _snd.Play(_snd.BossRoar);
            }
            _waveKills = 0;
            _levelTipText = waveNum % 3 == 0
                ? $"Boss wave {waveNum}: stay mobile and use armor upgrades."
                : waveNum < 4
                    ? $"Tip for wave {waveNum}: use platforms and trees to keep height."
                    : $"Tip for wave {waveNum}: pistol and magic staff work best on clustered enemies.";
            _levelTipTimer = 4.5f;
        }

        private Enemy CreateEnemy(EnemyType type, Vector2 pos, int wave)
        {
            float scale = 1f + wave * 0.08f;
            Enemy e = new Enemy { Type = type, Position = pos, Wave = wave };
            switch (type)
            {
                case EnemyType.Zombie:
                    e.MaxHealth  = (60 + wave * 12) * scale;   e.Speed = 70f + wave * 5f;
                    e.Damage     = 10f + wave * 2f;             e.AttackRange = 55f;
                    e.GoldDrop   = _rng.Next(5, 14);            e.XPDrop = 15 + wave * 3;
                    if (wave >= 3 && _rng.NextDouble() < 0.28)
                    {
                        e.Variant = 1;
                        e.WidthScale = 0.78f;
                        e.HeightScale = 0.82f;
                        e.Speed *= 1.7f;
                        e.MaxHealth *= 0.75f;
                    }
                    else if (wave >= 4 && _rng.NextDouble() < 0.25)
                    {
                        e.Variant = 2;
                        e.WidthScale = 1.26f;
                        e.HeightScale = 1.18f;
                        e.Speed *= 0.72f;
                        e.MaxHealth *= 1.75f;
                        e.Damage *= 1.45f;
                    }
                    if (_stageIndex >= 50 && _rng.NextDouble() < 0.2)
                    {
                        e.Variant = 3;
                        e.WidthScale = 1.12f;
                        e.HeightScale = 1.32f;
                        e.Speed *= 1.05f;
                        e.MaxHealth *= 2.1f;
                        e.Damage *= 1.8f;
                    }
                    break;
                case EnemyType.Goblin:
                    e.MaxHealth  = (35 + wave * 8) * scale;    e.Speed = 120f + wave * 8f;
                    e.Damage     = 14f + wave * 2f;             e.AttackRange = 45f;
                    e.GoldDrop   = _rng.Next(8, 18);            e.XPDrop = 20 + wave * 4;
                    break;
                case EnemyType.Skeleton:
                    e.MaxHealth  = (80 + wave * 18) * scale;   e.Speed = 60f + wave * 3f;
                    e.Damage     = 18f + wave * 3f;             e.AttackRange = 65f;
                    e.GoldDrop   = _rng.Next(12, 22);           e.XPDrop = 28 + wave * 5;
                    break;
                case EnemyType.Shielded:
                    e.MaxHealth  = (130 + wave * 22) * scale;  e.Speed = 55f + wave * 3f;
                    e.Damage     = 20f + wave * 4f;             e.AttackRange = 75f;
                    e.GoldDrop   = _rng.Next(18, 30);           e.XPDrop = 34 + wave * 6;
                    e.ShieldHealth = 60f + wave * 12f;
                    break;
                case EnemyType.Boss:
                    e.MaxHealth  = (500 + wave * 80) * scale;  e.Speed = 50f + wave * 4f;
                    e.Damage     = 30f + wave * 5f;             e.AttackRange = 90f;
                    e.GoldDrop   = _rng.Next(80, 140);          e.XPDrop = 200 + wave * 30;
                    break;
            }
            if (wave >= 5 && type == EnemyType.Goblin && _rng.NextDouble() < 0.2)
                e.Speed *= 1.35f;
            if (wave >= 6 && type == EnemyType.Skeleton && _rng.NextDouble() < 0.2)
                e.Speed *= 1.25f;
            e.Health = e.MaxHealth;
            e.AttackCooldown = (type == EnemyType.Boss) ? 2.2f : 1.4f;
            return e;
        }

        private void SpawnWelcomeParticles()
        {
            for (int i = 0; i < 80; i++)
                _welcomeParticles.Add(MakeWelcomeParticle());
        }

        private Particle MakeWelcomeParticle()
        {
            return new Particle
            {
                Position = new Vector2(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H)),
                Velocity = new Vector2((float)_rng.NextDouble() * 40f - 20f, -(float)_rng.NextDouble() * 30f - 5f),
                Life = 2f + (float)_rng.NextDouble() * 6f, MaxLife = 8f,
                Size = 2f + (float)_rng.NextDouble() * 5f,
                Color = new Color(_rng.Next(30,120), _rng.Next(160,255), _rng.Next(30,100), 200)
            };
        }

        protected override void LoadContent()
        {
            _sb    = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _circleTex = new Texture2D(GraphicsDevice, 16, 16);
            var cd = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f;
                    cd[y*16+x] = (float)Math.Sqrt(dx*dx + dy*dy) < 7.5f ? Color.White : Color.Transparent;
                }
            _circleTex.SetData(cd);

            try { _font = Content.Load<SpriteFont>("Fonts/GameFont"); } catch { }

            _rt   = new RenderTarget2D(GraphicsDevice, BASE_W, BASE_H);
            RecalcRTRect();
            SetupAudio();
            ApplyMusicState();
        }

        private void RecalcRTRect()
        {
            if (GraphicsDevice == null) return;
            int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float aspect = (float)BASE_W / BASE_H;
            float newAspect = (float)w / h;
            if (newAspect > aspect)
            {
                int rw = (int)(h * aspect);
                _rtRect = new Rectangle((w - rw) / 2, 0, rw, h);
            }
            else
            {
                int rh = (int)(w / aspect);
                _rtRect = new Rectangle(0, (h - rh) / 2, w, rh);
            }
        }

        protected override void Update(GameTime gt)
        {
            _kPrev = _kCurr; _kCurr = Keyboard.GetState();
            _mPrev = _mCurr; _mCurr = Mouse.GetState();

            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _globalTimer += dt;
            _blinkTimer  += dt;
            if (_blinkTimer > 0.5f) { _blinkOn = !_blinkOn; _blinkTimer = 0f; }

            if (KeyJustPressed(Keys.F11)) ToggleFullscreen();

            switch (State)
            {
                case GameState.Welcome: UpdateWelcome(dt); break;
                case GameState.LevelSelect: UpdateLevelSelect(dt); break;
                case GameState.Playing: UpdatePlaying(dt); break;
                case GameState.Shop:    UpdateShop(dt);    break;
                case GameState.FullMap: UpdateFullMap();   break;
                case GameState.StageClear: UpdateStageClear(dt); break;
                case GameState.Paused:  UpdatePaused();    break;
                case GameState.Help:    UpdateHelp();      break;
                case GameState.Credits: UpdateCredits();   break;
                case GameState.Win:     UpdateEndScreen(dt); break;
                case GameState.Lose:    UpdateEndScreen(dt); break;
            }
            base.Update(gt);
        }

        private void UpdateWelcome(float dt)
        {
            _welcomeTimer += dt;
            _orbAngle     += dt * 0.6f;
            _titleY = MathHelper.Lerp(_titleY, 112f, dt * 4f);
            if (_welcomeTimer > 0.8f) _menuItemsAlpha = Math.Min(1f, _menuItemsAlpha + dt * 1.5f);

            _lightningTimer -= dt;
            if (_lightningTimer <= 0f)
            {
                _showLightning  = true;
                _lightningAlpha = 0.7f;
                _lightningTimer = 4f + (float)_rng.NextDouble() * 8f;
            }
            if (_showLightning)
            {
                _lightningAlpha -= dt * 4f;
                if (_lightningAlpha <= 0f) { _showLightning = false; _lightningAlpha = 0f; }
            }

            for (int i = 0; i < _welcomeParticles.Count; i++)
            {
                var p = _welcomeParticles[i];
                p.Position += p.Velocity * dt;
                p.Life     -= dt;
                if (p.Life <= 0 || p.Position.Y > BASE_H + 30)
                    p = MakeWelcomeParticle();
                _welcomeParticles[i] = p;
            }

            if (KeyJustPressed(Keys.Up)   || KeyJustPressed(Keys.W)) _welcomeMenuSel = (_welcomeMenuSel - 1 + 6) % 6;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _welcomeMenuSel = (_welcomeMenuSel + 1) % 6;
            if (KeyJustPressed(Keys.Enter))
            {
                ActivateWelcomeSelection(_welcomeMenuSel);
            }
            for (int i = 0; i < 6; i++)
            {
                var r = WelcomeBtn(i);
                if (r.Contains(MousePos())) _welcomeMenuSel = i;
                if (MouseJustReleased() && r.Contains(MousePos())) ActivateWelcomeSelection(i);
            }
            if (KeyJustPressed(Keys.M) || (MouseJustReleased() && MusicButtonRect().Contains(MousePos()))) ToggleMusic();
        }

        private void ActivateWelcomeSelection(int idx)
        {
            switch (idx)
            {
                case 0: StartGame(); break;
                case 1: OpenShop(GameState.Welcome); break;
                case 2: GoSettings(); break;
                case 3: State = GameState.Help; break;
                case 4: State = GameState.Credits; break;
                case 5: Exit(); break;
            }
        }

        private void StartGame()
        {
            _selectedLevel = Math.Max(1, Math.Min(MAX_STAGE, _highestUnlockedStage));
            _levelSelectMessage = "Choose a stage. Locked levels unlock as you clear them.";
            _levelSelectTimer = 3f;
            State = GameState.LevelSelect;
        }
        private void OpenShop(GameState returnState) { _shopReturnState = returnState; State = GameState.Shop; }
        private void GoSettings() { _pauseReturn = State; _settingsSelected = 0; State = GameState.Paused; }

        private void UpdatePlaying(float dt)
        {
            if (_player.IsDead)
            {
                _player.DeathTimer += dt;
                if (_player.DeathTimer > 1.2f) State = GameState.Lose;
                return;
            }

            UpdateMobileControls();
            UpdateDayNight(dt);
            UpdateWeather(dt);
            UpdatePlayerMovement(dt);
            UpdatePlayerCombat(dt);
            UpdateEnemies(dt);
            UpdateParticles(dt);
            UpdateDamageNumbers(dt);
            UpdateFloatingCoins(dt);
            UpdateFruits(dt);
            UpdateProjectiles(dt);
            UpdateSlashes(dt);
            UpdateWaveSystem(dt);
            UpdateQuests();
            UpdateAchievements(dt);
            if (_levelUpTimer > 0f) _levelUpTimer -= dt;
            if (_comboDisplayTimer > 0f) _comboDisplayTimer -= dt;
            UpdateCamera(dt);

            if (KeyJustPressed(Keys.I) || KeyJustPressed(Keys.B)) { OpenShop(GameState.Playing); }
            if (KeyJustPressed(Keys.M)) State = GameState.FullMap;
            if (KeyJustPressed(Keys.Escape)) { GoSettings(); }
            if (KeyJustPressed(Keys.Q) && _player.Bombs > 0) ThrowBomb();
            if (KeyJustPressed(Keys.H) && _player.HealthPotions > 0)
            {
                _player.HealthPotions--;
                _player.Health = Math.Min(_player.MaxHealth, _player.Health + 40f);
                SpawnDamageNumber(_player.Position, 40f, isHeal: true);
            }

            if (_totalBossesDefeated >= BOSSES_TO_WIN && State != GameState.Win)
                State = GameState.Win;

            if (_levelTipTimer > 0f)
                _levelTipTimer -= dt;

            if (_stageAdvancePending && _player.Position.X > Globals.WorldWidth - 420f)
            {
                _stageAdvancePending = false;
                _highestUnlockedStage = Math.Max(_highestUnlockedStage, Math.Min(MAX_STAGE, _stageIndex + 1));
                _stageClearTimer = 0f;
                if (_stageIndex >= MAX_STAGE)
                {
                    State = GameState.Win;
                    return;
                }
                _snd.Play(_snd.LevelUp);
                State = GameState.StageClear;
            }
        }

        private void UpdateDayNight(float dt) { _dayTime = (_dayTime + _daySpeed * dt) % 1f; }

        private void UpdateWeather(float dt)
        {
            _weatherTimer -= dt;
            if (_weatherTimer <= 0f)
            {
                int r = _rng.Next(3);
                _weather      = (Weather)r;
                _weatherTimer = 20f + (float)_rng.NextDouble() * 40f;
            }
            if (_weather == Weather.Rain || _weather == Weather.Storm)
            {
                int spawnCount = _weather == Weather.Storm ? 8 : 3;
                for (int i = 0; i < spawnCount; i++)
                {
                    _rainDrops.Add(new Particle
                    {
                        Position = new Vector2(_cam.X + _rng.Next(0, BASE_W), _cam.Y - 20),
                        Velocity = new Vector2(-30f, 500f + (_weather == Weather.Storm ? 200f : 0f)),
                        Life = 1f, MaxLife = 1f, Size = 1.5f,
                        Color = new Color(160, 190, 220, 160)
                    });
                }
            }
            for (int i = _rainDrops.Count - 1; i >= 0; i--)
            {
                var r = _rainDrops[i];
                r.Position += r.Velocity * dt; r.Life -= dt;
                if (r.Life <= 0f || r.Position.Y > _cam.Y + BASE_H + 10) _rainDrops.RemoveAt(i);
                else _rainDrops[i] = r;
            }
        }

        private void UpdatePlayerMovement(float dt)
        {
            float spd = _player.MoveSpeed;
            float dx  = 0f;
            if (_kCurr.IsKeyDown(Keys.A) || _kCurr.IsKeyDown(Keys.Left) || _touchMoveLeft)  { dx = -spd; _player.FacingRight = false; }
            if (_kCurr.IsKeyDown(Keys.D) || _kCurr.IsKeyDown(Keys.Right) || _touchMoveRight) { dx =  spd; _player.FacingRight = true;  }
            _player.Position.X += dx * dt;
            _player.Position.X  = MathHelper.Clamp(_player.Position.X, 20, Globals.WorldWidth - 20);

            if ((KeyJustPressed(Keys.W) || KeyJustPressed(Keys.Up) || _touchJump) && _player.OnGround)
            {
                _player.VelocityY = _player.JumpPower * (_player.HasBoots ? 1.25f : 1f);
                _player.OnGround  = false;
            }
            _player.VelocityY += 980f * dt;
            _player.Position.Y += _player.VelocityY * dt;

            _player.OnGround = false;
            foreach (var plat in _platRects)
            {
                if (_player.VelocityY >= 0f && _player.Position.X > plat.X && _player.Position.X < plat.Right &&
                    _player.Position.Y >= plat.Y && _player.Position.Y <= plat.Y + _player.VelocityY * dt + 10f)
                {
                    _player.Position.Y = plat.Y; _player.VelocityY = 0f; _player.OnGround = true;
                }
            }
            if (_player.Position.Y >= Globals.GroundY)
            {
                _player.Position.Y = Globals.GroundY; _player.VelocityY = 0f; _player.OnGround = true;
            }

            if (Math.Abs(dx) > 0.1f) _player.WalkAnim += dt * 8f;
            _player.Stamina = Math.Min(_player.MaxStamina, _player.Stamina + 20f * dt);
            if (_player.IFrameTimer > 0f) _player.IFrameTimer -= dt;
            if (_player.ComboWindow > 0f)
            {
                _player.ComboWindow -= dt;
                if (_player.ComboWindow <= 0f) _player.ComboHits = 0;
            }
        }

        private void UpdatePlayerCombat(float dt)
        {
            if (_player.AttackTimer > 0f)      _player.AttackTimer   -= dt;
            if (_player.AttackCooldown > 0f)   _player.AttackCooldown -= dt;
            if (_player.AttackTimer <= 0f) _player.IsAttacking = false;

            bool attackPressed = KeyJustPressed(Keys.Z) || KeyJustPressed(Keys.X) || MouseJustReleased() || _touchAttack;
            if (attackPressed && _player.CanAttack) PerformPlayerAttack();
        }

        private void UpdateMobileControls()
        {
            _touchMoveLeft = _touchMoveRight = _touchJump = _touchAttack = false;
            if (!_mobileMode) return;

            foreach (var touch in TouchPanel.GetState())
                ApplyMobilePoint(new Point((int)touch.Position.X, (int)touch.Position.Y));

            if (_mCurr.LeftButton == ButtonState.Pressed)
                ApplyMobilePoint(_mCurr.Position);
        }

        private void ApplyMobilePoint(Point point)
        {
            if (MobileLeftRect().Contains(point)) _touchMoveLeft = true;
            if (MobileRightRect().Contains(point)) _touchMoveRight = true;
            if (MobileJumpRect().Contains(point)) _touchJump = true;
            if (MobileAttackRect().Contains(point)) _touchAttack = true;
        }

        private void PerformPlayerAttack()
        {
            if (_player.Weapon == WeaponType.Pistol)
            {
                FirePistolShot();
                return;
            }

            _player.IsAttacking = true;
            _player.AttackTimer = _player.AttackDuration;
            _player.AttackCooldown = _player.Weapon == WeaponType.Axe ? 0.7f : 0.45f;
            _player.SwordArcProgress = 0f;

            _player.ComboHits++;
            _player.ComboWindow = 0.8f;
            if (_player.ComboHits > 4) _player.ComboHits = 1;
            _lastCombo = _player.ComboHits;
            _comboDisplayTimer = 1.2f;

            _snd.PlayRandom(_snd.SwordSwing);

            // Optimized: only create slash effect every other attack to reduce GC pressure
            if ((_globalTimer * 2f) % 1f < 0.85f)
            {
                var slash = new SwordSlash
                {
                    Origin = _player.Position + new Vector2(_player.FacingRight ? 20 : -20, -50),
                    Angle = _player.FacingRight ? -MathHelper.Pi * 0.6f : MathHelper.Pi * 1.6f,
                    ArcSpan = MathHelper.Pi * 0.85f, Radius = 70f, FacingRight = _player.FacingRight
                };
                for (int i = 0; i < 5; i++)
                {
                    float ang = slash.Angle + (float)_rng.NextDouble() * slash.ArcSpan * (_player.FacingRight ? 1 : -1);
                    slash.Sparks.Add(new Particle
                    {
                        Position = slash.Origin + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * slash.Radius,
                        Velocity = new Vector2((float)_rng.NextDouble() * 200f - 100f, (float)_rng.NextDouble() * -200f),
                        Life = 0.25f, MaxLife = 0.25f, Size = 3f, IsSpark = true,
                        Color = GetWeaponColor(_player.Weapon)
                    });
                }
                _slashes.Add(slash);
            }

            float reach = _player.Weapon == WeaponType.MagicStaff ? 110f : 75f;
            bool didHit = false;
            // Use cached player position for performance
            Vector2 playerPos = _player.Position;
            float playerX = playerPos.X;
            bool facingRight = _player.FacingRight;
            
            for (int ei = 0; ei < _enemies.Count; ei++)
            {
                var en = _enemies[ei];
                if (!en.IsActive || en.IsDead) continue;
                
                float dx = Math.Abs(en.Position.X - playerX);
                float dy = Math.Abs(en.Position.Y - playerPos.Y);
                bool inDir = facingRight ? en.Position.X > playerX : en.Position.X < playerX;
                
                if (dx < reach && dy < 90f && inDir)
                {
                    bool crit = _rng.NextDouble() < _player.CritChance;
                    float dmg = _player.BaseDamage * (crit ? 2.2f : 1f) * (1f + (_player.ComboHits - 1) * 0.15f);
                    
                    if (_player.Weapon == WeaponType.MagicStaff)
                    {
                        float enX = en.Position.X, enY = en.Position.Y;
                        for (int ej = 0; ej < _enemies.Count; ej++)
                        {
                            var en2 = _enemies[ej];
                            if (!en2.IsDead && Math.Abs(en2.Position.X - enX) < 100f && Math.Abs(en2.Position.Y - enY) < 100f)
                                DamageEnemy(en2, dmg * 0.6f, false);
                        }
                    }

                    DamageEnemy(en, dmg, crit);
                    didHit = true;
                    SpawnHitParticles(en.Position);
                }
            }
            if (didHit)
            {
                _snd.PlayRandom(_snd.EnemyHit, 0.6f, 1f, 0.18f);
                TriggerScreenShake(0.1f, 3.5f);
            }
        }

        private void FirePistolShot()
        {
            _player.IsAttacking = true;
            _player.AttackTimer = 0.12f;
            _player.AttackCooldown = 0.25f;
            _snd.PlayRandom(_snd.SwordSwing, 0.4f, 0.7f, 0.12f);

            _projectiles.Add(new Projectile
            {
                Position = _player.Position + new Vector2(_player.FacingRight ? 28 : -28, -58),
                Velocity = new Vector2(_player.FacingRight ? 900f : -900f, -8f),
                Damage = _player.BaseDamage * 1.05f,
                Color = new Color(230, 230, 230)
            });
        }

        private Color GetWeaponColor(WeaponType w)
        {
            return w switch
            {
                WeaponType.Sword      => new Color(200, 230, 255),
                WeaponType.Axe        => new Color(255, 140, 60),
                WeaponType.MagicStaff => new Color(180, 80, 255),
                WeaponType.Pistol     => new Color(220, 220, 220),
                _                     => new Color(255, 220, 150)
            };
        }

        private void DamageEnemy(Enemy en, float dmg, bool crit)
        {
            if (en.ShieldHealth > 0f)
            {
                float shieldHit = Math.Min(en.ShieldHealth, dmg * 0.85f);
                en.ShieldHealth -= shieldHit;
                dmg *= 0.38f;
                if (en.ShieldHealth <= 0f)
                    QueueAchievement("SHIELD BROKEN!", "The guardian is now exposed.");
            }
            en.TakeDamage(dmg);
            if (en.Type == EnemyType.Zombie)
                _snd.PlayRandom(_snd.ZombieGroan, 0.22f, 0.42f, 0.08f);
            en.KnockbackX = (_player.FacingRight ? 1f : -1f) * (crit ? 280f : 160f);
            SpawnDamageNumber(en.Position + new Vector2(0, -40), dmg, false, crit);
            if (en.IsDead) OnEnemyKilled(en);
        }

        private void OnEnemyKilled(Enemy en)
        {
            _waveKills++;
            _snd.PlayRandom(_snd.EnemyDeath);
            SpawnCoin(en.Position, en.GoldDrop);
            _player.Gold += en.GoldDrop;
            _player.FruitCount += Math.Max(1, en.GoldDrop / 4);
            _snd.Play(_snd.CoinPickup);
            bool leveledUp;
            _player.AddXP(en.XPDrop, out leveledUp);
            if (leveledUp)
            {
                _levelUpTimer = 2.5f;
                _snd.Play(_snd.LevelUp);
                TriggerScreenShake(0.2f, 5f);
                QueueAchievement($"LEVEL {_player.Level}!", "You levelled up!");
            }
            if (en.IsBoss)
            {
                _totalBossesDefeated++;
                _bossDefeated = true;
                TriggerScreenShake(0.6f, 12f);
                QueueAchievement("BOSS DEFEATED!", $"Wave {_wave} boss slain!");
                _snd.Play(_snd.CoinPickup);
                var q = _quests.Find(x => x.Title == "Boss Slayer");
                if (q != null && !q.Complete) q.Progress++;
            }
            SpawnDeathParticles(en.Position, en.Type);
            var qfb = _quests.Find(x => x.Title == "First Blood");
            if (qfb != null && !qfb.Complete) qfb.Progress++;
        }

        private void UpdateEnemies(float dt)
        {
            foreach (var en in _enemies)
            {
                if (!en.IsActive) continue;
                if (en.IsDead)
                {
                    en.DeathTimer += dt;
                    en.Position.Y += 30f * dt;
                    en.Position.X += en.FacingRight ? 8f * dt : -8f * dt;
                    if (en.DeathTimer > 1.2f) en.DeathAnimDone = true;
                    continue;
                }
                if (en.HitFlash > 0f) en.HitFlash -= dt;
                if (Math.Abs(en.KnockbackX) > 1f)
                {
                    en.Position.X += en.KnockbackX * dt;
                    en.KnockbackX = MathHelper.Lerp(en.KnockbackX, 0f, dt * 10f);
                }

                float xDiff = _player.Position.X - en.Position.X;
                en.FacingRight = xDiff > 0;

                if (Math.Abs(xDiff) > en.AttackRange)
                {
                    float spd = en.Speed * (en.IsBoss && en.Health < en.MaxHealth * 0.5f ? 1.4f : 1f);
                    en.Position.X += Math.Sign(xDiff) * spd * dt;
                    en.WalkAnim   += dt * (spd / 70f) * 6f;
                }

                en.Position.Y += 500f * dt;
                if (en.Position.Y > Globals.GroundY) en.Position.Y = Globals.GroundY;

                foreach (var plat in _platRects)
                    if (en.Position.X > plat.X && en.Position.X < plat.Right && en.Position.Y >= plat.Y && en.Position.Y < plat.Y + 20f)
                        en.Position.Y = plat.Y;

                en.AttackTimer -= dt;
                if (en.AttackTimer <= 0f && Math.Abs(xDiff) < en.AttackRange && Math.Abs(en.Position.Y - _player.Position.Y) < 60f)
                {
                    en.AttackTimer = en.AttackCooldown;
                    if (!_player.IsInvincible && !_player.IsDead)
                    {
                        float dmgTaken = en.Damage * (1f - _player.Defense / 100f) * (_player.HasShield ? 0.7f : 1f);
                        _player.Health -= dmgTaken;
                        _player.IFrameTimer = 0.55f;
                        SpawnDamageNumber(_player.Position + new Vector2(0, -60), dmgTaken, false, false, true);
                        _snd.PlayRandom(_snd.PlayerHit);
                        TriggerScreenShake(0.18f, 6f);
                        if (_player.Health <= 0f) { _player.Health = 0f; _player.IsDead = true; _snd.Play(_snd.LoseStinger); }
                    }
                }
                // ---- Cave spawner logic (continuous zombie spawn while cave is alive)
                if (_caveHealth > 0)
                {
                    _caveSpawnTimer -= dt;
                    if (_caveSpawnTimer <= 0f)
                    {
                        // Spawn a basic zombie at the cave entrance
                        var caveZombie = CreateEnemy(EnemyType.Zombie, new Vector2(_cavePos.X, Globals.GroundY), wave: 1);
                        // Give it a slight offset so it appears on the ground
                        caveZombie.Position.Y = Globals.GroundY;
                        _enemies.Add(caveZombie);
                        // Reset spawn timer (random between 2-4 seconds)
                        _caveSpawnTimer = 2f + (float)_rng.NextDouble() * 2f;
                    }
                }
            }
            // End of foreach loop over enemies
            // Remove dead enemies after processing
            _enemies.RemoveAll(e => e.DeathAnimDone);
        }

        private void UpdateWaveSystem(float dt)
        {
            int alive = _enemies.Count(e => !e.IsDead && e.IsActive);
            if (alive == 0)
            {
                _waveCooldown -= dt;
                if (_waveCooldown <= 0f)
                {
                    _wave++;
                    SpawnWave(_wave);
                    QueueAchievement($"WAVE {_wave}!", "New enemy wave incoming!");
                    var q = _quests.Find(x => x.Title == "Wave Warrior");
                    if (q != null && !q.Complete) q.Progress++;
                }
                else if (_waveCooldown > 4f) _waveCooldown = 5f;
            }
        }

        private void UpdateQuests()
        {
            foreach (var q in _quests)
            {
                if (q.Complete) continue;
                switch (q.Title)
                {
                    case "Rich Adventurer": q.Progress = _player.Gold; break;
                    case "Level 5": q.Progress = _player.Level; break;
                }
                if (q.Progress >= q.Goal && !q.Complete)
                {
                    q.Complete = true; _player.Gold += q.RewardGold;
                    QueueAchievement("QUEST DONE!", $"{q.Title}: +{q.RewardGold} gold");
                }
            }
        }

        private void UpdateAchievements(float dt)
        {
            if (_currentAch != null)
            {
                _currentAch.ShowTimer -= dt;
                if (_currentAch.ShowTimer <= 0f) { _currentAch.Shown = true; _currentAch = null; }
            }
            if (_currentAch == null && _achQueue.Count > 0) { _currentAch = _achQueue[0]; _achQueue.RemoveAt(0); }
        }

        private void QueueAchievement(string title, string desc) { _achQueue.Add(new Achievement { Title = title, Desc = desc }); }

        private void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Position += p.Velocity * dt; p.Velocity.Y += 300f * dt;
                p.Life -= dt; p.Rotation += p.RotVel * dt;
                if (p.Life <= 0f) _particles.RemoveAt(i);
                else _particles[i] = p;
            }
        }

        private void UpdateDamageNumbers(float dt)
        {
            for (int i = _dmgNums.Count - 1; i >= 0; i--)
            {
                var n = _dmgNums[i];
                n.Position.Y -= 55f * dt; n.Life -= dt;
                if (n.Life <= 0f) _dmgNums.RemoveAt(i);
                else _dmgNums[i] = n;
            }
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];
                p.Position += p.Velocity * dt;
                p.Life -= dt;
                bool remove = p.Life <= 0f || p.Position.X < -100 || p.Position.X > Globals.WorldWidth + 100;

                if (!remove)
                {
                    for (int e = _enemies.Count - 1; e >= 0; e--)
                    {
                        var en = _enemies[e];
                        if (!en.IsActive || en.IsDead) continue;
                        if (Vector2.Distance(en.Position + new Vector2(0, -40), p.Position) < 34f)
                        {
                            bool crit = _rng.NextDouble() < _player.CritChance * 0.75f;
                            DamageEnemy(en, p.Damage * (crit ? 1.8f : 1f), crit);
                            SpawnParticle(p.Position, new Color(255, 255, 255, 180), 5f, 0.18f);
                            remove = true;
                            break;
                        }
                    }
                }

                if (remove) _projectiles.RemoveAt(i);
                else _projectiles[i] = p;
            }
        }

        private void UpdateFloatingCoins(float dt)
        {
            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                var c = _coins[i];
                c.Position += c.Velocity * dt; c.Velocity.Y += 400f * dt; c.Life -= dt;
                if (c.Life <= 0f) _coins.RemoveAt(i);
                else _coins[i] = c;
            }
        }

        private void UpdateFruits(float dt)
        {
            for (int i = _fruits.Count - 1; i >= 0; i--)
            {
                var f = _fruits[i];
                f.Spin += dt * 3f;
                if (Vector2.Distance(f.Position, _player.Position + new Vector2(0, -50)) < 30f)
                {
                    _player.Gold += f.Value;
                    _player.FruitCount += f.Value;
                    _snd.Play(_snd.CoinPickup);
                    _shopMessage = $"Collected {f.Kind}: +{f.Value} gold";
                    _shopMessageTimer = 1.8f;
                    _fruits.RemoveAt(i);
                }
                else
                {
                    _fruits[i] = f;
                }
            }
        }

        private void UpdateSlashes(float dt)
        {
            for (int i = _slashes.Count - 1; i >= 0; i--)
            {
                var s = _slashes[i];
                s.Life -= dt;
                s.Angle += (s.FacingRight ? 1f : -1f) * MathHelper.Pi * dt * 4f;
                for (int j = s.Sparks.Count - 1; j >= 0; j--)
                {
                    var sp = s.Sparks[j];
                    sp.Position += sp.Velocity * dt; sp.Life -= dt;
                    if (sp.Life <= 0f) s.Sparks.RemoveAt(j);
                    else s.Sparks[j] = sp;
                }
                if (s.Life <= 0f) _slashes.RemoveAt(i);
                else _slashes[i] = s;
            }
        }

        private void UpdateCamera(float dt)
        {
            Vector2 target = _player.Position - new Vector2(BASE_W * 0.45f, BASE_H * 0.62f);
            target.X = MathHelper.Clamp(target.X, 0, Globals.WorldWidth - BASE_W);
            target.Y = MathHelper.Clamp(target.Y, 0, Globals.WorldHeight - BASE_H);
            _cam = Vector2.Lerp(_cam, target, dt * 6f);
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                _cam += new Vector2(((float)_rng.NextDouble() * 2f - 1f) * _shakeIntensity,
                                    ((float)_rng.NextDouble() * 2f - 1f) * _shakeIntensity);
            }
        }

        private void UpdateShop(float dt)
        {
            if (_shopMessageTimer > 0f) _shopMessageTimer -= dt;
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.I) || KeyJustPressed(Keys.B))
            { State = _shopReturnState; return; }
            if (KeyJustPressed(Keys.Up)   || KeyJustPressed(Keys.W)) _shopSelected = (_shopSelected - 1 + _shopItems.Count) % _shopItems.Count;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _shopSelected = (_shopSelected + 1) % _shopItems.Count;
            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Z)) TryBuyItem(_shopSelected);
            for (int i = 0; i < _shopItems.Count; i++)
            {
                var r = ShopItemRect(i);
                if (r.Contains(MousePos())) _shopSelected = i;
                if (MouseJustReleased() && r.Contains(MousePos())) TryBuyItem(i);
            }
        }

        private void TryBuyItem(int idx)
        {
            var item = _shopItems[idx];
            if (item.Name == "VIP Gold Pack" && item.Purchased) return;
            if (_player.Gold < item.Cost)
            {
                _shopMessage = "Not enough money to buy that.";
                _shopMessageTimer = 2f;
                return;
            }
            if (item.Cost > 0)
                _player.Gold -= item.Cost;
            _snd.Play(_snd.ShopBuy);
            switch (item.Name)
            {
                case "Iron Sword":   _player.Weapon = WeaponType.Sword; _player.BaseDamage = Math.Max(_player.BaseDamage, 20f); break;
                case "Battle Axe":   _player.Weapon = WeaponType.Axe; _player.BaseDamage = Math.Max(_player.BaseDamage, 35f); _player.AttackDuration = 0.55f; break;
                case "Magic Staff":  _player.Weapon = WeaponType.MagicStaff; _player.BaseDamage = Math.Max(_player.BaseDamage, 28f); break;
                case "Hunter Pistol": _player.Weapon = WeaponType.Pistol; _player.BaseDamage = Math.Max(_player.BaseDamage, 18f); break;
                case "Health Potion": _player.HealthPotions++; break;
                case "Iron Shield":  _player.HasShield = true; _player.Defense = Math.Max(_player.Defense, 30f); break;
                case "Swift Boots":  _player.HasBoots = true; _player.MoveSpeed = Math.Max(_player.MoveSpeed, 260f); break;
                case "Bomb x3":      _player.Bombs += 3; break;
                case "Max HP Up":    _player.MaxHealth += 50f; _player.Health = Math.Min(_player.MaxHealth, _player.Health + 50f); break;
                case "Armor Upgrade": _player.ArmorLevel++; _player.Defense = Math.Max(_player.Defense, 10f + _player.ArmorLevel * 10f); _player.MaxHealth += 10f; _player.Health = Math.Min(_player.MaxHealth, _player.Health + 10f); break;
                case "VIP Gold Pack": _player.Gold += 250; item.Purchased = true; break;
            }
            _shopMessage = $"Purchased {item.Name}.";
            _shopMessageTimer = 1.8f;
        }

        private void ThrowBomb()
        {
            if (_player.Bombs <= 0) return;
            _player.Bombs--;
            float bx = _player.Position.X + (_player.FacingRight ? 250f : -250f);
            foreach (var en in _enemies)
            {
                if (en.IsDead) continue;
                if (Math.Abs(en.Position.X - bx) < 150f && Math.Abs(en.Position.Y - _player.Position.Y) < 120f)
                    DamageEnemy(en, 60f + _player.Level * 5f, false);
            }
            TriggerScreenShake(0.3f, 8f);
            for (int i = 0; i < 20; i++)
                SpawnParticle(new Vector2(bx, _player.Position.Y - 30f), new Color(255, 140, 30, 200), 8f, 0.8f);
        }

        private void UpdateFullMap() { if (KeyJustPressed(Keys.M) || KeyJustPressed(Keys.Escape)) State = GameState.Playing; }
        private void UpdatePaused()
        {
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter))
            {
                State = _pauseReturn;
                return;
            }

            if (KeyJustPressed(Keys.Up) || KeyJustPressed(Keys.W)) _settingsSelected = (_settingsSelected - 1 + 6) % 6;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _settingsSelected = (_settingsSelected + 1) % 6;

            if (KeyJustPressed(Keys.Left) || KeyJustPressed(Keys.Right) || KeyJustPressed(Keys.Space))
            {
                switch (_settingsSelected)
                {
                    case 0: ToggleFullscreen(); break;
                    case 1: ApplyWindowPreset(960, 540); break;
                    case 2: ApplyWindowPreset(1280, 720); break;
                    case 3: _rotateScreen = !_rotateScreen; break;
                    case 4: _mobileMode = !_mobileMode; break;
                    case 5: State = _pauseReturn; break;
                }
            }
            if (KeyJustPressed(Keys.F11)) ToggleFullscreen();
        }

        private void UpdateHelp()
        {
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || (MouseJustReleased() && BackButtonRect().Contains(MousePos())))
                State = GameState.Welcome;
        }

        private void UpdateCredits()
        {
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || (MouseJustReleased() && BackButtonRect().Contains(MousePos())))
                State = GameState.Welcome;
        }

        private void UpdateLevelSelect(float dt)
        {
            if (_levelSelectTimer > 0f) _levelSelectTimer -= dt;

            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Back))
            {
                State = GameState.Welcome;
                return;
            }

            if (MouseJustReleased() && BackButtonRect().Contains(MousePos()))
            {
                State = GameState.Welcome;
                return;
            }

            if (KeyJustPressed(Keys.Left)) _selectedLevel = Math.Max(1, _selectedLevel - 1);
            if (KeyJustPressed(Keys.Right)) _selectedLevel = Math.Min(MAX_STAGE, _selectedLevel + 1);
            if (KeyJustPressed(Keys.Up)) _selectedLevel = Math.Max(1, _selectedLevel - 10);
            if (KeyJustPressed(Keys.Down)) _selectedLevel = Math.Min(MAX_STAGE, _selectedLevel + 10);
            if (KeyJustPressed(Keys.M) || (MouseJustReleased() && MusicButtonRect().Contains(MousePos()))) ToggleMusic();

            var mouse = MousePos();
            for (int level = 1; level <= MAX_STAGE; level++)
            {
                if (LevelCellRect(level).Contains(mouse)) _selectedLevel = level;
            }

            bool selectPressed = KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space);
            bool selectClicked = MouseJustReleased() && LevelCellRect(_selectedLevel).Contains(mouse);
            if ((selectPressed || selectClicked) && _selectedLevel <= _highestUnlockedStage)
            {
                LoadStage(_selectedLevel);
                State = GameState.Playing;
            }
            else if ((selectPressed || selectClicked) && _selectedLevel > _highestUnlockedStage)
            {
                _levelSelectMessage = "That stage is locked. Clear earlier stages to unlock it.";
                _levelSelectTimer = 2f;
            }
        }

        private void UpdateStageClear(float dt)
        {
            _stageClearTimer += dt;
            if (_stageClearTimer < 1.5f) return;

            if (_stageIndex >= MAX_STAGE)
            {
                State = GameState.Win;
                return;
            }

            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || _stageClearTimer > 2.6f)
            {
                AdvanceStage();
                State = GameState.Playing;
            }
        }

        private void UpdateEndScreen(float dt)
        {
            _endScreenTimer += dt;
            if (!_endSoundPlayed)
            {
                _endSoundPlayed = true;
                if (State == GameState.Win)  _snd.Play(_snd.WinJingle);
                if (State == GameState.Lose) _snd.Play(_snd.LoseStinger);
            }
            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space)) RestartGame();
        }

        private void RestartGame()
        {
            _player = new Player { Position = new Vector2(300, Globals.GroundY) };
            _totalBossesDefeated = 0;
            _endScreenTimer = 0f; _endSoundPlayed = false; _cam = Vector2.Zero;
            _highestUnlockedStage = Math.Max(_highestUnlockedStage, 1);
            LoadStage(1);
            State = GameState.LevelSelect;
        }

        private void AdvanceStage()
        {
            _stageAdvancePending = false;
            if (_stageIndex >= MAX_STAGE)
            {
                State = GameState.Win;
                return;
            }

            LoadStage(_stageIndex + 1);
        }

        private void LoadStage(int stage)
        {
            _stageIndex = Math.Max(1, Math.Min(MAX_STAGE, stage));
            _player.Position = new Vector2(300, Globals.GroundY);
            _player.Health = _player.MaxHealth;
            _player.Stamina = _player.MaxStamina;
            _player.VelocityY = 0f;
            _player.OnGround = true;
            _player.IsAttacking = false;
            _player.AttackTimer = 0f;
            _player.AttackCooldown = 0f;
            _enemies.Clear();
            _particles.Clear(); _dmgNums.Clear(); _coins.Clear(); _slashes.Clear(); _projectiles.Clear(); _rainDrops.Clear();
            _wave = 1; _waveKills = 0; _bossSpawned = false; _bossDefeated = false;
            _stageAdvancePending = true;
            _stageClearTimer = 0f;
            SetupShop(); SetupQuests(); GenerateWorld(); SpawnWave(_wave);
            _levelTipText = $"Stage {_stageIndex} begins. Find the temple at the far end.";
            _levelTipTimer = 5f;
        }

        protected override void Draw(GameTime gt)
        {
            if (_rt == null) return;
            GraphicsDevice.SetRenderTarget(_rt);

            switch (State)
            {
                case GameState.Welcome:  GraphicsDevice.Clear(new Color(4, 10, 20)); DrawWelcome(gt); break;
                case GameState.LevelSelect: GraphicsDevice.Clear(new Color(8, 18, 10)); DrawLevelSelect(); break;
                case GameState.Playing:  GraphicsDevice.Clear(new Color(18, 45, 22)); DrawPlaying(gt); break;
                case GameState.Shop:     GraphicsDevice.Clear(new Color(20, 12, 5));  DrawShop();  break;
                case GameState.FullMap:  GraphicsDevice.Clear(new Color(10, 22, 10)); DrawFullMap(); break;
                case GameState.StageClear: GraphicsDevice.Clear(new Color(10, 24, 12)); DrawStageClear(); break;
                case GameState.Paused:   GraphicsDevice.Clear(new Color(10, 18, 28)); DrawPaused(); break;
                case GameState.Win:      GraphicsDevice.Clear(new Color(4, 20, 8));   DrawWin();   break;
                case GameState.Lose:     GraphicsDevice.Clear(new Color(20, 4, 4));   DrawLose();  break;
            }

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (_rotateScreen)
            {
                float bbW = GraphicsDevice.PresentationParameters.BackBufferWidth;
                float bbH = GraphicsDevice.PresentationParameters.BackBufferHeight;
                float scale = Math.Min(bbW / _rt.Width, bbH / _rt.Height);
                _sb.Draw(_rt,
                    new Vector2(bbW * 0.5f, bbH * 0.5f),
                    null,
                    Color.White,
                    MathHelper.PiOver2,
                    new Vector2(_rt.Width / 2f, _rt.Height / 2f),
                    scale,
                    SpriteEffects.None,
                    0f);
            }
            else
            {
                var destRect = _rtRect.Width > 0 ? _rtRect
                             : new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
                _sb.Draw(_rt, destRect, Color.White);
            }
            _sb.End();
            base.Draw(gt);
        }

        private void DrawWelcome(GameTime gt)
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DrawGradient(new Color(4, 8, 24), new Color(12, 30, 18), BASE_H);
            if (_showLightning) DrawRect(new Rectangle(0, 0, BASE_W, BASE_H), Color.White * _lightningAlpha * 0.35f);
            DrawMountainSilhouette(0.25f, new Color(14, 28, 20, 200));
            DrawMountainSilhouette(0.45f, new Color(18, 38, 24, 220));
            DrawMountainSilhouette(0.65f, new Color(24, 50, 28, 240));
            DrawRect(new Rectangle(0, BASE_H - 120, BASE_W, 120), new Color(10, 30, 15, 160));
            DrawCanopySilhouette();

            foreach (var p in _welcomeParticles)
            {
                float a = MathHelper.Clamp(p.Life / p.MaxLife, 0f, 1f) * (0.5f + 0.5f * (float)Math.Sin(_globalTimer * 3f + p.Position.X));
                DrawCircle(p.Position.X, p.Position.Y, p.Size, p.Color * a);
            }

            for (int k = 0; k < 3; k++)
            {
                float ang = _orbAngle + k * MathHelper.TwoPi / 3f;
                float ox = BASE_W * 0.5f + (float)Math.Cos(ang) * (160f + k * 40f);
                float oy = BASE_H * 0.38f + (float)Math.Sin(ang * 1.4f) * 25f;
                DrawCircle(ox, oy, 14f, new Color(100, 230, 120, 50));
                DrawCircle(ox, oy, 8f, new Color(160, 255, 160, 90));
            }

            int pw = 780, ph = 520;
            int px = (BASE_W - pw) / 2, py = (BASE_H - ph) / 2;
            DrawRoundedPanel(new Rectangle(px, py, pw, ph), new Color(0, 0, 0, 220));
            float glow = 0.6f + 0.4f * (float)Math.Sin(_globalTimer * 2.5f);
            DrawRect(new Rectangle(px, py, pw, 3), new Color(80, 220, 100) * glow);
            DrawRect(new Rectangle(px, py + ph - 3, pw, 3), new Color(80, 220, 100) * glow * 0.6f);
            DrawRect(new Rectangle(px, py, 3, ph), new Color(60, 180, 80, 120));
            DrawRect(new Rectangle(px + pw - 3, py, 3, ph), new Color(60, 180, 80, 120));
            DrawCircle(px + pw - 95, py + 55, 18f, new Color(90, 240, 120, 55));
            DrawCircle(px + pw - 95 + (float)Math.Cos(_orbAngle * 3f) * 14f, py + 55 + (float)Math.Sin(_orbAngle * 3f) * 14f, 7f, new Color(160, 255, 180, 180));

            if (_font != null)
            {
                string title = "JUNGLE ADVENTURE";
                float tScale = 2.4f;
                Vector2 tSize = _font.MeasureString(title) * tScale;
                float tx = (BASE_W - tSize.X) / 2f;
                float ty = _titleY;

                for (int g = 5; g >= 1; g--)
                {
                    float ga = 0.04f * glow;
                    Color gc = new Color(80, 220, 90) * ga;
                    for (int d = 0; d < 4; d++)
                    {
                        float ang2 = d * MathHelper.PiOver2 + _globalTimer * 0.5f;
                        _sb.DrawString(_font, title, new Vector2(tx + (float)Math.Cos(ang2) * g * 2, ty + (float)Math.Sin(ang2) * g * 2), gc, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                    }
                }
                _sb.DrawString(_font, title, new Vector2(tx + 4, ty + 4), Color.Black * 0.55f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, title, new Vector2(tx, ty), new Color(255, 245, 200), 0, Vector2.Zero, tScale, SpriteEffects.None, 0);

                string sub = "Explore  •  Build  •  Fight  •  Survive";
                Vector2 subSize = _font.MeasureString(sub);
                _sb.DrawString(_font, sub, new Vector2((BASE_W - subSize.X) / 2f, ty + tSize.Y + 6f), new Color(120, 200, 130) * _menuItemsAlpha);
                DrawRect(new Rectangle(BASE_W / 2 - 140, (int)(ty + tSize.Y + 32f), 280, 1), new Color(80, 160, 90, 140));
                _sb.DrawString(_font, "Jungle music: Canopy Pulse", new Vector2((BASE_W - 220) / 2f, py + ph - 138), new Color(120, 255, 180) * 0.8f * _menuItemsAlpha, 0, Vector2.Zero, 0.76f, SpriteEffects.None, 0);

                string[] menuLabels = { "  START GAME", "  SHOP", "  SETTINGS", "  HELP", "  CREDITS", "  EXIT" };
                for (int mi = 0; mi < menuLabels.Length; mi++)
                {
                    var r = WelcomeBtn(mi);
                    bool hov = mi == _welcomeMenuSel || r.Contains(MousePos());
                    bool pressed = hov && _mCurr.LeftButton == ButtonState.Pressed;
                    Color btnBase = pressed ? new Color(80, 170, 90) : hov ? new Color(30, 100, 40) : new Color(10, 36, 18);
                    Color btnAccent = mi == 0 ? new Color(80, 255, 120) : new Color(140, 200, 255);
                    float alphaMult = _menuItemsAlpha;

                    if (hov)
                    {
                        float p2 = 0.4f + 0.3f * (float)Math.Sin(_globalTimer * 4f);
                        DrawRect(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), btnAccent * p2 * 0.3f * alphaMult);
                    }
                    DrawRect(r, btnBase * alphaMult);
                    DrawRect(new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2),
                             Color.Lerp(btnBase, btnAccent, hov ? 0.35f : 0.08f) * alphaMult);
                    DrawRect(new Rectangle(r.X, r.Y, r.Width, 2), btnAccent * (hov ? 0.9f : 0.4f) * alphaMult);

                    if (mi == _welcomeMenuSel)
                        _sb.DrawString(_font, ">", new Vector2(r.X + 12, r.Y + 11), btnAccent * alphaMult);

                    Vector2 ls = _font.MeasureString(menuLabels[mi]);
                    _sb.DrawString(_font, menuLabels[mi], new Vector2(r.X + (r.Width - ls.X) / 2f, r.Y + 12), Color.White * alphaMult);
                }

                float chipY = py + ph - 80f;
                string[] chips = { "WASD/Arrows:Move", "SPACE:Attack", "Z:Alt Attack", "I/B:Shop", "M:Map", "Q:Bomb", "H:Potion", "ESC:Settings" };
                float chipX = px + 14f;
                foreach (var chip in chips)
                {
                    Vector2 cs = _font.MeasureString(chip) * 0.75f;
                    DrawRect(new Rectangle((int)chipX - 4, (int)chipY - 2, (int)cs.X + 8, (int)cs.Y + 4), new Color(0, 0, 0, 100));
                    _sb.DrawString(_font, chip, new Vector2(chipX, chipY), Color.White * 0.75f * _menuItemsAlpha, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
                    chipX += cs.X + 20f;
                }

                string hint = "Shop, help, and credits are now on the welcome screen.";
                Vector2 hs = _font.MeasureString(hint) * 0.78f;
                _sb.DrawString(_font, hint, new Vector2((BASE_W - hs.X) / 2f, py + ph - 112), Color.LightGreen * 0.85f, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);

                if (_blinkOn && _menuItemsAlpha > 0.8f)
                {
                    string pe = "PRESS ENTER TO BEGIN YOUR ADVENTURE";
                    Vector2 pes = _font.MeasureString(pe) * 0.9f;
                    _sb.DrawString(_font, pe, new Vector2((BASE_W - pes.X) / 2f, BASE_H - 38f), Color.Yellow * 0.9f, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
                }
                var musicR = MusicButtonRect();
                DrawRect(musicR, _musicEnabled ? new Color(20, 100, 55, 220) : new Color(120, 40, 40, 220));
                DrawRect(new Rectangle(musicR.X, musicR.Y, musicR.Width, 2), new Color(255, 255, 255, 120));
                _sb.DrawString(_font, _musicEnabled ? "MUSIC: ON" : "MUSIC: OFF", new Vector2(musicR.X + 16, musicR.Y + 10), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
                _sb.DrawString(_font, "v2.0  GOLD EDITION", new Vector2(BASE_W - 200, BASE_H - 22), Color.DimGray * 0.6f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            }
            _sb.End();
        }

        private void DrawLevelSelect()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }

            DrawGradient(new Color(6, 18, 10), new Color(18, 44, 20), BASE_H);
            for (int i = 0; i < 28; i++)
            {
                float x = 80 + (i * 43) % 1120;
                float y = 90 + (i * 23) % 520 + (float)Math.Sin(_globalTimer * 1.4f + i) * 8f;
                DrawCircle(x, y, 3f + (i % 3), new Color(120, 255, 150, 40));
            }

            int pw = 1120, ph = 560;
            int ppx = (BASE_W - pw) / 2, ppy = 84;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(120, 255, 150, 200));

            string title = "SELECT A STAGE";
            Vector2 ts = _font.MeasureString(title) * 1.8f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, 18), Color.White, 0, Vector2.Zero, 1.8f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Unlocked: {_highestUnlockedStage} / {MAX_STAGE}", new Vector2(ppx + 24, ppy + 16), Color.LightGreen, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "Choose a level. Cleared stages unlock the next one.", new Vector2(ppx + 24, ppy + 44), Color.White * 0.88f, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0);

            var musicR = MusicButtonRect();
            DrawRect(musicR, _musicEnabled ? new Color(20, 100, 55, 220) : new Color(120, 40, 40, 220));
            DrawRect(new Rectangle(musicR.X, musicR.Y, musicR.Width, 2), new Color(255, 255, 255, 120));
            _sb.DrawString(_font, _musicEnabled ? "MUSIC: ON" : "MUSIC: OFF", new Vector2(musicR.X + 16, musicR.Y + 10), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);

            int gridX = ppx + 26;
            int gridY = ppy + 88;
            int cellW = 104;
            int cellH = 42;
            int gap = 6;
            var mouse = MousePos();
            for (int level = 1; level <= MAX_STAGE; level++)
            {
                int idx = level - 1;
                int col = idx % 10;
                int row = idx / 10;
                Rectangle cell = new Rectangle(gridX + col * (cellW + gap), gridY + row * (cellH + gap), cellW, cellH);
                bool unlocked = level <= _highestUnlockedStage;
                bool selected = level == _selectedLevel;
                bool hovered = cell.Contains(mouse);
                Color fill = unlocked ? new Color(30, 80, 40, 230) : new Color(40, 40, 52, 220);
                if (selected) fill = unlocked ? new Color(60, 140, 70, 240) : new Color(70, 70, 90, 240);
                if (hovered) fill = Color.Lerp(fill, new Color(120, 220, 120, 255), 0.2f);
                DrawRect(cell, fill);
                DrawRect(new Rectangle(cell.X, cell.Y, cell.Width, 2), unlocked ? new Color(140, 255, 160, 160) : new Color(180, 180, 200, 80));
                string label = level.ToString();
                if (!unlocked)
                {
                    string lsMain = "X";
                    Vector2 lsz = _font.MeasureString(lsMain);
                    _sb.DrawString(_font, lsMain, new Vector2(cell.X + (cellW - lsz.X) / 2f, cell.Y + 10), Color.White * 0.5f, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0);
                    // Draw lock icon
                    int lockCX = cell.X + cellW / 2;
                    int lockY = cell.Y + 10;
                    DrawRect(new Rectangle(lockCX - 7, lockY + 6, 14, 12), new Color(180, 180, 200, 100));
                    DrawRect(new Rectangle(lockCX - 5, lockY + 2, 10, 6), new Color(200, 200, 220, 100));
                    DrawRect(new Rectangle(lockCX - 2, lockY + 8, 4, 4), Color.Black * 0.3f);
                }
                else
                {
                    Vector2 ls = _font.MeasureString(label);
                    _sb.DrawString(_font, label, new Vector2(cell.X + (cellW - ls.X) / 2f, cell.Y + 10), Color.White, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0);
                }
                if (selected && unlocked)
                    _sb.DrawString(_font, ">", new Vector2(cell.X + 10, cell.Y + 10), Color.Yellow, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            }

            string message = _levelSelectTimer > 0f ? _levelSelectMessage : "";
            if (!string.IsNullOrWhiteSpace(message))
                _sb.DrawString(_font, message, new Vector2(ppx + 24, ppy + ph - 56), Color.Yellow * MathHelper.Clamp(_levelSelectTimer / 2f, 0f, 1f), 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

            var backR = BackButtonRect();
            DrawRect(backR, new Color(20, 60, 40, 220));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(140, 255, 180, 180));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 18, backR.Y + 12), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawMountainSilhouette(float yCutoff, Color col)
        {
            int baseY = (int)(BASE_H * yCutoff);
            int seed = (int)(yCutoff * 1000);
            var rr = new Random(seed);
            float x = -100;
            while (x < BASE_W + 100)
            {
                int w = rr.Next(120, 280);
                int h = rr.Next(60, 180);
                DrawRect(new Rectangle((int)x - w / 2, baseY - h, w, h + 200), col);
                x += w * 0.65f + rr.Next(20, 80);
            }
        }

        private void DrawCanopySilhouette()
        {
            var rr = new Random(999);
            int by = BASE_H - 80;
            for (int i = 0; i < 24; i++)
            {
                int tx = i * 70 - 30;
                int th = rr.Next(80, 190);
                int tw = rr.Next(55, 110);
                DrawRect(new Rectangle(tx, by - th, tw, th + 80), new Color(8, 28, 10, 240));
                DrawRect(new Rectangle(tx - 15, by - th - 35, tw + 30, 55), new Color(12, 40, 14, 230));
            }
        }

        private void DrawRoundedPanel(Rectangle r, Color c)
        {
            DrawRect(r, c);
            DrawRect(new Rectangle(r.X - 1, r.Y + 4, r.Width + 2, r.Height - 8), c);
            DrawRect(new Rectangle(r.X + 4, r.Y - 1, r.Width - 8, r.Height + 2), c);
        }

        private void DrawPlaying(GameTime gt)
        {
            Color skyTop = DayNightSky(true);
            Color skyBottom = DayNightSky(false);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DrawGradient(skyTop, skyBottom, BASE_H);
            _sb.End();

            var camMatrix = Matrix.CreateTranslation(-_cam.X, -_cam.Y, 0f);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camMatrix);
            DrawWorldBackground();
            DrawPlatforms();
            DrawEnvironment();
            DrawEnemies();
            DrawProjectiles();
            DrawPlayer();
            DrawSlashes();
            DrawParticles();
            DrawRain();
            DrawDamageNumbers();
            DrawFloatingCoins();
            DrawFruits();
            _sb.End();

            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DrawHUD();
            DrawMinimap();
            DrawWaveInfo();
            DrawQuestPanel();
            DrawAchievementPopup();
            DrawComboDisplay();
            DrawLevelUpBanner();
            DrawLevelTipBanner();
            DrawWeaponIndicator();
            DrawMobileControls();
            _sb.End();
        }

        private void DrawMobileControls()
        {
            if (!_mobileMode) return;
            DrawMobileButton(MobileLeftRect(), "L");
            DrawMobileButton(MobileRightRect(), "R");
            DrawMobileButton(MobileJumpRect(), "JUMP");
            DrawMobileButton(MobileAttackRect(), "HIT");
        }

        private void DrawMobileButton(Rectangle r, string label)
        {
            Color fill = label == "HIT" ? new Color(180, 80, 80, 140) : new Color(40, 40, 60, 130);
            DrawRect(r, fill);
            DrawRect(new Rectangle(r.X, r.Y, r.Width, 2), new Color(255, 255, 255, 100));
            if (_font != null)
                _sb.DrawString(_font, label, new Vector2(r.X + 10, r.Y + 10), Color.White * 0.9f, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
        }

        private Rectangle MobileLeftRect() => new Rectangle(24, BASE_H - 120, 70, 70);
        private Rectangle MobileRightRect() => new Rectangle(104, BASE_H - 120, 70, 70);
        private Rectangle MobileJumpRect() => new Rectangle(BASE_W - 210, BASE_H - 120, 90, 70);
        private Rectangle MobileAttackRect() => new Rectangle(BASE_W - 110, BASE_H - 120, 90, 70);

        private Color DayNightSky(bool top)
        {
            float t = _dayTime;
            if (top)
            {
                if (t < 0.25f)      return Color.Lerp(new Color(100, 60, 30), new Color(30, 100, 180), t / 0.25f);
                else if (t < 0.5f)  return Color.Lerp(new Color(30, 100, 180), new Color(180, 80, 40), (t - 0.25f) / 0.25f);
                else if (t < 0.75f) return Color.Lerp(new Color(180, 80, 40), new Color(5, 5, 30), (t - 0.5f) / 0.25f);
                else                return Color.Lerp(new Color(5, 5, 30), new Color(100, 60, 30), (t - 0.75f) / 0.25f);
            }
            else
            {
                if (t < 0.25f)      return Color.Lerp(new Color(200, 130, 60), new Color(100, 180, 80), t / 0.25f);
                else if (t < 0.5f)  return Color.Lerp(new Color(100, 180, 80), new Color(220, 120, 50), (t - 0.25f) / 0.25f);
                else if (t < 0.75f) return Color.Lerp(new Color(220, 120, 50), new Color(20, 20, 50), (t - 0.5f) / 0.25f);
                else                return Color.Lerp(new Color(20, 20, 50), new Color(200, 130, 60), (t - 0.75f) / 0.25f);
            }
        }

        private void DrawWorldBackground()
        {
            Color ground = _stageTheme switch
            {
                1 => new Color(42, 64, 28),
                2 => new Color(24, 58, 50),
                3 => new Color(56, 44, 58),
                4 => new Color(72, 50, 28),
                5 => new Color(32, 34, 52),
                _ => new Color(30, 68, 32)
            };
            Color top = _stageTheme switch
            {
                1 => new Color(90, 150, 70),
                2 => new Color(70, 140, 130),
                3 => new Color(150, 110, 160),
                4 => new Color(175, 122, 72),
                5 => new Color(90, 96, 150),
                _ => new Color(60, 120, 50)
            };
            Color dirt = _stageTheme switch
            {
                1 => new Color(72, 48, 24),
                2 => new Color(34, 52, 44),
                3 => new Color(64, 34, 44),
                4 => new Color(96, 62, 34),
                5 => new Color(44, 42, 70),
                _ => new Color(80, 52, 28)
            };

            DrawRect(new Rectangle(-200, (int)Globals.GroundY, (int)Globals.WorldWidth + 400, 300), ground);
            DrawRect(new Rectangle(-200, (int)Globals.GroundY, (int)Globals.WorldWidth + 400, 12), top);
            DrawRect(new Rectangle(-200, (int)Globals.GroundY + 12, (int)Globals.WorldWidth + 400, 288), dirt);

            if (_stageTheme >= 2)
            {
                for (int i = 0; i < 12; i++)
                    DrawCircle(120 + i * 104, 120 + (float)Math.Sin(_globalTimer * 0.35f + i) * 12f, 12f, new Color(20, 30, 50, 25));
            }
            if (_stageTheme >= 4)
            {
                DrawRect(new Rectangle(80, 210, 120, 58), new Color(90, 70, 40, 120));
                DrawRect(new Rectangle(115, 170, 50, 44), new Color(110, 80, 48, 120));
            }
        }

        private void DrawPlatforms()
        {
            foreach (var p in _platRects)
            {
                DrawRect(p, new Color(55, 36, 18));
                DrawRect(new Rectangle(p.X, p.Y, p.Width, 6), new Color(70, 130, 55));
                DrawRect(new Rectangle(p.X + 4, p.Y + 8, p.Width - 8, p.Height - 10), new Color(45, 28, 12));
            }
        }

        private void DrawEnvironment()
        {
            foreach (var g in _grasses)
            {
                for (int b = 0; b < 4; b++)
                {
                    float bx = g.X + (b - 1.5f) * 5f;
                    float wave = (float)Math.Sin(_globalTimer * 1.8f + g.X * 0.05f + b) * 3f;
                    DrawRect(new Rectangle((int)bx, (int)g.Y - 14, 2, 14), new Color(60, 160, 50));
                    DrawRect(new Rectangle((int)(bx + wave * 0.5f), (int)g.Y - 24, 2, 12), new Color(80, 190, 60));
                }
            }
            foreach (var r in _rocks)
            {
                DrawRect(new Rectangle((int)r.X - 12, (int)r.Y - 10, 24, 14), new Color(100, 95, 85));
                DrawRect(new Rectangle((int)r.X - 10, (int)r.Y - 12, 20, 8), new Color(130, 125, 110));
            }
            for (int i = 0; i < _trees.Count; i++) DrawTree(_trees[i], _treeSizes[i]);
            if (_stageIndex >= 15)
                DrawVillageHouse((int)(Globals.WorldWidth * 0.28f), (int)Globals.GroundY, _stageTheme >= 2);
            if (_stageIndex >= 35)
                DrawMonsterNest((int)(Globals.WorldWidth * 0.68f), (int)Globals.GroundY);
            DrawTemple((int)Globals.WorldWidth - 340, (int)Globals.GroundY);
        }

        private void DrawVillageHouse(int x, int groundY, bool swampStyle)
        {
            int baseY = groundY;
            Color wall = swampStyle ? new Color(64, 82, 72) : new Color(140, 110, 70);
            Color roof = swampStyle ? new Color(34, 54, 40) : new Color(90, 48, 28);
            DrawRect(new Rectangle(x - 84, baseY - 94, 168, 74), wall);
            DrawRect(new Rectangle(x - 94, baseY - 112, 188, 22), roof);
            DrawRect(new Rectangle(x - 18, baseY - 66, 36, 46), new Color(30, 24, 18));
            DrawRect(new Rectangle(x - 58, baseY - 78, 24, 18), new Color(220, 240, 255, 120));
            DrawRect(new Rectangle(x + 34, baseY - 78, 24, 18), new Color(220, 240, 255, 120));
        }

        private void DrawMonsterNest(int x, int groundY)
        {
            int baseY = groundY;
            DrawRect(new Rectangle(x - 98, baseY - 72, 196, 54), new Color(70, 36, 30));
            DrawRect(new Rectangle(x - 118, baseY - 98, 236, 30), new Color(120, 44, 44));
            DrawRect(new Rectangle(x - 34, baseY - 124, 68, 34), new Color(40, 18, 18));
            DrawRect(new Rectangle(x - 12, baseY - 118, 24, 12), new Color(255, 70, 30));
            DrawRect(new Rectangle(x - 18, baseY - 112, 12, 12), Color.Black);
            DrawRect(new Rectangle(x + 6, baseY - 112, 12, 12), Color.Black);
        }

        private void DrawTemple(int x, int groundY)
        {
            int baseY = groundY;
            DrawRect(new Rectangle(x - 120, baseY - 24, 260, 24), new Color(80, 64, 40));
            DrawRect(new Rectangle(x - 80, baseY - 140, 180, 120), new Color(66, 72, 92));
            DrawRect(new Rectangle(x - 100, baseY - 164, 220, 30), new Color(110, 120, 150));
            DrawRect(new Rectangle(x - 60, baseY - 210, 140, 50), new Color(160, 180, 210));
            DrawRect(new Rectangle(x - 42, baseY - 192, 104, 32), new Color(255, 230, 120, 180));
            DrawRect(new Rectangle(x - 18, baseY - 150, 48, 78), new Color(20, 20, 30));
            DrawRect(new Rectangle(x - 132, baseY - 100, 24, 76), new Color(100, 110, 135));
            DrawRect(new Rectangle(x + 108, baseY - 100, 24, 76), new Color(100, 110, 135));
            DrawRect(new Rectangle(x - 150, baseY - 120, 300, 12), new Color(200, 200, 220));
        }

        private void DrawTree(Vector2 pos, float scale)
        {
            int tw = (int)(20 * scale), th = (int)(140 * scale);
            int cw = (int)(90 * scale), ch = (int)(80 * scale);
            int cx = (int)pos.X - cw / 2, ty = (int)pos.Y - th;
            float sw = (float)Math.Sin(_globalTimer * 0.7f + pos.X * 0.01f) * 2.5f * scale;
            DrawRect(new Rectangle((int)pos.X - tw / 2, ty, tw, th), new Color(80, 52, 28));
            DrawRect(new Rectangle((int)pos.X - tw / 4, ty, tw / 2, th), new Color(95, 62, 33));
            DrawRect(new Rectangle(cx + (int)sw, ty - (int)(35 * scale), cw, ch + 20), new Color(28, 90, 32, 230));
            DrawRect(new Rectangle(cx + 10 + (int)(sw * 0.7f), ty - (int)(65 * scale), cw - 20, ch - 10), new Color(40, 120, 44, 220));
            DrawRect(new Rectangle(cx + 20 + (int)(sw * 0.4f), ty - (int)(88 * scale), cw - 40, 44), new Color(50, 140, 50, 200));
            DrawRect(new Rectangle(cx + 18 + (int)sw, ty - (int)(60 * scale), 22, 20), new Color(80, 200, 70, 80));
        }

        private void DrawEnemies()
        {
            foreach (var en in _enemies)
            {
                if (!en.IsActive && !en.IsDead) continue;
                DrawEnemy(en);
            }
        }

        private void DrawEnemy(Enemy en)
        {
            float hitMult = en.HitFlash > 0f ? 1f : 0f;
            Color baseCol = GetEnemyColor(en.Type);
            Color drawCol = Color.Lerp(baseCol, Color.White, hitMult * 0.85f);
            float walk = (float)Math.Sin(en.WalkAnim) * 4f;
            int ex = (int)en.Position.X, ey = (int)en.Position.Y, dir = en.FacingRight ? 1 : -1;

            if (en.Type == EnemyType.Boss) { DrawBoss(en, ex, ey, drawCol, hitMult); return; }

            if (en.IsDead)
            {
                float deadT = MathHelper.Clamp(en.DeathTimer / 1.2f, 0f, 1f);
                DrawRect(new Rectangle(ex - 14, ey - 18 + (int)(deadT * 22f), 28, 12), drawCol * (1f - deadT));
                DrawRect(new Rectangle(ex - 12, ey - 8 + (int)(deadT * 22f), 24, 6), new Color(20, 20, 20) * (1f - deadT));
                return;
            }

            int bw = (int)((en.Type == EnemyType.Skeleton ? 16f : 20f) * en.WidthScale);
            int bh = (int)((en.Type == EnemyType.Goblin ? 24f : 34f) * en.HeightScale);
            if (en.Type == EnemyType.Shielded)
            {
                DrawRect(new Rectangle(ex - bw / 2 - 6, ey - bh - 24, bw + 12, bh + 14), new Color(80, 170, 150, 70));
                DrawRect(new Rectangle(ex - bw / 2 - 10, ey - bh - 16, 10, bh - 2), new Color(200, 220, 255));
            }
            DrawRect(new Rectangle(ex - bw / 2, ey - bh - 20, bw, bh), drawCol);
            int hw = bw + 4;
            DrawRect(new Rectangle(ex - hw / 2, ey - bh - 40, hw, 22), drawCol);
            DrawRect(new Rectangle(ex - hw / 2 + 4, ey - bh - 40, hw - 8, 10), drawCol * 0.85f);
            if (en.Variant == 1)
                DrawRect(new Rectangle(ex - bw / 2, ey - bh - 16, bw, 12), drawCol * 0.9f);
            if (en.Variant == 2)
                DrawRect(new Rectangle(ex - bw / 2 - 4, ey - bh - 22, bw + 8, 18), drawCol * 0.92f);
            DrawRect(new Rectangle(ex + dir * 10, ey - bh - 16 + (int)(walk * 0.8f), 5, 16), drawCol * 0.9f);
            DrawRect(new Rectangle(ex - dir * 16, ey - bh - 14 - (int)(walk * 0.8f), 5, 16), drawCol * 0.9f);
            int eyeOff = dir > 0 ? 3 : -3;
            DrawRect(new Rectangle(ex + eyeOff - 3, ey - bh - 34, 5, 5), en.Type == EnemyType.Skeleton ? Color.Cyan : Color.Red);
            DrawRect(new Rectangle(ex + eyeOff + 4, ey - bh - 34, 5, 5), en.Type == EnemyType.Skeleton ? Color.Cyan : Color.Red);
            DrawRect(new Rectangle(ex - 8, ey - 20, 6, 20 + (int)walk), drawCol * 0.8f);
            DrawRect(new Rectangle(ex + 2, ey - 20, 6, 20 - (int)walk), drawCol * 0.8f);
            DrawRect(new Rectangle(ex - bw / 2 - 5, ey - bh - 14 + (int)walk, 5, 18), drawCol * 0.85f);
            DrawRect(new Rectangle(ex + bw / 2, ey - bh - 14 - (int)walk, 5, 18), drawCol * 0.85f);
            if (en.Type == EnemyType.Skeleton)
                DrawRect(new Rectangle(ex + dir * 14, ey - bh - 10, 3, 24), new Color(220, 220, 220));

            float hpRatio = en.Health / en.MaxHealth;
            int barW = 44, barX = ex - barW / 2, barY = ey - bh - 55;
            DrawRect(new Rectangle(barX - 1, barY - 1, barW + 2, 9), Color.Black * 0.7f);
            DrawRect(new Rectangle(barX, barY, barW, 7), new Color(60, 10, 10));
            DrawRect(new Rectangle(barX, barY, (int)(barW * hpRatio), 7), Color.Lerp(Color.Red, Color.LimeGreen, hpRatio));
        }

        private void DrawBoss(Enemy en, int ex, int ey, Color drawCol, float hitMult)
        {
            float pulse = 1f + 0.06f * (float)Math.Sin(_globalTimer * 4f);
            float rage = 1f - en.Health / en.MaxHealth;
            Color rageGlow = new Color((int)(255 * rage), 30, 30, 160);
            if (rage > 0.3f) DrawCircle(ex, ey - 60, 55f * pulse, rageGlow * 0.4f);

            DrawRect(new Rectangle(ex - 32, ey - 90, 64, 70), drawCol);
            DrawRect(new Rectangle(ex - 44, ey - 80, 16, 40), drawCol * 0.9f);
            DrawRect(new Rectangle(ex + 28, ey - 80, 16, 40), drawCol * 0.9f);
            DrawRect(new Rectangle(ex - 26, ey - 120, 52, 36), drawCol);
            DrawRect(new Rectangle(ex - 16, ey - 130, 32, 16), drawCol * 0.85f);
            DrawRect(new Rectangle(ex - 28, ey - 136, 8, 20), new Color(200, 50, 50));
            DrawRect(new Rectangle(ex + 20, ey - 136, 8, 20), new Color(200, 50, 50));
            DrawRect(new Rectangle(ex - 8, ey - 140, 16, 24), new Color(220, 60, 60));
            DrawRect(new Rectangle(ex - 14, ey - 112, 10, 10), new Color(255, 80, 0));
            DrawRect(new Rectangle(ex + 4, ey - 112, 10, 10), new Color(255, 80, 0));
            DrawRect(new Rectangle(ex - 11, ey - 109, 6, 6), Color.White);
            DrawRect(new Rectangle(ex + 7, ey - 109, 6, 6), Color.White);
            DrawRect(new Rectangle(ex + 40, ey - 88, 8, 50), new Color(160, 100, 40));
            DrawRect(new Rectangle(ex + 34, ey - 94, 22, 10), new Color(200, 200, 200));
            DrawRect(new Rectangle(ex + 36, ey - 104, 8, 14), new Color(255, 160, 0));

            float hpR = en.Health / en.MaxHealth;
            int bW = 120, bX = ex - 60, bY = ey - 160;
            DrawRect(new Rectangle(bX - 2, bY - 2, bW + 4, 14), Color.Black * 0.85f);
            DrawRect(new Rectangle(bX, bY, bW, 10), new Color(60, 8, 8));
            Color hpCol = rage > 0.5f ? new Color(255, 60, 0) : Color.Lerp(Color.Red, Color.OrangeRed, rage);
            DrawRect(new Rectangle(bX, bY, (int)(bW * hpR), 10), hpCol);
            if (_font != null)
                _sb.DrawString(_font, $"BOSS  {en.Health:F0}/{en.MaxHealth:F0}", new Vector2(bX - 10, bY - 18), Color.OrangeRed * 0.9f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
        }

        private Color GetEnemyColor(EnemyType t)
        {
            return t switch
            {
                EnemyType.Zombie   => new Color(70, 120, 60),
                EnemyType.Goblin   => new Color(80, 150, 40),
                EnemyType.Skeleton => new Color(220, 215, 190),
                EnemyType.Shielded => new Color(110, 190, 160),
                EnemyType.Boss     => new Color(140, 30, 20),
                _                  => Color.Gray
            };
        }

        private void DrawPlayer()
        {
            int px = (int)_player.Position.X, py = (int)_player.Position.Y, dir = _player.FacingRight ? 1 : -1;
            float alpha = _player.IsInvincible ? (0.5f + 0.5f * (float)Math.Sin(_globalTimer * 20f)) : 1f;
            Color skinCol = new Color(255, 210, 170), shirt = new Color(40, 80, 200), pants = new Color(30, 50, 120), hair = new Color(80, 40, 10);
            float walk = _player.OnGround ? (float)Math.Sin(_player.WalkAnim) * 5f : 0f;

            if (_player.IsDead)
            {
                float deadT = MathHelper.Clamp(_player.DeathTimer / 1.2f, 0f, 1f);
                DrawRect(new Rectangle(px - 18, py - 10 + (int)(deadT * 18f), 36, 12), skinCol * (1f - deadT));
                DrawRect(new Rectangle(px - 18, py - 20 + (int)(deadT * 20f), 36, 6), new Color(120, 70, 40) * (1f - deadT));
                return;
            }

            DrawRect(new Rectangle(px - 10, py - 28, 8, 28 + (int)walk), pants * alpha);
            DrawRect(new Rectangle(px + 2, py - 28, 8, 28 - (int)walk), pants * alpha);
            DrawRect(new Rectangle(px - 12, py - 8, 10, 10), new Color(80, 50, 20) * alpha);
            DrawRect(new Rectangle(px + 2, py - 8, 10, 10), new Color(80, 50, 20) * alpha);
            DrawRect(new Rectangle(px - 14, py - 64, 28, 36), shirt * alpha);

            if (_player.HasShield)
            {
                int sdx = dir < 0 ? px + 12 : px - 22;
                DrawRect(new Rectangle(sdx, py - 60, 10, 30), new Color(150, 150, 180) * alpha);
                DrawRect(new Rectangle(sdx + 1, py - 58, 8, 10), new Color(200, 200, 240) * alpha);
            }

            DrawRect(new Rectangle(px - 12, py - 90, 24, 26), skinCol * alpha);
            DrawRect(new Rectangle(px - 12, py - 94, 24, 10), hair * alpha);
            DrawRect(new Rectangle(px - 14, py - 90, 4, 14), hair * alpha);
            DrawRect(new Rectangle(px + dir * 3 - 3, py - 80, 5, 5), Color.White * alpha);
            DrawRect(new Rectangle(px + dir * 3 - 1, py - 78, 3, 3), Color.Black * alpha);
            DrawCircularHealthRing(px, py - 74, 28f, _player.Health, _player.MaxHealth, new Color(255, 60, 60), new Color(60, 20, 20));
            float armSwing = _player.IsAttacking ? 1f - (_player.AttackTimer / Math.Max(_player.AttackDuration, 0.01f)) : 0f;
            DrawRect(new Rectangle(px + dir * 8, py - 66 + (int)(armSwing * 4f), 6, 18), skinCol * alpha);

            float atkProg = _player.IsAttacking ? _player.AttackTimer / _player.AttackDuration : 0f;
            DrawWeapon(px, py, dir, atkProg, alpha);
            DrawRect(new Rectangle(px - dir * 14, py - 64, 6, 22), skinCol * alpha);
        }

        private void DrawWeapon(int px, int py, int dir, float atkProg, float alpha)
        {
            float swingAngle = _player.IsAttacking
                ? MathHelper.Lerp(-MathHelper.PiOver2 * dir, MathHelper.PiOver2 * dir, 1f - atkProg)
                : -MathHelper.PiOver4 * dir * 0.4f;

            Color wCol = GetWeaponColor(_player.Weapon);
            Color skinCol = new Color(255, 210, 170);

            float ax = px + dir * 12 + (float)Math.Cos(swingAngle) * 16f;
            float ay = py - 56 + (float)Math.Sin(swingAngle) * 16f;
            DrawLine(new Vector2(px + dir * 12, py - 56), new Vector2(ax, ay), skinCol * alpha, 5);

            switch (_player.Weapon)
            {
                case WeaponType.Fists:
                    DrawRect(new Rectangle((int)ax - 4, (int)ay - 4, 8, 8), skinCol * alpha);
                    break;
                case WeaponType.Sword:
                    for (int seg = 0; seg < 5; seg++)
                    {
                        float bx = ax + dir * (float)Math.Cos(swingAngle + MathHelper.PiOver4 * dir) * seg * 9f;
                        float by = ay + (float)Math.Sin(swingAngle + MathHelper.PiOver4 * dir) * seg * 9f;
                        DrawRect(new Rectangle((int)bx, (int)by, 7 - seg, 7 - seg), wCol * alpha);
                    }
                    DrawRect(new Rectangle((int)ax - 5, (int)ay - 2, 10, 4), new Color(200, 180, 80) * alpha);
                    if (_player.IsAttacking) DrawCircle(ax + dir * 20, ay - 10, 14f, wCol * 0.35f * alpha);
                    break;
                case WeaponType.Axe:
                    for (int seg = 0; seg < 4; seg++)
                    {
                        float bx = ax + dir * seg * 8f;
                        float by = ay - seg * 4f;
                        DrawRect(new Rectangle((int)bx, (int)by, 5, 5), new Color(120, 80, 40) * alpha);
                    }
                    DrawRect(new Rectangle((int)(ax + dir * 26), (int)ay - 18, 18, 24), wCol * alpha);
                    DrawRect(new Rectangle((int)(ax + dir * 30), (int)ay - 24, 10, 8), wCol * alpha);
                    if (_player.IsAttacking) DrawCircle(ax + dir * 32, ay - 18, 18f, wCol * 0.4f * alpha);
                    break;
                case WeaponType.MagicStaff:
                    for (int seg = 0; seg < 6; seg++)
                    {
                        float bx = ax + dir * seg * 9f;
                        float by = ay - seg * 5f;
                        DrawRect(new Rectangle((int)bx, (int)by, 4, 4), new Color(120, 80, 200) * alpha);
                    }
                    float tipX = ax + dir * 54f, tipY = ay - 30f;
                    DrawCircle(tipX, tipY, 10f * (0.7f + 0.3f * (float)Math.Sin(_globalTimer * 6f)), wCol * 0.8f * alpha);
                    DrawCircle(tipX, tipY, 6f * (0.7f + 0.3f * (float)Math.Sin(_globalTimer * 6f)), Color.White * 0.6f * alpha);
                    if (_player.IsAttacking)
                        for (int sp = 0; sp < 5; sp++)
                            DrawCircle(tipX + (float)Math.Cos(_globalTimer * 5f + sp * MathHelper.TwoPi / 5f) * 20f,
                                       tipY + (float)Math.Sin(_globalTimer * 5f + sp * MathHelper.TwoPi / 5f) * 20f, 4f, wCol * 0.6f * alpha);
                    break;
                case WeaponType.Pistol:
                    DrawRect(new Rectangle((int)ax - 10, (int)ay - 4, 22, 8), wCol * alpha);
                    DrawRect(new Rectangle((int)ax + dir * 9, (int)ay - 6, 6, 4), new Color(90, 90, 90) * alpha);
                    DrawRect(new Rectangle((int)ax - 3, (int)ay - 10, 8, 16), new Color(40, 40, 40) * alpha);
                    if (_player.IsAttacking) DrawCircle(ax + dir * 24, ay - 2, 8f, Color.Yellow * 0.45f * alpha);
                    break;
            }
        }

        private void DrawProjectiles()
        {
            foreach (var p in _projectiles)
            {
                float a = MathHelper.Clamp(p.Life / 2f, 0f, 1f);
                DrawCircle(p.Position.X, p.Position.Y, 4f, p.Color * a);
                DrawLine(p.Position - new Vector2(p.Velocity.X > 0 ? 10f : -10f, 0f), p.Position, p.Color * 0.8f * a, 2);
            }
        }

        private void DrawSlashes()
        {
            foreach (var slash in _slashes)
            {
                float lifeR = slash.Life / slash.MaxLife;
                for (int i = 0; i < 18; i++)
                {
                    float t = (float)i / 18;
                    float ang = slash.FacingRight ? slash.Angle + slash.ArcSpan * t : slash.Angle - slash.ArcSpan * t;
                    Vector2 inner = slash.Origin + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * slash.Radius * 0.3f;
                    Vector2 outer = slash.Origin + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * slash.Radius * (0.7f + t * 0.3f);
                    float alpha = lifeR * (1f - t * 0.5f);
                    DrawLineThick(inner, outer, GetWeaponColor(_player.Weapon) * alpha, (int)(4 * lifeR));
                }
                DrawCircle(slash.Origin.X, slash.Origin.Y, slash.Radius * 0.5f * lifeR, GetWeaponColor(_player.Weapon) * 0.18f * lifeR);
                foreach (var sp in slash.Sparks)
                {
                    float sa = sp.Life / sp.MaxLife;
                    DrawCircle(sp.Position.X, sp.Position.Y, sp.Size * sa, sp.Color * sa);
                }
            }
        }

        private void DrawParticles()
        {
            foreach (var p in _particles)
            {
                float a = p.Life / p.MaxLife;
                if (p.IsSpark) DrawCircle(p.Position.X, p.Position.Y, p.Size * a, p.Color * a);
                else DrawRect(new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)(p.Size * a) + 1, (int)(p.Size * a) + 1), p.Color * a);
            }
        }

        private void DrawRain()
        {
            foreach (var r in _rainDrops)
            {
                float a = r.Life / r.MaxLife;
                DrawLine(r.Position, r.Position + r.Velocity * 0.025f, r.Color * a, 1);
            }
        }

        private void DrawDamageNumbers()
        {
            if (_font == null) return;
            foreach (var n in _dmgNums)
            {
                float a = n.Life / n.MaxLife;
                string s = n.IsHeal ? $"+{n.Value:F0}" : (n.IsCrit ? $"CRIT! {n.Value:F0}" : $"-{n.Value:F0}");
                Color c = n.IsHeal ? Color.LimeGreen : (n.IsCrit ? Color.Yellow : Color.OrangeRed);
                float scale = n.IsCrit ? 1.3f : 1f;
                Vector2 sz = _font.MeasureString(s) * scale;
                _sb.DrawString(_font, s, n.Position - new Vector2(sz.X / 2f, 0), c * a, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        private void DrawFloatingCoins()
        {
            foreach (var c in _coins)
            {
                float a = c.Life / 1.5f;
                DrawCircle(c.Position.X, c.Position.Y, 9f, new Color(255, 180, 60) * a);
                DrawCircle(c.Position.X + 1, c.Position.Y - 1, 6f, new Color(70, 210, 60) * a);
                DrawRect(new Rectangle((int)c.Position.X - 1, (int)c.Position.Y - 10, 2, 5), new Color(40, 140, 40) * a);
                if (_font != null && c.Amount > 0)
                    _sb.DrawString(_font, $"+{c.Amount}", c.Position + new Vector2(10, -10), Color.Gold * a, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            }
        }

        private void DrawFruits()
        {
            foreach (var f in _fruits)
            {
                float bob = (float)Math.Sin(_globalTimer * 2.5f + f.Position.X * 0.02f) * 3f;
                Color body = f.Kind.Contains("Temple") ? new Color(255, 210, 80) : new Color(120, 255, 120);
                DrawCircle(f.Position.X, f.Position.Y + bob, 8f, body);
                DrawCircle(f.Position.X + 2, f.Position.Y - 2 + bob, 4f, Color.White * 0.65f);
                DrawRect(new Rectangle((int)f.Position.X - 1, (int)f.Position.Y - 12 + (int)bob, 2, 5), new Color(40, 140, 40));
                if (_font != null)
                    _sb.DrawString(_font, $"+{f.Value}", f.Position + new Vector2(10, -14 + bob), Color.LightGreen, 0, Vector2.Zero, 0.68f, SpriteEffects.None, 0);
            }
        }

        private void DrawHUD()
        {
            if (_font == null) return;

            // Background bar
            DrawRect(new Rectangle(0, 0, BASE_W, 56), new Color(0, 0, 0, 190));
            DrawRect(new Rectangle(0, 54, BASE_W, 2), new Color(60, 160, 60, 160));

            // ---- Left section (HP, SP, XP) ----
            int leftX = 14;
            DrawStatBar(leftX, 10, 220, 16, _player.Health, _player.MaxHealth, new Color(200, 40, 40), new Color(80, 10, 10), "HP");
            DrawStatBar(leftX, 30, 220, 10, _player.Stamina, _player.MaxStamina, new Color(40, 150, 220), new Color(10, 40, 80), "SP");
            DrawStatBar(leftX + 236, 10, 180, 12, _player.XP, _player.XPToNext, new Color(150, 80, 220), new Color(40, 20, 80), "XP");
            _sb.DrawString(_font, $"Lv.{_player.Level}", new Vector2(leftX + 236, 26), Color.Violet * 0.9f, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);

            // ---- Center section (Gold, Coins, Armor) ----
            int centerX = 460;
            DrawCircle(centerX, 20, 10f, Color.Gold);
            _sb.DrawString(_font, $"{_player.Gold}", new Vector2(centerX + 24, 10), Color.Gold);
            _sb.DrawString(_font, $"Fruits {_player.FruitCount}", new Vector2(centerX + 80, 10), new Color(120, 255, 120), 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Armor {_player.ArmorLevel}", new Vector2(centerX + 80, 28), Color.LightGray, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);

            // ---- Right section (Wave, Resources, Time, Weather) ----
            int rightBase = BASE_W - 260;
            _sb.DrawString(_font, $"Wave {_wave}", new Vector2(rightBase, 10), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"W:{_player.WoodCount}  S:{_player.StoneCount}  P:{_player.HealthPotions}  B:{_player.Bombs}",
                new Vector2(rightBase, 30), Color.White * 0.85f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            string dayStr = _dayTime < 0.25f ? "Dawn" : _dayTime < 0.5f ? "Day" : _dayTime < 0.75f ? "Dusk" : "Night";
            _sb.DrawString(_font, dayStr, new Vector2(rightBase, 50), Color.LightYellow * 0.85f, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
            string wx = _weather == Weather.Rain ? "Rain" : _weather == Weather.Storm ? "Storm!" : "";
            if (!string.IsNullOrEmpty(wx))
                _sb.DrawString(_font, wx, new Vector2(rightBase, 68), Color.LightBlue * 0.85f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);

            // Stage tip (only show when timer active, placed below HUD to avoid overlap)
            if (_levelTipTimer > 0f)
                _sb.DrawString(_font, _levelTipText, new Vector2(12, 60), Color.Yellow * MathHelper.Clamp(_levelTipTimer / 4.5f, 0f, 1f), 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);

            // Bottom help line – keep it separate from the top HUD
            DrawRect(new Rectangle(0, BASE_H - 30, BASE_W, 30), new Color(0, 0, 0, 170));
            _sb.DrawString(_font, "WASD:Move  SPACE/Z:Attack  I:Shop  M:Map  Q:Bomb  H:Potion  F11:Fullscreen",
                new Vector2(10, BASE_H - 22), Color.White * 0.6f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
        }

        private void DrawCircularHealthRing(float cx, float cy, float radius, float value, float max, Color fill, Color bg)
        {
            if (max <= 0f) return;
            float ratio = MathHelper.Clamp(value / max, 0f, 1f);
            int segments = 40;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float ang = -MathHelper.PiOver2 + t * MathHelper.TwoPi;
                Color c = t <= ratio ? Color.Lerp(fill, Color.LimeGreen, ratio) : bg * 0.55f;
                DrawCircle(cx + (float)Math.Cos(ang) * radius, cy + (float)Math.Sin(ang) * radius, 2.5f, c);
            }
        }

        private void DrawStatBar(int x, int y, int w, int h, float val, float max, Color fill, Color bg, string label)
        {
            DrawRect(new Rectangle(x - 1, y - 1, w + 2, h + 2), Color.Black * 0.6f);
            DrawRect(new Rectangle(x, y, w, h), bg);
            int fw = (int)(w * MathHelper.Clamp(val / max, 0f, 1f));
            if (fw > 0) DrawRect(new Rectangle(x, y, fw, h), fill);
            DrawRect(new Rectangle(x, y, fw, h / 3), Color.White * 0.12f);
            if (_font != null)
                _sb.DrawString(_font, $"{label} {val:F0}/{max:F0}", new Vector2(x + w + 6, y - 1), Color.White * 0.8f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
        }

        private void DrawMinimap()
        {
            int mx = BASE_W - 170, my = 60, mw = 160, mh = 80;
            DrawRect(new Rectangle(mx - 2, my - 2, mw + 4, mh + 4), new Color(0, 0, 0, 180));
            DrawRect(new Rectangle(mx, my, mw, mh), new Color(10, 28, 12, 220));

            float scaleX = (float)mw / Globals.WorldWidth;
            float scaleY = (float)mh / Globals.GroundY;

            foreach (var t in _trees)
                DrawRect(new Rectangle(mx + (int)(t.X * scaleX) - 1, my + (int)(t.Y * scaleY) - 1, 2, 2), new Color(30, 100, 30));
            foreach (var en in _enemies)
                if (!en.IsDead)
                    DrawRect(new Rectangle(mx + (int)(en.Position.X * scaleX), my + (int)(en.Position.Y * scaleY), en.IsBoss ? 5 : 3, en.IsBoss ? 5 : 3), en.IsBoss ? Color.OrangeRed : Color.Red);
            if (_blinkOn)
                DrawCircle(mx + _player.Position.X * scaleX, my + _player.Position.Y * scaleY, 4f, Color.Yellow);
            float cvx = mx + _cam.X * scaleX, cvy = my + _cam.Y * scaleY;
            DrawRect(new Rectangle((int)cvx, (int)cvy, (int)(BASE_W * scaleX), (int)(BASE_H * scaleY)), new Color(255, 255, 255, 30));
            if (_font != null) _sb.DrawString(_font, "MAP", new Vector2(mx + 2, my - 14), Color.White * 0.6f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }

        private void DrawWaveInfo()
        {
            if (_font == null) return;
            int alive = _enemies.Count(e => !e.IsDead && e.IsActive);
            _sb.DrawString(_font, $"Enemies: {alive}   Wave {_wave}", new Vector2(BASE_W - 185, BASE_H - 56), Color.OrangeRed * 0.9f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            if (alive == 0)
                _sb.DrawString(_font, $"Next wave in {Math.Max(0f, _waveCooldown):F1}s", new Vector2(BASE_W / 2f - 100, BASE_H / 2f - 40), Color.Cyan * 0.9f);
        }

        private void DrawQuestPanel()
        {
            if (_font == null) return;
            int qx = 12, qy = 60, qw = 230;
            DrawRect(new Rectangle(qx - 2, qy - 2, qw + 4, 130), new Color(0, 0, 0, 120));
            _sb.DrawString(_font, "QUESTS", new Vector2(qx + 2, qy), Color.Wheat, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            int i = 0;
            foreach (var q in _quests)
            {
                if (i >= 4) break;
                Color qc = q.Complete ? Color.LimeGreen * 0.7f : Color.White * 0.8f;
                string label = q.Complete ? $"✓ {q.Title}" : $"• {q.Title} [{Math.Min(q.Progress, q.Goal)}/{q.Goal}]";
                _sb.DrawString(_font, label, new Vector2(qx + 4, qy + 16 + i * 22), qc, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
                i++;
            }
        }

        private void DrawAchievementPopup()
        {
            if (_currentAch == null || _font == null) return;
            float a = MathHelper.Clamp(_currentAch.ShowTimer / 0.4f, 0f, 1f);
            int apx = BASE_W / 2 - 160, apy = 64;
            DrawRect(new Rectangle(apx, apy, 320, 60), new Color(20, 60, 20) * a);
            DrawRect(new Rectangle(apx, apy, 320, 2), Color.LimeGreen * a);
            DrawRect(new Rectangle(apx, apy + 58, 320, 2), Color.LimeGreen * 0.5f * a);
            _sb.DrawString(_font, "ACHIEVEMENT UNLOCKED", new Vector2(apx + 12, apy + 5), Color.LimeGreen * a, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            _sb.DrawString(_font, _currentAch.Title, new Vector2(apx + 12, apy + 22), Color.White * a);
            _sb.DrawString(_font, _currentAch.Desc, new Vector2(apx + 12, apy + 40), Color.LightGray * a, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
        }

        private void DrawComboDisplay()
        {
            if (_comboDisplayTimer <= 0f || _font == null || _lastCombo < 2) return;
            float a = MathHelper.Clamp(_comboDisplayTimer, 0f, 1f);
            string s = $"COMBO x{_lastCombo}!";
            Color c = _lastCombo >= 4 ? Color.OrangeRed : Color.Yellow;
            float scale = 1.2f + (_lastCombo - 1) * 0.15f;
            Vector2 sz = _font.MeasureString(s) * scale;
            _sb.DrawString(_font, s, new Vector2(BASE_W / 2f - sz.X / 2f, BASE_H * 0.35f), c * a, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void DrawLevelUpBanner()
        {
            if (_levelUpTimer <= 0f || _font == null) return;
            float a = MathHelper.Clamp(_levelUpTimer / 0.5f, 0f, 1f);
            string s = $"LEVEL UP!  Now Level {_player.Level}";
            Vector2 sz = _font.MeasureString(s) * 1.4f;
            _sb.DrawString(_font, s, new Vector2((BASE_W - sz.X) / 2f, BASE_H * 0.45f), Color.Gold * a, 0, Vector2.Zero, 1.4f, SpriteEffects.None, 0);
        }

        private void DrawWeaponIndicator()
        {
            if (_font == null) return;
            _sb.DrawString(_font, $"[{_player.Weapon.ToString().ToUpper()}]", new Vector2(BASE_W / 2f - 40, BASE_H - 52), GetWeaponColor(_player.Weapon) * 0.9f, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }

        private void DrawShop()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(30, 16, 5), new Color(70, 40, 14), BASE_H);
            int pw = 900, ph = 640;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 190));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(40, 22, 8, 220));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(220, 170, 60, 200));
            DrawRect(new Rectangle(ppx, ppy + ph - 3, pw, 3), new Color(220, 170, 60, 100));

            string title = "TRADING POST";
            Vector2 ts = _font.MeasureString(title) * 1.8f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 14), Color.Gold, 0, Vector2.Zero, 1.8f, SpriteEffects.None, 0);
            DrawCircle(ppx + 28, ppy + 68, 12f, Color.Gold);
            _sb.DrawString(_font, $"Your Gold: {_player.Gold}", new Vector2(ppx + 46, ppy + 58), Color.Yellow, 0, Vector2.Zero, 1.1f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "ITEM", new Vector2(ppx + 22, ppy + 92), Color.Wheat, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "DESCRIPTION", new Vector2(ppx + 300, ppy + 92), Color.Wheat, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "COST", new Vector2(ppx + 730, ppy + 92), Color.Wheat, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            DrawRect(new Rectangle(ppx + 10, ppy + 112, pw - 20, 1), new Color(160, 130, 60, 120));

            for (int i = 0; i < _shopItems.Count; i++)
            {
                var r = ShopItemRect(i);
                var item = _shopItems[i];
                bool sel = i == _shopSelected, hov = r.Contains(MousePos());
                if (sel || hov) DrawRect(r, new Color(60, 38, 14, 200));
                if (sel) DrawRect(new Rectangle(r.X, r.Y, 3, r.Height), item.TintColor);
                Color txtCol = _player.Gold >= item.Cost ? Color.White : Color.Gray;
                DrawShopItemPreview(new Rectangle(r.X + 8, r.Y + 6, 48, 30), item);
                _sb.DrawString(_font, item.Name, new Vector2(r.X + 70, r.Y + 8), txtCol, 0, Vector2.Zero, 0.74f, SpriteEffects.None, 0);
                _sb.DrawString(_font, item.Description, new Vector2(r.X + 300, r.Y + 8), Color.LightGray * 0.9f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
                _sb.DrawString(_font, $"{item.Cost}g", new Vector2(r.X + 735, r.Y + 8), Color.Gold, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
                DrawRect(new Rectangle(r.X + 10, r.Bottom - 1, r.Width - 20, 1), new Color(60, 44, 20, 80));
            }
            if (_shopMessageTimer > 0f)
            {
                DrawRect(new Rectangle(ppx + 18, ppy + ph - 68, pw - 36, 28), new Color(80, 15, 15, 220));
                _sb.DrawString(_font, _shopMessage, new Vector2(ppx + 26, ppy + ph - 64), Color.OrangeRed * MathHelper.Clamp(_shopMessageTimer / 2f, 0f, 1f), 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            }
            _sb.DrawString(_font, "Up/Down or mouse to select  |  Enter/Z or click to buy  |  ESC/I to leave",
                new Vector2(ppx + 14, ppy + ph - 26), Color.Gray * 0.75f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            _sb.End();
        }

        private Rectangle ShopItemRect(int i)
        {
            int pw = 900;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - 640) / 2;
            return new Rectangle(ppx + 10, ppy + 120 + i * 48, pw - 20, 44);
        }

        private void DrawShopItemPreview(Rectangle box, ShopItem item)
        {
            Color c = item.TintColor;
            string icon = item.Name;
            if (icon.Contains("Sword"))
            {
                DrawRect(new Rectangle(box.X + 16, box.Y + 8, 4, 16), c);
                DrawRect(new Rectangle(box.X + 12, box.Y + 14, 12, 4), c);
            }
            else if (icon.Contains("Axe"))
            {
                DrawRect(new Rectangle(box.X + 17, box.Y + 6, 4, 18), c);
                DrawRect(new Rectangle(box.X + 8, box.Y + 8, 12, 14), c);
            }
            else if (icon.Contains("Staff"))
            {
                DrawRect(new Rectangle(box.X + 17, box.Y + 3, 4, 20), c);
                DrawCircle(box.X + 19, box.Y + 4, 5f, Color.White);
            }
            else if (icon.Contains("Pistol"))
            {
                DrawRect(new Rectangle(box.X + 9, box.Y + 11, 18, 6), c);
                DrawRect(new Rectangle(box.X + 20, box.Y + 8, 6, 4), new Color(80, 80, 80));
            }
            else if (icon.Contains("Shield"))
            {
                DrawRect(new Rectangle(box.X + 13, box.Y + 4, 12, 18), c);
                DrawRect(new Rectangle(box.X + 15, box.Y + 7, 8, 10), Color.White * 0.35f);
            }
            else if (icon.Contains("Potion"))
            {
                DrawRect(new Rectangle(box.X + 14, box.Y + 7, 8, 14), c);
                DrawCircle(box.X + 18, box.Y + 6, 4f, Color.Red);
            }
            else if (icon.Contains("Boot"))
            {
                DrawRect(new Rectangle(box.X + 10, box.Y + 13, 15, 6), c);
                DrawRect(new Rectangle(box.X + 21, box.Y + 7, 4, 8), c);
            }
            else if (icon.Contains("Bomb"))
            {
                DrawCircle(box.X + 18, box.Y + 12, 8f, c);
                DrawRect(new Rectangle(box.X + 16, box.Y + 2, 4, 5), Color.Orange);
            }
            else if (icon.Contains("MXHP") || icon.Contains("Armor"))
            {
                DrawRect(new Rectangle(box.X + 10, box.Y + 6, 16, 12), c);
                DrawRect(new Rectangle(box.X + 15, box.Y + 3, 6, 18), Color.White * 0.4f);
            }
            else if (icon.Contains("VIP"))
            {
                DrawCircle(box.X + 18, box.Y + 12, 8f, Color.Gold);
                DrawCircle(box.X + 18, box.Y + 12, 4f, Color.White * 0.6f);
            }
        }

        private void DrawFullMap()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawRect(new Rectangle(0, 0, BASE_W, BASE_H), new Color(8, 18, 10, 245));
            string hdr = "WORLD MAP";
            Vector2 hs = _font.MeasureString(hdr) * 1.6f;
            _sb.DrawString(_font, hdr, new Vector2((BASE_W - hs.X) / 2f, 16), Color.White, 0, Vector2.Zero, 1.6f, SpriteEffects.None, 0);

            int mx = 60, my = 70, mw = BASE_W - 340, mh = BASE_H - 140;
            DrawRect(new Rectangle(mx - 2, my - 2, mw + 4, mh + 4), new Color(40, 80, 40, 200));
            DrawRect(new Rectangle(mx, my, mw, mh), new Color(12, 30, 14));
            int cols = 12, rows = 8;
            for (int c = 0; c <= cols; c++) DrawRect(new Rectangle(mx + c * mw / cols, my, 1, mh), new Color(30, 60, 30, 80));
            for (int r = 0; r <= rows; r++) DrawRect(new Rectangle(mx, my + r * mh / rows, mw, 1), new Color(30, 60, 30, 80));

            float sx = (float)mw / Globals.WorldWidth;
            float sy = (float)mh / Globals.GroundY;

            foreach (var t in _trees) DrawRect(new Rectangle(mx + (int)(t.X * sx) - 2, my + (int)(t.Y * sy) - 2, 4, 4), new Color(30, 100, 30));
            foreach (var p in _platRects) DrawRect(new Rectangle(mx + (int)(p.X * sx), my + (int)(p.Y * sy), Math.Max(2, (int)(p.Width * sx)), 2), new Color(120, 80, 40));
            DrawRect(new Rectangle(mx + (int)((Globals.WorldWidth - 340) * sx) - 2, my + (int)(Globals.GroundY * sy) - 6, 6, 6), Color.Gold);
            foreach (var en in _enemies)
            {
                if (en.IsDead) continue;
                int ex = mx + (int)(en.Position.X * sx), ey2 = my + (int)(en.Position.Y * sy);
                int esz = en.IsBoss ? 6 : 3;
                DrawRect(new Rectangle(ex - esz / 2, ey2 - esz / 2, esz, esz), en.IsBoss ? Color.OrangeRed : GetEnemyColor(en.Type));
            }
            float px2 = mx + _player.Position.X * sx, py2 = my + _player.Position.Y * sy;
            if (_blinkOn) { DrawCircle(px2, py2, 7f, Color.Yellow * 0.6f); DrawCircle(px2, py2, 4f, Color.Yellow); }

            int lx = BASE_W - 260, ly = 70;
            DrawRect(new Rectangle(lx - 4, ly - 4, 240, 380), new Color(0, 0, 0, 180));
            _sb.DrawString(_font, "LEGEND", new Vector2(lx, ly), Color.Wheat);
            DrawLegendEntry(lx, ly + 26, Color.Yellow, "You (player)");
            DrawLegendEntry(lx, ly + 50, GetEnemyColor(EnemyType.Zombie), "Zombie");
            DrawLegendEntry(lx, ly + 74, GetEnemyColor(EnemyType.Goblin), "Goblin");
            DrawLegendEntry(lx, ly + 98, GetEnemyColor(EnemyType.Skeleton), "Skeleton");
            DrawLegendEntry(lx, ly + 122, Color.OrangeRed, "Boss!");
            DrawLegendEntry(lx, ly + 146, new Color(30, 100, 30), "Tree");
            DrawLegendEntry(lx, ly + 170, new Color(120, 80, 40), "Platform");

            _sb.DrawString(_font, "STATS", new Vector2(lx, ly + 200), Color.Wheat);
            _sb.DrawString(_font, $"HP:    {_player.Health:F0}/{_player.MaxHealth:F0}", new Vector2(lx, ly + 224), Color.OrangeRed);
            _sb.DrawString(_font, $"Lv:    {_player.Level}", new Vector2(lx, ly + 248), Color.Violet);
            _sb.DrawString(_font, $"Gold:  {_player.Gold}", new Vector2(lx, ly + 272), Color.Gold);
            _sb.DrawString(_font, $"Wave:  {_wave}", new Vector2(lx, ly + 296), Color.White);
            _sb.DrawString(_font, $"Kills: {_quests.Find(q => q.Title == "First Blood")?.Progress ?? 0}", new Vector2(lx, ly + 320), Color.White);
            _sb.DrawString(_font, $"Bosses defeated: {_totalBossesDefeated}/{BOSSES_TO_WIN}", new Vector2(lx, ly + 344), Color.OrangeRed);
            _sb.DrawString(_font, $"Stage: {_stageIndex}  Temple: {((int)Globals.WorldWidth - 340)}", new Vector2(lx, ly + 368), Color.LightGreen);
            string hint = "Press M or ESC to close";
            Vector2 hsize = _font.MeasureString(hint);
            _sb.DrawString(_font, hint, new Vector2((BASE_W - hsize.X) / 2f, BASE_H - 28), Color.Gray);
            _sb.End();
        }

        private void DrawStageClear()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(8, 30, 12), new Color(20, 60, 26), BASE_H);
            for (int i = 0; i < 24; i++)
                DrawCircle(120 + i * 48, 120 + (float)Math.Sin(_globalTimer * 2f + i) * 18f, 4f + (i % 3), new Color(120, 255, 160, 40));

            int pw = 700, ph = 360;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(140, 255, 160, 220));
            string msg = $"STAGE {_stageIndex} CLEARED";
            Vector2 ms = _font.MeasureString(msg) * 2f;
            _sb.DrawString(_font, msg, new Vector2((BASE_W - ms.X) / 2f, ppy + 28), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "The temple has been reached. The jungle awakens deeper ahead.", new Vector2(ppx + 48, ppy + 130), Color.LightGreen, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Gold: {_player.Gold}  Fruits: {_player.FruitCount}  Level: {_player.Level}", new Vector2(ppx + 48, ppy + 170), Color.White, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "Press Enter or wait to enter the next stage", new Vector2(ppx + 48, ppy + 260), Color.Yellow, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawLegendEntry(int x, int y, Color col, string label)
        {
            DrawRect(new Rectangle(x, y + 3, 12, 12), col);
            if (_font != null) _sb.DrawString(_font, label, new Vector2(x + 18, y), Color.LightGray, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
        }

        private void DrawPaused()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(18, 8, 30), new Color(34, 60, 88), BASE_H);
            for (int i = 0; i < 30; i++)
                DrawCircle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 2f + (float)_rng.NextDouble() * 4f, new Color(120, 180, 255, 45));
            int pw = 700, ph = 460;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 220));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(12, 16, 30, 230));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(120, 220, 255) * 0.9f);
            string title = "SETTINGS";
            Vector2 ts = _font.MeasureString(title) * 2f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 24), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            string[] options =
            {
                _fullscreen ? "Fullscreen: ON" : "Fullscreen: OFF",
                "Window: 960 x 540",
                "Window: 1280 x 720",
                _rotateScreen ? "Rotate Screen: ON" : "Rotate Screen: OFF",
                _mobileMode ? "Mobile Controls: ON" : "Mobile Controls: OFF",
                "Back to game"
            };
            for (int i = 0; i < options.Length; i++)
            {
                bool sel = i == _settingsSelected;
                if (sel) DrawRect(new Rectangle(ppx + 26, ppy + 106 + i * 56, pw - 52, 44), new Color(90, 200, 255, 35));
                _sb.DrawString(_font, sel ? $"> {options[i]}" : options[i], new Vector2(ppx + 42, ppy + 116 + i * 56), sel ? Color.Black : Color.White, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
            }
            _sb.DrawString(_font, "Use Up/Down and Space/Left/Right to adjust  |  Enter or ESC to return", new Vector2(ppx + 30, ppy + ph - 38), Color.LightGray * 0.8f, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawHelp()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(4, 24, 18), new Color(16, 44, 30), BASE_H);
            for (int i = 0; i < 24; i++)
                DrawCircle(120 + i * 46, 110 + (float)Math.Sin(_globalTimer * 1.8f + i) * 16f, 4f + (i % 3), new Color(120, 255, 180, 35));
            int pw = 760, ph = 500;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 220));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(120, 220, 120, 200));
            string title = "HELP";
            Vector2 ts = _font.MeasureString(title) * 2f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 20), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            string[] lines =
            {
                "Move: WASD or Arrow keys",
                "Attack: Space or Z",
                "Shop: I or B, or use the welcome shop button",
                "Map: M, Pause/Settings: Escape",
                "Bomb: Q, Potion: H",
                "Pistol shots and magic attacks work best on shielded enemies",
                "Platforms and trees let you climb for better positioning"
            };
            for (int i = 0; i < lines.Length; i++)
                _sb.DrawString(_font, lines[i], new Vector2(ppx + 34, ppy + 120 + i * 42), Color.White * 0.9f, 0, Vector2.Zero, 0.88f, SpriteEffects.None, 0);
            var backR = BackButtonRect();
            DrawRect(backR, new Color(20, 80, 50, 220));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(140, 255, 180, 180));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 22, backR.Y + 12), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawCredits()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(8, 14, 34), new Color(12, 26, 58), BASE_H);
            for (int i = 0; i < 20; i++)
                DrawCircle(140 + i * 55, 92 + (float)Math.Sin(_globalTimer * 1.5f + i * 0.4f) * 14f, 5f + (i % 2), new Color(140, 180, 255, 35));
            int pw = 760, ph = 420;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 220));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(140, 180, 255, 200));
            string title = "CREDITS";
            Vector2 ts = _font.MeasureString(title) * 2f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 22), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            string[] lines =
            {
                "Jungle Adventure",
                "Monogame jungle action prototype",
                "Version 1.0.3",
                "Design and gameplay systems expanded inside Game1.cs",
                "Art remains procedural and can be replaced with sprite assets later"
            };
            for (int i = 0; i < lines.Length; i++)
                _sb.DrawString(_font, lines[i], new Vector2(ppx + 34, ppy + 116 + i * 40), Color.White * 0.92f, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
            var backR = BackButtonRect();
            DrawRect(backR, new Color(24, 36, 92, 220));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(160, 200, 255, 180));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 22, backR.Y + 12), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawLevelTipBanner()
        {
            if (_levelTipTimer <= 0f || _font == null) return;
            float a = MathHelper.Clamp(_levelTipTimer / 4.5f, 0f, 1f);
            int bw = 620, bh = 52;
            int bx = BASE_W / 2 - bw / 2, by = BASE_H - 94;
            DrawRect(new Rectangle(bx, by, bw, bh), new Color(0, 0, 0, 170) * a);
            DrawRect(new Rectangle(bx, by, bw, 3), new Color(255, 220, 120, 220) * a);
            _sb.DrawString(_font, _levelTipText, new Vector2(bx + 16, by + 14), Color.White * a, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0);
        }

        private Rectangle BackButtonRect() => new Rectangle(BASE_W / 2 - 140, BASE_H - 78, 280, 42);

        private Rectangle MusicButtonRect() => new Rectangle(BASE_W - 224, 18, 194, 38);

        private Rectangle LevelCellRect(int level)
        {
            int gridX = 106;
            int gridY = 172;
            int cellW = 104;
            int cellH = 42;
            int gap = 6;
            int idx = level - 1;
            int col = idx % 10;
            int row = idx / 10;
            return new Rectangle(gridX + col * (cellW + gap), gridY + row * (cellH + gap), cellW, cellH);
        }

        private void DrawWin()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            float pulse = 0.7f + 0.3f * (float)Math.Sin(_endScreenTimer * 2f);
            DrawGradient(new Color((int)(4 * pulse), (int)(40 * pulse), (int)(8 * pulse)), new Color((int)(12 * pulse), (int)(80 * pulse), (int)(20 * pulse)), BASE_H);

            for (int i = 0; i < 5; i++)
                DrawRect(new Rectangle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 6, 6), new Color(_rng.Next(100, 255), _rng.Next(100, 255), _rng.Next(100, 255), 160));

            int pw = 700, ph = 440;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(8, 40, 12, 230));
            for (int edge = 0; edge < 4; edge++)
                DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(200, 220, 80) * (0.5f + 0.5f * (float)Math.Sin(_endScreenTimer * 3f + edge)));

            if (_font != null)
            {
                string ftitle = "VICTORY!";
                float tScale = 3.2f, glow = 0.6f + 0.4f * (float)Math.Sin(_endScreenTimer * 3f);
                Vector2 tsz = _font.MeasureString(ftitle) * tScale;
                float tx = (BASE_W - tsz.X) / 2f, ty = ppy + 30f;
                _sb.DrawString(_font, ftitle, new Vector2(tx - 4, ty - 4), new Color(200, 255, 100) * glow * 0.3f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ftitle, new Vector2(tx + 4, ty + 4), new Color(200, 255, 100) * glow * 0.3f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ftitle, new Vector2(tx + 3, ty + 3), Color.Black * 0.5f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ftitle, new Vector2(tx, ty), new Color(255, 255, 150), 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, "You defeated all the bosses and saved the jungle!", new Vector2(ppx + 30, ppy + 148), Color.LightGreen, 0, Vector2.Zero, 0.95f, SpriteEffects.None, 0);
                _sb.DrawString(_font, $"Final Level:  {_player.Level}", new Vector2(ppx + 60, ppy + 200), Color.White);
                _sb.DrawString(_font, $"Gold Earned:  {_player.Gold}", new Vector2(ppx + 60, ppy + 230), Color.Gold);
                _sb.DrawString(_font, $"Waves Survived: {_wave}", new Vector2(ppx + 60, ppy + 260), Color.Cyan);
                _sb.DrawString(_font, $"Bosses Slain: {_totalBossesDefeated}", new Vector2(ppx + 60, ppy + 290), Color.OrangeRed);
                if (_blinkOn)
                {
                    string pe = "Press ENTER to play again";
                    Vector2 ps = _font.MeasureString(pe);
                    _sb.DrawString(_font, pe, new Vector2((BASE_W - ps.X) / 2f, ppy + ph - 45), Color.Yellow * 0.9f);
                }
            }
            _sb.End();
        }

        private void DrawLose()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            float t = MathHelper.Clamp(_endScreenTimer / 1.5f, 0f, 1f);
            DrawGradient(Color.Lerp(Color.Black, new Color(40, 4, 4), t), Color.Lerp(Color.Black, new Color(80, 8, 8), t), BASE_H);
            for (int i = 0; i < 30; i++)
                DrawRect(new Rectangle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 1, _rng.Next(12, 30)), new Color(60, 60, 80, 100));

            int pw = 680, ph = 430;
            int ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 215));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(40, 4, 4, 230));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(200, 30, 30) * 0.9f);
            DrawRect(new Rectangle(ppx, ppy + ph - 3, pw, 3), new Color(200, 30, 30) * 0.5f);

            if (_font != null)
            {
                string ltitle = "GAME OVER";
                float tScale = 3f, red = 0.6f + 0.4f * (float)Math.Sin(_endScreenTimer * 2.5f);
                Vector2 tsz = _font.MeasureString(ltitle) * tScale;
                float tx = (BASE_W - tsz.X) / 2f, ty = ppy + 26f;
                _sb.DrawString(_font, ltitle, new Vector2(tx + 3, ty + 3), Color.Black * 0.55f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ltitle, new Vector2(tx, ty), new Color(255, 60, 60) * red, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, "The jungle claimed you...", new Vector2(ppx + 30, ppy + 148), Color.OrangeRed * 0.9f);
                _sb.DrawString(_font, $"Level Reached:  {_player.Level}", new Vector2(ppx + 60, ppy + 192), Color.White);
                _sb.DrawString(_font, $"Gold Collected: {_player.Gold}", new Vector2(ppx + 60, ppy + 222), Color.Gold);
                _sb.DrawString(_font, $"Waves Survived: {_wave - 1}", new Vector2(ppx + 60, ppy + 252), Color.Cyan);
                _sb.DrawString(_font, $"Bosses Slain:   {_totalBossesDefeated}", new Vector2(ppx + 60, ppy + 282), Color.OrangeRed);
                string[] tips = { "Tip: Buy the Iron Shield to reduce damage.", "Tip: Level up to increase HP and damage.", "Tip: Use Health Potions (H key) to survive.", "Tip: Combo hits deal bonus damage!", "Tip: Boss waves spawn every 3rd wave.", "Tip: The Magic Staff hits multiple enemies!" };
                _sb.DrawString(_font, tips[((int)(_endScreenTimer * 0.5f)) % tips.Length], new Vector2(ppx + 30, ppy + 330), Color.Gray * 0.85f, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
                if (_blinkOn)
                {
                    string pe = "Press ENTER to try again";
                    Vector2 ps = _font.MeasureString(pe);
                    _sb.DrawString(_font, pe, new Vector2((BASE_W - ps.X) / 2f, ppy + ph - 42), Color.Yellow * 0.9f);
                }
            }
            _sb.End();
        }

        private void DrawGradient(Color top, Color bottom, int height)
        {
            for (int y = 0; y < height; y += 8)
                DrawRect(new Rectangle(0, y, BASE_W, 8), Color.Lerp(top, bottom, (float)y / height));
        }

        private void DrawCircle(float x, float y, float radius, Color color)
        {
            if (_circleTex == null) return;
            _sb.Draw(_circleTex, new Vector2(x, y), null, color, 0f, new Vector2(8, 8), radius / 8f, SpriteEffects.None, 0);
        }

        private void DrawRect(Rectangle r, Color c) { if (_pixel != null) _sb.Draw(_pixel, r, c); }

        private void DrawLine(Vector2 a, Vector2 b, Color c, int thickness = 1)
        {
            float dist = Vector2.Distance(a, b);
            float ang = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
            _sb.Draw(_pixel, a, null, c, ang, Vector2.Zero, new Vector2(dist, thickness), SpriteEffects.None, 0);
        }

        private void DrawLineThick(Vector2 a, Vector2 b, Color c, int thickness)
        {
            for (int i = -thickness / 2; i <= thickness / 2; i++)
            {
                Vector2 off = new Vector2(-(b.Y - a.Y), b.X - a.X);
                if (off.LengthSquared() > 0.001f) off.Normalize();
                DrawLine(a + off * i, b + off * i, c, 1);
            }
        }

        private void SpawnParticle(Vector2 pos, Color col, float size, float life)
        {
            _particles.Add(new Particle
            {
                Position = pos,
                Velocity = new Vector2((float)_rng.NextDouble() * 200f - 100f, (float)_rng.NextDouble() * -180f),
                Life = life, MaxLife = life, Size = size, Color = col,
                RotVel = (float)_rng.NextDouble() * 6f - 3f
            });
        }

        private void SpawnHitParticles(Vector2 pos)
        {
            for (int i = 0; i < 5; i++)
                SpawnParticle(pos + new Vector2((float)_rng.NextDouble() * 30f - 15f, -20f), new Color(255, 80, 30, 200), 4f + (float)_rng.NextDouble() * 4f, 0.4f);
        }

        private void SpawnDeathParticles(Vector2 pos, EnemyType type)
        {
            Color c = GetEnemyColor(type);
            for (int i = 0; i < 16; i++)
                SpawnParticle(pos + new Vector2((float)_rng.NextDouble() * 40f - 20f, -(float)_rng.NextDouble() * 60f), c, 5f + (float)_rng.NextDouble() * 6f, 0.7f + (float)_rng.NextDouble() * 0.5f);
        }

        private void SpawnDamageNumber(Vector2 pos, float val, bool isHeal = false, bool isCrit = false, bool isPlayer = false)
        {
            _dmgNums.Add(new DamageNumber { Position = pos + new Vector2((float)_rng.NextDouble() * 30f - 15f, 0f), Value = val, IsCrit = isCrit, IsHeal = isHeal });
        }

        private void SpawnCoin(Vector2 pos, int amount)
        {
            _coins.Add(new FloatingCoin { Position = pos + new Vector2((float)_rng.NextDouble() * 30f - 15f, -20f), Velocity = new Vector2((float)_rng.NextDouble() * 120f - 60f, -200f - (float)_rng.NextDouble() * 100f), Amount = amount });
        }

        private void SetupAudio()
        {
            try
            {
                _snd.SwordSwing = CreateToneSound(260f, 0.09f, 0.35f);
                _snd.PlayerHit = CreateToneSound(120f, 0.12f, 0.34f, 80f);
                _snd.EnemyHit = CreateToneSound(180f, 0.08f, 0.3f, 60f);
                _snd.EnemyDeath = CreateNoiseBurst(0.18f, 0.22f, 0.2f);
                _snd.ZombieGroan = CreateNoiseBurst(0.2f, 0.14f, 0.1f);
                _snd.CoinPickup = CreateToneSound(640f, 0.08f, 0.28f, 880f);
                _snd.LevelUp = CreateToneSound(780f, 0.18f, 0.25f, 1040f);
                _snd.WinJingle = CreateVictoryJingle();
                _snd.LoseStinger = CreateToneSound(95f, 0.32f, 0.4f, 60f);
                _snd.ShopBuy = CreateToneSound(540f, 0.1f, 0.28f, 720f);
                _snd.BossRoar = CreateBossRoar();
                _snd.RainLoop = CreateNoiseBurst(0.32f, 0.08f, 0.0f);
                _musicLoop = CreateMusicLoop();
            }
            catch
            {
                _snd = new SoundManager();
                _musicLoop = null;
            }
        }

        private SoundEffect _musicLoop;

        private void ToggleMusic()
        {
            _musicEnabled = !_musicEnabled;
            ApplyMusicState();
        }

        private void ApplyMusicState()
        {
            try
            {
                if (_musicInstance != null)
                {
                    _musicInstance.Stop();
                    _musicInstance.Dispose();
                    _musicInstance = null;
                }

                if (_musicEnabled && _musicLoop != null)
                {
                    _musicInstance = _musicLoop.CreateInstance();
                    _musicInstance.IsLooped = true;
                    _musicInstance.Volume = 0.28f;
                    _musicInstance.Play();
                }
            }
            catch { }
        }

        private SoundEffect CreateToneSound(float frequency, float durationSeconds, float volume, float sweepTo = 0f)
        {
            const int sampleRate = 22050;
            int samples = Math.Max(1, (int)(sampleRate * durationSeconds));
            byte[] buffer = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                float t = i / (float)sampleRate;
                float freq = sweepTo > 0f ? MathHelper.Lerp(frequency, sweepTo, progress) : frequency;
                float envelope = (float)Math.Exp(-progress * 4.5f);
                short sample = (short)(Math.Sin(2 * Math.PI * freq * t) * envelope * volume * short.MaxValue);
                buffer[i * 2] = (byte)(sample & 0xff);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
            }
            return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
        }

        private SoundEffect CreateNoiseBurst(float durationSeconds, float volume, float sweepTo = 0f)
        {
            const int sampleRate = 22050;
            int samples = Math.Max(1, (int)(sampleRate * durationSeconds));
            byte[] buffer = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                float envelope = (float)Math.Exp(-progress * 5f);
                float tone = (float)(_rng.NextDouble() * 2.0 - 1.0);
                if (sweepTo > 0f)
                    tone *= 0.65f + 0.35f * (float)Math.Sin(progress * MathHelper.TwoPi * 4f);
                short sample = (short)(tone * envelope * volume * short.MaxValue);
                buffer[i * 2] = (byte)(sample & 0xff);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
            }
            return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
        }

        private SoundEffect CreateVictoryJingle()
        {
            const int sampleRate = 22050;
            float durationSeconds = 1.0f;
            int samples = (int)(sampleRate * durationSeconds);
            byte[] buffer = new byte[samples * 2];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                int noteIndex = Math.Min(notes.Length - 1, (int)(progress * notes.Length));
                float freq = notes[noteIndex];
                float envelope = (float)Math.Exp(-progress * 3.8f);
                float sampleValue = (float)Math.Sin(2 * Math.PI * freq * i / sampleRate) * envelope * 0.25f;
                short sample = (short)(sampleValue * short.MaxValue);
                buffer[i * 2] = (byte)(sample & 0xff);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
            }
            return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
        }

        private SoundEffect CreateBossRoar()
        {
            return CreateToneSound(60f, 0.42f, 0.42f, 32f);
        }

        private SoundEffect CreateMusicLoop()
        {
            const int sampleRate = 22050;
            float durationSeconds = 4f;
            int samples = (int)(sampleRate * durationSeconds);
            byte[] buffer = new byte[samples * 2];
            float[] chord = { 196f, 246.94f, 293.66f, 392f };
            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                float beat = progress * 8f;
                int step = (int)beat;
                float freq = chord[step % chord.Length];
                float pulse = 0.5f + 0.5f * (float)Math.Sin(progress * MathHelper.TwoPi * 2f);
                float note = (float)Math.Sin(2 * Math.PI * freq * i / sampleRate);
                float harmony = (float)Math.Sin(2 * Math.PI * (freq * 2f) * i / sampleRate) * 0.25f;
                float bass = (float)Math.Sin(2 * Math.PI * (freq * 0.5f) * i / sampleRate) * 0.12f;
                float envelope = 0.18f + 0.12f * pulse;
                short sample = (short)((note * 0.55f + harmony + bass) * envelope * short.MaxValue);
                buffer[i * 2] = (byte)(sample & 0xff);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
            }
            return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
        }

        private void TriggerScreenShake(float duration, float intensity)
        {
            _shakeTimer = Math.Max(_shakeTimer, duration);
            _shakeIntensity = Math.Max(_shakeIntensity, intensity);
        }

        private bool KeyJustPressed(Keys k) => _kCurr.IsKeyDown(k) && _kPrev.IsKeyUp(k);
        private bool MouseJustReleased() => _mPrev.LeftButton == ButtonState.Pressed && _mCurr.LeftButton == ButtonState.Released;
        private Point MousePos() => _mCurr.Position;

        private Rectangle WelcomeBtn(int idx) => new Rectangle((BASE_W - 340) / 2, 246 + idx * 44, 340, 38);

        private void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            _gfx.IsFullScreen = _fullscreen;
            _gfx.PreferredBackBufferWidth  = _fullscreen ? GraphicsDevice.DisplayMode.Width  : BASE_W;
            _gfx.PreferredBackBufferHeight = _fullscreen ? GraphicsDevice.DisplayMode.Height : BASE_H;
            _gfx.ApplyChanges();
            RecalcRTRect();
        }

        private void ApplyWindowPreset(int width, int height)
        {
            _fullscreen = false;
            _gfx.IsFullScreen = false;
            _gfx.PreferredBackBufferWidth = width;
            _gfx.PreferredBackBufferHeight = height;
            _gfx.ApplyChanges();
            RecalcRTRect();
        }
    }
}
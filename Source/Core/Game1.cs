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
    public enum EnemyType  { Zombie, Goblin, Skeleton, Shielded, Boss, Ogre, Giant, Demon, Shadow }
    public enum WeaponType { Fists, Sword, Axe, MagicStaff, Pistol }
    public enum Weather    { Clear, Rain, Storm }

    // =====================================================================
    //  SOUND MANAGER
    // =====================================================================
    public class SoundManager
    {
        private static readonly Random _sharedRandom = new Random();
        public SoundEffect SwordSwing, PlayerHit, EnemyHit, EnemyDeath, ZombieGroan, CoinPickup, LevelUp, WinJingle, LoseStinger, ShopBuy, BossRoar, RainLoop;
        public void Play(SoundEffect sfx, float vol = 1f, float p = 0f) { if (sfx != null) { try { sfx.Play(vol, p, 0f); } catch { } } }
        public void PlayRandom(SoundEffect sfx, float vMin = 0.7f, float vMax = 1f, float pR = 0.2f)
        {
            if (sfx == null) return;
            float v = vMin + (float)_sharedRandom.NextDouble() * (vMax - vMin);
            float p = (float)(_sharedRandom.NextDouble() * 2 - 1) * pR;
            try { sfx.Play(v, p, 0f); } catch { }
        }
    }

    public class DamageNumber { public Vector2 Position; public float Value; public float Life = 1.2f; public float MaxLife = 1.2f; public bool IsCrit, IsHeal; }
    public class FloatingCoin { public Vector2 Position; public Vector2 Velocity; public float Life = 1.5f; public int Amount; }
    public class FruitPickup { public Vector2 Position; public int Value; public string Kind; public float Spin; }
    public class Projectile { public Vector2 Position, Velocity; public float Damage; public float Life = 2f; public bool Friendly = true; public Color Color; }
    public class Particle { public Vector2 Position, Velocity; public float Life, MaxLife, Size, Rotation, RotVel; public Color Color; public bool IsSpark; }
    public class Enemy
    {
        public EnemyType Type; public Vector2 Position; public bool IsActive = true;
        public float Health, MaxHealth, Speed, Damage, AttackRange, AttackCooldown, AttackTimer, HitFlash, KnockbackX;
        public bool FacingRight, IsDead, DeathAnimDone; public float DeathTimer;
        public int GoldDrop, XPDrop; public float WalkAnim; public int Wave; public bool IsBoss;
        public float BossPhase, ShieldHealth, WidthScale = 1f, HeightScale = 1f; public int Variant = 0;
        public void TakeDamage(float dmg) { if (IsDead) return; Health -= dmg; HitFlash = 0.18f; if (Health <= 0) { Health = 0; IsDead = true; IsActive = false; } }
    }
    public class Player
    {
        public Vector2 Position; public bool IsActive = true;
        public float Health = 100f, MaxHealth = 100f, Stamina = 100f, MaxStamina = 100f, BaseDamage = 18f, CritChance = 0.15f, Defense = 0f, MoveSpeed = 220f, JumpPower = -520f, VelocityY = 0f;
        public bool OnGround = true, FacingRight = true, IsDead = false; public float DeathTimer = 0f;
        public int Gold = 80, FruitCount = 0, WoodCount = 0, StoneCount = 0, XP = 0, Level = 1, XPToNext = 100;
        public WeaponType Weapon = WeaponType.Fists; public bool HasShield, HasBoots; public int ArmorLevel, HealthPotions, Bombs;
        public bool IsAttacking; public float AttackTimer, AttackDuration = 0.35f, AttackCooldown, IFrameTimer, ComboWindow;
        public int ComboHits; public float WalkAnim, SwordArcProgress;
        public bool IsInvincible => IFrameTimer > 0f;
        public bool CanAttack => AttackCooldown <= 0f && !IsDead;
        public void AddXP(int amount, out bool leveledUp)
        {
            XP += amount; leveledUp = false;
            while (XP >= XPToNext) { XP -= XPToNext; Level++; XPToNext = (int)(XPToNext * 1.45f); MaxHealth += 15f; Health = MaxHealth; BaseDamage += 3f; Defense += 1f; leveledUp = true; }
        }
    }
    public class ShopItem { public string Name, Description; public int Cost; public string Icon; public bool IsUnlocked = true, Purchased; public Color TintColor; }
    public class Achievement { public string Title, Desc; public float ShowTimer = 3.5f; public bool Shown; }
    public class Quest { public string Title, Desc; public int Progress, Goal; public bool Complete; public int RewardGold; }
    public class SwordSlash { public Vector2 Origin; public float Angle, Life = 0.28f, MaxLife = 0.28f, ArcSpan, Radius; public bool FacingRight; public List<Particle> Sparks = new List<Particle>(); }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _gfx;
        private SpriteBatch _sb;
        private SoundManager _snd = new SoundManager();
        private Random _rng = new Random();
        public GameState State = GameState.Welcome;
        private GameState _pauseReturn, _shopReturnState;
        private int _shopSelected, _settingsSelected;
        private const int BASE_W = 1280, BASE_H = 720;
        private RenderTarget2D _rt;
        private Rectangle _rtRect;
        private bool _fullscreen;
        private Texture2D _pixel, _circleTex;
        private SpriteFont _font;
        private Player _player = new Player();
        private List<Enemy> _enemies = new List<Enemy>();
        private List<Vector2> _trees = new List<Vector2>();
        private List<float> _treeSizes = new List<float>();
        private List<Vector2> _grasses = new List<Vector2>(), _rocks = new List<Vector2>();
        private List<Rectangle> _platRects = new List<Rectangle>();
        private Vector2 _cam;
        private float _shakeTimer, _shakeIntensity;
        private List<Particle> _particles = new List<Particle>(), _rainDrops = new List<Particle>();
        private List<DamageNumber> _dmgNums = new List<DamageNumber>();
        private List<FloatingCoin> _coins = new List<FloatingCoin>();
        private List<FruitPickup> _fruits = new List<FruitPickup>();
        private List<Projectile> _projectiles = new List<Projectile>();
        private List<SwordSlash> _slashes = new List<SwordSlash>();
        private List<Achievement> _achQueue = new List<Achievement>();
        private Achievement _currentAch;
        private List<Quest> _quests = new List<Quest>();
        private List<ShopItem> _shopItems = new List<ShopItem>();
        private int _wave = 1, _waveKills, _waveGoal;
        private float _waveCooldown;
        private int _totalBossesDefeated;
        private const int BOSSES_TO_WIN = 3;
        private float _dayTime, _daySpeed = 0.003f;
        private Weather _weather = Weather.Clear;
        private float _weatherTimer;
        private float _welcomeTimer, _lightningTimer, _lightningAlpha;
        private bool _showLightning;
        private List<Particle> _welcomeParticles = new List<Particle>();
        private float _titleY = -120f, _menuItemsAlpha, _orbAngle, _levelTipTimer;
        private int _welcomeMenuSel;
        private string _levelTipText = "";
        private bool _musicEnabled = true;
        private SoundEffectInstance _musicInstance;
        private int _highestUnlockedStage = 1, _selectedLevel = 1;
        private float _levelSelectTimer;
        private string _levelSelectMessage = "";
        private bool _rotateScreen, _mobileMode = true;
        private bool _touchMoveLeft, _touchMoveRight, _touchJump, _touchAttack;
        private string _shopMessage = "";
        private float _shopMessageTimer;
        private int _stageIndex = 1, _stageTheme;
        private bool _stageAdvancePending;
        private float _stageClearTimer;
        private const int MAX_STAGE = 60;
        private float _endScreenTimer;
        private bool _endSoundPlayed;
        private KeyboardState _kPrev, _kCurr;
        private MouseState _mPrev, _mCurr;
        private float _globalTimer, _blinkTimer;
        private bool _blinkOn = true;
        private Vector2 _cavePos = Vector2.Zero;
        private int _caveHealth;
        private float _caveSpawnTimer, _levelUpTimer, _deathParticleTimer, _comboDisplayTimer;
        private int _lastCombo;
        private string[] _fruitKinds = { "Apple", "Banana", "Cherry", "Dragon Fruit", "Elderberry", "Fig", "Grape", "Kiwi", "Lemon", "Mango", "Orange", "Peach", "Quince", "Raspberry", "Strawberry" };
        private List<Rectangle> _riverRects = new List<Rectangle>();
        private List<Vector2> _bigTrees = new List<Vector2>();
        private List<float> _bigTreeSizes = new List<float>();
        private List<Vector2> _bushes = new List<Vector2>(), _flowers = new List<Vector2>(), _mushrooms = new List<Vector2>();
        private float _creditScrollY = BASE_H + 50f;
        private string[] _creditLines = { "", "JUNGLE ADVENTURE", "", "Developed By", "Dhurgham Alsaadi", "", "Game Design & Programming", "Procedural Art & Audio", "Gameplay Systems", "Level Design", "", "Special Thanks", "Monogame Framework", "The Open Source Community", "", "Version 2.0", "Gold Edition", "", "All Rights Reserved", "2026", "", "", "Thank you for playing!", "", "Press ESC to return" };
        private int _maxEnemiesPerFrame = 5;
        private List<Vector2> _enemySpawnPoints = new List<Vector2>();

        public Game1()
        {
            _gfx = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _gfx.PreferredBackBufferWidth = BASE_W;
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
                new ShopItem { Name="Health Potion", Description="Restore 40 HP  (stackable)",  Cost=25,  Icon="[POTN]",   TintColor=new Color(255,80,80) },
                new ShopItem { Name="Iron Shield",   Description="-30% incoming damage",        Cost=80,  Icon="[SHLD]",   TintColor=new Color(160,160,200) },
                new ShopItem { Name="Swift Boots",   Description="+40 speed  +jump height",     Cost=70,  Icon="[BOOT]",   TintColor=new Color(255,220,80)  },
                new ShopItem { Name="Bomb x3",       Description="Throw bomb  big AoE",          Cost=45,  Icon="[BOMB]",   TintColor=new Color(80,255,80)   },
                new ShopItem { Name="Max HP Up",     Description="+50 Max HP  permanent",       Cost=90,  Icon="[MXHP]",   TintColor=new Color(255,120,180) },
                new ShopItem { Name="Armor Upgrade", Description="+10 defense  stronger body",  Cost=120, Icon="[ARMR]",   TintColor=new Color(180,220,160) },
                new ShopItem { Name="VIP Gold Pack", Description="+250 gold  instant boost",    Cost=0,   Icon="[VIP++]",  TintColor=new Color(255,215,0)   },
            };
        }

        private void SetupQuests()
        {
            _quests = new List<Quest>
            {
                new Quest { Title="First Blood",   Desc="Kill 3 enemies",        Goal=3,  RewardGold=30  },
                new Quest { Title="Wave Warrior",  Desc="Survive 3 waves",       Goal=3,  RewardGold=60  },
                new Quest { Title="Rich Adventurer",Desc="Collect 200 coins",    Goal=200,RewardGold=50  },
                new Quest { Title="Level 5",       Desc="Reach player level 5",  Goal=5,  RewardGold=100 },
                new Quest { Title="Boss Slayer",   Desc="Defeat the first boss", Goal=1,  RewardGold=150 },
            };
        }

        private void GenerateWorld()
        {
            _stageTheme = Math.Min(5, (_stageIndex - 1) / 20);
            _trees.Clear(); _treeSizes.Clear(); _grasses.Clear(); _rocks.Clear();
            _platRects.Clear(); _fruits.Clear(); _riverRects.Clear(); _bigTrees.Clear();
            _bigTreeSizes.Clear(); _bushes.Clear(); _flowers.Clear(); _mushrooms.Clear();
            _enemySpawnPoints.Clear();

            int treeCount = 40 + (_stageIndex * 4);
            int grassCount = 60 + (_stageIndex * 8);
            int rockCount = 26 + (_stageIndex * 4);
            int platCount = 14 + (_stageIndex * 2);
            int houseCount = 2 + (_stageIndex / 18);

            // ---- Rivers with bridges across the world ----
            float river1X = Globals.WorldWidth * 0.30f;
            float river2X = Globals.WorldWidth * 0.60f;
            float river3X = Globals.WorldWidth * 0.78f;
            int riverW = 80;
            int riverY = (int)Globals.GroundY - 20;
            int riverH = 100;

            // River 1
            Rectangle r1 = new Rectangle((int)river1X, riverY, riverW, riverH);
            _riverRects.Add(r1);
            // Bridge over river 1
            _platRects.Add(new Rectangle((int)river1X - 10, riverY - 20, riverW + 20, 12));

            // River 2
            Rectangle r2 = new Rectangle((int)river2X, riverY, riverW, riverH);
            _riverRects.Add(r2);
            _platRects.Add(new Rectangle((int)river2X - 10, riverY - 20, riverW + 20, 12));

            // River 3 (later stages)
            if (_stageIndex >= 15)
            {
                Rectangle r3 = new Rectangle((int)river3X, riverY, riverW, riverH);
                _riverRects.Add(r3);
                _platRects.Add(new Rectangle((int)river3X - 10, riverY - 20, riverW + 20, 12));
            }

            // Trees
            for (int i = 0; i < treeCount; i++)
            {
                float x = _rng.Next(100, (int)Globals.WorldWidth - 100);
                _trees.Add(new Vector2(x, Globals.GroundY));
                _treeSizes.Add(0.75f + (float)_rng.NextDouble() * 0.7f);
            }

            // Big trees
            int bigTreeCount = 3 + (_stageIndex / 12);
            for (int i = 0; i < bigTreeCount; i++)
            {
                _bigTrees.Add(new Vector2(_rng.Next(300, (int)Globals.WorldWidth - 300), Globals.GroundY));
                _bigTreeSizes.Add(1.6f + (float)_rng.NextDouble() * 1.0f);
            }

            // Bushes
            int bushCount = 15 + (_stageIndex * 3);
            for (int i = 0; i < bushCount; i++)
                _bushes.Add(new Vector2(_rng.Next(50, (int)Globals.WorldWidth - 50), Globals.GroundY));

            // Flowers
            int flowerCount = 10 + (_stageIndex * 4);
            for (int i = 0; i < flowerCount; i++)
                _flowers.Add(new Vector2(_rng.Next(50, (int)Globals.WorldWidth - 50), Globals.GroundY));

            // Mushrooms
            int mushCount = 5 + (_stageIndex / 4);
            for (int i = 0; i < mushCount; i++)
                _mushrooms.Add(new Vector2(_rng.Next(50, (int)Globals.WorldWidth - 50), Globals.GroundY));

            for (int i = 0; i < grassCount; i++)
                _grasses.Add(new Vector2(_rng.Next(50, (int)Globals.WorldWidth - 50), Globals.GroundY));

            for (int i = 0; i < rockCount; i++)
                _rocks.Add(new Vector2(_rng.Next(100, (int)Globals.WorldWidth - 100), Globals.GroundY - 6));

            // Platforms
            for (int i = 0; i < platCount; i++)
            {
                int pw = _rng.Next(100, 240);
                int px = _rng.Next(200, (int)Globals.WorldWidth - 300);
                int py = (int)Globals.GroundY - _rng.Next(70, 200);
                _platRects.Add(new Rectangle(px, py, pw, 18));
            }

            // Fruits on platforms
            for (int i = 0; i < 15; i++)
            {
                int pw = 160, px = _rng.Next(150, (int)Globals.WorldWidth - 300);
                int py = (int)Globals.GroundY - _rng.Next(70, 150);
                _platRects.Add(new Rectangle(px, py, pw, 18));
                _fruits.Add(new FruitPickup { Position = new Vector2(px + pw / 2f, py - 16), Value = 5 + _rng.Next(0, 10), Kind = _fruitKinds[_rng.Next(_fruitKinds.Length)], Spin = (float)_rng.NextDouble() * MathHelper.TwoPi });
            }

            // ---- Enemy spawn points throughout the world ----
            int spawnCount = 6 + (_stageIndex * 3);
            for (int i = 0; i < spawnCount; i++)
            {
                float spX = _rng.Next(500, (int)Globals.WorldWidth - 200);
                _enemySpawnPoints.Add(new Vector2(spX, Globals.GroundY));
            }

            // Houses
            for (int h = 0; h < houseCount; h++)
            {
                if (_stageIndex >= 10 + h * 4)
                {
                    float hx = 600 + h * 500 + _rng.Next(0, 200);
                    if (hx > Globals.WorldWidth - 500) break;
                    int houseY = (int)Globals.GroundY - 96;
                    _platRects.Add(new Rectangle((int)hx - 18, houseY, 210, 18));
                    _platRects.Add(new Rectangle((int)hx + 34, houseY - 60, 92, 14));
                    _fruits.Add(new FruitPickup { Position = new Vector2(hx + 88, houseY - 78), Value = 10 + _stageIndex / 10, Kind = _fruitKinds[_rng.Next(_fruitKinds.Length)], Spin = 0f });
                }
            }
            if (_stageIndex >= 30)
            {
                int nestX = (int)Globals.WorldWidth - 900;
                int nestY = (int)Globals.GroundY - 188;
                _platRects.Add(new Rectangle(nestX, nestY, 180, 18));
                _platRects.Add(new Rectangle(nestX + 36, nestY - 54, 104, 12));
                _fruits.Add(new FruitPickup { Position = new Vector2(nestX + 78, nestY - 76), Value = 16 + _stageIndex / 8, Kind = "Nest Fruit", Spin = 0f });
            }
            if (_stageIndex >= 40)
            {
                int caveX = (int)Globals.WorldWidth - 780;
                int caveY = (int)Globals.GroundY - 120;
                _cavePos = new Vector2(caveX, caveY);
                _caveHealth = 250 + (_stageIndex - 40) * 30;
                _platRects.Add(new Rectangle(caveX - 30, caveY, 60, 12));
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
            // Limit spawned enemies per wave to prevent lag
            int count = Math.Min(12, 3 + waveNum);
            _waveGoal = count;
            bool isBossWave = (waveNum % 3 == 0);

            // ---- Spawn enemies throughout the world, not just near player ----
            // First, spawn some from pre-defined spawn points
            int enemyFromPoints = Math.Min(count / 2, _enemySpawnPoints.Count);
            for (int i = 0; i < enemyFromPoints; i++)
            {
                int idx = _rng.Next(0, _enemySpawnPoints.Count);
                Vector2 sp = _enemySpawnPoints[idx];
                EnemyType type = GetRandomEnemyTypeForStage();
                _enemies.Add(CreateEnemy(type, sp, waveNum));
            }

            // Rest near player
            for (int i = enemyFromPoints; i < count; i++)
            {
                EnemyType type = GetRandomEnemyTypeForStage();
                float spawnX = _player.Position.X + (float)(_rng.NextDouble() < 0.5 ? -1 : 1) * (_rng.Next(400, 1000));
                spawnX = MathHelper.Clamp(spawnX, 100, Globals.WorldWidth - 100);
                _enemies.Add(CreateEnemy(type, new Vector2(spawnX, Globals.GroundY), waveNum));
            }

            if (isBossWave)
            {
                float bx = _player.Position.X + (_rng.NextDouble() < 0.5 ? -1000 : 1000);
                bx = MathHelper.Clamp(bx, 200, Globals.WorldWidth - 200);
                var boss = CreateEnemy(EnemyType.Boss, new Vector2(bx, Globals.GroundY), waveNum);
                boss.IsBoss = true;
                _enemies.Add(boss);
                _snd.Play(_snd.BossRoar);
            }
            _waveKills = 0;
            _levelTipText = waveNum % 3 == 0 ? $"Boss wave {waveNum}!" : $"Wave {waveNum}!";
            _levelTipTimer = 3f;
        }

        private EnemyType GetRandomEnemyTypeForStage()
        {
            float r = (float)_rng.NextDouble();
            if (_stageIndex >= 40 && r < 0.10f) return EnemyType.Shadow;
            if (_stageIndex >= 28 && r < 0.15f) return EnemyType.Demon;
            if (_stageIndex >= 18 && r < 0.20f) return EnemyType.Giant;
            if (_stageIndex >= 8 && r < 0.25f) return EnemyType.Ogre;
            if (_stageIndex >= 28 && r < 0.35f) return EnemyType.Shielded;
            if (_stageIndex >= 18 && r < 0.45f) return EnemyType.Skeleton;
            if (_stageIndex >= 8 && r < 0.55f) return EnemyType.Goblin;
            return EnemyType.Zombie;
        }

        private Enemy CreateEnemy(EnemyType type, Vector2 pos, int wave)
        {
            float scale = 1f + wave * 0.06f;
            Enemy e = new Enemy { Type = type, Position = pos, Wave = wave };
            switch (type)
            {
                case EnemyType.Zombie:
                    e.MaxHealth = (50 + wave * 10) * scale; e.Speed = 65f + wave * 4f;
                    e.Damage = 8f + wave * 2f; e.AttackRange = 55f;
                    e.GoldDrop = _rng.Next(4, 12); e.XPDrop = 12 + wave * 3;
                    if (wave >= 3 && _rng.NextDouble() < 0.25) { e.Variant = 1; e.Speed *= 1.6f; e.MaxHealth *= 0.75f; }
                    else if (wave >= 4 && _rng.NextDouble() < 0.22) { e.Variant = 2; e.WidthScale = 1.26f; e.HeightScale = 1.18f; e.MaxHealth *= 1.7f; e.Damage *= 1.4f; }
                    break;
                case EnemyType.Goblin:
                    e.MaxHealth = (30 + wave * 7) * scale; e.Speed = 110f + wave * 7f;
                    e.Damage = 12f + wave * 2f; e.AttackRange = 45f;
                    e.GoldDrop = _rng.Next(6, 16); e.XPDrop = 18 + wave * 4;
                    break;
                case EnemyType.Skeleton:
                    e.MaxHealth = (70 + wave * 16) * scale; e.Speed = 55f + wave * 3f;
                    e.Damage = 16f + wave * 3f; e.AttackRange = 65f;
                    e.GoldDrop = _rng.Next(10, 20); e.XPDrop = 25 + wave * 5;
                    break;
                case EnemyType.Shielded:
                    e.MaxHealth = (110 + wave * 20) * scale; e.Speed = 50f + wave * 3f;
                    e.Damage = 18f + wave * 4f; e.AttackRange = 75f;
                    e.GoldDrop = _rng.Next(16, 28); e.XPDrop = 30 + wave * 6;
                    e.ShieldHealth = 50f + wave * 10f;
                    break;
                case EnemyType.Boss:
                    e.MaxHealth = (450 + wave * 70) * scale; e.Speed = 45f + wave * 4f;
                    e.Damage = 28f + wave * 5f; e.AttackRange = 90f;
                    e.GoldDrop = _rng.Next(70, 130); e.XPDrop = 180 + wave * 30;
                    break;
                case EnemyType.Ogre:
                    e.MaxHealth = (180 + wave * 30) * scale; e.Speed = 38f + wave * 3f;
                    e.Damage = 22f + wave * 4f; e.AttackRange = 70f;
                    e.GoldDrop = _rng.Next(22, 40); e.XPDrop = 45 + wave * 8;
                    e.WidthScale = 1.4f; e.HeightScale = 1.5f;
                    break;
                case EnemyType.Giant:
                    e.MaxHealth = (300 + wave * 50) * scale; e.Speed = 28f + wave * 2f;
                    e.Damage = 32f + wave * 6f; e.AttackRange = 100f;
                    e.GoldDrop = _rng.Next(35, 65); e.XPDrop = 70 + wave * 12;
                    e.WidthScale = 1.9f; e.HeightScale = 2.1f;
                    break;
                case EnemyType.Demon:
                    e.MaxHealth = (130 + wave * 25) * scale; e.Speed = 95f + wave * 10f;
                    e.Damage = 20f + wave * 3f; e.AttackRange = 50f;
                    e.GoldDrop = _rng.Next(18, 35); e.XPDrop = 35 + wave * 7;
                    e.WidthScale = 1.1f; e.HeightScale = 1.2f;
                    break;
                case EnemyType.Shadow:
                    e.MaxHealth = (80 + wave * 16) * scale; e.Speed = 130f + wave * 12f;
                    e.Damage = 16f + wave * 3f; e.AttackRange = 45f;
                    e.GoldDrop = _rng.Next(10, 22); e.XPDrop = 28 + wave * 5;
                    e.WidthScale = 0.85f; e.HeightScale = 0.85f;
                    break;
            }
            e.Health = e.MaxHealth;
            e.AttackCooldown = (type == EnemyType.Boss) ? 2.0f : 1.3f;
            return e;
        }

        private void SpawnWelcomeParticles()
        {
            for (int i = 0; i < 60; i++) _welcomeParticles.Add(MakeWelcomeParticle());
        }

        private Particle MakeWelcomeParticle()
        {
            return new Particle { Position = new Vector2(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H)), Velocity = new Vector2((float)_rng.NextDouble() * 30f - 15f, -(float)_rng.NextDouble() * 25f - 5f), Life = 2f + (float)_rng.NextDouble() * 6f, MaxLife = 8f, Size = 2f + (float)_rng.NextDouble() * 4f, Color = new Color(_rng.Next(30, 120), _rng.Next(160, 255), _rng.Next(30, 100), 200) };
        }

        protected override void LoadContent()
        {
            _sb = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _circleTex = new Texture2D(GraphicsDevice, 16, 16);
            var cd = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f;
                    cd[y * 16 + x] = (float)Math.Sqrt(dx * dx + dy * dy) < 7.5f ? Color.White : Color.Transparent;
                }
            _circleTex.SetData(cd);
            try { _font = Content.Load<SpriteFont>("Fonts/GameFont"); } catch { }
            _rt = new RenderTarget2D(GraphicsDevice, BASE_W, BASE_H);
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
            float dt = Math.Min((float)gt.ElapsedGameTime.TotalSeconds, 0.033f); // Cap delta time to prevent physics explosions
            _globalTimer += dt;
            _blinkTimer += dt;
            if (_blinkTimer > 0.5f) { _blinkOn = !_blinkOn; _blinkTimer = 0f; }
            if (KeyJustPressed(Keys.F11)) ToggleFullscreen();
            switch (State)
            {
                case GameState.Welcome: UpdateWelcome(dt); break;
                case GameState.LevelSelect: UpdateLevelSelect(dt); break;
                case GameState.Playing: UpdatePlaying(dt); break;
                case GameState.Shop: UpdateShop(dt); break;
                case GameState.FullMap: UpdateFullMap(); break;
                case GameState.StageClear: UpdateStageClear(dt); break;
                case GameState.Paused: UpdatePaused(); break;
                case GameState.Help: UpdateHelp(); break;
                case GameState.Credits: UpdateCredits(dt); break;
                case GameState.Win: UpdateEndScreen(dt); break;
                case GameState.Lose: UpdateEndScreen(dt); break;
            }
            base.Update(gt);
        }

        private void UpdateWelcome(float dt)
        {
            _welcomeTimer += dt; _orbAngle += dt * 0.6f;
            _titleY = MathHelper.Lerp(_titleY, 112f, dt * 4f);
            if (_welcomeTimer > 0.8f) _menuItemsAlpha = Math.Min(1f, _menuItemsAlpha + dt * 1.5f);
            _lightningTimer -= dt;
            if (_lightningTimer <= 0f) { _showLightning = true; _lightningAlpha = 0.7f; _lightningTimer = 4f + (float)_rng.NextDouble() * 8f; }
            if (_showLightning) { _lightningAlpha -= dt * 4f; if (_lightningAlpha <= 0f) { _showLightning = false; _lightningAlpha = 0f; } }
            for (int i = 0; i < _welcomeParticles.Count; i++) { var p = _welcomeParticles[i]; p.Position += p.Velocity * dt; p.Life -= dt; if (p.Life <= 0 || p.Position.Y > BASE_H + 30) p = MakeWelcomeParticle(); _welcomeParticles[i] = p; }
            if (KeyJustPressed(Keys.Up) || KeyJustPressed(Keys.W)) _welcomeMenuSel = (_welcomeMenuSel - 1 + 6) % 6;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _welcomeMenuSel = (_welcomeMenuSel + 1) % 6;
            if (KeyJustPressed(Keys.Enter)) ActivateWelcomeSelection(_welcomeMenuSel);
            for (int i = 0; i < 6; i++) { var r = WelcomeBtn(i); if (r.Contains(MousePos())) _welcomeMenuSel = i; if (MouseJustReleased() && r.Contains(MousePos())) ActivateWelcomeSelection(i); }
            if (KeyJustPressed(Keys.M) || (MouseJustReleased() && MusicButtonRect().Contains(MousePos()))) ToggleMusic();
        }

        private void ActivateWelcomeSelection(int idx)
        {
            switch (idx) { case 0: StartGame(); break; case 1: OpenShop(GameState.Welcome); break; case 2: GoSettings(); break; case 3: State = GameState.Help; break; case 4: State = GameState.Credits; _creditScrollY = BASE_H + 50f; break; case 5: Exit(); break; }
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
                if (_deathParticleTimer <= 0f && _player.DeathTimer < 1.2f)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        _particles.Add(new Particle { Position = _player.Position + new Vector2(_rng.Next(-20, 20), _rng.Next(-60, -10)), Velocity = new Vector2((float)_rng.NextDouble() * 140f - 70f, -(float)_rng.NextDouble() * 100f - 30f), Life = 0.8f + (float)_rng.NextDouble() * 0.5f, MaxLife = 1.3f, Size = 4f + (float)_rng.NextDouble() * 6f, Color = new Color(200, 40, 40, 200), RotVel = (float)_rng.NextDouble() * 8f - 4f });
                    }
                    _deathParticleTimer = 0.1f;
                }
                _deathParticleTimer -= dt;
                if (_player.DeathTimer < 1.0f) TriggerScreenShake(0.05f, 2f);
                if (_player.DeathTimer > 1.8f) State = GameState.Lose;
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

            // ---- River death check ----
            foreach (var river in _riverRects)
            {
                if (_player.Position.X > river.X && _player.Position.X < river.X + river.Width &&
                    _player.Position.Y > river.Y - 10 && _player.Position.Y < river.Y + river.Height)
                {
                    bool onBridge = false;
                    foreach (var plat in _platRects)
                        if (plat.Y < river.Y - 5 && _player.Position.X > plat.X && _player.Position.X < plat.Right && _player.Position.Y <= plat.Y + 5)
                        { onBridge = true; break; }
                    if (!onBridge)
                    {
                        _player.Health -= 999f;
                        _player.IsDead = true;
                        _snd.Play(_snd.LoseStinger);
                        break;
                    }
                }
            }

            if (KeyJustPressed(Keys.I) || KeyJustPressed(Keys.B)) OpenShop(GameState.Playing);
            if (KeyJustPressed(Keys.M)) State = GameState.FullMap;
            if (KeyJustPressed(Keys.Escape)) GoSettings();
            if (KeyJustPressed(Keys.Q) && _player.Bombs > 0) ThrowBomb();
            if (KeyJustPressed(Keys.H) && _player.HealthPotions > 0) { _player.HealthPotions--; _player.Health = Math.Min(_player.MaxHealth, _player.Health + 40f); SpawnDamageNumber(_player.Position, 40f, isHeal: true); }
            if (_totalBossesDefeated >= BOSSES_TO_WIN && State != GameState.Win) State = GameState.Win;
            if (_levelTipTimer > 0f) _levelTipTimer -= dt;

            if (_stageAdvancePending && _player.Position.X > Globals.WorldWidth - 420f)
            {
                _stageAdvancePending = false;
                _highestUnlockedStage = Math.Max(_highestUnlockedStage, Math.Min(MAX_STAGE, _stageIndex + 1));
                _stageClearTimer = 0f;
                if (_stageIndex >= MAX_STAGE) { State = GameState.Win; return; }
                _snd.Play(_snd.LevelUp);
                State = GameState.StageClear;
            }
        }

        private void UpdateDayNight(float dt) { _dayTime = (_dayTime + _daySpeed * dt) % 1f; }

        private void UpdateWeather(float dt)
        {
            _weatherTimer -= dt;
            if (_weatherTimer <= 0f) { _weather = (Weather)_rng.Next(3); _weatherTimer = 20f + (float)_rng.NextDouble() * 40f; }
            if (_weather == Weather.Rain || _weather == Weather.Storm)
            {
                int sc = _weather == Weather.Storm ? 8 : 3;
                for (int i = 0; i < sc; i++)
                    _rainDrops.Add(new Particle { Position = new Vector2(_cam.X + _rng.Next(0, BASE_W), _cam.Y - 20), Velocity = new Vector2(-30f, 500f + (_weather == Weather.Storm ? 200f : 0f)), Life = 1f, MaxLife = 1f, Size = 1.5f, Color = new Color(160, 190, 220, 160) });
            }
            for (int i = _rainDrops.Count - 1; i >= 0; i--) { var r = _rainDrops[i]; r.Position += r.Velocity * dt; r.Life -= dt; if (r.Life <= 0f || r.Position.Y > _cam.Y + BASE_H + 10) _rainDrops.RemoveAt(i); else _rainDrops[i] = r; }
        }

        private void UpdatePlayerMovement(float dt)
        {
            float spd = _player.MoveSpeed, dx = 0f;
            if (_kCurr.IsKeyDown(Keys.A) || _kCurr.IsKeyDown(Keys.Left) || _touchMoveLeft) { dx = -spd; _player.FacingRight = false; }
            if (_kCurr.IsKeyDown(Keys.D) || _kCurr.IsKeyDown(Keys.Right) || _touchMoveRight) { dx = spd; _player.FacingRight = true; }
            _player.Position.X += dx * dt;
            _player.Position.X = MathHelper.Clamp(_player.Position.X, 20, Globals.WorldWidth - 20);

            if ((KeyJustPressed(Keys.W) || KeyJustPressed(Keys.Up) || _touchJump) && _player.OnGround)
            {
                _player.VelocityY = _player.JumpPower * (_player.HasBoots ? 1.25f : 1f);
                _player.OnGround = false;
            }
            _player.VelocityY += 980f * dt;
            _player.Position.Y += _player.VelocityY * dt;
            _player.OnGround = false;

            foreach (var plat in _platRects)
            {
                if (_player.VelocityY >= 0f && _player.Position.X > plat.X && _player.Position.X < plat.Right &&
                    _player.Position.Y >= plat.Y && _player.Position.Y <= plat.Y + _player.VelocityY * dt + 10f)
                { _player.Position.Y = plat.Y; _player.VelocityY = 0f; _player.OnGround = true; }
            }
            if (_player.Position.Y >= Globals.GroundY) { _player.Position.Y = Globals.GroundY; _player.VelocityY = 0f; _player.OnGround = true; }

            if (Math.Abs(dx) > 0.1f) _player.WalkAnim += dt * 8f;
            _player.Stamina = Math.Min(_player.MaxStamina, _player.Stamina + 20f * dt);
            if (_player.IFrameTimer > 0f) _player.IFrameTimer -= dt;
            if (_player.ComboWindow > 0f) { _player.ComboWindow -= dt; if (_player.ComboWindow <= 0f) _player.ComboHits = 0; }

            // ---- Enemy collision: push player back if touching enemy ----
            foreach (var en in _enemies)
            {
                if (en.IsDead || !en.IsActive) continue;
                float dist = Vector2.Distance(_player.Position, en.Position + new Vector2(0, -40));
                if (dist < 35f && Math.Abs(_player.Position.Y - (en.Position.Y - 40)) < 50f)
                {
                    float pushDir = _player.Position.X < en.Position.X ? -1f : 1f;
                    _player.Position.X += pushDir * 60f * dt;
                    _player.Position.X = MathHelper.Clamp(_player.Position.X, 20, Globals.WorldWidth - 20);
                    break;
                }
            }
        }

        private void UpdatePlayerCombat(float dt)
        {
            if (_player.AttackTimer > 0f) _player.AttackTimer -= dt;
            if (_player.AttackCooldown > 0f) _player.AttackCooldown -= dt;
            if (_player.AttackTimer <= 0f) _player.IsAttacking = false;
            bool attackPressed = KeyJustPressed(Keys.Z) || KeyJustPressed(Keys.X) || MouseJustReleased() || _touchAttack;
            if (attackPressed && _player.CanAttack) PerformPlayerAttack();
        }

        private void UpdateMobileControls()
        {
            _touchMoveLeft = _touchMoveRight = _touchJump = _touchAttack = false;
            if (!_mobileMode) return;
            foreach (var touch in TouchPanel.GetState()) ApplyMobilePoint(new Point((int)touch.Position.X, (int)touch.Position.Y));
            if (_mCurr.LeftButton == ButtonState.Pressed) ApplyMobilePoint(_mCurr.Position);
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
            if (_player.Weapon == WeaponType.Pistol) { FirePistolShot(); return; }
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

            // Minimal slash effect - reduce GC pressure
            if (_slashes.Count < 3)
            {
                var slash = new SwordSlash { Origin = _player.Position + new Vector2(_player.FacingRight ? 20 : -20, -50), Angle = _player.FacingRight ? -MathHelper.Pi * 0.6f : MathHelper.Pi * 1.6f, ArcSpan = MathHelper.Pi * 0.85f, Radius = 70f, FacingRight = _player.FacingRight };
                _slashes.Add(slash);
            }

            float reach = _player.Weapon == WeaponType.MagicStaff ? 110f : 75f;
            bool didHit = false;
            Vector2 playerPos = _player.Position;
            float playerX = playerPos.X;
            bool facingRight = _player.FacingRight;

            for (int ei = _enemies.Count - 1; ei >= 0; ei--)
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
                        for (int ej = _enemies.Count - 1; ej >= 0; ej--) { var en2 = _enemies[ej]; if (!en2.IsDead && Math.Abs(en2.Position.X - en.Position.X) < 100f && Math.Abs(en2.Position.Y - en.Position.Y) < 100f) DamageEnemy(en2, dmg * 0.6f, false); }
                    }
                    DamageEnemy(en, dmg, crit);
                    didHit = true;
                }
            }
            if (didHit) { _snd.PlayRandom(_snd.EnemyHit, 0.6f, 1f, 0.18f); TriggerScreenShake(0.08f, 3f); }
        }

        private void FirePistolShot()
        {
            _player.IsAttacking = true; _player.AttackTimer = 0.12f; _player.AttackCooldown = 0.25f;
            _snd.PlayRandom(_snd.SwordSwing, 0.4f, 0.7f, 0.12f);
            if (_projectiles.Count < 5)
                _projectiles.Add(new Projectile { Position = _player.Position + new Vector2(_player.FacingRight ? 28 : -28, -58), Velocity = new Vector2(_player.FacingRight ? 900f : -900f, -8f), Damage = _player.BaseDamage * 1.05f, Color = new Color(230, 230, 230) });
        }

        private Color GetWeaponColor(WeaponType w)
        {
            return w switch { WeaponType.Sword => new Color(200, 230, 255), WeaponType.Axe => new Color(255, 140, 60), WeaponType.MagicStaff => new Color(180, 80, 255), WeaponType.Pistol => new Color(220, 220, 220), _ => new Color(255, 220, 150) };
        }

        private void DamageEnemy(Enemy en, float dmg, bool crit)
        {
            if (en.ShieldHealth > 0f) { float shieldHit = Math.Min(en.ShieldHealth, dmg * 0.85f); en.ShieldHealth -= shieldHit; dmg *= 0.38f; if (en.ShieldHealth <= 0f) QueueAchievement("SHIELD BROKEN!", "The guardian is now exposed."); }
            en.TakeDamage(dmg);
            if (en.Type == EnemyType.Zombie) _snd.PlayRandom(_snd.ZombieGroan, 0.22f, 0.42f, 0.08f);
            en.KnockbackX = (_player.FacingRight ? 1f : -1f) * (crit ? 280f : 160f);
            SpawnDamageNumber(en.Position + new Vector2(0, -40), dmg, false, crit);
            if (en.IsDead) OnEnemyKilled(en);
        }

        private void OnEnemyKilled(Enemy en)
        {
            _waveKills++;
            _snd.PlayRandom(_snd.EnemyDeath);
            _player.Gold += en.GoldDrop;
            _player.FruitCount += Math.Max(1, en.GoldDrop / 4);
            _snd.Play(_snd.CoinPickup);
            bool leveledUp;
            _player.AddXP(en.XPDrop, out leveledUp);
            if (leveledUp) { _levelUpTimer = 2.5f; _snd.Play(_snd.LevelUp); TriggerScreenShake(0.2f, 5f); QueueAchievement($"LEVEL {_player.Level}!", "You levelled up!"); }
            if (en.IsBoss) { _totalBossesDefeated++; TriggerScreenShake(0.6f, 12f); QueueAchievement("BOSS DEFEATED!", $"Wave {_wave} boss slain!"); _snd.Play(_snd.CoinPickup); var q = _quests.Find(x => x.Title == "Boss Slayer"); if (q != null && !q.Complete) q.Progress++; }
            var qfb = _quests.Find(x => x.Title == "First Blood"); if (qfb != null && !qfb.Complete) qfb.Progress++;
        }

        private void UpdateEnemies(float dt)
        {
            // Use indexed for loop (NOT foreach) to avoid enumeration modification issues
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var en = _enemies[i];
                if (!en.IsActive || en == null) { _enemies.RemoveAt(i); continue; }
                if (en.IsDead)
                {
                    en.DeathTimer += dt;
                    if (en.DeathTimer > 1.2f) { _enemies.RemoveAt(i); continue; }
                    en.Position.Y += 30f * dt;
                    en.Position.X += en.FacingRight ? 8f * dt : -8f * dt;
                    continue;
                }
                if (en.HitFlash > 0f) en.HitFlash -= dt;
                if (Math.Abs(en.KnockbackX) > 1f) { en.Position.X += en.KnockbackX * dt; en.KnockbackX = MathHelper.Lerp(en.KnockbackX, 0f, dt * 10f); }
                float xDiff = _player.Position.X - en.Position.X;
                en.FacingRight = xDiff > 0;
                if (Math.Abs(xDiff) > en.AttackRange)
                {
                    float spd = en.Speed * (en.IsBoss && en.Health < en.MaxHealth * 0.5f ? 1.4f : 1f);
                    en.Position.X += Math.Sign(xDiff) * spd * dt;
                    en.WalkAnim += dt * (spd / 70f) * 6f;
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
                        TriggerScreenShake(0.15f, 5f);
                        if (_player.Health <= 0f) { _player.Health = 0f; _player.IsDead = true; _snd.Play(_snd.LoseStinger); }
                    }
                }
                // Cave spawner
                if (_caveHealth > 0) { _caveSpawnTimer -= dt; if (_caveSpawnTimer <= 0f && _enemies.Count < 20) { _enemies.Add(CreateEnemy(EnemyType.Zombie, new Vector2(_cavePos.X, Globals.GroundY), 1)); _caveSpawnTimer = 3f + (float)_rng.NextDouble() * 2f; } }
            }
        }

        private void UpdateWaveSystem(float dt)
        {
            int alive = 0;
            for (int i = 0; i < _enemies.Count; i++) if (!_enemies[i].IsDead && _enemies[i].IsActive) alive++;
            if (alive == 0)
            {
                _waveCooldown -= dt;
                if (_waveCooldown <= 0f)
                {
                    _wave++;
                    SpawnWave(_wave);
                    QueueAchievement($"WAVE {_wave}!", "New enemy wave incoming!");
                    var q = _quests.Find(x => x.Title == "Wave Warrior"); if (q != null && !q.Complete) q.Progress++;
                }
                else if (_waveCooldown > 4f) _waveCooldown = 5f;
            }
        }

        private void UpdateQuests()
        {
            foreach (var q in _quests)
            {
                if (q.Complete) continue;
                switch (q.Title) { case "Rich Adventurer": q.Progress = _player.Gold; break; case "Level 5": q.Progress = _player.Level; break; }
                if (q.Progress >= q.Goal && !q.Complete) { q.Complete = true; _player.Gold += q.RewardGold; QueueAchievement("QUEST DONE!", $"{q.Title}: +{q.RewardGold} gold"); }
            }
        }

        private void UpdateAchievements(float dt)
        {
            if (_currentAch != null) { _currentAch.ShowTimer -= dt; if (_currentAch.ShowTimer <= 0f) { _currentAch.Shown = true; _currentAch = null; } }
            if (_currentAch == null && _achQueue.Count > 0) { _currentAch = _achQueue[0]; _achQueue.RemoveAt(0); }
        }

        private void QueueAchievement(string title, string desc) { if (_achQueue.Count < 5) _achQueue.Add(new Achievement { Title = title, Desc = desc }); }

        private void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Position += p.Velocity * dt; p.Velocity.Y += 300f * dt;
                p.Life -= dt; p.Rotation += p.RotVel * dt;
                if (p.Life <= 0f) _particles.RemoveAt(i); else _particles[i] = p;
            }
            // Cap particles to prevent lag
            if (_particles.Count > 100) _particles.RemoveRange(0, _particles.Count - 100);
        }

        private void UpdateDamageNumbers(float dt)
        {
            for (int i = _dmgNums.Count - 1; i >= 0; i--) { var n = _dmgNums[i]; n.Position.Y -= 55f * dt; n.Life -= dt; if (n.Life <= 0f) _dmgNums.RemoveAt(i); else _dmgNums[i] = n; }
            if (_dmgNums.Count > 30) _dmgNums.RemoveRange(0, _dmgNums.Count - 30);
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i]; p.Position += p.Velocity * dt; p.Life -= dt;
                bool remove = p.Life <= 0f || p.Position.X < -100 || p.Position.X > Globals.WorldWidth + 100;
                if (!remove)
                {
                    for (int e = _enemies.Count - 1; e >= 0; e--)
                    {
                        var en = _enemies[e]; if (!en.IsActive || en.IsDead) continue;
                        if (Vector2.Distance(en.Position + new Vector2(0, -40), p.Position) < 34f)
                        { DamageEnemy(en, p.Damage * (_rng.NextDouble() < _player.CritChance * 0.75f ? 1.8f : 1f), false); remove = true; break; }
                    }
                }
                if (remove) _projectiles.RemoveAt(i); else _projectiles[i] = p;
            }
        }

        private void UpdateFloatingCoins(float dt)
        {
            for (int i = _coins.Count - 1; i >= 0; i--) { var c = _coins[i]; c.Position += c.Velocity * dt; c.Velocity.Y += 400f * dt; c.Life -= dt; if (c.Life <= 0f) _coins.RemoveAt(i); else _coins[i] = c; }
        }

        private void UpdateFruits(float dt)
        {
            for (int i = _fruits.Count - 1; i >= 0; i--)
            {
                var f = _fruits[i]; f.Spin += dt * 3f;
                if (Vector2.Distance(f.Position, _player.Position + new Vector2(0, -50)) < 30f)
                { _player.Gold += f.Value; _player.FruitCount += f.Value; _snd.Play(_snd.CoinPickup); _shopMessage = $"Collected {f.Kind}: +{f.Value} gold"; _shopMessageTimer = 1.8f; _fruits.RemoveAt(i); }
                else _fruits[i] = f;
            }
        }

        private void UpdateSlashes(float dt)
        {
            for (int i = _slashes.Count - 1; i >= 0; i--)
            {
                var s = _slashes[i]; s.Life -= dt;
                if (s.Life <= 0f) _slashes.RemoveAt(i); else _slashes[i] = s;
            }
        }

        private void UpdateCamera(float dt)
        {
            Vector2 target = _player.Position - new Vector2(BASE_W * 0.45f, BASE_H * 0.62f);
            target.X = MathHelper.Clamp(target.X, 0, Globals.WorldWidth - BASE_W);
            target.Y = MathHelper.Clamp(target.Y, 0, Globals.WorldHeight - BASE_H);
            _cam = Vector2.Lerp(_cam, target, dt * 6f);
            if (_shakeTimer > 0f) { _shakeTimer -= dt; _cam += new Vector2(((float)_rng.NextDouble() * 2f - 1f) * _shakeIntensity, ((float)_rng.NextDouble() * 2f - 1f) * _shakeIntensity); }
        }

        private void UpdateShop(float dt)
        {
            if (_shopMessageTimer > 0f) _shopMessageTimer -= dt;
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.I) || KeyJustPressed(Keys.B)) { State = _shopReturnState; return; }
            if (KeyJustPressed(Keys.Up) || KeyJustPressed(Keys.W)) _shopSelected = (_shopSelected - 1 + _shopItems.Count) % _shopItems.Count;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _shopSelected = (_shopSelected + 1) % _shopItems.Count;
            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Z)) TryBuyItem(_shopSelected);
            for (int i = 0; i < _shopItems.Count; i++) { var r = ShopItemRect(i); if (r.Contains(MousePos())) _shopSelected = i; if (MouseJustReleased() && r.Contains(MousePos())) TryBuyItem(i); }
        }

        private void TryBuyItem(int idx)
        {
            var item = _shopItems[idx];
            if (item.Name == "VIP Gold Pack" && item.Purchased) return;
            if (_player.Gold < item.Cost) { _shopMessage = "Not enough money!"; _shopMessageTimer = 2f; return; }
            if (item.Cost > 0) _player.Gold -= item.Cost;
            _snd.Play(_snd.ShopBuy);
            switch (item.Name)
            {
                case "Iron Sword": _player.Weapon = WeaponType.Sword; _player.BaseDamage = Math.Max(_player.BaseDamage, 20f); break;
                case "Battle Axe": _player.Weapon = WeaponType.Axe; _player.BaseDamage = Math.Max(_player.BaseDamage, 35f); _player.AttackDuration = 0.55f; break;
                case "Magic Staff": _player.Weapon = WeaponType.MagicStaff; _player.BaseDamage = Math.Max(_player.BaseDamage, 28f); break;
                case "Hunter Pistol": _player.Weapon = WeaponType.Pistol; _player.BaseDamage = Math.Max(_player.BaseDamage, 18f); break;
                case "Health Potion": _player.HealthPotions++; break;
                case "Iron Shield": _player.HasShield = true; _player.Defense = Math.Max(_player.Defense, 30f); break;
                case "Swift Boots": _player.HasBoots = true; _player.MoveSpeed = Math.Max(_player.MoveSpeed, 260f); break;
                case "Bomb x3": _player.Bombs += 3; break;
                case "Max HP Up": _player.MaxHealth += 50f; _player.Health = Math.Min(_player.MaxHealth, _player.Health + 50f); break;
                case "Armor Upgrade": _player.ArmorLevel++; _player.Defense = Math.Max(_player.Defense, 10f + _player.ArmorLevel * 10f); _player.MaxHealth += 10f; _player.Health = Math.Min(_player.MaxHealth, _player.Health + 10f); break;
                case "VIP Gold Pack": _player.Gold += 250; item.Purchased = true; break;
            }
            _shopMessage = $"Purchased {item.Name}."; _shopMessageTimer = 1.8f;
        }

        private void ThrowBomb()
        {
            if (_player.Bombs <= 0) return;
            _player.Bombs--;
            float bx = _player.Position.X + (_player.FacingRight ? 250f : -250f);
            foreach (var en in _enemies) { if (!en.IsDead && Math.Abs(en.Position.X - bx) < 150f && Math.Abs(en.Position.Y - _player.Position.Y) < 120f) DamageEnemy(en, 60f + _player.Level * 5f, false); }
            TriggerScreenShake(0.3f, 8f);
        }

        private void UpdateFullMap() { if (KeyJustPressed(Keys.M) || KeyJustPressed(Keys.Escape)) State = GameState.Playing; }

        private void UpdatePaused()
        {
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter)) { State = _pauseReturn; return; }
            if (KeyJustPressed(Keys.Up) || KeyJustPressed(Keys.W)) _settingsSelected = (_settingsSelected - 1 + 6) % 6;
            if (KeyJustPressed(Keys.Down) || KeyJustPressed(Keys.S)) _settingsSelected = (_settingsSelected + 1) % 6;
            if (KeyJustPressed(Keys.Left) || KeyJustPressed(Keys.Right) || KeyJustPressed(Keys.Space))
            {
                switch (_settingsSelected)
                {
                    case 0: ToggleFullscreen(); break; case 1: ApplyWindowPreset(960, 540); break;
                    case 2: ApplyWindowPreset(1280, 720); break; case 3: _rotateScreen = !_rotateScreen; break;
                    case 4: _mobileMode = !_mobileMode; break; case 5: State = _pauseReturn; break;
                }
            }
            if (KeyJustPressed(Keys.F11)) ToggleFullscreen();
        }

        private void UpdateHelp()
        {
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || (MouseJustReleased() && BackButtonRect().Contains(MousePos()))) State = GameState.Welcome;
        }

        private void UpdateCredits(float dt)
        {
            _creditScrollY -= dt * 45f; // Scroll up
            if (_creditScrollY < -(_creditLines.Length * 45f + 100f)) _creditScrollY = BASE_H + 50f;
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || (MouseJustReleased() && BackButtonRect().Contains(MousePos())))
            { State = GameState.Welcome; _creditScrollY = BASE_H + 50f; }
        }

        private void UpdateLevelSelect(float dt)
        {
            if (_levelSelectTimer > 0f) _levelSelectTimer -= dt;
            if (KeyJustPressed(Keys.Escape) || KeyJustPressed(Keys.Back)) { State = GameState.Welcome; return; }
            if (MouseJustReleased() && BackButtonRect().Contains(MousePos())) { State = GameState.Welcome; return; }
            if (KeyJustPressed(Keys.Left)) _selectedLevel = Math.Max(1, _selectedLevel - 1);
            if (KeyJustPressed(Keys.Right)) _selectedLevel = Math.Min(MAX_STAGE, _selectedLevel + 1);
            if (KeyJustPressed(Keys.Up)) _selectedLevel = Math.Max(1, _selectedLevel - 10);
            if (KeyJustPressed(Keys.Down)) _selectedLevel = Math.Min(MAX_STAGE, _selectedLevel + 10);
            if (KeyJustPressed(Keys.M) || (MouseJustReleased() && MusicButtonRect().Contains(MousePos()))) ToggleMusic();
            var mouse = MousePos();
            for (int level = 1; level <= MAX_STAGE; level++) { if (LevelCellRect(level).Contains(mouse)) _selectedLevel = level; }
            bool selectPressed = KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space);
            bool selectClicked = MouseJustReleased() && LevelCellRect(_selectedLevel).Contains(mouse);
            if ((selectPressed || selectClicked) && _selectedLevel <= _highestUnlockedStage) { LoadStage(_selectedLevel); State = GameState.Playing; }
            else if ((selectPressed || selectClicked) && _selectedLevel > _highestUnlockedStage) { _levelSelectMessage = "That stage is locked. Clear earlier stages first."; _levelSelectTimer = 2f; }
        }

        private void UpdateStageClear(float dt)
        {
            _stageClearTimer += dt;
            if (_stageClearTimer < 1.5f) return;
            if (_stageIndex >= MAX_STAGE) { State = GameState.Win; return; }
            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space) || _stageClearTimer > 2.6f) { AdvanceStage(); State = GameState.Playing; }
        }

        private void UpdateEndScreen(float dt)
        {
            _endScreenTimer += dt;
            if (!_endSoundPlayed) { _endSoundPlayed = true; if (State == GameState.Win) _snd.Play(_snd.WinJingle); if (State == GameState.Lose) _snd.Play(_snd.LoseStinger); }
            if (KeyJustPressed(Keys.Enter) || KeyJustPressed(Keys.Space)) RestartGame();
        }

        private void RestartGame()
        {
            _player = new Player { Position = new Vector2(300, Globals.GroundY) };
            _totalBossesDefeated = 0; _endScreenTimer = 0f; _endSoundPlayed = false; _cam = Vector2.Zero;
            _highestUnlockedStage = Math.Max(_highestUnlockedStage, 1);
            LoadStage(1); State = GameState.LevelSelect;
        }

        private void AdvanceStage()
        {
            _stageAdvancePending = false;
            if (_stageIndex >= MAX_STAGE) { State = GameState.Win; return; }
            LoadStage(_stageIndex + 1);
        }

        private void LoadStage(int stage)
        {
            _stageIndex = Math.Max(1, Math.Min(MAX_STAGE, stage));
            _player.Position = new Vector2(300, Globals.GroundY);
            _player.Health = _player.MaxHealth; _player.Stamina = _player.MaxStamina;
            _player.VelocityY = 0f; _player.OnGround = true; _player.IsAttacking = false;
            _player.AttackTimer = 0f; _player.AttackCooldown = 0f;
            _enemies.Clear(); _particles.Clear(); _dmgNums.Clear(); _coins.Clear(); _slashes.Clear(); _projectiles.Clear(); _rainDrops.Clear();
            _wave = 1; _waveKills = 0;
            _stageAdvancePending = true; _stageClearTimer = 0f;
            SetupShop(); SetupQuests(); GenerateWorld(); SpawnWave(_wave);
            string[] stageNames = { "The Forgotten Jungle", "The Dark Swamp", "The Crystal Caves", "The Ancient Ruins", "The Volcanic Depths", "The Abyssal Realm" };
            int nameIdx = Math.Min((_stageIndex - 1) / 20, stageNames.Length - 1);
            _levelTipText = $"Stage {_stageIndex}: {stageNames[nameIdx]}";
            _levelTipTimer = 4f;
        }

        protected override void Draw(GameTime gt)
        {
            if (_rt == null) return;
            GraphicsDevice.SetRenderTarget(_rt);
            switch (State)
            {
                case GameState.Welcome: GraphicsDevice.Clear(new Color(4, 10, 20)); DrawWelcome(gt); break;
                case GameState.LevelSelect: GraphicsDevice.Clear(new Color(8, 18, 10)); DrawLevelSelect(); break;
                case GameState.Playing: GraphicsDevice.Clear(new Color(18, 45, 22)); DrawPlaying(gt); break;
                case GameState.Shop: GraphicsDevice.Clear(new Color(20, 12, 5)); DrawShop(); break;
                case GameState.FullMap: GraphicsDevice.Clear(new Color(10, 22, 10)); DrawFullMap(); break;
                case GameState.StageClear: GraphicsDevice.Clear(new Color(10, 24, 12)); DrawStageClear(); break;
                case GameState.Paused: GraphicsDevice.Clear(new Color(10, 18, 28)); DrawPaused(); break;
                case GameState.Win: GraphicsDevice.Clear(new Color(4, 20, 8)); DrawWin(); break;
                case GameState.Lose: GraphicsDevice.Clear(new Color(20, 4, 4)); DrawLose(); break;
                case GameState.Help: GraphicsDevice.Clear(new Color(8, 18, 12)); DrawHelpScreen(); break;
                case GameState.Credits: GraphicsDevice.Clear(new Color(4, 8, 20)); DrawCreditsScreen(); break;
            }
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (_rotateScreen)
            {
                float bbW = GraphicsDevice.PresentationParameters.BackBufferWidth;
                float bbH = GraphicsDevice.PresentationParameters.BackBufferHeight;
                float scale = Math.Min(bbW / _rt.Width, bbH / _rt.Height);
                _sb.Draw(_rt, new Vector2(bbW * 0.5f, bbH * 0.5f), null, Color.White, MathHelper.PiOver2, new Vector2(_rt.Width / 2f, _rt.Height / 2f), scale, SpriteEffects.None, 0);
            }
            else
            {
                _sb.Draw(_rt, _rtRect.Width > 0 ? _rtRect : new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight), Color.White);
            }
            _sb.End();
            base.Draw(gt);
        }

        // =====================================================================
        //  DRAW METHODS
        // =====================================================================

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
            int pw = 780, ph = 520, px = (BASE_W - pw) / 2, py = (BASE_H - ph) / 2;
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
                float tx = (BASE_W - tSize.X) / 2f, ty = _titleY;
                for (int g = 5; g >= 1; g--)
                {
                    float ga = 0.04f * glow; Color gc = new Color(80, 220, 90) * ga;
                    for (int d = 0; d < 4; d++) { float ang2 = d * MathHelper.PiOver2 + _globalTimer * 0.5f; _sb.DrawString(_font, title, new Vector2(tx + (float)Math.Cos(ang2) * g * 2, ty + (float)Math.Sin(ang2) * g * 2), gc, 0, Vector2.Zero, tScale, SpriteEffects.None, 0); }
                }
                _sb.DrawString(_font, title, new Vector2(tx + 4, ty + 4), Color.Black * 0.55f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, title, new Vector2(tx, ty), new Color(255, 245, 200), 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                string sub = "Explore  •  Build  •  Fight  •  Survive";
                Vector2 subSize = _font.MeasureString(sub);
                _sb.DrawString(_font, sub, new Vector2((BASE_W - subSize.X) / 2f, ty + tSize.Y + 6f), new Color(120, 200, 130) * _menuItemsAlpha);
                DrawRect(new Rectangle(BASE_W / 2 - 140, (int)(ty + tSize.Y + 32f), 280, 1), new Color(80, 160, 90, 140));
                string[] menuLabels = { "  START GAME", "  SHOP", "  SETTINGS", "  HELP", "  CREDITS", "  EXIT" };
                for (int mi = 0; mi < menuLabels.Length; mi++)
                {
                    var r = WelcomeBtn(mi);
                    bool hov = mi == _welcomeMenuSel || r.Contains(MousePos());
                    bool pressed = hov && _mCurr.LeftButton == ButtonState.Pressed;
                    Color btnBase = pressed ? new Color(80, 170, 90) : hov ? new Color(30, 100, 40) : new Color(10, 36, 18);
                    Color btnAccent = mi == 0 ? new Color(80, 255, 120) : new Color(140, 200, 255);
                    float alphaMult = _menuItemsAlpha;
                    if (hov) { float p2 = 0.4f + 0.3f * (float)Math.Sin(_globalTimer * 4f); DrawRect(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), btnAccent * p2 * 0.3f * alphaMult); }
                    DrawRect(r, btnBase * alphaMult);
                    DrawRect(new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), Color.Lerp(btnBase, btnAccent, hov ? 0.35f : 0.08f) * alphaMult);
                    DrawRect(new Rectangle(r.X, r.Y, r.Width, 2), btnAccent * (hov ? 0.9f : 0.4f) * alphaMult);
                    if (mi == _welcomeMenuSel) _sb.DrawString(_font, ">", new Vector2(r.X + 12, r.Y + 11), btnAccent * alphaMult);
                    Vector2 ls = _font.MeasureString(menuLabels[mi]);
                    _sb.DrawString(_font, menuLabels[mi], new Vector2(r.X + (r.Width - ls.X) / 2f, r.Y + 12), Color.White * alphaMult);
                }
                float chipY = py + ph - 80f;
                string[] chips = { "WASD/Arrows:Move", "SPACE/Z:Attack", "I/B:Shop", "M:Map", "Q:Bomb", "H:Potion", "ESC:Settings" };
                float chipX = px + 14f;
                foreach (var chip in chips) { Vector2 cs = _font.MeasureString(chip) * 0.75f; DrawRect(new Rectangle((int)chipX - 4, (int)chipY - 2, (int)cs.X + 8, (int)cs.Y + 4), new Color(0, 0, 0, 100)); _sb.DrawString(_font, chip, new Vector2(chipX, chipY), Color.White * 0.75f * _menuItemsAlpha, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0); chipX += cs.X + 18f; }
                if (_blinkOn && _menuItemsAlpha > 0.8f) { string pe = "PRESS ENTER TO BEGIN YOUR ADVENTURE"; Vector2 pes = _font.MeasureString(pe) * 0.9f; _sb.DrawString(_font, pe, new Vector2((BASE_W - pes.X) / 2f, BASE_H - 38f), Color.Yellow * 0.9f, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0); }
                var musicR = MusicButtonRect();
                DrawRect(musicR, _musicEnabled ? new Color(20, 100, 55, 220) : new Color(120, 40, 40, 220));
                DrawRect(new Rectangle(musicR.X, musicR.Y, musicR.Width, 2), new Color(255, 255, 255, 120));
                _sb.DrawString(_font, _musicEnabled ? "MUSIC: ON" : "MUSIC: OFF", new Vector2(musicR.X + 16, musicR.Y + 10), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            }
            _sb.End();
        }

        private void DrawLevelSelect()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(6, 18, 10), new Color(18, 44, 20), BASE_H);
            for (int i = 0; i < 28; i++) { float x = 80 + (i * 43) % 1120; float y = 90 + (i * 23) % 520 + (float)Math.Sin(_globalTimer * 1.4f + i) * 8f; DrawCircle(x, y, 3f + (i % 3), new Color(120, 255, 150, 40)); }
            int pw = 1120, ph = 560, ppx = (BASE_W - pw) / 2, ppy = 84;
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
            int gridX = ppx + 26, gridY = ppy + 88, cellW = 104, cellH = 42, gap = 6;
            var mouse = MousePos();
            for (int level = 1; level <= MAX_STAGE; level++)
            {
                int idx = level - 1, col = idx % 10, row = idx / 10;
                Rectangle cell = new Rectangle(gridX + col * (cellW + gap), gridY + row * (cellH + gap), cellW, cellH);
                bool unlocked = level <= _highestUnlockedStage, selected = level == _selectedLevel, hovered = cell.Contains(mouse);
                Color fill = unlocked ? new Color(30, 80, 40, 230) : new Color(40, 40, 52, 220);
                if (selected) fill = unlocked ? new Color(60, 140, 70, 240) : new Color(70, 70, 90, 240);
                if (hovered) fill = Color.Lerp(fill, new Color(120, 220, 120, 255), 0.2f);
                DrawRect(cell, fill);
                DrawRect(new Rectangle(cell.X, cell.Y, cell.Width, 2), unlocked ? new Color(140, 255, 160, 160) : new Color(180, 180, 200, 80));
                if (!unlocked) { _sb.DrawString(_font, "X", new Vector2(cell.X + (cellW - _font.MeasureString("X").X) / 2f, cell.Y + 10), Color.White * 0.5f, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0); }
                else { Vector2 ls = _font.MeasureString(level.ToString()); _sb.DrawString(_font, level.ToString(), new Vector2(cell.X + (cellW - ls.X) / 2f, cell.Y + 10), Color.White, 0, Vector2.Zero, 0.82f, SpriteEffects.None, 0); }
                if (selected && unlocked) _sb.DrawString(_font, ">", new Vector2(cell.X + 10, cell.Y + 10), Color.Yellow, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            }
            string message = _levelSelectTimer > 0f ? _levelSelectMessage : "";
            if (!string.IsNullOrWhiteSpace(message)) _sb.DrawString(_font, message, new Vector2(ppx + 24, ppy + ph - 56), Color.Yellow * MathHelper.Clamp(_levelSelectTimer / 2f, 0f, 1f), 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            var backR = BackButtonRect();
            DrawRect(backR, new Color(20, 60, 40, 220));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(140, 255, 180, 180));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 18, backR.Y + 12), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawMountainSilhouette(float yCutoff, Color col)
        {
            int baseY = (int)(BASE_H * yCutoff);
            var rr = new Random((int)(yCutoff * 1000));
            float x = -100;
            while (x < BASE_W + 100) { int w = rr.Next(120, 280), h = rr.Next(60, 180); DrawRect(new Rectangle((int)x - w / 2, baseY - h, w, h + 200), col); x += w * 0.65f + rr.Next(20, 80); }
        }

        private void DrawCanopySilhouette()
        {
            var rr = new Random(999);
            int by = BASE_H - 80;
            for (int i = 0; i < 24; i++) { int tx = i * 70 - 30, th = rr.Next(80, 190), tw = rr.Next(55, 110); DrawRect(new Rectangle(tx, by - th, tw, th + 80), new Color(8, 28, 10, 240)); DrawRect(new Rectangle(tx - 15, by - th - 35, tw + 30, 55), new Color(12, 40, 14, 230)); }
        }

        private void DrawRoundedPanel(Rectangle r, Color c)
        {
            DrawRect(r, c); DrawRect(new Rectangle(r.X - 1, r.Y + 4, r.Width + 2, r.Height - 8), c); DrawRect(new Rectangle(r.X + 4, r.Y - 1, r.Width - 8, r.Height + 2), c);
        }

        private void DrawPlaying(GameTime gt)
        {
            Color skyTop = DayNightSky(true), skyBottom = DayNightSky(false);
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
            DrawMobileButton(MobileLeftRect(), "L"); DrawMobileButton(MobileRightRect(), "R");
            DrawMobileButton(MobileJumpRect(), "JUMP"); DrawMobileButton(MobileAttackRect(), "HIT");
        }

        private void DrawMobileButton(Rectangle r, string label)
        {
            Color fill = label == "HIT" ? new Color(180, 80, 80, 140) : new Color(40, 40, 60, 130);
            DrawRect(r, fill); DrawRect(new Rectangle(r.X, r.Y, r.Width, 2), new Color(255, 255, 255, 100));
            if (_font != null) _sb.DrawString(_font, label, new Vector2(r.X + 10, r.Y + 10), Color.White * 0.9f, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
        }

        private Rectangle MobileLeftRect() => new Rectangle(24, BASE_H - 120, 70, 70);
        private Rectangle MobileRightRect() => new Rectangle(104, BASE_H - 120, 70, 70);
        private Rectangle MobileJumpRect() => new Rectangle(BASE_W - 210, BASE_H - 120, 90, 70);
        private Rectangle MobileAttackRect() => new Rectangle(BASE_W - 110, BASE_H - 120, 90, 70);

        private Color DayNightSky(bool top)
        {
            float t = _dayTime;
            if (top) { if (t < 0.25f) return Color.Lerp(new Color(100, 60, 30), new Color(30, 100, 180), t / 0.25f); else if (t < 0.5f) return Color.Lerp(new Color(30, 100, 180), new Color(180, 80, 40), (t - 0.25f) / 0.25f); else if (t < 0.75f) return Color.Lerp(new Color(180, 80, 40), new Color(5, 5, 30), (t - 0.5f) / 0.25f); else return Color.Lerp(new Color(5, 5, 30), new Color(100, 60, 30), (t - 0.75f) / 0.25f); }
            else { if (t < 0.25f) return Color.Lerp(new Color(200, 130, 60), new Color(100, 180, 80), t / 0.25f); else if (t < 0.5f) return Color.Lerp(new Color(100, 180, 80), new Color(220, 120, 50), (t - 0.25f) / 0.25f); else if (t < 0.75f) return Color.Lerp(new Color(220, 120, 50), new Color(20, 20, 50), (t - 0.5f) / 0.25f); else return Color.Lerp(new Color(20, 20, 50), new Color(200, 130, 60), (t - 0.75f) / 0.25f); }
        }

        private void DrawWorldBackground()
        {
            Color ground = _stageTheme switch { 1 => new Color(42, 64, 28), 2 => new Color(24, 58, 50), 3 => new Color(56, 44, 58), 4 => new Color(72, 50, 28), 5 => new Color(32, 34, 52), _ => new Color(30, 68, 32) };
            Color top = _stageTheme switch { 1 => new Color(90, 150, 70), 2 => new Color(70, 140, 130), 3 => new Color(150, 110, 160), 4 => new Color(175, 122, 72), 5 => new Color(90, 96, 150), _ => new Color(60, 120, 50) };
            Color dirt = _stageTheme switch { 1 => new Color(72, 48, 24), 2 => new Color(34, 52, 44), 3 => new Color(64, 34, 44), 4 => new Color(96, 62, 34), 5 => new Color(44, 42, 70), _ => new Color(80, 52, 28) };
            DrawRect(new Rectangle(-200, (int)Globals.GroundY, (int)Globals.WorldWidth + 400, 300), ground);
            DrawRect(new Rectangle(-200, (int)Globals.GroundY, (int)Globals.WorldWidth + 400, 12), top);
            DrawRect(new Rectangle(-200, (int)Globals.GroundY + 12, (int)Globals.WorldWidth + 400, 288), dirt);
            if (_stageTheme >= 2) for (int i = 0; i < 12; i++) DrawCircle(120 + i * 104, 120 + (float)Math.Sin(_globalTimer * 0.35f + i) * 12f, 12f, new Color(20, 30, 50, 25));
            if (_stageTheme >= 4) { DrawRect(new Rectangle(80, 210, 120, 58), new Color(90, 70, 40, 120)); DrawRect(new Rectangle(115, 170, 50, 44), new Color(110, 80, 48, 120)); }
        }

        private void DrawPlatforms()
        {
            foreach (var p in _platRects) { DrawRect(p, new Color(55, 36, 18)); DrawRect(new Rectangle(p.X, p.Y, p.Width, 6), new Color(70, 130, 55)); DrawRect(new Rectangle(p.X + 4, p.Y + 8, p.Width - 8, p.Height - 10), new Color(45, 28, 12)); }
        }

        private void DrawEnvironment()
        {
            // Rivers with bridges
            foreach (var river in _riverRects)
            {
                Color waterCol = Color.Lerp(new Color(20, 60, 120), new Color(40, 100, 180), 0.5f + 0.5f * (float)Math.Sin(_globalTimer * 0.5f + river.X));
                DrawRect(river, waterCol);
                for (int ri = 0; ri < 3; ri++) { float rippleX = river.X + (float)Math.Sin(_globalTimer * 1.5f + ri * 2f + river.X * 0.01f) * 15f + 10f; DrawRect(new Rectangle((int)rippleX, river.Y + 10 + ri * 35, 20, 2), Color.White * 0.2f); }
                DrawRect(new Rectangle(river.X - 8, river.Y, 8, river.Height), new Color(60, 40, 20));
                DrawRect(new Rectangle(river.X + river.Width, river.Y, 8, river.Height), new Color(60, 40, 20));
                // Bridge visual
                int bridgeX = river.X - 10, bridgeY = river.Y - 22, bridgeW = river.Width + 20;
                DrawRect(new Rectangle(bridgeX, bridgeY, bridgeW, 8), new Color(120, 80, 50));
                DrawRect(new Rectangle(bridgeX, bridgeY, bridgeW, 3), new Color(180, 140, 80));
                DrawRect(new Rectangle(bridgeX, bridgeY + 8, bridgeW, 2), new Color(80, 50, 30));
                // Bridge railings
                DrawRect(new Rectangle(bridgeX - 2, bridgeY - 6, 4, 14), new Color(100, 70, 40));
                DrawRect(new Rectangle(bridgeX + bridgeW / 4, bridgeY - 6, 4, 14), new Color(100, 70, 40));
                DrawRect(new Rectangle(bridgeX + bridgeW / 2, bridgeY - 6, 4, 14), new Color(100, 70, 40));
                DrawRect(new Rectangle(bridgeX + 3 * bridgeW / 4, bridgeY - 6, 4, 14), new Color(100, 70, 40));
                DrawRect(new Rectangle(bridgeX + bridgeW - 2, bridgeY - 6, 4, 14), new Color(100, 70, 40));
            }

            // Flowers
            foreach (var f in _flowers) { float sway = (float)Math.Sin(_globalTimer * 2.2f + f.X * 0.03f) * 2f; Color flowerColor = new Color(_rng.Next(200, 255), _rng.Next(50, 150), _rng.Next(50, 200)); DrawCircle(f.X + sway, f.Y - 6, 3f, flowerColor); DrawCircle(f.X + sway - 1, f.Y - 4, 2f, new Color(255, 255, 100) * 0.6f); DrawRect(new Rectangle((int)f.X, (int)f.Y - 10, 1, 8), new Color(60, 140, 40)); }
            // Mushrooms
            foreach (var m in _mushrooms) { Color stem = new Color(220, 200, 170); Color cap = new Color(_rng.Next(180, 220), _rng.Next(40, 80), _rng.Next(40, 80)); DrawRect(new Rectangle((int)m.X, (int)m.Y - 10, 4, 10), stem); DrawRect(new Rectangle((int)m.X - 4, (int)m.Y - 14, 12, 6), cap); DrawCircle(m.X, m.Y - 15, 3f, new Color(255, 255, 200, 100)); }
            // Bushes
            foreach (var b in _bushes) { Color bushCol = new Color(30 + _rng.Next(0, 30), 90 + _rng.Next(0, 40), 30 + _rng.Next(0, 20)); DrawCircle(b.X, b.Y - 6, 10f, bushCol); DrawCircle(b.X - 6, b.Y - 4, 8f, bushCol); DrawCircle(b.X + 6, b.Y - 4, 8f, bushCol); DrawCircle(b.X, b.Y - 10, 7f, bushCol); DrawCircle(b.X - 3, b.Y - 12, 2f, new Color(200, 50, 50)); DrawCircle(b.X + 4, b.Y - 11, 2f, new Color(200, 50, 50)); }
            // Grasses
            foreach (var g in _grasses) { for (int b = 0; b < 4; b++) { float bx = g.X + (b - 1.5f) * 5f; float wave = (float)Math.Sin(_globalTimer * 1.8f + g.X * 0.05f + b) * 3f; DrawRect(new Rectangle((int)bx, (int)g.Y - 14, 2, 14), new Color(60, 160, 50)); DrawRect(new Rectangle((int)(bx + wave * 0.5f), (int)g.Y - 24, 2, 12), new Color(80, 190, 60)); } }
            // Rocks
            foreach (var r in _rocks) { DrawRect(new Rectangle((int)r.X - 12, (int)r.Y - 10, 24, 14), new Color(100, 95, 85)); DrawRect(new Rectangle((int)r.X - 10, (int)r.Y - 12, 20, 8), new Color(130, 125, 110)); DrawRect(new Rectangle((int)r.X - 6, (int)r.Y - 14, 8, 4), new Color(160, 155, 140)); }
            // Trees
            for (int i = 0; i < _trees.Count; i++) DrawTree(_trees[i], _treeSizes[i]);
            // Big trees
            for (int i = 0; i < _bigTrees.Count; i++) DrawBigTree(_bigTrees[i], _bigTreeSizes[i]);
            // Houses
            if (_stageIndex >= 10) DrawVillageHouse((int)(Globals.WorldWidth * 0.25f), (int)Globals.GroundY, _stageTheme >= 2);
            if (_stageIndex >= 20) DrawVillageHouse((int)(Globals.WorldWidth * 0.50f), (int)Globals.GroundY, _stageTheme >= 2);
            if (_stageIndex >= 30) { int nestX = (int)Globals.WorldWidth - 900; DrawMonsterNest(nestX, (int)Globals.GroundY); }
            DrawTemple((int)Globals.WorldWidth - 340, (int)Globals.GroundY);
        }

        private void DrawBigTree(Vector2 pos, float scale)
        {
            int tw = (int)(30 * scale), th = (int)(220 * scale), cw = (int)(180 * scale), ch = (int)(140 * scale);
            int cx = (int)pos.X - cw / 2, ty = (int)pos.Y - th;
            float sw = (float)Math.Sin(_globalTimer * 0.5f + pos.X * 0.008f) * 3f * scale;
            DrawRect(new Rectangle((int)pos.X - tw / 2, ty, tw, th), new Color(60, 38, 20));
            DrawRect(new Rectangle((int)pos.X - tw / 4, ty, tw / 2, th), new Color(75, 48, 25));
            DrawRect(new Rectangle(cx + (int)sw, ty - (int)(50 * scale), cw, ch + 30), new Color(20, 80, 28, 230));
            DrawRect(new Rectangle(cx + 15 + (int)(sw * 0.7f), ty - (int)(95 * scale), cw - 30, ch - 20), new Color(30, 110, 38, 220));
            DrawRect(new Rectangle(cx + 30 + (int)(sw * 0.4f), ty - (int)(130 * scale), cw - 60, 50), new Color(40, 130, 45, 200));
            float vineSway = (float)Math.Sin(_globalTimer * 1.2f + pos.X * 0.02f) * 6f;
            DrawRect(new Rectangle((int)(pos.X - 8 + vineSway), ty + 50, 3, 60), new Color(40, 100, 30, 150));
            DrawRect(new Rectangle((int)(pos.X + 6 + vineSway), ty + 80, 3, 50), new Color(40, 100, 30, 150));
            DrawRect(new Rectangle((int)(pos.X + vineSway), ty + 110, 3, 40), new Color(40, 100, 30, 150));
            DrawCircle(pos.X + vineSway - 20, ty + 120, 4f, new Color(255, 180, 60, 180));
            DrawCircle(pos.X + vineSway + 15, ty + 150, 3f, new Color(255, 100, 50, 180));
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
            int tw = (int)(20 * scale), th = (int)(140 * scale), cw = (int)(90 * scale), ch = (int)(80 * scale);
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
            foreach (var en in _enemies) { if (en == null || (!en.IsActive && !en.IsDead)) continue; DrawEnemy(en); }
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
            int bh = (int)((en.Type == EnemyType.Goblin || en.Type == EnemyType.Shadow ? 24f : 34f) * en.HeightScale);
            if (en.Type == EnemyType.Shielded) { DrawRect(new Rectangle(ex - bw / 2 - 6, ey - bh - 24, bw + 12, bh + 14), new Color(80, 170, 150, 70)); DrawRect(new Rectangle(ex - bw / 2 - 10, ey - bh - 16, 10, bh - 2), new Color(200, 220, 255)); }
            if (en.Type == EnemyType.Demon) { DrawCircle(ex, ey - bh - 30, 20f, new Color(200, 30, 30, 80)); }
            if (en.Type == EnemyType.Shadow) { DrawCircle(ex, ey - bh - 20, 18f, new Color(80, 40, 120, 60)); }
            DrawRect(new Rectangle(ex - bw / 2, ey - bh - 20, bw, bh), drawCol);
            int hw = bw + 4;
            DrawRect(new Rectangle(ex - hw / 2, ey - bh - 40, hw, 22), drawCol);
            DrawRect(new Rectangle(ex - hw / 2 + 4, ey - bh - 40, hw - 8, 10), drawCol * 0.85f);
            DrawRect(new Rectangle(ex + dir * 10, ey - bh - 16 + (int)(walk * 0.8f), 5, 16), drawCol * 0.9f);
            DrawRect(new Rectangle(ex - dir * 16, ey - bh - 14 - (int)(walk * 0.8f), 5, 16), drawCol * 0.9f);
            int eyeOff = dir > 0 ? 3 : -3;
            Color eyeCol = en.Type == EnemyType.Skeleton ? Color.Cyan : en.Type == EnemyType.Demon ? Color.Red : en.Type == EnemyType.Shadow ? Color.Purple : Color.Red;
            DrawRect(new Rectangle(ex + eyeOff - 3, ey - bh - 34, 5, 5), eyeCol);
            DrawRect(new Rectangle(ex + eyeOff + 4, ey - bh - 34, 5, 5), eyeCol);
            // Ogre/Giant special features
            if (en.Type == EnemyType.Ogre) { DrawRect(new Rectangle(ex - 8, ey - bh - 46, 16, 8), new Color(200, 120, 40)); DrawRect(new Rectangle(ex - 10, ey - bh - 24, 20, 8), new Color(160, 100, 40)); }
            if (en.Type == EnemyType.Giant) { DrawRect(new Rectangle(ex - 10, ey - bh - 50, 20, 10), new Color(180, 130, 60)); DrawRect(new Rectangle(ex - 12, ey - bh - 26, 24, 10), new Color(200, 120, 40)); }
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
            float hpR = en.Health / en.MaxHealth;
            int bW = 120, bX = ex - 60, bY = ey - 160;
            DrawRect(new Rectangle(bX - 2, bY - 2, bW + 4, 14), Color.Black * 0.85f);
            DrawRect(new Rectangle(bX, bY, bW, 10), new Color(60, 8, 8));
            Color hpCol = rage > 0.5f ? new Color(255, 60, 0) : Color.Lerp(Color.Red, Color.OrangeRed, rage);
            DrawRect(new Rectangle(bX, bY, (int)(bW * hpR), 10), hpCol);
            if (_font != null) _sb.DrawString(_font, $"BOSS  {en.Health:F0}/{en.MaxHealth:F0}", new Vector2(bX - 10, bY - 18), Color.OrangeRed * 0.9f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
        }

        private Color GetEnemyColor(EnemyType t)
        {
            return t switch { EnemyType.Zombie => new Color(70, 120, 60), EnemyType.Goblin => new Color(80, 150, 40), EnemyType.Skeleton => new Color(220, 215, 190), EnemyType.Shielded => new Color(110, 190, 160), EnemyType.Boss => new Color(140, 30, 20), EnemyType.Ogre => new Color(140, 100, 50), EnemyType.Giant => new Color(100, 80, 60), EnemyType.Demon => new Color(200, 40, 40), EnemyType.Shadow => new Color(100, 60, 140), _ => Color.Gray };
        }

        private void DrawPlayer()
        {
            int px = (int)_player.Position.X, py = (int)_player.Position.Y, dir = _player.FacingRight ? 1 : -1;
            float alpha = _player.IsInvincible ? (0.5f + 0.5f * (float)Math.Sin(_globalTimer * 20f)) : 1f;
            Color skinCol = new Color(255, 210, 170), shirt = new Color(40, 80, 200), pants = new Color(30, 50, 120), hair = new Color(80, 40, 10);
            float walk = _player.OnGround ? (float)Math.Sin(_player.WalkAnim) * 5f : 0f;
            if (_player.IsDead) { float deadT = MathHelper.Clamp(_player.DeathTimer / 1.2f, 0f, 1f); DrawRect(new Rectangle(px - 18, py - 10 + (int)(deadT * 18f), 36, 12), skinCol * (1f - deadT)); DrawRect(new Rectangle(px - 18, py - 20 + (int)(deadT * 20f), 36, 6), new Color(120, 70, 40) * (1f - deadT)); return; }
            DrawRect(new Rectangle(px - 10, py - 28, 8, 28 + (int)walk), pants * alpha);
            DrawRect(new Rectangle(px + 2, py - 28, 8, 28 - (int)walk), pants * alpha);
            DrawRect(new Rectangle(px - 12, py - 8, 10, 10), new Color(80, 50, 20) * alpha);
            DrawRect(new Rectangle(px + 2, py - 8, 10, 10), new Color(80, 50, 20) * alpha);
            DrawRect(new Rectangle(px - 14, py - 64, 28, 36), shirt * alpha);
            if (_player.HasShield) { int sdx = dir < 0 ? px + 12 : px - 22; DrawRect(new Rectangle(sdx, py - 60, 10, 30), new Color(150, 150, 180) * alpha); DrawRect(new Rectangle(sdx + 1, py - 58, 8, 10), new Color(200, 200, 240) * alpha); }
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
            float swingAngle = _player.IsAttacking ? MathHelper.Lerp(-MathHelper.PiOver2 * dir, MathHelper.PiOver2 * dir, 1f - atkProg) : -MathHelper.PiOver4 * dir * 0.4f;
            Color wCol = GetWeaponColor(_player.Weapon), skinCol = new Color(255, 210, 170);
            float ax = px + dir * 12 + (float)Math.Cos(swingAngle) * 16f, ay = py - 56 + (float)Math.Sin(swingAngle) * 16f;
            DrawLine(new Vector2(px + dir * 12, py - 56), new Vector2(ax, ay), skinCol * alpha, 5);
            switch (_player.Weapon)
            {
                case WeaponType.Fists: DrawRect(new Rectangle((int)ax - 4, (int)ay - 4, 8, 8), skinCol * alpha); break;
                case WeaponType.Sword: for (int seg = 0; seg < 5; seg++) { float bx = ax + dir * (float)Math.Cos(swingAngle + MathHelper.PiOver4 * dir) * seg * 9f, by = ay + (float)Math.Sin(swingAngle + MathHelper.PiOver4 * dir) * seg * 9f; DrawRect(new Rectangle((int)bx, (int)by, 7 - seg, 7 - seg), wCol * alpha); } DrawRect(new Rectangle((int)ax - 5, (int)ay - 2, 10, 4), new Color(200, 180, 80) * alpha); break;
                case WeaponType.Axe: for (int seg = 0; seg < 4; seg++) { float bx = ax + dir * seg * 8f, by = ay - seg * 4f; DrawRect(new Rectangle((int)bx, (int)by, 5, 5), new Color(120, 80, 40) * alpha); } DrawRect(new Rectangle((int)(ax + dir * 26), (int)ay - 18, 18, 24), wCol * alpha); DrawRect(new Rectangle((int)(ax + dir * 30), (int)ay - 24, 10, 8), wCol * alpha); break;
                case WeaponType.MagicStaff: for (int seg = 0; seg < 6; seg++) { float bx = ax + dir * seg * 9f, by = ay - seg * 5f; DrawRect(new Rectangle((int)bx, (int)by, 4, 4), new Color(120, 80, 200) * alpha); } float tipX = ax + dir * 54f, tipY = ay - 30f; DrawCircle(tipX, tipY, 10f * (0.7f + 0.3f * (float)Math.Sin(_globalTimer * 6f)), wCol * 0.8f * alpha); DrawCircle(tipX, tipY, 6f * (0.7f + 0.3f * (float)Math.Sin(_globalTimer * 6f)), Color.White * 0.6f * alpha); break;
                case WeaponType.Pistol: DrawRect(new Rectangle((int)ax - 10, (int)ay - 4, 22, 8), wCol * alpha); DrawRect(new Rectangle((int)ax + dir * 9, (int)ay - 6, 6, 4), new Color(90, 90, 90) * alpha); DrawRect(new Rectangle((int)ax - 3, (int)ay - 10, 8, 16), new Color(40, 40, 40) * alpha); break;
            }
        }

        private void DrawProjectiles()
        {
            foreach (var p in _projectiles) { float a = MathHelper.Clamp(p.Life / 2f, 0f, 1f); DrawCircle(p.Position.X, p.Position.Y, 4f, p.Color * a); DrawLine(p.Position - new Vector2(p.Velocity.X > 0 ? 10f : -10f, 0f), p.Position, p.Color * 0.8f * a, 2); }
        }

        private void DrawSlashes()
        {
            foreach (var slash in _slashes)
            {
                float lifeR = slash.Life / slash.MaxLife;
                for (int i = 0; i < 12; i++)
                {
                    float t = (float)i / 12, ang = slash.FacingRight ? slash.Angle + slash.ArcSpan * t : slash.Angle - slash.ArcSpan * t;
                    Vector2 inner = slash.Origin + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * slash.Radius * 0.3f;
                    Vector2 outer = slash.Origin + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * slash.Radius * (0.7f + t * 0.3f);
                    DrawLineThick(inner, outer, GetWeaponColor(_player.Weapon) * lifeR * (1f - t * 0.5f), (int)(3 * lifeR));
                }
            }
        }

        private void DrawParticles()
        {
            foreach (var p in _particles) { float a = p.Life / p.MaxLife; if (p.IsSpark) DrawCircle(p.Position.X, p.Position.Y, p.Size * a, p.Color * a); else DrawRect(new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)(p.Size * a) + 1, (int)(p.Size * a) + 1), p.Color * a); }
        }

        private void DrawRain() { foreach (var r in _rainDrops) { float a = r.Life / r.MaxLife; DrawLine(r.Position, r.Position + r.Velocity * 0.025f, r.Color * a, 1); } }

        private void DrawDamageNumbers()
        {
            if (_font == null) return;
            foreach (var n in _dmgNums) { float a = n.Life / n.MaxLife; string s = n.IsHeal ? $"+{n.Value:F0}" : (n.IsCrit ? $"CRIT! {n.Value:F0}" : $"-{n.Value:F0}"); Color c = n.IsHeal ? Color.LimeGreen : (n.IsCrit ? Color.Yellow : Color.OrangeRed); float scale = n.IsCrit ? 1.3f : 1f; Vector2 sz = _font.MeasureString(s) * scale; _sb.DrawString(_font, s, n.Position - new Vector2(sz.X / 2f, 0), c * a, 0, Vector2.Zero, scale, SpriteEffects.None, 0); }
        }

        private void DrawFloatingCoins()
        {
            foreach (var c in _coins) { float a = c.Life / 1.5f; DrawCircle(c.Position.X, c.Position.Y, 9f, new Color(255, 180, 60) * a); DrawCircle(c.Position.X + 1, c.Position.Y - 1, 6f, new Color(70, 210, 60) * a); DrawRect(new Rectangle((int)c.Position.X - 1, (int)c.Position.Y - 10, 2, 5), new Color(40, 140, 40) * a); if (_font != null && c.Amount > 0) _sb.DrawString(_font, $"+{c.Amount}", c.Position + new Vector2(10, -10), Color.Gold * a, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0); }
        }

        private void DrawFruits()
        {
            foreach (var f in _fruits)
            {
                float bob = (float)Math.Sin(_globalTimer * 2.5f + f.Position.X * 0.02f) * 3f;
                Color body = f.Kind.Contains("Temple") ? new Color(255, 210, 80) : f.Kind == "Apple" ? new Color(220, 40, 40) : f.Kind == "Banana" ? new Color(255, 220, 60) : f.Kind == "Cherry" ? new Color(200, 40, 60) : f.Kind == "Grape" ? new Color(120, 60, 180) : f.Kind == "Lemon" ? new Color(240, 240, 60) : f.Kind == "Orange" ? new Color(255, 140, 40) : new Color(120, 255, 120);
                DrawCircle(f.Position.X, f.Position.Y + bob, 8f, body);
                DrawCircle(f.Position.X + 2, f.Position.Y - 2 + bob, 4f, Color.White * 0.65f);
                DrawRect(new Rectangle((int)f.Position.X - 1, (int)f.Position.Y - 12 + (int)bob, 2, 5), new Color(40, 140, 40));
                if (_font != null) _sb.DrawString(_font, $"+{f.Value}", f.Position + new Vector2(10, -14 + bob), Color.LightGreen, 0, Vector2.Zero, 0.68f, SpriteEffects.None, 0);
            }
        }

        private void DrawHUD()
        {
            if (_font == null) return;
            DrawRect(new Rectangle(0, 0, BASE_W, 54), new Color(0, 0, 0, 180));
            DrawRect(new Rectangle(0, 52, BASE_W, 2), new Color(60, 160, 60, 160));
            int leftX = 12;
            DrawStatBar(leftX, 8, 200, 14, _player.Health, _player.MaxHealth, new Color(200, 40, 40), new Color(80, 10, 10), "HP");
            DrawStatBar(leftX, 26, 200, 10, _player.Stamina, _player.MaxStamina, new Color(40, 150, 220), new Color(10, 40, 80), "SP");
            DrawStatBar(leftX + 220, 8, 150, 12, _player.XP, _player.XPToNext, new Color(150, 80, 220), new Color(40, 20, 80), "XP");
            _sb.DrawString(_font, $"Lv.{_player.Level}", new Vector2(leftX + 220, 24), Color.Violet * 0.9f, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
            int centerX = 430;
            DrawCircle(centerX, 16, 8f, Color.Gold);
            _sb.DrawString(_font, $"{_player.Gold}", new Vector2(centerX + 18, 6), Color.Gold, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Fruits:{_player.FruitCount}", new Vector2(centerX + 18, 26), new Color(120, 255, 120), 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Armor:{_player.ArmorLevel}", new Vector2(centerX + 110, 26), Color.LightGray, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            int rightBase = BASE_W - 240;
            _sb.DrawString(_font, $"Wave {_wave}", new Vector2(rightBase, 6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"P:{_player.HealthPotions} B:{_player.Bombs}", new Vector2(rightBase, 26), Color.White * 0.85f, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            string dayStr = _dayTime < 0.25f ? "Dawn" : _dayTime < 0.5f ? "Day" : _dayTime < 0.75f ? "Dusk" : "Night";
            _sb.DrawString(_font, dayStr, new Vector2(rightBase + 120, 26), Color.LightYellow * 0.85f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            if (_levelTipTimer > 0f) _sb.DrawString(_font, _levelTipText, new Vector2(12, 56), Color.Yellow * MathHelper.Clamp(_levelTipTimer / 4.5f, 0f, 1f), 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            DrawRect(new Rectangle(0, BASE_H - 26, BASE_W, 26), new Color(0, 0, 0, 160));
            _sb.DrawString(_font, "WASD:Move  Z:Attack  I:Shop  M:Map  Q:Bomb  H:Potion  F11:Fullscreen", new Vector2(10, BASE_H - 20), Color.White * 0.6f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"[{_player.Weapon.ToString().ToUpper()}]", new Vector2(BASE_W / 2f - 30, BASE_H - 22), GetWeaponColor(_player.Weapon) * 0.9f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }

        private void DrawCircularHealthRing(float cx, float cy, float radius, float val, float max, Color fill, Color bg)
        {
            if (max <= 0f) return;
            float ratio = MathHelper.Clamp(val / max, 0f, 1f);
            for (int i = 0; i < 40; i++) { float t = (float)i / 40, ang = -MathHelper.PiOver2 + t * MathHelper.TwoPi; Color c = t <= ratio ? Color.Lerp(fill, Color.LimeGreen, ratio) : bg * 0.55f; DrawCircle(cx + (float)Math.Cos(ang) * radius, cy + (float)Math.Sin(ang) * radius, 2.5f, c); }
        }

        private void DrawStatBar(int x, int y, int w, int h, float val, float max, Color fill, Color bg, string label)
        {
            DrawRect(new Rectangle(x - 1, y - 1, w + 2, h + 2), Color.Black * 0.6f);
            DrawRect(new Rectangle(x, y, w, h), bg);
            int fw = (int)(w * MathHelper.Clamp(val / max, 0f, 1f));
            if (fw > 0) DrawRect(new Rectangle(x, y, fw, h), fill);
            DrawRect(new Rectangle(x, y, fw, h / 3), Color.White * 0.12f);
            if (_font != null) _sb.DrawString(_font, $"{label} {val:F0}/{max:F0}", new Vector2(x + w + 6, y - 1), Color.White * 0.8f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }

        private void DrawMinimap()
        {
            int mx = BASE_W - 168, my = 56, mw = 158, mh = 78;
            DrawRect(new Rectangle(mx - 2, my - 2, mw + 4, mh + 4), new Color(0, 0, 0, 180));
            DrawRect(new Rectangle(mx, my, mw, mh), new Color(10, 28, 12, 220));
            float scaleX = (float)mw / Globals.WorldWidth, scaleY = (float)mh / Globals.GroundY;
            foreach (var t in _trees) DrawRect(new Rectangle(mx + (int)(t.X * scaleX) - 1, my + (int)(t.Y * scaleY) - 1, 2, 2), new Color(30, 100, 30));
            foreach (var en in _enemies) if (en != null && !en.IsDead) DrawRect(new Rectangle(mx + (int)(en.Position.X * scaleX), my + (int)(en.Position.Y * scaleY), en.IsBoss ? 5 : 3, en.IsBoss ? 5 : 3), en.IsBoss ? Color.OrangeRed : Color.Red);
            if (_blinkOn) DrawCircle(mx + _player.Position.X * scaleX, my + _player.Position.Y * scaleY, 4f, Color.Yellow);
            float cvx = mx + _cam.X * scaleX, cvy = my + _cam.Y * scaleY;
            DrawRect(new Rectangle((int)cvx, (int)cvy, (int)(BASE_W * scaleX), (int)(BASE_H * scaleY)), new Color(255, 255, 255, 30));
        }

        private void DrawWaveInfo()
        {
            if (_font == null) return;
            int alive = 0; for (int i = 0; i < _enemies.Count; i++) if (_enemies[i] != null && !_enemies[i].IsDead && _enemies[i].IsActive) alive++;
            _sb.DrawString(_font, $"Enemies: {alive}  Wave {_wave}", new Vector2(BASE_W - 170, BASE_H - 42), Color.OrangeRed * 0.9f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            if (alive == 0) _sb.DrawString(_font, $"Next wave in {Math.Max(0f, _waveCooldown):F1}s", new Vector2(BASE_W / 2f - 80, BASE_H / 2f - 30), Color.Cyan * 0.9f);
        }

        private void DrawQuestPanel()
        {
            if (_font == null) return;
            int qx = 10, qy = 56, qw = 200;
            DrawRect(new Rectangle(qx - 2, qy - 2, qw + 4, 100), new Color(0, 0, 0, 100));
            _sb.DrawString(_font, "QUESTS", new Vector2(qx + 2, qy), Color.Wheat, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            int i = 0;
            foreach (var q in _quests) { if (i >= 3) break; Color qc = q.Complete ? Color.LimeGreen * 0.7f : Color.White * 0.8f; string label = q.Complete ? $"✓ {q.Title}" : $"• {q.Title} [{Math.Min(q.Progress, q.Goal)}/{q.Goal}]"; _sb.DrawString(_font, label, new Vector2(qx + 4, qy + 14 + i * 20), qc, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0); i++; }
        }

        private void DrawAchievementPopup()
        {
            if (_currentAch == null || _font == null) return;
            float a = MathHelper.Clamp(_currentAch.ShowTimer / 0.4f, 0f, 1f);
            int apx = BASE_W / 2 - 140, apy = 60;
            DrawRect(new Rectangle(apx, apy, 280, 50), new Color(20, 60, 20) * a);
            DrawRect(new Rectangle(apx, apy, 280, 2), Color.LimeGreen * a);
            _sb.DrawString(_font, "ACHIEVEMENT!", new Vector2(apx + 10, apy + 4), Color.LimeGreen * a, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            _sb.DrawString(_font, _currentAch.Title, new Vector2(apx + 10, apy + 20), Color.White * a, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            _sb.DrawString(_font, _currentAch.Desc, new Vector2(apx + 10, apy + 34), Color.LightGray * a, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
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
            string s = $"LEVEL UP! Now Level {_player.Level}";
            Vector2 sz = _font.MeasureString(s) * 1.2f;
            _sb.DrawString(_font, s, new Vector2((BASE_W - sz.X) / 2f, BASE_H * 0.4f), Color.Gold * a, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
        }

        private void DrawLevelTipBanner()
        {
            if (_levelTipTimer <= 0f || _font == null) return;
            float a = MathHelper.Clamp(_levelTipTimer / 4.5f, 0f, 1f);
            int bw = 500, bh = 40, bx = BASE_W / 2 - bw / 2, by = BASE_H - 80;
            DrawRect(new Rectangle(bx, by, bw, bh), new Color(0, 0, 0, 150) * a);
            DrawRect(new Rectangle(bx, by, bw, 2), new Color(255, 220, 120, 200) * a);
            _sb.DrawString(_font, _levelTipText, new Vector2(bx + 16, by + 10), Color.White * a, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
        }

        private void DrawWeaponIndicator() { if (_font != null) _sb.DrawString(_font, $"[{_player.Weapon.ToString().ToUpper()}]", new Vector2(BASE_W / 2f - 30, BASE_H - 22), GetWeaponColor(_player.Weapon) * 0.8f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0); }

        private void DrawShop()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(30, 16, 5), new Color(70, 40, 14), BASE_H);
            int pw = 900, ph = 640, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
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
                var r = ShopItemRect(i); var item = _shopItems[i];
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
            if (_shopMessageTimer > 0f) { DrawRect(new Rectangle(ppx + 18, ppy + ph - 68, pw - 36, 28), new Color(80, 15, 15, 220)); _sb.DrawString(_font, _shopMessage, new Vector2(ppx + 26, ppy + ph - 64), Color.OrangeRed * MathHelper.Clamp(_shopMessageTimer / 2f, 0f, 1f), 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0); }
            _sb.DrawString(_font, "Up/Down or mouse to select  |  Enter/Z or click to buy  |  ESC/I to leave", new Vector2(ppx + 14, ppy + ph - 26), Color.Gray * 0.75f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);
            _sb.End();
        }

        private Rectangle ShopItemRect(int i) { int pw = 900, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - 640) / 2; return new Rectangle(ppx + 10, ppy + 120 + i * 48, pw - 20, 44); }

        private void DrawShopItemPreview(Rectangle box, ShopItem item)
        {
            Color c = item.TintColor;
            if (item.Name.Contains("Sword")) { DrawRect(new Rectangle(box.X + 16, box.Y + 8, 4, 16), c); DrawRect(new Rectangle(box.X + 12, box.Y + 14, 12, 4), c); }
            else if (item.Name.Contains("Axe")) { DrawRect(new Rectangle(box.X + 17, box.Y + 6, 4, 18), c); DrawRect(new Rectangle(box.X + 8, box.Y + 8, 12, 14), c); }
            else if (item.Name.Contains("Staff")) { DrawRect(new Rectangle(box.X + 17, box.Y + 3, 4, 20), c); DrawCircle(box.X + 19, box.Y + 4, 5f, Color.White); }
            else if (item.Name.Contains("Pistol")) { DrawRect(new Rectangle(box.X + 9, box.Y + 11, 18, 6), c); DrawRect(new Rectangle(box.X + 20, box.Y + 8, 6, 4), new Color(80, 80, 80)); }
            else if (item.Name.Contains("Shield")) { DrawRect(new Rectangle(box.X + 13, box.Y + 4, 12, 18), c); DrawRect(new Rectangle(box.X + 15, box.Y + 7, 8, 10), Color.White * 0.35f); }
            else if (item.Name.Contains("Potion")) { DrawRect(new Rectangle(box.X + 14, box.Y + 7, 8, 14), c); DrawCircle(box.X + 18, box.Y + 6, 4f, Color.Red); }
            else if (item.Name.Contains("Boot")) { DrawRect(new Rectangle(box.X + 10, box.Y + 13, 15, 6), c); DrawRect(new Rectangle(box.X + 21, box.Y + 7, 4, 8), c); }
            else if (item.Name.Contains("Bomb")) { DrawCircle(box.X + 18, box.Y + 12, 8f, c); DrawRect(new Rectangle(box.X + 16, box.Y + 2, 4, 5), Color.Orange); }
            else if (item.Name.Contains("Armor")) { DrawRect(new Rectangle(box.X + 10, box.Y + 6, 16, 12), c); DrawRect(new Rectangle(box.X + 15, box.Y + 3, 6, 18), Color.White * 0.4f); }
            else if (item.Name.Contains("VIP")) { DrawCircle(box.X + 18, box.Y + 12, 8f, Color.Gold); DrawCircle(box.X + 18, box.Y + 12, 4f, Color.White * 0.6f); }
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
            float sx = (float)mw / Globals.WorldWidth, sy = (float)mh / Globals.GroundY;
            foreach (var t in _trees) DrawRect(new Rectangle(mx + (int)(t.X * sx) - 2, my + (int)(t.Y * sy) - 2, 4, 4), new Color(30, 100, 30));
            foreach (var p in _platRects) DrawRect(new Rectangle(mx + (int)(p.X * sx), my + (int)(p.Y * sy), Math.Max(2, (int)(p.Width * sx)), 2), new Color(120, 80, 40));
            DrawRect(new Rectangle(mx + (int)((Globals.WorldWidth - 340) * sx) - 2, my + (int)(Globals.GroundY * sy) - 6, 6, 6), Color.Gold);
            foreach (var en in _enemies) { if (en == null || en.IsDead) continue; int ex = mx + (int)(en.Position.X * sx), ey2 = my + (int)(en.Position.Y * sy); int esz = en.IsBoss ? 6 : 3; DrawRect(new Rectangle(ex - esz / 2, ey2 - esz / 2, esz, esz), en.IsBoss ? Color.OrangeRed : GetEnemyColor(en.Type)); }
            float px2 = mx + _player.Position.X * sx, py2 = my + _player.Position.Y * sy;
            if (_blinkOn) { DrawCircle(px2, py2, 7f, Color.Yellow * 0.6f); DrawCircle(px2, py2, 4f, Color.Yellow); }
            int lx = BASE_W - 260, ly = 70;
            DrawRect(new Rectangle(lx - 4, ly - 4, 240, 380), new Color(0, 0, 0, 180));
            _sb.DrawString(_font, "LEGEND", new Vector2(lx, ly), Color.Wheat);
            DrawLegendEntry(lx, ly + 26, Color.Yellow, "You"); DrawLegendEntry(lx, ly + 50, GetEnemyColor(EnemyType.Zombie), "Zombie");
            DrawLegendEntry(lx, ly + 74, GetEnemyColor(EnemyType.Goblin), "Goblin"); DrawLegendEntry(lx, ly + 98, GetEnemyColor(EnemyType.Skeleton), "Skeleton");
            DrawLegendEntry(lx, ly + 122, Color.OrangeRed, "Boss!"); DrawLegendEntry(lx, ly + 146, new Color(30, 100, 30), "Tree");
            _sb.DrawString(_font, "STATS", new Vector2(lx, ly + 200), Color.Wheat);
            _sb.DrawString(_font, $"HP: {_player.Health:F0}/{_player.MaxHealth:F0}", new Vector2(lx, ly + 224), Color.OrangeRed);
            _sb.DrawString(_font, $"Lv: {_player.Level}", new Vector2(lx, ly + 248), Color.Violet);
            _sb.DrawString(_font, $"Gold: {_player.Gold}", new Vector2(lx, ly + 272), Color.Gold);
            string hint = "Press M or ESC to close";
            _sb.DrawString(_font, hint, new Vector2((BASE_W - _font.MeasureString(hint).X) / 2f, BASE_H - 28), Color.Gray);
            _sb.End();
        }

        private void DrawStageClear()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(8, 30, 12), new Color(20, 60, 26), BASE_H);
            for (int i = 0; i < 24; i++) DrawCircle(120 + i * 48, 120 + (float)Math.Sin(_globalTimer * 2f + i) * 18f, 4f + (i % 3), new Color(120, 255, 160, 40));
            int pw = 700, ph = 360, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(140, 255, 160, 220));
            string msg = $"STAGE {_stageIndex} CLEARED!";
            Vector2 ms = _font.MeasureString(msg) * 2f;
            _sb.DrawString(_font, msg, new Vector2((BASE_W - ms.X) / 2f, ppy + 28), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            _sb.DrawString(_font, $"Gold: {_player.Gold}  Fruits: {_player.FruitCount}  Level: {_player.Level}", new Vector2(ppx + 48, ppy + 170), Color.White, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
            _sb.DrawString(_font, "Press Enter or wait to enter the next stage", new Vector2(ppx + 48, ppy + 260), Color.Yellow, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawLegendEntry(int x, int y, Color col, string label) { DrawRect(new Rectangle(x, y + 3, 12, 12), col); if (_font != null) _sb.DrawString(_font, label, new Vector2(x + 18, y), Color.LightGray, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0); }

        private void DrawPaused()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(18, 8, 30), new Color(34, 60, 88), BASE_H);
            for (int i = 0; i < 30; i++) DrawCircle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 2f + (float)_rng.NextDouble() * 4f, new Color(120, 180, 255, 45));
            int pw = 700, ph = 460, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 220));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(12, 16, 30, 230));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(120, 220, 255) * 0.9f);
            string title = "SETTINGS";
            Vector2 ts = _font.MeasureString(title) * 2f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 24), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            string[] options = { _fullscreen ? "Fullscreen: ON" : "Fullscreen: OFF", "Window: 960 x 540", "Window: 1280 x 720", _rotateScreen ? "Rotate: ON" : "Rotate: OFF", _mobileMode ? "Mobile: ON" : "Mobile: OFF", "Back to game" };
            for (int i = 0; i < options.Length; i++) { bool sel = i == _settingsSelected; if (sel) DrawRect(new Rectangle(ppx + 26, ppy + 106 + i * 56, pw - 52, 44), new Color(90, 200, 255, 35)); _sb.DrawString(_font, sel ? $"> {options[i]}" : options[i], new Vector2(ppx + 42, ppy + 116 + i * 56), sel ? Color.Black : Color.White, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0); }
            _sb.DrawString(_font, "Up/Down to navigate  |  Enter or ESC to return", new Vector2(ppx + 30, ppy + ph - 38), Color.LightGray * 0.8f, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawHelpScreen()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            DrawGradient(new Color(4, 24, 18), new Color(16, 44, 30), BASE_H);
            // Decorative background elements
            for (int i = 0; i < 24; i++) { float x = 80 + (i * 48) % 1100; float y = 60 + (float)Math.Sin(_globalTimer * 1.5f + i * 0.7f) * 20f + (i * 20) % 600; DrawCircle(x, y, 4f + (i % 4), new Color(80, 220, 120, 40 + (i % 3) * 15)); }
            int pw = 800, ph = 520, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(120, 255, 150, 200));
            string title = "HOW TO PLAY";
            Vector2 ts = _font.MeasureString(title) * 2f;
            _sb.DrawString(_font, title, new Vector2((BASE_W - ts.X) / 2f, ppy + 18), Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            string[] lines = {
                "MOVEMENT: WASD or Arrow Keys",
                "JUMP: W or Up Arrow (on ground only)",
                "ATTACK: Z or X key",
                "SHOP: I or B key",
                "MAP: M key",
                "BOMB: Q key  |  HEALTH POTION: H key",
                "SETTINGS: ESC key",
                "",
                "OBJECTIVE:",
                "Reach the temple at the far end of the level",
                "Defeat all enemy waves to advance",
                "Collect fruits and gold to buy items in the shop",
                "",
                "TIPS:",
                "Use platforms to gain height advantage",
                
            };
            for (int i = 0; i < lines.Length; i++)
            {
                Color lineCol = Color.White;
                if (lines[i] == "") continue;
                if (lines[i] == "OBJECTIVE:" || lines[i] == "TIPS:") lineCol = Color.Gold;
                _sb.DrawString(_font, lines[i], new Vector2(ppx + 30, ppy + 90 + i * 28), lineCol * 0.95f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            }
            var backR = BackButtonRect();
            DrawRect(backR, new Color(20, 80, 50, 220));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(140, 255, 180, 180));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 18, backR.Y + 12), Color.White, 0, Vector2.Zero, 0.78f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawCreditsScreen()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_font == null) { _sb.End(); return; }
            // Star field background
            DrawGradient(new Color(2, 6, 18), new Color(8, 16, 40), BASE_H);
            for (int i = 0; i < 40; i++) { float sx = (i * 37 + 13) % BASE_W; float sy = (i * 23 + 47) % BASE_H; float sa = 0.3f + 0.7f * (float)Math.Sin(_globalTimer * 0.5f + i); DrawCircle(sx, sy, 1.5f + (i % 3) * 0.5f, new Color(200, 220, 255, (int)(sa * 200))); }
            // Decorative glowing orbs
            for (int k = 0; k < 3; k++) { float ang = _globalTimer * 0.3f + k * MathHelper.TwoPi / 3f; float ox = BASE_W / 2 + (float)Math.Cos(ang) * 200f, oy = BASE_H / 2 + (float)Math.Sin(ang * 1.3f) * 80f; DrawCircle(ox, oy, 25f, new Color(60, 120, 255, 20)); DrawCircle(ox, oy, 12f, new Color(100, 160, 255, 40)); }
            // Scrolling credits text
            float yPos = _creditScrollY;
            foreach (string line in _creditLines)
            {
                Color col;
                if (line == "" || string.IsNullOrWhiteSpace(line)) { yPos += 30f; continue; }
                else if (line.Contains("JUNGLE ADVENTURE")) col = Color.Gold;
                else if (line.Contains("Developed By") || line.Contains("Dhurgham Alsaadi")) col = Color.LightGreen;
                else if (line.Contains("Version")) col = Color.Cyan;
                else if (line.Contains("Thanks") || line.Contains("Thank you")) col = Color.Orange;
                else col = Color.LightGray;
                float scale = line.Contains("JUNGLE") ? 1.8f : line.Contains("Dhurgham") ? 1.5f : (line.Contains("Version") || line.Contains("Thank you")) ? 1.2f : 1.0f;
                Vector2 sz = _font.MeasureString(line) * scale;
                if (yPos > -50 && yPos < BASE_H + 50) _sb.DrawString(_font, line, new Vector2((BASE_W - sz.X) / 2f, yPos), col * MathHelper.Clamp(Math.Min(yPos, BASE_H - yPos) / 60f, 0f, 1f), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                yPos += 38f * (line.Contains("JUNGLE") ? 1.5f : line.Contains("Dhurgham") ? 1.3f : 1f);
            }
            // Decorative border
            DrawRect(new Rectangle(0, 0, BASE_W, 2), new Color(80, 180, 255, 80));
            DrawRect(new Rectangle(0, BASE_H - 2, BASE_W, 2), new Color(80, 180, 255, 80));
            var backR = BackButtonRect();
            DrawRect(backR, new Color(24, 36, 92, 200));
            DrawRect(new Rectangle(backR.X, backR.Y, backR.Width, 2), new Color(160, 200, 255, 150));
            _sb.DrawString(_font, "BACK TO WELCOME", new Vector2(backR.X + 18, backR.Y + 12), Color.White * 0.9f, 0, Vector2.Zero, 0.72f, SpriteEffects.None, 0);
            _sb.End();
        }

        private void DrawWin()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            float pulse = 0.7f + 0.3f * (float)Math.Sin(_endScreenTimer * 2f);
            DrawGradient(new Color((int)(4 * pulse), (int)(40 * pulse), (int)(8 * pulse)), new Color((int)(12 * pulse), (int)(80 * pulse), (int)(20 * pulse)), BASE_H);
            for (int i = 0; i < 5; i++) DrawRect(new Rectangle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 6, 6), new Color(_rng.Next(100, 255), _rng.Next(100, 255), _rng.Next(100, 255), 160));
            int pw = 700, ph = 440, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 210));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(8, 40, 12, 230));
            if (_font != null)
            {
                string ftitle = "VICTORY!";
                float tScale = 3.2f, glow = 0.6f + 0.4f * (float)Math.Sin(_endScreenTimer * 3f);
                Vector2 tsz = _font.MeasureString(ftitle) * tScale;
                float tx = (BASE_W - tsz.X) / 2f, ty = ppy + 30f;
                _sb.DrawString(_font, ftitle, new Vector2(tx + 3, ty + 3), Color.Black * 0.5f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ftitle, new Vector2(tx, ty), new Color(255, 255, 150), 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, "You defeated all the bosses and saved the jungle!", new Vector2(ppx + 30, ppy + 148), Color.LightGreen, 0, Vector2.Zero, 0.95f, SpriteEffects.None, 0);
                _sb.DrawString(_font, $"Final Level: {_player.Level}", new Vector2(ppx + 60, ppy + 200), Color.White);
                _sb.DrawString(_font, $"Gold Earned: {_player.Gold}", new Vector2(ppx + 60, ppy + 230), Color.Gold);
                _sb.DrawString(_font, $"Waves Survived: {_wave}", new Vector2(ppx + 60, ppy + 260), Color.Cyan);
                if (_blinkOn) { string pe = "Press ENTER to play again"; Vector2 ps = _font.MeasureString(pe); _sb.DrawString(_font, pe, new Vector2((BASE_W - ps.X) / 2f, ppy + ph - 45), Color.Yellow * 0.9f); }
            }
            _sb.End();
        }

        private void DrawLose()
        {
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            float t = MathHelper.Clamp(_endScreenTimer / 1.5f, 0f, 1f);
            DrawGradient(Color.Lerp(Color.Black, new Color(40, 4, 4), t), Color.Lerp(Color.Black, new Color(80, 8, 8), t), BASE_H);
            for (int i = 0; i < 30; i++) DrawRect(new Rectangle(_rng.Next(0, BASE_W), _rng.Next(0, BASE_H), 1, _rng.Next(12, 30)), new Color(60, 60, 80, 100));
            int pw = 680, ph = 430, ppx = (BASE_W - pw) / 2, ppy = (BASE_H - ph) / 2;
            DrawRect(new Rectangle(ppx, ppy, pw, ph), new Color(0, 0, 0, 215));
            DrawRect(new Rectangle(ppx + 2, ppy + 2, pw - 4, ph - 4), new Color(40, 4, 4, 230));
            DrawRect(new Rectangle(ppx, ppy, pw, 3), new Color(200, 30, 30) * 0.9f);
            if (_font != null)
            {
                string ltitle = "GAME OVER";
                float tScale = 3f, red = 0.6f + 0.4f * (float)Math.Sin(_endScreenTimer * 2.5f);
                Vector2 tsz = _font.MeasureString(ltitle) * tScale;
                float tx = (BASE_W - tsz.X) / 2f, ty = ppy + 26f;
                _sb.DrawString(_font, ltitle, new Vector2(tx + 3, ty + 3), Color.Black * 0.55f, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, ltitle, new Vector2(tx, ty), new Color(255, 60, 60) * red, 0, Vector2.Zero, tScale, SpriteEffects.None, 0);
                _sb.DrawString(_font, "The jungle claimed you...", new Vector2(ppx + 30, ppy + 148), Color.OrangeRed * 0.9f);
                _sb.DrawString(_font, $"Level Reached: {_player.Level}", new Vector2(ppx + 60, ppy + 192), Color.White);
                _sb.DrawString(_font, $"Gold Collected: {_player.Gold}", new Vector2(ppx + 60, ppy + 222), Color.Gold);
                _sb.DrawString(_font, $"Waves Survived: {_wave - 1}", new Vector2(ppx + 60, ppy + 252), Color.Cyan);
                string[] tips = { "Buy the Iron Shield to reduce damage", "Level up to increase HP and damage", "Use Health Potions (H key) to survive", "Boss waves spawn every 3rd wave" };
                _sb.DrawString(_font, tips[((int)(_endScreenTimer * 0.5f)) % tips.Length], new Vector2(ppx + 30, ppy + 330), Color.Gray * 0.85f, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);
                if (_blinkOn) { string pe = "Press ENTER to try again"; Vector2 ps = _font.MeasureString(pe); _sb.DrawString(_font, pe, new Vector2((BASE_W - ps.X) / 2f, ppy + ph - 42), Color.Yellow * 0.9f); }
            }
            _sb.End();
        }

        private void DrawGradient(Color top, Color bottom, int height) { for (int y = 0; y < height; y += 8) DrawRect(new Rectangle(0, y, BASE_W, 8), Color.Lerp(top, bottom, (float)y / height)); }
        private void DrawCircle(float x, float y, float radius, Color color) { if (_circleTex != null) _sb.Draw(_circleTex, new Vector2(x, y), null, color, 0f, new Vector2(8, 8), radius / 8f, SpriteEffects.None, 0); }
        private void DrawRect(Rectangle r, Color c) { if (_pixel != null) _sb.Draw(_pixel, r, c); }
        private void DrawLine(Vector2 a, Vector2 b, Color c, int thickness = 1) { float dist = Vector2.Distance(a, b); float ang = (float)Math.Atan2(b.Y - a.Y, b.X - a.X); _sb.Draw(_pixel, a, null, c, ang, Vector2.Zero, new Vector2(dist, thickness), SpriteEffects.None, 0); }
        private void DrawLineThick(Vector2 a, Vector2 b, Color c, int thickness) { for (int i = -thickness / 2; i <= thickness / 2; i++) { Vector2 off = new Vector2(-(b.Y - a.Y), b.X - a.X); if (off.LengthSquared() > 0.001f) off.Normalize(); DrawLine(a + off * i, b + off * i, c, 1); } }
        private void SpawnParticle(Vector2 pos, Color col, float size, float life) { if (_particles.Count < 150) _particles.Add(new Particle { Position = pos, Velocity = new Vector2((float)_rng.NextDouble() * 200f - 100f, (float)_rng.NextDouble() * -180f), Life = life, MaxLife = life, Size = size, Color = col, RotVel = (float)_rng.NextDouble() * 6f - 3f }); }
        private void SpawnDamageNumber(Vector2 pos, float val, bool isHeal = false, bool isCrit = false, bool isPlayer = false) { if (_dmgNums.Count < 30) _dmgNums.Add(new DamageNumber { Position = pos + new Vector2((float)_rng.NextDouble() * 30f - 15f, 0f), Value = val, IsCrit = isCrit, IsHeal = isHeal }); }
        private void TriggerScreenShake(float duration, float intensity) { _shakeTimer = Math.Max(_shakeTimer, duration); _shakeIntensity = Math.Max(_shakeIntensity, intensity); }
        private bool KeyJustPressed(Keys k) => _kCurr.IsKeyDown(k) && _kPrev.IsKeyUp(k);
        private bool MouseJustReleased() => _mPrev.LeftButton == ButtonState.Pressed && _mCurr.LeftButton == ButtonState.Released;
        private Point MousePos() => _mCurr.Position;
        private Rectangle WelcomeBtn(int idx) => new Rectangle((BASE_W - 340) / 2, 246 + idx * 44, 340, 38);
        private Rectangle BackButtonRect() => new Rectangle(BASE_W / 2 - 140, BASE_H - 78, 280, 42);
        private Rectangle MusicButtonRect() => new Rectangle(BASE_W - 224, 18, 194, 38);
        private Rectangle LevelCellRect(int level) { int idx = level - 1, col = idx % 10, row = idx / 10; return new Rectangle(106 + col * (104 + 6), 172 + row * (42 + 6), 104, 42); }

        private void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen; _gfx.IsFullScreen = _fullscreen;
            _gfx.PreferredBackBufferWidth = _fullscreen ? GraphicsDevice.DisplayMode.Width : BASE_W;
            _gfx.PreferredBackBufferHeight = _fullscreen ? GraphicsDevice.DisplayMode.Height : BASE_H;
            _gfx.ApplyChanges(); RecalcRTRect();
        }

        private void ApplyWindowPreset(int width, int height)
        {
            _fullscreen = false; _gfx.IsFullScreen = false; _gfx.PreferredBackBufferWidth = width; _gfx.PreferredBackBufferHeight = height;
            _gfx.ApplyChanges(); RecalcRTRect();
        }

        // Audio setup
        private SoundEffect _musicLoop;
        private void ToggleMusic() { _musicEnabled = !_musicEnabled; ApplyMusicState(); }
        private void ApplyMusicState()
        {
            try
            {
                if (_musicInstance != null) { _musicInstance.Stop(); _musicInstance.Dispose(); _musicInstance = null; }
                if (_musicEnabled && _musicLoop != null) { _musicInstance = _musicLoop.CreateInstance(); _musicInstance.IsLooped = true; _musicInstance.Volume = 0.28f; _musicInstance.Play(); }
            }
            catch { }
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
            catch { _snd = new SoundManager(); _musicLoop = null; }
        }

        private SoundEffect CreateToneSound(float freq, float dur, float vol, float sweepTo = 0f)
        {
            const int rate = 22050; int samples = Math.Max(1, (int)(rate * dur));
            byte[] buffer = new byte[samples * 2];
            for (int i = 0; i < samples; i++) { float p = (float)i / samples, t = i / (float)rate; float f = sweepTo > 0f ? MathHelper.Lerp(freq, sweepTo, p) : freq; float e = (float)Math.Exp(-p * 4.5f); short s = (short)(Math.Sin(2 * Math.PI * f * t) * e * vol * short.MaxValue); buffer[i * 2] = (byte)(s & 0xff); buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff); }
            return new SoundEffect(buffer, rate, AudioChannels.Mono);
        }

        private SoundEffect CreateNoiseBurst(float dur, float vol, float sweepTo = 0f)
        {
            const int rate = 22050; int samples = Math.Max(1, (int)(rate * dur));
            byte[] buffer = new byte[samples * 2];
            for (int i = 0; i < samples; i++) { float p = (float)i / samples, e = (float)Math.Exp(-p * 5f); float tone = (float)(_rng.NextDouble() * 2.0 - 1.0); if (sweepTo > 0f) tone *= 0.65f + 0.35f * (float)Math.Sin(p * MathHelper.TwoPi * 4f); short s = (short)(tone * e * vol * short.MaxValue); buffer[i * 2] = (byte)(s & 0xff); buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff); }
            return new SoundEffect(buffer, rate, AudioChannels.Mono);
        }

        private SoundEffect CreateVictoryJingle()
        {
            const int rate = 22050; int samples = (int)(rate * 1.0f);
            byte[] buffer = new byte[samples * 2];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < samples; i++) { float p = (float)i / samples; int n = Math.Min(notes.Length - 1, (int)(p * notes.Length)); float e = (float)Math.Exp(-p * 3.8f); float sv = (float)Math.Sin(2 * Math.PI * notes[n] * i / rate) * e * 0.25f; short s = (short)(sv * short.MaxValue); buffer[i * 2] = (byte)(s & 0xff); buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff); }
            return new SoundEffect(buffer, rate, AudioChannels.Mono);
        }

        private SoundEffect CreateBossRoar() => CreateToneSound(60f, 0.42f, 0.42f, 32f);

        private SoundEffect CreateMusicLoop()
        {
            const int rate = 22050; float dur = 4f; int samples = (int)(rate * dur);
            byte[] buffer = new byte[samples * 2];
            float[] chord = { 196f, 246.94f, 293.66f, 392f };
            for (int i = 0; i < samples; i++) { float p = (float)i / samples; int step = (int)(p * 8f); float freq = chord[step % chord.Length]; float pulse = 0.5f + 0.5f * (float)Math.Sin(p * MathHelper.TwoPi * 2f); float note = (float)Math.Sin(2 * Math.PI * freq * i / rate); float harmony = (float)Math.Sin(2 * Math.PI * (freq * 2f) * i / rate) * 0.25f; float bass = (float)Math.Sin(2 * Math.PI * (freq * 0.5f) * i / rate) * 0.12f; short s = (short)((note * 0.55f + harmony + bass) * (0.18f + 0.12f * pulse) * short.MaxValue); buffer[i * 2] = (byte)(s & 0xff); buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff); }
            return new SoundEffect(buffer, rate, AudioChannels.Mono);
        }
    }
}
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the shared states for the AI combat system.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Base class for combat states.
    /// </summary>
    /// <summary>
    /// Base class for combat states.
    /// </summary>
    public abstract class CombatStateBase
    {
        /// <summary>
        /// Error message displayed when the user is facing the wrong way.
        /// </summary>
        const string FacingErrorMessage = "You are facing the wrong way!";
        /// <summary>
        /// Represents the error message for when the target is not in line of sight.
        /// </summary>
        const string LosErrorMessage = "Target not in line of sight";
        /// <summary>
        /// The constant string representing the battle stance.
        /// </summary>
        const string BattleStance = "Battle Stance";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents the desired range for a value.
        /// </summary>
        readonly int desiredRange;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;

        /// <summary>
        /// Represents a boolean value indicating whether backpedaling is occurring.
        /// </summary>
        bool backpedaling;
        /// <summary>
        /// Represents the start time of the backpedal.
        /// </summary>
        int backpedalStartTime;
        /// <summary>
        /// Represents a boolean value indicating whether there are any line of sight (LOS) obstacles.
        /// </summary>
        bool noLos;
        /// <summary>
        /// Represents the start time for the "no loss" period.
        /// </summary>
        int noLosStartTime;

        /// <summary>
        /// Represents the loop timer.
        /// </summary>
        private int loopTimer;
        /// <summary>
        /// Represents the last recorded health of the target.
        /// </summary>
        private int lastTargetHealth;

        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Initializes a new instance of the CombatStateBase class.
        /// </summary>
        public CombatStateBase(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, int desiredRange)
        {
            player = ObjectManager.Player;
            this.target = target;
            player.Target = target;

            this.botStates = botStates;
            this.container = container;
            this.desiredRange = desiredRange;

            WoWEventHandler.OnErrorMessage += OnErrorMessageCallback;

            loopTimer = 0;
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        public bool Update()
        {
            if (player.DeathsAtWp > 2 && player.CurrWpId != 0)
            {
                // Select new waypoint based on links
                var hotspot = container.GetCurrentHotspot();
                var waypoint = hotspot.Waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();
                string wpLinks = waypoint.Links.Replace(":0", "");
                if (wpLinks.EndsWith(" "))
                    wpLinks = wpLinks.Remove(wpLinks.Length - 1);
                string[] linkSplit = wpLinks.Split(' ');
                int randLink = random.Next() % linkSplit.Length;
                var linkWp = hotspot.Waypoints.Where(x => x.ID == Int32.Parse(linkSplit[randLink])).FirstOrDefault();

                Console.WriteLine($"Forcing teleport to linked WP: {linkWp.ID} after release due to deathcount > 2");
                player.LuaCall($"SendChatMessage('.npcb wp go {linkWp.ID}')");
                player.DeathsAtWp = 0;
            }

            // melee classes occasionally end up in a weird state where they are too close to hit the mob,
            // so we backpedal a bit to correct the position
            if (backpedaling && Environment.TickCount - backpedalStartTime > 500)
            {
                player.StopMovement(ControlBits.Back);
                backpedaling = false;
            }
            if (backpedaling)
                return true;

            // the server-side los check is broken on Kronos, so we have to rely on an error message on the client.
            // when we see it, move toward the unit a bit to correct the position.
            if (noLos && Environment.TickCount - noLosStartTime > 1000)
            {
                player.StopMovement(ControlBits.Front);
                noLos = false;
            }

            if (loopTimer == 0)
                lastTargetHealth = target.Health;
            loopTimer++;
            if (loopTimer > 150)
            {
                if (target.Health == lastTargetHealth)
                {
                    var ran = random.Next(0, 4);
                    if (ran == 0 && loopTimer < 160)
                    {
                        player.StartMovement(ControlBits.Back);
                        player.StartMovement(ControlBits.StrafeLeft);
                        player.Jump();
                    }
                    else if (ran == 1 && loopTimer < 160)
                    {
                        player.StartMovement(ControlBits.Back);
                        player.StartMovement(ControlBits.StrafeRight);
                        player.Jump();
                    }
                    else
                    {
                        player.StopAllMovement();
                        var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                        player.MoveToward(nextWaypoint);
                    }
                    if (loopTimer > 200)
                        loopTimer = 0;
                    return true;
                }
            }

            if (noLos)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
                return true;
            }

            // see if somebody else stole the mob we were targeting
            if (target.TappedByOther || player.Health <= 0 || target.Name == player.BotFriend)
            {
                CleanUp();
                return true;
            }

            // when killing certain summoned units (like totems), our local reference to target will still have 100% health even after the totem is destroyed
            // so we need to lookup the target again in the object manager, and if it's null, we can assume it's dead and leave combat.
            var checkTarget = ObjectManager.Units.FirstOrDefault(u => u.Guid == target.Guid);
            if (target.Health == 0 || target.TappedByOther || checkTarget == null)
            {
                player.StopAllMovement();

                if (ObjectManager.Aggressors.Count() == 0 && player.Class == Class.Warrior && player.CurrentStance != BattleStance) TryUseAbility(BattleStance);

                if (Wait.For("PopCombatState", 1500))
                {
                    CleanUp();
                    botStates.Push(new LootState(botStates, container, target));
                }

                return true;
            }

            // ensure the correct target is set
            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position))
                player.Face(target.Position);

            // make sure casters don't move or anything while they're casting by returning here
            if ((player.IsCasting || player.IsChanneling) && player.Class != Class.Warrior)
                return true;

            // ensure we're in range of the target
            if (player.Position.DistanceTo(target.Position) > desiredRange)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else if (player.IsMoving && player.Position.DistanceTo(target.Position) < desiredRange - 1)
                player.StopAllMovement();

            // ensure auto-attack is turned on ONLY if player does not have a wand
            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (wand == null)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var autoAttackAction = player.Class == Class.Warrior ? 84 : 12;

                    // first check if auto attack is in the correct action slot
                    var isInCorrectSpot = player.LuaCallWithResults($"{{0}} = IsAttackAction({autoAttackAction})");
                    if (isInCorrectSpot.Length == 0 || isInCorrectSpot[0] != "1")
                    {
                        var error = "You must place the <Attack> action from your spellbook on the last slot on your primary action bar.";
                        player.LuaCall($"message('{error}')");
                        return false;
                    }

                    var autoAttackLuaScript = $"if IsCurrentAction('{autoAttackAction}') == nil then CastSpellByName('Attack') end";
                    player.LuaCall(autoAttackLuaScript);
                }
                else
                {
                    player.LuaCall("StartAttack()");
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to cast a spell with the specified name and range.
        /// </summary>
        public void TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false) =>
                    TryCastSpellInternal(name, minRange, maxRange, condition, callback, castOnSelf);

        /// <summary>
        /// Tries to cast a spell with the specified name.
        /// </summary>
        public void TryCastSpell(string name, bool condition = true, Action callback = null, bool castOnSelf = false) =>
                    TryCastSpellInternal(name, 0, int.MaxValue, condition, callback, castOnSelf);

        /// <summary>
        /// Tries to cast a spell with the given parameters.
        /// </summary>
        void TryCastSpellInternal(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && distanceToTarget >= minRange && distanceToTarget <= maxRange && condition && !player.IsStunned && ((!player.IsCasting && !player.IsChanneling) || player.Class == Class.Warrior))
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var castOnSelfString = castOnSelf ? ",1" : "";
                    player.LuaCall($"CastSpellByName(\"{name}\"{castOnSelfString})");
                    callback?.Invoke();
                }
                else
                {
                    var targetGuid = castOnSelf ? player.Guid : target.Guid;
                    player.CastSpell(name, targetGuid);
                    callback?.Invoke();
                }
            }
        }

        /// <summary>
        /// Tries to use an ability with the given name, required resource, condition, and callback.
        /// </summary>
        // shared by 
        public void TryUseAbility(string name, int requiredResource = 0, bool condition = true, Action callback = null)
        {
            int playerResource = 0;

            if (player.Class == Class.Warrior)
                playerResource = player.Rage;
            else if (player.Class == Class.Rogue)
                playerResource = player.Energy;
            // todo: feral druids (bear/cat form)

            if (player.IsSpellReady(name) && playerResource >= requiredResource && condition && !player.IsStunned && !player.IsCasting)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpellByName('{name}')");
                    callback?.Invoke();
                }
                else
                {
                    player.CastSpell(name, target.Guid);
                    callback?.Invoke();
                }
            }
        }

        /// <summary>
        /// Tries to use an ability by its ID.
        /// </summary>
        // https://vanilla-wow.fandom.com/wiki/API_CastSpell
        // The id is counted from 1 through all spell types (tabs on the right side of SpellBookFrame).
        public void TryUseAbilityById(string name, int id, int requiredRage = 0, bool condition = true, Action callback = null)
        {
            if (player.IsSpellReady(name) && player.Rage >= requiredRage && condition && !player.IsStunned && !player.IsCasting)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpell({id}, 'spell')");
                    callback?.Invoke();
                }
                else
                {
                    player.CastSpell(name, target.Guid);
                    callback?.Invoke();
                }

            }
        }

        /// <summary>
        /// Cleans up the player's movement, pops the top state from the botStates stack, and unsubscribes from the OnErrorMessage event.
        /// </summary>
        void CleanUp()
        {
            player.StopAllMovement();
            botStates.Pop();
            WoWEventHandler.OnErrorMessage -= OnErrorMessageCallback;
        }

        /// <summary>
        /// Callback method for handling error messages.
        /// </summary>
        void OnErrorMessageCallback(object sender, OnUiMessageArgs e)
        {
            if (e.Message == FacingErrorMessage && !backpedaling)
            {
                backpedaling = true;
                backpedalStartTime = Environment.TickCount;
                player.StartMovement(ControlBits.Back);
            }
            else if (e.Message == LosErrorMessage)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
            }
        }
    }
}

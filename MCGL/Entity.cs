using System;
using System.Collections.Generic;
using System.Linq;

namespace MCGL
{
    public enum EntityType
    {
        ANY,
        Player,
        Item,
        XPOrb,
        LeashKnot,
        Painting,
        ItemFrame,
        ArmorStand,
        EnderCrystal,
        ThrownEgg,
        Arrow,
        Snowball,
        Fireball,
        SmallFireball,
        ThrownEnderpearl,
        EyeOfEnderSignal,
        ThrownPotion,
        ThrownExpBottle,
        WitherSkull,
        FireworksRocketEntity,
        PrimedTnt,
        FallingSand,
        MinecartCommandBlock,
        Boat,
        MinecartRideable,
        MinecartChest,
        MinecartFurnace,
        MinecartTNT,
        MinecartHopper,
        MinecartSpawner,
        Mob,
        Monster,
        Creeper,
        Skeleton,
        Spider,
        Giant,
        Zombie,
        Slime,
        Ghast,
        PigZombie,
        Enderman,
        CaveSpider,
        Silverfish,
        Blaze,
        LavaSlime,
        EnderDragon,
        WitherBoss,
        Witch,
        Endermite,
        Guardian,
        Shulker,
        Rabbit,
        Bat,
        Pig,
        Sheep,
        Cow,
        Chicken,
        Squid,
        Wolf,
        MushroomCow,
        SnowMan,
        Ozelot,
        VillagerGolem,
        EntityHorse,
        Villager
    }

    public enum SelectorType
    {
        SINGLE,
        MULTIPLE,
        RANDOM,
    }

    public enum Gamemode
    {
        ANY,
        SURVIVAL,
        CREATIVE,
        ADVENTURE,
        SPECTATOR
    }

    class EntityScore
    {
        public int? min = null;
        public int? max = null;
        
        public EntityScore(int? min, int? max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public class Entities
    {
        public static readonly Entities Player = GetSingle(EntityType.Player);
        public static readonly Entities AllPlayers = Get(EntityType.Player, null);
        public static readonly Entities ArmorStand = GetSingle(EntityType.ArmorStand);
        public static readonly Entities ArmorStands = Get(EntityType.ArmorStand, null);

        private static readonly Random random = new Random();

        SelectorType selectorType = SelectorType.MULTIPLE;
        EntityType entityType = EntityType.ANY;
        Gamemode gamemode = Gamemode.ANY;

        int? count = null;

        string name = null;
        string team = null;

        int? minRadius = null;
        int? maxRadius = null;

        int? x = null,
             y = null,
             z = null,
            dx = null,
            dy = null,
            dz = null;

        int? rotationMinX = null,
             rotationMaxX = null,
             rotationMinY = null,
             rotationMaxY = null;

        int? experienceMin = null;
        int? experienceMax = null;

        Dictionary<string, EntityScore> scores = new Dictionary<string, EntityScore>();
        
        string tag = null;

        public static Entities Copy(Entities entities)
        {
            var copy = new Entities();
            Copy(entities, copy);
            return copy;
        }

        public static void Copy(Entities source, Entities target)
        {
            target.selectorType = source.selectorType;
            target.entityType = source.entityType;
            target.gamemode = source.gamemode;

            target.count = source.count;
            target.name = source.name;
            target.team = source.team;

            target.minRadius = source.minRadius;
            target.maxRadius = source.maxRadius;

            target.x = source.x;
            target.y = source.y;
            target.z = source.z;
            target.dx = source.dx;
            target.dy = source.dy;
            target.dz = source.dz;

            target.tag = source.tag;

            target.rotationMinX = source.rotationMinX;
            target.rotationMinY = source.rotationMinY;
            target.rotationMaxX = source.rotationMaxX;
            target.rotationMaxY = source.rotationMaxY;
            target.experienceMin = source.experienceMin;
            target.experienceMax = source.experienceMax;

            foreach (var scoreName in source.scores.Keys)
            {
                var sourceScore = source.scores[scoreName];
                target.scores[scoreName] = new EntityScore(sourceScore.min, sourceScore.max);
            }
        }

        public static Entities Get(EntityType type = EntityType.ANY, int? count = null)
        {
            var entities = new Entities();
            entities.selectorType = count == 1 ? SelectorType.SINGLE : SelectorType.MULTIPLE;
            entities.entityType = type;
            entities.count = count;
            return entities;
        }

        public static Entities GetSingle(EntityType type = EntityType.ANY)
        {
            var entities = new Entities();
            entities.selectorType = SelectorType.SINGLE;
            entities.entityType = type;
            entities.count = 1;
            return entities;
        }

        public static Entities GetRandom(EntityType type = EntityType.ANY, int? count = 1)
        {
            var entities = new Entities();
            entities.selectorType = SelectorType.RANDOM;
            entities.entityType = type;
            entities.count = count;
            return entities;
        }

        public Entities WithType(EntityType entityType)
        {
            Entities entities = Copy(this);
            entities.entityType = entityType;
            return entities;
        }

        public Entities WithName(string name)
        {
            var entities = Copy(this);
            entities.name = name;
            return entities;
        }

        public Entities WithTeam(string team)
        {
            var entities = Copy(this);
            entities.team = team;
            return entities;
        }

        public Entities WithScore(string objective, int? min, int? max)
        {
            var entities = Copy(this);
            EntityScore score = new EntityScore(min, max);
            entities.scores[objective] = score;
            return entities;
        }

        public Entities WithMaxScore(string objective, int max)
        {
            return WithScore(objective, null, max);
        }

        public Entities WithMinScore(string objective, int min)
        {
            return WithScore(objective, min, null);
        }

        public Entities ClearScores()
        {
            var entities = Copy(this);
            entities.scores.Clear();
            return entities;
        }

        public Entities AtCoordinates(int x, int y, int z, int? maxRadius = 0, int? minRadius = null)
        {
            var entities = Copy(this);
            entities.x = x;
            entities.y = y;
            entities.z = z;
            entities.dx = null;
            entities.dy = null;
            entities.dz = null;
            entities.minRadius = minRadius;
            entities.maxRadius = maxRadius;
            return entities;
        }

        public Entities InArea(int xMin, int yMin, int zMin, int xMax, int yMax, int zMax)
        {
            var entities = Copy(this);
            entities.x = x;
            entities.y = y;
            entities.z = z;
            entities.dx = xMax - xMin;
            entities.dy = yMax - yMin;
            entities.dz = zMax - zMin;
            entities.minRadius = null;
            entities.maxRadius = null;
            return entities;
        }

        public Entities InRadius(int? min, int? max)
        {
            var entities = Copy(this);
            entities.minRadius = min;
            entities.maxRadius = max;
            return entities;
        }

        public Entities WithRotation(int? minXRotation, int?maxXRotation, int? minYRotation, int? maxYRotation)
        {
            var entities = Copy(this);
            entities.rotationMinX = minXRotation;
            entities.rotationMaxX = maxXRotation;
            entities.rotationMinY = minYRotation;
            entities.rotationMaxY = maxYRotation;
            return entities;
        }

        public Entities WithHorizontalRotation(int? min, int? max)
        {
            return WithRotation(min, max, null, null);
        }

        public Entities WithVerticalRotation(int? min, int? max)
        {
            return WithRotation(null, null, min, max);
        }

        public Entities WithGamemode(Gamemode gamemode)
        {
            var entities = Copy(this);
            entities.gamemode = gamemode;
            return entities;
        }

        public Entities WithExperienceLevel(int? min, int? max)
        {
            var entities = Copy(this);
            entities.experienceMin = min;
            entities.experienceMax = max;
            return entities;
        }

        public Entities WithTag(string tag)
        {
            var entities = Copy(this);
            entities.tag = tag;
            return entities;
        }

        public string GetSelectorString()
        {
            var elements = new List<string>();
            var result = String.Empty;

            if (selectorType == SelectorType.SINGLE)
            {
                if (entityType == EntityType.Player)
                {
                    result = "@p";
                }
                else
                {
                    result = "@e";
                    elements.Add("c=1");
                }
            }
            else if (selectorType == SelectorType.MULTIPLE)
            {
                if (entityType == EntityType.Player)
                {
                    result = "@a";
                }
                else
                {
                    result = "@e";
                }
                if (count != null) elements.Add("c=" + count);
            }
            else if (selectorType == SelectorType.RANDOM)
            {
                result = "@r";
                if (count != null) elements.Add("count=" + count);
            }

            if (name != null) elements.Add("name=" + name);
            if (team != null) elements.Add("team=" + team);
            if (entityType != EntityType.Player && entityType != EntityType.ANY) elements.Add("type=" + entityType.ToString());
            if (experienceMin != null) elements.Add("lm=" + experienceMin);
            if (experienceMax != null) elements.Add("l=" + experienceMax);
            if (gamemode != Gamemode.ANY) elements.Add("m=" + gamemode.ToString().ToLower());

            if (x != null && y != null && z != null)
            {
                elements.Add("x=" + x);
                elements.Add("y=" + y);
                elements.Add("z=" + z);
            }

            if (dx != null && dy != null && dz != null)
            {
                elements.Add("dx=" + dx);
                elements.Add("dy=" + dy);
                elements.Add("dz=" + dz);
            }

            if (minRadius != null) elements.Add("rm=" + minRadius);
            if (maxRadius != null) elements.Add("r=" + maxRadius);
            if (rotationMinX != null) elements.Add("rxm=" + rotationMinX);
            if (rotationMaxX != null) elements.Add("rx=" + rotationMaxX);
            if (rotationMinY != null) elements.Add("rym=" + rotationMinY);
            if (rotationMaxY != null) elements.Add("ry=" + rotationMaxY);
            if (tag != null) elements.Add("tag=" + tag);

            foreach (var scoreName in scores.Keys)
            {
                var score = scores[scoreName];
                if (score.min != null) elements.Add("score_" + scoreName + "_min=" + score.min);
                if (score.max != null) elements.Add("score_" + scoreName + "=" + score.max);
            }

            if (elements.Count > 0) {
                result += "[" + String.Join(",", elements) + "]";
            }

            return result;
        }

        public static string RandomString(int length = 16)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

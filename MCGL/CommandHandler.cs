using NBT.IO;
using NBT.Tags;
using System;
using System.Collections.Generic;
using System.IO;

namespace MCGL
{
    public abstract class Condition
    {
        public string id = Entities.RandomString(16);

        public abstract void Init(Chain chain);
        public abstract void Dispose(Chain chain);
    }

    public class EntityCanBeFoundCondition : Condition
    {
        public Entities entities;
        public NBTObject NBT;

        public string detectBlock;
        public Coordinates detectCoords;
        public int? detectDataValue;
        
        public EntityCanBeFoundCondition(Entities entities, NBTObject NBT = null, string detectBlock = null, Coordinates detectCoords = null, int? detectDataValue = null)
        {
            this.entities = entities;
            this.NBT = NBT == null ? new NBTObject() : NBT;

            this.detectBlock = detectBlock;
            this.detectCoords = detectCoords;
            this.detectDataValue = detectDataValue;
        }

        public override void Init(Chain chain)
        {
            if (detectBlock != null)
                chain.PushExecutionAs(entities, NBT, null, detectBlock, detectCoords, detectDataValue);

            chain.TestForEntity(entities, NBT);
            chain.AddTag(id, chain.baseEntity, chain.baseNBT, true);

            if (detectBlock != null)
                chain.PopExecution();
        }

        public override void Dispose(Chain chain)
        {
            chain.SimpleCommand(new Command("scoreboard players tag " + chain.baseEntity.GetSelectorString() + " remove " + id + " " + chain.baseNBT.ToString(), CommandType.CHAIN));
        }
    }

    public class AreasAreEqualCondition : Condition
    {
        Area source;
        Area target;
        string mode;
        
        public AreasAreEqualCondition(Area source, Area target, string mode = "all")
        {
            this.source = source;
            this.target = target;
            this.mode = mode;
        }

        public override void Init(Chain chain)
        {
            chain.TestForBlocks(source, target.min, mode);
            chain.AddTag(id, chain.baseEntity, chain.baseNBT, true);
        }

        public override void Dispose(Chain chain)
        {
            chain.SimpleCommand(new Command("scoreboard players tag " + chain.baseEntity.GetSelectorString() + " remove " + id + " " + chain.baseNBT.ToString(), CommandType.CHAIN));
        }
    }

    public class BlockExistsAtCondition : Condition
    {
        string block;
        Coordinates coordinates;
        NBTObject NBT;
        int dataValue;

        public BlockExistsAtCondition(Coordinates location, string block, int dataValue = -1, NBTObject NBT = null)
        {
            this.block = block;
            this.NBT = NBT == null ? new NBTObject() : NBT;
            this.dataValue = dataValue;
            this.coordinates = location;
        }

        public override void Init(Chain chain)
        {
            chain.TestForBlock(coordinates, block, dataValue, NBT);
            chain.AddTag(id, chain.baseEntity, chain.baseNBT, true);
        }

        public override void Dispose(Chain chain)
        {
            chain.SimpleCommand(new Command("scoreboard players tag " + chain.baseEntity.GetSelectorString() + " remove " + id + " " + chain.baseNBT.ToString(), CommandType.CHAIN));
        }
    }

    public enum CommandType
    {
        IMPULSE,
        REPEAT,
        CHAIN,
        CHAIN_CONDITIONAL
    }

    public class Command
    {
        public CommandType type;
        public string command;

        public Command(string command, CommandType type)
        {
            this.command = command;
            this.type = type;
        }
    }

    public class Chain
    {
        Stack<Condition> conditions = new Stack<Condition>();
        Stack<Condition> executions = new Stack<Condition>();

        List<Command> commands = new List<Command>();

        public Entities baseEntity;
        public NBTObject baseNBT;

        Entities execution;
        Entities executionWithout;
        NBTObject executionNBT;

        Stack<Coordinates> executionCoords = new Stack<Coordinates>();
        Stack<string> detects = new Stack<string>();

        bool ignore = false;
        int singleExecution = 0;
        int singleCondition = 0;

        public CommandType next = CommandType.CHAIN;

        public Chain(CommandType type = CommandType.IMPULSE, bool hideBaseEntity = false)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo) System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            next = type;
            var id = Entities.RandomString(32);

            baseEntity = Entities.GetSingle(EntityType.ArmorStand).WithName(id);
            baseNBT = new NBTObject().Set("CustomName", id);
            if (hideBaseEntity)
                baseNBT = baseNBT.Set("Invisible", true);

            SummonEntity(EntityType.ArmorStand, new RelativeCoordinates(0, 0, 0), baseNBT);
        }

        public Command[] GetCommands()
        {
            Kill(baseEntity);
            return commands.ToArray();
        }

        public void GenerateSchematics(string path, bool trackOutput = false)
        {
            var commands = GetCommands();

            NBTFile file = new NBTFile();
            file.RootTagName = "Schematic";

            file.RootTag.Add("Materials", new TagString("Alpha"));

            List<List<Command>> saveCommands = new List<List<Command>>();
            saveCommands.Add(new List<Command>());

            short current = 0;
            short currentList = 0;

            int maxWidth = 0;
            int width = 32;

            for (int i = 0; i < commands.Length;)
            {
                var command = commands[i];

                if (current < width)
                {
                    saveCommands[currentList].Add(command);
                    current++;
                    i++;
                    maxWidth++;
                }
                else if (command.type == CommandType.CHAIN_CONDITIONAL)
                {
                    width++;
                }
                else if (command.type == CommandType.CHAIN && commands[i - 1].type == CommandType.CHAIN_CONDITIONAL)
                {
                    width++;
                }
                else
                {
                    saveCommands.Add(new List<Command>());
                    current = 0;
                    currentList++;
                }
            }

            if (maxWidth < width)
                width = maxWidth;

            int height = 1;
            int length = saveCommands.Count;
            
            file.RootTag.Add("Width", new TagShort((short) width));
            file.RootTag.Add("Length", new TagShort((short) length));
            file.RootTag.Add("Height", new TagShort((short) height));
            
            var blockIds = new Byte[width * height * length];
            var blockData = new Byte[width * height * length];

            TagList tileEntities = new TagList(TagTypes.TagCompound);

            for (int row = 0; row < saveCommands.Count; row++)
            {
                if (saveCommands[row].Count == width)
                {
                    var indices = new List<int>();
                    CreateCommandblockLines(trackOutput, blockIds, blockData, tileEntities, row, saveCommands, indices, width);
                    break;
                }
            }

            file.RootTag.Add("Blocks", new TagByteArray(blockIds));
            file.RootTag.Add("Data", new TagByteArray(blockData));

            file.RootTag.Add("Entities", new TagList(TagTypes.TagCompound));
            file.RootTag.Add("TileEntities", tileEntities);

            if (File.Exists(path))
                File.Delete(path);

            file.Save(path);
        }

        private void CreateCommandblockLines(bool trackOutput, Byte[] blockIds, Byte[] blockData, TagList tileEntities, int row, List<List<Command>> commands, List<int> indices, int maxWidth, int startAt = 0)
        {
            var current = commands[row];
            if (row % 2 != 0)
                current.Reverse();
            
            for (int x = 0; x < current.Count; x++)
            {
                int realX = startAt + x;

                Command command = current[x];

                switch (command.type)
                {
                    case CommandType.IMPULSE:
                        blockIds[row * maxWidth + realX] = 137;
                        break;
                    case CommandType.REPEAT:
                        blockIds[row * maxWidth + realX] = 210;
                        break;
                    default:
                        blockIds[row * maxWidth + realX] = 211;
                        break;
                }

                byte data = (byte) (row % 2 == 0 ? 5 : 4);

                if ((data == 4 && x == 0) || (data == 5 && x == current.Count - 1))
                    data = 3;

                if (command.type == CommandType.CHAIN_CONDITIONAL)
                    data += 8;

                blockData[row * maxWidth + realX] = data;

                TagCompound cmd = new TagCompound();

                cmd.Add("id", new TagString("Control"));
                cmd.Add("x", new TagInt(realX));
                cmd.Add("y", new TagInt(0));
                cmd.Add("z", new TagInt(row));
                cmd.Add("CustomName", new TagString("@"));

                cmd.Add("auto", new TagByte((byte) (command.type == CommandType.CHAIN || command.type == CommandType.CHAIN_CONDITIONAL ? 1 : 0)));
                
                cmd.Add("Command", new TagString(command.command));
                cmd.Add("TrackOutput", new TagByte((byte) (trackOutput ? 1 : 0)));
                cmd.Add("SuccessCount", new TagInt(0));

                tileEntities.Add(cmd);
            }

            indices.Add(row);

            if (row + 1 < commands.Count && !indices.Contains(row + 1))
            {
                var index = (row % 2 == 0) ? current.Count - commands[row + 1].Count : startAt;
                if (current.Count == commands[row + 1].Count)
                    index = startAt;
                CreateCommandblockLines(trackOutput, blockIds, blockData, tileEntities, row + 1, commands, indices, maxWidth, index);
            }

            if (row - 1 >= 0 && !indices.Contains(row - 1))
            {
                var index = (row % 2 == 0) ? startAt : current.Count - commands[row - 1].Count;
                if (current.Count == commands[row - 1].Count)
                    index = startAt;
                CreateCommandblockLines(trackOutput, blockIds, blockData, tileEntities, row - 1, commands, indices, maxWidth, index);
            }
        }

        public Condition EntityCanBeFound(Entities entities, NBTObject NBT = null, string detectBlock = null, Coordinates detectCoords = null, int? detectDataValue = null)
        {
            return new EntityCanBeFoundCondition(entities, NBT, detectBlock, detectCoords, detectDataValue);
        }

        public Condition AreasAreEqual(Area source, Area target, string mode = "all")
        {
            return new AreasAreEqualCondition(source, target, mode);
        }

        public Condition BlockExistsAt(Coordinates location, string block, int dataValue = -1, NBTObject NBT = null)
        {
            return new BlockExistsAtCondition(location, block, dataValue, NBT);
        }

        public Chain ExecuteAs(Entities entities, NBTObject NBT = null, Coordinates coordinates = null, string detectBlock = null, Coordinates detectCoordinates = null, int? detectDataValue = null)
        {
            PushExecutionAs(entities, NBT, coordinates, detectBlock, detectCoordinates, detectDataValue);
            singleExecution++;
            return this;
        }

        public Chain IfCondition(Condition condition)
        {
            PushCondition(condition);
            singleCondition++;
            return this;
        }

        public void PushCondition(Condition condition)
        {
            condition.Init(this);
            conditions.Push(condition);
        }

        public void PopCondition()
        {
            conditions.Pop().Dispose(this);
        }

        public void PushExecutionAs(Entities entities, NBTObject NBT = null, Coordinates coordinates = null, string detectBlock = null, Coordinates detectCoordinates = null, int? detectDataValue = null)
        {
            string detect = null;

            if (detectBlock != null && (detectCoordinates == null || detectDataValue == null))
                throw new ArgumentNullException("All of the detect parameters have to be set.");

            if (detectBlock != null)
                detect = "detect " + detectCoordinates.ToString() + " " + detectBlock + " " + detectDataValue;

            if (coordinates == null)
                coordinates = RelativeCoordinates.Zero;

            if (NBT == null)
                NBT = new NBTObject();

            if (execution != null)
            {
                RemoveTag("MCGL_EXECUTING", executionWithout);
                execution = null;
                var cond = new EntityCanBeFoundCondition(executionWithout, executionNBT);
                cond.Init(this);
                executions.Push(cond);
            }

            AddTag("MCGL_EXECUTING", entities, NBT);
            executionWithout = entities;
            execution = entities.WithTag("MCGL_EXECUTING");
            executionNBT = NBT;
            executionCoords.Push(coordinates);
            detects.Push(detect);
        }

        public void PopExecution()
        {
            execution = null;
            RemoveTag("MCGL_EXECUTING", executionWithout, executionNBT);

            if (executions.Count > 0)
            {
                var cond = ((EntityCanBeFoundCondition) executions.Pop());
                AddTag("MCGL_EXECUTING", executionWithout, cond.NBT);
                cond.Dispose(this);
                executionWithout = cond.entities;
                execution = cond.entities.WithTag("MCGL_EXECUTING");
            }

            executionCoords.Pop();
            detects.Pop();
        }

        public void Ignore()
        {
            ignore = true;
        }

        public void StopIgnore()
        {
            ignore = false;
        }

        public string GetConditionString()
        {
            var result = String.Empty;
            foreach (var condition in conditions)
            {
                result += "execute " + baseEntity.WithTag(condition.id).GetSelectorString() + " ~ ~ ~ ";
            }
            foreach (var condition in executions)
            {
                result += "execute " + baseEntity.WithTag(condition.id).GetSelectorString() + " ~ ~ ~ ";
            }
            return result;
        }

        public string GetExecutionString()
        {
            var result = GetConditionString();
            if (execution != null)
            {
                var detect = detects.Peek();
                result += "execute " + execution.GetSelectorString() + " " + executionCoords.Peek().ToString() + " " + (detect == null ? "" : detect + " ");
            }
            return result;
        }

        public void EntityCommand(Command command, Entities entities)
        {
            var cmd = GetExecutionString();
            if (cmd.EndsWith(entities.GetSelectorString() + " ~ ~ ~ "))
                cmd = GetConditionString();
            cmd += command.command;
            SimpleCommand((new Command(cmd, command.type)));
        }

        public void CombineCommand(Command command)
        {
            SimpleCommand(new Command((ignore ? "" : GetExecutionString()) + command.command, command.type));
        }

        public void SimpleCommand(Command command)
        {
            commands.Add(command);
            next = CommandType.CHAIN;

            if (singleCondition > 0)
            {
                singleCondition--;
                PopCondition();
            }

            if (singleExecution > 0)
            {
                singleExecution--;
                PopExecution();
            }
        }

        public void CustomCommand(string command, bool ifPrevious = false)
        {
            CombineCommand(new Command(command, CurrentType(ifPrevious)));
        }

        public void GiveAchievement(string achievement, Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("achievement give " + achievement + " " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void TakeAchievement(string achievement, Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("achievement take " + achievement + " " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void SetBlockData(Coordinates coordinates, NBTObject NBT, bool ifPrevious = false)
        {
            CombineCommand(new Command("blockdata " + coordinates.ToString() + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void ClearInventory(Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("clear " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void ClearItemFromInventory(Entities players, string item, int dataValue = -1, int amount = -1, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            EntityCommand(new Command("clear " + players.GetSelectorString() + " " + item + " " + dataValue + " " + amount + " " + NBT.ToString(), CurrentType(ifPrevious)), players);
        }

        public void CloneArea(Area source, Coordinates target, string mask = "replace", string mode = "normal", string block = null, bool ifPrevious = false)
        {
            if (mask == "filtered" && block == null)
                throw new ArgumentException("Block has to be specified in order to use the \"filtered\" mask mode.");
            CombineCommand(new Command("clone " + source.ToString() + " " + target.ToString() + " " + mask + " " + mode + (block != null ? " " + block : ""), CurrentType(ifPrevious)));
        }

        public void SetDefaultGamemode(string gamemode, bool ifPrevious = false)
        {
            CombineCommand(new Command("defaultgamemode " + gamemode, CurrentType(ifPrevious)));
        }

        public void SetDifficulty(string difficulty, bool ifPrevious = false)
        {
            CombineCommand(new Command("difficulty " + difficulty, CurrentType(ifPrevious)));
        }

        public void GiveEffect(Entities entities, string effect, int duration = 30, int amplifier = 0, bool hideParticles = false, bool ifPrevious = false)
        {
            EntityCommand(new Command("effect " + entities.GetSelectorString() + " " + effect + " " + duration + " " + amplifier + " " + hideParticles, CurrentType(ifPrevious)), entities);
        }

        public void TakeEffect(Entities entities, string effect, bool ifPrevious = false)
        {
            GiveEffect(entities, effect, 0, 0, true, ifPrevious);
        }

        public void ClearEffects(Entities entities, bool ifPrevious = false)
        {
            EntityCommand(new Command("effect " + entities.GetSelectorString() + " clear", CurrentType(ifPrevious)), entities);
        }

        public void Enchant(Entities players, string enchantment, int level = 1, bool ifPrevious = false)
        {
            if (level < 1)
                throw new ArgumentException("Level must be greater than zero.");
            EntityCommand(new Command("enchant " + players.GetSelectorString() + " " + enchantment + " " + level, CurrentType(ifPrevious)), players);
        }

        public void SetEntityData(Entities entities, NBTObject NBT, bool ifPrevious = false)
        {
            EntityCommand(new Command("entitydata " + entities.GetSelectorString() + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        public void FillArea(Area area, string block, int dataValue = 0, string mode = "keep", NBTObject NBT = null, bool ifPrevious = false)
        {
            if (mode == "replace")
                throw new ArgumentException("Cannot use mode \"replace\". Use ReplaceArea() instead.");
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("fill " + area.ToString() + " " + block + " " + dataValue + " " + mode + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void ReplaceArea(Area area, string block, int dataValue = 0, string replaceWith = null, int replaceDataValue = -1, bool ifPrevious = false)
        {
            if (replaceWith == null)
                CombineCommand(new Command("fill " + area.ToString() + " " + block + " " + dataValue + " replace", CurrentType(ifPrevious)));
            else
                CombineCommand(new Command("fill " + area.ToString() + " " + block + " " + dataValue + " replace " + replaceWith + " " + replaceDataValue, CurrentType(ifPrevious)));
        }

        public void SetGamemode(Entities players, string gamemode, bool ifPrevious = false)
        {
            EntityCommand(new Command("gamemode " + gamemode + " " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void SetGamerule(string gamerule, object value, bool ifPrevious = false)
        {
            CombineCommand(new Command("gamerule " + gamerule + " " + value.ToString(), CurrentType(ifPrevious)));
        }

        public void GiveItem(Entities players, string item, int amount = 1, int dataValue = 0, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            EntityCommand(new Command("give " + players.GetSelectorString() + " " + item + " " + amount + " " + dataValue + " " + NBT.ToString(), CurrentType(ifPrevious)), players);
        }

        public void Kill(Entities entities, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                EntityCommand(new Command("kill " + entities.GetSelectorString(), CurrentType(ifPrevious)), entities);
            else
            {
                AddTag("MCGL_KILL", entities, NBT);
                Kill(entities.WithTag("MCGL_KILL"));
            }
        }

        public void SendStatusMessage(string message, bool ifPrevious = false)
        {
            if (execution == null)
                throw new Exception("Call PushExecutionAs() first.");
            CombineCommand(new Command("me " + message, CurrentType(ifPrevious)));
        }

        public void ShowParticles(string particle, Coordinates location, Coordinates distance, double speed, int count = 0, string mode = "default", Entities players = null, string parameters = "", bool ifPrevious = false)
        {
            if (players == null)
                CombineCommand(new Command("particle " + particle + " " + location.ToString() + " " + distance.ToString() + " " + speed + " " + count + " " + mode, CurrentType(ifPrevious)));
            else
                CombineCommand(new Command("particle " + particle + " " + location.ToString() + " " + distance.ToString() + " " + speed + " " + count + " " + mode + " " + players.GetSelectorString() + " " + parameters, CurrentType(ifPrevious)));
        }

        public void PlaySound(string sound, string source, Entities players, Coordinates location = null, double volume = 1.0, double pitch = 1.0, double minimumVolume = 0.0, bool ifPrevious = false)
        {
            if (location == null)
                EntityCommand(new Command("playsound " + sound + " " + source + " " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
            else
                EntityCommand(new Command("playsound " + sound + " " + source + " " + players.GetSelectorString() + " " + location.ToString() + " " + volume + " " + pitch + " " + minimumVolume, CurrentType(ifPrevious)), players);
        }

        public void ReplaceBlockItem(Coordinates location, string slot, string item, int amount = 1, int dataValue = 0, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("replaceitem block " + location.ToString() + " " + slot + " " + item + " " + amount + " " + dataValue + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void ReplaceEntityItem(Entities entities, string slot, string item, int amount = 1, int dataValue = 0, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("replaceitem entity " + entities.GetSelectorString() + " " + slot + " " + item + " " + amount + " " + dataValue + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void SendChatMessage(string message, bool ifPrevious = false)
        {
            CombineCommand(new Command("say " + message, CurrentType(ifPrevious)));
        }

        public void SendPrivateMessage(Entities targets, string message, bool ifPrevious = false)
        {
            CombineCommand(new Command("tell " + targets.GetSelectorString() + " " + message, CurrentType(ifPrevious)));
        }

        public void SendRawChatMessage(Entities targets, RawText text, bool ifPrevious = false)
        {
            EntityCommand(new Command("tellraw " + targets.GetSelectorString() + " " + text.ToString(), CurrentType(ifPrevious)), targets);
        }

        public void SetBlock(Coordinates location, string block, int dataValue = 0, string mode = "replace", NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("setblock " + location.ToString() + " " + block + " " + dataValue + " " + mode + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void SetGlobalSpawn(Coordinates location, bool ifPrevious = false)
        {
            CombineCommand(new Command("setworldspawn " + location.ToString(), CurrentType(ifPrevious)));
        }

        public void SetSpawnPoint(Entities players, Coordinates location = null, bool ifPrevious = false)
        {
            if (location == null)
                EntityCommand(new Command("spawnpoint " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
            else
                EntityCommand(new Command("spawnpoint " + players.GetSelectorString() + " " + location.ToString(), CurrentType(ifPrevious)), players);
        }

        public void SpreadEntities(Coordinates center, double targetDistance, double range, bool respectTeams, Entities entities, bool ifPrevious = false)
        {
            EntityCommand(new Command("spreadplayers " + center.To2DString() + " " + targetDistance + " " + range + " " + respectTeams + " " + entities.GetSelectorString(), CurrentType(ifPrevious)), entities);
        }

        public void SetBlockStat(Coordinates location, string stat, Entities targets, string objective, bool ifPrevious = false)
        {
            CombineCommand(new Command("stats block " + location.ToString() + " set " + stat + " " + targets.GetSelectorString() + " " + objective, CurrentType(ifPrevious)));
        }

        public void ClearBlockStat(Coordinates location, string stat, bool ifPrevious = false)
        {
            CombineCommand(new Command("stats block " + location.ToString() + " clear " + stat, CurrentType(ifPrevious)));
        }

        public void SetEntityStat(Entities entities, string stat, Entities targets, string objective, bool ifPrevious = false)
        {
            CombineCommand(new Command("stats entity " + entities.GetSelectorString() + " set " + stat + " " + targets.GetSelectorString() + " " + objective, CurrentType(ifPrevious)));
        }

        public void ClearEntityStat(Entities entities, string stat, bool ifPrevious = false)
        {
            CombineCommand(new Command("stats entity " + entities.GetSelectorString() + " clear " + stat, CurrentType(ifPrevious)));
        }

        public void SummonEntity(EntityType type, Coordinates location, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (type == EntityType.ANY)
                throw new ArgumentException("Cannot summon an entity with type ANY.");
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("summon " + type.ToString() + " " + location.ToString() + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void TestForEntity(Entities entities, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("testfor " + entities.GetSelectorString() + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void TestForBlock(Coordinates location, string block, int dataValue = -1, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            CombineCommand(new Command("testforblock " + location.ToString() + " " + block + " " + dataValue + " " + NBT.ToString(), CurrentType(ifPrevious)));
        }

        public void TestForBlocks(Area source, Coordinates target, string mode = "all", bool ifPrevious = false)
        {
            CombineCommand(new Command("testforblocks " + source.ToString() + " " + target.ToString() + " " + mode, CurrentType(ifPrevious)));
        }

        public void SetTime(int value, bool ifPrevious = false)
        {
            CombineCommand(new Command("time set " + value, CurrentType(ifPrevious)));
        }

        public void AddTime(int value, bool ifPrevious = false)
        {
            CombineCommand(new Command("time add " + value, CurrentType(ifPrevious)));
        }

        public void QueryTime(string value, bool ifPrevious = false)
        {
            CombineCommand(new Command("time add " + value, CurrentType(ifPrevious)));
        }

        public void ToggleDownfall(bool ifPrevious = false)
        {
            CombineCommand(new Command("toggledownfall", CurrentType(ifPrevious)));
        }

        public void TeleportToLocation(Entities entities, Coordinates target, Rotation rotation = null, bool ifPrevious = false)
        {
            EntityCommand(new Command("tp " + entities.GetSelectorString() + " " + target.ToString() + (rotation == null ? "" : " " + rotation.ToString()), CurrentType(ifPrevious)), entities);
        }

        public void TeleportToEntity(Entities entities, Entities target, bool ifPrevious = false)
        {
            CombineCommand(new Command("tp " + entities.GetSelectorString() + " " + target.GetSelectorString(), CurrentType(ifPrevious)));
        }

        public void AddToTrigger(string objective, int value, bool ifPrevious = false)
        {
            if (execution == null)
                throw new Exception("Call PushExecutionAs() first.");
            CombineCommand(new Command("trigger " + objective + " add " + value, CurrentType(ifPrevious)));
        }

        public void SetTrigger(string objective, int value, bool ifPrevious = false)
        {
            if (execution == null)
                throw new Exception("Call PushExecutionAs() first.");
            CombineCommand(new Command("trigger " + objective + " set " + value, CurrentType(ifPrevious)));
        }

        public void SetWeather(string weather, int duration = 9000, bool ifPrevious = false)
        {
            CombineCommand(new Command("weather " + weather + " " + duration, CurrentType(ifPrevious)));
        }

        public void AddExperience(Entities players, int amount, bool ifPrevious = false)
        {
            EntityCommand(new Command("xp " + amount + " " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void AddExperienceLevels(Entities players, int amount, bool ifPrevious = false)
        {
            EntityCommand(new Command("xp " + amount + "L " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        public void RemoveExperience(Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("xp -2147483648L " + players.GetSelectorString(), CurrentType(ifPrevious)), players);
        }

        // ---------------------------
        // WORLDBORDER
        // ---------------------------

        public void IncreaseWorldBorder(int distance, int time = 0, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder add " + distance + " " + time, CurrentType(ifPrevious)));
        }

        public void SetWorldBorder(int distance, int time = 0, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder set " + distance + " " + time, CurrentType(ifPrevious)));
        }

        public void SetWorldBorderCenter(Coordinates center, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder center " + center.To2DString(), CurrentType(ifPrevious)));
        }

        public void SetWorldBorderDamageRate(int damagePerBlock, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder damage amount " + damagePerBlock, CurrentType(ifPrevious)));
        }

        public void SetWorldBorderDamageBuffer(int distance, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder damage buffer " + distance, CurrentType(ifPrevious)));
        }

        public void SetWorldBorderWarningDistance(int distance, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder warning distance " + distance, CurrentType(ifPrevious)));
        }

        public void SetWorldBorderWarningTime(int time, bool ifPrevious = false)
        {
            CombineCommand(new Command("worldborder warning time " + time, CurrentType(ifPrevious)));
        }

        // ---------------------------
        // TITLE
        // ---------------------------

        public void ClearTitle(Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("title " + players.GetSelectorString() + " clear", CurrentType(ifPrevious)), players);
        }

        public void ResetTitle(Entities players, bool ifPrevious = false)
        {
            EntityCommand(new Command("title " + players.GetSelectorString() + " reset", CurrentType(ifPrevious)), players);
        }

        public void SetTitleTimes(Entities players, int fadeIn, int show, int fadeOut, bool ifPrevious = false)
        {
            EntityCommand(new Command("title " + players.GetSelectorString() + " times " + fadeIn + " " + show + " " + fadeOut, CurrentType(ifPrevious)), players);
        }

        public void SetSubtitle(Entities players, RawText title, bool ifPrevious = false)
        {
            EntityCommand(new Command("title " + players.GetSelectorString() + " subtitle " + title.ToString(), CurrentType(ifPrevious)), players);
        }

        public void ShowTitle(Entities players, RawText title, bool ifPrevious = false)
        {
            EntityCommand(new Command("title " + players.GetSelectorString() + " title " + title.ToString(), CurrentType(ifPrevious)), players);
        }

        // ---------------------------
        // SCOREBOARD
        // ---------------------------

        // Objectives

        public void AddScoreboardObjective(string objective, string criteria, string displayName = "", bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard objectives add " + objective + " " + criteria + " " + displayName, CurrentType(ifPrevious)));
        }

        public void RemoveScoreboardObjective(string objective, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard objectives remove " + objective, CurrentType(ifPrevious)));
        }

        public void ShowScoreboardObjective(string objective, string slot, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard objectives setdisplay " + slot + " " + objective, CurrentType(ifPrevious)));
        }

        public void HideScoreboard(string slot, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard objectives setdisplay " + slot, CurrentType(ifPrevious)));
        }

        // Players

        public void SetScore(Entities entities, string objective, int score, NBTObject NBT = null, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players set " + entities.GetSelectorString() + " " + objective + " " + score + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        public void AddToScore(Entities entities, string objective, int amount, NBTObject NBT = null, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players add " + entities.GetSelectorString() + " " + objective + " " + amount + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        public void RemoveFromScore(Entities entities, string objective, int amount, NBTObject NBT = null, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players remove " + entities.GetSelectorString() + " " + objective + " " + amount + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        public void ResetScore(Entities entities, string objective, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players reset " + entities.GetSelectorString() + " " + objective, CurrentType(ifPrevious)), entities);
        }

        public void EnableTrigger(Entities players, string trigger, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players enable " + players.GetSelectorString() + " " + trigger, CurrentType(ifPrevious)), players);
        }

        public void TestForScore(Entities entities, string objective, int min, int max = 2147483647, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard players test " + entities.GetSelectorString() + " " + objective + " " + min + " " + max, CurrentType(ifPrevious)), entities);
        }

        public void ModifyScore(Entities targetEntities, string targetObjective, string operation, Entities sourceEntities, string sourceObjective = null, bool ifPrevious = false)
        {
            if (sourceObjective == null)
                sourceObjective = targetObjective;
            CombineCommand(new Command("scoreboard players operation " + targetEntities.GetSelectorString() + " " + targetObjective + " " + operation + " " + sourceEntities.GetSelectorString() + " " + sourceObjective, CurrentType(ifPrevious)));
        }

        // Teams

        public void AddTeam(string name, string displayName = "", bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard teams add " + name + " " + displayName, CurrentType(ifPrevious)));
        }

        public void RemoveTeam(string team, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard teams remove " + team, CurrentType(ifPrevious)));
        }

        public void EmptyTeam(string team, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard teams empty " + team, CurrentType(ifPrevious)));
        }

        public void AddEntityToTeam(Entities entities, string team, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard teams join " + team + entities.GetSelectorString(), CurrentType(ifPrevious)), entities);
        }

        public void RemoveEntityFromTeam(Entities entities, string team, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard teams leave " + team + entities.GetSelectorString(), CurrentType(ifPrevious)), entities);
        }

        public void RemoveEntityFromAllTeams(Entities entities, bool ifPrevious = false)
        {
            EntityCommand(new Command("scoreboard teams leave " + entities.GetSelectorString(), CurrentType(ifPrevious)), entities);
        }

        public void SetTeamOption(string team, string option, string value, bool ifPrevious = false)
        {
            CombineCommand(new Command("scoreboard teams option " + team + " " + option + " " + value, CurrentType(ifPrevious)));
        }

        // Tags

        public void AddTag(string tag, Entities entities, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            EntityCommand(new Command("scoreboard players tag " + entities.GetSelectorString() + " add " + tag + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        public void RemoveTag(string tag, Entities entities, NBTObject NBT = null, bool ifPrevious = false)
        {
            if (NBT == null)
                NBT = new NBTObject();
            EntityCommand(new Command("scoreboard players tag " + entities.GetSelectorString() + " remove " + tag + " " + NBT.ToString(), CurrentType(ifPrevious)), entities);
        }

        private CommandType CurrentType(bool ifPrevious)
        {
            return ifPrevious ? CommandType.CHAIN_CONDITIONAL : next;
        }
    }
}

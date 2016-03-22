using System;
using System.Collections.Generic;

namespace MCGL
{
    public class NBTObject
    {
        public static readonly NBTObject InGround = new NBTObject().Set("inGround", true);

        public static NBTObject Combine(params NBTObject[] objects)
        {
            NBTObject NBT = new NBTObject();

            foreach (var part in objects)
            {
                foreach (var data in part.data)
                {
                    NBT.SetCustom(data.Key, data.Value);
                }
            }

            return NBT;
        }

        public readonly Dictionary<string, string> data = new Dictionary<string, string>();

        public NBTObject Set(string key, string value)
        {
            data[key] = "\"" + value + "\"";
            return this;
        }

        public NBTObject Set(string key, string[] values)
        {
            data[key] = "[\"" + String.Join("\",\"", values) + "\"]";
            return this;
        }

        public NBTObject Set(string key, int value)
        {
            data[key] = value.ToString() + "s";
            return this;
        }

        public NBTObject Set(string key, int[] values)
        {
            data[key] = "[" + String.Join("s,", values) + "s]";
            return this;
        }

        public NBTObject Set(string key, double value)
        {
            data[key] = value.ToString() + "d";
            return this;
        }

        public NBTObject Set(string key, double[] values)
        {
            data[key] = "[" + String.Join("d,", values) + "d]";
            return this;
        }

        public NBTObject Set(string key, Boolean value)
        {
            data[key] = value ? "1b" : "0b";
            return this;
        }

        public NBTObject Set(string key, NBTObject value)
        {
            data[key] = value.ToString();
            return this;
        }

        public NBTObject Set(string key, NBTObject[] values)
        {
            var result = "[";
            foreach (var value in values)
            {
                result += value.ToString() + ",";
            }
            result = result.Substring(0, result.Length - 1);
            result += "]";

            data[key] = result;
            return this;
        }

        public NBTObject Set(string key, NBTArray value)
        {
            data[key] = value.ToString();
            return this;
        }

        public NBTObject SetCustom(string key, string value)
        {
            data[key] = value;
            return this;
        }

        new public string ToString()
        {
            var result = "{";
            foreach (var part in data.Keys)
            {
                result += part + ":" + data[part] + ",";
            }
            if (data.Keys.Count > 0) result = result.Substring(0, result.Length - 1);
            result += "}";

            return result;
        }
    }

    public class NBTArray
    {
        List<string> data = new List<string>();

        public NBTArray Add(string value)
        {
            data.Add("\"" + value + "\"");
            return this;
        }

        public NBTArray Add(int value)
        {
            data.Add(value.ToString() + "s");
            return this;
        }

        public NBTArray Add(double value)
        {
            data.Add(value.ToString() + "d");
            return this;
        }

        public NBTArray Add(Boolean value)
        {
            data.Add(value ? "1b" : "0b");
            return this;
        }

        public NBTArray Add(NBTObject value)
        {
            data.Add(value.ToString());
            return this;
        }

        new public string ToString()
        {
            var result = "[";
            result += String.Join(",", data);
            result += "]";
            return result;
        }
    }

    public enum TextColor
    {
        BLACK,
        DARK_BLUE,
        DARK_GREEN,
        DARK_AQUA,
        DARK_RED,
        DARK_PURPLE,
        GOLD,
        GRAY,
        DARK_GRAY,
        BLUE,
        GREEN,
        AQUA,
        RED,
        LIGHT_PURPLE,
        YELLOW,
        WHITE,
        NONE
    }

    public enum TextClickEvent
    {
        RUN_COMMAND,
        SUGGEST_COMMAND,
        OPEN_URL,
        CHANGE_PAGE
    }

    public enum TextHoverEvent
    {
        SHOW_TEXT,
        SHOW_ITEM,
        SHOW_ENTITY,
        SHOW_ACHIEVEMENT
    }

    public class RawText : NBTArray
    {
        TextColor defaultColor;

        public RawText(TextColor defaultColor = TextColor.NONE)
        {
            this.defaultColor = defaultColor;
            Add("");
        }

        public RawText Add(string text, TextColor color = TextColor.NONE)
        {
            color = color == TextColor.NONE ? defaultColor : color;
            Add(new TextElement(text, color));
            return this;
        }
    }

    public class TextElement : NBTObject
    {
        public TextElement(string text, TextColor color = TextColor.NONE)
        {
            Set("text", text);
            Set("color", color.ToString().ToLower());
        }

        public TextElement SetClickEvent(TextClickEvent clickEvent, string value)
        {
            Set("clickEvent", new NBTObject()
                .Set("action", clickEvent.ToString().ToLower())
                .Set("value", value));
            return this;
        }

        public TextElement SetHoverEvent(TextHoverEvent hoverEvent, object value)
        {
            Set("clickEvent", new NBTObject()
                .Set("action", hoverEvent.ToString().ToLower())
                .Set("value", value.ToString()));
            return this;
        }

        public TextElement SetInsertion(string value)
        {
            Set("insertion", value);
            return this;
        }
    }

    public class ScoreElement : NBTObject
    {
        public ScoreElement(Entities selector, string objective)
        {
            Set("score", new NBTObject()
                .Set("name", selector.ToString())
                .Set("objective", objective));
        }
    }

    public class SelectorElement : NBTObject
    {
        public SelectorElement(Entities selector)
        {
            Set("selector", selector.ToString());
        }
    }
}

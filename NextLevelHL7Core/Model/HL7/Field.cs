using System;
using System.Collections.Generic;

namespace NextLevelHL7
{
    public class Field
    {
        public static char FieldDelimiter { get; set; } = '^';

        private Dictionary<int, string> _Fields = new Dictionary<int, string>();
        public string Value { get; private set; }

        public Field()
        {
        }

        public Field(string input)
        {
            Parse(input);
        }

        public string GetSubSegment(int index)
        {
            if (_Fields.ContainsKey(index))
                return _Fields[index];
            return null;
        }
        
        public void SetSubSegment(int index, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _Fields[index] = value;
            else
                _Fields.Remove(index);
        }

        public string this[int index]
        {
            get
            {
                return GetSubSegment(index);
            }
            set
            {
                SetSubSegment(index, value);
            }
        }

        public void Parse(string input)
        {
            Value = input;
            string[] tokens = input.Split(new char[] { FieldDelimiter }, StringSplitOptions.None);
            for (int i = 0; i < tokens.Length; i++)
                SetSubSegment(i, tokens[i]);
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(Field field)
        {
            return field.ToString();
        }

        public static implicit operator bool(Field field)
        {
            return !string.IsNullOrEmpty(field.Value);
        }
    }
}
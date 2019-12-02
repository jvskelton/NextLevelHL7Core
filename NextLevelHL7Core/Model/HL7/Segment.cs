using System;
using System.Collections.Generic;
using System.Text;

namespace NextLevelHL7
{
    public class Segment
    {
        public static char FieldDelimiter { get; set; } = '|';

        private Dictionary<int, string> _Fields = new Dictionary<int, string>(20);

        public Segment()
        {
        }

        public Segment(string name)
        {
            _Fields.Add(0, name);
        }

        public string Name
        {
            get
            {
                if (!_Fields.ContainsKey(0)) return string.Empty;
                return _Fields[0];
            }
        }

        public string GetField(int index)
        {
            if (Name == "MSH" && index == 1)
                return "|";

            if (!_Fields.ContainsKey(index))
                return string.Empty;

            return _Fields[index];
        }

        public void SetField(int index, string value)
        {
            if (Name == "MSH" && index == 1)
                return;

            if (string.IsNullOrEmpty(value))
                _Fields.Remove(index);
            else
                _Fields[index] = value;
        }

        public Field this[int index]
        {
            get
            {
                Field field = new Field(GetField(index));
                return field;
            }
        }

        public void Parse(string text)
        {
            int count = 0;
            char[] delimiter = { FieldDelimiter };

            string temp = text.Trim(FieldDelimiter);
            var tokens = temp.Split(delimiter, StringSplitOptions.None);

            foreach (var item in tokens)
            {
                SetField(count, item);
                if (item == "MSH")
                    ++count;
                ++count;
            }
        }

        public string Serialize()
        {
            int max = 0;
            foreach (var field in _Fields)
                if (max < field.Key) max = field.Key;

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i <= max; i++)
            {
                if (_Fields.ContainsKey(i))
                {
                    stringBuilder.Append(_Fields[i]);
                    if (i == 0 && Name == "MSH")
                        ++i;
                }
                if (i != max)
                    stringBuilder.Append(FieldDelimiter);
            }
            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            return base.ToString();
        }
    }
}
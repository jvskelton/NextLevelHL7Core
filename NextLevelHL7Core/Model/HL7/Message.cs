using NextLevelHL7.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace NextLevelHL7
{
    public class Message
    {
        public static string HL7NewLineSequence { get; set; } = "\r";
        public string Text { get; private set; }

        private const string MSH = "MSH";
        private const int MSHMsgTime = 7;
        private const int MSHMsgType = 9;
        private const int MSHMsgControlId = 10;

        private LinkedList<Segment> _Segments;

        public Message()
        {
            Clear();
        }

        public void Clear()
        {
            _Segments = new LinkedList<Segment>();
        }

        protected Segment Header()
        {
            if (_Segments.Count == 0 || _Segments.First.Value.Name != MSH)
                return null;
            return _Segments.First.Value;
        }


        public string MessageType()
        {
            Segment msh = Header();
            if (msh == null)
                return string.Empty;
            
            return msh.GetField(MSHMsgType);
        }

        public string MessageControlId()
        {
            Segment msh = Header();
            if (msh == null)
                return string.Empty;
            return msh.GetField(MSHMsgControlId);
        }

        public DateTime? MessageDateTime()
        {
            Segment msh = Header();
            if (msh == null)
                return null;

            DateTime? messageDateTime = msh.GetField(MSHMsgTime).ParseDate();
            return messageDateTime;
        }

        public void Add(Segment segment)
        {
            if (!string.IsNullOrEmpty(segment.Name) && segment.Name.Length == 3)
                _Segments.AddLast(segment);
        }

        public Segment FindSegment(string name)
        {
            foreach (Segment segment in _Segments)
                if (segment.Name == name)
                    return segment;
            return null;
        }

        public Segment FindPreviousSegment(string name, Segment current)
        {
            var node = _Segments.Find(current);
            if ( node == null)
                throw new NullReferenceException();
            
            while (node.Previous != null)
            {
                node = node.Previous;
                if (node.Value.Name == name) return node.Value;
            }
            return null;
        }

        public Segment FindNextSegment(string name, Segment current)
        {
            var node = _Segments.Find(current);
            if (node == null)
                throw new NullReferenceException();

            while (node.Next != null)
            {
                node = node.Next;
                if (node.Value.Name == name)
                    return node.Value;
            }
            return null;
        }

        public List<Segment> FindSegments(string name)
        {
            List<Segment> segments = new List<Segment>();
            foreach (Segment segment in _Segments)
                if (segment.Name == name)
                    segments.Add(segment);
            return segments;
        }

        public void Parse(string text)
        {
            Text = text;

            Clear();

            char[] delimiters = { '\r' };
            string[] tokens = text.Split(delimiters, StringSplitOptions.None);
            
            foreach (string item in tokens)
            {
                Segment segment = new Segment();
                segment.Parse(item.Trim('\n'));
                Add(segment);
            }
        }

        public string Serialize()
        {
            StringBuilder stringBuilder = new StringBuilder();
            char[] delimiters = { '\r','\n' };

            foreach (Segment segment in _Segments)
            {
                stringBuilder.Append(segment.Serialize());                
                stringBuilder.Append(HL7NewLineSequence);
            }
            return stringBuilder.ToString().TrimEnd(delimiters);
        }

        public override string ToString()
        {
            return MessageType();
        }
    }
}

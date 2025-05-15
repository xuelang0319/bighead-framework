using System.Collections.Generic;
using System.Text;

namespace Bighead.Core.Generate
{
    public class GenEnum : GenBasic
    {
        private readonly Queue<GenEnumElement> _elements = new Queue<GenEnumElement>();
        private const string Type = "enum";

        public GenEnum(int charLength, string name) : base(charLength, name)
        {
        }

        public GenEnumElement AddEnumElement(string name, int value = 0)
        {
            var element = new GenEnumElement(CharLength + 4, name, value);
            _elements.Enqueue(element);
            return element;
        }

        public override StringBuilder StartGenerate()
        {
            var enumBuilder = new StringBuilder().Append(GetModifier).Append(Space).Append(Type).Append(Space).Append(Name);

            var contentBuilder = new StringBuilder();
            while (_elements.Count > 0)
            {
                var elementBuilder = _elements.Dequeue().StartGenerate();
                contentBuilder.Append(elementBuilder);
                if (_elements.Count > 0) contentBuilder.AppendLine();
            }

            enumBuilder.Append(AddBraces(contentBuilder));
            return enumBuilder;
        }
    }

    public class GenEnumElement : Core.Generate.GenBasic
    {
        private readonly int _value;
        
        public GenEnumElement(int charLength, string name, int value = 0) : base(charLength, name)
        {
            _value = value;
        }

        public override StringBuilder StartGenerate()
        {
            var builder = new StringBuilder().Append($"{Charactor}{Name}");
            builder = _value == 0 ? builder.Append(",") : builder.Append($" = {_value},");
            return builder;
        }
    }
}
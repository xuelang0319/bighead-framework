//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月23日   |   属性模板
//

using System.Text;

namespace Bighead.Core.Generate
{
    public class GenProperty : GenBasic
    {
        private readonly string _type;
        private string _value;
        private bool _get = true;
        private bool _set = true;
        private bool _override = false;
        public bool AsParamUseThis = false;

        public GenProperty(int charLength, string name, string type) : base(charLength, name)
        {
            _type = type;
        }

        public string AsParam()
        {
            return AsParamUseThis ? $"this {_type} {Name}" : $"{_type} {Name}";
        }

        public GenProperty SetSet(bool b)
        {
            _set = b;
            return this;
        }
        
        public GenProperty SetGet(bool b)
        {
            _get = b;
            return this;
        }

        public GenProperty SetValue(string value)
        {
            _value = value;
            return this;
        }

        public GenProperty SetOverrider(bool b)
        {
            _override = b;
            return this;
        }

        public override StringBuilder StartGenerate()
        {
            var getset = string.Empty;
            switch (_get)
            {
                case true when _set:
                    getset = " { get; set;}";
                    break;
                case true:
                    getset = " { get;}";
                    break;
                default:
                {
                    if (_set) getset = " { set;}";
                    break;
                }
            }
            
            var value = string.IsNullOrEmpty(_value) ? string.Empty : $" = {_value};";
            var Override = _override ? "override " : "";
            
            if (string.IsNullOrEmpty(getset) && string.IsNullOrEmpty(value)) getset = ";";
            return new StringBuilder($"{Charactor}{GetAnnotation()}{GetModifier} {Override}{_type} {Name}{getset}{value}");
        }
    }
}
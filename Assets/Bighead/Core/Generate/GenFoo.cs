//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月23日   |   方法模板
//

using System.Collections.Generic;
using System.Text;

namespace Bighead.Core.Generate
{
    public class GenFoo : GenBasic
    {
        private readonly string _returnType;
        private List<GenProperty> _params;
        private List<string> _details;
        private bool _isOverride = false;
        private bool _isConstruct = false;
        private bool _isVirtual = false;
        public bool Partial;

        public GenFoo(int charLength, string name, string returnType) : base(charLength, name)
        {
            _returnType = returnType;
        }
        
        public override StringBuilder StartGenerate()
        {
            var builder = new StringBuilder();

            builder.Append(Charactor);
            builder.Append(GetAnnotation());

            builder
                .Append(GetModifier)
                .Append(Space)
                .Append(Partial ? "partial " : "")
                .Append(_isOverride ? "override " : "")
                .Append(_isVirtual ? "virtual " : "")
                .Append(_isConstruct ? "" : _returnType + Space)
                .Append(Name);
            
            var paramBuilder = new StringBuilder();
            if (_params != null)
                for (int i = 0; i < _params.Count; i++)
                {
                    paramBuilder.Append(_params[i].AsParam());
                    if (i < _params.Count - 1)
                        paramBuilder.Append(", ");
                }

            builder.Append(AddBrackets(paramBuilder));

            var detailBuilder = new StringBuilder();
            var charactor = new string(' ', 4);
            if (_details != null)
                for (int i = 0; i < _details.Count; i++)
                {
                    // 由于要加入大括号，所以每个提前向后移动
                    detailBuilder
                        .Append(charactor)
                        .Append(_details[i]);
                    if (i < _details.Count - 1)
                        detailBuilder.AppendLine();
                }

            builder.Append(AddBraces(detailBuilder));
            return builder;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        public GenFoo AddParam(string type, string name, bool useThis = false)
        {
            if (_params == null) _params = new List<GenProperty>();
            var property = new GenProperty(CharLength + 4, name, type);
            property.AsParamUseThis = useThis;
            _params.Add(property);
            return this;
        }
        
        /// <summary>
        /// 设置Overrider
        /// </summary>
        public GenFoo SetOverrider(bool b)
        {
            _isOverride = b;
            return this;
        }

        /// <summary>
        /// 设置是否为构造方法
        /// </summary>
        public GenFoo SetConstruct(bool b)
        {
            _isConstruct = b;
            return this;
        }
        
        /// <summary>
        /// 设置是否为虚方法
        /// </summary>
        public GenFoo SetVirtual(bool b)
        {
            _isVirtual = b;
            return this;
        }

        /// <summary>
        /// 添加方法代码行
        /// </summary>
        public GenFoo AddDetail(string line)
        {
            if (_details == null) _details = new List<string>();
            _details.Add(Charactor + line);
            return this;
        }
    }
}
//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月22日   |   类模板,当前版本没有提供接口
//

using System.Collections.Generic;
using System.Text;

namespace Bighead.GenBasic
{
    public class GenClass : Bighead.GenBasic.GenBasic
    {
        private readonly List<string> Attributes = new List<string>();
        private readonly List<string> Usings = new List<string>();
        private readonly List<GenFoo> Foos = new List<GenFoo>();
        private readonly List<GenProperty> Properties = new List<GenProperty>();
        private readonly List<GenStruct> Structs = new List<GenStruct>();
        public string Parent;
        public string virtualType;
        private string Namespace;
        public bool IsPartial = false;

        public GenClass(int charLength, string name) : base(charLength, name)
        {
        }

        /// <summary>
        /// 添加方法
        /// </summary>
        public GenFoo AddFoo(string name, string returnType)
        {
            var foo = new GenFoo(CharLength + 4, name, returnType);
            Foos.Add(foo);
            return foo;
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        public GenProperty AddProperty(string name, string type)
        {
            var property = new GenProperty(CharLength + 4, name, type);
            Properties.Add(property);
            return property;
        }

        /// <summary>
        /// 添加结构类型
        /// </summary>
        public GenStruct AddStruct(string name)
        {
            var genStruct = new GenStruct(CharLength, name);
            Structs.Add(genStruct);
            return genStruct;
        }

        /// <summary>
        /// 获取引用
        /// </summary>
        private string GetUsing(string str)
        {
            return "using " + str + ";" + CharNewLine;
        }

        /// <summary>
        /// 添加引用
        /// </summary>
        public GenClass AddUsing(string str)
        {
            Usings.Add(str);
            return this;
        }
        
        /// <summary>
        /// 添加引用
        /// </summary>
        public GenClass AddUsing(IEnumerable<string> str)
        {
            Usings.AddRange(str);
            return this;
        }

        /// <summary>
        /// 添加特性
        /// </summary>
        public GenClass AddAttributes(string str)
        {
            Attributes.Add(str);
            return this;
        }

        public void SetNameSpace(string nameSpace)
        {
            Namespace = nameSpace;
            AddCharactor(4);

            for (int i = 0; i < Properties.Count; i++)
            {
                Properties[i].AddCharactor(4);
            }

            for (int i = 0; i < Foos.Count; i++)
            {
                Foos[i].AddCharactor(4);
            }
        }

        /// <summary>
        /// 开始生成
        /// </summary>
        public override StringBuilder StartGenerate()
        {
            var builder = new StringBuilder();

            var hasUsing = false;
            for (var i = 0; i < Usings.Count; i++)
            {
                builder.Append(GetUsing(Usings[i]));
                hasUsing = true;
            }

            for (int i = 0; i < Structs.Count; i++)
            {
                var tempStruct = Structs[i];
                if (tempStruct.Usings.Count > 0)
                {
                    hasUsing = true;
                    for (int j = 0; j < tempStruct.Usings.Count; j++)
                    {
                        var tempUsing = tempStruct.Usings[j];
                        if(Usings.Contains(tempUsing)) continue;
                        builder.Append(GetUsing(tempUsing));
                    }
                }
            }

            if (hasUsing) builder.Append(CharNewLine);

            if (!string.IsNullOrEmpty(Namespace))
            {
                builder
                    .Append(CharNewLine)
                    .Append($"namespace {Namespace}")
                    .Append(CharNewLine)
                    .Append("{")
                    .Append(CharNewLine);
            }

            builder.Append(GetAnnotation());
            
            if (Attributes.Count > 0)
            {
                builder.Append(CharNewLine + new string(' ', string.IsNullOrEmpty(Namespace)? 0 : 4));
                foreach (var attribute in Attributes)
                {
                    builder.Append($"[{attribute}]");
                }

                builder.Append(CharNewLine);
            }

            builder.Append(new string(' ', string.IsNullOrEmpty(Namespace)? 0 : 4) + GetModifier + Space + (IsPartial ? "partial " : "") + "class" + Space + Name);
            
            if (Parent != null)
            {
                builder.Append(" : " + Parent);
            }

            if (!string.IsNullOrEmpty(virtualType))
            {
                builder.Append($"<{virtualType}>");
            }

            var detailBuilder = new StringBuilder();
            for (int i = 0; i < Properties.Count; i++)
            {
                detailBuilder.Append(Properties[i].StartGenerate());
                if (i < Properties.Count - 1) detailBuilder.Append(CharNewLine);
            }

            if (Properties.Count > 0 && Foos.Count > 0) detailBuilder.AppendLine().Append(CharNewLine);

            for (int i = 0; i < Foos.Count; i++)
            {
                detailBuilder.Append(Foos[i].StartGenerate());
                if (i < Foos.Count - 1) detailBuilder.Append(CharNewLine);
            }

            builder.AppendLine().Append(AddBraces(detailBuilder, false));


            if (!string.IsNullOrEmpty(Namespace))
            {
                builder
                    .Append(CharNewLine)
                    .Append("}");
            }

            for (int i = 0; i < Structs.Count; i++)
            {
                builder.Append(Structs[i].AsSubStruct());
            }
            
            return builder;
        }
    }
}
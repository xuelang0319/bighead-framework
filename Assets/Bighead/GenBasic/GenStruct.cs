using System.Collections.Generic;
using System.Text;

namespace Bighead.GenBasic
{
    public class GenStruct : Bighead.GenBasic.GenBasic
    {
        private readonly List<string> Attributes = new List<string>();
        public readonly List<string> Usings = new List<string>();
        private readonly List<string> Interfaces = new List<string>();
        private readonly List<GenFoo> Foos = new List<GenFoo>();
        private readonly List<GenProperty> Properties = new List<GenProperty>();

        private string Namespace;
        
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
        /// 获取引用
        /// </summary>
        private string GetUsing(string str)
        {
            return "using " + str + ";" + CharNewLine;
        }
        
        /// <summary>
        /// 添加引用
        /// </summary>
        public GenStruct AddUsing(string str)
        {
            Usings.Add(str);
            return this;
        }

        /// <summary>
        /// 添加特性
        /// </summary>
        public GenStruct AddAttributes(string str)
        {
            Attributes.Add(str);
            return this;
        }
        
        /// <summary>
        /// 设置命名空间
        /// </summary>
        /// <param name="nameSpace"></param>
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
        /// 设置接口
        /// </summary>
        /// <param name="interfaceName"></param>
        public GenStruct SetInterface(string interfaceName)
        {
            Interfaces.Add(interfaceName);
            return this;
        }

        public GenStruct(int charLength, string name) : base(charLength, name)
        {
            
        }

        public string AsSubStruct()
        {
           var builder = new StringBuilder();

           var hasNameSpace = false;
            if (!string.IsNullOrEmpty(Namespace))
            {
                hasNameSpace = true;
                builder
                    .Append(CharNewLine)
                    .Append($"namespace {Namespace}")
                    .Append(CharNewLine)
                    .Append("{");
            }
            
            var annotation = GetAnnotation(hasNameSpace ? 4 : 0);
            if (annotation.Length > 0)
            {
                builder.Append(annotation);
            }
            else
            {
                builder.Append(CharNewLine);
            }
            
            if (Attributes.Count > 0)
            {
                var offsetStr = new string(' ', hasNameSpace ? 4 : 0); 
                foreach (var attribute in Attributes)
                {
                    builder.Append(offsetStr + $"[{attribute}]").Append(CharNewLine);
                }
            }
            
            builder.Append(new string(' ', string.IsNullOrEmpty(Namespace)? 0 : 4) + GetModifier + Space + "struct" + Space + Name);

            if (Interfaces.Count > 0)
            {
                var interfaceStr = string.Join(", ", Interfaces);
                builder.Append(" : ").Append(interfaceStr);
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

            return builder.ToString();
        }

        public override StringBuilder StartGenerate()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < Usings.Count; i++)
            {
                builder.Append(GetUsing(Usings[i]));
                if (i == Usings.Count - 1) builder.Append(CharNewLine);
            }
            
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
                builder.Append(CharNewLine + new string(' ', (string.IsNullOrEmpty(Namespace)? 0 : 4) + CharLength));
                foreach (var attribute in Attributes)
                {
                    builder.Append($"[{attribute}]");
                }

                builder.Append(CharNewLine);
            }
            
            builder.Append(new string(' ', string.IsNullOrEmpty(Namespace)? 0 : 4) + GetModifier + Space + "struct" + Space + Name);

            if (Interfaces.Count > 0)
            {
                var interfaceStr = string.Join(", ", Interfaces);
                builder.Append(" : ").Append(interfaceStr);
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

            return builder;
        }
    }
}
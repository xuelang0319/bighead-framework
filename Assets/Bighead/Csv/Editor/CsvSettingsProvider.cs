#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Bighead.Csv; // 引用 CsvSettings / CsvTypeMappingEntry

namespace Bighead.Csv.Editor
{
    public sealed class CsvSettingsProvider : SettingsProvider
    {
        private SerializedObject _so;
        private CsvSettings _settings;

        public CsvSettingsProvider(string path, SettingsScope scope) : base(path, scope)
        {
        }

        public static bool IsSettingsAvailable() => File.Exists(CsvSettingsCreator.AssetPath);

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            if (!IsSettingsAvailable()) CsvSettingsCreator.EnsureCreatedOnce();
            var provider = new CsvSettingsProvider("Project/Bighead Csv", SettingsScope.Project)
            {
                keywords = new HashSet<string>(new[]
                {
                    "Csv", "Excel", "Bytes", "Code", "Namespace", "Load", "Fragment", "TypeMap", "Channel", "Encoding", "NameMode"
                })
            };
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = AssetDatabase.LoadAssetAtPath<CsvSettings>(CsvSettingsCreator.AssetPath);
            if (_settings != null)
            {
                _settings.Normalize();
                _so = new SerializedObject(_settings);
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("CsvSettings 未找到或未编译通过。", MessageType.Error);
                if (GUILayout.Button("创建/修复 CsvSettings")) CsvSettingsCreator.OpenSettings();
                return;
            }

            _settings.Normalize();
            if (_so == null) _so = new SerializedObject(_settings);
            _so.Update();

            // 路径
            DrawPath("ExcelFolder", "Excel Folder");
            DrawPath("CsvOutFolder", "CSV Out Folder");
            DrawPath("BytesOutFolder", "Bytes Out Folder");
            DrawPath("CodeOutFolder", "Code Out Folder");

            // 代码
            EditorGUILayout.Space(8);
            SafePropField("CodeNamespace", "Code Namespace");

            // 加载与分片
            EditorGUILayout.Space(8);
            SafePropField("DefaultLoadStrategy", "Load Strategy");
            SafePropField("DefaultFragmentSize", "Fragment Size");
            SafePropField("DefaultIndexStrategy", "Index Strategy");
            SafePropField("KeyJoiner", "Key Joiner");

            if (_settings.DefaultLoadStrategy == LoadStrategy.Eager)
                EditorGUILayout.HelpBox("加载方式：Eager（启动即全量加载）—— 分片与索引策略无效", MessageType.Info);
            else if (_settings.DefaultFragmentSize <= 0)
                EditorGUILayout.HelpBox("Lazy 模式下 FragmentSize <= 0：视为不分片（按需但不切片）", MessageType.Info);

            // —— 产物 —— 
            EditorGUILayout.Space(8);
            SafePropField("BytesFormat", "Bytes Data Format"); // Raw/Base64/GZipBase64
            EditorGUILayout.HelpBox("说明：这是 Bytes 文件的数据格式，不是加密。Raw=原始字节，Base64/GZipBase64=以文本形式存储。", MessageType.Info);
            SafePropField("EnableEncryption", "Enable Encryption");
            if (_settings.EnableEncryption)
            {
                SafePropField("EncryptionKeyProvider", "Key Provider (optional)");
                SafePropField("FixedEncryptionKey",    "Fixed Key (fallback)");
                EditorGUILayout.HelpBox("若同时设置 Provider 与 Fixed Key，则优先使用 Provider 返回的密钥。", MessageType.None);
            }

            SafePropField("BytesEnableCRC", "Bytes CRC32");

            // —— CSV 额外 —— 
            EditorGUILayout.Space(4);
            SafePropField("CsvUtf8WithBom", "CSV UTF8 with BOM");

            // 类型映射
            EditorGUILayout.Space(8);
            var typeMapsProp = _so.FindProperty("TypeMappings");
            if (typeMapsProp != null) EditorGUILayout.PropertyField(typeMapsProp, true);

            var dupCsvTypes = FindDuplicatedCsvTypes(_settings.TypeMappings);
            if (dupCsvTypes.Count > 0)
                EditorGUILayout.HelpBox("类型映射 csvType 存在重复: " + string.Join(", ", dupCsvTypes), MessageType.Warning);

            // 覆写
            EditorGUILayout.Space(8);
            var overridesProp = _so.FindProperty("Overrides");
            if (overridesProp != null) EditorGUILayout.PropertyField(overridesProp, true);

            if (_settings.Overrides != null)
            {
                foreach (var ov in _settings.Overrides)
                {
                    if (ov == null) continue;
                    if (ov.keyColumns != null && ov.keyColumns.Any(i => i < 0))
                        EditorGUILayout.HelpBox($"覆写 {ov.excelName}${ov.sheetName} 的 KeyColumns 含负数索引",
                            MessageType.Error);
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建目录")) CreateFolders(_settings);
            if (GUILayout.Button("打开 Excel 目录")) RevealDir(PathUtil.Rel2Abs(_settings.ExcelFolder));
            if (GUILayout.Button("打开 Bytes 目录")) RevealDir(PathUtil.Rel2Abs(_settings.BytesOutFolder));
            if (GUILayout.Button("打开 Code 目录")) RevealDir(PathUtil.Rel2Abs(_settings.CodeOutFolder));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Generate All（读取→生成→原子替换）", GUILayout.Height(30)))
                CsvGeneratePipeline.GenerateAll(_settings);

            _so.ApplyModifiedProperties();
        }

        private void SafePropField(string propName, string label)
        {
            var prop = _so.FindProperty(propName);
            if (prop != null) EditorGUILayout.PropertyField(prop, new GUIContent(label));
            else EditorGUILayout.HelpBox($"找不到字段: {propName}", MessageType.Error);
        }

        private void DrawPath(string propName, string label)
        {
            var prop = _so.FindProperty(propName);
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"找不到路径字段: {propName}", MessageType.Error);
                return;
            }

            const float btnW = 28f;
            const float gap = 4f;

            // 取一行 rect（单行高度）
            var row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            // 画 label，并得到去掉 label 后的可用区域
            var contentRect = EditorGUI.PrefixLabel(row, new GUIContent(label));

            // 拆成 文本框 + 按钮
            var fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width - (btnW + gap),
                contentRect.height);
            var btnRect = new Rect(fieldRect.xMax + gap, contentRect.y, btnW, contentRect.height);

            EditorGUI.BeginChangeCheck();
            var newVal = EditorGUI.TextField(fieldRect, prop.stringValue ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
                prop.stringValue = newVal;

            if (GUI.Button(btnRect, "…"))
            {
                // 允许选择 Assets 外目录
                var pickStart = string.IsNullOrEmpty(prop.stringValue)
                    ? Application.dataPath
                    : PathUtil.Rel2Abs(prop.stringValue);
                var abs = EditorUtility.OpenFolderPanel(label, pickStart, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    // 始终转换为“相对 Assets”的路径（可能包含 ../）
                    prop.stringValue = PathUtil.Abs2RelFromAssets(abs);
                }
            }
        }


        // —— Utils ——
        private static void CreateFolders(CsvSettings s)
        {
            EnsureDir(Rel2Abs(s.ExcelFolder));
            EnsureDir(Rel2Abs(s.CsvOutFolder));
            EnsureDir(Rel2Abs(s.BytesOutFolder));
            EnsureDir(Rel2Abs(s.CodeOutFolder));
            AssetDatabase.Refresh();
        }

        private static string Rel2Abs(string rel)
        {
            rel = string.IsNullOrWhiteSpace(rel) ? string.Empty : rel.Trim().Replace('\\', '/');
            if (string.IsNullOrEmpty(rel)) return Application.dataPath;
            return Path.Combine(Application.dataPath, rel).Replace('\\', '/');
        }

        private static void EnsureDir(string abs)
        {
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }

        private static void RevealDir(string abs)
        {
            EditorUtility.RevealInFinder(abs);
        }

        private static List<string> FindDuplicatedCsvTypes(List<CsvTypeMappingEntry> maps)
        {
            if (maps == null) return new List<string>();
            return maps.Where(m => m != null && !string.IsNullOrWhiteSpace(m.csvType))
                .GroupBy(m => m.csvType)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
        }
    }
}
#endif
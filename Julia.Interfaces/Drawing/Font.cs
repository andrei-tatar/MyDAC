using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Julia.Interfaces.Drawing
{
    public class Font
    {
        private readonly Dictionary<char, Image> _chars;

        public string Name { get; private set; }

        public Image GetImage(char c)
        {
            Image found;
            return _chars.TryGetValue(c, out found) ? found : null;
        }

        public void Measure(string text, out int width, out int height)
        {
            width = 0;
            height = 0;

            foreach (var ch in text)
            {
                var img = GetImage(ch);
                if (img == null) continue;

                width += img.Width;
                height = Math.Max(height, img.Height);
            }
        }

        public Font(string xmlFilePath)
        {
            _chars = new Dictionary<char, Image>();

            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            var dataElements = doc.GetElementsByTagName("data");
            if (dataElements.Count != 1)
                throw new Exception("Invalid file");

            var dataNode = dataElements.Item(0);
            if (!dataNode.Attributes["type"].Value.Equals("font", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid file");

            Name = dataNode.Attributes["name"].Value;

            var charsNode = dataNode.SelectNodes("chars").Item(0);
            foreach (XmlNode charNode in charsNode.ChildNodes)
            {
                var charValue = charNode.Attributes["character"].Value[0];
                var pictureData = Convert.FromBase64String(charNode.InnerText);
                using (var stream = new MemoryStream(pictureData))
                {
                    var bitmap = System.Drawing.Image.FromStream(stream);
                    _chars.Add(charValue, Image.FromBitmap(bitmap, color => (color & 0xFF) < 150 ? Color.Mask : Color.Transparent));
                }
            }
        }
    }
}
